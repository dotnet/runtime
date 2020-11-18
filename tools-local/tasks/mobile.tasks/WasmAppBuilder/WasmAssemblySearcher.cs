// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class WasmAssemblySearcher : Task
{
    [Required]
    public string? MainAssembly { get; set; }

    // If true, continue when a referenced assembly cannot be found.
    // If false, throw an exception.
    public bool SkipMissingAssemblies { get; set; }

    // Either one of these two need to be set
    public ITaskItem[]? AssemblySearchPaths { get; set; }
    public ITaskItem[]? Assemblies { get; set; }
    public ITaskItem[]? ExtraAssemblies { get; set; }
    public int DebugLevel { get; set; }

    // The set of assemblies the app will use
    [Output]
    public ITaskItem[]? ReferencedAssemblies { get; set; }

    private SortedDictionary<string, Assembly>? _assemblies;
    private Resolver? _resolver;

    public override bool Execute ()
    {
        if (AssemblySearchPaths == null && Assemblies == null)
            throw new ArgumentException("Either the AssemblySearchPaths or the Assemblies property needs to be set.");

        var paths = new List<string>();
        _assemblies = new SortedDictionary<string, Assembly>();

        if (AssemblySearchPaths != null)
        {
            // Collect and load assemblies used by the app
            foreach (var v in AssemblySearchPaths!)
            {
                var dir = v.ItemSpec;
                if (!Directory.Exists(dir))
                    throw new ArgumentException($"Directory '{dir}' doesn't exist or not a directory.");
                paths.Add(dir);
            }
            _resolver = new Resolver(paths);
            var mlc = new MetadataLoadContext(_resolver, "System.Private.CoreLib");

            var mainAssembly = mlc.LoadFromAssemblyPath(MainAssembly);
            Add(mlc, mainAssembly);

            if (ExtraAssemblies != null)
            {
                foreach (var item in ExtraAssemblies)
                {
                    try
                    {
                        var refAssembly = mlc.LoadFromAssemblyPath(item.ItemSpec);
                        Add(mlc, refAssembly);
                    }
                    catch (System.IO.FileLoadException)
                    {
                        if (!SkipMissingAssemblies)
                            throw;
                    }
                }
            }
        }
        else
        {
            string corelibPath = string.Empty;
            string runtimeSourceDir = string.Empty;
            foreach (var v in Assemblies!)
            {
                if (v.ItemSpec.EndsWith ("System.Private.CoreLib.dll"))
                    corelibPath = Path.GetDirectoryName (v.ItemSpec)!;
            }
            runtimeSourceDir = corelibPath!;
            _resolver = new Resolver(new List<string>() { corelibPath });
            var mlc = new MetadataLoadContext(_resolver, "System.Private.CoreLib");

            foreach (var v in Assemblies!)
            {
                var assembly = mlc.LoadFromAssemblyPath(v.ItemSpec);
                Add(mlc, assembly);
            }
        }

        ReferencedAssemblies = GetOutputReferencedAssemblies();

        return true;
    }

    private ITaskItem[] GetOutputReferencedAssemblies()
    {
        int count = 0;
        ITaskItem[] items = new TaskItem[_assemblies!.Count];

        foreach (var asm in _assemblies)
        {
            items[count++] = new TaskItem(asm.Value.Location);
        }

        return items;
    }

    private void Add(MetadataLoadContext mlc, Assembly assembly)
    {
        if (_assemblies!.ContainsKey(assembly.GetName().Name!))
            return;
        _assemblies![assembly.GetName().Name!] = assembly;
        foreach (var aname in assembly.GetReferencedAssemblies())
        {
            try
            {
                Assembly refAssembly = mlc.LoadFromAssemblyName(aname);
                Add(mlc, refAssembly);
            }
            catch (FileNotFoundException)
            {
            }
        }
    }
}

internal class Resolver : MetadataAssemblyResolver
{
    private List<string> _searchPaths;

    public Resolver(List<string> searchPaths)
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
