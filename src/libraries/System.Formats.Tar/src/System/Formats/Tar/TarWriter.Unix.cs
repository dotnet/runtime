// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Formats.Tar
{
    // Unix specific methods for the TarWriter class.
    public sealed partial class TarWriter : IDisposable
    {
        // Unix specific implementation of the method that reads an entry from disk and writes it into the archive stream.
        partial void ReadFileFromDiskAndWriteToArchiveStreamAsEntry(string fullPath, string entryName)
        {
            Interop.Sys.FileStatus status = default;
            status.Mode = default;
            status.Dev = default;
            Interop.CheckIo(Interop.Sys.LStat(fullPath, out status));

            TarEntryType entryType = (status.Mode & (uint)Interop.Sys.FileTypes.S_IFMT) switch
            {
                // Hard links are treated as regular files.
                // Unix socket files do not get added to tar files.
                Interop.Sys.FileTypes.S_IFBLK => TarEntryType.BlockDevice,
                Interop.Sys.FileTypes.S_IFCHR => TarEntryType.CharacterDevice,
                Interop.Sys.FileTypes.S_IFIFO => TarEntryType.Fifo,
                Interop.Sys.FileTypes.S_IFLNK => TarEntryType.SymbolicLink,
                Interop.Sys.FileTypes.S_IFREG => Format is TarFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile,
                Interop.Sys.FileTypes.S_IFDIR => TarEntryType.Directory,
                _ => throw new IOException(string.Format(SR.TarUnsupportedFile, fullPath)),
            };

            FileSystemInfo info = entryType is TarEntryType.Directory ? new DirectoryInfo(fullPath) : new FileInfo(fullPath);

            TarEntry entry = Format switch
            {
                TarFormat.V7 => new V7TarEntry(entryType, entryName),
                TarFormat.Ustar => new UstarTarEntry(entryType, entryName),
                TarFormat.Pax => new PaxTarEntry(entryType, entryName),
                TarFormat.Gnu => new GnuTarEntry(entryType, entryName),
                _ => throw new FormatException(string.Format(SR.TarInvalidFormat, Format)),
            };

            if ((entryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice) && status.Dev > 0)
            {
                uint major;
                uint minor;
                unsafe
                {
                    Interop.CheckIo(Interop.Sys.GetDeviceIdentifiers((ulong)status.Dev, &major, &minor));
                }

                entry._header._devMajor = (int)major;
                entry._header._devMinor = (int)minor;
            }

            entry._header._mTime = TarHelpers.GetDateTimeFromSecondsSinceEpoch(status.MTime);
            entry._header._aTime = TarHelpers.GetDateTimeFromSecondsSinceEpoch(status.ATime);
            entry._header._cTime = TarHelpers.GetDateTimeFromSecondsSinceEpoch(status.CTime);

            entry._header._mode = (status.Mode & 4095); // First 12 bits

            entry.Uid = (int)status.Uid;
            entry.Gid = (int)status.Gid;

            // TODO: Add these p/invokes https://github.com/dotnet/runtime/issues/68230
            entry._header._uName = "";// Interop.Sys.GetUName();
            entry._header._gName = "";// Interop.Sys.GetGName();

            if (entry.EntryType == TarEntryType.SymbolicLink)
            {
                entry.LinkName = info.LinkTarget ?? string.Empty;
            }

            if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
            {
                FileStreamOptions options = new()
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.Read,
                    Options = FileOptions.None
                };

                Debug.Assert(entry._header._dataStream == null);
                entry._header._dataStream = File.Open(fullPath, options);
            }

            WriteEntry(entry);
            if (entry._header._dataStream != null)
            {
                entry._header._dataStream.Dispose();
            }
        }
    }
}
