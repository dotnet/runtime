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

        [DllImport(Libraries.Kernel32)]
        internal static extern IntPtr LocalAlloc(uint uFlags, UIntPtr uBytes);

        [DllImport(Libraries.Kernel32)]
        internal static extern IntPtr LocalReAlloc(IntPtr hMem, IntPtr uBytes, uint uFlags);

        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr hMem);
    }
}
