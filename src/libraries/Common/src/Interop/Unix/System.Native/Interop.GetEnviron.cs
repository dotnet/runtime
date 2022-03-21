// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetEnviron")]
        internal static unsafe partial IntPtr GetEnviron();

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_FreeEnviron")]
        internal static unsafe partial void FreeEnviron(IntPtr environ);
    }
}
