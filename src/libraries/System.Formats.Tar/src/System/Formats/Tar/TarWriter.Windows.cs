// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Formats.Tar
{
    // Windows specific methods for the TarWriter class.
    public sealed partial class TarWriter : IDisposable
    {
        // Windows files don't have a mode. Use a mode of 755 for directories and files.
        private const UnixFileMode DefaultWindowsMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

        // Windows specific implementation of the method that reads an entry from disk and writes it into the archive stream.
        private TarEntry ConstructEntryForWriting(string fullPath, string entryName, FileOptions fileOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(fullPath));

            FileAttributes attributes = File.GetAttributes(fullPath);

            bool isDirectory = (attributes & FileAttributes.Directory) != 0;

            FileSystemInfo info = isDirectory ? new DirectoryInfo(fullPath) : new FileInfo(fullPath);

            TarEntryType entryType;
            string? linkTarget = null;
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                linkTarget = info.LinkTarget;
                if (linkTarget is not null)
                {
                    // Only symlinks (IO_REPARSE_TAG_SYMLINK) and junctions (IO_REPARSE_TAG_MOUNT_POINT)
                    // have a non-null LinkTarget. Write them as symbolic link entries.
                    entryType = TarEntryType.SymbolicLink;
                }
                else if (isDirectory)
                {
                    // Non-symlink directory reparse points (e.g., OneDrive directories)
                    // are treated as regular directories.
                    entryType = TarEntryType.Directory;
                }
                else if ((attributes & (FileAttributes.Normal | FileAttributes.Archive)) != 0)
                {
                    // Non-symlink file reparse points (e.g., deduplication) may have
                    // transparently accessible content. Classify as regular file and
                    // attempt to open the content below.
                    entryType = TarHelpers.GetRegularFileEntryTypeForFormat(Format);
                }
                else
                {
                    throw new IOException(SR.Format(SR.TarUnsupportedFile, fullPath));
                }
            }
            else if (isDirectory)
            {
                entryType = TarEntryType.Directory;
            }
            else if ((attributes & (FileAttributes.Normal | FileAttributes.Archive)) != 0)
            {
                entryType = TarHelpers.GetRegularFileEntryTypeForFormat(Format);
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

            entry._header._mTime = info.LastWriteTimeUtc;
            // We do not set atime and ctime by default because many external tools are unable to read GNU entries
            // that have these fields set to non-zero values. This is because the GNU format writes atime and ctime in the same
            // location where other formats expect the prefix field to be written.
            // If the user wants to set atime and ctime, they can do so by constructing the entry manually from the file and
            // then setting the values.

            entry.Mode = DefaultWindowsMode;

            if (entry.EntryType == TarEntryType.SymbolicLink)
            {
                entry.LinkName = linkTarget ?? string.Empty;
            }

            if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
            {
                Debug.Assert(entry._header._dataStream == null);
                try
                {
                    entry._header._dataStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, fileOptions);
                }
                catch (Exception e) when ((attributes & FileAttributes.ReparsePoint) != 0 && (e is IOException or UnauthorizedAccessException))
                {
                    // Non-symlink reparse points with inaccessible content (e.g., AppExecLinks)
                    // cannot be archived as regular files.
                    throw new IOException(SR.Format(SR.TarUnsupportedFile, fullPath), e);
                }
            }

            return entry;
        }
    }
}
