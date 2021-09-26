﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using InitialForce.GitHubActions.TestGroups.PullRequest.Configs;

namespace InitialForce.GitHubActions.TestGroups.PullRequest.Services
{
    public class JiraService
    {
        private readonly JiraConfig _config;

        public JiraService(JiraConfig config) => _config = config;

        public async Task<List<string>> GetIssueComponents(string issueKey)
        {
            var issue = await _config.Url
                .AppendPathSegment($"/rest/api/2/issue/{issueKey}")
                .WithBasicAuth(_config.Username, _config.Password)
                .WithHeader("Accept", "application/json")
                .GetAsync()
                .ReceiveJson<dynamic>();

            var components = new List<string>();

            foreach (var component in issue.fields.components)
            {
                components.Add(component.name.ToString());
            }

            return components;
        }
    }
}
