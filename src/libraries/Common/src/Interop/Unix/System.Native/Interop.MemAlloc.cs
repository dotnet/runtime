// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedAlloc")]
        internal static partial void* AlignedAlloc(nuint alignment, nuint size);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedFree")]
        internal static partial void AlignedFree(void* ptr);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedRealloc")]
        internal static partial void* AlignedRealloc(void* ptr, nuint alignment, nuint new_size);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Calloc")]
        internal static partial void* Calloc(nuint num, nuint size);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Free")]
        internal static partial void Free(void* ptr);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Malloc")]
        internal static partial void* Malloc(nuint size);

        [GeneratedDllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Realloc")]
        internal static partial void* Realloc(void* ptr, nuint new_size);
    }
}
