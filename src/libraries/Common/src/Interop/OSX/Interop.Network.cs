// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NetworkFramework
    {
        // Network Framework reference counting functions
        [LibraryImport(Libraries.NetworkFramework, EntryPoint = "nw_retain")]
        internal static partial IntPtr Retain(IntPtr obj);

        [LibraryImport(Libraries.NetworkFramework, EntryPoint = "nw_release")]
        internal static partial void Release(IntPtr obj);
    }
}
