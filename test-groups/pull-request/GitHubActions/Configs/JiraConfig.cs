namespace InitialForce.GitHubActions.TestGroups.PullRequest.Configs
{
    public record JiraConfig
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
