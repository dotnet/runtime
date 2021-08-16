// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [DllImport(Libraries.User32, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int PostMessageW(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImport(Libraries.User32, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int PostMessageW(HandleRef hwnd, int msg, IntPtr wparam, IntPtr lparam);
    }
}
