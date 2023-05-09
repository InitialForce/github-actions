namespace InitialForce.GitHubActions.TestGroups.PullRequest.Services;

public class JiraService
{
    private readonly JiraConfig _config;

    public JiraService(JiraConfig config) => _config = config;

    public async Task<List<string>> GetIssueLabels(string issueKey)
    {
        var issue = await _config.Url
            .AppendPathSegment($"/rest/api/2/issue/{issueKey}")
            .WithBasicAuth(_config.Username, _config.Password)
            .WithHeader("Accept", "application/json")
            .GetAsync()
            .ReceiveJson<dynamic>();

        var labels = new List<string>();

        foreach (var label in issue.fields.labels)
        {
            labels.Add(label.ToString());
        }

        return labels;
    }
}