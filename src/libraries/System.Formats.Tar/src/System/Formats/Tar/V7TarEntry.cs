// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Represents a tar entry from an archive of the V7 format.
    /// </summary>
    public sealed class V7TarEntry : TarEntry
    {
        // Constructor called when reading a TarEntry from a TarReader.
        internal V7TarEntry(TarHeader header, TarReader readerOfOrigin)
            : base(header, readerOfOrigin, TarEntryFormat.V7)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="V7TarEntry"/> instance with the specified entry type and entry name.
        /// </summary>
        /// <param name="entryType">The type of the entry.</param>
        /// <param name="entryName">A string with the path and file name of this entry.</param>
        /// <remarks>When creating an instance using the <see cref="V7TarEntry(TarEntryType, string)"/> constructor, only the following entry types are supported: <see cref="TarEntryType.Directory"/>, <see cref="TarEntryType.HardLink"/>, <see cref="TarEntryType.SymbolicLink"/> and <see cref="TarEntryType.V7RegularFile"/>.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="entryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><para><paramref name="entryName"/> is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="entryType"/> is not supported for creating an entry.</para></exception>
        public V7TarEntry(TarEntryType entryType, string entryName)
            : base(entryType, entryName, TarEntryFormat.V7, isGea: false)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="V7TarEntry"/> instance by converting the specified <paramref name="other"/> entry into the V7 format.
        /// </summary>
        /// <exception cref="ArgumentException"><para><paramref name="other"/> is a <see cref="PaxGlobalExtendedAttributesTarEntry"/> and cannot be converted.</para>
        /// <para>-or-</para>
        /// <para>The entry type of <paramref name="other"/> is not supported for conversion to the V7 format.</para></exception>
        public V7TarEntry(TarEntry other)
            : base(other, TarEntryFormat.V7)
        {
        }

        // Determines if the current instance's entry type supports setting a data stream.
        internal override bool IsDataStreamSetterSupported() => EntryType == TarEntryType.V7RegularFile;
    }
}
