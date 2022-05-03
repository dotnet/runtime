// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        public const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        public const int LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

        [LibraryImport(Libraries.Kernel32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial SafeLibraryHandle LoadLibraryExW(string lpwLibFileName, IntPtr hFile, uint dwFlags);
    }
}
