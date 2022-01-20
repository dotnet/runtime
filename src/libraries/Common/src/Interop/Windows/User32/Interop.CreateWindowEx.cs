// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [GeneratedDllImport(Libraries.User32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static partial IntPtr CreateWindowExW(
            int exStyle,
            string lpszClassName,
            string lpszWindowName,
            int style,
            int x,
            int y,
            int width,
            int height,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInst,
            IntPtr pvParam);
    }
}
