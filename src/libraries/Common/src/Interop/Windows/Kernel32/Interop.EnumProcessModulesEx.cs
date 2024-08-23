// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const int LIST_MODULES_ALL = 0x00000003;

        [LibraryImport(Libraries.Kernel32, EntryPoint = "K32EnumProcessModulesEx", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EnumProcessModulesEx(SafeProcessHandle handle, IntPtr[]? modules, int size, out int needed, int filterFlag);
    }
}
