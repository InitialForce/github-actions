using CommandLine;

namespace InitialForce.GitHubActions.TestGroups.Branch
{
    public class ActionInputs
    {
        [Option("test-assembly",
            Required = true,
            HelpText = "Test assembly file.")]
        public string TestAssembly { get; set; } = null!;

        [Option("maximum-tests-group",
            Required = false,
            Default = 100,
            HelpText = "Maximum tests per group.")]
        public int MaximumTestsGroup { get; set; }
    }
}
