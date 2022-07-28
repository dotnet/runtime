// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32)]
        internal static partial IntPtr GetProcessHeap();

        [Flags]
        internal enum HeapAllocFlags : int
        {
            None = 0x00000000,
            HEAP_NO_SERIALIZE = 0x00000001,
            HEAP_ZERO_MEMORY = 0x00000008,
            HEAP_GENERATE_EXCEPTIONS = 0x00000004,
        }

        [LibraryImport(Libraries.Kernel32)]
        internal static partial SafeHeapAllocHandle HeapAlloc(IntPtr hHeap, HeapAllocFlags dwFlags, nint dwBytes);

        [LibraryImport(Libraries.Kernel32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool HeapFree(IntPtr hHeap, HeapAllocFlags dwFlags, IntPtr lpMem);
    }
}
