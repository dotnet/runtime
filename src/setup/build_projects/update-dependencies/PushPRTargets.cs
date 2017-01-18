// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.Build.Framework;
using Octokit;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Scripts
{
    /// <summary>
    /// Creates a GitHub Pull Request for the current changes in the repo.
    /// </summary>
    public static class PushPRTargets
    {
        private static readonly Config s_config = Config.Instance;

        [Target(nameof(CommitChanges), nameof(CreatePR))]
        public static BuildTargetResult PushPR(BuildTargetContext c) => c.Success();

        /// <summary>
        /// Commits all the current changes in the repo and pushes the commit to a remote
        /// so a PR can be created for it.
        /// </summary>
        [Target]
        public static BuildTargetResult CommitChanges(BuildTargetContext c)
        {
            CommandResult statusResult = Cmd("git", "status", "--porcelain")
                .CaptureStdOut()
                .Execute();
            statusResult.EnsureSuccessful();

            bool hasModifiedFiles = !string.IsNullOrWhiteSpace(statusResult.StdOut);
            bool hasUpdatedDependencies = c.GetDependencyInfos().Where(d => d.IsUpdated).Any();

            if (hasModifiedFiles != hasUpdatedDependencies)
            {
                return c.Failed($"'git status' does not match DependencyInfo information. Git has modified files: {hasModifiedFiles}. DependencyInfo is updated: {hasUpdatedDependencies}.");
            }

            if (!hasUpdatedDependencies)
            {
                c.Warn("Dependencies are currently up to date");
                return c.Success();
            }

            string userName = s_config.UserName;
            string email = s_config.Email;

            string commitMessage = GetCommitMessage(c);

            Cmd("git", "commit", "-a", "-m", commitMessage, "--author", $"{userName} <{email}>")
                .EnvironmentVariable("GIT_COMMITTER_NAME", userName)
                .EnvironmentVariable("GIT_COMMITTER_EMAIL", email)
                .Execute()
                .EnsureSuccessful();

            string remoteUrl = $"github.com/{s_config.GitHubOriginOwner}/{s_config.GitHubProject}.git";
            string remoteBranchName = $"UpdateDependencies{DateTime.UtcNow.ToString("yyyyMMddhhmmss")}";
            string refSpec = $"HEAD:refs/heads/{remoteBranchName}";

            string logMessage = $"git push https://{remoteUrl} {refSpec}";
            BuildReporter.BeginSection("EXEC", logMessage);

            CommandResult pushResult =
                Cmd("git", "push", $"https://{userName}:{s_config.Password}@{remoteUrl}", refSpec)
                    .QuietBuildReporter()  // we don't want secrets showing up in our logs
                    .CaptureStdErr() // git push will write to StdErr upon success, disable that
                    .CaptureStdOut()
                    .Execute();

            var message = logMessage + $" exited with {pushResult.ExitCode}";
            if (pushResult.ExitCode == 0)
            {
                BuildReporter.EndSection("EXEC", message.Green(), success: true);
            }
            else
            {
                BuildReporter.EndSection("EXEC", message.Red().Bold(), success: false);
            }

            pushResult.EnsureSuccessful(suppressOutput: true);

            c.SetRemoteBranchName(remoteBranchName);

            return c.Success();
        }

        /// <summary>
        /// Creates a GitHub PR for the remote branch created above.
        /// </summary>
        [Target]
        public static BuildTargetResult CreatePR(BuildTargetContext c)
        {
            string remoteBranchName = c.GetRemoteBranchName();
            string commitMessage = c.GetCommitMessage();

            NewPullRequest prInfo = new NewPullRequest(
                $"[{s_config.GitHubUpstreamBranch}] {commitMessage}",
                s_config.GitHubOriginOwner + ":" + remoteBranchName,
                s_config.GitHubUpstreamBranch);

            string[] prNotifications = s_config.GitHubPullRequestNotifications;
            if (prNotifications.Length > 0)
            {
                prInfo.Body = $"/cc @{string.Join(" @", prNotifications)}";
            }

            GitHubClient gitHub = new GitHubClient(new ProductHeaderValue("dotnetDependencyUpdater"));

            gitHub.Credentials = new Credentials(s_config.Password);

            PullRequest createdPR = gitHub.PullRequest.Create(s_config.GitHubUpstreamOwner, s_config.GitHubProject, prInfo).Result;
            c.Info($"Created Pull Request: {createdPR.HtmlUrl}");

            return c.Success();
        }

        private static string GetRemoteBranchName(this BuildTargetContext c)
        {
            return (string)c.BuildContext["RemoteBranchName"];
        }

        private static void SetRemoteBranchName(this BuildTargetContext c, string value)
        {
            c.BuildContext["RemoteBranchName"] = value;
        }

        private static string GetCommitMessage(this BuildTargetContext c)
        {
            const string commitMessagePropertyName = "CommitMessage";

            string message;
            object messageObject;
            if (c.BuildContext.Properties.TryGetValue(commitMessagePropertyName, out messageObject))
            {
                message = (string)messageObject;
            }
            else
            {
                DependencyInfo[] updatedDependencies = c.GetDependencyInfos()
                    .Where(d => d.IsUpdated)
                    .ToArray();

                string updatedDependencyNames = string.Join(", ", updatedDependencies.Select(d => d.Name));
                string updatedDependencyVersions = string.Join(", ", updatedDependencies.Select(d => d.NewReleaseVersion));

                message = $"Updating {updatedDependencyNames} to {updatedDependencyVersions}";
                if (updatedDependencies.Count() > 1)
                {
                    message += " respectively";
                }

                c.BuildContext[commitMessagePropertyName] = message;
            }

            return message;
        }
    }
}
