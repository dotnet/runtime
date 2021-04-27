// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [DllImport(Libraries.User32)]
        public static extern unsafe bool EnumWindows(delegate* unmanaged<IntPtr, IntPtr, Interop.BOOL> callback, IntPtr extraData);
    }
}
