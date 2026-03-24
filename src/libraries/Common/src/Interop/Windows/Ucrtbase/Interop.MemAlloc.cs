// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class Ucrtbase
    {
[RequiresUnsafe]
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [LibraryImport(Libraries.Ucrtbase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* _aligned_malloc(nuint size, nuint alignment);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Ucrtbase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void _aligned_free(void* ptr);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Ucrtbase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* _aligned_realloc(void* ptr, nuint size, nuint alignment);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Ucrtbase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* calloc(nuint num, nuint size);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Ucrtbase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void free(void* ptr);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Ucrtbase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* malloc(nuint size);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Ucrtbase)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void* realloc(void* ptr, nuint new_size);
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
    }
}
