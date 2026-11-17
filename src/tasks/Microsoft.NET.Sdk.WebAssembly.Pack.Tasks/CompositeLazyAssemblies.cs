// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly;

public sealed class GetCompositeLazyExclusions : Task
{
    public ITaskItem[] LazyAssemblies { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public ITaskItem[] Exclusions { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        var exclusions = new List<ITaskItem>(LazyAssemblies.Length);
        foreach (ITaskItem assembly in LazyAssemblies)
        {
            exclusions.Add(new TaskItem(CompositeLazyAssemblyNames.GetSimpleName(assembly.ItemSpec) + ".dll"));
        }

        Exclusions = exclusions.ToArray();
        return true;
    }
}

public sealed class ValidateCompositeLazyReferences : Task
{
    public ITaskItem[] LazyAssemblies { get; set; } = Array.Empty<ITaskItem>();
    public ITaskItem[] CompositeInputs { get; set; } = Array.Empty<ITaskItem>();
    public ITaskItem[] CompileList { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        var requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ITaskItem lazy in LazyAssemblies)
        {
            requested.Add(CompositeLazyAssemblyNames.GetSimpleName(lazy.ItemSpec));
        }

        if (requested.Count == 0)
        {
            return true;
        }

        var compiled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lazyAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ITaskItem input in CompileList)
        {
            string name = Path.GetFileNameWithoutExtension(input.ItemSpec);
            if (!requested.Contains(name) || input.GetMetadata("CreateCompositeImage") == "true")
            {
                continue;
            }

            if (string.IsNullOrEmpty(input.GetMetadata("OutputR2RImage")))
            {
                Log.LogError($"Lazy-loaded assembly '{input.ItemSpec}' has no separate ReadyToRun output.");
                continue;
            }

            try
            {
                using FileStream stream = File.OpenRead(input.ItemSpec);
                using PEReader pe = new(stream);
                MetadataReader metadata = pe.GetMetadataReader();
                lazyAssemblyNames.Add(metadata.GetString(metadata.GetAssemblyDefinition().Name));
                compiled.Add(name);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException)
            {
                Log.LogError($"Cannot read lazy-loaded assembly '{input.ItemSpec}': {ex.Message}");
            }
        }

        foreach (string name in requested)
        {
            if (!compiled.Contains(name))
            {
                Log.LogError($"Lazy-loaded assembly '{name}' is not scheduled for a separate ReadyToRun compilation. Ensure it is present in the published closure and contains compilable IL.");
            }
        }

        foreach (ITaskItem input in CompositeInputs)
        {
            try
            {
                using FileStream stream = File.OpenRead(input.ItemSpec);
                using PEReader pe = new(stream);
                MetadataReader metadata = pe.GetMetadataReader();
                foreach (AssemblyReferenceHandle handle in metadata.AssemblyReferences)
                {
                    string reference = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
                    if (lazyAssemblyNames.Contains(reference))
                    {
                        Log.LogError($"Composite assembly '{input.ItemSpec}' directly references lazy-loaded assembly '{reference}'. Remove the direct reference or do not mark '{reference}' for lazy loading.");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException)
            {
                Log.LogError($"Cannot inspect composite assembly '{input.ItemSpec}': {ex.Message}");
            }
        }

        return !Log.HasLoggedErrors;
    }
}

internal static class CompositeLazyAssemblyNames
{
    public static string GetSimpleName(string path)
    {
        string name = Path.GetFileName(path);
        string extension = Path.GetExtension(name);
        if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".wasm", StringComparison.OrdinalIgnoreCase))
        {
            return name.Substring(0, name.Length - extension.Length);
        }

        return name;
    }
}
