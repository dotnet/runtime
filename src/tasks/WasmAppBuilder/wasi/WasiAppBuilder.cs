// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Microsoft.Build.Framework;

namespace Microsoft.WebAssembly.Build.Tasks;

public class WasiAppBuilder : WasmAppBuilderBaseTask
{
    public bool IsSingleFileBundle { get; set; }

    protected override bool ExecuteInternal()
    {
        if (!InvariantGlobalization && string.IsNullOrEmpty(IcuDataFileName))
            throw new LogAsErrorException("IcuDataFileName property shouldn't be empty if InvariantGlobalization=false");

        if (Assemblies.Length == 0)
        {
            Log.LogError("Cannot build Wasm app without any assemblies");
            return false;
        }

        var _assemblies = new List<string>();
        foreach (string asm in Assemblies!)
        {
            if (!_assemblies.Contains(asm))
                _assemblies.Add(asm);
        }
        MainAssemblyName = Path.GetFileName(MainAssemblyName);

        // Create app
        string asmRootPath = Path.Combine(AppDir, "managed");
        Directory.CreateDirectory(AppDir!);
        Directory.CreateDirectory(asmRootPath);
        foreach (string assembly in _assemblies)
        {
            FileCopyChecked(assembly, Path.Combine(asmRootPath, Path.GetFileName(assembly)), "Assemblies");

            if (DebugLevel != 0)
            {
                string pdb = Path.ChangeExtension(assembly, ".pdb");
                if (File.Exists(pdb))
                    FileCopyChecked(pdb, Path.Combine(asmRootPath, Path.GetFileName(pdb)), "Assemblies");
            }
        }

        foreach (ITaskItem item in NativeAssets)
        {
            string dest = Path.Combine(AppDir!, Path.GetFileName(item.ItemSpec));
            if (!FileCopyChecked(item.ItemSpec, dest, "NativeAssets"))
                return false;
        }

        foreach (ITaskItem assembly in SatelliteAssemblies)
        {
            string culture = assembly.GetMetadata("CultureName") ?? string.Empty;
            string fullPath = assembly.GetMetadata("Identity");
            if (string.IsNullOrEmpty(culture))
            {
                Log.LogWarning(null, "WASM0002", "", "", 0, 0, 0, 0, $"Missing CultureName metadata for satellite assembly {fullPath}");
                continue;
            }
            // FIXME: validate the culture?

            string name = Path.GetFileName(fullPath);
            string directory = Path.Combine(AppDir, "managed", culture);
            Directory.CreateDirectory(directory);
            FileCopyChecked(fullPath, Path.Combine(directory, name), "SatelliteAssemblies");
        }

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

    protected override void AddToRuntimeConfig(JsonObject wasmHostProperties, JsonArray runtimeArgsArray, JsonArray perHostConfigs)
    {
        if (IsSingleFileBundle)
            wasmHostProperties["singleFileBundle"] = true;
    }
}
