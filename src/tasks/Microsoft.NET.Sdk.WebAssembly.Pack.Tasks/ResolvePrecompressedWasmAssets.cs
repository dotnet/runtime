// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly;

public sealed class ResolvePrecompressedWasmRuntimeAssets : Task
{
    [Required] public string RuntimePackDirectory { get; set; } = "";
    [Required] public string OutputPath { get; set; } = "";
    public bool IncludeReadyToRun { get; set; }
    public ITaskItem[] RuntimeAssets { get; set; } = [];
    public ITaskItem[] CopyLocalAssets { get; set; } = [];
    [Output] public ITaskItem[] UpdatedRuntimeAssets { get; set; } = [];
    [Output] public ITaskItem[] UpdatedCopyLocalAssets { get; set; } = [];
    [Output] public ITaskItem[] Representations { get; set; } = [];
    [Output] public string[] FileWrites { get; set; } = [];

    public override bool Execute()
    {
        var mappings = new Dictionary<string, (string Output, ITaskItem Asset)>(StringComparer.OrdinalIgnoreCase);
        var unusedR2R = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new List<(ITaskItem Asset, string Output)>();
        foreach (ITaskItem asset in PrecompressedWasmAssets.Read(RuntimePackDirectory))
        {
            string kind = asset.GetMetadata("Kind");
            if (kind == "Webcil")
                continue;
            string relative = asset.GetMetadata("RuntimePath");
            string original = PrecompressedWasmAssets.ResolvePath(RuntimePackDirectory, relative);
            if (kind == "ReadyToRun" && !IncludeReadyToRun)
            {
                unusedR2R.Add(original);
                continue;
            }
            if (!PrecompressedWasmAssets.HasEncoded(asset))
            {
                Log.LogError($"Cannot read gzip-only runtime asset '{asset.ItemSpec}'. Restore the runtime pack.");
                return false;
            }
            string output = PrecompressedWasmAssets.ResolvePath(OutputPath, relative.Substring(PrecompressedWasmAssets.NativePath.Length));
            asset.SetMetadata("ExpectedAsset", output);
            mappings.Add(original, (output, asset));
            pending.Add((asset, output));
        }
        FileWrites = PrecompressedWasmAssets.Materialize(pending, OutputPath);
        UpdatedRuntimeAssets = RuntimeAssets.Where(a => !unusedR2R.Contains(Path.GetFullPath(a.ItemSpec))).Select(Map).ToArray();
        UpdatedCopyLocalAssets = CopyLocalAssets.Where(a => !unusedR2R.Contains(Path.GetFullPath(a.ItemSpec))).Select(Map).ToArray();
        Representations = pending.Select(p => p.Asset).ToArray();
        return true;

        ITaskItem Map(ITaskItem original)
        {
            if (!mappings.TryGetValue(Path.GetFullPath(original.ItemSpec), out (string Output, ITaskItem Asset) mapping))
                return original;
            var result = new TaskItem(mapping.Output, original.CloneCustomMetadata());
            result.SetMetadata("OriginalItemSpec", mapping.Output);
            PrecompressedWasmAssets.ApplyContentMetadata(result, mapping.Asset);
            return result;
        }
    }
}

public sealed class ResolvePrecompressedWasmAssemblies : Task
{
    [Required] public string RuntimePackDirectory { get; set; } = "";
    [Required] public string OutputPath { get; set; } = "";
    public string PrebuiltR2RDirectory { get; set; } = "";
    public int WebcilVersion { get; set; }
    public ITaskItem[] Candidates { get; set; } = [];
    [Output] public ITaskItem[] UpdatedCandidates { get; set; } = [];
    [Output] public ITaskItem[] ConversionCandidates { get; set; } = [];
    [Output] public ITaskItem[] Representations { get; set; } = [];
    [Output] public string SelectionStamp { get; set; } = "";
    [Output] public string[] FileWrites { get; set; } = [];

    public override bool Execute()
    {
        Dictionary<string, ITaskItem> assets = PrecompressedWasmAssets.Read(RuntimePackDirectory)
            .Where(a => a.GetMetadata("Kind") != "Native").ToDictionary(a => a.GetMetadata("SourcePath"), StringComparer.Ordinal);
        Dictionary<string, ITaskItem> assetsByPath = assets.Values.ToDictionary(
            a => PrecompressedWasmAssets.ResolvePath(RuntimePackDirectory, a.GetMetadata("SourcePath")), StringComparer.OrdinalIgnoreCase);
        string r2rDirectory = PrecompressedWasmAssets.ResolvePath(RuntimePackDirectory, PrecompressedWasmAssets.NativePath + "r2r/");
        bool useR2R = PrebuiltR2RDirectory.Length != 0 &&
            string.Equals(Path.GetFullPath(PrebuiltR2RDirectory).TrimEnd('/', '\\'), r2rDirectory.TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase);
        var pending = new List<(ITaskItem Asset, string Output)>();
        var candidates = new List<ITaskItem>();
        var conversions = new List<ITaskItem>();
        foreach (ITaskItem original in Candidates)
        {
            var item = new TaskItem(original);
            string sourcePath = item.GetMetadata("PathInPackage").Replace('\\', '/');
            ITaskItem? asset;
            if (sourcePath.Length != 0)
                assets.TryGetValue(sourcePath, out asset);
            else
                assetsByPath.TryGetValue(Path.GetFullPath(item.ItemSpec), out asset);
            bool selected = false;
            if (asset is not null && item.GetMetadata("AssetTraitName") != "Culture" &&
                string.Equals(item.GetMetadata("NuGetPackageId"), asset.GetMetadata("NuGetPackageId"), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.GetMetadata("NuGetPackageVersion"), asset.GetMetadata("NuGetPackageVersion"), StringComparison.OrdinalIgnoreCase) &&
                item.GetMetadata("R2RWebcilPath").Length == 0 &&
                asset.GetMetadata("WebcilVersion") == WebcilVersion.ToString(System.Globalization.CultureInfo.InvariantCulture) &&
                (useR2R || asset.GetMetadata("Kind") != "ReadyToRun"))
            {
                (Guid mvid, bool hasIL) = PrecompressedWasmAssets.ReadAssembly(item.ItemSpec);
                selected = mvid.ToString() == asset.GetMetadata("Mvid") &&
                    hasIL.ToString() == asset.GetMetadata("HasIL") &&
                    (hasIL || asset.GetMetadata("Kind") != "ReadyToRun") &&
                    PrecompressedWasmAssets.HasEncoded(asset);
                if (selected)
                {
                    string output = Path.GetFullPath(item.GetMetadata("WebcilOutputPath"));
                    asset.SetMetadata("ExpectedAsset", output);
                    pending.Add((asset, output));
                    PrecompressedWasmAssets.ApplyContentMetadata(item, asset);
                }
                else
                {
                    Log.LogMessage(MessageImportance.Normal, $"Not reusing '{asset.ItemSpec}' for '{item.ItemSpec}': module version, IL flavor or file length mismatch.");
                }
            }
            candidates.Add(item);
            if (!selected)
                conversions.Add(item);
        }
        FileWrites = PrecompressedWasmAssets.Materialize(pending, OutputPath);
        UpdatedCandidates = candidates.ToArray();
        ConversionCandidates = conversions.ToArray();
        Representations = pending.Select(p => p.Asset).ToArray();
        SelectionStamp = PrecompressedWasmAssets.Hash(System.Text.Encoding.UTF8.GetBytes(string.Join("|",
            Representations.Select(a => a.ItemSpec + ":" + PrecompressedWasmAssets.ContentStamp(a)))));
        return true;
    }
}
