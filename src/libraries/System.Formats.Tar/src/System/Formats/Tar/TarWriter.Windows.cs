// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Windows specific methods for the TarWriter class.
    public sealed partial class TarWriter : IDisposable
    {
        // Creating archives in Windows always sets the mode to 777
        private const UnixFileMode DefaultWindowsMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.UserExecute;

        // Windows specific implementation of the method that reads an entry from disk and writes it into the archive stream.
        private TarEntry ConstructEntryForWriting(string fullPath, string entryName, FileOptions fileOptions)
        {
            Debug.Assert(!string.IsNullOrEmpty(fullPath));

            FileAttributes attributes = File.GetAttributes(fullPath);

            TarEntryType entryType;
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                entryType = TarEntryType.SymbolicLink;
            }
            else if (attributes.HasFlag(FileAttributes.Directory))
            {
                entryType = TarEntryType.Directory;
            }
            else if (attributes.HasFlag(FileAttributes.Normal) || attributes.HasFlag(FileAttributes.Archive))
            {
                entryType = Format is TarEntryFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
            }
            else
            {
                throw new IOException(string.Format(SR.TarUnsupportedFile, fullPath));
            }

            TarEntry entry = Format switch
            {
                TarEntryFormat.V7 => new V7TarEntry(entryType, entryName),
                TarEntryFormat.Ustar => new UstarTarEntry(entryType, entryName),
                TarEntryFormat.Pax => new PaxTarEntry(entryType, entryName),
                TarEntryFormat.Gnu => new GnuTarEntry(entryType, entryName),
                _ => throw new FormatException(string.Format(SR.TarInvalidFormat, Format)),
            };

            FileSystemInfo info = attributes.HasFlag(FileAttributes.Directory) ? new DirectoryInfo(fullPath) : new FileInfo(fullPath);

            entry._header._mTime = info.LastWriteTimeUtc;
            entry._header._aTime = info.LastAccessTimeUtc;
            entry._header._cTime = info.LastWriteTimeUtc; // There is no "change time" property

            entry.Mode = DefaultWindowsMode;

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
                    Options = fileOptions
                };

                Debug.Assert(entry._header._dataStream == null);
                entry._header._dataStream = new FileStream(fullPath, options);
            }

            return entry;
        }
    }
}
