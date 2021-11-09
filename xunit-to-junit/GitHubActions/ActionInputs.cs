using CommandLine;

namespace InitialForce.GitHubActions.XUnitToJUnit
{
    public class ActionInputs
    {
        [Option("xunit-path",
            Required = true,
            HelpText = "XUnit files path.")]
        public string XUnitPath { get; set; } = null!;

        [Option("junit-output-path",
            Required = true,
            HelpText = "JUnit converted files path.")]
        public string JUnitOutputPath { get; set; }
    }
}
