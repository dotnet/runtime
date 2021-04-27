// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class WasmAppBuilder : Task
{
    [NotNull]
    [Required]
    public string? AppDir { get; set; }

    [NotNull]
    [Required]
    public string? MainJS { get; set; }

    [NotNull]
    [Required]
    public string[]? Assemblies { get; set; }

    [NotNull]
    [Required]
    public ITaskItem[]? NativeAssets { get; set; }

    private List<string> _fileWrites = new();

    [Output]
    public string[]? FileWrites => _fileWrites.ToArray();

    // full list of ICU data files we produce can be found here:
    // https://github.com/dotnet/icu/tree/maint/maint-67/icu-filters
    public string? IcuDataFileName { get; set; }

    public int DebugLevel { get; set; }
    public ITaskItem[]? SatelliteAssemblies { get; set; }
    public ITaskItem[]? FilesToIncludeInFileSystem { get; set; }
    public ITaskItem[]? RemoteSources { get; set; }
    public bool InvariantGlobalization { get; set; }
    public ITaskItem[]? ExtraFilesToDeploy { get; set; }

    // <summary>
    // Extra json elements to add to mono-config.js
    //
    // Metadata:
    // - Value: can be a number, bool, quoted string, or json string
    //
    // Examples:
    //      <WasmExtraConfig Include="enable_profiler" Value="true" />
    //      <WasmExtraConfig Include="json" Value="{ &quot;abc&quot;: 4 }" />
    //      <WasmExtraConfig Include="string_val" Value="&quot;abc&quot;" />
    //       <WasmExtraConfig Include="string_with_json" Value="&quot;{ &quot;abc&quot;: 4 }&quot;" />
    // </summary>
    public ITaskItem[]? ExtraConfig { get; set; }

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
        [JsonExtensionData]
        public Dictionary<string, object?> Extra { get; set; } = new();
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
        if (!File.Exists(MainJS))
            throw new ArgumentException($"File MainJS='{MainJS}' doesn't exist.");
        if (!InvariantGlobalization && string.IsNullOrEmpty(IcuDataFileName))
            throw new ArgumentException("IcuDataFileName property shouldn't be empty if InvariantGlobalization=false");

        if (Assemblies?.Length == 0)
        {
            Log.LogError("Cannot build Wasm app without any assemblies");
            return false;
        }

        var _assemblies = new List<string>();
        foreach (var asm in Assemblies!)
        {
            if (!_assemblies.Contains(asm))
                _assemblies.Add(asm);
        }

        var config = new WasmAppConfig ();

        // Create app
        var asmRootPath = Path.Join(AppDir, config.AssemblyRoot);
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(asmRootPath);
        foreach (var assembly in _assemblies)
        {
            FileCopyChecked(assembly, Path.Join(asmRootPath, Path.GetFileName(assembly)), "Assemblies");
            if (DebugLevel != 0)
            {
                var pdb = assembly;
                pdb = Path.ChangeExtension(pdb, ".pdb");
                if (File.Exists(pdb))
                    FileCopyChecked(pdb, Path.Join(asmRootPath, Path.GetFileName(pdb)), "Assemblies");
            }
        }

        foreach (ITaskItem item in NativeAssets)
        {
            string dest = Path.Combine(AppDir!, Path.GetFileName(item.ItemSpec));
            if (!FileCopyChecked(item.ItemSpec, dest, "NativeAssets"))
                return false;
        }
        FileCopyChecked(MainJS!, Path.Join(AppDir, "runtime.js"), string.Empty);

        var html = @"<html><body><script type=""text/javascript"" src=""runtime.js""></script></body></html>";
        File.WriteAllText(Path.Join(AppDir, "index.html"), html);

        foreach (var assembly in _assemblies)
        {
            config.Assets.Add(new AssemblyEntry(Path.GetFileName(assembly)));
            if (DebugLevel != 0) {
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

        foreach (ITaskItem extra in ExtraConfig ?? Enumerable.Empty<ITaskItem>())
        {
            string name = extra.ItemSpec;
            if (!TryParseExtraConfigValue(extra, out object? valueObject))
                return false;

            config.Extra[name] = valueObject;
        }

        string monoConfigPath = Path.Join(AppDir, "mono-config.js");
        using (var sw = File.CreateText(monoConfigPath))
        {
            var json = JsonSerializer.Serialize (config, new JsonSerializerOptions { WriteIndented = true });
            sw.Write($"config = {json};");
        }
        _fileWrites.Add(monoConfigPath);

        if (ExtraFilesToDeploy != null)
        {
            foreach (ITaskItem item in ExtraFilesToDeploy!)
            {
                string src = item.ItemSpec;

                string dstDir = Path.Combine(AppDir!, item.GetMetadata("TargetPath"));
                if (!Directory.Exists(dstDir))
                    Directory.CreateDirectory(dstDir);

                string dst = Path.Combine(dstDir, Path.GetFileName(src));
                if (!FileCopyChecked(src, dst, "ExtraFilesToDeploy"))
                    return false;
            }
        }

        return !Log.HasLoggedErrors;
    }

    private bool TryParseExtraConfigValue(ITaskItem extraItem, out object? valueObject)
    {
        valueObject = null;
        string? rawValue = extraItem.GetMetadata("Value");
        if (string.IsNullOrEmpty(rawValue))
            return true;

        if (TryConvert(rawValue, typeof(double), out valueObject) || TryConvert(rawValue, typeof(bool), out valueObject))
            return true;

        // Try parsing as a quoted string
        if (rawValue!.Length > 1 && rawValue![0] == '"' && rawValue![^1] == '"')
        {
            valueObject = rawValue![1..^1];
            return true;
        }

        // try parsing as json
        try
        {
            JsonDocument jdoc = JsonDocument.Parse(rawValue);
            valueObject = jdoc.RootElement;
            return true;
        }
        catch (JsonException je)
        {
            Log.LogError($"ExtraConfig: {extraItem.ItemSpec} with Value={rawValue} cannot be parsed as a number, boolean, string, or json object/array: {je.Message}");
            return false;
        }
    }

    private static bool TryConvert(string str, Type type, out object? value)
    {
        value = null;
        try
        {
            value = Convert.ChangeType(str, type);
            return true;
        }
        catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
        {
            return false;
        }
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
