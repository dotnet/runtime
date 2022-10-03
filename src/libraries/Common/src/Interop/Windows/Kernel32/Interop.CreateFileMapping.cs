// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateFileMappingW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeMemoryMappedFileHandle CreateFileMapping(
            SafeFileHandle hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
            int flProtect,
            int dwMaximumSizeHigh,
            int dwMaximumSizeLow,
            string? lpName);

        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateFileMappingW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeMemoryMappedFileHandle CreateFileMapping(
            IntPtr hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
            int flProtect,
            int dwMaximumSizeHigh,
            int dwMaximumSizeLow,
            string? lpName);
    }
}
