// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.WebAssembly;

namespace Microsoft.WebAssembly.Build.Tasks;

public class WasmAppBuilder : WasmAppBuilderBaseTask
{
    public ITaskItem[]? RemoteSources { get; set; }
    public bool IncludeThreadsWorker { get; set; }
    public int PThreadPoolSize { get; set; }
    public bool UseWebcil { get; set; }

    // <summary>
    // Extra json elements to add to _framework/blazor.boot.json
    //
    // Metadata:
    // - Value: can be a number, bool, quoted string, or json string
    //
    // Examples:
    //      <WasmExtraConfig Include="enableProfiler" Value="true" />
    //      <WasmExtraConfig Include="json" Value="{ &quot;abc&quot;: 4 }" />
    //      <WasmExtraConfig Include="string_val" Value="&quot;abc&quot;" />
    //       <WasmExtraConfig Include="string_with_json" Value="&quot;{ &quot;abc&quot;: 4 }&quot;" />
    // </summary>
    public ITaskItem[]? ExtraConfig { get; set; }

    protected override bool ValidateArguments()
    {
        if (!base.ValidateArguments())
            return false;

        if (!InvariantGlobalization && (IcuDataFileNames == null || IcuDataFileNames.Length == 0))
            throw new LogAsErrorException($"{nameof(IcuDataFileNames)} property shouldn't be empty when {nameof(InvariantGlobalization)}=false");

        if (Assemblies.Length == 0)
        {
            Log.LogError("Cannot build Wasm app without any assemblies");
            return false;
        }

        return true;
    }

    protected override bool ExecuteInternal()
    {
        if (!ValidateArguments())
            return false;

        var _assemblies = new List<string>();
        foreach (var asm in Assemblies!)
        {
            if (!_assemblies.Contains(asm))
                _assemblies.Add(asm);
        }
        MainAssemblyName = Path.GetFileName(MainAssemblyName);

        var config = new BootJsonData()
        {
            entryAssembly = MainAssemblyName,
            icuDataMode = InvariantGlobalization ? ICUDataMode.Invariant : HybridGlobalization ? ICUDataMode.Hybrid : ICUDataMode.Sharded
        };

        // Create app
        var asmRootPath = AppDir;
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(asmRootPath);
        if (UseWebcil)
            Log.LogMessage(MessageImportance.Normal, "Converting assemblies to Webcil");
        foreach (var assembly in _assemblies)
        {
            if (UseWebcil)
            {
                var tmpWebcil = Path.GetTempFileName();
                var webcilWriter = Microsoft.WebAssembly.Build.Tasks.WebcilConverter.FromPortableExecutable(inputPath: assembly, outputPath: tmpWebcil, logger: Log);
                webcilWriter.ConvertToWebcil();
                var finalWebcil = Path.Combine(asmRootPath, Path.ChangeExtension(Path.GetFileName(assembly), ".webcil"));
                if (Utils.CopyIfDifferent(tmpWebcil, finalWebcil, useHash: true))
                    Log.LogMessage(MessageImportance.Low, $"Generated {finalWebcil} .");
                else
                    Log.LogMessage(MessageImportance.Low, $"Skipped generating {finalWebcil} as the contents are unchanged.");
                _fileWrites.Add(finalWebcil);
            }
            else
            {
                FileCopyChecked(assembly, Path.Combine(asmRootPath, Path.GetFileName(assembly)), "Assemblies");
            }
            if (DebugLevel != 0)
            {
                var pdb = assembly;
                pdb = Path.ChangeExtension(pdb, ".pdb");
                if (File.Exists(pdb))
                    FileCopyChecked(pdb, Path.Combine(asmRootPath, Path.GetFileName(pdb)), "Assemblies");
            }
        }

        foreach (ITaskItem item in NativeAssets)
        {
            var name = Path.GetFileName(item.ItemSpec);
            var dest = Path.Combine(AppDir!, name);
            if (!FileCopyChecked(item.ItemSpec, dest, "NativeAssets"))
                return false;

            if (!IncludeThreadsWorker && name == "dotnet.native.worker.js")
                continue;

            config.resources.runtime[name] = Utils.ComputeIntegrity(item.ItemSpec);
        }

        string packageJsonPath = Path.Combine(AppDir, "package.json");
        if (!File.Exists(packageJsonPath))
        {
            var json = @"{ ""type"":""module"" }";
            File.WriteAllText(packageJsonPath, json);
        }

        foreach (var assembly in _assemblies)
        {
            string assemblyPath = assembly;
            var bytes = File.ReadAllBytes(assemblyPath);
            // for the is IL IsAssembly check we need to read the bytes from the original DLL
            if (!Utils.IsManagedAssembly(bytes))
            {
                Log.LogMessage(MessageImportance.Low, "Skipping non-assembly file: " + assemblyPath);
            }
            else
            {
                if (UseWebcil)
                {
                    assemblyPath = Path.Combine(asmRootPath, Path.ChangeExtension(Path.GetFileName(assembly), ".webcil"));
                    // For the hash, read the bytes from the webcil file, not the dll file.
                    bytes = File.ReadAllBytes(assemblyPath);
                }

                config.resources.assembly[Path.GetFileName(assemblyPath)] = Utils.ComputeIntegrity(bytes);
                if (DebugLevel != 0)
                {
                    var pdb = Path.ChangeExtension(assembly, ".pdb");
                    config.resources.pdb[Path.GetFileName(pdb)] = Utils.ComputeIntegrity(pdb);
                }
            }
        }

        config.debugBuild = DebugLevel > 0;

        ProcessSatelliteAssemblies(args =>
        {
            string name = Path.GetFileName(args.fullPath);
            string directory = Path.Combine(AppDir, args.culture);
            Directory.CreateDirectory(directory);
            if (UseWebcil)
            {
                var tmpWebcil = Path.GetTempFileName();
                var webcilWriter = Microsoft.WebAssembly.Build.Tasks.WebcilConverter.FromPortableExecutable(inputPath: args.fullPath, outputPath: tmpWebcil, logger: Log);
                webcilWriter.ConvertToWebcil();
                var finalWebcil = Path.Combine(directory, Path.ChangeExtension(name, ".webcil"));
                if (Utils.CopyIfDifferent(tmpWebcil, finalWebcil, useHash: true))
                    Log.LogMessage(MessageImportance.Low, $"Generated {finalWebcil} .");
                else
                    Log.LogMessage(MessageImportance.Low, $"Skipped generating {finalWebcil} as the contents are unchanged.");
                _fileWrites.Add(finalWebcil);

                if (!config.resources.satelliteResources.TryGetValue(args.culture, out var cultureSatelliteResources))
                    config.resources.satelliteResources[args.culture] = cultureSatelliteResources = new();

                cultureSatelliteResources[Path.GetFileName(finalWebcil)] = Utils.ComputeIntegrity(finalWebcil);
            }
            else
            {
                var satellitePath = Path.Combine(directory, name);
                FileCopyChecked(args.fullPath, satellitePath, "SatelliteAssemblies");

                if (!config.resources.satelliteResources.TryGetValue(args.culture, out var cultureSatelliteResources))
                    config.resources.satelliteResources[args.culture] = cultureSatelliteResources = new();

                cultureSatelliteResources[name] = Utils.ComputeIntegrity(satellitePath);
            }
        });

        if (FilesToIncludeInFileSystem.Length > 0)
        {
            string supportFilesDir = Path.Combine(AppDir, "supportFiles");
            Directory.CreateDirectory(supportFilesDir);

            var i = 0;
            StringDictionary targetPathTable = new();
            var vfs = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in FilesToIncludeInFileSystem)
            {
                string? targetPath = item.GetMetadata("TargetPath");
                if (string.IsNullOrEmpty(targetPath))
                {
                    targetPath = Path.GetFileName(item.ItemSpec);
                }

                // We normalize paths from `\` to `/` as MSBuild items could use `\`.
                targetPath = targetPath.Replace('\\', '/');
                if (targetPathTable.ContainsKey(targetPath))
                {
                    string firstPath = Path.GetFullPath(targetPathTable[targetPath]!);
                    string secondPath = Path.GetFullPath(item.ItemSpec);

                    if (firstPath == secondPath)
                    {
                        Log.LogWarning(null, "WASM0003", "", "", 0, 0, 0, 0, $"Found identical vfs mappings for target path: {targetPath}, source file: {firstPath}. Ignoring.");
                        continue;
                    }

                    throw new LogAsErrorException($"Found more than one file mapping to the target VFS path: {targetPath}. Source files: {firstPath}, and {secondPath}");
                }

                targetPathTable[targetPath] = item.ItemSpec;

                var generatedFileName = $"{i++}_{Path.GetFileName(item.ItemSpec)}";
                var vfsPath = Path.Combine(supportFilesDir, generatedFileName);
                FileCopyChecked(item.ItemSpec, vfsPath, "FilesToIncludeInFileSystem");

                vfs[targetPath] = new()
                {
                    [$"supportFiles/{generatedFileName}"] = Utils.ComputeIntegrity(vfsPath)
                };
            }

            if (vfs.Count > 0)
                config.resources.vfs = vfs;
        }

        if (!InvariantGlobalization)
        {
            bool loadRemote = RemoteSources?.Length > 0;
            foreach (var idfn in IcuDataFileNames)
            {
                if (!File.Exists(idfn))
                {
                    Log.LogError($"Expected the file defined as ICU resource: {idfn} to exist but it does not.");
                    return false;
                }

                // TODO MF: LoadRemote
                config.resources.runtime[Path.GetFileName(idfn)] = Utils.ComputeIntegrity(idfn);
            }
        }


        if (RemoteSources?.Length > 0)
        {
            // TODO MF: RemoteSources
            //foreach (var source in RemoteSources)
            //    if (source != null && source.ItemSpec != null)
            //        config.RemoteSources.Add(source.ItemSpec);
        }

        var extraConfiguration = new Dictionary<string, object?>();

        if (PThreadPoolSize < -1)
        {
            throw new LogAsErrorException($"PThreadPoolSize must be -1, 0 or positive, but got {PThreadPoolSize}");
        }
        else if (PThreadPoolSize > -1)
        {
            extraConfiguration["pthreadPoolSize"] = PThreadPoolSize;
        }

        foreach (ITaskItem extra in ExtraConfig ?? Enumerable.Empty<ITaskItem>())
        {
            string name = extra.ItemSpec;
            if (!TryParseExtraConfigValue(extra, out object? valueObject))
                return false;

            extraConfiguration[name] = valueObject;
        }

        if (extraConfiguration.Count > 0)
        {
            config.extensions["extra"] = extraConfiguration;
        }

        string tmpMonoConfigPath = Path.GetTempFileName();
        using (var sw = File.CreateText(tmpMonoConfigPath))
        {
            var sb = new StringBuilder();

            static void AddDictionary(StringBuilder sb, Dictionary<string, string> res)
            {
                foreach (var asset in res)
                    sb.Append(asset.Value);
            }

            AddDictionary(sb, config.resources.assembly);
            AddDictionary(sb, config.resources.runtime);

            if (config.resources.lazyAssembly != null)
                AddDictionary(sb, config.resources.lazyAssembly);

            if (config.resources.satelliteResources != null)
            {
                foreach (var culture in config.resources.satelliteResources)
                    AddDictionary(sb, culture.Value);
            }

            if (config.resources.vfs != null)
            {
                foreach (var entry in config.resources.vfs)
                    AddDictionary(sb, entry.Value);
            }

            config.resources.hash = Utils.ComputeTextIntegrity(sb.ToString());

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            sw.Write(json);
        }

        string monoConfigDir = Path.Combine(AppDir, "_framework");
        if (!Directory.Exists(monoConfigDir))
            Directory.CreateDirectory(monoConfigDir);

        string monoConfigPath = Path.Combine(monoConfigDir, "blazor.boot.json"); // TODO: Unify with Wasm SDK
        Utils.CopyIfDifferent(tmpMonoConfigPath, monoConfigPath, useHash: false);
        _fileWrites.Add(monoConfigPath);

        foreach (ITaskItem item in ExtraFilesToDeploy!)
        {
            string src = item.ItemSpec;
            string dst;

            string tgtPath = item.GetMetadata("TargetPath");
            if (!string.IsNullOrEmpty(tgtPath))
            {
                dst = Path.Combine(AppDir!, tgtPath);
                string? dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
                    Directory.CreateDirectory(dstDir!);
            }
            else
            {
                dst = Path.Combine(AppDir!, Path.GetFileName(src));
            }

            if (!FileCopyChecked(src, dst, "ExtraFilesToDeploy"))
                return false;
        }

        UpdateRuntimeConfigJson();
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
        if (rawValue!.Length > 1 && rawValue![0] == '"' && rawValue![rawValue!.Length - 1] == '"')
        {
            valueObject = rawValue!.Substring(1, rawValue!.Length - 2);
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
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }
}
