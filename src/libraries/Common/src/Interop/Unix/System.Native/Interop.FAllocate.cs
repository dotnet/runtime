// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Returns -1 on error, 0 on success.
        /// </summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_FAllocate", SetLastError = true)]
        internal static partial int FAllocate(SafeFileHandle fd, long offset, long length);
    }
}
