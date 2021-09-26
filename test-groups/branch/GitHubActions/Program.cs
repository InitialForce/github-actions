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
using InitialForce.GitHubActions.TestGroups.Branch;
using InitialForce.GitHubActions.TestGroups.Branch.Extensions;

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

        var groupCategories = inputs.TestAssembly
            .GetTestClasses()
            .PartitionByCount(inputs.MaximumTestsGroup);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("result");

            foreach (var group in groupCategories)
            {
                var traitCommand = string.Join("\" -class \"", group.Class.Split(","));
                traitCommand = $"-class \"{traitCommand}\"";

                writer.WriteStartObject();
                writer.WriteString("group", (groupCategories.IndexOf(group) + 1).ToString());
                writer.WriteString("items", group.Class);
                writer.WriteString("command", traitCommand);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        Console.WriteLine($"::set-output name=count::{groupCategories.Count}");
        Console.WriteLine($"::set-output name=mode::class");
        Console.WriteLine($"::set-output name=tests::{Encoding.UTF8.GetString(stream.ToArray()).Replace("\"", "\\\"")}");

        Environment.Exit(0);
    }
    catch (Exception exception)
    {
        logger.LogError(exception.ToString());
        Environment.Exit(2);
    }
}