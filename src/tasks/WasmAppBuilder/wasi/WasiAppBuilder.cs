// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Build.Framework;

namespace Microsoft.WebAssembly.Build.Tasks;

public class WasiAppBuilder : WasmAppBuilderBaseTask
{
    public bool IsSingleFileBundle { get; set; }
    public bool OutputSymbolsToAppBundle { get; set; }

    protected override bool ValidateArguments()
    {
        if (!base.ValidateArguments())
            return false;

        if (!InvariantGlobalization && !IsSingleFileBundle && (IcuDataFileNames == null || IcuDataFileNames.Length == 0))
            throw new LogAsErrorException($"{nameof(IcuDataFileNames)} property shouldn't be empty when {nameof(InvariantGlobalization)}=false");

        if (Assemblies.Length == 0 && !IsSingleFileBundle)
            throw new LogAsErrorException("Cannot build Wasm app without any assemblies");

        if (IsSingleFileBundle)
        {
            if (ExtraFilesToDeploy.Length > 0)
            {
                throw new LogAsErrorException($"$({nameof(ExtraFilesToDeploy)}) is not supported for single file bundles. " +
                                              $"Value: {string.Join(",", ExtraFilesToDeploy.Select(e => e.GetMetadata("FullPath")))}");
            }

            if (FilesToIncludeInFileSystem.Length > 0)
            {
                throw new LogAsErrorException($"$({nameof(FilesToIncludeInFileSystem)}) is not supported for single file bundles. " +
                                              $"Value: {string.Join(",", FilesToIncludeInFileSystem.Select(e => e.ItemSpec))}");
            }
        }

        return true;
    }

    protected override bool ExecuteInternal()
    {
        if (!ValidateArguments())
            return false;

        var _assemblies = new List<string>();
        foreach (string asm in Assemblies!)
        {
            if (!_assemblies.Contains(asm))
                _assemblies.Add(asm);
        }
        MainAssemblyName = Path.GetFileName(MainAssemblyName);

        // Create app
        Directory.CreateDirectory(AppDir!);

        if (!IsSingleFileBundle)
        {
            string asmRootPath = Path.Combine(AppDir, "managed");
            Directory.CreateDirectory(asmRootPath);
            foreach (string assembly in _assemblies)
            {
                FileCopyChecked(assembly, Path.Combine(asmRootPath, Path.GetFileName(assembly)), "Assemblies");

                if (OutputSymbolsToAppBundle)
                {
                    string pdb = Path.ChangeExtension(assembly, ".pdb");
                    if (File.Exists(pdb))
                        FileCopyChecked(pdb, Path.Combine(asmRootPath, Path.GetFileName(pdb)), "Assemblies");
                }
            }
        }

        // TODO: Files on disk are not solved for IsSingleFileBundle yet
        foreach (ITaskItem item in NativeAssets)
        {
            string dest = Path.Combine(AppDir, Path.GetFileName(item.ItemSpec));
            if (!FileCopyChecked(item.ItemSpec, dest, "NativeAssets"))
                return false;
        }

        ProcessSatelliteAssemblies(args =>
        {
            string name = Path.GetFileName(args.fullPath);
            string directory = Path.Combine(AppDir, "managed", args.culture);
            Directory.CreateDirectory(directory);
            FileCopyChecked(args.fullPath, Path.Combine(directory, name), "SatelliteAssemblies");
        });

        if (!DeployFiles(ExtraFilesToDeploy, nameof(ExtraFilesToDeploy)))
            return false;
        if (!DeployFiles(FilesToIncludeInFileSystem, nameof(FilesToIncludeInFileSystem)))
            return false;

        Directory.CreateDirectory(Path.Combine(AppDir, "tmp"));

        UpdateRuntimeConfigJson();
        return !Log.HasLoggedErrors;

        bool DeployFiles(ITaskItem[] fileItems, string label)
        {
            foreach (ITaskItem item in fileItems)
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

                if (!FileCopyChecked(src, dst, label))
                    return false;
            }

            return true;
        }

    }

    protected override void AddToRuntimeConfig(JsonObject wasmHostProperties, JsonArray runtimeArgsArray, JsonArray perHostConfigs)
    {
        if (IsSingleFileBundle)
            wasmHostProperties["singleFileBundle"] = true;
    }
}
