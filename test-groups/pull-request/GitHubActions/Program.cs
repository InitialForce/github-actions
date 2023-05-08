using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InitialForce.GitHubActions.TestGroups.PullRequest;
using InitialForce.GitHubActions.TestGroups.PullRequest.Configs;
using InitialForce.GitHubActions.TestGroups.PullRequest.Extensions;
using InitialForce.GitHubActions.TestGroups.PullRequest.Services;

using var host = Host
    .CreateDefaultBuilder(args)
    .Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("GitHubActions");

var parser = Parser.Default.ParseArguments<ActionInputs>(() => new(), args);

parser.WithNotParsed(
    errors =>
    {
        logger.LogError(
            string.Join(Environment.NewLine, errors.Select(error => error.ToString())));

        Environment.Exit(2);
    });

await parser.WithParsedAsync(options => StartExecutionAsync(options, logger));

await host.RunAsync();

static async Task StartExecutionAsync(ActionInputs inputs, ILogger logger)
{
    try
    {
        using CancellationTokenSource tokenSource = new();

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


        Console.WriteLine($"echo \"count={groupCategories.Count}\" >> $GITHUB_OUTPUT");
        Console.WriteLine("echo \"mode=category\" >> $GITHUB_OUTPUT");
        Console.WriteLine($"echo \"tests={Encoding.UTF8.GetString(stream.ToArray()).Replace("\"", "\\\"")}\" >> $GITHUB_OUTPUT");

        Environment.Exit(0);
    }
    catch (Exception e)
    {
        logger.LogError(e.ToString());
        Environment.Exit(2);
    }
}