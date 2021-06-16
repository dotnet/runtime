// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedAlloc")]
        internal static extern void* AlignedAlloc(nuint alignment, nuint size);

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedFree")]
        internal static extern void AlignedFree(void* ptr);

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedRealloc")]
        internal static extern void* AlignedRealloc(void* ptr, nuint alignment, nuint new_size);

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Calloc")]
        internal static extern void* Calloc(nuint num, nuint size);

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Free")]
        internal static extern void Free(void* ptr);

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Malloc")]
        internal static extern void* Malloc(nuint size);

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Realloc")]
        internal static extern void* Realloc(void* ptr, nuint new_size);
    }
}
