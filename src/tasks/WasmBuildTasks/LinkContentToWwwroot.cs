// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

public class LinkContentToWwwroot : Task
{
    // Inputs
    [Required]
    public ITaskItem[] Content { get; set; } = [];

    [Required]
    public string MSBuildProjectDirectory { get; set; } = string.Empty;

    // Outputs
    [Output]
    public ITaskItem[] WasmFilesToIncludeInFileSystem { get; set; } = [];

    [Output]
    public ITaskItem[] ContentOut { get; set; } = [];

    public override bool Execute()
    {
        var wasmFiles = new List<ITaskItem>();
        var contentItems = new List<ITaskItem>();

        foreach (var item in Content)
        {
            string copyToOutput = item.GetMetadata("CopyToOutputDirectory") ?? string.Empty;
            string targetPath = item.GetMetadata("TargetPath") ?? string.Empty;
            string identity = item.ItemSpec ?? string.Empty;
            string link = item.GetMetadata("Link") ?? string.Empty;

            bool copyPreserveOrAlways = string.Equals(copyToOutput, "PreserveNewest", StringComparison.OrdinalIgnoreCase) || string.Equals(copyToOutput, "Always", StringComparison.OrdinalIgnoreCase);
            if (!copyPreserveOrAlways)
            {
                Log.LogMessage(MessageImportance.Low, $"Skipping item with Identity '{identity}' (TargetPath '{targetPath}', Link '{link}') because CopyToOutputDirectory is not 'PreserveNewest' or 'Always' ('{copyPreserveOrAlways}').");
                continue;
            }

            if (!string.IsNullOrEmpty(link) && item.GetMetadata("BuildReference") == "true" && item.GetMetadata("OriginalItemName") == "WasmAssembliesFinal" && !string.Equals(Path.GetExtension(link), Path.GetExtension(identity), StringComparison.OrdinalIgnoreCase))
            {
                Log.LogMessage(MessageImportance.Low, $"Ignoring Link '{link}' for Identity '{identity}', because it has a different extension and is coming from nested publish (WasmAssembliesFinal).");
                link = string.Empty;
            }

            // Case 1: use TargetPath when present
            if (!string.IsNullOrEmpty(targetPath))
            {
                // Add TargetPath to WasmFilesToIncludeInFileSystem
                wasmFiles.Add(new TaskItem(targetPath));

                // Content: ContentRoot = Identity with TargetPath removed, TargetPath = wwwroot\TargetPath
                var contentRoot = Path.GetDirectoryName(identity);

                var outItem = new TaskItem(identity, item.CloneCustomMetadata());
                outItem.SetMetadata("ContentRoot", contentRoot);
                outItem.SetMetadata("TargetPath", Path.Combine("wwwroot", targetPath));
                contentItems.Add(outItem);

                Log.LogMessage(MessageImportance.Low, $"Adding TargetPath '{targetPath}' and ContentRoot '{contentRoot}' for Identity '{identity}'.");
                continue;
            }

            // Case 2: use Identity when Link is empty
            if (string.IsNullOrEmpty(link))
            {
                var isRooted = Path.IsPathRooted(identity);
                targetPath = isRooted ? Path.GetFileName(identity) : identity;
                var contentRoot = isRooted ? Path.GetDirectoryName(identity) : Path.GetDirectoryName(Path.GetFullPath(identity, MSBuildProjectDirectory));

                // Add TargetPath to WasmFilesToIncludeInFileSystem
                var wasmVfsFile = new TaskItem(targetPath);
                wasmFiles.Add(wasmVfsFile);
                if (isRooted && identity.StartsWith(MSBuildProjectDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    var wasmVfsTargetPath = Path.GetRelativePath(MSBuildProjectDirectory, identity);
                    if (!string.IsNullOrEmpty(wasmVfsTargetPath))
                        wasmVfsFile.SetMetadata("TargetPath", wasmVfsTargetPath);
                }

                var outItem = new TaskItem(identity, item.CloneCustomMetadata());
                outItem.SetMetadata("ContentRoot", contentRoot);
                outItem.SetMetadata("TargetPath", Path.Combine("wwwroot", targetPath));
                contentItems.Add(outItem);

                Log.LogMessage(MessageImportance.Low, $"Adding TargetPath '{targetPath}' and ContentRoot '{contentRoot}' for Identity '{identity}'.");
                continue;
            }

            // Case 3: update Link to point to wwwroot
            if (!string.IsNullOrEmpty(link) && !link.StartsWith("wwwroot"))
            {
                var isRooted = Path.IsPathRooted(identity);
                var contentRoot = isRooted ? Path.GetDirectoryName(identity) : Path.GetDirectoryName(Path.GetFullPath(identity, MSBuildProjectDirectory));

                // Add Link to WasmFilesToIncludeInFileSystem
                wasmFiles.Add(new TaskItem(link));

                var outItem = new TaskItem(identity, item.CloneCustomMetadata());
                outItem.SetMetadata("ContentRoot", contentRoot);
                outItem.SetMetadata("Link", Path.Combine("wwwroot", link));
                contentItems.Add(outItem);

                Log.LogMessage(MessageImportance.Low, $"Adding Link '{link}' and ContentRoot '{contentRoot}' for Identity '{identity}'.");
                continue;
            }
        }

        WasmFilesToIncludeInFileSystem = wasmFiles.ToArray();
        ContentOut = contentItems.ToArray();

        Log.LogMessage(MessageImportance.Low, $"Produced {WasmFilesToIncludeInFileSystem.Length} VFS files and {ContentOut.Length} content items.");

        return !Log.HasLoggedErrors;
    }
}
