using System;
using CommandLine;

namespace InitialForce.GitHubActions.TestGroups.PullRequest
{
    public class ActionInputs
    {
        private string _gitHubRepository = null!;

        [Option("github-owner",
            SetName = "pr",
            Required = true,
            HelpText = "GitHub owner name, for example: \"initialforce\". Assign from `github.repository_owner`.")]
        public string GitHubOwner { get; set; } = null!;

        [Option("github-repo",
            SetName = "pr",
            Required = true,
            HelpText = "GitHub repository name, for example: \"desktop\". Assign from `github.repository`.")]
        public string GitHubRepository
        {
            get => _gitHubRepository;
            set => ParseAndAssign(value, str => _gitHubRepository = str);
        }

        [Option("github-pat",
            SetName = "pr",
            Required = true,
            HelpText = "GitHub Personal Access Token (PAT).")]
        public string GitHubPersonalAccessToken { get; set; } = null!;

        [Option("jira-url",
            SetName = "pr",
            Required = true,
            HelpText = "Jira url.")]
        public string JiraUrl { get; set; } = null!;

        [Option("jira-username",
            SetName = "pr",
            Required = true,
            HelpText = "Jira username.")]
        public string JiraUsername { get; set; } = null!;

        [Option("jira-password",
            SetName = "pr",
            Required = true,
            HelpText = "Jira password.")]
        public string JiraPassword { get; set; } = null!;

        [Option("pull-request",
            Required = true,
            HelpText = "Pull request number.")]
        public int PullRequestNumber { get; set; }

        [Option("test-assembly",
            Required = true,
            HelpText = "Test assembly file.")]
        public string TestAssembly { get; set; } = null!;

        [Option("maximum-tests-group",
            Required = false,
            Default = 100,
            HelpText = "Maximum tests per group.")]
        public int MaximumTestsGroup { get; set; }

        static void ParseAndAssign(string? value, Action<string>? assign)
        {
            if (value is { Length: > 0 } && assign is not null)
            {
                assign(value.Split("/")[^1]);
            }
        }
    }
}
