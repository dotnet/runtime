// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Re-aligns the Webcil payload of Webcil-in-Wasm modules in place after a tool such as wasm-opt rewrote them.
/// </summary>
public class AlignWebcilPayload : Task
{
    [Required]
    public ITaskItem[] Files { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        foreach (ITaskItem file in Files)
        {
            try
            {
                WebcilWasmPayloadAlignment.AlignPayload(file.ItemSpec);
            }
            catch (Exception ex) when (ex is BadImageFormatException or InvalidOperationException or IOException)
            {
                Log.LogError($"Could not align the Webcil payload in '{file.ItemSpec}': {ex.Message}");
            }
        }

        return !Log.HasLoggedErrors;
    }
}
