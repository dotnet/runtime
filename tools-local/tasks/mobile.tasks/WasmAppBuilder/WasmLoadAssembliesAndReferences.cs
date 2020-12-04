// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class WasmLoadAssembliesAndReferences : Task
{
    [Required]
    public string[]? Assemblies { get; set; }

    // Either one of these two need to be set
    public string[]? AssemblySearchPaths { get; set; }

    // If true, continue when a referenced assembly cannot be found.
    // If false, throw an exception.
    public bool SkipMissingAssemblies { get; set; }

    // The set of assemblies the app will use
    [Output]
    public string[]? ReferencedAssemblies { get; private set; }

    private SortedDictionary<string, Assembly> _assemblies = new SortedDictionary<string, Assembly>();

    public override bool Execute ()
    {
        if (Assemblies == null)
        {
            Log.LogError("The Assemblies property needs to be set to the assemblies being resolved");
            return false;
        }

        SearchPathsAssemblyResolver? _resolver;

        if (AssemblySearchPaths != null)
        {
            foreach (var path in AssemblySearchPaths)
            {
                if (!Directory.Exists(path))
                {
                    Log.LogError($"Directory '{path}' in AssemblySearchPaths does not exist or is not a directory.");
                    return false;
                }
            }
            _resolver = new SearchPathsAssemblyResolver(AssemblySearchPaths);
        }
        else
        {
            string? corelibPath = Path.GetDirectoryName(Assemblies!.FirstOrDefault(asm => asm.EndsWith("System.Private.CoreLib.dll")));
            if (corelibPath == null)
            {
                Log.LogError("Could not find 'System.Private.CoreLib.dll' within Assemblies.");
                return false;
            }
            _resolver = new SearchPathsAssemblyResolver(new string[] { corelibPath });
        }
        var mlc = new MetadataLoadContext(_resolver, "System.Private.CoreLib");

        foreach (var asm in Assemblies)
        {
            var asmFullPath = Path.GetFullPath(asm);
            try
            {
                var refAssembly = mlc.LoadFromAssemblyPath(asmFullPath);
                AddAssemblyAndReferences(mlc, refAssembly);
            }
            catch (Exception ex) when (ex is FileLoadException || ex is BadImageFormatException || ex is FileNotFoundException)
            {
                if (SkipMissingAssemblies)
                    Log.LogMessage(MessageImportance.Low, $"Loading extra assembly '{asm}' failed with {ex}. Skipping");
                else
                {
                    Log.LogError($"Failed to load assembly from ExtraAssemblies '{asm}': {ex}");
                    return false;
                }
            }
        }

        ReferencedAssemblies = _assemblies.Values.Select(asm => asm.Location).ToArray();

        return !Log.HasLoggedErrors;
    }

    private void AddAssemblyAndReferences(MetadataLoadContext mlc, Assembly assembly)
    {
        if (_assemblies!.ContainsKey(assembly.GetName().Name!))
            return;
        _assemblies![assembly.GetName().Name!] = assembly;
        foreach (var aname in assembly.GetReferencedAssemblies())
        {
            try
            {
                Assembly refAssembly = mlc.LoadFromAssemblyName(aname);
                AddAssemblyAndReferences(mlc, refAssembly);
            }
            catch (FileNotFoundException)
            {
                if (SkipMissingAssemblies)
                    Log.LogMessage(MessageImportance.Low, $"Loading extra assembly '{aname.Name}' failed with FileNotFoundException. Skipping");
            }
        }
    }
}

internal class SearchPathsAssemblyResolver : MetadataAssemblyResolver
{
    private readonly string[] _searchPaths;

    public SearchPathsAssemblyResolver(string[] searchPaths)
    {
        _searchPaths = searchPaths;
    }

    public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        foreach (var dir in _searchPaths)
        {
            var path = Path.Combine(dir, name + ".dll");
            if (File.Exists(path))
            {
                return context.LoadFromAssemblyPath(path);
            }
        }
        return null;
    }
}
