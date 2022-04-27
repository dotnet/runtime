// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Represents a tar entry from an archive of the GNU format.
    /// </summary>
    /// <remarks>Even though the <see cref="TarFormat.Gnu"/> format is not POSIX compatible, it implements and supports the Unix-specific fields that were defined in the POSIX IEEE P1003.1 standard from 1988: <c>devmajor</c>, <c>devminor</c>, <c>gname</c> and <c>uname</c>.</remarks>
    public sealed class GnuTarEntry : PosixTarEntry
    {
        // Constructor used when reading an existing archive.
        internal GnuTarEntry(TarHeader header, TarReader readerOfOrigin)
            : base(header, readerOfOrigin)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="GnuTarEntry"/> instance with the specified entry type and entry name.
        /// </summary>
        /// <param name="entryType">The type of the entry.</param>
        /// <param name="entryName">A string with the path and file name of this entry.</param>
        /// <exception cref="ArgumentException"><paramref name="entryName"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">The entry type is not supported for creating an entry.</exception>
        /// <remarks>When creating an instance using the <see cref="GnuTarEntry(TarEntryType, string)"/> constructor, only the following entry types are supported:
        /// <list type="bullet">
        /// <item>In all platforms: <see cref="TarEntryType.Directory"/>, <see cref="TarEntryType.HardLink"/>, <see cref="TarEntryType.SymbolicLink"/>, <see cref="TarEntryType.RegularFile"/>.</item>
        /// <item>In Unix platforms only: <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> and <see cref="TarEntryType.Fifo"/>.</item>
        /// </list>
        /// </remarks>
        public GnuTarEntry(TarEntryType entryType, string entryName)
            : base(entryType, entryName, TarFormat.Gnu)
        {
        }

        /// <summary>
        /// A timestamp that represents the last time the file represented by this entry was accessed.
        /// </summary>
        /// <remarks>In Unix platforms, this timestamp is commonly known as <c>atime</c>.</remarks>
        public DateTimeOffset AccessTime
        {
            get => _header._aTime;
            set
            {
                if (value < DateTimeOffset.UnixEpoch)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _header._aTime = value;
            }
        }

        /// <summary>
        /// A timestamp that represents the last time the metadata of the file represented by this entry was changed.
        /// </summary>
        /// <remarks>In Unix platforms, this timestamp is commonly known as <c>ctime</c>.</remarks>
        public DateTimeOffset ChangeTime
        {
            get => _header._cTime;
            set
            {
                if (value < DateTimeOffset.UnixEpoch)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _header._cTime = value;
            }
        }

        // Determines if the current instance's entry type supports setting a data stream.
        internal override bool IsDataStreamSetterSupported() => EntryType is TarEntryType.RegularFile;
    }
}
