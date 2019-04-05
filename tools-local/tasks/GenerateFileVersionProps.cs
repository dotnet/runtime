// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class GenerateFileVersionProps : BuildTask
    {
        private const string PlatformManifestsItem = "PackageConflictPlatformManifests";
        private const string PreferredPackagesProperty = "PackageConflictPreferredPackages";
        private static readonly Version ZeroVersion = new Version(0, 0, 0, 0);


        [Required]
        public ITaskItem[] Files { get; set; }

        [Required]
        public string PackageId { get; set; }

        [Required]
        public string PackageVersion { get; set; }

        [Required]
        public string PlatformManifestFile { get; set; }

        [Required]
        public string PropsFile { get; set; }

        [Required]
        public string PreferredPackages { get; set; }

        /// <summary>
        /// The task normally enforces that all DLL and EXE files have a non-0.0.0.0 FileVersion.
        /// This flag disables the check.
        /// </summary>
        public bool PermitDllAndExeFilesLackingFileVersion { get; set; }

        public override bool Execute()
        {
            var fileVersions = new Dictionary<string, FileVersionData>(StringComparer.OrdinalIgnoreCase);
            foreach(var file in Files)
            {
                var targetPath = file.GetMetadata("TargetPath");

                if (!targetPath.StartsWith("runtimes/"))
                {
                    continue;
                }

                if (file.GetMetadata("IsSymbolFile").Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file.ItemSpec);

                var current = GetFileVersionData(file);

                FileVersionData existing;

                if (fileVersions.TryGetValue(fileName, out existing))
                {
                    if (current.AssemblyVersion != null)
                    {
                        if (existing.AssemblyVersion == null)
                        {
                            fileVersions[fileName] = current;
                            continue;
                        }
                        else if (current.AssemblyVersion != existing.AssemblyVersion)
                        {
                            if (current.AssemblyVersion > existing.AssemblyVersion)
                            {
                                fileVersions[fileName] = current;
                            }
                            continue;
                        }
                    }

                    if (current.FileVersion != null && 
                        existing.FileVersion != null)
                    {
                        if (current.FileVersion > existing.FileVersion)
                        {
                            fileVersions[fileName] = current;
                        }
                    }
                }
                else
                {
                    fileVersions[fileName] = current;
                }
            }

            // Check for versionless files after all duplicate filenames are resolved, rather than
            // logging errors immediately upon encountering a versionless file. There may be
            // duplicate filenames where only one has a version, and this is ok. The highest version
            // is used.
            if (!PermitDllAndExeFilesLackingFileVersion)
            {
                var versionlessFiles = fileVersions
                    .Where(p =>
                        p.Key.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        p.Key.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    .Where(p => (p.Value.FileVersion ?? ZeroVersion) == ZeroVersion)
                    .Select(p => p.Value.File.ItemSpec)
                    .ToArray();

                if (versionlessFiles.Any())
                {
                    Log.LogError(
                        $"Missing FileVersion in {versionlessFiles.Length} shared framework files:" +
                        string.Concat(versionlessFiles.Select(f => Environment.NewLine + f)));
                }
            }

            var props = ProjectRootElement.Create();
            var itemGroup = props.AddItemGroup();
            // set the platform manifest when the platform is not being published as part of the app
            itemGroup.Condition = "'$(RuntimeIdentifier)' == '' or '$(SelfContained)' != 'true'";

            var manifestFileName = Path.GetFileName(PlatformManifestFile);
            itemGroup.AddItem(PlatformManifestsItem, $"$(MSBuildThisFileDirectory){manifestFileName}");

            Directory.CreateDirectory(Path.GetDirectoryName(PlatformManifestFile));
            using (var manifestWriter = File.CreateText(PlatformManifestFile))
            {
                foreach (var fileData in fileVersions)
                {
                    var name = fileData.Key;
                    var versions = fileData.Value;
                    var assemblyVersion = versions.AssemblyVersion?.ToString() ?? String.Empty;
                    var fileVersion = versions.FileVersion?.ToString() ?? String.Empty;

                    manifestWriter.WriteLine($"{name}|{PackageId}|{assemblyVersion}|{fileVersion}");
                }
            }

            var propertyGroup = props.AddPropertyGroup();
            propertyGroup.AddProperty(PreferredPackagesProperty, PreferredPackages);

            var versionPropertyName = $"_{PackageId.Replace(".", "_")}_Version";
            propertyGroup.AddProperty(versionPropertyName, PackageVersion);

            props.Save(PropsFile);

            return !Log.HasLoggedErrors;
        }

        FileVersionData GetFileVersionData(ITaskItem file)
        {
            var filePath = file.GetMetadata("FullPath");

            if (File.Exists(filePath))
            {
                return new FileVersionData()
                {
                    AssemblyVersion = FileUtilities.GetAssemblyName(filePath)?.Version,
                    FileVersion = FileUtilities.GetFileVersion(filePath),
                    File = file
                };
            }
            else
            {
                // allow for the item to specify version directly
                Version assemblyVersion, fileVersion;

                Version.TryParse(file.GetMetadata("AssemblyVersion"), out assemblyVersion);
                Version.TryParse(file.GetMetadata("FileVersion"), out fileVersion);

                if (fileVersion == null)
                {
                    // FileVersionInfo will return 0.0.0.0 if a file doesn't have a version.
                    // match that behavior
                    fileVersion = ZeroVersion;
                }

                return new FileVersionData()
                {
                    AssemblyVersion = assemblyVersion,
                    FileVersion = fileVersion,
                    File = file
                };
            }
        }

        class FileVersionData
        {
            public Version AssemblyVersion { get; set; }
            public Version FileVersion { get; set; }
            public ITaskItem File { get; set; }
        }
    }
}
