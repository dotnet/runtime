// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Writes the WebAssembly tool-conventions <c>producers</c> and <c>build_id</c> custom sections
/// into a linked <c>dotnet.native.wasm</c> module.
///
/// See:
///   - https://github.com/WebAssembly/tool-conventions/blob/main/ProducersSection.md
///   - https://github.com/WebAssembly/tool-conventions/blob/main/BuildId.md
/// </summary>
public class WasmWriteMetadataSections : Task
{
    /// <summary>Path to the WebAssembly module to modify in place.</summary>
    [Required]
    public string WasmModulePath { get; set; } = string.Empty;

    /// <summary>Runtime engine name used for the producers <c>processed-by</c> field (e.g. Mono, CoreCLR).</summary>
    public string RuntimeName { get; set; } = string.Empty;

    /// <summary>Runtime/product version used for the producers <c>processed-by</c> and <c>sdk</c> fields.</summary>
    public string ProductVersion { get; set; } = string.Empty;

    /// <summary>When true, the <c>producers</c> section is (re)written with .NET tool information.</summary>
    public bool EmitProducersSection { get; set; } = true;

    /// <summary>
    /// Optional build id value as a hex string (with or without a leading <c>0x</c>). When set, a
    /// <c>build_id</c> section carrying these raw bytes is written. Leave empty to skip (for example
    /// when the id was already emitted by the linker via <c>--build-id</c>).
    /// </summary>
    public string BuildId { get; set; } = string.Empty;

    public override bool Execute()
    {
        if (!File.Exists(WasmModulePath))
        {
            Log.LogError($"'{nameof(WasmModulePath)}={WasmModulePath}' does not exist");
            return false;
        }

        List<WasmCustomSectionWriter.ProducerValue>? producers = null;
        if (EmitProducersSection)
        {
            producers = new List<WasmCustomSectionWriter.ProducerValue>
            {
                new(WasmCustomSectionWriter.ProducersFieldLanguage, "C#", string.Empty),
            };

            if (!string.IsNullOrEmpty(RuntimeName))
                producers.Add(new(WasmCustomSectionWriter.ProducersFieldProcessedBy, RuntimeName, ProductVersion));

            producers.Add(new(WasmCustomSectionWriter.ProducersFieldSdk, ".NET", ProductVersion));
        }

        byte[]? buildIdBytes = null;
        if (!string.IsNullOrEmpty(BuildId))
        {
            if (!TryParseHex(BuildId, out buildIdBytes))
            {
                Log.LogError($"'{nameof(BuildId)}={BuildId}' is not a valid hex string");
                return false;
            }
        }

        if (producers is null && buildIdBytes is null)
            return true;

        try
        {
            WasmCustomSectionWriter.WriteMetadata(WasmModulePath, producers, buildIdBytes);
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to write metadata sections into '{WasmModulePath}': {ex.Message}");
            return false;
        }

        return !Log.HasLoggedErrors;
    }

    private static bool TryParseHex(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        string hex = value.Trim();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex.Substring(2);

        if (hex.Length == 0 || (hex.Length % 2) != 0)
            return false;

        var result = new byte[hex.Length / 2];
        for (int i = 0; i < result.Length; i++)
        {
#if NETFRAMEWORK
            if (!byte.TryParse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result[i]))
#else
            if (!byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result[i]))
#endif
                return false;
        }

        bytes = result;
        return true;
    }
}
