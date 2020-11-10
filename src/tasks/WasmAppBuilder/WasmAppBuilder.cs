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
    [Required]
    public string? AppDir { get; set; }
    [Required]
    public string? MicrosoftNetCoreAppRuntimePackDir { get; set; }
    [Required]
    public string? MainAssembly { get; set; }
    [Required]
    public string? MainJS { get; set; }

    // full list of ICU data files we produce can be found here:
    // https://github.com/dotnet/icu/tree/maint/maint-67/icu-filters
    public string? IcuDataFileName { get; set; } = "icudt.dat";

    // Either one of these two need to be set
    public ITaskItem[]? AssemblySearchPaths { get; set; }
    public ITaskItem[]? Assemblies { get; set; }
    public int DebugLevel { get; set; }
    public ITaskItem[]? ExtraAssemblies { get; set; }
    public ITaskItem[]? SatelliteAssemblies { get; set; }
    public ITaskItem[]? FilesToIncludeInFileSystem { get; set; }
    public ITaskItem[]? RemoteSources { get; set; }
    public bool InvariantGlobalization { get; set; }

    private List<string>? _assemblies;

    private class WasmAppConfig
    {
        [JsonPropertyName("assembly_root")]
        public string AssemblyRoot { get; set; } = "managed";
        [JsonPropertyName("debug_level")]
        public int DebugLevel { get; set; } = 0;
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

    private class SatelliteAssemblyEntry : AssetEntry
    {
        public SatelliteAssemblyEntry(string name, string culture) : base(name, "resource")
        {
            CultureName = culture;
        }

        [JsonPropertyName("culture")]
        public string CultureName { get; set; }
    }

    private class VfsEntry : AssetEntry {
        public VfsEntry(string name) : base(name, "vfs") {}
        [JsonPropertyName("virtual_path")]
        public string? VirtualPath { get; set; }
    }

    private class IcuData : AssetEntry {
        public IcuData(string name) : base(name, "icu") {}
        [JsonPropertyName("load_remote")]
        public bool LoadRemote { get; set; }
    }

    public override bool Execute ()
    {
        if (!File.Exists(MainAssembly))
            throw new ArgumentException($"File MainAssembly='{MainAssembly}' doesn't exist.");
        if (!File.Exists(MainJS))
            throw new ArgumentException($"File MainJS='{MainJS}' doesn't exist.");
        if (!InvariantGlobalization && string.IsNullOrEmpty(IcuDataFileName))
            throw new ArgumentException("IcuDataFileName property shouldn't be empty if InvariantGlobalization=false");

        _assemblies = new List<string>();
        var runtimeSourceDir = Path.Join (MicrosoftNetCoreAppRuntimePackDir, "native");

        if (Assemblies != null)
        {
            foreach (var asm in Assemblies!)
            {
                if (!_assemblies.Contains(asm.ItemSpec))
                {
                    _assemblies.Add(asm.ItemSpec);
                }
            }
        }
        if (MainAssembly != null)
        {
            if (!_assemblies.Contains(MainAssembly))
            {
                _assemblies.Add(MainAssembly);
            }
        }
        if (ExtraAssemblies != null)
        {
            foreach (var item in ExtraAssemblies)
            {
                if (!_assemblies.Contains(item.ItemSpec))
                {
                    _assemblies.Add(item.ItemSpec);
                }
            }
        }

        var config = new WasmAppConfig ();

        // Create app
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(Path.Join(AppDir, config.AssemblyRoot));
        foreach (var assembly in _assemblies!) {
            File.Copy(assembly, Path.Join(AppDir, config.AssemblyRoot, Path.GetFileName(assembly)), true);
            if (DebugLevel > 0) {
                var pdb = assembly;
                pdb = Path.ChangeExtension(pdb, ".pdb");
                if (File.Exists(pdb))
                    File.Copy(pdb, Path.Join(AppDir, config.AssemblyRoot, Path.GetFileName(pdb)), true);
            }
        }

        List<string> nativeAssets = new List<string>() { "dotnet.wasm", "dotnet.js", "dotnet.timezones.blat" };

        if (!InvariantGlobalization)
            nativeAssets.Add(IcuDataFileName!);

        foreach (var f in nativeAssets)
            File.Copy(Path.Join (runtimeSourceDir, f), Path.Join(AppDir, f), true);
        File.Copy(MainJS!, Path.Join(AppDir, "runtime.js"),  true);

        var html = @"<html><body><script type=""text/javascript"" src=""runtime.js""></script></body></html>";
        File.WriteAllText(Path.Join(AppDir, "index.html"), html);

        foreach (var assembly in _assemblies) {
            config.Assets.Add(new AssemblyEntry(Path.GetFileName(assembly)));
            if (DebugLevel > 0) {
                var pdb = assembly;
                pdb = Path.ChangeExtension(pdb, ".pdb");
                if (File.Exists(pdb))
                    config.Assets.Add(new AssemblyEntry(Path.GetFileName(pdb)));
            }
        }

        config.DebugLevel = DebugLevel;

        if (SatelliteAssemblies != null)
        {
            foreach (var assembly in SatelliteAssemblies)
            {
                string culture = assembly.GetMetadata("CultureName") ?? string.Empty;
                string fullPath = assembly.GetMetadata("Identity");
                string name = Path.GetFileName(fullPath);
                string directory = Path.Join(AppDir, config.AssemblyRoot, culture);
                Directory.CreateDirectory(directory);
                File.Copy(fullPath, Path.Join(directory, name), true);
                config.Assets.Add(new SatelliteAssemblyEntry(name, culture));
            }
        }

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

        if (!InvariantGlobalization)
            config.Assets.Add(new IcuData(IcuDataFileName!) { LoadRemote = RemoteSources?.Length > 0 });

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
}
