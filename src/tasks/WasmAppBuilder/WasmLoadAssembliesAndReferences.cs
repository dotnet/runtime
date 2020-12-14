// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class WasmLoadAssembliesAndReferences : Task
{
    [Required]
    [NotNull]
    public string[]? Assemblies { get; set; }

    [Required]
    [NotNull]
    public string[]? AssemblySearchPaths { get; set; }

    // If true, continue when a referenced assembly cannot be found.
    public bool SkipMissingAssemblies { get; set; }

    // The set of assemblies the app will use
    [Output]
    public string[]? ReferencedAssemblies { get; private set; }

    private SortedDictionary<string, Assembly> _assemblies = new();

    public override bool Execute ()
    {
        string? badPath = AssemblySearchPaths.FirstOrDefault(path => !Directory.Exists(path));
        if (badPath != null)
        {
            Log.LogError($"Directory '{badPath}' in AssemblySearchPaths does not exist or is not a directory.");
            return false;
        }

        SearchPathsAssemblyResolver resolver = new(AssemblySearchPaths);
        MetadataLoadContext mlc = new(resolver, "System.Private.CoreLib");
        foreach (var asm in Assemblies)
        {
            var asmFullPath = Path.GetFullPath(asm);
            if (!File.Exists(asmFullPath))
            {
                Log.LogError($"Could not find assembly '{asmFullPath}'");
                return false;
            }

            var refAssembly = mlc.LoadFromAssemblyPath(asmFullPath);
            if (!AddAssemblyAndReferences(mlc, refAssembly))
                return !Log.HasLoggedErrors;
        }

        ReferencedAssemblies = _assemblies.Values.Select(asm => asm.Location).ToArray();
        return !Log.HasLoggedErrors;
    }

    private bool AddAssemblyAndReferences(MetadataLoadContext mlc, Assembly assembly)
    {
        if (_assemblies.ContainsKey(assembly.GetName().Name!))
            return true;

        _assemblies[assembly.GetName().Name!] = assembly;
        foreach (var aname in assembly.GetReferencedAssemblies())
        {
            try
            {
                Assembly refAssembly = mlc.LoadFromAssemblyName(aname);
                if (!AddAssemblyAndReferences(mlc, refAssembly))
                    return false;
            }
            catch (Exception ex) when (ex is FileLoadException || ex is BadImageFormatException || ex is FileNotFoundException)
            {
                if (SkipMissingAssemblies)
                {
                    Log.LogWarning($"Loading assembly reference '{aname}' for '{assembly.GetName()}' failed: {ex.Message} Skipping.");
                }
                else
                {
                    Log.LogError($"Failed to load assembly reference '{aname}' for '{assembly.GetName()}': {ex.Message}");
                    return false;
                }
            }
        }

        return true;
    }
}

internal class SearchPathsAssemblyResolver : MetadataAssemblyResolver
{
    private readonly string[] _searchPaths;

    public SearchPathsAssemblyResolver(string[] searchPaths) => _searchPaths = searchPaths;

    public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
    {
        string? name = assemblyName.Name;
        foreach (var dir in _searchPaths)
        {
            string path = Path.Combine(dir, name + ".dll");
            if (File.Exists(path))
                return context.LoadFromAssemblyPath(path);
        }
        return null;
    }
}
