namespace InitialForce.GitHubActions.ReSharper.Report.Services;

public class MarkdownService
{
    public string Generate(List<IssueDto> issues)
    {
        var groupedIssues = issues
            .GroupBy(g => g.Project)
            .OrderBy(o => o.Key)
            .Select(g => new { Project = g.Key, Issues = g });

        var markdown = new StringBuilder();

        markdown.Append("# ReSharper Inspection Code\r\n");
        markdown.Append($"Found {issues.Count} issues during the inspection.\r\n\r\n");

        foreach (var projectIssues in groupedIssues)
        {
            markdown.Append($"## {projectIssues.Project}\r\n\r\n");

            foreach (var issue in projectIssues.Issues)
            {
                markdown.Append($"> :{ConvertSeverity(issue.Severity)}: [{issue.FilePath}]({issue.FilePath}#L{issue.Line ?? 0})<br/>\r\n**[{issue.Type}]** {issue.Message}\r\n\r\n");
            }
        }

        return markdown.ToString();
    }

    private static string ConvertSeverity(string severity)
    {
        switch (severity.ToLower())
        {
            case "hint":
            case "suggestion":
                return "information_source";
            case "warning":
                return "warning";
            default:
                return "x";
        }
    }
}
