// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using WasmAppBuilder;

namespace Microsoft.NET.Sdk.WebAssembly;

public class ConvertDllsToWebCIL : Task
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
    public ITaskItem[] WebCILCandidates { get; set; }

    protected readonly List<string> _fileWrites = new();

    [Output]
    public string[]? FileWrites => _fileWrites.ToArray();

    public override bool Execute()
    {
        var webCILCandidates = new List<ITaskItem>();

        if (!IsEnabled)
        {
            WebCILCandidates = Candidates;
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
                webCILCandidates.Add(candidate);
                continue;
            }

            try
            {
                TaskItem webcilItem = ConvertDll(tmpDir, candidate);
                webCILCandidates.Add(webcilItem);
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to convert '{candidate.ItemSpec}' to webcil: {ex.Message}");
                return false;
            }
        }

        Directory.Delete(tmpDir, true);

        WebCILCandidates = webCILCandidates.ToArray();
        return true;
    }

    private TaskItem ConvertDll(string tmpDir, ITaskItem candidate)
    {
        var dllFilePath = candidate.ItemSpec;
        var webcilFileName = Path.GetFileNameWithoutExtension(dllFilePath) + Utils.WebCILInWasmExtension;
        string candidatePath = candidate.GetMetadata("AssetTraitName") == "Culture"
            ? Path.Combine(OutputPath, candidate.GetMetadata("AssetTraitValue"))
            : OutputPath;

        string finalWebCIL = Path.Combine(candidatePath, webcilFileName);

        if (Utils.IsNewerThan(dllFilePath, finalWebCIL))
        {
            var tmpWebCIL = Path.Combine(tmpDir, webcilFileName);
            var logAdapter = new LogAdapter(Log);
            var webcilWriter = Microsoft.WebAssembly.Build.Tasks.WebCILConverter.FromPortableExecutable(inputPath: dllFilePath, outputPath: tmpWebCIL, logger: logAdapter);
            webcilWriter.ConvertToWebCIL();

            if (!Directory.Exists(candidatePath))
                Directory.CreateDirectory(candidatePath);

            if (Utils.MoveIfDifferent(tmpWebCIL, finalWebCIL))
                Log.LogMessage(MessageImportance.Low, $"Generated {finalWebCIL} .");
            else
                Log.LogMessage(MessageImportance.Low, $"Skipped generating {finalWebCIL} as the contents are unchanged.");
        }
        else
        {
            Log.LogMessage(MessageImportance.Low, $"Skipping {dllFilePath} as it is older than the output file {finalWebCIL}");
        }

        _fileWrites.Add(finalWebCIL);

        var webcilItem = new TaskItem(finalWebCIL, candidate.CloneCustomMetadata());
        webcilItem.SetMetadata("RelativePath", Path.ChangeExtension(candidate.GetMetadata("RelativePath"), Utils.WebCILInWasmExtension));
        webcilItem.SetMetadata("OriginalItemSpec", finalWebCIL);

        if (webcilItem.GetMetadata("AssetTraitName") == "Culture")
        {
            string relatedAsset = webcilItem.GetMetadata("RelatedAsset");
            relatedAsset = Path.ChangeExtension(relatedAsset, Utils.WebCILInWasmExtension);
            webcilItem.SetMetadata("RelatedAsset", relatedAsset);
            Log.LogMessage(MessageImportance.Low, $"Changing related asset of {webcilItem} to {relatedAsset}.");
        }

        return webcilItem;
    }
}
