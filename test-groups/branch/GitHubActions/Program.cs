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

        var groupCategories = inputs.TestAssembly
            .GetTestClasses()
            .PartitionByCount(inputs.MaximumTestsGroup);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("result");
            //-class "MotionCatalyst.Test.UI.Features.GroupsFeature" -class "MotionCatalyst.Test.UI.Features.TestSetsFeature"
            //--filter (FullyQualifiedName~MotionCatalyst.Test.UI.Features.GroupsFeature) | (FullyQualifiedName~MotionCatalyst.Test.UI.Features.TestSetsFeature) .\MotionCatalyst.Test.UI.dll
            foreach (var group in groupCategories)
            {

                List<string> classes = group.Class.Split(",").Select(d => $"(FullyQualifiedName~{d})").ToList();
                var traitCommand = string.Join(" | ", classes);
                traitCommand = $"--filter \"{traitCommand}\"";

                writer.WriteStartObject();
                writer.WriteString("group", (groupCategories.IndexOf(group) + 1).ToString());
                writer.WriteString("items", group.Class);
                writer.WriteString("command", traitCommand);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await Task.WhenAll(
                core.SetOutputAsync("count", groupCategories.Count).AsTask(),
                core.SetOutputAsync("mode", "class").AsTask(),
                core.SetOutputAsync("tests", Encoding.UTF8.GetString(stream.ToArray()).Replace("\"", "\\\"")).AsTask())
            .ConfigureAwait(false);

        Environment.Exit(0);
    }
    catch (Exception exception)
    {
        core.SetFailed(exception.ToString());
    }
}