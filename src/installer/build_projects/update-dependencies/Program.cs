// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.VersionTools;
using Microsoft.DotNet.VersionTools.Automation;
using Microsoft.DotNet.VersionTools.Dependencies;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Scripts
{
    public class Program
    {
        private static readonly Config s_config = Config.Instance;

        public static void Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            bool onlyUpdate;
            ParseArgs(args, out onlyUpdate);

            List<BuildInfo> buildInfos = new List<BuildInfo>();

            buildInfos.Add(GetBuildInfo("CoreFx", s_config.CoreFxVersionUrl, fetchLatestReleaseFile: false));
            buildInfos.Add(GetBuildInfo("CoreClr", s_config.CoreClrVersionUrl, fetchLatestReleaseFile: false));
            buildInfos.Add(GetBuildInfo("Standard", s_config.StandardVersionUrl, fetchLatestReleaseFile: false));

            IEnumerable<IDependencyUpdater> updaters = GetUpdaters();
            var dependencyBuildInfos = buildInfos.Select(buildInfo =>
                new DependencyBuildInfo(
                    buildInfo,
                    upgradeStableVersions: true,
                    disabledPackages: Enumerable.Empty<string>()));
            DependencyUpdateResults updateResults = DependencyUpdateUtils.Update(updaters, dependencyBuildInfos);

            if (!onlyUpdate && updateResults.ChangesDetected())
            {
                GitHubAuth gitHubAuth = new GitHubAuth(s_config.Password, s_config.UserName, s_config.Email);
                GitHubProject origin = new GitHubProject(s_config.GitHubProject, s_config.UserName);
                GitHubBranch upstreamBranch = new GitHubBranch(
                    s_config.GitHubUpstreamBranch,
                    new GitHubProject(s_config.GitHubProject, s_config.GitHubUpstreamOwner));

                string suggestedMessage = updateResults.GetSuggestedCommitMessage();
                string body = string.Empty;
                if (s_config.GitHubPullRequestNotifications.Any())
                {
                    body += PullRequestCreator.NotificationString(s_config.GitHubPullRequestNotifications);
                }

                new PullRequestCreator(gitHubAuth, origin, upstreamBranch)
                    .CreateOrUpdateAsync(
                        suggestedMessage,
                        suggestedMessage + $" ({upstreamBranch.Name})",
                        body)
                    .Wait();
            }
        }

        private static bool ParseArgs(string[] args, out bool updateOnly)
        {
            updateOnly = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--update", StringComparison.OrdinalIgnoreCase))
                {
                    updateOnly = true;
                }
                if (args[i] == "-e")
                {
                    i++;
                    while (i < args.Length && !args[i].StartsWith("-", StringComparison.Ordinal))
                    {
                        int idx = args[i].IndexOf('=');
                        if (idx < 0)
                        {
                            Console.Error.WriteLine($"Unrecognized argument '{args[i]}'");
                            return false;
                        }

                        string name = args[i].Substring(0, idx);
                        string value = args[i].Substring(idx + 1);
                        Environment.SetEnvironmentVariable(name, value);
                        i++;
                    }
                }
            }

            return true;
        }

        private static BuildInfo GetBuildInfo(string name, string packageVersionsUrl, bool fetchLatestReleaseFile = true)
        {
            const string FileUrlProtocol = "file://";

            if (packageVersionsUrl.StartsWith(FileUrlProtocol, StringComparison.Ordinal))
            {
                return LocalFileGetAsync(
                           name,
                           packageVersionsUrl.Substring(FileUrlProtocol.Length),
                           fetchLatestReleaseFile)
                       .Result;
            }
            else
            {
                return BuildInfo.Get(name, packageVersionsUrl, fetchLatestReleaseFile);
            }
        }

        private static async Task<BuildInfo> LocalFileGetAsync(
            string name,
            string path,
            bool fetchLatestReleaseFile = true)
        {
            string latestPackagesPath = Path.Combine(path, BuildInfo.LatestPackagesTxtFilename);
            using (var packageFileStream = File.OpenRead(latestPackagesPath))
            using (var packageReader = new StreamReader(packageFileStream))
            {
                Dictionary<string, string> packages = await BuildInfo.ReadPackageListAsync(packageReader);

                string latestReleaseVersion;
                if (fetchLatestReleaseFile)
                {
                    string latestReleasePath = Path.Combine(path, BuildInfo.LatestTxtFilename);
                    latestReleaseVersion = File.ReadLines(latestReleasePath).First().Trim();
                }
                else
                {
                    latestReleaseVersion = FindLatestReleaseFromPackages(packages);
                }

                return new BuildInfo
                {
                    Name = name,
                    LatestPackages = packages,
                    LatestReleaseVersion = latestReleaseVersion
                };
            }
        }

        private static string FindLatestReleaseFromPackages(IDictionary<string, string> packages)
        {
            IEnumerable<NuGetVersion> versions = packages.Values
                .Select(versionString => new NuGetVersion(versionString));

            return
                versions.FirstOrDefault(v => v.IsPrerelease)?.Release ??
                    // if there are no prerelease versions, just grab the first version
                    versions.FirstOrDefault()?.ToNormalizedString();
        }

        private static IEnumerable<IDependencyUpdater> GetUpdaters()
        {
            yield return CreateProjectJsonUpdater();

            yield return CreateDependencyVersionsUpdater("CoreCLRVersion", "transport.Microsoft.NETCore.Runtime.CoreCLR");
            yield return CreateDependencyVersionsUpdater("JitVersion", "transport.Microsoft.NETCore.Jit");

            yield return CreateRegexUpdater(Path.Combine("pkg", "dir.props"), new Regex(@"Microsoft\.NETCore\.Platforms\\(?<version>.*)\\runtime\.json"), "Microsoft.NETCore.Platforms");
        }

        private static IDependencyUpdater CreateProjectJsonUpdater()
        {
            var projectJsonFiles = new List<string>
            {
                Path.Combine(Dirs.PkgProjects, "Microsoft.NETCore.App", "project.json.template"),
                Path.Combine(Dirs.PkgProjects, "Microsoft.NETCore.UniversalWindowsPlatform", "project.json.template"),
                Path.Combine(Dirs.PkgDeps, "project.json")
            };

            return new ProjectJsonUpdater(projectJsonFiles);
        }

        private static IDependencyUpdater CreateDependencyVersionsUpdater(string dependencyPropertyName, string packageId)
        {
            string dependencyVersionsPath = Path.Combine("build_projects", "shared-build-targets-utils", "DependencyVersions.cs");
            Regex dependencyVersionsRegex = new Regex($@"{dependencyPropertyName} = ""(?<version>.*)"";");

            return CreateRegexUpdater(dependencyVersionsPath, dependencyVersionsRegex, packageId);
        }

        private static IDependencyUpdater CreateRegexUpdater(string repoRelativePath, Regex regex, string packageId)
        {
            return new FileRegexPackageUpdater()
            {
                Path = Path.Combine(Dirs.RepoRoot, repoRelativePath),
                PackageId = packageId,
                Regex = regex,
                VersionGroupName = "version"
            };
        }
    }
}