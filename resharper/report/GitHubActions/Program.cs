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
        
        var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        
        if (!string.IsNullOrEmpty(githubOutput))
        {
            Console.WriteLine($"Setting output to GITHUB_OUTPUT file: {githubOutput}");
            File.AppendAllText(githubOutput, $"status={(!issues.Any() ? "success" : "failure")}{Environment.NewLine}");
        }
        else
        {
            Console.WriteLine("Warning: GITHUB_OUTPUT environment variable not set. Outputs will not be available.");
        }

        Environment.Exit(0);
    }
    catch (FileNotFoundException fnf)
    {
        // Special handling for file not found errors with suggestions
        Console.WriteLine();
        Console.WriteLine("❌ ERROR: " + fnf.Message);
        Console.WriteLine();
        
        logger.LogError($"File not found: {fnf.FileName}");
        
        var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        
        if (!string.IsNullOrEmpty(githubOutput))
        {
            try
            {
                File.AppendAllText(githubOutput, $"status=failure{Environment.NewLine}");
                File.AppendAllText(githubOutput, $"debug=File not found: {fnf.FileName}{Environment.NewLine}");
            }
            catch (Exception outputEx)
            {
                logger.LogError($"Failed to write to GITHUB_OUTPUT: {outputEx.Message}");
            }
        }
        
        Environment.Exit(1);
    }
    catch (Exception e)
    {
        logger.LogError($"Error occurred: {e.Message}");
        logger.LogError(e.ToString());
        
        var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
        
        if (!string.IsNullOrEmpty(githubOutput))
        {
            try
            {
                // Escape the error message for GitHub Actions output format
                var escapedError = e.ToString()
                    .Replace("%", "%25")
                    .Replace("\r", "%0D")
                    .Replace("\n", "%0A");
                    
                File.AppendAllText(githubOutput, $"status=failure{Environment.NewLine}");
                File.AppendAllText(githubOutput, $"debug={escapedError}{Environment.NewLine}");
            }
            catch (Exception outputEx)
            {
                logger.LogError($"Failed to write to GITHUB_OUTPUT: {outputEx.Message}");
            }
        }
        else
        {
            Console.WriteLine("Warning: GITHUB_OUTPUT environment variable not set. Outputs will not be available.");
        }
        Environment.Exit(99);
    }

    return Task.CompletedTask;
}