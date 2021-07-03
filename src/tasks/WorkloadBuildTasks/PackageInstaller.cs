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
        private string _nugetConfigContents;
        private TaskLoggingHelper _logger;

        public PackageInstaller(string nugetConfigContents, TaskLoggingHelper logger)
        {
            _nugetConfigContents = nugetConfigContents;
            _logger = logger;
        }

        public bool Install(bool errorOnMissing=true, params PackageReference[] references)
        {
            if (!references.Any())
                return true;

            // Restore packages

            var restoreProjectPath = Path.Combine(_tempDir, "restore", "Restore.csproj");

            var restoreProjectDirectory = Directory.CreateDirectory(Path.GetDirectoryName(restoreProjectPath)!);
            File.WriteAllText(Path.Combine(restoreProjectDirectory.FullName, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(restoreProjectDirectory.FullName, "Directory.Build.targets"), "<Project />");
            File.WriteAllText(restoreProjectPath, GenerateProject(references));
            File.WriteAllText(Path.Combine(restoreProjectDirectory.FullName, "nuget.config"), _nugetConfigContents);

            string restoreDir = Path.Combine(_tempDir, "nuget-packages");
            if (Directory.Exists(restoreDir))
            {
                _logger.LogMessage(MessageImportance.Low, $"Deleting {restoreDir}");
                Directory.Delete(restoreDir, recursive: true);
            }

            _logger.LogMessage(MessageImportance.Low, $"Restoring packages: {string.Join(", ", references.Select(r => $"{r.Name}/{r.Version}"))}");

            string args = $"restore {restoreProjectPath} /p:RestorePackagesPath={restoreDir}";
            (int exitCode, string output) = Utils.TryRunProcess("dotnet", args, silent: false, debugMessageImportance: MessageImportance.Low);
            if (exitCode != 0)
            {
                //FIXME: umm.. should this also just warn?
                _logger.LogError($"Restoring packages failed with exit code: {exitCode}. Output:{Environment.NewLine}{output}");
                return false;
            }

            return LayoutPacksFromRestoredNuGets(references, restoreDir, errorOnMissing);
        }

        private bool LayoutPacksFromRestoredNuGets(IEnumerable<PackageReference> restored, string restoreDir, bool errorOnMissing)
        {
            foreach (var pkgRef in restored)
            {
                var source = Path.Combine(restoreDir, pkgRef.Name.ToLower(), pkgRef.Version);
                if (!Directory.Exists(source))
                {
                    string msg = $"Failed to restore {pkgRef.Name}/{pkgRef.Version} (could not find {source})";
                    if (errorOnMissing)
                    {
                        _logger.LogError(msg);
                        return false;
                    }
                    else
                    {
                        _logger.LogWarning(msg);
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(pkgRef.relativeSourceDir))
                    source = Path.Combine(source, pkgRef.relativeSourceDir);

                if (!CopyDirectoryAfresh(source, pkgRef.OutputDir))
                    return false;
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
    }
}
