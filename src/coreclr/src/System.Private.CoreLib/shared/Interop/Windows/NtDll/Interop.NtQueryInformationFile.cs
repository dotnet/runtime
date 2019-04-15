// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class NtDll
    {
        [DllImport(Libraries.NtDll, ExactSpelling = true)]
        unsafe internal static extern int NtQueryInformationFile(
            SafeFileHandle FileHandle,
            out IO_STATUS_BLOCK IoStatusBlock,
            void* FileInformation,
            uint Length,
            uint FileInformationClass);

        [StructLayout(LayoutKind.Sequential)]
        internal struct IO_STATUS_BLOCK
        {
            IO_STATUS Status;
            IntPtr Information;
        }

        // This isn't an actual Windows type, we have to separate it out as the size of IntPtr varies by architecture
        // and we can't specify the size at compile time to offset the Information pointer in the status block.
        [StructLayout(LayoutKind.Explicit)]
        internal struct IO_STATUS
        {
            [FieldOffset(0)]
            int Status;

            [FieldOffset(0)]
            IntPtr Pointer;
        }

        internal const uint FileModeInformation = 16;
        internal const uint FILE_SYNCHRONOUS_IO_ALERT = 0x00000010;
        internal const uint FILE_SYNCHRONOUS_IO_NONALERT = 0x00000020;

        internal const int STATUS_INVALID_HANDLE = unchecked((int)0xC0000008);
    }
}
