// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint LMEM_FIXED = 0x0000;
        internal const uint LMEM_MOVEABLE = 0x0002;
        internal const uint LMEM_ZEROINIT = 0x0040;

        [LibraryImport(Libraries.Kernel32)]
        internal static partial IntPtr LocalAlloc(uint uFlags, nuint uBytes);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial IntPtr LocalReAlloc(IntPtr hMem, nuint uBytes, uint uFlags);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial IntPtr LocalFree(IntPtr hMem);
    }
}
