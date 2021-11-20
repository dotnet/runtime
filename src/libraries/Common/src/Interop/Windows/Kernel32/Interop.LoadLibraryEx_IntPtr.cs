// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        internal const int LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial IntPtr LoadLibraryEx(string libFilename, IntPtr reserved, int flags);
    }
}
