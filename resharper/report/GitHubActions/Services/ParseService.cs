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
                throw new Exception($"Inspection file {inputs.InspectionFile} doesn't exists");
            }

            var xElement = XElement.Load(inputs.InspectionFile);
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
    }
}
