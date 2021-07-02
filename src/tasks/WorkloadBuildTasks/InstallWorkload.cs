// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace Microsoft.Workload.Build.Tasks
{
    public class InstallWorkload : Task
    {
        [Required, NotNull]
        public ITaskItem?     WorkloadId         { get; set; }

        [Required, NotNull]
        public string?        VersionBand        { get; set; }

        [Required, NotNull]
        public ITaskItem?     BuiltNuGetsPath    { get; set; }

        [Required, NotNull]
        public string?        OutputDir          { get; set; }

        public ITaskItem[]?   ExtraNuGetSources  { get; set; }
        public string?        Rid                { get; set; }

        private string? _packsDir;

        private static string? GetRid()
        {
            if (OperatingSystem.IsWindows())
                return Environment.Is64BitProcess ? "win-x64": "win-x86";
            else if (OperatingSystem.IsMacOS())
                return "osx-x64";
            else if (OperatingSystem.IsLinux())
                return "linux-x64";
            else
                return null;
        }

        public override bool Execute()
        {
            Utils.Logger = Log;

            if (!HasMetadata(WorkloadId, nameof(WorkloadId), "Version") ||
                !HasMetadata(WorkloadId, nameof(WorkloadId), "ManifestName"))
            {
                return false;
            }

            if (!Directory.Exists(OutputDir))
            {
                Log.LogError($"Cannot find OutputDir={OutputDir}");
                return false;
            }

            _packsDir = Path.Combine(OutputDir, "packs");
            Rid ??= GetRid();
            if (Rid == null)
            {
                Log.LogError("Unsupported platform");
                return false;
            }

            if (!InstallWorkloadManifest(WorkloadId.GetMetadata("ManifestName"), WorkloadId.GetMetadata("Version"), out ManifestInformation? manifest))
            {
                return false;
            }

            Dictionary<string, PackVersionInformation> allPacks = new();
            Dictionary<string, WorkloadInformation> allWorkloads = new();
            allWorkloads.Merge(manifest.Workloads);
            allPacks.Merge(manifest.Packs);

            Log.LogMessage(MessageImportance.High, $"START: allWorklaodS: {allWorkloads.DumpToString()}");
            Log.LogMessage(MessageImportance.High, $"START: allPacks: {allPacks.DumpToString()}");

            var packsNeeded = GetPacksNeededForWorkload(WorkloadId.ItemSpec, manifest, allWorkloads, allPacks);

            Log.LogMessage(MessageImportance.High, $"Final needed: {packsNeeded.DumpToString()}");

            var packRefs = ResolvePacks(packsNeeded, allPacks);
            foreach (var pr in packRefs)
                Log.LogMessage(MessageImportance.High, $"Final resolved: {pr}");


            // if (!InstallWorkloadManifest(WorkloadId.ItemSpec, WorkloadId.GetMetadata("Version"), out ManifestInformation? manifest))
            //     return false;

            // IEnumerable<PackageReference> references = GetPackageReferencesForWorkload(string workloadId, Dictionary<string, WorkloadInformation> workloads, Dictionary<string, PackVersionInformation> packs);
            // // IEnumerable<PackageReference> remaining = LayoutPacksFromBuiltNuGets(references);
            // if (!remaining.Any())
            //     return !Log.HasLoggedErrors;

            // // if (!InstallPacksWithNuGetRestore(remaining))
            //     return false;

            return !Log.HasLoggedErrors;
        }

        // private bool InstallWorkloadManifest(string name, string version, [NotNullWhen(true)] out ManifestInformation? manifest)
        private bool InstallWorkloadManifest(string name,
                                             string version,
                                             [NotNullWhen(true)] out ManifestInformation? manifest)
                                            //  ref Dictionary<string, WorkloadInformation> workloads,
                                            //  ref Dictionary<string, PackVersionInformation> allPacks)
        {
            Log.LogMessage(MessageImportance.High, $"InstallWorkloadManifest: installing {name}");
            PackageInstaller installer = new(BuiltNuGetsPath.GetMetadata("FullPath"), ExtraNuGetSources ?? Array.Empty<ITaskItem>(), Log);
            PackageReference pkgRef = new(Name: $"{name}.Manifest-{VersionBand}",
                                          Version: version,
                                          OutputDir: Path.Combine(OutputDir, "sdk-manifests", VersionBand, name),
                                          relativeSourceDir: "data");

            manifest = null;
            if (!installer.Install(pkgRef))
                return false;

            string manifestDir = pkgRef.OutputDir;
            string jsonPath = Path.Combine(manifestDir, "WorkloadManifest.json");
            if (!File.Exists(jsonPath))
            {
                Log.LogError($"Could not find WorkloadManifest.json at {jsonPath}");
                return false;
            }

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

                // foreach (var kvp in manifest.Packs)
                //     allPacks.Add(kvp.Key, kvp.Value);

                // // get the workload that we want

                // if (manifest.DependsOn != null)
                // {
                //     foreach ((string depName, string depVersion) in manifest.DependsOn)
                //     {
                //         Log.LogMessage(MessageImportance.High, $"{depName} = {depVersion}");

                //         if (!InstallWorkloadManifest(depName, depVersion, ref workloads, ref allPacks))
                //             return false;
                //     }
                // }

                return true;
            }
            catch (JsonException je)
            {
                Log.LogError($"Failed to read from {jsonPath}: {je.Message}");
                return false;
            }
        }

        private IEnumerable<string> GetPacksNeededForWorkload(string workloadId, ManifestInformation manifest,
                                                                Dictionary<string, WorkloadInformation> allWorkloads,
                                                                Dictionary<string, PackVersionInformation> allPacks)
        {
            if (!manifest.Workloads.TryGetValue(workloadId, out WorkloadInformation? workload))
                throw new Exception($"Could not find workload {workloadId}");

            // WorkloadInformation? workload = manifest.Workloads[workloadId];

            // Dictionary<string, PackVersionInformation> allPacks = new(manifest.Packs);
            HashSet<string> packsNeededForWorkload = new(workload.Packs);
            Log.LogMessage(MessageImportance.High, $"GetPackRef.. start packsNeeded: {packsNeededForWorkload.DumpToString()}");
            if (workload.Extends == null || workload.Extends.Count == 0)
                return packsNeededForWorkload;

            Log.LogMessage(MessageImportance.High, $"- ReadPacksForWorkload: {workload.Description}");

            foreach (var extendsWorkloadId in workload.Extends)
            {
                Log.LogMessage(MessageImportance.High, $"- GetPacks: looking for extends: {extendsWorkloadId}");
                if (!allWorkloads.ContainsKey(extendsWorkloadId))
                {
                    Log.LogMessage(MessageImportance.High, $"Could not find workload {extendsWorkloadId} needed by {workload.Description} in the manifest");

                    foreach ((string depName, string depVersion) in manifest.DependsOn)
                    {
                        Log.LogMessage(MessageImportance.High, $"ReadPacks.. before installing dep: {packsNeededForWorkload.DumpToString()}");
                        Log.LogMessage(MessageImportance.High, $"{depName} = {depVersion}, let's try installing that!");

                        if (!InstallWorkloadManifest(depName, depVersion, out ManifestInformation? depManifest))
                        {
                            Log.LogMessage(MessageImportance.High, $"Could not find workload {extendsWorkloadId} needed by {workload.Description}");
                            //FIXME: skip as arg
                            continue;
                        }

                        Log.LogMessage(MessageImportance.High, $"\tAdding workloads, and packs that we found for {depName}");

                        //FIXME: duplicate key
                        allWorkloads.Merge(depManifest.Workloads);
                        allPacks.Merge(depManifest.Packs);
                    }
                }

                if (allWorkloads.TryGetValue(extendsWorkloadId, out WorkloadInformation? extendsWorkloadInfo))// && ReadPacksForWorkload(depWorkload))
                {
                    Log.LogMessage(MessageImportance.High, $"\tReadPacks.. found the extended one with :{extendsWorkloadInfo.Packs.DumpToString()}");
                    packsNeededForWorkload.UnionWith(extendsWorkloadInfo.Packs);
                    Log.LogMessage(MessageImportance.High, $"ReadPacks.. after adding new installing dep({extendsWorkloadId}): {packsNeededForWorkload.DumpToString()}");
                }
                else
                {
                    Log.LogMessage(MessageImportance.High, $"GetPacksNeeded .. couldn't find workload {extendsWorkloadId} in allWorkloads: {allWorkloads.DumpToString()}");
                }
            }

            return packsNeededForWorkload;
        }

        private IEnumerable<PackageReference> ResolvePacks(IEnumerable<string> packsToResolve, IDictionary<string, PackVersionInformation> allPacks)
        {
            // Resolve pack references
            List<PackageReference> references = new();
            foreach (var packRefName in packsToResolve.Distinct())
            {
                if (!allPacks.TryGetValue(packRefName, out PackVersionInformation? packInfo))
                    throw new Exception($"Could not find pack named {packRefName}");

                if (packInfo.AliasTo == null || !packInfo.AliasTo.TryGetValue(Rid!, out string? actualPackageName))
                    actualPackageName = packRefName;

                if (!string.IsNullOrEmpty(actualPackageName))// && !actualPackageName.Contains("cross", StringComparison.InvariantCultureIgnoreCase))
                {
                    references.Add(new PackageReference(actualPackageName,
                                                        packInfo.Version,
                                                        Path.Combine(_packsDir!, $"{actualPackageName}.{packInfo.Version}")));
                }
            }

            return references;
        }

        private bool HasMetadata(ITaskItem item, string itemName, string metadataName)
        {
            if (!string.IsNullOrEmpty(item.GetMetadata(metadataName)))
                return true;

            Log.LogError($"{itemName} item ({item.ItemSpec}) is missing Name metadata");
            return false;
        }

        private record ManifestInformation(
            object Version,
            string Description,

            [property: JsonPropertyName("depends-on")]
            IDictionary<string, string> DependsOn,
            IDictionary<string, WorkloadInformation> Workloads,
            IDictionary<string, PackVersionInformation> Packs,
            object Data
        );

        private record WorkloadInformation(
            bool Abstract,
            string Kind,
            string Description,

            List<string> Packs,
            List<string> Extends,
            List<string> Platforms
        );

        private record PackVersionInformation(
            string Kind,
            string Version,
            [property: JsonPropertyName("alias-to")]
            Dictionary<string, string> AliasTo
        );
    }

    internal record PackageReference(string Name, string Version, string OutputDir, string? relativeSourceDir=null, string? RestoredPath=null);

    internal static class DictionaryExtensions
    {
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> self, IDictionary<TKey, TValue> other, bool overwrite=false)
        {
            if (overwrite)
            {
                foreach (var kvp in other)
                    self[kvp.Key] = kvp.Value;
            }
            else
            {
                foreach (var kvp in other)
                    self.Add(kvp.Key, kvp.Value);
            }

            return self;
        }

        public static string DumpToString<TKey, TValue>(this IDictionary<TKey, TValue> self)
        {
            StringBuilder sb = new();
            sb.AppendLine("{");
            foreach (var kvp in self)
                sb.AppendLine($"\t[{kvp.Key}] = {kvp.Value}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string DumpToString<TElement>(this IEnumerable<TElement> self)
        {
            StringBuilder sb = new();
            sb.AppendLine("[");
            foreach (var elem in self)
                sb.AppendLine($"\t{elem}");
            sb.AppendLine("]");
            return sb.ToString();
        }
    }
}
