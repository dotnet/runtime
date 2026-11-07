// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.NET.Sdk.WebAssembly;

public sealed class WasmStampBuildId : Task
{
    [Required]
    public ITaskItem InputWasm { get; set; }

    [Required]
    public string OutputPath { get; set; }

    [Required]
    public string BuildId { get; set; }

    [Output]
    public ITaskItem StampedWasm { get; private set; }

    [Output]
    public ITaskItem[] FileWrites { get; private set; }

    public override bool Execute()
    {
        string? temporaryPath = null;
        try
        {
            byte[] buildId = WasmCustomSectionWriter.ParseBuildId(BuildId)
                ?? throw new ArgumentException($"WasmNativeBuildId '{BuildId}' is not a valid hex string.");
            byte[] module = File.ReadAllBytes(InputWasm.ItemSpec);
            byte[] output = WasmCustomSectionWriter.ReplaceBuildId(module, buildId);

            string outputDirectory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            temporaryPath = OutputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllBytes(temporaryPath, output);
            if (!Utils.MoveIfDifferent(temporaryPath, OutputPath))
                File.Delete(temporaryPath);

            StampedWasm = new TaskItem(OutputPath, InputWasm.CloneCustomMetadata());
            FileWrites = [new TaskItem(OutputPath)];
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
        }
        finally
        {
            if (temporaryPath is not null && File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        return !Log.HasLoggedErrors;
    }
}
