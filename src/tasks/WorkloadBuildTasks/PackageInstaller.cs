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
    internal class PackageInstaller
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "install-workload", Path.GetRandomFileName());
        private string _localNuGetsPath;
        private ITaskItem[] _extraNuGetSources;
        private TaskLoggingHelper _logger;

        private const string s_stampFileName = ".installed.stamp";

        public PackageInstaller(string localNuGetsPath, ITaskItem[] extraNuGetSources, TaskLoggingHelper logger)
        {
            _localNuGetsPath = localNuGetsPath;
            _extraNuGetSources = extraNuGetSources;
            _logger = logger;
        }

        public bool Install(params PackageReference[] references)
        {
            if (references.Length == 0)
                return true;

            StringBuilder errorBuilder = new();

            if (!TryInstallFromLocalNuGets(references, errorBuilder, out IEnumerable<PackageReference>? remainingForRestore))
            {
                // fatal error
                _logger.LogError($"Failing to install local nugets: {errorBuilder}");
                return false;
            }

            if (!TryInstallViaNuGetRestore(remainingForRestore, errorBuilder, out IEnumerable<PackageReference>? remaining))
            {
                // fatal error
                _logger.LogError($"Failing to install local nugets: {errorBuilder}");
                return false;
            }

            return true;
        }

        private bool TryInstallFromLocalNuGets(IEnumerable<PackageReference> references,
                                               StringBuilder errorBuilder,
                                               [NotNullWhen(true)] out IEnumerable<PackageReference>? remaining)
        {
            remaining = null;
            var allFiles = string.Join($"{Environment.NewLine}  ", Directory.EnumerateFiles(_localNuGetsPath, "*", new EnumerationOptions { RecurseSubdirectories = true }));
            _logger.LogMessage(MessageImportance.Low, $"Files in {_localNuGetsPath}: {allFiles}");

            List<PackageReference> remainingList = new(references.Count());
            foreach (var pkgRef in references)
            {
                var nupkgFileName = $"{pkgRef.Name}.{pkgRef.Version}.nupkg";
                var nupkgPath = Path.Combine(_localNuGetsPath, nupkgFileName);
                if (!File.Exists(nupkgPath))
                {
                    string[] found = Directory.GetFiles(_localNuGetsPath, nupkgFileName, new EnumerationOptions
                                        {
                                            MatchCasing = MatchCasing.CaseInsensitive,
                                            RecurseSubdirectories = true
                                        });

                    if (found.Length == 0)
                    {
                        remainingList.Add(pkgRef);
                        continue;
                    }

                    nupkgPath = found[0];
                    nupkgFileName = Path.GetFileName(nupkgPath);
                }

                // string installedPackDir = Path.Combine(_packsDir!, reference.Name, reference.Version);
                string stampFilePath = Path.Combine(pkgRef.OutputDir, s_stampFileName);
                if (!IsFileNewer(nupkgPath, stampFilePath))
                {
                    _logger.LogMessage(MessageImportance.Normal, $"Skipping {pkgRef.Name}/{pkgRef.Version} as it is already installed in {pkgRef.OutputDir}.{Environment.NewLine}  {nupkgPath} is older than {stampFilePath}");
                    continue;
                }

                if (Directory.Exists(pkgRef.OutputDir))
                {
                    _logger.LogMessage(MessageImportance.Normal, $"Deleting {pkgRef.OutputDir}");
                    Directory.Delete(pkgRef.OutputDir, recursive: true);
                }

                _logger.LogMessage(MessageImportance.High, $"Extracting {nupkgPath} => {pkgRef.OutputDir}");

                if (string.IsNullOrEmpty(pkgRef.relativeSourceDir))
                {
                    ZipFile.ExtractToDirectory(nupkgPath, pkgRef.OutputDir);
                }
                else
                {
                    string tmpUnzipDir = Path.Combine(_tempDir, "tmp-unzip", pkgRef.Name);
                    ZipFile.ExtractToDirectory(nupkgPath, tmpUnzipDir);

                    var sourceDir = Path.Combine(tmpUnzipDir, pkgRef.relativeSourceDir);
                    var targetDir = Path.Combine(pkgRef.OutputDir);
                    if (!CopyDirectoryAfresh(sourceDir, targetDir))
                        return false;
                }

                // Add .nupkg.sha512, so it gets picked up when resolving nugets
                File.WriteAllText(Path.Combine(pkgRef.OutputDir, $"{nupkgFileName}.sha512"), string.Empty);

                File.WriteAllText(stampFilePath, string.Empty);
            }

            remaining = remainingList;
            return true;
        }

        private bool TryInstallViaNuGetRestore(IEnumerable<PackageReference> references, StringBuilder errorBuilder, out IEnumerable<PackageReference>? remaining)
        {
            remaining = SkipInstalledPacks(references);
            if (!remaining.Any())
                return true;

            return TryRestorePackages(remaining, out PackageReference[]? restored) &&
                    LayoutPacksFromRestoredNuGets(restored);

            IEnumerable<PackageReference> SkipInstalledPacks(IEnumerable<PackageReference> candidates)
            {
                List<PackageReference> needed = new List<PackageReference>(candidates.Count());
                foreach (PackageReference pr in candidates)
                {
                    var packStampFile = Path.Combine(pr.OutputDir, s_stampFileName);

                    if (File.Exists(packStampFile))
                    {
                        _logger.LogMessage(MessageImportance.Normal, $"Skipping {pr.Name}/{pr.Version} as it is already installed in {pr.OutputDir}.{Environment.NewLine} {packStampFile} exists.");
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
                        _logger.LogError($"Failed to restore {pkgRef.Name}/{pkgRef.Version}");
                        return false;
                    }

                    var source = pkgRef.RestoredPath;
                    if (!string.IsNullOrEmpty(pkgRef.relativeSourceDir))
                        source = Path.Combine(source, pkgRef.relativeSourceDir);

                    if (!CopyDirectoryAfresh(source, pkgRef.OutputDir))
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

            if (_extraNuGetSources.Length > 0)
            {
                StringBuilder nugetConfigBuilder = new();
                nugetConfigBuilder.AppendLine($"<configuration>{Environment.NewLine}<packageSources>");

                foreach (ITaskItem source in _extraNuGetSources)
                {
                    string key = source.ItemSpec;
                    string value = source.GetMetadata("Value");
                    if (string.IsNullOrEmpty(value))
                    {
                        _logger.LogError($"ExtraNuGetSource {key} is missing Value metadata");
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
                _logger.LogMessage(MessageImportance.Low, $"Deleting {restoreDir}");
                Directory.Delete(restoreDir, recursive: true);
            }

            _logger.LogMessage(MessageImportance.High, $"Restoring packages: {string.Join(", ", references.Select(r => $"{r.Name}/{r.Version}"))}");

            string args = $"restore {restoreProject} /p:RestorePackagesPath={restoreDir}";
            (int exitCode, string output) = Utils.TryRunProcess("dotnet", args, silent: false, debugMessageImportance: MessageImportance.Normal);
            if (exitCode != 0)
            {
                _logger.LogError($"Restoring packages returned exit code: {exitCode}. Output:{Environment.NewLine}{output}");
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

        private bool CopyDirectoryAfresh(string srcDir, string destDir)
        {
            try
            {
                _logger.LogMessage(MessageImportance.Low, $"Copying {srcDir} to {destDir}");
                if (Directory.Exists(destDir))
                {
                    _logger.LogMessage(MessageImportance.Normal, $"Deleting {destDir}");
                    Directory.Delete(destDir, recursive: true);
                }

                Directory.CreateDirectory(destDir);
                Utils.DirectoryCopy(srcDir, destDir);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed while copying {srcDir} => {destDir}: {ex.Message}");
                if (ex is IOException)
                    return false;

                throw;
            }
        }

        private static bool IsFileNewer(string sourceFile, string stampFile)
        {
            if (!File.Exists(sourceFile))
                return true;

            if (!File.Exists(stampFile))
                return true;

            return File.GetLastWriteTimeUtc(sourceFile) > File.GetLastWriteTimeUtc(stampFile);
        }
    }
}
