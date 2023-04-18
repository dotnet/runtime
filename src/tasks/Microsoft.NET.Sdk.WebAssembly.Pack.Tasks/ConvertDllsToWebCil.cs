// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ResourceHashesByNameDictionary = System.Collections.Generic.Dictionary<string, string>;

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

        for (int i = 0; i < Candidates.Length; i++)
        {
            var candidate = Candidates[i];

            var extension = candidate.GetMetadata("Extension");
            var filePath = candidate.ItemSpec;

            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);

            if (extension == ".dll")
            {
                var tmpWebcil = Path.GetTempFileName();
                var webcilWriter = Microsoft.WebAssembly.Build.Tasks.WebcilConverter.FromPortableExecutable(inputPath: filePath, outputPath: tmpWebcil, logger: Log);
                webcilWriter.ConvertToWebcil();

                var finalWebcil = Path.Combine(OutputPath, Path.GetFileNameWithoutExtension(filePath) + ".webcil");
                if (Utils.CopyIfDifferent(tmpWebcil, finalWebcil, useHash: true))
                    Log.LogMessage(MessageImportance.Low, $"Generated {finalWebcil} .");
                else
                    Log.LogMessage(MessageImportance.Low, $"Skipped generating {finalWebcil} as the contents are unchanged.");

                _fileWrites.Add(finalWebcil);

                var webcilItem = new TaskItem(finalWebcil, candidate.CloneCustomMetadata());
                webcilItem.SetMetadata("RelativePath", Path.ChangeExtension(candidate.GetMetadata("RelativePath"), ".webcil"));
                webcilItem.SetMetadata("AssetTraitName", "WasmResource");
                webcilItem.SetMetadata("AssetTraitValue", "runtime");
                webcilItem.SetMetadata("OriginalItemSpec", finalWebcil);

                webCilCandidates.Add(webcilItem);
            }
            else
            {
                webCilCandidates.Add(candidate);
            }
        }

        WebCilCandidates = webCilCandidates.ToArray();
        return true;
    }
}
