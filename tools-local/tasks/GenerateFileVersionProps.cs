// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using System.IO;
using System.Collections.Generic;
using Microsoft.Build.Construction;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class GenerateFileVersionProps : BuildTask
    {
        const string PlatformManifestsItem = "PackageConflictPlatformManifests";
        const string PreferredPackagesProperty = "PackageConflictPreferredPackages";

        [Required]
        public ITaskItem[] Files { get; set; }

        [Required]
        public string PackageId { get; set; }

        [Required]
        public string PlatformManifestFile { get; set; }

        [Required]
        public string PropsFile { get; set; }

        [Required]
        public string PreferredPackages { get; set; }

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
                    if (current.AssemblyVersion != null &&
                        existing.AssemblyVersion != null &&
                        current.AssemblyVersion != existing.AssemblyVersion)
                    {
                        if (current.AssemblyVersion > existing.AssemblyVersion)
                        {
                            fileVersions[fileName] = current;
                        }
                        continue;
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

            var props = ProjectRootElement.Create();
            var itemGroup = props.AddItemGroup();
            // set the platform manifest when the platform is not being published as part of the app
            itemGroup.Condition = "'$(RuntimeIdentifier)' == '' or '$(SelfContained)' != 'true'";

            var manifestFileName = Path.GetFileName(PlatformManifestFile);
            itemGroup.AddItem(PlatformManifestsItem, $"$(MSBuildThisFileDirectory){manifestFileName}");

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
                    AssemblyVersion = FileUtilities.TryGetAssemblyVersion(filePath),
                    FileVersion = FileUtilities.GetFileVersion(filePath)
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
                    fileVersion = new Version(0, 0, 0, 0);
                }

                return new FileVersionData()
                {
                    AssemblyVersion = assemblyVersion,
                    FileVersion = fileVersion
                };
            }
        }

        class FileVersionData
        {
            public Version AssemblyVersion { get; set; }
            public Version FileVersion { get; set; }
        }
    }
}
