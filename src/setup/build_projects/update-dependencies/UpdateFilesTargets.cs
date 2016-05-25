// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace Microsoft.DotNet.Scripts
{
    public static class UpdateFilesTargets
    {
        private static HttpClient s_client = new HttpClient();

        [Target(nameof(GetDependencies), nameof(ReplaceVersions))]
        public static BuildTargetResult UpdateFiles(BuildTargetContext c) => c.Success();

        /// <summary>
        /// Gets all the dependency information and puts it in the build properties.
        /// </summary>
        [Target]
        public static BuildTargetResult GetDependencies(BuildTargetContext c)
        {
            List<DependencyInfo> dependencyInfos = c.GetDependencyInfos();

            dependencyInfos.Add(CreateDependencyInfo("CoreFx", Config.Instance.CoreFxVersionUrl).Result);

            return c.Success();
        }

        private static async Task<DependencyInfo> CreateDependencyInfo(string name, string packageVersionsUrl)
        {
            List<PackageInfo> newPackageVersions = new List<PackageInfo>();

            using (Stream versionsStream = await s_client.GetStreamAsync(packageVersionsUrl))
            using (StreamReader reader = new StreamReader(versionsStream))
            {
                string currentLine;
                while ((currentLine = await reader.ReadLineAsync()) != null)
                {
                    int spaceIndex = currentLine.IndexOf(' ');

                    newPackageVersions.Add(new PackageInfo()
                    {
                        Id = currentLine.Substring(0, spaceIndex),
                        Version = new NuGetVersion(currentLine.Substring(spaceIndex + 1))
                    });
                }
            }

            string newReleaseVersion = newPackageVersions
                .Where(p => p.Version.IsPrerelease)
                .Select(p => p.Version.Release)
                .FirstOrDefault()
                ??
                // if there are no prerelease versions, just grab the first version
                newPackageVersions
                    .Select(p => p.Version.ToNormalizedString())
                    .FirstOrDefault();

            return new DependencyInfo()
            {
                Name = name,
                NewVersions = newPackageVersions,
                NewReleaseVersion = newReleaseVersion
            };
        }

        [Target(nameof(ReplaceProjectJson), nameof(ReplaceCrossGen), nameof(ReplaceCoreHostPackaging))]
        public static BuildTargetResult ReplaceVersions(BuildTargetContext c) => c.Success();

        /// <summary>
        /// Replaces all the dependency versions in the project.json files.
        /// </summary>
        [Target]
        public static BuildTargetResult ReplaceProjectJson(BuildTargetContext c)
        {
            List<DependencyInfo> dependencyInfos = c.GetDependencyInfos();

            IEnumerable<string> projectJsonFiles = Directory.GetFiles(Dirs.RepoRoot, "project.json", SearchOption.AllDirectories);

            JObject projectRoot;
            foreach (string projectJsonFile in projectJsonFiles)
            {
                try
                {
                    projectRoot = ReadProject(projectJsonFile);
                }
                catch (Exception e)
                {
                    c.Warn($"Non-fatal exception occurred reading '{projectJsonFile}'. Skipping file. Exception: {e}. ");
                    continue;
                }

                if (projectRoot == null)
                {
                    c.Warn($"A non valid JSON file was encountered '{projectJsonFile}'. Skipping file.");
                    continue;
                }

                bool changedAnyPackage = FindAllDependencyProperties(projectRoot)
                    .Select(dependencyProperty => ReplaceDependencyVersion(dependencyProperty, dependencyInfos))
                    .ToArray()
                    .Any(shouldWrite => shouldWrite);

                if (changedAnyPackage)
                {
                    c.Info($"Writing changes to {projectJsonFile}");
                    WriteProject(projectRoot, projectJsonFile);
                }
            }

            return c.Success();
        }

        /// <summary>
        /// Replaces the single dependency with the updated version, if it matches any of the dependencies that need to be updated.
        /// </summary>
        private static bool ReplaceDependencyVersion(JProperty dependencyProperty, List<DependencyInfo> dependencyInfos)
        {
            string id = dependencyProperty.Name;
            foreach (DependencyInfo dependencyInfo in dependencyInfos)
            {
                foreach (PackageInfo packageInfo in dependencyInfo.NewVersions)
                {
                    if (id == packageInfo.Id)
                    {
                        if (dependencyProperty.Value is JObject)
                        {
                            dependencyProperty.Value["version"] = packageInfo.Version.ToNormalizedString();
                        }
                        else
                        {
                            dependencyProperty.Value = packageInfo.Version.ToNormalizedString();
                        }

                        // mark the DependencyInfo as updated so we can tell which dependencies were updated
                        dependencyInfo.IsUpdated = true;

                        return true;
                    }
                }
            }

            return false;
        }

        private static JObject ReadProject(string projectJsonPath)
        {
            using (TextReader projectFileReader = File.OpenText(projectJsonPath))
            {
                var projectJsonReader = new JsonTextReader(projectFileReader);

                var serializer = new JsonSerializer();
                return serializer.Deserialize<JObject>(projectJsonReader);
            }
        }

        private static void WriteProject(JObject projectRoot, string projectJsonPath)
        {
            string projectJson = JsonConvert.SerializeObject(projectRoot, Formatting.Indented);

            File.WriteAllText(projectJsonPath, projectJson + Environment.NewLine);
        }

        private static IEnumerable<JProperty> FindAllDependencyProperties(JObject projectJsonRoot)
        {
            return projectJsonRoot
                .Descendants()
                .OfType<JProperty>()
                .Where(property => property.Name == "dependencies")
                .Select(property => property.Value)
                .SelectMany(o => o.Children<JProperty>());
        }

        /// <summary>
        /// Replaces version number that is hard-coded in the CrossGen script.
        /// </summary>
        [Target]
        public static BuildTargetResult ReplaceCrossGen(BuildTargetContext c)
        {
            ReplaceFileContents(@"build_projects\shared-build-targets-utils\DependencyVersions.cs", dependencyVersionsContent =>
            {
                DependencyInfo coreFXInfo = c.GetCoreFXDependency();
                Regex regex = new Regex(@"CoreCLRVersion = ""(?<version>\d.\d.\d)-(?<release>.*)"";");

                // TODO: this needs a CoreCLR DependencyInfo
                return regex.ReplaceGroupValue(dependencyVersionsContent, "release", coreFXInfo.NewReleaseVersion);
            });

            return c.Success();
        }

        /// <summary>
        /// Replaces version number that is hard-coded in the corehost packaging dir.props file.
        /// </summary>
        [Target]
        public static BuildTargetResult ReplaceCoreHostPackaging(BuildTargetContext c)
        {
            ReplaceFileContents(@"pkg\dir.props", contents =>
            {
                DependencyInfo coreFXInfo = c.GetCoreFXDependency();
                Regex regex = new Regex(@"Microsoft\.NETCore\.Platforms\\(?<version>\d\.\d\.\d)-(?<release>.*)\\runtime\.json");

                // TODO: this needs a CoreCLR DependencyInfo
                return regex.ReplaceGroupValue(contents, "release", coreFXInfo.NewReleaseVersion);
            });

            return c.Success();
        }

        private static DependencyInfo GetCoreFXDependency(this BuildTargetContext c)
        {
            return c.GetDependencyInfos().Single(d => d.Name == "CoreFx");
        }

        private static void ReplaceFileContents(string repoRelativePath, Func<string, string> replacement)
        {
            string fullPath = Path.Combine(Dirs.RepoRoot, repoRelativePath);
            string contents = File.ReadAllText(fullPath);

            contents = replacement(contents);

            File.WriteAllText(fullPath, contents, Encoding.UTF8);
        }

        private static string ReplaceGroupValue(this Regex regex, string input, string groupName, string newValue)
        {
            return regex.Replace(input, m =>
            {
                string replacedValue = m.Value;
                Group group = m.Groups[groupName];
                int startIndex = group.Index - m.Index;

                replacedValue = replacedValue.Remove(startIndex, group.Length);
                replacedValue = replacedValue.Insert(startIndex, newValue);

                return replacedValue;
            });
        }
    }
}
