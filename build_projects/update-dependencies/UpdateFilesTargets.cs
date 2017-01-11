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
        private static readonly string FileUrlScheme = "file://";

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
            dependencyInfos.Add(CreateDependencyInfo("CoreClr", Config.Instance.CoreClrVersionUrl).Result);
            dependencyInfos.Add(CreateDependencyInfo("Roslyn", Config.Instance.RoslynVersionUrl).Result);

            return c.Success();
        }

        private static async Task<DependencyInfo> CreateDependencyInfo(string name, string packageVersionsUrl)
        {
            List<PackageInfo> newPackageVersions = new List<PackageInfo>();

            using (Stream versionsStream = packageVersionsUrl.StartsWith(FileUrlScheme, StringComparison.Ordinal) ? File.OpenRead(packageVersionsUrl.Substring(FileUrlScheme.Length)) : await s_client.GetStreamAsync(packageVersionsUrl))
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

        [Target(nameof(ReplaceProjectJson), nameof(ReplaceDependencyVersions), nameof(ReplaceCoreHostPackaging))]
        public static BuildTargetResult ReplaceVersions(BuildTargetContext c) => c.Success();

        /// <summary>
        /// Replaces all the dependency versions in the project.json files.
        /// </summary>
        [Target]
        public static BuildTargetResult ReplaceProjectJson(BuildTargetContext c)
        {
            List<DependencyInfo> dependencyInfos = c.GetDependencyInfos();

            var projectJsonFiles = new List<string>
            {
                Path.Combine(Dirs.PkgProjects, "Microsoft.NETCore.App", "project.json.template"),
                Path.Combine(Dirs.PkgDeps, "project.json")
            };
            //projectJsonFiles.AddRange(Directory.GetFiles(Dirs.RepoRoot, "project.json", SearchOption.AllDirectories));

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
                        string oldVersion;
                        if (dependencyProperty.Value is JObject)
                        {
                            oldVersion = (string)dependencyProperty.Value["version"];
                        }
                        else
                        {
                            oldVersion = (string)dependencyProperty.Value;
                        }

                        string newVersion = packageInfo.Version.ToNormalizedString();
                        if (oldVersion != newVersion)
                        {
                            if (dependencyProperty.Value is JObject)
                            {
                                dependencyProperty.Value["version"] = newVersion;
                            }
                            else
                            {
                                dependencyProperty.Value = newVersion;
                            }

                            // mark the DependencyInfo as updated so we can tell which dependencies were updated
                            dependencyInfo.IsUpdated = true;

                            return true;
                        }
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
        /// Replaces version numbers hard-coded in DependencyVersions.cs.
        /// </summary>
        [Target]
        public static BuildTargetResult ReplaceDependencyVersions(BuildTargetContext c)
        {
            ReplaceFileContents(Path.Combine("build_projects", "shared-build-targets-utils", "DependencyVersions.cs"), fileContents =>
            {
                fileContents = ReplaceDependencyVersion(c, fileContents, "CoreCLRVersion", "Microsoft.NETCore.Runtime.CoreCLR");
                fileContents = ReplaceDependencyVersion(c, fileContents, "JitVersion", "Microsoft.NETCore.Jit");

                return fileContents;
            });

            return c.Success();
        }

        private static string ReplaceDependencyVersion(BuildTargetContext c, string fileContents, string dependencyPropertyName, string packageId)
        {
            Regex regex = new Regex($@"{dependencyPropertyName} = ""(?<version>.*)"";");
            string newVersion = c.GetNewVersion(packageId);

            if (newVersion != null)
            {
                fileContents = regex.ReplaceGroupValue(fileContents, "version", newVersion);
            }
            return fileContents;
        }

        /// <summary>
        /// Replaces version number that is hard-coded in the corehost packaging dir.props file.
        /// </summary>
        [Target]
        public static BuildTargetResult ReplaceCoreHostPackaging(BuildTargetContext c)
        {
            ReplaceFileContents(Path.Combine("pkg", "dir.props"), contents =>
            {
                Regex regex = new Regex(@"Microsoft\.NETCore\.Platforms\\(?<version>.*)\\runtime\.json");
                string newNetCorePlatformsVersion = c.GetNewVersion("Microsoft.NETCore.Platforms");

                if (newNetCorePlatformsVersion != null)
                {
                    contents = regex.ReplaceGroupValue(contents, "version", newNetCorePlatformsVersion);
                }
                return contents;
            });

            return c.Success();
        }

        private static string GetNewVersion(this BuildTargetContext c, string packageId)
        {
            string newVersion = c.GetDependencyInfos()
                .SelectMany(d => d.NewVersions)
                .FirstOrDefault(p => p.Id == packageId)
                ?.Version
                .ToNormalizedString();

            if (string.IsNullOrEmpty(newVersion))
            {
                c.Info($"Could not find package version information for '{packageId}'");
                return null;
            }

            return newVersion;
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
