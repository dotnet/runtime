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
    [Required]
    public string[]? Assemblies { get; set; }

    private List<string> _fileWrites = new();
    [Output]
    public string[]? FileWrites => _fileWrites.ToArray();

    // full list of ICU data files we produce can be found here:
    // https://github.com/dotnet/icu/tree/maint/maint-67/icu-filters
    public string? IcuDataFileName { get; set; } = "icudt.dat";

    public int DebugLevel { get; set; }
    public ITaskItem[]? SatelliteAssemblies { get; set; }
    public ITaskItem[]? FilesToIncludeInFileSystem { get; set; }
    public ITaskItem[]? RemoteSources { get; set; }
    public bool InvariantGlobalization { get; set; }

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

    private class AssetEntry
    {
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

    private class VfsEntry : AssetEntry
    {
        public VfsEntry(string name) : base(name, "vfs") {}
        [JsonPropertyName("virtual_path")]
        public string? VirtualPath { get; set; }
    }

    private class IcuData : AssetEntry
    {
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
        if (Assemblies == null)
        {
            Log.LogError($"Assemblies should not be null.");
            return false;
        }

        var _assemblies = new List<string>();
        var runtimeSourceDir = Path.Join(MicrosoftNetCoreAppRuntimePackDir, "native");

        foreach (var asm in Assemblies)
        {
            if (!_assemblies.Contains(asm))
                _assemblies.Add(asm);

            if (asm.EndsWith("System.Private.CoreLib.dll"))
                runtimeSourceDir = Path.GetDirectoryName(asm);
        }

        if (MainAssembly != null)
        {
            if (!_assemblies.Contains(MainAssembly))
                _assemblies.Add(MainAssembly);
        }

        var config = new WasmAppConfig ();

        // Create app
        var asmRootPath = Path.Join(AppDir, config.AssemblyRoot);
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(asmRootPath);
        foreach (var assembly in _assemblies)
        {
            FileCopyChecked(assembly, Path.Join(asmRootPath, Path.GetFileName(assembly)), "Assemblies");
            if (DebugLevel > 0)
            {
                var pdb = assembly;
                pdb = Path.ChangeExtension(pdb, ".pdb");
                if (File.Exists(pdb))
                    FileCopyChecked(pdb, Path.Join(asmRootPath, Path.GetFileName(pdb)), "Assemblies");
            }
        }

        List<string> nativeAssets = new List<string>() { "dotnet.wasm", "dotnet.js", "dotnet.timezones.blat" };

        if (!InvariantGlobalization)
            nativeAssets.Add(IcuDataFileName!);

        if (Path.TrimEndingDirectorySeparator(Path.GetFullPath(runtimeSourceDir)) != Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppDir!)))
        {
            foreach (var f in nativeAssets)
                FileCopyChecked(Path.Join(runtimeSourceDir, f), Path.Join(AppDir, f), "NativeAssets");
        }
        FileCopyChecked(MainJS!, Path.Join(AppDir, "runtime.js"), string.Empty);

        var html = @"<html><body><script type=""text/javascript"" src=""runtime.js""></script></body></html>";
        File.WriteAllText(Path.Join(AppDir, "index.html"), html);

        foreach (var assembly in _assemblies)
        {
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
                FileCopyChecked(fullPath, Path.Join(directory, name), "SatelliteAssemblies");
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

                FileCopyChecked(item.ItemSpec, Path.Join(supportFilesDir, generatedFileName), "FilesToIncludeInFileSystem");

                var asset = new VfsEntry ($"supportFiles/{generatedFileName}") {
                    VirtualPath = targetPath
                };
                config.Assets.Add(asset);
            }
        }

        if (!InvariantGlobalization)
            config.Assets.Add(new IcuData(IcuDataFileName!) { LoadRemote = RemoteSources?.Length > 0 });

        config.Assets.Add(new VfsEntry ("dotnet.timezones.blat") { VirtualPath = "/usr/share/zoneinfo/"});

        if (RemoteSources?.Length > 0)
        {
            foreach (var source in RemoteSources)
                if (source != null && source.ItemSpec != null)
                    config.RemoteSources.Add(source.ItemSpec);
        }

        string monoConfigPath = Path.Join(AppDir, "mono-config.js");
        using (var sw = File.CreateText(monoConfigPath))
        {
            var json = JsonSerializer.Serialize (config, new JsonSerializerOptions { WriteIndented = true });
            sw.Write($"config = {json};");
        }
        _fileWrites.Add(monoConfigPath);

        return true;
    }

    private bool FileCopyChecked(string src, string dst, string label)
    {
        if (!File.Exists(src))
        {
            Log.LogError($"{label} file '{src}' not found");
            return false;
        }

        Log.LogMessage(MessageImportance.Low, $"Copying file from '{src}' to '{dst}'");
        File.Copy(src, dst, true);
        _fileWrites.Add(dst);

        return true;
    }
}
