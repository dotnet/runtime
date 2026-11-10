// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.WebAssembly.Build.Tasks;

public sealed class WasmRegenerateBuildId : Task
{
    [Required]
    public string WasmPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        string temporaryPath = WasmPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            byte[] output = WasmCustomSectionWriter.RegenerateBuildId(File.ReadAllBytes(WasmPath));
            File.WriteAllBytes(temporaryPath, output);
            if (!Utils.MoveIfDifferent(temporaryPath, WasmPath))
                File.Delete(temporaryPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
