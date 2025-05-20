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

        // Constructor called when reading a TarEntry from a TarReader.
        internal PaxTarEntry(TarHeader header, TarReader readerOfOrigin)
            : base(header, readerOfOrigin, TarEntryFormat.Pax)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="PaxTarEntry"/> instance with the specified entry type and entry name.
        /// </summary>
        /// <param name="entryType">The type of the entry.</param>
        /// <param name="entryName">A string with the path and file name of this entry.</param>
        /// <remarks><para>When creating an instance using the <see cref="PaxTarEntry(TarEntryType, string)"/> constructor, only the following entry types are supported:</para>
        /// <list type="bullet">
        /// <item>In all platforms: <see cref="TarEntryType.Directory"/>, <see cref="TarEntryType.HardLink"/>, <see cref="TarEntryType.SymbolicLink"/>, <see cref="TarEntryType.RegularFile"/>.</item>
        /// <item>In Unix platforms only: <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> and <see cref="TarEntryType.Fifo"/>.</item>
        /// </list>
        /// <para>Use the <see cref="PaxTarEntry(TarEntryType, string, IEnumerable{KeyValuePair{string, string}})"/> constructor to include extended attributes when creating the entry.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="entryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><para><paramref name="entryName"/> is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="entryType"/> is not supported in the specified format.</para></exception>
        public PaxTarEntry(TarEntryType entryType, string entryName)
            : base(entryType, entryName, TarEntryFormat.Pax, isGea: false)
        {
            _header._prefix = string.Empty;
        }

        /// <summary>
        /// Initializes a new <see cref="PaxTarEntry"/> instance with the specified entry type, entry name and extended attributes.
        /// </summary>
        /// <param name="entryType">The type of the entry.</param>
        /// <param name="entryName">A string with the path and file name of this entry.</param>
        /// <param name="extendedAttributes">An enumeration of string key-value pairs that represents the metadata to include in the Extended Attributes entry that precedes the current entry.</param>
        /// <remarks>When creating an instance using the <see cref="PaxTarEntry(TarEntryType, string)"/> constructor, only the following entry types are supported:
        /// <list type="bullet">
        /// <item>In all platforms: <see cref="TarEntryType.Directory"/>, <see cref="TarEntryType.HardLink"/>, <see cref="TarEntryType.SymbolicLink"/>, <see cref="TarEntryType.RegularFile"/>.</item>
        /// <item>In Unix platforms only: <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> and <see cref="TarEntryType.Fifo"/>.</item>
        /// </list>
        /// The specified <paramref name="extendedAttributes"/> are additional attributes to be used for the entry.
        /// <para>It may include PAX attributes like:</para>
        /// <list type="bullet">
        /// <item>Access time, under the name <c>atime</c>, as a <see cref="double"/> number.</item>
        /// <item>Change time, under the name <c>ctime</c>, as a <see cref="double"/> number.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="extendedAttributes"/> or <paramref name="entryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><para><paramref name="entryName"/> is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="entryType"/> is not supported in the specified format.</para></exception>
        public PaxTarEntry(TarEntryType entryType, string entryName, IEnumerable<KeyValuePair<string, string>> extendedAttributes)
            : base(entryType, entryName, TarEntryFormat.Pax, isGea: false)
        {
            ArgumentNullException.ThrowIfNull(extendedAttributes);

            _header._prefix = string.Empty;
            _header.AddExtendedAttributes(extendedAttributes);
        }

        /// <summary>
        /// Initializes a new <see cref="PaxTarEntry"/> instance by converting the specified <paramref name="other"/> entry into the PAX format.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="other"/> is a <see cref="PaxGlobalExtendedAttributesTarEntry"/> and cannot be converted.</para>
        /// <para>-or-</para>
        /// <para>The entry type of <paramref name="other"/> is not supported for conversion to the PAX format.</para>
        /// </exception>
        /// <remarks>When converting a <see cref="GnuTarEntry"/> to <see cref="PaxTarEntry"/> using this constructor, the <see cref="GnuTarEntry.AccessTime"/> and <see cref="GnuTarEntry.ChangeTime"/> values will get transfered to the <see cref="ExtendedAttributes" /> dictionary only if their values are not <see langword="default"/> (which is <see cref="DateTimeOffset.MinValue"/>).</remarks>
        public PaxTarEntry(TarEntry other)
            : base(other, TarEntryFormat.Pax)
        {
            if (other._header._format is TarEntryFormat.Ustar or TarEntryFormat.Pax)
            {
                _header._prefix = other._header._prefix;
            }

            if (other is PaxTarEntry paxOther)
            {
                _header.AddExtendedAttributes(paxOther.ExtendedAttributes);
            }
            else if (other is GnuTarEntry gnuOther)
            {
                if (gnuOther.AccessTime != default)
                {
                    _header.ExtendedAttributes[TarHeader.PaxEaATime] = TarHelpers.GetTimestampStringFromDateTimeOffset(gnuOther.AccessTime);
                }
                if (gnuOther.ChangeTime != default)
                {
                    _header.ExtendedAttributes[TarHeader.PaxEaCTime] = TarHelpers.GetTimestampStringFromDateTimeOffset(gnuOther.ChangeTime);
                }
            }
        }

        /// <summary>
        /// Returns the extended attributes for this entry.
        /// </summary>
        /// <remarks>The extended attributes are specified when constructing an entry and updated with additional attributes when the entry is written. Use <see cref="PaxTarEntry(TarEntryType, string, IEnumerable{KeyValuePair{string, string}})"/> to append custom extended attributes.
        /// <para>The following common PAX attributes may be included:</para>
        /// <list type="bullet">
        /// <item>Modification time, under the name <c>mtime</c>, as a <see cref="double"/> number.</item>
        /// <item>Access time, under the name <c>atime</c>, as a <see cref="double"/> number.</item>
        /// <item>Change time, under the name <c>ctime</c>, as a <see cref="double"/> number.</item>
        /// <item>Path, under the name <c>path</c>, as a string.</item>
        /// <item>Group name, under the name <c>gname</c>, as a string.</item>
        /// <item>User name, under the name <c>uname</c>, as a string.</item>
        /// <item>File length, under the name <c>size</c>, as an <see cref="int"/>.</item>
        /// </list>
        /// </remarks>
        public IReadOnlyDictionary<string, string> ExtendedAttributes => _readOnlyExtendedAttributes ??= _header.ExtendedAttributes.AsReadOnly();

        // Determines if the current instance's entry type supports setting a data stream.
        internal override bool IsDataStreamSetterSupported() => EntryType == TarEntryType.RegularFile;
    }
}
