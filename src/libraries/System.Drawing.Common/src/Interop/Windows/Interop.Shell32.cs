// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Shell32
    {
        [DllImport(Libraries.Shell32, CharSet = CharSet.Unicode)]
        internal static extern unsafe IntPtr ExtractAssociatedIcon(HandleRef hInst, char* iconPath, ref int index);
    }
}