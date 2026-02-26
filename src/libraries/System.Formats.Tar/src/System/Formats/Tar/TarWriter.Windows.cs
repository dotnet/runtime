// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Formats.Tar
{
    // Windows specific methods for the TarWriter class.
    public sealed partial class TarWriter : IDisposable
    {
        // Windows files don't have a mode. Use a mode of 755 for directories and files.
        private const UnixFileMode DefaultWindowsMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

        private Dictionary<(uint, ulong), string>? _hardLinkTargets;

        // Windows specific implementation of the method that reads an entry from disk and writes it into the archive stream.
        private TarEntry ConstructEntryForWriting(string fullPath, string entryName, FileOptions fileOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(fullPath));

            using SafeFileHandle handle = Interop.Kernel32.CreateFile(
                fullPath,
                Interop.Kernel32.GenericOperations.GENERIC_READ,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS | Interop.Kernel32.FileOperations.FILE_FLAG_OPEN_REPARSE_POINT);

            if (handle.IsInvalid)
            {
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError(), fullPath);
            }

            if (!Interop.Kernel32.GetFileInformationByHandle(handle, out Interop.Kernel32.BY_HANDLE_FILE_INFORMATION fileInfo))
            {
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError(), fullPath);
            }

            FileAttributes attributes = (FileAttributes)fileInfo.dwFileAttributes;

            // Track files that have more than one hard link.
            // If we encounter the file again, we'll add a TarEntryType.HardLink.
            string? hardLinkTarget = null;
            if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0 && fileInfo.nNumberOfLinks > 1)
            {
                _hardLinkTargets ??= new Dictionary<(uint, ulong), string>();

                ulong fileIndex = ((ulong)fileInfo.nFileIndexHigh << 32) | fileInfo.nFileIndexLow;
                (uint, ulong) fileId = (fileInfo.dwVolumeSerialNumber, fileIndex);
                if (!_hardLinkTargets.TryGetValue(fileId, out hardLinkTarget))
                {
                    _hardLinkTargets.Add(fileId, entryName);
                }
            }

            TarEntryType entryType;
            if (hardLinkTarget != null)
            {
                entryType = TarEntryType.HardLink;
            }
            else if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                entryType = TarEntryType.SymbolicLink;
            }
            else if ((attributes & FileAttributes.Directory) != 0)
            {
                entryType = TarEntryType.Directory;
            }
            else if ((attributes & (FileAttributes.Normal | FileAttributes.Archive)) != 0)
            {
                entryType = Format is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
            }
            else
            {
                throw new IOException(SR.Format(SR.TarUnsupportedFile, fullPath));
            }

            TarEntry entry = Format switch
            {
                TarEntryFormat.V7 => new V7TarEntry(entryType, entryName),
                TarEntryFormat.Ustar => new UstarTarEntry(entryType, entryName),
                TarEntryFormat.Pax => new PaxTarEntry(entryType, entryName),
                TarEntryFormat.Gnu => new GnuTarEntry(entryType, entryName),
                _ => throw new InvalidDataException(SR.Format(SR.TarInvalidFormat, Format)),
            };

            entry._header._mTime = fileInfo.ftLastWriteTime.ToDateTimeUtc();
            // We do not set atime and ctime by default because many external tools are unable to read GNU entries
            // that have these fields set to non-zero values. This is because the GNU format writes atime and ctime in the same
            // location where other formats expect the prefix field to be written.
            // If the user wants to set atime and ctime, they can do so by constructing the entry manually from the file and
            // then setting the values.

            entry.Mode = DefaultWindowsMode;

            if (entry.EntryType == TarEntryType.SymbolicLink)
            {
                FileSystemInfo info = new FileInfo(fullPath);
                entry.LinkName = info.LinkTarget ?? string.Empty;
            }

            if (entry.EntryType == TarEntryType.HardLink)
            {
                Debug.Assert(hardLinkTarget is not null);
                entry.LinkName = hardLinkTarget;
            }

            if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
            {
                Debug.Assert(entry._header._dataStream == null);
                entry._header._dataStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, fileOptions);
            }

            return entry;
        }
    }
}
