await using var provider = new ServiceCollection()
    .AddGitHubActionsCore()
    .BuildServiceProvider();

using var host = Host
    .CreateDefaultBuilder(args)
    .Build();

var core = provider.GetRequiredService<ICoreService>();

var parser = Parser.Default.ParseArguments<ActionInputs>(() => new(), args);

parser.WithNotParsed(
    errors => core.SetFailed(string.Join(Environment.NewLine, errors.Select(error => error.ToString()))));

await parser.WithParsedAsync(options => StartExecutionAsync(options, core)).ConfigureAwait(false);

await host.RunAsync().ConfigureAwait(false);

static async Task StartExecutionAsync(ActionInputs inputs, ICoreService core)
{
    try
    {
        CancellationTokenSource tokenSource = new();

        Console.CancelKeyPress += (_, _) => tokenSource.Cancel();

        var gitHubConfig = new GitHubConfig()
        {
            PersonalAccessToken = inputs.GitHubPersonalAccessToken,
            Owner = inputs.GitHubOwner,
            Repository = inputs.GitHubRepository
        };
        var githubService = new GithubService(gitHubConfig);

        var jiraConfig = new JiraConfig()
        {
            Url = inputs.JiraUrl,
            Username = inputs.JiraUsername,
            Password = inputs.JiraPassword
        };
        var jiraService = new JiraService(jiraConfig);

        var issues = await githubService.GetPrRelatedIssues(inputs.PullRequestNumber);

        if (!issues.Any())
            throw new Exception($"No Jira related issues found on pull request {inputs.PullRequestNumber}");

        var labels = await jiraService.GetIssueLabels(issues.First());

        if (!labels.Any())
            throw new Exception($"No labels found on Jira issue {issues.First()}");

        var groupCategories = inputs.TestAssembly
            .GetTestCategories()
            .FilterByLabels(labels)
            .PartitionByCount(inputs.MaximumTestsGroup);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("result");

            foreach (var group in groupCategories)
            {
                var traitCommand = string.Join("\" -trait \"Category=", group.Category.Split(","));
                traitCommand = $"-trait \"Category={traitCommand}\"";

                writer.WriteStartObject();
                writer.WriteString("group", (groupCategories.IndexOf(group) + 1).ToString());
                writer.WriteString("items", group.Category);
                writer.WriteString("command", traitCommand);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await Task.WhenAll(
                core.SetOutputAsync("count", groupCategories.Count).AsTask(), 
                core.SetOutputAsync("mode", "category").AsTask(), 
                core.SetOutputAsync("tests", Encoding.UTF8.GetString(stream.ToArray()).Replace("\"", "\\\"")).AsTask())
            .ConfigureAwait(false);
    }
    catch (Exception exception)
    {
        core.SetFailed(exception.ToString());
    }
}