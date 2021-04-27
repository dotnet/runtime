// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetProcessHeap();

        [Flags]
        internal enum HeapAllocFlags : int
        {
            None = 0x00000000,
            HEAP_NO_SERIALIZE = 0x00000001,
            HEAP_ZERO_MEMORY = 0x00000008,
            HEAP_GENERATE_EXCEPTIONS = 0x00000004,
        }

        internal static SafeHeapAllocHandle HeapAlloc(IntPtr hHeap, HeapAllocFlags dwFlags, int dwBytes)
        {
            return HeapAlloc(hHeap, dwFlags, new IntPtr(dwBytes));
        }

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode)]
        private static extern SafeHeapAllocHandle HeapAlloc(IntPtr hHeap, HeapAllocFlags dwFlags, IntPtr dwBytes);

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode)]
        internal static extern bool HeapFree(IntPtr hHeap, HeapAllocFlags dwFlags, IntPtr lpMem);
    }
}
