using System.IO;
using System.Xml.Linq;

namespace InitialForce.GitHubActions.ReSharper.Report.Services
{
    public class ParseService
    {
        public List<IssueDto> ParseIssues(ActionInputs inputs)
        {
            if (!File.Exists(inputs.InspectionFile))
            {
                var currentDir = Directory.GetCurrentDirectory();
                var absolutePath = Path.GetFullPath(inputs.InspectionFile);
                var fileName = Path.GetFileName(inputs.InspectionFile);
                
                // Get workspace root (GitHub Actions workspace or parent directories)
                var workspaceRoot = GetWorkspaceRoot(currentDir);
                
                // Search for inspection files in multiple locations
                var searchSuggestions = new List<string>();
                
                // Search in workspace root and all subdirectories
                searchSuggestions.AddRange(FindInspectionFiles(workspaceRoot, fileName));
                
                // Also search in current directory if different from workspace
                if (currentDir != workspaceRoot)
                {
                    searchSuggestions.AddRange(FindInspectionFiles(currentDir, fileName));
                }
                
                // Search in parent directory too (in case we're in a subdirectory)
                var parentDir = Directory.GetParent(currentDir)?.FullName;
                if (parentDir != null && parentDir != workspaceRoot)
                {
                    searchSuggestions.AddRange(FindInspectionFiles(parentDir, fileName));
                }
                
                // Remove duplicates and order by relevance
                searchSuggestions = searchSuggestions
                    .Distinct()
                    .OrderBy(f => GetPathRelevanceScore(f, inputs.InspectionFile))
                    .ToList();
                
                var errorMessage = $"Inspection file not found: '{inputs.InspectionFile}'. " +
                    $"Absolute path: '{absolutePath}'. " +
                    $"Current directory: '{currentDir}'.";
                
                if (workspaceRoot != currentDir)
                {
                    errorMessage += $" Workspace root: '{workspaceRoot}'.";
                }
                
                if (searchSuggestions.Any())
                {
                    errorMessage += "\n\n🔍 Found possible inspection files in the workspace:";
                    foreach (var suggestion in searchSuggestions.Take(5))
                    {
                        var relativePath = Path.GetRelativePath(currentDir, suggestion);
                        errorMessage += $"\n  ✓ ./{relativePath.Replace('\\', '/')}";
                    }
                    errorMessage += "\n\n💡 Suggestion: Update your workflow to use one of the paths above.";
                    
                    if (searchSuggestions.Count > 5)
                    {
                        errorMessage += $"\n   (and {searchSuggestions.Count - 5} more files found)";
                    }
                }
                else
                {
                    errorMessage += "\n\n⚠️ No inspection log files found in the workspace. " +
                        "Please ensure the inspection step completed successfully.";
                }
                
                throw new FileNotFoundException(errorMessage, inputs.InspectionFile);
            }

            XElement xElement;
            try
            {
                xElement = XElement.Load(inputs.InspectionFile);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse XML from inspection file '{inputs.InspectionFile}'. " +
                    $"Please ensure the file contains valid XML format. Error: {ex.Message}", ex);
            }
            
            var issueTypes = ExtractIssueTypes(xElement);
            var issues = ExtractIssues(xElement, issueTypes);

            var severities = inputs.Severity.ToLower().Split(",");
            return issues
                .Where(s => severities.Contains(s.Severity.ToLower()))
                .ToList();
        }

        private Dictionary<string, string> ExtractIssueTypes(XElement xElement) =>
            xElement.Elements()
                .Where(w => w.Name == "IssueTypes")
                .Elements()
                .ToDictionary(s => s.Attribute("Id")!.Value, s => s.Attribute("Severity")?.Value ?? "ERROR");

        private List<IssueDto> ExtractIssues(XElement xElement, Dictionary<string, string> issueTypes)
        {
            return xElement.Elements()
                .Where(w => w.Name == "Issues")
                .SelectMany(s => s.Elements())
                .Elements()
                .Where(w => w.Attribute("Message") != null &&
                            w.Attribute("File") != null &&
                            w.Attribute("TypeId") != null)
                .Select(issue => new IssueDto()
                {
                    Message = issue.Attribute("Message")!.Value,
                    Type = issue.Attribute("TypeId")!.Value,
                    FilePath = ExtractFileUrl(issue.Attribute("File")),
                    Severity = ExtractSeverity(issue, issueTypes),
                    Column = ExtractStartOffset(issue.Attribute("Offset")),
                    EndColumn = ExtractEndOffset(issue.Attribute("Offset")),
                    Line = ExtractLine(issue.Attribute("Line")),
                    Project = issue.Parent!.Attribute("Name")!.Value
                })
                .ToList();
        }

        private static string ExtractFileUrl(XAttribute? attribute) => 
            attribute!.Value.Replace(".\\", "").Replace("\\", "/");

        private static string ExtractSeverity(XElement element, IReadOnlyDictionary<string, string> issueTypes)
        {
            var selfSeverity = element.Attribute("Severity");
            if (selfSeverity != null) return selfSeverity.Value;

            var severity = element.Attribute("TypeId")!.Value;

            return !issueTypes.ContainsKey(severity) ? "ERROR" : issueTypes[severity];
        }

        private static int? ExtractStartOffset(XAttribute? offsetAttribute) => 
            offsetAttribute == null ? new int?() : Convert.ToInt32(offsetAttribute!.Value.Split("-").First());

        private static int? ExtractEndOffset(XAttribute? offsetAttribute) =>
            offsetAttribute == null ? new int?() : Convert.ToInt32(offsetAttribute!.Value.Split("-").Last());

        private static int? ExtractLine(XAttribute? offsetAttribute) =>
            offsetAttribute == null ? new int?() : Convert.ToInt32(offsetAttribute!.Value);
        
        private string GetWorkspaceRoot(string currentDir)
        {
            // Check for GitHub Actions workspace environment variable
            var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            if (!string.IsNullOrEmpty(githubWorkspace) && Directory.Exists(githubWorkspace))
            {
                return githubWorkspace;
            }
            
            // Look for common workspace indicators
            var dir = new DirectoryInfo(currentDir);
            while (dir != null)
            {
                // Check for common CI/workspace indicators
                if (File.Exists(Path.Combine(dir.FullName, ".github", "workflows")) ||
                    Directory.Exists(Path.Combine(dir.FullName, ".github")) ||
                    File.Exists(Path.Combine(dir.FullName, ".gitignore")) ||
                    Directory.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    return dir.FullName;
                }
                
                // Don't go too far up
                if (dir.Parent == null || dir.Parent.FullName == dir.Root.FullName)
                {
                    break;
                }
                
                dir = dir.Parent;
            }
            
            // Default to current directory if no workspace root found
            return currentDir;
        }
        
        private int GetPathRelevanceScore(string filePath, string originalPath)
        {
            var score = 0;
            
            // Prefer files with similar directory structure
            var originalParts = originalPath.Replace('\\', '/').Split('/');
            var fileParts = filePath.Replace('\\', '/').Split('/');
            
            foreach (var part in originalParts)
            {
                if (fileParts.Contains(part))
                {
                    score -= 10; // Lower score is better
                }
            }
            
            // Prefer shorter paths
            score += filePath.Length;
            
            // Prefer files in expected locations
            if (filePath.Contains("inspection-results")) score -= 50;
            if (filePath.Contains("artifacts")) score -= 30;
            if (filePath.Contains("src")) score -= 20;
            if (filePath.Contains("BUILD")) score += 20; // Deprioritize BUILD directory
            
            return score;
        }
        
        private List<string> FindInspectionFiles(string baseDirectory, string fileName)
        {
            var foundFiles = new List<string>();
            
            if (!Directory.Exists(baseDirectory))
            {
                return foundFiles;
            }
            
            try
            {
                // Search for the specific file name first
                var specificFiles = Directory.GetFiles(baseDirectory, fileName, SearchOption.AllDirectories)
                    .Where(f => !f.Contains("node_modules") && !f.Contains(".git") && !f.Contains("/.git/"))
                    .ToList();
                
                foundFiles.AddRange(specificFiles);
                
                // If no specific file found, search for any .log files that might be inspection results
                if (!foundFiles.Any() && fileName.EndsWith(".log"))
                {
                    var patterns = new[] { "inspect*.log", "*inspection*.log", "inspectcode.xml" };
                    foreach (var pattern in patterns)
                    {
                        var matchingFiles = Directory.GetFiles(baseDirectory, pattern, SearchOption.AllDirectories)
                            .Where(f => !f.Contains("node_modules") && !f.Contains(".git") && !f.Contains("/.git/"))
                            .Where(f => IsLikelyInspectionFile(f))
                            .Take(5);
                        
                        foundFiles.AddRange(matchingFiles);
                    }
                }
            }
            catch (Exception ex)
            {
                // If directory search fails, just log and return what we have
                Console.WriteLine($"Note: Could not search all directories in {baseDirectory}: {ex.Message}");
            }
            
            return foundFiles.Distinct().ToList();
        }
        
        private bool IsLikelyInspectionFile(string filePath)
        {
            try
            {
                // Quick check if file looks like ReSharper inspection output
                var firstLine = File.ReadLines(filePath).FirstOrDefault();
                return firstLine != null && 
                    (firstLine.Contains("<?xml") || 
                     firstLine.Contains("JetBrains") || 
                     firstLine.Contains("Inspect Code"));
            }
            catch
            {
                return false;
            }
        }
    }
}
