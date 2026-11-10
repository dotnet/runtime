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

    /// <summary>
    /// When set, files are copied to this directory (using their target path) and the
    /// copies are used as ContentOut identities instead of the originals. This prevents
    /// multiple static web assets from pointing to the same original file on disk.
    /// </summary>
    public string VfsOutputPath { get; set; } = string.Empty;

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
                var isRooted = Path.IsPathRooted(identity);
                var contentRoot = isRooted ? Path.GetDirectoryName(identity)! : Path.GetDirectoryName(Path.GetFullPath(identity, MSBuildProjectDirectory))!;
                var outIdentity = CopyToVfsIfNeeded(identity, targetPath, ref contentRoot);

                var outItem = new TaskItem(outIdentity, item.CloneCustomMetadata());
                outItem.SetMetadata("ContentRoot", contentRoot);
                outItem.SetMetadata("TargetPath", Path.Combine("wwwroot", targetPath));
                contentItems.Add(outItem);

                Log.LogMessage(MessageImportance.Low, $"Adding TargetPath '{targetPath}' and ContentRoot '{contentRoot}' for Identity '{outIdentity}'.");
                continue;
            }

            // Case 2: use Identity when Link is empty
            if (string.IsNullOrEmpty(link))
            {
                var isRooted = Path.IsPathRooted(identity);
                targetPath = isRooted ? Path.GetFileName(identity) : identity;
                var contentRoot = isRooted ? Path.GetDirectoryName(identity)! : Path.GetDirectoryName(Path.GetFullPath(identity, MSBuildProjectDirectory))!;

                // Add TargetPath to WasmFilesToIncludeInFileSystem
                var wasmVfsFile = new TaskItem(targetPath);
                wasmFiles.Add(wasmVfsFile);
                if (isRooted && identity.StartsWith(MSBuildProjectDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    var wasmVfsTargetPath = Path.GetRelativePath(MSBuildProjectDirectory, identity);
                    if (!string.IsNullOrEmpty(wasmVfsTargetPath))
                        wasmVfsFile.SetMetadata("TargetPath", wasmVfsTargetPath);
                }

                var outIdentity = CopyToVfsIfNeeded(identity, targetPath, ref contentRoot);

                var outItem = new TaskItem(outIdentity, item.CloneCustomMetadata());
                outItem.SetMetadata("ContentRoot", contentRoot);
                outItem.SetMetadata("TargetPath", Path.Combine("wwwroot", targetPath));
                contentItems.Add(outItem);

                Log.LogMessage(MessageImportance.Low, $"Adding TargetPath '{targetPath}' and ContentRoot '{contentRoot}' for Identity '{outIdentity}'.");
                continue;
            }

            // Case 3: update Link to point to wwwroot
            if (!string.IsNullOrEmpty(link) && !link.StartsWith("wwwroot"))
            {
                var isRooted = Path.IsPathRooted(identity);
                var contentRoot = isRooted ? Path.GetDirectoryName(identity)! : Path.GetDirectoryName(Path.GetFullPath(identity, MSBuildProjectDirectory))!;

                // Add Link to WasmFilesToIncludeInFileSystem
                wasmFiles.Add(new TaskItem(link));

                var outIdentity = CopyToVfsIfNeeded(identity, link, ref contentRoot);

                var outItem = new TaskItem(outIdentity, item.CloneCustomMetadata());
                outItem.SetMetadata("ContentRoot", contentRoot);
                outItem.SetMetadata("Link", Path.Combine("wwwroot", link));
                contentItems.Add(outItem);

                Log.LogMessage(MessageImportance.Low, $"Adding Link '{link}' and ContentRoot '{contentRoot}' for Identity '{outIdentity}'.");
                continue;
            }
        }

        WasmFilesToIncludeInFileSystem = wasmFiles.ToArray();
        ContentOut = contentItems.ToArray();

        Log.LogMessage(MessageImportance.Low, $"Produced {WasmFilesToIncludeInFileSystem.Length} VFS files and {ContentOut.Length} content items.");

        return !Log.HasLoggedErrors;
    }

    /// <summary>
    /// If <see cref="VfsOutputPath"/> is set, copies <paramref name="sourceFile"/> to
    /// VfsOutputPath/<paramref name="relativePath"/> and updates <paramref name="contentRoot"/>
    /// to point at the copy's directory. Returns the path to use as the output item Identity.
    /// </summary>
    private string CopyToVfsIfNeeded(string sourceFile, string relativePath, ref string contentRoot)
    {
        if (string.IsNullOrEmpty(VfsOutputPath))
            return sourceFile;

        // Normalize backslash separators (MSBuild metadata may use '\' even on Linux)
        relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
        var vfsFilePath = Path.Combine(VfsOutputPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(vfsFilePath)!);
        File.Copy(sourceFile, vfsFilePath, overwrite: true);
        contentRoot = Path.GetDirectoryName(vfsFilePath)!;
        return vfsFilePath;
    }
}
