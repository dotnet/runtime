// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Formats.Tar
{
    // Unix specific methods for the TarWriter class.
    public sealed partial class TarWriter : IDisposable, IAsyncDisposable
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
                Interop.Sys.FileTypes.S_IFREG => TarEntryType.RegularFile,
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
                _ => throw new NotSupportedException(SR.UnknownFormat),
            };

            if ((entryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice) && status.Dev > 0)
            {
                Interop.Sys.GetDeviceIdentifiers((ulong)status.Dev, out uint major, out uint minor);
                entry._header._devMajor = (int)major;
                entry._header._devMinor = (int)minor;
            }

            entry._header._mTime = DateTimeOffset.FromUnixTimeSeconds(status.MTime);
            entry._header._aTime = DateTimeOffset.FromUnixTimeSeconds(status.ATime);
            entry._header._cTime = DateTimeOffset.FromUnixTimeSeconds(status.CTime);

            entry._header._mode = (status.Mode & 4095); // First 12 bits

            entry.Uid = (int)status.Uid;
            entry.Gid = (int)status.Gid;

            // TODO: Add these p/invokes
            entry._header._uName = "";// Interop.Sys.GetUName();
            entry._header._gName = "";// Interop.Sys.GetGName();

            if (entry.EntryType == TarEntryType.SymbolicLink)
            {
                entry.LinkName = info.LinkTarget ?? string.Empty;
            }

            if (entry.EntryType == TarEntryType.RegularFile)
            {
                FileStreamOptions options = new()
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.Read,
                    Options = FileOptions.None
                };

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
