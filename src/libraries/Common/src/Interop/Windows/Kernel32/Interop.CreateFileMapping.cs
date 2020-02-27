// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class Kernel32
    {
        public static SafeMemoryMappedFileHandle CreateFileMapping(
                SafeFileHandle hFile,
                ref SECURITY_ATTRIBUTES securityAttributes,
                int pageProtection,
                long maximumSize,
                string? name)
        {
            return CreateFileMapping(
                hFile,
                ref securityAttributes,
                pageProtection,
                dwMaximumSizeHigh: (int)(maximumSize >> 32), // Split the maximumSize long into two ints
                dwMaximumSizeLow: (int)maximumSize,
                name);
        }

        public static SafeMemoryMappedFileHandle CreateFileMapping(
                IntPtr hFile,
                ref SECURITY_ATTRIBUTES securityAttributes,
                int pageProtection,
                long maximumSize,
                string? name)
        {
            return CreateFileMapping(
                hFile,
                ref securityAttributes,
                pageProtection,
                dwMaximumSizeHigh: (int)(maximumSize >> 32), // Split the maximumSize long into two ints
                dwMaximumSizeLow: (int)maximumSize,
                name);
        }

        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFileMappingW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeMemoryMappedFileHandle CreateFileMapping(
            SafeFileHandle hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
            int flProtect,
            int dwMaximumSizeHigh,
            int dwMaximumSizeLow,
            string? lpName);

        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFileMappingW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeMemoryMappedFileHandle CreateFileMapping(
            IntPtr hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
            int flProtect,
            int dwMaximumSizeHigh,
            int dwMaximumSizeLow,
            string? lpName);
    }
}
