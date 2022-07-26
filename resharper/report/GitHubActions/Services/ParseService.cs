using System.IO;
using System.Xml.Linq;

namespace InitialForce.GitHubActions.ReSharper.Report.Services
{
    public class ParseService
    {
        public List<IssueDto> ParseIssues(ActionInputs input)
        {
            if (!File.Exists(input.InspectionFile))
            {
                throw new Exception($"Inspection file {input.InspectionFile} doesn't exists");
            }

            var xElement = XElement.Load(input.InspectionFile);
            var issueTypes = ExtractIssueTypes(xElement);
            var issues = ExtractIssues(xElement, issueTypes, input.WorkspaceDirectory);

            var severities = input.Severity.ToLower().Split(",");
            return issues
                .Where(s => severities.Contains(s.Severity.ToLower()))
                .ToList();
        }

        private Dictionary<string, string> ExtractIssueTypes(XElement xElement) =>
            xElement.Elements()
                .Where(w => w.Name == "IssueTypes")
                .Elements()
                .ToDictionary(s => s.Attribute("Id")!.Value, s => s.Attribute("Severity")?.Value ?? "ERROR");

        private List<IssueDto> ExtractIssues(XElement xElement, Dictionary<string, string> issueTypes,
            string workspaceDirectory)
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
                    FilePath = issue.Attribute("File")!.Value.Replace(workspaceDirectory, string.Empty).Replace("\\", "/"),
                    Severity = ExtractSeverity(issue.Attribute("TypeId"), issueTypes),
                    Column = ExtractStartOffset(issue.Attribute("Offset")),
                    EndColumn = ExtractEndOffset(issue.Attribute("Offset")),
                    Line = ExtractLine(issue.Attribute("Line")),
                    Project = issue.Parent!.Attribute("Name")!.Value
                })
                .ToList();
        }

        private static string ExtractSeverity(XAttribute? attribute, Dictionary<string, string> issueTypes)
        {
            var severity = attribute!.Value;

            return !issueTypes.ContainsKey(severity) ? "ERROR" : issueTypes[severity];
        }

        private static int? ExtractStartOffset(XAttribute? offsetAttribute) => 
            offsetAttribute == null ? new int?() : Convert.ToInt32(offsetAttribute!.Value.Split("-").First());

        private static int? ExtractEndOffset(XAttribute? offsetAttribute) =>
            offsetAttribute == null ? new int?() : Convert.ToInt32(offsetAttribute!.Value.Split("-").Last());

        private static int? ExtractLine(XAttribute? offsetAttribute) =>
            offsetAttribute == null ? new int?() : Convert.ToInt32(offsetAttribute!.Value);
    }
}
