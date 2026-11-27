// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal const string ProcMountInfoFilePath = "/proc/self/mountinfo";

        internal static Error GetFileSystemTypeForRealPath(string path, out string format)
        {
            format = "";

            if (File.Exists(ProcMountInfoFilePath))
            {
                try
                {
                    string? fileSystemType = GetFileSystemTypeForPath(path, File.ReadLines(ProcMountInfoFilePath));
                    if (fileSystemType is not null)
                    {
                        format = fileSystemType;
                        return Error.SUCCESS;
                    }
                    return Error.ENOENT;
                }
                catch (Exception e)
                {
                    Debug.Fail($"Failed to read \"{ProcMountInfoFilePath}\": {e}");
                }
            }

            return Error.ENOTSUP;
        }
    }
}
