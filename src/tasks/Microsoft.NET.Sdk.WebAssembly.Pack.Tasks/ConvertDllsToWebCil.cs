// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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

    [Output]
    public string[]? FileWrites => _fileWrites.ToArray();

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
        string candidatePath = candidate.GetMetadata("AssetTraitName") == "Culture"
            ? Path.Combine(OutputPath, candidate.GetMetadata("AssetTraitValue"))
            : OutputPath;

        string finalWebcil = Path.Combine(candidatePath, webcilFileName);

        if (Utils.IsNewerThan(dllFilePath, finalWebcil))
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
}
