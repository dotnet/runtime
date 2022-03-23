// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class User32
    {
        [LibraryImport(Libraries.User32, SetLastError = true)]
        public static unsafe partial int GetWindowTextW(IntPtr hWnd, char* lpString, int nMaxCount);
    }
}
