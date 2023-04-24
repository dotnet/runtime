// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetModuleFileNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint GetModuleFileName(IntPtr hModule, ref char lpFilename, uint nSize);
    }
}
