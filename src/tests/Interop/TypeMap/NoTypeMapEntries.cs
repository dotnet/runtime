// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

public static class ValidateNoTypeMapEntries
{
    public static bool HasR2RDumpFile
    {
        get
        {
            if (string.IsNullOrEmpty(typeof(ValidateNoTypeMapEntries).Assembly.Location))
            {
                return false;
            }
            return System.IO.File.Exists(typeof(ValidateNoTypeMapEntries).Assembly.Location + ".r2rdump");
        }
    }

    public static void VerifyNoTypeMapSections()
    {
        string r2rDumpFile = typeof(ValidateNoTypeMapEntries).Assembly.Location + ".r2rdump";
        string[] r2rDumpLines = System.IO.File.ReadAllLines(r2rDumpFile);
        foreach (string line in r2rDumpLines)
        {
            if (line.StartsWith("ExternalTypeMap") || line.StartsWith("ProxyTypeMap") || line.StartsWith("TypeMapAssemblyTargets"))
            {
                throw new InvalidOperationException($"Unexpected TypeMap section found in R2R dump for file with no type maps: {line}");
            }
        }
    }
}
