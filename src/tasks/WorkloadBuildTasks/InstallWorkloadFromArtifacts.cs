// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace Microsoft.Workload.Build.Tasks
{
    public class InstallWorkloadFromArtifacts : Task
    {
        [Required, NotNull]
        public ITaskItem?     WorkloadId         { get; set; }

        [Required, NotNull]
        public string?        VersionBand        { get; set; }

        [Required, NotNull]
        public string?        LocalNuGetsPath    { get; set; }

        [Required, NotNull]
        public string?        TemplateNuGetConfigPath { get; set; }

        [Required, NotNull]
        public string?        SdkDir             { get; set; }

        public bool           OnlyUpdateManifests{ get; set; }

        private const string s_nugetInsertionTag = "<!-- TEST_RESTORE_SOURCES_INSERTION_LINE -->";

        public override bool Execute()
        {
            try
            {
                return ExecuteInternal();
            }
            catch (LogAsErrorException laee)
            {
                Log.LogError(laee.Message);
                return false;
            }
        }

        private bool ExecuteInternal()
        {
            if (!HasMetadata(WorkloadId, nameof(WorkloadId), "Version") ||
                !HasMetadata(WorkloadId, nameof(WorkloadId), "ManifestName"))
            {
                return false;
            }

            if (!Directory.Exists(SdkDir))
            {
                Log.LogError($"Cannot find SdkDir={SdkDir}");
                return false;
            }

            if (!File.Exists(TemplateNuGetConfigPath))
            {
                Log.LogError($"Cannot find TemplateNuGetConfigPath={TemplateNuGetConfigPath}");
                return false;
            }

            Log.LogMessage(MessageImportance.High, $"{Environment.NewLine}** Installing workload manifest {WorkloadId.ItemSpec} **{Environment.NewLine}");

            string nugetConfigContents = GetNuGetConfig();
            if (!InstallWorkloadManifest(WorkloadId.GetMetadata("ManifestName"), WorkloadId.GetMetadata("Version"), nugetConfigContents, stopOnMissing: true))
                return false;

            if (OnlyUpdateManifests)
                return !Log.HasLoggedErrors;

            string nugetConfigPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllText(nugetConfigPath, nugetConfigContents);

            Log.LogMessage(MessageImportance.High, $"{Environment.NewLine}** workload install **{Environment.NewLine}");
            (int exitCode, string output) = Utils.TryRunProcess(
                                                    Log,
                                                    Path.Combine(SdkDir, "dotnet"),
                                                    $"workload install --skip-manifest-update --no-cache --configfile \"{nugetConfigPath}\" {WorkloadId.ItemSpec}",
                                                    workingDir: Path.GetTempPath(),
                                                    silent: false,
                                                    debugMessageImportance: MessageImportance.High);
            if (exitCode != 0)
            {
                Log.LogError($"workload install failed: {output}");

                foreach (var dir in Directory.EnumerateDirectories(Path.Combine(SdkDir, "sdk-manifests"), "*", SearchOption.AllDirectories))
                    Log.LogMessage(MessageImportance.Low, $"\t{Path.Combine(SdkDir, "sdk-manifests", dir)}");

                foreach (var dir in Directory.EnumerateDirectories(Path.Combine(SdkDir, "packs"), "*", SearchOption.AllDirectories))
                    Log.LogMessage(MessageImportance.Low, $"\t{Path.Combine(SdkDir, "packs", dir)}");

                return false;
            }

            return !Log.HasLoggedErrors;
        }

        private string GetNuGetConfig()
        {
            string contents = File.ReadAllText(TemplateNuGetConfigPath);
            if (contents.IndexOf(s_nugetInsertionTag) < 0)
                throw new LogAsErrorException($"Could not find {s_nugetInsertionTag} in {TemplateNuGetConfigPath}");

            return contents.Replace(s_nugetInsertionTag, $@"<add key=""nuget-local"" value=""{LocalNuGetsPath}"" />");
        }

        private bool InstallWorkloadManifest(string name, string version, string nugetConfigContents, bool stopOnMissing)
        {
            Log.LogMessage(MessageImportance.High, $"Installing workload manifest for {name}/{version}");

            // Find any existing directory with the manifest name, ignoring the case
            // Multiple directories for a manifest, differing only in case causes
            // workload install to fail due to duplicate manifests!
            // This is applicable only on case-sensitive filesystems
            string outputDir = FindSubDirIgnoringCase(Path.Combine(SdkDir, "sdk-manifests", VersionBand), name);

            PackageReference pkgRef = new(Name: $"{name}.Manifest-{VersionBand}",
                                          Version: version,
                                          OutputDir: outputDir,
                                          relativeSourceDir: "data");

            if (!PackageInstaller.Install(new[]{ pkgRef }, nugetConfigContents, Log, stopOnMissing))
                return false;

            string manifestDir = pkgRef.OutputDir;
            string jsonPath = Path.Combine(manifestDir, "WorkloadManifest.json");
            if (!File.Exists(jsonPath))
            {
                Log.LogError($"Could not find WorkloadManifest.json at {jsonPath}");
                return false;
            }

            ManifestInformation? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<ManifestInformation>(
                                                    File.ReadAllBytes(jsonPath),
                                                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                                                    {
                                                        AllowTrailingCommas = true,
                                                        ReadCommentHandling = JsonCommentHandling.Skip
                                                    });

                if (manifest == null)
                {
                    Log.LogError($"Could not parse manifest from {jsonPath}.");
                    return false;
                }
            }
            catch (JsonException je)
            {
                Log.LogError($"Failed to read from {jsonPath}: {je.Message}");
                return false;
            }

            if (manifest.DependsOn != null)
            {
                foreach ((string depName, string depVersion) in manifest.DependsOn)
                {
                    if (!InstallWorkloadManifest(depName, depVersion, nugetConfigContents, stopOnMissing: false))
                    {
                        Log.LogWarning($"Could not install manifest {depName}/{depVersion}. This can be ignored if the workload {WorkloadId.ItemSpec} doesn't depend on it.");
                        continue;
                    }
                }
            }

            return true;
        }

        private bool HasMetadata(ITaskItem item, string itemName, string metadataName)
        {
            if (!string.IsNullOrEmpty(item.GetMetadata(metadataName)))
                return true;

            Log.LogError($"{itemName} item ({item.ItemSpec}) is missing Name metadata");
            return false;
        }

        private string FindSubDirIgnoringCase(string parentDir, string dirName)
        {
            IEnumerable<string> matchingDirs = Directory.EnumerateDirectories(parentDir,
                                                            dirName,
                                                            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

            string? first = matchingDirs.FirstOrDefault();
            if (matchingDirs.Count() > 1)
            {
                Log.LogWarning($"Found multiple directories with names that differ only in case. {string.Join(", ", matchingDirs.ToArray())}"
                                + $"{Environment.NewLine}Using the first one: {first}");
            }

            return first ?? Path.Combine(parentDir, dirName);
        }

        private sealed record ManifestInformation(
            object Version,
            string Description,

            [property: JsonPropertyName("depends-on")]
            IDictionary<string, string> DependsOn,
            IDictionary<string, WorkloadInformation> Workloads,
            IDictionary<string, PackVersionInformation> Packs,
            object Data
        );

        private sealed record WorkloadInformation(
            bool Abstract,
            string Kind,
            string Description,

            List<string> Packs,
            List<string> Extends,
            List<string> Platforms
        );

        private sealed record PackVersionInformation(
            string Kind,
            string Version,
            [property: JsonPropertyName("alias-to")]
            Dictionary<string, string> AliasTo
        );
    }

    internal sealed record PackageReference(string Name,
                                     string Version,
                                     string OutputDir,
                                     string relativeSourceDir = "");
}
