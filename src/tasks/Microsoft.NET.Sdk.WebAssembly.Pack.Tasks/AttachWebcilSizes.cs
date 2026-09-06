// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.NET.Sdk.WebAssembly;

// Reads payloadSize/tableSize from already-produced webcil-in-wasm images and attaches them as
// PayloadSize/TableSize metadata on the corresponding resource items, so GenerateWasmBootJson can
// emit them into the boot config without opening the files itself. Non-webcil resources pass
// through unchanged. This runs on every boot-config generation (only the small size header of each
// webcil is read), so the metadata is always present even on incremental builds where
// ConvertDllsToWebcil was skipped for unchanged assemblies.
public class AttachWebcilSizes : Task
{
    [Required]
    public ITaskItem[] Candidates { get; set; }

    [Output]
    public ITaskItem[] CandidatesWithSizes { get; set; }

    public override bool Execute()
    {
        var result = new ITaskItem[Candidates.Length];
        for (int i = 0; i < Candidates.Length; i++)
        {
            ITaskItem candidate = Candidates[i];
            if (!IsWebcilInWasm(candidate))
            {
                result[i] = candidate;
                continue;
            }

            // OriginalItemSpec is the produced ".wasm" path; fall back to the item spec.
            string path = candidate.GetMetadata("OriginalItemSpec");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                path = candidate.ItemSpec;

            if (!WebcilReader.TryReadWebcilInWasmSizes(path, out int payloadSize, out int tableSize, out string failureReason) || payloadSize <= 0)
            {
                Log.LogError($"Could not read the Webcil payload/table sizes from '{path}' ({failureReason}). The runtime loader requires payloadSize for every webcil-in-wasm assembly.");
                return false;
            }

            var item = new TaskItem(candidate);
            item.SetMetadata("PayloadSize", payloadSize.ToString(CultureInfo.InvariantCulture));
            if (tableSize > 0)
                item.SetMetadata("TableSize", tableSize.ToString(CultureInfo.InvariantCulture));
            result[i] = item;
        }

        CandidatesWithSizes = result;
        return !Log.HasLoggedErrors;
    }

    // A webcil-in-wasm assembly is a ".wasm" resource that is not the native runtime wasm
    // (dotnet.native.wasm), which is identified by its "WasmResource"/"native" asset trait.
    private static bool IsWebcilInWasm(ITaskItem candidate)
    {
        if (!string.Equals(candidate.GetMetadata("Extension"), ".wasm", StringComparison.OrdinalIgnoreCase))
            return false;

        return !(string.Equals(candidate.GetMetadata("AssetTraitName"), "WasmResource", StringComparison.OrdinalIgnoreCase)
                 && string.Equals(candidate.GetMetadata("AssetTraitValue"), "native", StringComparison.OrdinalIgnoreCase));
    }
}
