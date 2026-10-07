// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System.Formats.Tar
{
    // Windows specific methods for the TarEntry class.
    public abstract partial class TarEntry
    {
        private static readonly char[] s_directorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

        private static string ResolveExistingPath(string path, out bool isOrdinaryDirectory)
        {
            isOrdinaryDirectory = false;
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default;
            if (Interop.Kernel32.GetFileAttributesEx(path, Interop.Kernel32.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, ref data))
            {
                FileAttributes attributes = (FileAttributes)data.dwFileAttributes;
                isOrdinaryDirectory = (attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == FileAttributes.Directory;
                return (attributes & FileAttributes.ReparsePoint) != 0 ?
                    ResolveSymlink(path) :
                    Path.GetFullPath(path);
            }

            return Path.Exists(path) ? ResolveSymlink(path) : path;
        }

#pragma warning disable IDE0060
        // Throws on Windows. Block devices are not supported on this platform.
        private void ExtractAsBlockDevice(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.BlockDevice);
            throw new InvalidOperationException(SR.IO_DeviceFiles_NotSupported);
        }

        // Throws on Windows. Character devices are not supported on this platform.
        private void ExtractAsCharacterDevice(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.CharacterDevice);
            throw new InvalidOperationException(SR.IO_DeviceFiles_NotSupported);
        }

        // Throws on Windows. Fifo files are not supported on this platform.
        private void ExtractAsFifo(string destinationFileName)
        {
            Debug.Assert(EntryType is TarEntryType.Fifo);
            throw new InvalidOperationException(SR.IO_FifoFiles_NotSupported);
        }

        // Best-effort attempt to mark the file as sparse on Windows so subsequent unwritten ranges
        // remain real holes (unallocated extents) rather than being zero-filled on disk. The call
        // is silently ignored if the underlying file system does not support sparse files
        // (e.g. FAT/exFAT), in which case the extraction still produces correct content but the
        // file occupies its full logical size on disk.
        private static unsafe void TryMarkFileSparse(FileStream fs)
        {
            Interop.Kernel32.DeviceIoControl(
                fs.SafeFileHandle,
                Interop.Kernel32.FSCTL_SET_SPARSE,
                null,
                0,
                null,
                0,
                out _,
                IntPtr.Zero);
        }
#pragma warning restore IDE0060
    }
}
