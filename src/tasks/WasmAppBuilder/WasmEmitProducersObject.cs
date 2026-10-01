// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Emits a small relocatable WebAssembly object carrying the tool-conventions <c>producers</c>
/// custom section with .NET toolchain information. The object is meant to be passed as a link
/// input so that <c>wasm-ld</c> merges it into <c>dotnet.native.wasm</c>, alongside the entries
/// clang/LLVM already contribute. This keeps <c>producers</c> linker-driven, matching how
/// <c>build_id</c> is emitted by the linker via <c>--build-id</c>.
///
/// See: https://github.com/WebAssembly/tool-conventions/blob/main/ProducersSection.md
/// </summary>
public class WasmEmitProducersObject : Task
{
    /// <summary>Path of the object file to write.</summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>Runtime engine name used for the producers <c>processed-by</c> field (e.g. Mono, CoreCLR).</summary>
    public string RuntimeName { get; set; } = string.Empty;

    /// <summary>Runtime/product version used for the producers <c>processed-by</c> and <c>sdk</c> fields.</summary>
    public string ProductVersion { get; set; } = string.Empty;

    public override bool Execute()
    {
        var producers = new List<WasmCustomSectionWriter.ProducerValue>
        {
            new(WasmCustomSectionWriter.ProducersFieldLanguage, "C#", string.Empty),
        };

        if (!string.IsNullOrEmpty(RuntimeName))
            producers.Add(new(WasmCustomSectionWriter.ProducersFieldProcessedBy, RuntimeName, ProductVersion));

        producers.Add(new(WasmCustomSectionWriter.ProducersFieldSdk, ".NET", ProductVersion));

        try
        {
            string? dir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            byte[] bytes = WasmCustomSectionWriter.BuildProducersObject(producers);

            // Avoid rewriting an identical object so the file timestamp stays stable and the native
            // link (which consumes this object) is not forced to run on every incremental build.
            if (File.Exists(OutputPath) && ByteArraysEqual(File.ReadAllBytes(OutputPath), bytes))
                return true;

            File.WriteAllBytes(OutputPath, bytes);
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to write producers object '{OutputPath}': {ex.Message}");
            return false;
        }

        return !Log.HasLoggedErrors;
    }

    private static bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }
}
