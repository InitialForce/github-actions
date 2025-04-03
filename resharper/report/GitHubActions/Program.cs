using System.IO;
using CommandLine;
using InitialForce.GitHubActions.ReSharper.Report;
using InitialForce.GitHubActions.ReSharper.Report.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// The check run support reports with a maximum of 65k characters.
const int charactersLimitInReport = 65000;

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

static Task StartExecutionAsync(ActionInputs inputs, ILogger logger)
{
    try
    {
        var parseService = new ParseService();
        var markdownService = new MarkdownService();

        var issues = parseService.ParseIssues(inputs);
        var originalNumberOfIssues = issues.Count;
        var markdown = markdownService.Generate(issues, inputs);

        while (markdown.Length > charactersLimitInReport)
        {
            issues.RemoveAt(issues.Count - 1);
            markdown = markdownService.Generate(issues, inputs, originalNumberOfIssues);
        }

        File.WriteAllText(inputs.OutputFile, markdown);
        
        Console.WriteLine($"GITHUB_OUTPUT={Environment.GetEnvironmentVariable("GITHUB_OUTPUT")}");
        File.AppendAllText(Environment.GetEnvironmentVariable("GITHUB_OUTPUT"), $"status={(!issues.Any() ? "success" : "failure")}{Environment.NewLine}");

        Environment.Exit(0);
    }
    catch (Exception e)
    {
        File.AppendAllText(Environment.GetEnvironmentVariable("GITHUB_OUTPUT"), $"status=failure{Environment.NewLine}");
        File.AppendAllText(Environment.GetEnvironmentVariable("GITHUB_OUTPUT"), $"debug={e}{Environment.NewLine}");

        logger.LogError(e.ToString());
        Environment.Exit(99);
    }

    return Task.CompletedTask;
}