using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using InitialForce.GitHubActions.XUnitToJUnit;

using var host = Host
    .CreateDefaultBuilder(args)
    .Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("GitHubActions");

var parser = Parser.Default.ParseArguments<ActionInputs>(() => new(), args);

parser.WithParsed(options => StartExecution(options, logger));

parser.WithNotParsed(
    errors =>
    {
        logger.LogError(
            string.Join(Environment.NewLine, errors.Select(error => error.ToString())));

        Environment.Exit(2);
    });

host.Run();

static void StartExecution(ActionInputs inputs, ILogger logger)
{
    try
    {
        using CancellationTokenSource tokenSource = new();

        Console.CancelKeyPress += (_, _) => tokenSource.Cancel();

        var xUnitFiles = Directory
            .GetFiles(inputs.XUnitPath, "*.*", SearchOption.AllDirectories)
            .Where(file => new [] { ".xml" }
                .Contains(Path.GetExtension(file)))
            .ToList();

        foreach (var xUnitFile in xUnitFiles)
        {
            var jUnitFile = Path.Combine(inputs.JUnitOutputPath, Path.GetFileName(xUnitFile));

            JUnitTransformer.Transform(xUnitFile, jUnitFile);
        }

        Environment.Exit(0);
    }
    catch (Exception exception)
    {
        logger.LogError(exception.ToString());
        Environment.Exit(2);
    }
}