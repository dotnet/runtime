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

        // private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "install-workload", Path.GetRandomFileName());
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

            if (!HasMetadata(WorkloadId, nameof(WorkloadId), "Version"))
                return false;

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
            // if (!installer.Install(


            // if (!InstallWorkloadManifest(WorkloadId.ItemSpec, WorkloadId.GetMetadata("Version"), out ManifestInformation? manifest))
            //     return false;

            // IEnumerable<PackageReference> references = GetPackageReferencesForWorkload(manifest, WorkloadId.ItemSpec);
            // IEnumerable<PackageReference> remaining = LayoutPacksFromBuiltNuGets(references);
            // if (!remaining.Any())
            //     return !Log.HasLoggedErrors;

            // if (!InstallPacksWithNuGetRestore(remaining))
            //     return false;

            return !Log.HasLoggedErrors;
        }

        private bool InstallWorkloadManifest(string workloadId, string workloadVersion, [NotNullWhen(true)] out ManifestInformation? manifest)
        {
            manifest = null;
            StringBuilder errorBuilder = new();

            if (TryInstallManifestFromArtifacts(workloadId, workloadVersion))

            string jsonPath = Path.Combine(targetManifestDirectory, "WorkloadManifest.json");
            if (!File.Exists(jsonPath))
            {
                Log.LogError($"Could not find WorkloadManifest.json at {jsonPath}");
                return false;
            }

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

            manifestNupkgPath = nupkgPath;
            return true;
        }



        private IEnumerable<PackageReference> GetPackageReferencesForWorkload(ManifestInformation manifest, string workloadId)
        {
            var workload = manifest.Workloads[workloadId];
            var subset = workload.Packs;
            if (workload.Extends.Count > 0)
            {
                subset = new List<string>(subset);
                //FIXME: use exceptions!
                if (!ProcessWorkload(workload))
                    throw new KeyNotFoundException();
            }

            List<PackageReference> references = new();
            foreach (var item in manifest.Packs)
            {
                if (subset != null && !subset.Contains(item.Key))
                {
                    Log.LogMessage(MessageImportance.Low, $"Ignoring pack {item.Key} as it is not in the workload");
                    continue;
                }

                var packageName = item.Key;
                if (item.Value.AliasTo is Dictionary<string, string> alias)
                {
                    alias.TryGetValue(Rid!, out packageName);
                }

                if (!string.IsNullOrEmpty(packageName) && !packageName.Contains("cross", StringComparison.InvariantCultureIgnoreCase))
                    references.Add(new PackageReference(packageName, item.Value.Version));
            }

            return references;

            bool ProcessWorkload(WorkloadInformation workload)
            {
                if (workload.Extends == null || workload.Extends.Count == 0)
                    return true;

                foreach (var w in workload.Extends)
                {
                    if (!manifest.Workloads.TryGetValue(w, out WorkloadInformation? depWorkload))
                    {
                        Log.LogError($"Could not find workload {w} needed by {workload.Description} in the manifest");
                        return false;
                    }
                    //FIXME:

                    if (!ProcessWorkload(depWorkload))
                        return false;

                    subset.AddRange(depWorkload.Packs);
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
            IDictionary<string, object> DependsOn,
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

    internal record PackageReference(string Name, string Version, string OutputDir, string? RestoredPath=null);
}
