// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.WebAssembly;

namespace Microsoft.NET.Sdk.WebAssembly;

// This target computes the list of publish static web assets based on the changes that happen during publish and the list of build static
// web assets.
// In this target we need to do 2 things:
// * Harmonize the list of dlls produced at build time with the list of resolved files to publish.
//   * We iterate over the list of existing static web assets and do as follows:
//     * If we find the assembly in the resolved files to publish and points to the original assembly (linker disabled or assembly not linked)
//       we create a new "Publish" static web asset for the assembly.
//     * If we find the assembly in the resolved files to publish and points to a new location, we assume this assembly has been updated (as part of linking)
//       and we create a new "Publish" static web asset for the asembly pointing to the new location.
//     * If we don't find the assembly on the resolved files to publish it has been linked out from the app, so we don't add any new static web asset and we
//       also avoid adding any existing related static web asset (satellite assemblies and compressed versions).
//   * We update static web assets for satellite assemblies and compressed assets accordingly.
// * Look at the list of "native" assets and determine whether we need to create new publish assets for the current build assets or if we need to
//   update the native assets because the app was ahead of time compiled.
public class ComputeWasmPublishAssets : Task
{
    [Required]
    public ITaskItem[] ResolvedFilesToPublish { get; set; }

    public ITaskItem CustomIcuCandidate { get; set; }

    [Required]
    public ITaskItem[] WasmAotAssets { get; set; }

    [Required]
    public ITaskItem[] ExistingAssets { get; set; }

    [Required]
    public bool TimeZoneSupport { get; set; }

    [Required]
    public bool InvariantGlobalization { get; set; }

    [Required]
    public bool CopySymbols { get; set; }

    [Required]
    public string PublishPath { get; set; }

    [Required]
    public string DotNetJsVersion { get; set; }

    public bool FingerprintDotNetJs { get; set; }

    public bool EnableThreads { get; set; }

    public bool IsWebCilEnabled { get; set; }

    [Output]
    public ITaskItem[] NewCandidates { get; set; }

    [Output]
    public ITaskItem[] FilesToRemove { get; set; }

    public override bool Execute()
    {
        var filesToRemove = new List<ITaskItem>();
        var newAssets = new List<ITaskItem>();

        try
        {
            // We'll do a first pass over the resolved files to publish to figure out what files need to be removed
            // as well as categorize resolved files into different groups.
            var resolvedFilesToPublishToRemove = new Dictionary<string, ITaskItem>(StringComparer.Ordinal);

            // These assemblies are keyed of the assembly name "computed" based on the relative path, which must be
            // unique.
            var resolvedAssembliesToPublish = new Dictionary<string, ITaskItem>(StringComparer.Ordinal);
            var resolvedSymbolsToPublish = new Dictionary<string, ITaskItem>(StringComparer.Ordinal);
            var satelliteAssemblyToPublish = new Dictionary<(string, string), ITaskItem>(EqualityComparer<(string, string)>.Default);
            var resolvedNativeAssetToPublish = new Dictionary<string, ITaskItem>(StringComparer.Ordinal);
            GroupResolvedFilesToPublish(
                resolvedFilesToPublishToRemove,
                resolvedAssembliesToPublish,
                satelliteAssemblyToPublish,
                resolvedSymbolsToPublish,
                resolvedNativeAssetToPublish);

            // Group candidate static web assets
            var assemblyAssets = new Dictionary<string, ITaskItem>();
            var symbolAssets = new Dictionary<string, ITaskItem>();
            var nativeAssets = new Dictionary<string, ITaskItem>();
            var satelliteAssemblyAssets = new Dictionary<string, ITaskItem>();
            var compressedRepresentations = new Dictionary<string, ITaskItem>();
            GroupExistingStaticWebAssets(
                assemblyAssets,
                nativeAssets,
                satelliteAssemblyAssets,
                symbolAssets,
                compressedRepresentations);

            var newStaticWebAssets = ComputeUpdatedAssemblies(
                satelliteAssemblyToPublish,
                filesToRemove,
                resolvedAssembliesToPublish,
                assemblyAssets,
                satelliteAssemblyAssets,
                compressedRepresentations);

            newAssets.AddRange(newStaticWebAssets);

            var nativeStaticWebAssets = ProcessNativeAssets(
                nativeAssets,
                resolvedFilesToPublishToRemove,
                resolvedNativeAssetToPublish,
                compressedRepresentations,
                filesToRemove);

            newAssets.AddRange(nativeStaticWebAssets);

            var symbolStaticWebAssets = ProcessSymbolAssets(
                symbolAssets,
                compressedRepresentations,
                resolvedFilesToPublishToRemove,
                resolvedSymbolsToPublish,
                filesToRemove);

            newAssets.AddRange(symbolStaticWebAssets);

            foreach (var kvp in resolvedFilesToPublishToRemove)
            {
                var resolvedPublishFileToRemove = kvp.Value;
                filesToRemove.Add(resolvedPublishFileToRemove);
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            return false;
        }

        FilesToRemove = filesToRemove.ToArray();
        NewCandidates = newAssets.ToArray();

        return !Log.HasLoggedErrors;
    }

    private List<ITaskItem> ProcessNativeAssets(
        Dictionary<string, ITaskItem> nativeAssets,
        IDictionary<string, ITaskItem> resolvedPublishFilesToRemove,
        Dictionary<string, ITaskItem> resolvedNativeAssetToPublish,
        Dictionary<string, ITaskItem> compressedRepresentations,
        List<ITaskItem> filesToRemove)
    {
        var nativeStaticWebAssets = new List<ITaskItem>();

        // Keep track of the updated assets to determine what compressed assets we can reuse
        var updateMap = new Dictionary<string, ITaskItem>();

        foreach (var kvp in nativeAssets)
        {
            var key = kvp.Key;
            var asset = kvp.Value;
            var isDotNetJs = IsDotNetJs(key);
            var isDotNetWorkerJs = IsDotNetWorkerJs(key);
            var isDotNetWasm = IsDotNetWasm(key);

            if (!isDotNetJs && !isDotNetWasm && !isDotNetWorkerJs)
            {
                if (resolvedNativeAssetToPublish.TryGetValue(Path.GetFileName(asset.GetMetadata("OriginalItemSpec")), out var existing))
                {
                    if (!resolvedPublishFilesToRemove.TryGetValue(existing.ItemSpec, out var removed))
                    {
                        // This is a native asset like timezones.blat or similar that was not filtered and that needs to be updated
                        // to a publish asset.
                        var newAsset = new TaskItem(asset);
                        ApplyPublishProperties(newAsset);
                        nativeStaticWebAssets.Add(newAsset);
                        filesToRemove.Add(existing);
                        updateMap.Add(asset.ItemSpec, newAsset);
                        Log.LogMessage(MessageImportance.Low, "Promoting asset '{0}' to Publish asset.", asset.ItemSpec);
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.Low, "Removing asset '{0}'.", existing.ItemSpec);
                        // This was a file that was filtered, so just remove it, we don't need to add any publish static web asset
                        filesToRemove.Add(removed);

                        // Remove the file from the list to avoid double processing later when we process other files we filtered.
                        resolvedPublishFilesToRemove.Remove(existing.ItemSpec);
                    }
                }

                continue;
            }

            if (isDotNetJs || isDotNetWorkerJs)
            {
                var baseName = isDotNetWorkerJs ? "dotnet.worker" : "dotnet";

                var aotDotNetJs = WasmAotAssets.SingleOrDefault(a => $"{a.GetMetadata("FileName")}{a.GetMetadata("Extension")}" == $"{baseName}.js");
                ITaskItem newDotNetJs = null;
                if (aotDotNetJs != null)
                {
                    newDotNetJs = new TaskItem(Path.GetFullPath(aotDotNetJs.ItemSpec), asset.CloneCustomMetadata());
                    newDotNetJs.SetMetadata("OriginalItemSpec", aotDotNetJs.ItemSpec);

                    string relativePath = FingerprintDotNetJs
                        ? $"_framework/{$"{baseName}.{DotNetJsVersion}.{FileHasher.GetFileHash(aotDotNetJs.ItemSpec)}.js"}"
                        : $"_framework/{baseName}.js";

                    newDotNetJs.SetMetadata("RelativePath", relativePath);

                    updateMap.Add(asset.ItemSpec, newDotNetJs);
                    Log.LogMessage(MessageImportance.Low, "Replacing asset '{0}' with AoT version '{1}'", asset.ItemSpec, newDotNetJs.ItemSpec);
                }
                else
                {
                    newDotNetJs = new TaskItem(asset);
                    Log.LogMessage(MessageImportance.Low, "Promoting asset '{0}' to Publish asset.", asset.ItemSpec);
                }

                ApplyPublishProperties(newDotNetJs);
                nativeStaticWebAssets.Add(newDotNetJs);
                if (resolvedNativeAssetToPublish.TryGetValue($"{baseName}.js", out var resolved))
                {
                    filesToRemove.Add(resolved);
                }
                continue;
            }

            if (isDotNetWasm)
            {
                var aotDotNetWasm = WasmAotAssets.SingleOrDefault(a => $"{a.GetMetadata("FileName")}{a.GetMetadata("Extension")}" == "dotnet.wasm");
                ITaskItem newDotNetWasm = null;
                if (aotDotNetWasm != null)
                {
                    newDotNetWasm = new TaskItem(Path.GetFullPath(aotDotNetWasm.ItemSpec), asset.CloneCustomMetadata());
                    newDotNetWasm.SetMetadata("OriginalItemSpec", aotDotNetWasm.ItemSpec);
                    updateMap.Add(asset.ItemSpec, newDotNetWasm);
                    Log.LogMessage(MessageImportance.Low, "Replacing asset '{0}' with AoT version '{1}'", asset.ItemSpec, newDotNetWasm.ItemSpec);
                }
                else
                {
                    newDotNetWasm = new TaskItem(asset);
                    Log.LogMessage(MessageImportance.Low, "Promoting asset '{0}' to Publish asset.", asset.ItemSpec);
                }

                ApplyPublishProperties(newDotNetWasm);
                nativeStaticWebAssets.Add(newDotNetWasm);
                if (resolvedNativeAssetToPublish.TryGetValue("dotnet.wasm", out var resolved))
                {
                    filesToRemove.Add(resolved);
                }
                continue;
            }
        }

        var compressedUpdatedFiles = ProcessCompressedAssets(compressedRepresentations, nativeAssets, updateMap);
        foreach (var f in compressedUpdatedFiles)
        {
            nativeStaticWebAssets.Add(f);
        }

        return nativeStaticWebAssets;

        static bool IsDotNetJs(string key)
        {
            var fileName = Path.GetFileName(key);
            return fileName.StartsWith("dotnet.", StringComparison.Ordinal) && fileName.EndsWith(".js", StringComparison.Ordinal) && !fileName.Contains("worker");
        }

        static bool IsDotNetWorkerJs(string key)
        {
            var fileName = Path.GetFileName(key);
            return fileName.StartsWith("dotnet.worker.", StringComparison.Ordinal) && fileName.EndsWith(".js", StringComparison.Ordinal);
        }

        static bool IsDotNetWasm(string key) => string.Equals("dotnet.wasm", Path.GetFileName(key), StringComparison.Ordinal);
    }

    private List<ITaskItem> ProcessSymbolAssets(
        Dictionary<string, ITaskItem> symbolAssets,
        Dictionary<string, ITaskItem> compressedRepresentations,
        Dictionary<string, ITaskItem> resolvedPublishFilesToRemove,
        Dictionary<string, ITaskItem> resolvedSymbolAssetToPublish,
        List<ITaskItem> filesToRemove)
    {
        var symbolStaticWebAssets = new List<ITaskItem>();
        var updateMap = new Dictionary<string, ITaskItem>();

        foreach (var kvp in symbolAssets)
        {
            var asset = kvp.Value;
            if (resolvedSymbolAssetToPublish.TryGetValue(Path.GetFileName(asset.GetMetadata("OriginalItemSpec")), out var existing))
            {
                if (!resolvedPublishFilesToRemove.TryGetValue(existing.ItemSpec, out var removed))
                {
                    // This is a symbol asset like classlibrary.pdb or similar that was not filtered and that needs to be updated
                    // to a publish asset.
                    var newAsset = new TaskItem(asset);
                    ApplyPublishProperties(newAsset);
                    symbolStaticWebAssets.Add(newAsset);
                    updateMap.Add(newAsset.ItemSpec, newAsset);
                    filesToRemove.Add(existing);
                    Log.LogMessage(MessageImportance.Low, "Promoting asset '{0}' to Publish asset.", asset.ItemSpec);
                }
                else
                {
                    // This was a file that was filtered, so just remove it, we don't need to add any publish static web asset
                    filesToRemove.Add(removed);

                    // Remove the file from the list to avoid double processing later when we process other files we filtered.
                    resolvedPublishFilesToRemove.Remove(existing.ItemSpec);
                }
            }
        }

        var compressedFiles = ProcessCompressedAssets(compressedRepresentations, symbolAssets, updateMap);

        foreach (var file in compressedFiles)
        {
            symbolStaticWebAssets.Add(file);
        }

        return symbolStaticWebAssets;
    }

    private List<ITaskItem> ComputeUpdatedAssemblies(
        IDictionary<(string, string assemblyName), ITaskItem> satelliteAssemblies,
        List<ITaskItem> filesToRemove,
        Dictionary<string, ITaskItem> resolvedAssembliesToPublish,
        Dictionary<string, ITaskItem> assemblyAssets,
        Dictionary<string, ITaskItem> satelliteAssemblyAssets,
        Dictionary<string, ITaskItem> compressedRepresentations)
    {
        // All assemblies, satellite assemblies and gzip files are initially defined as build assets.
        // We need to update them to publish assets when they haven't changed or when they have been linked.
        // For satellite assemblies and compressed files, we won't include them in the list of assets to update
        // when the original assembly they depend on has been linked out.
        var assetsToUpdate = new Dictionary<string, ITaskItem>();
        var linkedAssets = new Dictionary<string, ITaskItem>();

        foreach (var kvp in assemblyAssets)
        {
            var asset = kvp.Value;
            var fileName = Path.GetFileName(asset.GetMetadata("RelativePath"));
            if (IsWebCilEnabled)
                fileName = Path.ChangeExtension(fileName, ".dll");

            if (resolvedAssembliesToPublish.TryGetValue(fileName, out var existing))
            {
                // We found the assembly, so it'll have to be updated.
                assetsToUpdate.Add(asset.ItemSpec, asset);
                filesToRemove.Add(existing);
                if (!string.Equals(asset.ItemSpec, existing.GetMetadata("FullPath"), StringComparison.Ordinal))
                {
                    linkedAssets.Add(asset.ItemSpec, existing);
                }
            }
        }

        foreach (var kvp in satelliteAssemblyAssets)
        {
            var satelliteAssembly = kvp.Value;
            var relatedAsset = satelliteAssembly.GetMetadata("RelatedAsset");
            if (assetsToUpdate.ContainsKey(relatedAsset))
            {
                assetsToUpdate.Add(satelliteAssembly.ItemSpec, satelliteAssembly);
                var culture = satelliteAssembly.GetMetadata("AssetTraitValue");
                var fileName = Path.GetFileName(satelliteAssembly.GetMetadata("RelativePath"));
                if (IsWebCilEnabled)
                    fileName = Path.ChangeExtension(fileName, ".dll");

                if (satelliteAssemblies.TryGetValue((culture, fileName), out var existing))
                {
                    filesToRemove.Add(existing);
                }
                else
                {
                    var message = $"Can't find the original satellite assembly in the list of resolved files to " +
                        $"publish for asset '{satelliteAssembly.ItemSpec}'.";
                    throw new InvalidOperationException(message);
                }
            }
        }

        var compressedFiles = ProcessCompressedAssets(compressedRepresentations, assetsToUpdate, linkedAssets);

        foreach (var file in compressedFiles)
        {
            assetsToUpdate.Add(file.ItemSpec, file);
        }

        var updatedAssetsMap = new Dictionary<string, ITaskItem>(StringComparer.Ordinal);
        foreach (var asset in assetsToUpdate.Select(a => a.Value).OrderBy(a => a.GetMetadata("AssetRole"), Comparer<string>.Create(OrderByAssetRole)))
        {
            var assetTraitName = asset.GetMetadata("AssetTraitName");
            switch (assetTraitName)
            {
                case "WasmResource":
                    ITaskItem newAsemblyAsset = null;
                    if (linkedAssets.TryGetValue(asset.ItemSpec, out var linked))
                    {
                        newAsemblyAsset = new TaskItem(linked.GetMetadata("FullPath"), asset.CloneCustomMetadata());
                        newAsemblyAsset.SetMetadata("OriginalItemSpec", linked.ItemSpec);
                        Log.LogMessage(MessageImportance.Low, "Replacing asset '{0}' with linked version '{1}'",
                            asset.ItemSpec,
                            newAsemblyAsset.ItemSpec);
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.Low, "Linked asset not found for asset '{0}'", asset.ItemSpec);
                        newAsemblyAsset = new TaskItem(asset);
                    }
                    ApplyPublishProperties(newAsemblyAsset);

                    updatedAssetsMap.Add(asset.ItemSpec, newAsemblyAsset);
                    break;
                default:
                    // Satellite assembliess and compressed assets
                    var dependentAsset = new TaskItem(asset);
                    ApplyPublishProperties(dependentAsset);
                    UpdateRelatedAssetProperty(asset, dependentAsset, updatedAssetsMap);
                    Log.LogMessage(MessageImportance.Low, "Promoting asset '{0}' to Publish asset.", asset.ItemSpec);

                    updatedAssetsMap.Add(asset.ItemSpec, dependentAsset);
                    break;
            }
        }

        return updatedAssetsMap.Values.ToList();
    }

    private List<ITaskItem> ProcessCompressedAssets(
        Dictionary<string, ITaskItem> compressedRepresentations,
        Dictionary<string, ITaskItem> assetsToUpdate,
        Dictionary<string, ITaskItem> updatedAssets)
    {
        var processed = new List<string>();
        var runtimeAssetsToUpdate = new List<ITaskItem>();
        foreach (var kvp in compressedRepresentations)
        {
            var compressedAsset = kvp.Value;
            var relatedAsset = compressedAsset.GetMetadata("RelatedAsset");
            if (assetsToUpdate.ContainsKey(relatedAsset))
            {
                if (!updatedAssets.ContainsKey(relatedAsset))
                {
                    Log.LogMessage(MessageImportance.Low, "Related assembly for '{0}' was not updated and the compressed asset can be reused.", relatedAsset);
                    var newCompressedAsset = new TaskItem(compressedAsset);
                    ApplyPublishProperties(newCompressedAsset);
                    runtimeAssetsToUpdate.Add(newCompressedAsset);
                }
                else
                {
                    Log.LogMessage(MessageImportance.Low, "Related assembly for '{0}' was updated and the compressed asset will be discarded.", relatedAsset);
                }

                processed.Add(kvp.Key);
            }
        }

        // Remove all the elements we've found to avoid having to iterate over them when we process other assets.
        foreach (var element in processed)
        {
            compressedRepresentations.Remove(element);
        }

        return runtimeAssetsToUpdate;
    }

    private static void UpdateRelatedAssetProperty(ITaskItem asset, TaskItem newAsset, Dictionary<string, ITaskItem> updatedAssetsMap)
    {
        if (!updatedAssetsMap.TryGetValue(asset.GetMetadata("RelatedAsset"), out var updatedRelatedAsset))
        {
            throw new InvalidOperationException("Related asset not found.");
        }

        newAsset.SetMetadata("RelatedAsset", updatedRelatedAsset.ItemSpec);
    }

    private int OrderByAssetRole(string left, string right)
    {
        var leftScore = GetScore(left);
        var rightScore = GetScore(right);

        return leftScore - rightScore;

        static int GetScore(string candidate) => candidate switch
        {
            "Primary" => 0,
            "Related" => 1,
            "Alternative" => 2,
            _ => throw new InvalidOperationException("Invalid asset role"),
        };
    }

    private void ApplyPublishProperties(ITaskItem newAsemblyAsset)
    {
        newAsemblyAsset.SetMetadata("AssetKind", "Publish");
        newAsemblyAsset.SetMetadata("ContentRoot", Path.Combine(PublishPath, "wwwroot"));
        newAsemblyAsset.SetMetadata("CopyToOutputDirectory", "Never");
        newAsemblyAsset.SetMetadata("CopyToPublishDirectory", "PreserveNewest");
    }

    private void GroupExistingStaticWebAssets(
        Dictionary<string, ITaskItem> assemblyAssets,
        Dictionary<string, ITaskItem> nativeAssets,
        Dictionary<string, ITaskItem> satelliteAssemblyAssets,
        Dictionary<string, ITaskItem> symbolAssets,
        Dictionary<string, ITaskItem> compressedRepresentations)
    {
        foreach (var asset in ExistingAssets)
        {
            var traitName = asset.GetMetadata("AssetTraitName");
            if (IsWebAssemblyResource(traitName))
            {
                var traitValue = asset.GetMetadata("AssetTraitValue");
                if (IsRuntimeAsset(traitValue))
                {
                    assemblyAssets.Add(asset.ItemSpec, asset);
                }
                else if (IsNativeAsset(traitValue))
                {
                    nativeAssets.Add(asset.ItemSpec, asset);
                }
                else if (IsSymbolAsset(traitValue))
                {
                    symbolAssets.Add(asset.ItemSpec, asset);
                }
            }
            else if (IsCulture(traitName))
            {
                satelliteAssemblyAssets.Add(asset.ItemSpec, asset);
            }
            else if (IsAlternative(asset))
            {
                compressedRepresentations.Add(asset.ItemSpec, asset);
            }
        }
    }

    private void GroupResolvedFilesToPublish(
        Dictionary<string, ITaskItem> resolvedFilesToPublishToRemove,
        Dictionary<string, ITaskItem> resolvedAssemblyToPublish,
        Dictionary<(string, string), ITaskItem> satelliteAssemblyToPublish,
        Dictionary<string, ITaskItem> resolvedSymbolsToPublish,
        Dictionary<string, ITaskItem> resolvedNativeAssetToPublish)
    {
        var resolvedFilesToPublish = ResolvedFilesToPublish.ToList();
        if (AssetsComputingHelper.TryGetAssetFilename(CustomIcuCandidate, out string customIcuCandidateFilename))
        {
            var customIcuCandidate = AssetsComputingHelper.GetCustomIcuAsset(CustomIcuCandidate);
            resolvedFilesToPublish.Add(customIcuCandidate);
        }

        foreach (var candidate in resolvedFilesToPublish)
        {
            if (AssetsComputingHelper.ShouldFilterCandidate(candidate, TimeZoneSupport, InvariantGlobalization, CopySymbols, customIcuCandidateFilename, EnableThreads, out var reason))
            {
                Log.LogMessage(MessageImportance.Low, "Skipping asset '{0}' because '{1}'", candidate.ItemSpec, reason);
                if (!resolvedFilesToPublishToRemove.ContainsKey(candidate.ItemSpec))
                {
                    resolvedFilesToPublishToRemove.Add(candidate.ItemSpec, candidate);
                }
                else
                {
                    Log.LogMessage(MessageImportance.Low, "Duplicate candidate '{0}' found in ResolvedFilesToPublish", candidate.ItemSpec);
                }
                continue;
            }

            var extension = candidate.GetMetadata("Extension");
            if (string.Equals(extension, ".dll", StringComparison.Ordinal) || string.Equals(extension, ".webcil", StringComparison.Ordinal))
            {
                var culture = candidate.GetMetadata("Culture");
                var inferredCulture = candidate.GetMetadata("DestinationSubDirectory").Replace("\\", "/").Trim('/');
                if (!string.IsNullOrEmpty(culture) || !string.IsNullOrEmpty(inferredCulture))
                {
                    var finalCulture = !string.IsNullOrEmpty(culture) ? culture : inferredCulture;
                    var assemblyName = Path.GetFileName(candidate.GetMetadata("RelativePath").Replace("\\", "/"));
                    if (!satelliteAssemblyToPublish.ContainsKey((finalCulture, assemblyName)))
                    {
                        satelliteAssemblyToPublish.Add((finalCulture, assemblyName), candidate);
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.Low, "Duplicate candidate '{0}' found in ResolvedFilesToPublish", candidate.ItemSpec);
                    }
                    continue;
                }

                var candidateName = Path.GetFileName(candidate.GetMetadata("RelativePath"));
                if (!resolvedAssemblyToPublish.ContainsKey(candidateName))
                {
                    resolvedAssemblyToPublish.Add(candidateName, candidate);
                }
                else
                {
                    Log.LogMessage(MessageImportance.Low, "Duplicate candidate '{0}' found in ResolvedFilesToPublish", candidate.ItemSpec);
                }

                continue;
            }

            if (string.Equals(extension, ".pdb", StringComparison.Ordinal))
            {
                var candidateName = Path.GetFileName(candidate.GetMetadata("RelativePath"));
                if (!resolvedSymbolsToPublish.ContainsKey(candidateName))
                {
                    resolvedSymbolsToPublish.Add(candidateName, candidate);
                }
                else
                {
                    Log.LogMessage(MessageImportance.Low, "Duplicate candidate '{0}' found in ResolvedFilesToPublish", candidate.ItemSpec);
                }

                continue;
            }

            // Capture all the native unfiltered assets since we need to process them to determine what static web assets need to get
            // upgraded
            if (string.Equals(candidate.GetMetadata("AssetType"), "native", StringComparison.Ordinal))
            {
                var candidateName = $"{candidate.GetMetadata("FileName")}{extension}";
                if (!resolvedNativeAssetToPublish.ContainsKey(candidateName))
                {
                    resolvedNativeAssetToPublish.Add(candidateName, candidate);
                }
                else
                {
                    Log.LogMessage(MessageImportance.Low, "Duplicate candidate '{0}' found in ResolvedFilesToPublish", candidate.ItemSpec);
                }
                continue;
            }
        }
    }

    private static bool IsNativeAsset(string traitValue) => string.Equals(traitValue, "native", StringComparison.Ordinal);

    private static bool IsRuntimeAsset(string traitValue) => string.Equals(traitValue, "runtime", StringComparison.Ordinal);
    private static bool IsSymbolAsset(string traitValue) => string.Equals(traitValue, "symbol", StringComparison.Ordinal);

    private static bool IsAlternative(ITaskItem asset) => string.Equals(asset.GetMetadata("AssetRole"), "Alternative", StringComparison.Ordinal);

    private static bool IsCulture(string traitName) => string.Equals(traitName, "Culture", StringComparison.Ordinal);

    private static bool IsWebAssemblyResource(string traitName) => string.Equals(traitName, "WasmResource", StringComparison.Ordinal);
}
