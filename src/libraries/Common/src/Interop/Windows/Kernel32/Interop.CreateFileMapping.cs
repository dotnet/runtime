// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "CreateFileMappingW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial SafeMemoryMappedFileHandle CreateFileMapping(
#else
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFileMappingW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeMemoryMappedFileHandle CreateFileMapping(
#endif
            SafeFileHandle hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
            int flProtect,
            int dwMaximumSizeHigh,
            int dwMaximumSizeLow,
            string? lpName);

#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "CreateFileMappingW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial SafeMemoryMappedFileHandle CreateFileMapping(
#else
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFileMappingW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeMemoryMappedFileHandle CreateFileMapping(
#endif
            IntPtr hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
            int flProtect,
            int dwMaximumSizeHigh,
            int dwMaximumSizeLow,
            string? lpName);
    }
}
