// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
#if !MS_IO_REDIST // SafeLibraryHandle is inaccessible due to its protection level
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Ansi, BestFitMapping = false)]
        public static extern IntPtr GetProcAddress(SafeLibraryHandle hModule, string lpProcName);
#endif

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Ansi, BestFitMapping = false)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    }
}
