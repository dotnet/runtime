// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [DllImport(Libraries.User32, ExactSpelling = true)]
        public static unsafe extern int GetWindowThreadProcessId(IntPtr handle, int* processId);

        [DllImport(Libraries.User32, ExactSpelling = true)]
        public static extern int GetWindowThreadProcessId(HandleRef handle, out int processId);
    }
}
