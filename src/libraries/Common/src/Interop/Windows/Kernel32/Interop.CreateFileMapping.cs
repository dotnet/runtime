// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
            // split the long into two ints
            int capacityHigh, capacityLow;
            SplitLong(maximumSize, out capacityHigh, out capacityLow);

            return CreateFileMapping(hFile, ref securityAttributes, pageProtection, capacityHigh, capacityLow, name);
        }

        public static SafeMemoryMappedFileHandle CreateFileMapping(
                IntPtr hFile,
                ref SECURITY_ATTRIBUTES securityAttributes,
                int pageProtection,
                long maximumSize,
                string? name)
        {
            // split the long into two ints
            int capacityHigh, capacityLow;
            SplitLong(maximumSize, out capacityHigh, out capacityLow);

            return CreateFileMapping(hFile, ref securityAttributes, pageProtection, capacityHigh, capacityLow, name);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SplitLong(long number, out int high, out int low)
        {
            high = unchecked((int)(number >> 32));
            low = unchecked((int)(number & 0x00000000FFFFFFFFL));
        }
    }
}
