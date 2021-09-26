using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InitialForce.GitHubActions.TestGroups.PullRequest.Configs;
using Octokit;
using Octokit.Internal;

namespace InitialForce.GitHubActions.TestGroups.PullRequest.Services
{
    public class GithubService
    {
        private readonly GitHubConfig _config;
        private readonly GitHubClient _client;
        private readonly string _httpUserAgent = "initial-force";

        public GithubService(GitHubConfig config)
        {
            _config = config;
            var credentials = new InMemoryCredentialStore(new Credentials(config.PersonalAccessToken));
            _client = new GitHubClient(new ProductHeaderValue(_httpUserAgent), credentials);
        }

        public async Task<List<string>> GetPrRelatedIssues(int pullRequestId)
        {
            var pullRequest = await _client.PullRequest.Get(_config.Owner, _config.Repository, pullRequestId);

            var regex = new Regex("((?<!([A-Z]{1,10})-?)[A-Z]+-\\d+)");

            return regex
                .Matches(pullRequest.Title)
                .Select(match => match.ToString())
                .ToList();
        }
    }
}
