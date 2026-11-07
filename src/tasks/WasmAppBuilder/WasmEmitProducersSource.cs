// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Emits WebAssembly assembly source carrying the tool-conventions <c>producers</c> custom section
/// with .NET toolchain information. Emscripten assembles the source while linking and
/// <c>wasm-ld</c> merges it with producer entries contributed by clang/LLVM.
///
/// See: https://github.com/WebAssembly/tool-conventions/blob/main/ProducersSection.md
/// </summary>
public class WasmEmitProducersSource : Task
{
    /// <summary>Path of the assembly source file to write.</summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>Runtime engine name used for the producers <c>processed-by</c> field (e.g. Mono, CoreCLR).</summary>
    public string RuntimeName { get; set; } = string.Empty;

    /// <summary>Runtime/product version used for the producers <c>processed-by</c> and <c>sdk</c> fields.</summary>
    public string ProductVersion { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            string? directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            byte[] bytes = Encoding.UTF8.GetBytes(BuildSource());

            // Avoid rewriting identical source so a native link that consumes it remains incremental.
            if (File.Exists(OutputPath) && ByteArraysEqual(File.ReadAllBytes(OutputPath), bytes))
                return true;

            File.WriteAllBytes(OutputPath, bytes);
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }

        return !Log.HasLoggedErrors;
    }

    private string BuildSource()
    {
        var source = new StringBuilder();
        source.Append(".section .custom_section.producers,\"\",@\n");
        source.Append(".uleb128 ").Append(string.IsNullOrEmpty(RuntimeName) ? 2 : 3).Append('\n');
        AppendField(source, "language", "C#", string.Empty, "language");

        if (!string.IsNullOrEmpty(RuntimeName))
            AppendField(source, "processed-by", RuntimeName, ProductVersion, "processed_by");

        AppendField(source, "sdk", ".NET", ProductVersion, "sdk");
        return source.ToString();
    }

    private static void AppendField(StringBuilder source, string field, string name, string version, string label)
    {
        AppendString(source, field, $"{label}_field");
        source.Append(".uleb128 1\n");
        AppendString(source, name, $"{label}_name");
        AppendString(source, version, $"{label}_version");
    }

    private static void AppendString(StringBuilder source, string value, string label)
    {
        source.Append(".uleb128 .Ldotnet_producers_").Append(label).Append("_end - .Ldotnet_producers_").Append(label).Append("_start\n");
        source.Append(".Ldotnet_producers_").Append(label).Append("_start:\n");
        source.Append(".ascii \"").Append(EscapeString(value)).Append("\"\n");
        source.Append(".Ldotnet_producers_").Append(label).Append("_end:\n");
    }

    private static string EscapeString(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

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
