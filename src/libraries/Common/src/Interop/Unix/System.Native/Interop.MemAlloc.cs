// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_MemAlloc")]
        internal static extern IntPtr MemAlloc(nuint sizeInBytes);

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_MemReAlloc")]
        internal static extern IntPtr MemReAlloc(IntPtr ptr, nuint newSize);

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_MemFree")]
        internal static extern void MemFree(IntPtr ptr);
    }
}
