﻿using System.Text.Json.Serialization;

namespace SpawnDev.BlazorJS.FromTypeScript.GitHub
{
    /// <summary>
    /// https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS
    /// </summary>
    public class GitHubRepo
    {
        public long Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        public string Name { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        public bool Private { get; set; }

        [JsonPropertyName("default_branch")]
        public string DefaultBranch { get; set; }
    }
}

//{
//    "id": 574774543,
//  "node_id": "R_kgDOIkJdDw",
//  "name": "SpawnDev.BlazorJS",
//  "full_name": "LostBeard/SpawnDev.BlazorJS",
//  "private": false,
//  "owner": {
//        "login": "LostBeard",
//    "id": 21184383,
//    "node_id": "MDQ6VXNlcjIxMTg0Mzgz",
//    "avatar_url": "https://avatars.githubusercontent.com/u/21184383?v=4",
//    "gravatar_id": "",
//    "url": "https://api.github.com/users/LostBeard",
//    "html_url": "https://github.com/LostBeard",
//    "followers_url": "https://api.github.com/users/LostBeard/followers",
//    "following_url": "https://api.github.com/users/LostBeard/following{/other_user}",
//    "gists_url": "https://api.github.com/users/LostBeard/gists{/gist_id}",
//    "starred_url": "https://api.github.com/users/LostBeard/starred{/owner}{/repo}",
//    "subscriptions_url": "https://api.github.com/users/LostBeard/subscriptions",
//    "organizations_url": "https://api.github.com/users/LostBeard/orgs",
//    "repos_url": "https://api.github.com/users/LostBeard/repos",
//    "events_url": "https://api.github.com/users/LostBeard/events{/privacy}",
//    "received_events_url": "https://api.github.com/users/LostBeard/received_events",
//    "type": "User",
//    "user_view_type": "public",
//    "site_admin": false
//  },
//  "html_url": "https://github.com/LostBeard/SpawnDev.BlazorJS",
//  "description": "Full Blazor WebAssembly and Javascript interop. Supports all Javascript data types and web browser APIs.",
//  "fork": false,
//  "url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS",
//  "forks_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/forks",
//  "keys_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/keys{/key_id}",
//  "collaborators_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/collaborators{/collaborator}",
//  "teams_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/teams",
//  "hooks_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/hooks",
//  "issue_events_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/issues/events{/number}",
//  "events_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/events",
//  "assignees_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/assignees{/user}",
//  "branches_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/branches{/branch}",
//  "tags_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/tags",
//  "blobs_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/git/blobs{/sha}",
//  "git_tags_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/git/tags{/sha}",
//  "git_refs_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/git/refs{/sha}",
//  "trees_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/git/trees{/sha}",
//  "statuses_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/statuses/{sha}",
//  "languages_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/languages",
//  "stargazers_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/stargazers",
//  "contributors_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/contributors",
//  "subscribers_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/subscribers",
//  "subscription_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/subscription",
//  "commits_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/commits{/sha}",
//  "git_commits_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/git/commits{/sha}",
//  "comments_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/comments{/number}",
//  "issue_comment_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/issues/comments{/number}",
//  "contents_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/contents/{+path}",
//  "compare_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/compare/{base}...{head}",
//  "merges_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/merges",
//  "archive_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/{archive_format}{/ref}",
//  "downloads_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/downloads",
//  "issues_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/issues{/number}",
//  "pulls_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/pulls{/number}",
//  "milestones_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/milestones{/number}",
//  "notifications_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/notifications{?since,all,participating}",
//  "labels_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/labels{/name}",
//  "releases_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/releases{/id}",
//  "deployments_url": "https://api.github.com/repos/LostBeard/SpawnDev.BlazorJS/deployments",
//  "created_at": "2022-12-06T03:25:27Z",
//  "updated_at": "2025-09-07T23:53:48Z",
//  "pushed_at": "2025-09-07T23:53:45Z",
//  "git_url": "git://github.com/LostBeard/SpawnDev.BlazorJS.git",
//  "ssh_url": "git@github.com:LostBeard/SpawnDev.BlazorJS.git",
//  "clone_url": "https://github.com/LostBeard/SpawnDev.BlazorJS.git",
//  "svn_url": "https://github.com/LostBeard/SpawnDev.BlazorJS",
//  "homepage": "https://blazorjs.spawndev.com",
//  "size": 22206,
//  "stargazers_count": 137,
//  "watchers_count": 137,
//  "language": "C#",
//  "has_issues": true,
//  "has_projects": true,
//  "has_downloads": true,
//  "has_wiki": true,
//  "has_pages": false,
//  "has_discussions": true,
//  "forks_count": 10,
//  "mirror_url": null,
//  "archived": false,
//  "disabled": false,
//  "open_issues_count": 2,
//  "license": {
//        "key": "mit",
//    "name": "MIT License",
//    "spdx_id": "MIT",
//    "url": "https://api.github.com/licenses/mit",
//    "node_id": "MDc6TGljZW5zZTEz"
//  },
//  "allow_forking": true,
//  "is_template": false,
//  "web_commit_signoff_required": false,
//  "topics": [
//    "blazor",
//    "blazor-webassembly",
//    "browser-api",
//    "csharp",
//    "dom",
//    "dotnet",
//    "gamepad-api",
//    "geolocation-api",
//    "indexeddb",
//    "indexeddb-api",
//    "interop",
//    "javascript",
//    "web-bluetooth-api",
//    "web-crypto-api",
//    "web-serial-api",
//    "webassembly",
//    "webbrowser",
//    "webgl",
//    "webgpu",
//    "webusb-api"
//  ],
//  "visibility": "public",
//  "forks": 10,
//  "open_issues": 2,
//  "watchers": 137,
//  "default_branch": "main",
//  "temp_clone_token": null,
//  "network_count": 10,
//  "subscribers_count": 4
//}