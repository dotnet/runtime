// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Represents a tar entry from an archive of the V7 format.
    /// </summary>
    public sealed class V7TarEntry : TarEntry
    {
        // Constructor used when reading an existing archive.
        internal V7TarEntry(TarHeader header, TarReader readerOfOrigin)
            : base(header, readerOfOrigin)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="V7TarEntry"/> instance with the specified entry type and entry name.
        /// </summary>
        /// <param name="entryType">The type of the entry.</param>
        /// <param name="entryName">A string with the path and file name of this entry.</param>
        /// <exception cref="ArgumentException"><paramref name="entryName"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">The entry type is not supported for creating an entry.</exception>
        /// <remarks>When creating an instance using the <see cref="V7TarEntry(TarEntryType, string)"/> constructor, only the following entry types are supported: <see cref="TarEntryType.Directory"/>, <see cref="TarEntryType.HardLink"/>, <see cref="TarEntryType.SymbolicLink"/> and <see cref="TarEntryType.V7RegularFile"/>.</remarks>
        public V7TarEntry(TarEntryType entryType, string entryName)
            : base(entryType, entryName, TarFormat.V7)
        {
        }

        // Determines if the current instance's entry type supports setting a data stream.
        internal override bool IsDataStreamSetterSupported() => EntryType == TarEntryType.V7RegularFile;
    }
}
