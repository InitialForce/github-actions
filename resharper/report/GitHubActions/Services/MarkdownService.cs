namespace InitialForce.GitHubActions.ReSharper.Report.Services;

public class MarkdownService
{
    public string Generate(List<IssueDto> issues, ActionInputs inputs)
    {
        var filteredIssues = issues
            .Take(inputs.MaximumIssues)
            .ToList();

        var numberIssuesFound = issues.Count;
        var numberFilteredIssues = filteredIssues.Count;

        var groupedIssues = filteredIssues
            .GroupBy(g => g.Project)
            .OrderBy(o => o.Key)
            .Select(g => new { Project = g.Key, Issues = g });

        if (!issues.Any())
        {
            return ":white_check_mark: Great job! No issues were found during the inspection.";
        }

        var markdown = new StringBuilder();
        markdown.Append($"Found {numberIssuesFound} issues during the inspection{(numberIssuesFound != numberFilteredIssues ? $" (Displaying the limit of {numberFilteredIssues} issues)" : "")}.\r\n\r\n");

        foreach (var projectIssues in groupedIssues)
        {
            markdown.Append($"## {projectIssues.Project}\r\n\r\n");

            foreach (var issue in projectIssues.Issues)
            {
                markdown.Append($"> :{ConvertSeverity(issue.Severity)}: [{issue.FilePath}]({ConvertFileUrl(issue.FilePath, inputs)}#L{issue.Line ?? 0}) *(line {issue.Line ?? 0})*\r\n**[{issue.Type}]** {issue.Message}\r\n\r\n");
            }
        }

        return markdown.ToString();
    }

    private static string ConvertFileUrl(string file, ActionInputs input) => 
        $"https://github.com/{input.RepositoryName}/blob/{input.RefName}/{(!string.IsNullOrWhiteSpace(input.WorkingDirectory) ? $"{input.WorkingDirectory.Replace("./", "/")}/" : "")}{file}";

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
