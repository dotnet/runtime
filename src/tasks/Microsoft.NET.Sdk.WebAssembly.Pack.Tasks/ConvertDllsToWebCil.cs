// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using WasmAppBuilder;

namespace Microsoft.NET.Sdk.WebAssembly;

public class ConvertDllsToWebCil : Task
{
    [Required]
    public ITaskItem[] Candidates { get; set; }

    [Required]
    public string OutputPath { get; set; }

    [Required]
    public bool IsEnabled { get; set; }

    [Output]
    public ITaskItem[] WebCilCandidates { get; set; }

    protected readonly List<string> _fileWrites = new();

    [Output]
    public string[]? FileWrites => _fileWrites.ToArray();

    public override bool Execute()
    {
        var webCilCandidates = new List<ITaskItem>();

        if (!IsEnabled)
        {
            WebCilCandidates = Candidates;
            return true;
        }

        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);

        for (int i = 0; i < Candidates.Length; i++)
        {
            var candidate = Candidates[i];
            var extension = candidate.GetMetadata("Extension");

            if (extension != ".dll")
            {
                webCilCandidates.Add(candidate);
                continue;
            }

            try
            {
                TaskItem webcilItem = ConvertDll(candidate);
                webCilCandidates.Add(webcilItem);
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to convert '{candidate.ItemSpec}' to webcil: {ex.Message}");
                return false;
            }
        }

        WebCilCandidates = webCilCandidates.ToArray();
        return true;
    }

    private TaskItem ConvertDll(ITaskItem candidate)
    {
        var dllFilePath = candidate.ItemSpec;
        var webcilFileName = Path.GetFileNameWithoutExtension(dllFilePath) + Utils.WebcilInWasmExtension;
        string candidatePath = candidate.GetMetadata("AssetTraitName") == "Culture"
            ? Path.Combine(OutputPath, candidate.GetMetadata("AssetTraitValue"))
            : OutputPath;

        string finalWebcil = Path.Combine(candidatePath, webcilFileName);

        if (Utils.IsNewerThan(dllFilePath, finalWebcil))
        {
            var logAdapter = new LogAdapter(Log);
            var webcilWriter = Microsoft.WebAssembly.Build.Tasks.WebcilConverter.FromPortableExecutable(logger: logAdapter, inputPath: dllFilePath);
            var webcilData = webcilWriter.ConvertToWebcilBytes();

            if (!Directory.Exists(candidatePath))
                Directory.CreateDirectory(candidatePath);

            ArtifactWriter.PersistFileIfChanged(Log, webcilData, finalWebcil);
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
