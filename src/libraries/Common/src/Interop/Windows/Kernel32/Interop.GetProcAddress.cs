// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, CharSet = CharSet.Ansi)]
        public static partial IntPtr GetProcAddress(SafeLibraryHandle hModule, string lpProcName);

        [GeneratedDllImport(Libraries.Kernel32, CharSet = CharSet.Ansi)]
        public static partial IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    }
}
