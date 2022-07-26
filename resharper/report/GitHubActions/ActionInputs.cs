using CommandLine;

namespace InitialForce.GitHubActions.ReSharper.Report
{
    public class ActionInputs
    {
        [Option("inspection-file",
            Required = true,
            HelpText = "Inspection file.")]
        public string InspectionFile { get; set; } = null!;

        [Option("output-file",
            Required = true,
            HelpText = "Output file.")]
        public string OutputFile { get; set; } = null!;

        [Option("severity",
            Required = true,
            HelpText = "Severity (use comma for multiple severities).")]
        public string Severity { get; set; } = null!;

        [Option("maximum-issues",
            Required = true,
            HelpText = "Maximum number of issues to display.",
            Default = 100)]
        public int MaximumIssues { get; set; } = 100;

        [Option("working-directory",
            Required = false,
            HelpText = "Inspection working directory.")]
        public string WorkingDirectory { get; set; } = null!;

        [Option("repository-name",
            Required = true,
            HelpText = "Repository name.")]
        public string RepositoryName { get; set; } = null!;

        [Option("ref-name",
            Required = true,
            HelpText = "Ref name.")]
        public string RefName { get; set; } = null!;
    }
}
