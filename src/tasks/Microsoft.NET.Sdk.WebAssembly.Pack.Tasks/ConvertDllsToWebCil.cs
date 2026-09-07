// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.NET.Sdk.WebAssembly;

public class ConvertDllsToWebcil : Task
{
    [Required]
    public ITaskItem[] Candidates { get; set; }

    [Required]
    public string OutputPath { get; set; }

    [Required]
    public string IntermediateOutputPath { get; set; }

    [Required]
    public bool IsEnabled { get; set; }

    public string? ConversionStamp { get; set; }

    /// <summary>
    /// Directory holding prebuilt R2R webcil-in-wasm images (CoreCLR browser). For a managed non-culture
    /// .dll candidate with empty <c>R2RWebcilPath</c>, the matching image is staged instead of
    /// converting the IL. Empty for Mono.
    /// </summary>
    public string? PrebuiltR2RDirectory { get; set; }

    public int WebcilVersion { get; set; }

    [Output]
    public ITaskItem[] WebcilCandidates { get; set; }

    /// <summary>
    /// Files from shared locations (runtime pack, NuGet cache) that need Framework
    /// SourceType materialization to get unique per-project Identity.
    /// When <see cref="IsEnabled"/> is true, this is non-DLL items without
    /// WasmNativeBuildOutput metadata (DLLs are converted to webcil, making them
    /// per-project already). When <see cref="IsEnabled"/> is false, DLLs are also
    /// included since they retain their shared paths without conversion.
    /// Items with WasmNativeBuildOutput metadata are always excluded — they're
    /// already unique per project.
    /// </summary>
    [Output]
    public ITaskItem[] PassThroughCandidates { get; set; }

    protected readonly List<string> _fileWrites = new();
    protected readonly List<string> _filesToTouch = new();

    [Output]
    public string[]? FileWrites => _fileWrites.ToArray();

    [Output]
    public string[]? FilesToTouch => _filesToTouch.ToArray();

    public override bool Execute()
    {
        var webcilCandidates = new List<ITaskItem>();
        var passThroughCandidates = new List<ITaskItem>();

        if (!IsEnabled)
        {
            // When webcil is disabled, no conversion occurs. All candidates pass
            // through unchanged as WebcilCandidates (backward compat for publish).
            // All candidates (DLLs and non-DLLs) without WasmNativeBuildOutput
            // metadata are also pass-through candidates for Framework materialization.
            // Unlike the enabled path (where DLLs are converted to webcil and become
            // per-project), disabled DLLs retain their shared NuGet cache paths and
            // need materialization to get unique per-project Identity.
            WebcilCandidates = Candidates;
            foreach (var candidate in Candidates)
            {
                if (string.IsNullOrEmpty(candidate.GetMetadata("WasmNativeBuildOutput")))
                {
                    passThroughCandidates.Add(candidate);
                }
            }
            PassThroughCandidates = passThroughCandidates.ToArray();
            return true;
        }

        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);

        string tmpDir = Path.Combine(IntermediateOutputPath, Guid.NewGuid().ToString("N"));
        if (!Directory.Exists(tmpDir))
            Directory.CreateDirectory(tmpDir);

        for (int i = 0; i < Candidates.Length; i++)
        {
            var candidate = Candidates[i];
            var extension = candidate.GetMetadata("Extension");

            if (extension != ".dll")
            {
                // Non-DLL files always appear in WebcilCandidates (backward compat
                // for publish and other callers that only consume WebcilCandidates).
                webcilCandidates.Add(candidate);

                // Additionally classify shared framework files as pass-throughs.
                // Items with WasmNativeBuildOutput metadata are per-project native
                // build outputs (e.g. dotnet.native.wasm from obj/wasm/for-build/)
                // that don't need Framework materialization.
                bool isNativeBuildOutput = !string.IsNullOrEmpty(candidate.GetMetadata("WasmNativeBuildOutput"));
                if (!isNativeBuildOutput)
                {
                    passThroughCandidates.Add(candidate);
                }
                continue;
            }

            try
            {
                TaskItem webcilItem = ConvertDll(tmpDir, candidate);
                webcilCandidates.Add(webcilItem);
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to convert '{candidate.ItemSpec}' to webcil: {ex.Message}");
                return false;
            }
        }

        Directory.Delete(tmpDir, true);

        WebcilCandidates = webcilCandidates.ToArray();
        PassThroughCandidates = passThroughCandidates.ToArray();
        return true;
    }

    private TaskItem ConvertDll(string tmpDir, ITaskItem candidate)
    {
        var dllFilePath = candidate.ItemSpec;
        var webcilFileName = Path.GetFileNameWithoutExtension(dllFilePath) + Utils.WebcilInWasmExtension;
        bool isCulture = candidate.GetMetadata("AssetTraitName") == "Culture";
        string culture = isCulture ? candidate.GetMetadata("AssetTraitValue") : null;
        string candidatePath = isCulture
            ? Path.Combine(OutputPath, culture)
            : OutputPath;

        string finalWebcil = Path.Combine(candidatePath, webcilFileName);

        // A prebuilt R2R webcil-in-wasm image from the runtime pack replaces conversion of the .dll:
        // stage (copy) it into the webcil output so it flows through the same downstream metadata as a
        // converted assembly, but carries native code. The .dll is kept only as the metadata source.
        string r2rWebcilPath = candidate.GetMetadata("R2RWebcilPath");
        bool forceWebcilConversion = !string.IsNullOrEmpty(ConversionStamp)
            && File.Exists(ConversionStamp)
            && Utils.IsNewerThan(ConversionStamp, finalWebcil);
        if (string.IsNullOrEmpty(r2rWebcilPath) && !isCulture && !string.IsNullOrEmpty(PrebuiltR2RDirectory))
        {
            string assemblyName = Path.GetFileNameWithoutExtension(dllFilePath);

            // Probe .dll before .wasm: per-app crossgen (--out:<name>.dll) writes the R2R image to
            // <name>.dll and can leave a same-named <name>.wasm that is NOT it. Pack images are
            // <name>.wasm with no sibling .dll, so they still resolve.
            string candidateDll = Path.Combine(PrebuiltR2RDirectory, assemblyName + ".dll");
            string candidateWasm = Path.Combine(PrebuiltR2RDirectory, assemblyName + Utils.WebcilInWasmExtension);
            string prebuilt = File.Exists(candidateDll) ? candidateDll
                            : File.Exists(candidateWasm) ? candidateWasm
                            : null;

            // Reuse the prebuilt image only for assemblies with IL and when its assembly version matches
            // the candidate. No-IL assemblies and app-local assemblies that shadow a framework assembly
            // fall through to regular WebCIL conversion.
            if (prebuilt != null)
            {
                if (PrebuiltR2RMatchesCandidate(dllFilePath, prebuilt, out bool candidateHasILCode))
                {
                    r2rWebcilPath = prebuilt;
                }
                else if (candidateHasILCode)
                {
                    forceWebcilConversion = true;
                }
            }
        }
        bool outputHasExpectedFlavor = OutputHasExpectedWebcilFlavor(finalWebcil, useR2R: !string.IsNullOrEmpty(r2rWebcilPath));
        if (!string.IsNullOrEmpty(r2rWebcilPath))
        {
            if (forceWebcilConversion || !outputHasExpectedFlavor || Utils.IsNewerThan(r2rWebcilPath, finalWebcil))
            {
                if (!Directory.Exists(candidatePath))
                    Directory.CreateDirectory(candidatePath);

                // Copy (not move): the runtime pack's native/*.wasm is a shared source that must survive staging.
                if (Utils.CopyIfDifferent(r2rWebcilPath, finalWebcil, useHash: false))
                    Log.LogMessage(MessageImportance.Low, $"Staged prebuilt R2R webcil {finalWebcil} from {r2rWebcilPath} .");
                else
                    Log.LogMessage(MessageImportance.Low, $"Skipped staging {finalWebcil} as the contents are unchanged.");
                _filesToTouch.Add(finalWebcil);
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, $"Skipping {r2rWebcilPath} as it is older than the output file {finalWebcil}");
            }
        }
        else if (forceWebcilConversion || !outputHasExpectedFlavor || Utils.IsNewerThan(dllFilePath, finalWebcil))
        {
            var tmpWebcil = Path.Combine(tmpDir, webcilFileName);
            var logAdapter = new Microsoft.WebAssembly.Build.Tasks.LogAdapter(Log);
            var webcilWriter = Microsoft.WebAssembly.Build.Tasks.WebcilConverter.FromPortableExecutable(inputPath: dllFilePath, outputPath: tmpWebcil, logger: logAdapter, webcilVersion: WebcilVersion);
            webcilWriter.ConvertToWebcil();

            if (!Directory.Exists(candidatePath))
                Directory.CreateDirectory(candidatePath);

            if (Utils.MoveIfDifferent(tmpWebcil, finalWebcil))
                Log.LogMessage(MessageImportance.Low, $"Generated {finalWebcil} .");
            else
                Log.LogMessage(MessageImportance.Low, $"Skipped generating {finalWebcil} as the contents are unchanged.");
            _filesToTouch.Add(finalWebcil);
        }
        else
        {
            Log.LogMessage(MessageImportance.Low, $"Skipping {dllFilePath} as it is older than the output file {finalWebcil}");
        }

        _fileWrites.Add(finalWebcil);

        var webcilItem = new TaskItem(finalWebcil, candidate.CloneCustomMetadata());
        webcilItem.SetMetadata("RelativePath", Path.ChangeExtension(candidate.GetMetadata("RelativePath"), Utils.WebcilInWasmExtension));
        webcilItem.SetMetadata("OriginalItemSpec", finalWebcil);

        if (webcilItem.GetMetadata("AssetTraitName") == "Culture")
        {
            string relatedAsset = webcilItem.GetMetadata("RelatedAsset");
            relatedAsset = Path.ChangeExtension(relatedAsset, Utils.WebcilInWasmExtension);
            webcilItem.SetMetadata("RelatedAsset", relatedAsset);
            Log.LogMessage(MessageImportance.Low, $"Changing related asset of {webcilItem} to {relatedAsset}.");
        }

        return webcilItem;
    }

    private static bool OutputHasExpectedWebcilFlavor(string path, bool useR2R)
    {
        if (!File.Exists(path))
            return false;

        using FileStream stream = File.OpenRead(path);
        return WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int tableSize, out _)
            && useR2R == (tableSize > 0);
    }

    private bool PrebuiltR2RMatchesCandidate(string candidateDllPath, string prebuiltImagePath, out bool candidateHasILCode)
    {
        if (IsR2RWebcil(candidateDllPath))
        {
            candidateHasILCode = true;
            return true;
        }

        candidateHasILCode = AssemblyHasILCode(candidateDllPath, out Version candidateVersion);
        if (!candidateHasILCode)
        {
            Log.LogMessage(MessageImportance.Low,
                $"Not staging prebuilt R2R image '{prebuiltImagePath}' for no-IL assembly '{candidateDllPath}'; converting it to WebCIL instead.");
            return false;
        }

        Version prebuiltVersion = TryReadAssemblyVersion(prebuiltImagePath);

        // If the prebuilt identity is unreadable, keep it (prior behavior) so the common case where
        // every candidate is the pack's own assembly is never regressed.
        if (prebuiltVersion is null || candidateVersion.Equals(prebuiltVersion))
            return true;

        Log.LogMessage(MessageImportance.Normal,
            $"Not staging prebuilt R2R image '{prebuiltImagePath}' (v{prebuiltVersion}) for '{candidateDllPath}' (v{candidateVersion}): assembly version mismatch; converting IL instead.");
        return false;
    }

    private static bool IsR2RWebcil(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return WebcilReader.TryReadWebcilInWasmSizes(stream, out _, out int tableSize, out _)
            && tableSize > 0;
    }

    private static bool AssemblyHasILCode(string path, out Version version)
    {
        using FileStream stream = File.OpenRead(path);
        using var peReader = new PEReader(stream);
        MetadataReader metadataReader = peReader.GetMetadataReader();
        version = metadataReader.GetAssemblyDefinition().Version;

        foreach (MethodDefinitionHandle methodDefinitionHandle in metadataReader.MethodDefinitions)
        {
            if (metadataReader.GetMethodDefinition(methodDefinitionHandle).RelativeVirtualAddress > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static Version TryReadAssemblyVersion(string path)
    {
        try
        {
            using FileStream stream = File.OpenRead(path);
            if (path.EndsWith(Utils.WebcilInWasmExtension, StringComparison.OrdinalIgnoreCase))
            {
                using var webcilReader = new WebcilReader(stream, path);
                return webcilReader.GetMetadataReader().GetAssemblyDefinition().Version;
            }

            using var peReader = new PEReader(stream);
            return peReader.GetMetadataReader().GetAssemblyDefinition().Version;
        }
        catch
        {
            return null;
        }
    }
}
