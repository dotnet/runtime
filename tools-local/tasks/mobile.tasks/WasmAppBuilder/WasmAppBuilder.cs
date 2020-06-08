// -*- indent-tabs-mode: nil -*-
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class WasmAppBuilder : Task
{
    // FIXME: Document

    [Required]
    public string? AppDir { get; set; }
    [Required]
    public string? MicrosoftNetCoreAppRuntimePackDir { get; set; }
    [Required]
    public string? MainAssembly { get; set; }
    [Required]
    public string? MainJS { get; set; }
    [Required]
    public ITaskItem[]? AssemblySearchPaths { get; set; }
    public ITaskItem[]? ExtraAssemblies { get; set; }
    public ITaskItem[]? ExtraFiles { get; set; }

    Dictionary<string, Assembly>? _assemblies;
    Resolver? _resolver;

    public override bool Execute ()
    {
        if (!File.Exists(MainAssembly))
            throw new ArgumentException($"File MainAssembly='{MainAssembly}' doesn't exist.");
        if (!File.Exists(MainJS))
            throw new ArgumentException($"File MainJS='{MainJS}' doesn't exist.");

        var paths = new List<string>();
        _assemblies = new Dictionary<string, Assembly>();

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

        var mainAssembly = mlc.LoadFromAssemblyPath (MainAssembly);
        Add(mlc, mainAssembly);

        if (ExtraAssemblies != null)
        {
            foreach (var item in ExtraAssemblies)
            {
                var refAssembly = mlc.LoadFromAssemblyPath(item.ItemSpec);
                Add(mlc, refAssembly);
            }
        }

        // Create app
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(Path.Join(AppDir, "managed"));
        foreach (var assembly in _assemblies!.Values)
            File.Copy(assembly.Location, Path.Join(AppDir, "managed", Path.GetFileName(assembly.Location)), true);
        foreach (var f in new string[] { "dotnet.wasm", "dotnet.js" })
            File.Copy(Path.Join (MicrosoftNetCoreAppRuntimePackDir, "native", f), Path.Join(AppDir, f), true);
        File.Copy(MainJS!, Path.Join(AppDir, "runtime.js"),  true);

        if (ExtraFiles != null)
        {
            foreach (var item in ExtraFiles)
                File.Copy(item.ItemSpec, Path.Join(AppDir, Path.GetFileName(item.ItemSpec)),  true);
        }

        using (var sw = File.CreateText(Path.Join(AppDir, "mono-config.js")))
        {
            sw.WriteLine("config = {");
            sw.WriteLine("\tvfs_prefix: \"managed\",");
            sw.WriteLine("\tdeploy_prefix: \"managed\",");
            sw.WriteLine("\tenable_debugging: 0,");
            sw.WriteLine("\tfile_list: [");
            foreach (var assembly in _assemblies.Values)
            {
                sw.Write("\"" + Path.GetFileName(assembly.Location) + "\"");
                sw.Write(", ");
            }
            sw.WriteLine ("],");
            sw.WriteLine ("}");
        }

        using (var sw = File.CreateText(Path.Join(AppDir, "run-v8.sh")))
        {
            sw.WriteLine("v8 --expose_wasm runtime.js -- --enable-gc --run " + Path.GetFileName(MainAssembly) + " $*");
        }

        return true;
    }

    private void Add(MetadataLoadContext mlc, Assembly assembly)
    {
        _assemblies![assembly.GetName().Name!] = assembly;
        foreach (var aname in assembly.GetReferencedAssemblies())
        {
            var refAssembly = mlc.LoadFromAssemblyName(aname);
            Add(mlc, refAssembly);
        }
    }
}

class Resolver : MetadataAssemblyResolver
{
    List<String> _searchPaths;

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
