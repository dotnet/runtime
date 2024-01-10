// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

public abstract class WasmAppBuilderBaseTask : Task
{
    [NotNull]
    [Required]
    public string? AppDir { get; set; }

    [Required]
    public string[] Assemblies { get; set; } = Array.Empty<string>();

    public string? RuntimeConfigJsonPath { get; set; }

    // files like dotnet.native.wasm, icudt.dat etc
    [NotNull]
    [Required]
    public ITaskItem[] NativeAssets { get; set; } = Array.Empty<ITaskItem>();

    protected readonly List<string> _fileWrites = new();

    [Output]
    public string[]? FileWrites => _fileWrites.ToArray();

    // full list of ICU data files we produce can be found here:
    // https://github.com/dotnet/icu/tree/maint/maint-67/icu-filters
    public string[] IcuDataFileNames { get; set; } = Array.Empty<string>();

    public int DebugLevel { get; set; }
    public ITaskItem[] SatelliteAssemblies { get; set; } = Array.Empty<ITaskItem>();
    public bool HybridGlobalization { get; set; }
    public bool InvariantGlobalization { get; set; }
    public ITaskItem[] FilesToIncludeInFileSystem { get; set; } = Array.Empty<ITaskItem>();
    public ITaskItem[] ExtraFilesToDeploy { get; set; } = Array.Empty<ITaskItem>();

    public string? DefaultHostConfig { get; set; }

    [Required, NotNull]
    public string? MainAssemblyName { get; set; }

    [Required]
    public ITaskItem[] HostConfigs { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] RuntimeArgsForHost { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            return ExecuteInternal();
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError(laee.Message);
            return false;
        }
    }

    protected abstract bool ExecuteInternal();

    protected virtual bool ValidateArguments() => true;

    protected void ProcessSatelliteAssemblies(Action<(string fullPath, string culture)> addSatelliteAssemblyFunc)
    {
        foreach (var assembly in SatelliteAssemblies)
        {
            string culture = assembly.GetMetadata("CultureName") ?? string.Empty;
            string fullPath = assembly.GetMetadata("Identity");
            if (string.IsNullOrEmpty(culture))
            {
                Log.LogWarning(null, "WASM0002", "", "", 0, 0, 0, 0, $"Missing CultureName metadata for satellite assembly {fullPath}");
                continue;
            }

            // FIXME: validate the culture?
            addSatelliteAssemblyFunc((fullPath, culture));
        }
    }

    protected virtual void UpdateRuntimeConfigJson()
    {
        if (string.IsNullOrEmpty(RuntimeConfigJsonPath))
            return;

        if (!File.Exists(RuntimeConfigJsonPath))
        {
            Log.LogMessage(MessageImportance.Low, $"Could not find {nameof(RuntimeConfigJsonPath)}={RuntimeConfigJsonPath}. Ignoring.");
            return;
        }

        string[] matchingAssemblies = Assemblies.Where(asm => Path.GetFileName(asm) == MainAssemblyName).ToArray();
        if (matchingAssemblies.Length == 0)
            throw new LogAsErrorException($"Could not find main assembly named {MainAssemblyName} in the list of assemblies");

        if (matchingAssemblies.Length > 1)
            throw new LogAsErrorException($"Found more than one assembly matching the main assembly name {MainAssemblyName}: {string.Join(",", matchingAssemblies)}");

        var rootNode = JsonNode.Parse(File.ReadAllText(RuntimeConfigJsonPath),
                                            new JsonNodeOptions { PropertyNameCaseInsensitive = true });
        if (rootNode == null)
            throw new LogAsErrorException($"Failed to parse {RuntimeConfigJsonPath}");

        JsonObject? rootObject = rootNode.AsObject();
        if (!rootObject.TryGetPropertyValue("runtimeOptions", out JsonNode? runtimeOptionsNode)
                || !(runtimeOptionsNode is JsonObject runtimeOptionsObject))
        {
            throw new LogAsErrorException($"Could not find node named 'runtimeOptions' in {RuntimeConfigJsonPath}");
        }

        JsonObject wasmHostProperties = runtimeOptionsObject.GetOrCreate<JsonObject>("wasmHostProperties", () => new JsonObject());
        JsonArray runtimeArgsArray = wasmHostProperties.GetOrCreate<JsonArray>("runtimeArgs", () => new JsonArray());
        JsonArray perHostConfigs = wasmHostProperties.GetOrCreate<JsonArray>("perHostConfig", () => new JsonArray());

        if (string.IsNullOrEmpty(DefaultHostConfig) && HostConfigs.Length > 0)
            DefaultHostConfig = HostConfigs[0].ItemSpec;

        if (!string.IsNullOrEmpty(DefaultHostConfig))
            wasmHostProperties["defaultConfig"] = DefaultHostConfig;

        wasmHostProperties["mainAssembly"] = MainAssemblyName;

        foreach (JsonValue? rarg in RuntimeArgsForHost.Select(ri => JsonValue.Create(ri.ItemSpec)))
        {
            if (rarg is not null)
                runtimeArgsArray.Add(rarg);
        }

        foreach (ITaskItem hostConfigItem in HostConfigs)
        {
            var hostConfigObject = new JsonObject();

            string name = hostConfigItem.ItemSpec;
            string host = hostConfigItem.GetMetadata("host");
            if (string.IsNullOrEmpty(host))
                throw new LogAsErrorException($"BUG: Could not find required metadata 'host' for host config named '{name}'");

            hostConfigObject.Add("name", name);
            foreach (KeyValuePair<string, string> kvp in hostConfigItem.CloneCustomMetadata().Cast<KeyValuePair<string, string>>())
                hostConfigObject.Add(kvp.Key, kvp.Value);

            perHostConfigs.Add(hostConfigObject);
        }

        AddToRuntimeConfig(wasmHostProperties: wasmHostProperties, runtimeArgsArray: runtimeArgsArray, perHostConfigs: perHostConfigs);

        string dstPath = Path.Combine(AppDir!, Path.GetFileName(RuntimeConfigJsonPath));
        using FileStream? fs = new FileStream(dstPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
        rootObject.WriteTo(writer);
        _fileWrites.Add(dstPath);

        Log.LogMessage(MessageImportance.Low, $"Generated {dstPath} from {RuntimeConfigJsonPath}");
    }

    protected virtual void AddToRuntimeConfig(JsonObject wasmHostProperties, JsonArray runtimeArgsArray, JsonArray perHostConfigs)
    {
    }

    protected bool FileCopyChecked(string src, string dst, string label)
    {
        if (!File.Exists(src))
        {
            Log.LogError($"{label} file '{src}' not found");
            return false;
        }

        Log.LogMessage(MessageImportance.Low, $"Copying file from '{src}' to '{dst}'");
        try
        {
            File.Copy(src, dst, true);
            _fileWrites.Add(dst);

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new LogAsErrorException($"{label} Failed to copy {src} to {dst} because {ex.Message}");
        }
    }
}
