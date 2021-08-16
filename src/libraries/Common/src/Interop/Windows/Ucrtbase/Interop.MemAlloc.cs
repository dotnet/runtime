// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Ucrtbase
    {
        [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void* _aligned_malloc(nuint size, nuint alignment);

        [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void _aligned_free(void* ptr);

        [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void* _aligned_realloc(void* ptr, nuint size, nuint alignment);

        [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void* calloc(nuint num, nuint size);

        [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void free(void* ptr);

        [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void* malloc(nuint size);

        [DllImport(Libraries.Ucrtbase, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void* realloc(void* ptr, nuint new_size);
    }
}
