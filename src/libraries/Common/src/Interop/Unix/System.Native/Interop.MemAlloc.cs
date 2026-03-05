// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedAlloc")]
        [RequiresUnsafe]
        internal static partial void* AlignedAlloc(nuint alignment, nuint size);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedFree")]
        [RequiresUnsafe]
        internal static partial void AlignedFree(void* ptr);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_AlignedRealloc")]
        [RequiresUnsafe]
        internal static partial void* AlignedRealloc(void* ptr, nuint alignment, nuint new_size);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Calloc")]
        [RequiresUnsafe]
        internal static partial void* Calloc(nuint num, nuint size);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Free")]
        [RequiresUnsafe]
        internal static partial void Free(void* ptr);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Malloc")]
        [RequiresUnsafe]
        internal static partial void* Malloc(nuint size);

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_Realloc")]
        [RequiresUnsafe]
        internal static partial void* Realloc(void* ptr, nuint new_size);
    }
}
