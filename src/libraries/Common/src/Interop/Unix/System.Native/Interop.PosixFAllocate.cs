// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Returns -1 on ENOSPC, -2 on EFBIG. On success or ignorable error, 0 is returned.
        /// </summary>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_PosixFAllocate", SetLastError = false)]
        internal static extern int PosixFAllocate(SafeFileHandle fd, long offset, long length);
    }
}
