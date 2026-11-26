// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.WebAssembly.Webcil;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Strips the name and DWARF custom sections from the first core module of a WASI component, optionally
/// writing the function names to a symbol map sidecar.
/// </summary>
public sealed class StripWasiComponentSymbols : Task
{
    [Required, NotNull]
    public string? InputPath { get; set; }

    [Required, NotNull]
    public string? OutputPath { get; set; }

    public string? SymbolMapPath { get; set; }

    [Output]
    public ITaskItem[] FileWrites { get; private set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        string outputPath = Path.GetFullPath(OutputPath);
        string? symbolMapPath = string.IsNullOrEmpty(SymbolMapPath) ? null : Path.GetFullPath(SymbolMapPath);
        try
        {
            int functionCount = WasmComponentSymbols.StripFirstCoreModule(Path.GetFullPath(InputPath), outputPath, symbolMapPath);
            Log.LogMessage(MessageImportance.Low, $"Stripped symbols from {outputPath}; {functionCount} function names.");

            List<ITaskItem> fileWrites = new() { new TaskItem(outputPath) };
            if (symbolMapPath is not null)
                fileWrites.Add(new TaskItem(symbolMapPath));
            FileWrites = fileWrites.ToArray();
            return true;
        }
        catch (Exception ex)
        {
            File.Delete(outputPath);
            if (symbolMapPath is not null)
                File.Delete(symbolMapPath);
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
