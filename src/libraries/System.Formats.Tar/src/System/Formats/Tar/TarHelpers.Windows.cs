// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Formats.Tar
{
    internal struct FileId : IEquatable<FileId>
    {
        internal long VolumeSerialNumber;
        internal long FileIndex;

        internal FileId(long volumeSerialNumber, long fileIndex)
        {
            VolumeSerialNumber = volumeSerialNumber;
            FileIndex = fileIndex;
        }

        public bool Equals(FileId other) => VolumeSerialNumber == other.VolumeSerialNumber && FileIndex == other.FileIndex;
        public override bool Equals(object? obj) => obj is FileId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(VolumeSerialNumber, FileIndex);
    }

    internal static partial class TarHelpers
    {
        internal static SortedDictionary<string, UnixFileMode>? CreatePendingModesDictionary()
            => null;

#pragma warning disable IDE0060
        internal static void CreateDirectory(string fullPath, UnixFileMode? mode, SortedDictionary<string, UnixFileMode>? pendingModes)
            => Directory.CreateDirectory(fullPath);
#pragma warning restore IDE0060

        internal static void SetPendingModes(SortedDictionary<string, UnixFileMode>? pendingModes)
            => Debug.Assert(pendingModes is null);

        internal static FileId GetFileId(string filePath)
        {
            using SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (!Interop.Kernel32.GetFileInformationByHandle(handle, out Interop.Kernel32.BY_HANDLE_FILE_INFORMATION fileInfo))
            {
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError(), filePath);
            }
            ulong fileIndex = ((ulong)fileInfo.nFileIndexHigh << 32) | fileInfo.nFileIndexLow;
            return new FileId((long)fileInfo.dwVolumeSerialNumber, (long)fileIndex);
        }

        internal static bool TryGetPathInfo(string path, out FileId fileId, out bool isSymlink)
        {
            using SafeFileHandle handle = Interop.Kernel32.CreateFile(
                path,
                Interop.Kernel32.GenericOperations.GENERIC_READ,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_OPEN_REPARSE_POINT | Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS);

            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastPInvokeError();
                if (error == Interop.Errors.ERROR_FILE_NOT_FOUND || error == Interop.Errors.ERROR_PATH_NOT_FOUND)
                {
                    fileId = default;
                    isSymlink = false;
                    return false;
                }
                throw Win32Marshal.GetExceptionForWin32Error(error, path);
            }

            if (!Interop.Kernel32.GetFileInformationByHandle(handle, out Interop.Kernel32.BY_HANDLE_FILE_INFORMATION fileInfo))
            {
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError(), path);
            }

            ulong fileIndex = ((ulong)fileInfo.nFileIndexHigh << 32) | fileInfo.nFileIndexLow;
            fileId = new FileId((long)fileInfo.dwVolumeSerialNumber, (long)fileIndex);
            const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
            isSymlink = (fileInfo.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0;
            return true;
        }
    }
}
