// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32)]
        public static partial IntPtr GetProcAddress(SafeLibraryHandle hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        [LibraryImport(Libraries.Kernel32)]
        public static partial IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);
    }
}
