// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.InteropServices
{
    // Reduced version of NativeMemory for targets older than .NET 6.
    internal static unsafe class NativeMemory
    {
        public static void* Alloc(nuint byteCount)
        {
            return (void*)(nint)Marshal.AllocHGlobal((nint)byteCount);
        }

        public static void* Realloc(void* ptr, nuint byteCount)
        {
            return (void*)(nint)Marshal.ReAllocHGlobal((nint)ptr, (nint)byteCount);
        }

        public static void Free(void* ptr)
        {
            Marshal.FreeHGlobal((nint)ptr);
        }
    }
}
