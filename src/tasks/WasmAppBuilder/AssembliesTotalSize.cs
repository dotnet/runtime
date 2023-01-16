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

// estimate the total size of the assemblies we are going to load x2 and round it up to 64K
public class AssembliesTotalSize : Task
{
    [Required]
    [NotNull]
    public string[]? Assemblies { get; set; }

    [Output]
    public long? TotalSize { get; private set; }

    public override bool Execute ()
    {
        long totalSize=0;
        foreach (var asm in Assemblies)
        {
            var info = new FileInfo(asm);
            if (!info.Exists)
            {
                Log.LogError($"Could not find assembly '{asm}'");
                return false;
            }
            totalSize += info.Length;
        }
        totalSize *= 2;
        totalSize += 0x10000;
        totalSize &= 0xFFFF0000;

        TotalSize = totalSize;

        return true;
    }
}
