// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Formats.Tar
{
    /// <summary>
    /// Represents a tar entry from an archive of the PAX format.
    /// </summary>
    public sealed class PaxTarEntry : PosixTarEntry
    {
        private ReadOnlyDictionary<string, string>? _readOnlyExtendedAttributes;

        // Constructor used when reading an existing archive.
        internal PaxTarEntry(TarHeader header, TarReader readerOfOrigin)
            : base(header, readerOfOrigin)
        {
            _header._extendedAttributes ??= new Dictionary<string, string>();
            _readOnlyExtendedAttributes = null;
        }

        /// <summary>
        /// Initializes a new <see cref="PaxTarEntry"/> instance with the specified entry type, entry name, and the default extended attributes.
        /// </summary>
        /// <param name="entryType">The type of the entry.</param>
        /// <param name="entryName">A string with the path and file name of this entry.</param>
        /// <exception cref="ArgumentException"><paramref name="entryName"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">The entry type is not supported for creating an entry.</exception>
        /// <remarks><para>When creating an instance using the <see cref="PaxTarEntry(TarEntryType, string)"/> constructor, only the following entry types are supported:</para>
        /// <list type="bullet">
        /// <item>In all platforms: <see cref="TarEntryType.Directory"/>, <see cref="TarEntryType.HardLink"/>, <see cref="TarEntryType.SymbolicLink"/>, <see cref="TarEntryType.RegularFile"/>.</item>
        /// <item>In Unix platforms only: <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> and <see cref="TarEntryType.Fifo"/>.</item>
        /// </list>
        /// <para>Use the <see cref="PaxTarEntry(TarEntryType, string, IEnumerable{KeyValuePair{string, string}})"/> constructor to include additional extended attributes when creating the entry.</para>
        /// <para>The following entries are always found in the Extended Attributes dictionary of any PAX entry:</para>
        /// <list type="bullet">
        /// <item>Modification time, under the name <c>mtime</c>, as a <see cref="double"/> number.</item>
        /// <item>Access time, under the name <c>atime</c>, as a <see cref="double"/> number.</item>
        /// <item>Change time, under the name <c>ctime</c>, as a <see cref="double"/> number.</item>
        /// <item>Path, under the name <c>path</c>, as a string.</item>
        /// </list>
        /// <para>The following entries are only found in the Extended Attributes dictionary of a PAX entry if certain conditions are met:</para>
        /// <list type="bullet">
        /// <item>Group name, under the name <c>gname</c>, as a string, if it is larger than 32 bytes.</item>
        /// <item>User name, under the name <c>uname</c>, as a string, if it is larger than 32 bytes.</item>
        /// <item>File length, under the name <c>size</c>, as an <see cref="int"/>, if the string representation of the number is larger than 12 bytes.</item>
        /// </list>
        /// </remarks>
        public PaxTarEntry(TarEntryType entryType, string entryName)
            : base(entryType, entryName, TarFormat.Pax)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="PaxTarEntry"/> instance with the specified entry type, entry name and Extended Attributes enumeration.
        /// </summary>
        /// <param name="entryType">The type of the entry.</param>
        /// <param name="entryName">A string with the path and file name of this entry.</param>
        /// <param name="extendedAttributes">An enumeration of string key-value pairs that represents the metadata to include in the Extended Attributes entry that precedes the current entry.</param>
        /// <exception cref="ArgumentNullException"><paramref name="extendedAttributes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entryName"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">The entry type is not supported for creating an entry.</exception>
        /// <remarks>When creating an instance using the <see cref="PaxTarEntry(TarEntryType, string)"/> constructor, only the following entry types are supported:
        /// <list type="bullet">
        /// <item>In all platforms: <see cref="TarEntryType.Directory"/>, <see cref="TarEntryType.HardLink"/>, <see cref="TarEntryType.SymbolicLink"/>, <see cref="TarEntryType.RegularFile"/>.</item>
        /// <item>In Unix platforms only: <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> and <see cref="TarEntryType.Fifo"/>.</item>
        /// </list>
        /// The specified <paramref name="extendedAttributes"/> get appended to the default attributes, unless the specified enumeration overrides any of them.
        /// <para>The following entries are always found in the Extended Attributes dictionary of any PAX entry:</para>
        /// <list type="bullet">
        /// <item>Modification time, under the name <c>mtime</c>, as a <see cref="double"/> number.</item>
        /// <item>Access time, under the name <c>atime</c>, as a <see cref="double"/> number.</item>
        /// <item>Change time, under the name <c>ctime</c>, as a <see cref="double"/> number.</item>
        /// <item>Path, under the name <c>path</c>, as a string.</item>
        /// </list>
        /// <para>The following entries are only found in the Extended Attributes dictionary of a PAX entry if certain conditions are met:</para>
        /// <list type="bullet">
        /// <item>Group name, under the name <c>gname</c>, as a string, if it is larger than 32 bytes.</item>
        /// <item>User name, under the name <c>uname</c>, as a string, if it is larger than 32 bytes.</item>
        /// <item>File length, under the name <c>size</c>, as an <see cref="int"/>, if the string representation of the number is larger than 12 bytes.</item>
        /// </list>
        /// </remarks>
        public PaxTarEntry(TarEntryType entryType, string entryName, IEnumerable<KeyValuePair<string, string>> extendedAttributes)
            : base(entryType, entryName, TarFormat.Pax)
        {
            ArgumentNullException.ThrowIfNull(extendedAttributes);
            _header.ReplaceNormalAttributesWithExtended(extendedAttributes);
        }

        /// <summary>
        /// Returns the extended attributes for this entry.
        /// </summary>
        /// <remarks>The extended attributes are specified when constructing an entry. Use <see cref="PaxTarEntry(TarEntryType, string, IEnumerable{KeyValuePair{string, string}})"/> to append your own enumeration of extended attributes to the current entry on top of the default ones. Use <see cref="PaxTarEntry(TarEntryType, string)"/> to only use the default extended attributes.
        /// <para>The following entries are always found in the Extended Attributes dictionary of any PAX entry:</para>
        /// <list type="bullet">
        /// <item>Modification time, under the name <c>mtime</c>, as a <see cref="double"/> number.</item>
        /// <item>Access time, under the name <c>atime</c>, as a <see cref="double"/> number.</item>
        /// <item>Change time, under the name <c>ctime</c>, as a <see cref="double"/> number.</item>
        /// <item>Path, under the name <c>path</c>, as a string.</item>
        /// </list>
        /// <para>The following entries are only found in the Extended Attributes dictionary of a PAX entry if certain conditions are met:</para>
        /// <list type="bullet">
        /// <item>Group name, under the name <c>gname</c>, as a string, if it is larger than 32 bytes.</item>
        /// <item>User name, under the name <c>uname</c>, as a string, if it is larger than 32 bytes.</item>
        /// <item>File length, under the name <c>size</c>, as an <see cref="int"/>, if the string representation of the number is larger than 12 bytes.</item>
        /// </list>
        /// </remarks>
        public IReadOnlyDictionary<string, string> ExtendedAttributes
        {
            get
            {
                Debug.Assert(_header._extendedAttributes != null);
                return _readOnlyExtendedAttributes ??= _header._extendedAttributes.AsReadOnly();
            }
        }

        // Determines if the current instance's entry type supports setting a data stream.
        internal override bool IsDataStreamSetterSupported() => EntryType == TarEntryType.RegularFile;
    }
}
