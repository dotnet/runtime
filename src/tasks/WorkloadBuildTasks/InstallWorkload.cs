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
        public ITaskItem?     ManifestPackage    { get; set; }

        [Required, NotNull]
        public ITaskItem?     BuiltNuGetsPath    { get; set; }

        [Required, NotNull]
        public string?        OutputDir          { get; set; }

        public ITaskItem[]?   ExtraNuGetSources  { get; set; }
        public string?        Rid                { get; set; }

        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "install-workload", Path.GetRandomFileName());
        private string? _packsDir;
        private const string s_stampFileName = ".installed.stamp";

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

            if (!HasMetadata(ManifestPackage, nameof(ManifestPackage), "VersionBand") ||
                !HasMetadata(ManifestPackage, nameof(ManifestPackage), "Version") ||
                !HasMetadata(WorkloadId, nameof(WorkloadId), "Name"))
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

            if (!InstallWorkloadManifest(out ManifestInformation? manifest, out string? manifestNupkgPath))
                return false;

            IEnumerable<PackageReference> references = GetPackageReferencesForWorkload(manifest, WorkloadId.ItemSpec);
            IEnumerable<PackageReference> remaining = LayoutPacksFromBuiltNuGets(references);
            if (!remaining.Any())
                return !Log.HasLoggedErrors;

            if (!InstallPacksWithNuGetRestore(remaining))
                return false;

            return !Log.HasLoggedErrors;
        }

        private bool InstallWorkloadManifest([NotNullWhen(true)] out ManifestInformation? manifest, [NotNullWhen(true)] out string? manifestNupkgPath)
        {
            manifest = null;
            manifestNupkgPath = null;

            string builtNuGetsFullPath = BuiltNuGetsPath.GetMetadata("FullPath");
            string pkgName = ManifestPackage.ItemSpec;
            string pkgVersion = ManifestPackage.GetMetadata("Version");

            var nupkgFileName = $"{pkgName}.{pkgVersion}.nupkg";
            var nupkgPath = Path.Combine(builtNuGetsFullPath, nupkgFileName);
            if (!File.Exists(nupkgPath))
            {
                Log.LogError($"Could not find nupkg for the manifest at {nupkgPath}");
                return false;
            }

            string baseManifestDir = Path.Combine(OutputDir, "sdk-manifests");

            string tmpManifestDir = Path.Combine(_tempDir, "manifest");
            ZipFile.ExtractToDirectory(nupkgPath, tmpManifestDir);

            var sourceManifestDirectory = Path.Combine(tmpManifestDir, "data");
            var targetManifestDirectory = Path.Combine(baseManifestDir, ManifestPackage.GetMetadata("VersionBand"), WorkloadId.GetMetadata("Name"));
            if (!CopyDirectory(sourceManifestDirectory, targetManifestDirectory))
                return false;

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

        private IEnumerable<PackageReference> LayoutPacksFromBuiltNuGets(IEnumerable<PackageReference> references)
        {
            string builtNuGetsFullPath = BuiltNuGetsPath.GetMetadata("FullPath");

            var allFiles = string.Join($"{Environment.NewLine}  ", Directory.EnumerateFiles(builtNuGetsFullPath, "*", new EnumerationOptions { RecurseSubdirectories = true }));
            Log.LogMessage(MessageImportance.Low, $"Files in {builtNuGetsFullPath}: {allFiles}");

            List<PackageReference> remaining = new(references.Count());
            foreach (var reference in references)
            {
                var nupkgFileName = $"{reference.Name}.{reference.Version}.nupkg";
                var nupkgPath = Path.Combine(builtNuGetsFullPath, nupkgFileName);
                if (!File.Exists(nupkgPath))
                {
                    string[] found = Directory.GetFiles(builtNuGetsFullPath, nupkgFileName, new EnumerationOptions
                                        {
                                            MatchCasing = MatchCasing.CaseInsensitive,
                                            RecurseSubdirectories = true
                                        });

                    if (found.Length == 0)
                    {
                        remaining.Add(reference);
                        continue;
                    }

                    nupkgPath = found[0];
                    nupkgFileName = Path.GetFileName(nupkgPath);
                }

                string installedPackDir = Path.Combine(_packsDir!, reference.Name, reference.Version);
                string stampFilePath = Path.Combine(installedPackDir, s_stampFileName);
                if (!IsFileNewer(nupkgPath, stampFilePath))
                {
                    Log.LogMessage(MessageImportance.Normal, $"Skipping {reference.Name}/{reference.Version} as it is already installed in {installedPackDir}.{Environment.NewLine}  {nupkgPath} is older than {stampFilePath}");
                    continue;
                }

                if (Directory.Exists(installedPackDir))
                {
                    Log.LogMessage(MessageImportance.Normal, $"Deleting {installedPackDir}");
                    Directory.Delete(installedPackDir, recursive: true);
                }

                Log.LogMessage(MessageImportance.High, $"Extracting {nupkgPath} => {installedPackDir}");
                ZipFile.ExtractToDirectory(nupkgPath, installedPackDir);

                // Add .nupkg.sha512, so it gets picked up when resolving nugets
                File.WriteAllText(Path.Combine(installedPackDir, $"{nupkgFileName}.sha512"), string.Empty);

                File.WriteAllText(stampFilePath, string.Empty);
            }

            return remaining;
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

        private bool InstallPacksWithNuGetRestore(IEnumerable<PackageReference> references)
        {
            var remaining = SkipInstalledPacks(references);
            if (!remaining.Any())
                return true;

            return TryRestorePackages(remaining, out PackageReference[]? restored) &&
                    LayoutPacksFromRestoredNuGets(restored);

            IEnumerable<PackageReference> SkipInstalledPacks(IEnumerable<PackageReference> candidates)
            {
                List<PackageReference> needed = new List<PackageReference>(candidates.Count());
                foreach (PackageReference pr in candidates)
                {
                    var installedPackDir = Path.Combine(_packsDir!, pr.Name, pr.Version);
                    var packStampFile = Path.Combine(installedPackDir, s_stampFileName);

                    if (File.Exists(packStampFile))
                    {
                        Log.LogMessage(MessageImportance.Normal, $"Skipping {pr.Name}/{pr.Version} as it is already installed in {installedPackDir}.{Environment.NewLine} {packStampFile} exists.");
                        continue;
                    }

                    needed.Add(pr);
                }

                return needed;
            }

            bool LayoutPacksFromRestoredNuGets(IEnumerable<PackageReference> restored)
            {
                foreach (var pkgRef in restored)
                {
                    if (pkgRef.RestoredPath == null)
                    {
                        Log.LogError($"Failed to restore {pkgRef.Name}/{pkgRef.Version}");
                        return false;
                    }

                    var source = pkgRef.RestoredPath;
                    var destDir = Path.Combine(_packsDir!, pkgRef.Name, pkgRef.Version);
                    if (!CopyDirectory(source, destDir))
                        return false;
                }

                return true;
            }
        }

        private bool TryRestorePackages(IEnumerable<PackageReference> references, [NotNullWhen(true)]out PackageReference[]? restoredPackages)
        {
            if (!references.Any())
            {
                restoredPackages = Array.Empty<PackageReference>();
                return true;
            }

            restoredPackages = null;

            var restoreProject = Path.Combine(_tempDir, "restore", "Restore.csproj");
            var restoreProjectDirectory = Directory.CreateDirectory(Path.GetDirectoryName(restoreProject)!);

            File.WriteAllText(Path.Combine(restoreProjectDirectory.FullName, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(restoreProjectDirectory.FullName, "Directory.Build.targets"), "<Project />");

            StringBuilder projectFileBuilder = new();
            projectFileBuilder.Append(@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <NoWarn>$(NoWarn);NU1213</NoWarn>
    </PropertyGroup>
    <ItemGroup>
");

            foreach (var reference in references)
            {
                string itemName = "PackageReference";
                projectFileBuilder.AppendLine($"<{itemName} Include=\"{reference.Name}\" Version=\"{reference.Version}\" />");
            }

            projectFileBuilder.Append(@"
    </ItemGroup>
</Project>
");
            File.WriteAllText(restoreProject, projectFileBuilder.ToString());

            if (ExtraNuGetSources?.Length > 0)
            {
                StringBuilder nugetConfigBuilder = new();
                nugetConfigBuilder.AppendLine($"<configuration>{Environment.NewLine}<packageSources>");

                foreach (ITaskItem source in ExtraNuGetSources)
                {
                    string key = source.ItemSpec;
                    string value = source.GetMetadata("Value");
                    if (string.IsNullOrEmpty(value))
                    {
                        Log.LogError($"ExtraNuGetSource {key} is missing Value metadata");
                        return false;
                    }

                    nugetConfigBuilder.AppendLine($@"<add key=""{key}"" value=""{value}"" />");
                }

                nugetConfigBuilder.AppendLine($"</packageSources>{Environment.NewLine}</configuration>");

                File.WriteAllText(Path.Combine(restoreProjectDirectory.FullName, "nuget.config"), nugetConfigBuilder.ToString());
            }

            string restoreDir = Path.Combine(_tempDir, "nuget-packages");
            if (Directory.Exists(restoreDir))
            {
                Log.LogMessage(MessageImportance.Low, $"Deleting {restoreDir}");
                Directory.Delete(restoreDir, recursive: true);
            }

            Log.LogMessage(MessageImportance.High, $"Restoring packages: {string.Join(", ", references.Select(r => $"{r.Name}/{r.Version}"))}");

            string args = $"restore {restoreProject} /p:RestorePackagesPath={restoreDir}";
            (int exitCode, string output) = Utils.TryRunProcess("dotnet", args, silent: false, debugMessageImportance: MessageImportance.Normal);
            if (exitCode != 0)
            {
                Log.LogError($"Restoring packages returned exit code: {exitCode}. Output:{Environment.NewLine}{output}");
                return false;
            }

            restoredPackages = references.Select(reference =>
            {
                var expectedPath = Path.Combine(restoreDir, reference.Name.ToLower(), reference.Version);
                if (Directory.Exists(expectedPath))
                {
                    reference = reference with { RestoredPath = expectedPath };
                    File.WriteAllText(Path.Combine(reference.RestoredPath, s_stampFileName), string.Empty);
                }
                return reference;
            }).ToArray();

            return true;
        }

        private bool CopyDirectory(string srcDir, string destDir)
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, $"Copying {srcDir} to {destDir}");
                if (Directory.Exists(destDir))
                {
                    Log.LogMessage(MessageImportance.Normal, $"Deleting {destDir}");
                    Directory.Delete(destDir, recursive: true);
                }

                Directory.CreateDirectory(destDir);
                Utils.DirectoryCopy(srcDir, destDir);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed while copying {srcDir} => {destDir}: {ex.Message}");
                if (ex is IOException)
                    return false;

                throw;
            }
        }

        private bool HasMetadata(ITaskItem item, string itemName, string metadataName)
        {
            if (!string.IsNullOrEmpty(item.GetMetadata(metadataName)))
                return true;

            Log.LogError($"{itemName} item ({item.ItemSpec}) is missing Name metadata");
            return false;
        }

        private static bool IsFileNewer(string sourceFile, string stampFile)
        {
            if (!File.Exists(sourceFile))
                return true;

            if (!File.Exists(stampFile))
                return true;

            return File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(stampFile);
        }

        private record PackageReference(string Name, string Version, string? RestoredPath=null);

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
}
