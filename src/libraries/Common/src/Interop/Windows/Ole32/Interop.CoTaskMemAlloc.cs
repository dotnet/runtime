// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        [LibraryImport(Libraries.Ole32)]
        internal static partial IntPtr CoTaskMemAlloc(nuint cb);

        [LibraryImport(Libraries.Ole32)]
        internal static partial IntPtr CoTaskMemRealloc(IntPtr pv, nuint cb);

        [LibraryImport(Libraries.Ole32)]
        internal static partial void CoTaskMemFree(IntPtr ptr);
    }
}
