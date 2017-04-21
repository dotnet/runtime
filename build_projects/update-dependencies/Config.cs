// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Scripts
{
    /// <summary>
    /// Holds the configuration information for the update-dependencies script.
    /// </summary>
    /// <remarks>
    /// The following Environment Variables are required by this script:
    /// 
    /// GITHUB_USER - The user to commit the changes as.
    /// GITHUB_EMAIL - The user's email to commit the changes as.
    /// GITHUB_PASSWORD - The password/personal access token of the GitHub user.
    ///
    /// The following Environment Variables can optionally be specified:
    /// 
    /// COREFX_VERSION_URL - The Url to get the current CoreFx version. (ex. "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/corefx/master/")
    /// CORECLR_VERSION_URL - The Url to get the current CoreCLR version. (ex. "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/coreclr/master/")
    /// GITHUB_ORIGIN_OWNER - The owner of the GitHub fork to push the commit and create the PR from. (ex. "dotnet-bot")
    /// GITHUB_UPSTREAM_OWNER - The owner of the GitHub base repo to create the PR to. (ex. "dotnet")
    /// GITHUB_PROJECT - The repo name under the ORIGIN and UPSTREAM owners. (ex. "cli")
    /// GITHUB_UPSTREAM_BRANCH - The branch in the GitHub base repo to create the PR to. (ex. "rel/1.0.0")
    /// GITHUB_PULL_REQUEST_NOTIFICATIONS - A semi-colon ';' separated list of GitHub users to notify on the PR.
    /// </remarks>
    public class Config
    {
        public static Config Instance { get; } = new Config();

        private static BranchInfo s_branchInfo = new BranchInfo(Dirs.RepoRoot);

        private Lazy<string> _userName = new Lazy<string>(() => GetEnvironmentVariable("GITHUB_USER"));
        private Lazy<string> _email = new Lazy<string>(() => GetEnvironmentVariable("GITHUB_EMAIL"));
        private Lazy<string> _password = new Lazy<string>(() => GetEnvironmentVariable("GITHUB_PASSWORD"));
        private Lazy<string> _coreFxVersionUrl = new Lazy<string>(() => GetEnvironmentVariable("COREFX_VERSION_URL", "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/corefx/master/"));
        private Lazy<string> _coreClrVersionUrl = new Lazy<string>(() => GetEnvironmentVariable("CORECLR_VERSION_URL", "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/coreclr/master/"));
        private Lazy<string> _standardVersionUrl = new Lazy<string>(() => GetEnvironmentVariable("STANDARD_VERSION_URL", "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/standard/master/"));
        private Lazy<string> _gitHubOriginOwner;
        private Lazy<string> _gitHubUpstreamOwner = new Lazy<string>(() => GetEnvironmentVariable("GITHUB_UPSTREAM_OWNER", "dotnet"));
        private Lazy<string> _gitHubProject = new Lazy<string>(() => GetEnvironmentVariable("GITHUB_PROJECT", "core-setup"));
        private Lazy<string> _gitHubUpstreamBranch = new Lazy<string>(() => GetEnvironmentVariable("GITHUB_UPSTREAM_BRANCH", s_branchInfo.Entries["BRANCH_NAME"]));
        private Lazy<string[]> _gitHubPullRequestNotifications = new Lazy<string[]>(() => 
                                                GetEnvironmentVariable("GITHUB_PULL_REQUEST_NOTIFICATIONS", "")
                                                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        private Config()
        {
            _gitHubOriginOwner = new Lazy<string>(() => GetEnvironmentVariable("GITHUB_ORIGIN_OWNER", UserName));
        }

        public string UserName => _userName.Value;
        public string Email => _email.Value; 
        public string Password => _password.Value;
        public string CoreFxVersionUrl => _coreFxVersionUrl.Value;
        public string CoreClrVersionUrl => _coreClrVersionUrl.Value;
        public string StandardVersionUrl => _standardVersionUrl.Value;
        public string GitHubOriginOwner => _gitHubOriginOwner.Value;
        public string GitHubUpstreamOwner => _gitHubUpstreamOwner.Value;
        public string GitHubProject => _gitHubProject.Value;
        public string GitHubUpstreamBranch => _gitHubUpstreamBranch.Value;
        public string[] GitHubPullRequestNotifications => _gitHubPullRequestNotifications.Value;

        private static string GetEnvironmentVariable(string name, string defaultValue = null)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (value == null)
            {
                value = defaultValue;
            }

            if (value == null)
            {
                throw new InvalidOperationException($"Can't find environment variable '{name}'.");
            }

            return value;
        }
    }
}
