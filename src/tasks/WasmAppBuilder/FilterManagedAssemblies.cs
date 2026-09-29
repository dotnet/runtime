// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Narrows a bundle's files down to the managed assemblies the portable call-helpers generator
/// can scan.
/// An app bundle legitimately carries native payloads named <c>.dll</c> - per-architecture content
/// shipped by a NuGet package is the usual case - and the tools that read managed metadata cannot
/// be handed those. It can also carry several managed files sharing a simple name, satellite
/// assemblies for different cultures being the common case, and the type system holds a single
/// module per simple name, so only the first of those is worth scanning.
/// </summary>
public class FilterManagedAssemblies : Task
{
    [Required, NotNull]
    public ITaskItem[]? Assemblies { get; set; }

    [Output]
    public ITaskItem[] ManagedAssemblies { get; private set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        List<ITaskItem> managedAssemblies = new(Assemblies.Length);
        Dictionary<string, string> claimedSimpleNames = new(Assemblies.Length, StringComparer.OrdinalIgnoreCase);

        foreach (ITaskItem assembly in Assemblies)
        {
            string path = assembly.GetMetadata("FullPath");

            if (!File.Exists(path))
            {
                Log.LogError($"Cannot find assembly '{path}'.");
                continue;
            }

            bool isManaged;
            try
            {
                isManaged = Utils.IsManagedAssembly(path);
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to read assembly '{path}': {ex.Message}");
                continue;
            }

            if (!isManaged)
            {
                Log.LogMessage(MessageImportance.Low, $"Skipping unmanaged {path}.");
                continue;
            }

            // Unmanaged files are already gone, so a native payload can never claim a simple name
            // ahead of the managed assembly that shares it.
            string simpleName = Path.GetFileNameWithoutExtension(path);
            if (claimedSimpleNames.TryGetValue(simpleName, out string? claimedPath))
            {
                Log.LogMessage(MessageImportance.Low, $"Skipping {path}, '{simpleName}' is already provided by {claimedPath}.");
                continue;
            }

            claimedSimpleNames.Add(simpleName, path);
            managedAssemblies.Add(assembly);
        }

        ManagedAssemblies = managedAssemblies.ToArray();

        return !Log.HasLoggedErrors;
    }
}
