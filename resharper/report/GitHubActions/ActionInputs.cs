using CommandLine;

namespace InitialForce.GitHubActions.ReSharper.Report
{
    public class ActionInputs
    {
        [Option("inspection-file",
            Required = true,
            HelpText = "Inspection file.")]
        public string InspectionFile { get; set; } = null!;

        [Option("workspace-directory",
            Required = true,
            HelpText = "Workspace directory.")]
        public string WorkspaceDirectory { get; set; } = null!;

        [Option("output-file",
            Required = true,
            HelpText = "Output file.")]
        public string OutputFile { get; set; } = null!;

        [Option("severity",
            Required = true,
            HelpText = "Severity (use comma for multiple severities).")]
        public string Severity { get; set; } = null!;
    }
}
