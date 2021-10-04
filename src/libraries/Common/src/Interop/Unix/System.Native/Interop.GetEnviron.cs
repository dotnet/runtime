// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetEnviron")]
        internal static extern unsafe IntPtr GetEnviron();

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_FreeEnviron")]
        internal static extern unsafe void FreeEnviron(IntPtr environ);
    }
}