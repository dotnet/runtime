// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        [RequiresUnsafe]
        [LibraryImport(Libraries.Ole32)]
        internal static partial IntPtr CoTaskMemAlloc(nuint cb);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Ole32)]
        internal static partial IntPtr CoTaskMemRealloc(IntPtr pv, nuint cb);

        [RequiresUnsafe]
        [LibraryImport(Libraries.Ole32)]
        internal static partial void CoTaskMemFree(IntPtr ptr);
    }
}
