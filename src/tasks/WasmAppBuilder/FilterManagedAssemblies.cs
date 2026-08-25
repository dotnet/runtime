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
/// Narrows a bundle's files down to the managed assemblies.
/// An app bundle legitimately carries native payloads named <c>.dll</c> - per-architecture content
/// shipped by a NuGet package is the usual case - and the tools that read managed metadata cannot
/// be handed those.
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

            managedAssemblies.Add(assembly);
        }

        ManagedAssemblies = managedAssemblies.ToArray();

        return !Log.HasLoggedErrors;
    }
}
