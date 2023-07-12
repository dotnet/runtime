// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://docs.microsoft.com/windows/win32/api/winioctl/ni-winioctl-fsctl_get_reparse_point
        internal const int FSCTL_GET_REPARSE_POINT = 0x000900a8;

        // https://docs.microsoft.com/windows-hardware/drivers/ddi/ntddstor/ni-ntddstor-ioctl_storage_read_capacity
        internal const int IOCTL_STORAGE_READ_CAPACITY = 0x002D5140;

        // https://learn.microsoft.com/windows/win32/api/winioctl/ni-winioctl-fsctl_set_sparse
        internal const int FSCTL_SET_SPARSE = 0x000900c4;
        internal struct FILE_SET_SPARSE_BUFFER
        {
            internal byte SetSparse;
        }

        // https://learn.microsoft.com/windows/win32/api/winioctl/ni-winioctl-fsctl_get_integrity_information
        internal const int FSCTL_GET_INTEGRITY_INFORMATION = 0x0009027C;
        internal struct FSCTL_GET_INTEGRITY_INFORMATION_BUFFER
        {
            internal ushort ChecksumAlgorithm;
            internal ushort Reserved;
            internal uint Flags;
            internal uint ChecksumChunkSizeInBytes;
            internal uint ClusterSizeInBytes;
        }

        // https://learn.microsoft.com/windows/win32/api/winioctl/ni-winioctl-fsctl_set_integrity_information
        internal const int FSCTL_SET_INTEGRITY_INFORMATION = 0x0009C280;
        internal struct FSCTL_SET_INTEGRITY_INFORMATION_BUFFER
        {
            internal ushort ChecksumAlgorithm;
            internal ushort Reserved;
            internal uint Flags;
        }

        // https://learn.microsoft.com/windows/win32/api/winioctl/ni-winioctl-fsctl_duplicate_extents_to_file
        internal const int FSCTL_DUPLICATE_EXTENTS_TO_FILE = 0x00098344;
        internal struct DUPLICATE_EXTENTS_DATA
        {
            internal IntPtr FileHandle;
            internal long SourceFileOffset;
            internal long TargetFileOffset;
            internal long ByteCount;
        }

        [LibraryImport(Libraries.Kernel32, EntryPoint = "DeviceIoControl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool DeviceIoControl(
            SafeHandle hDevice,
            uint dwIoControlCode,
            void* lpInBuffer,
            uint nInBufferSize,
            void* lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);
    }
}
