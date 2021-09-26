namespace InitialForce.GitHubActions.TestGroups.PullRequest.Configs
{
    public record GitHubConfig
    {
        public string PersonalAccessToken { get; set; }
        public string Owner { get; set; }
        public string Repository { get; set; }
    }
}
