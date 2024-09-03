// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{

    internal static partial class Sys
    {
        [Flags]
        internal enum MemoryMappedProtections
        {
            PROT_NONE = 0x0,
            PROT_READ = 0x1,
            PROT_WRITE = 0x2,
            PROT_EXEC = 0x4
        }

        [Flags]
        internal enum MemoryMappedFlags
        {
            MAP_SHARED = 0x01,
            MAP_PRIVATE = 0x02,
            MAP_ANONYMOUS = 0x10,
        }

        // NOTE: Shim returns null pointer on failure, not non-null MAP_FAILED sentinel.
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_MMap", SetLastError = true)]
        internal static partial IntPtr MMap(
            IntPtr addr, ulong len,
            MemoryMappedProtections prot, MemoryMappedFlags flags,
            SafeFileHandle fd, long offset);

        // NOTE: Shim returns null pointer on failure, not non-null MAP_FAILED sentinel.
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_MMap", SetLastError = true)]
        internal static partial IntPtr MMap(
            IntPtr addr, ulong len,
            MemoryMappedProtections prot, MemoryMappedFlags flags,
            IntPtr fd, long offset);
    }
}
