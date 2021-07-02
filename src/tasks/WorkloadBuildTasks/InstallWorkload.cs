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

            PackageInstaller installer = new(BuiltNuGetsPath.GetMetadata("FullPath"), ExtraNuGetSources ?? Array.Empty<ITaskItem>(), Log);

            Dictionary<string, PackVersionInformation> packs = new();
            Dictionary<string, WorkloadInformation> workloads = new();
            if (!InstallWorkloadManifest(WorkloadId.GetMetadata("ManifestName"), WorkloadId.GetMetadata("Version"), ref workloads, ref packs))
            {
                return false;
            }

            // foreach (var kvp in packs)
                // Log.LogMessage(MessageImportance.High, $"Final: {kvp.Key} => {kvp.Value}");


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
                                             ref Dictionary<string, WorkloadInformation> workloads,
                                             ref Dictionary<string, PackVersionInformation> allPacks)
        {
            PackageInstaller installer = new(BuiltNuGetsPath.GetMetadata("FullPath"), ExtraNuGetSources ?? Array.Empty<ITaskItem>(), Log);
            PackageReference pkgRef = new(Name: $"{name}.Manifest-{VersionBand}",
                                          Version: version,
                                          OutputDir: Path.Combine(OutputDir, "sdk-manifests", VersionBand, name),
                                          relativeSourceDir: "data");

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
                ManifestInformation? manifest = JsonSerializer.Deserialize<ManifestInformation>(
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

                foreach (var kvp in manifest.Packs)
                    allPacks.Add(kvp.Key, kvp.Value);

                // get the workload that we want

                if (manifest.DependsOn != null)
                {
                    foreach ((string depName, string depVersion) in manifest.DependsOn)
                    {
                        Log.LogMessage(MessageImportance.High, $"{depName} = {depVersion}");

                        if (!InstallWorkloadManifest(depName, depVersion, ref workloads, ref allPacks))
                            return false;
                    }
                }

                return true;
            }
            catch (JsonException je)
            {
                Log.LogError($"Failed to read from {jsonPath}: {je.Message}");
                return false;
            }
        }

        private IEnumerable<PackageReference> GetPackageReferencesForWorkload(string workloadId,
                                                                              Dictionary<string, WorkloadInformation> allWorkloads,
                                                                              Dictionary<string, PackVersionInformation> allPacks)
        {
            WorkloadInformation? workload = allWorkloads[workloadId];
            List<string> packsNeededForWorkload = workload.Packs;
            if (workload.Extends.Count > 0)
            {
                packsNeededForWorkload = new List<string>(packsNeededForWorkload);
                //FIXME: use exceptions!
                if (!ReadPacksForWorkload(workload))
                    throw new KeyNotFoundException();
            }

            List<PackageReference> references = new();
            foreach (KeyValuePair<string, PackVersionInformation> item in allPacks)
            {
                if (/*packsNeededForWorkload != null && */!packsNeededForWorkload.Contains(item.Key))
                {
                    Log.LogMessage(MessageImportance.Low, $"Ignoring pack {item.Key} as it is not in the workload");
                    continue;
                }

                //if (item.Value.AliasTo is Dictionary<string, string> alias)
                if (!item.Value.AliasTo.TryGetValue(Rid!, out string? packageName))
                    packageName = item.Key;

                if (!string.IsNullOrEmpty(packageName) && !packageName.Contains("cross", StringComparison.InvariantCultureIgnoreCase))
                    references.Add(new PackageReference(packageName, item.Value.Version, Path.Combine(_packsDir!, $"{packageName}.{item.Value.Version}")));
            }

            return references;

            bool ReadPacksForWorkload(WorkloadInformation workload)
            {
                if (workload.Extends == null || workload.Extends.Count == 0)
                    return true;

                foreach (var w in workload.Extends)
                {
                    if (!allWorkloads.TryGetValue(w, out WorkloadInformation? depWorkload))
                    {
                        Log.LogError($"Could not find workload {w} needed by {workload.Description} in the manifest");
                        return false;
                    }
                    //FIXME:

                    if (!ReadPacksForWorkload(depWorkload))
                        return false;

                    packsNeededForWorkload.AddRange(depWorkload.Packs);
                }

                return true;
            }
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
}
