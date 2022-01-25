// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        private static volatile int s_tryCopyFileRange;

        private static bool TryCopyFileRange
        {
            get
            {
                int tryCfr = s_tryCopyFileRange;
                if (tryCfr == 0) // Uninitialized.
                {
                    // Avoid known issues with copy_file_range that are fixed Linux 5.3+ (https://lwn.net/Articles/789527/).
                    s_tryCopyFileRange = tryCfr =
                        !OperatingSystem.IsOSPlatform("LINUX") ||
                        Environment.OSVersion.Version.Major > 5 || (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 3) ? 1 : -1;
                }
                return tryCfr == 1;
            }
        }

        internal static int CopyFile(SafeFileHandle source, SafeFileHandle destination, long sourceLength)
            => CopyFile(source, destination, sourceLength, TryCopyFileRange ? 1 : 0);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_CopyFile", SetLastError = true)]
        private static partial int CopyFile(SafeFileHandle source, SafeFileHandle destination, long sourceLength, int tryCopyFileRange);
    }
}
