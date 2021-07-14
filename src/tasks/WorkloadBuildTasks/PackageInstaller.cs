// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace Microsoft.Workload.Build.Tasks
{
    internal class PackageInstaller
    {
        private readonly string _tempDir;
        private string _nugetConfigContents;
        private TaskLoggingHelper _logger;
        private string _packagesDir;

        private PackageInstaller(string nugetConfigContents, TaskLoggingHelper logger)
        {
            _nugetConfigContents = nugetConfigContents;

            _logger = logger;
            _tempDir = Path.Combine(Path.GetTempPath(), "install-workload", Path.GetRandomFileName());
            _packagesDir = Path.Combine(_tempDir, "nuget-packages");
        }

        public static bool Install(PackageReference[] references, string nugetConfigContents, TaskLoggingHelper logger, bool stopOnMissing=true)
        {
            if (!references.Any())
                return true;

            return new PackageInstaller(nugetConfigContents, logger)
                        .InstallActual(references, stopOnMissing);
        }

        private bool InstallActual(PackageReference[] references, bool stopOnMissing)
        {
            // Restore packages
            if (Directory.Exists(_packagesDir))
            {
                _logger.LogMessage(MessageImportance.Low, $"Deleting {_packagesDir}");
                Directory.Delete(_packagesDir, recursive: true);
            }

            var projecDir = Path.Combine(_tempDir, "restore");
            var projectPath = Path.Combine(projecDir, "Restore.csproj");

            Directory.CreateDirectory(projecDir);

            File.WriteAllText(Path.Combine(projecDir, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(projecDir, "Directory.Build.targets"), "<Project />");
            File.WriteAllText(projectPath, GenerateProject(references));
            File.WriteAllText(Path.Combine(projecDir, "nuget.config"), _nugetConfigContents);

            _logger.LogMessage(MessageImportance.Low, $"Restoring packages: {string.Join(", ", references.Select(r => $"{r.Name}/{r.Version}"))}");

            string args = $"restore \"{projectPath}\" /p:RestorePackagesPath=\"{_packagesDir}\"";
            (int exitCode, string output) = Utils.TryRunProcess("dotnet", args, silent: false, debugMessageImportance: MessageImportance.Low);
            if (exitCode != 0)
            {
                LogErrorOrWarning($"Restoring packages failed with exit code: {exitCode}. Output:{Environment.NewLine}{output}", stopOnMissing);
                return false;
            }

            IList<(PackageReference, string)> failedToRestore = references
                                                             .Select(r => (r, Path.Combine(_packagesDir, r.Name.ToLower(), r.Version)))
                                                             .Where(tuple => !Directory.Exists(tuple.Item2))
                                                             .ToList();

            if (failedToRestore.Count > 0)
            {
                _logger.LogMessage(MessageImportance.Normal, output);
                foreach ((PackageReference pkgRef, string pkgDir) in failedToRestore)
                    LogErrorOrWarning($"Could not restore {pkgRef.Name}/{pkgRef.Version} (can't find {pkgDir})", stopOnMissing);

                return false;
            }

            return LayoutPackages(references, stopOnMissing);
        }

        private bool LayoutPackages(IEnumerable<PackageReference> references, bool stopOnMissing)
        {
            foreach (var pkgRef in references)
            {
                var source = Path.Combine(_packagesDir, pkgRef.Name.ToLower(), pkgRef.Version, pkgRef.relativeSourceDir);
                if (!Directory.Exists(source))
                {
                    LogErrorOrWarning($"Failed to restore {pkgRef.Name}/{pkgRef.Version} (could not find {source})", stopOnMissing);
                    if (stopOnMissing)
                        return false;
                }
                else
                {
                    if (!CopyDirectoryAfresh(source, pkgRef.OutputDir) && stopOnMissing)
                        return false;
                }
            }

            return true;
        }

        private static string GenerateProject(IEnumerable<PackageReference> references)
        {
            StringBuilder projectFileBuilder = new();
            projectFileBuilder.Append(@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>");

            foreach (var reference in references)
                projectFileBuilder.AppendLine($"<PackageReference Include=\"{reference.Name}\" Version=\"{reference.Version}\" />");

            projectFileBuilder.Append(@"
    </ItemGroup>
</Project>
");

            return projectFileBuilder.ToString();
        }

        private bool CopyDirectoryAfresh(string srcDir, string destDir)
        {
            try
            {
                if (Directory.Exists(destDir))
                {
                    _logger.LogMessage(MessageImportance.Low, $"Deleting {destDir}");
                    Directory.Delete(destDir, recursive: true);
                }

                _logger.LogMessage(MessageImportance.Low, $"Copying {srcDir} to {destDir}");
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

        private void LogErrorOrWarning(string msg, bool stopOnMissing)
        {
            if (stopOnMissing)
                _logger.LogError(msg);
            else
                _logger.LogWarning(msg);
        }
    }
}
