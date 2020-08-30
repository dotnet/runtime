// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        [DllImport(Libraries.Ole32)]
        internal static extern IntPtr CoTaskMemAlloc(UIntPtr cb);

        [DllImport(Libraries.Ole32)]
        internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, UIntPtr cb);

        [DllImport(Libraries.Ole32)]
        internal static extern void CoTaskMemFree(IntPtr ptr);
    }
}
