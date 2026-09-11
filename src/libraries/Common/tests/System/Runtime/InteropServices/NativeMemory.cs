// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Minimal polyfill of the .NET Core <c>NativeMemory</c> APIs for .NETFramework test targets.
    /// </summary>
    internal static unsafe class NativeMemory
    {
        public static void* Alloc(nuint byteCount) => (void*)Marshal.AllocHGlobal((IntPtr)(void*)byteCount);

        public static void Free(void* ptr) => Marshal.FreeHGlobal((IntPtr)ptr);
    }
}
