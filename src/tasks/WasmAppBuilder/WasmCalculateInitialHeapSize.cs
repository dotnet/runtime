// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

/// <summary>estimate the total memory needed for the assemblies.</summary>
public class WasmCalculateInitialHeapSize : Task
{
    [Required]
    [NotNull]
    public string[]? Assemblies { get; set; }

    [Output]
    public long? TotalSize { get; private set; }

    public override bool Execute ()
    {
        long totalDllSize=0;
        foreach (var asm in Assemblies)
        {
            var info = new FileInfo(asm);
            if (!info.Exists)
            {
                Log.LogError($"Could not find assembly '{asm}'");
                return false;
            }
            totalDllSize += info.Length;
        }

        // this is arbitrary guess about memory overhead of the runtime, after the assemblies are loaded
        const double extraMemoryRatio = 1.2;
        long memorySize = (long) (totalDllSize * extraMemoryRatio);

        // round it up to 64KB page size for wasm
        TotalSize = (memorySize + 0x10000) & 0xFFFF0000;

        return true;
    }
}
