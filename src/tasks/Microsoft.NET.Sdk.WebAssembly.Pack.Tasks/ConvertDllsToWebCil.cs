// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using WasmAppBuilder;

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

    [Output]
    public ITaskItem[] WebcilCandidates { get; set; }

    /// <summary>
    /// Non-DLL files from shared locations (runtime pack, NuGet cache) that were not
    /// converted to webcil. These are candidates for Framework SourceType materialization.
    /// When IsEnabled is false, all candidates (DLL and non-DLL) appear here.
    /// Items with WasmNativeBuildOutput metadata (per-project native build outputs)
    /// are excluded — they're already unique per project.
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
            // through unchanged. All are also pass-through candidates since none
            // were converted to webcil.
            WebcilCandidates = Candidates;
            PassThroughCandidates = Candidates;
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
            var logAdapter = new LogAdapter(Log);
            var webcilWriter = Microsoft.WebAssembly.Build.Tasks.WebcilConverter.FromPortableExecutable(inputPath: dllFilePath, outputPath: tmpWebcil, logger: logAdapter);
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
