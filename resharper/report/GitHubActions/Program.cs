﻿using System.IO;
using CommandLine;
using InitialForce.GitHubActions.ReSharper.Report;
using InitialForce.GitHubActions.ReSharper.Report.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        var markdown = markdownService.Generate(issues, inputs);

        File.WriteAllText(inputs.OutputFile, markdown);
        
        Environment.Exit(!issues.Any() ? 0 : 2);
    }
    catch (Exception e)
    {
        logger.LogError(e.ToString());
        Environment.Exit(2);
    }

    return Task.CompletedTask;
}