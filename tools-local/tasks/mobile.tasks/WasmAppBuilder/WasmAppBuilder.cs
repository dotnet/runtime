// -*- indent-tabs-mode: nil -*-
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
    public ITaskItem[]? FilesToIncludeInFileSystem { get; set; }
    public ITaskItem[]? RemoteSources { get; set; }

    SortedDictionary<string, Assembly>? _assemblies;
    Resolver? _resolver;

    private class WasmAppConfig
    {
        [JsonPropertyName("assembly_root")]
        public string AssemblyRoot { get; set; } = "managed";
        [JsonPropertyName("enable_debugging")]
        public int EnableDebugging { get; set; } = 0;
        [JsonPropertyName("assets")]
        public List<object> Assets { get; } = new List<object>();
        [JsonPropertyName("remote_sources")]
        public List<string> RemoteSources { get; set; } = new List<string>();
    }

    private class AssetEntry {
        protected AssetEntry (string name, string behavior)
        {
            Name = name;
            Behavior = behavior;
        }
        [JsonPropertyName("behavior")]
        public string Behavior { get; init; }
        [JsonPropertyName("name")]
        public string Name { get; init; }
    }

    private class AssemblyEntry : AssetEntry
    {
        public AssemblyEntry(string name) : base(name, "assembly") {}
    }

    private class VfsEntry : AssetEntry {
        public VfsEntry(string name) : base(name, "vfs") {}
        [JsonPropertyName("virtual_path")]
        public string? VirtualPath { get; set; }
    }

    private class IcuData : AssetEntry {
        public IcuData(string name = "icudt.dat") : base(name, "icu") {}
        [JsonPropertyName("load_remote")]
        public bool LoadRemote { get; set; }
    }

    public override bool Execute ()
    {
        if (!File.Exists(MainAssembly))
            throw new ArgumentException($"File MainAssembly='{MainAssembly}' doesn't exist.");
        if (!File.Exists(MainJS))
            throw new ArgumentException($"File MainJS='{MainJS}' doesn't exist.");

        var paths = new List<string>();
        _assemblies = new SortedDictionary<string, Assembly>();

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
                var refAssembly = mlc.LoadFromAssemblyPath(item.ItemSpec);
                Add(mlc, refAssembly);
            }
        }

        var config = new WasmAppConfig ();

        // Create app
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(Path.Join(AppDir, config.AssemblyRoot));
        foreach (var assembly in _assemblies!.Values)
            File.Copy(assembly.Location, Path.Join(AppDir, config.AssemblyRoot, Path.GetFileName(assembly.Location)), true);
        foreach (var f in new string[] { "dotnet.wasm", "dotnet.js", "dotnet.timezones.blat", "icudt.dat" })
            File.Copy(Path.Join (MicrosoftNetCoreAppRuntimePackDir, "native", f), Path.Join(AppDir, f), true);
        File.Copy(MainJS!, Path.Join(AppDir, "runtime.js"),  true);

        foreach (var assembly in _assemblies.Values)
            config.Assets.Add(new AssemblyEntry (Path.GetFileName(assembly.Location)));

        if (FilesToIncludeInFileSystem != null)
        {
            string supportFilesDir = Path.Join(AppDir, "supportFiles");
            Directory.CreateDirectory(supportFilesDir);

            var i = 0;
            foreach (var item in FilesToIncludeInFileSystem)
            {
                string? targetPath = item.GetMetadata("TargetPath");
                if (string.IsNullOrEmpty(targetPath))
                {
                    targetPath = Path.GetFileName(item.ItemSpec);
                }

                // We normalize paths from `\` to `/` as MSBuild items could use `\`.
                targetPath = targetPath.Replace('\\', '/');

                var generatedFileName = $"{i++}_{Path.GetFileName(item.ItemSpec)}";

                File.Copy(item.ItemSpec, Path.Join(supportFilesDir, generatedFileName), true);

                var asset = new VfsEntry ($"supportFiles/{generatedFileName}") {
                    VirtualPath = targetPath
                };
                config.Assets.Add(asset);
            }
        }

        config.Assets.Add(new IcuData { LoadRemote = RemoteSources?.Length > 0 });
        config.Assets.Add(new VfsEntry ("dotnet.timezones.blat") { VirtualPath = "/usr/share/zoneinfo/"});

        if (RemoteSources?.Length > 0) {
            foreach (var source in RemoteSources)
                if (source != null && source.ItemSpec != null)
                    config.RemoteSources.Add(source.ItemSpec);
        }

        using (var sw = File.CreateText(Path.Join(AppDir, "mono-config.js")))
        {
            var json = JsonSerializer.Serialize (config, new JsonSerializerOptions { WriteIndented = true });
            sw.Write($"config = {json};");
        }

        using (var sw = File.CreateText(Path.Join(AppDir, "run-v8.sh")))
        {
            sw.WriteLine("v8 --expose_wasm runtime.js -- --run " + Path.GetFileName(MainAssembly) + " $*");
        }

        return true;
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
