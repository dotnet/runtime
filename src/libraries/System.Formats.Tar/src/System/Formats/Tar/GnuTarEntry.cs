// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Tar
{
    /// <summary>
    /// Represents a tar entry from an archive of the GNU format.
    /// </summary>
    /// <remarks>Even though the <see cref="TarEntryFormat.Gnu"/> format is not POSIX compatible, it implements and supports the Unix-specific fields that were defined in the POSIX IEEE P1003.1 standard from 1988: <c>devmajor</c>, <c>devminor</c>, <c>gname</c> and <c>uname</c>.</remarks>
    public sealed class GnuTarEntry : PosixTarEntry
    {
        // Constructor called when reading a TarEntry from a TarReader.
        internal GnuTarEntry(TarHeader header, TarReader readerOfOrigin)
            : base(header, readerOfOrigin, TarEntryFormat.Gnu)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="GnuTarEntry"/> instance with the specified entry type and entry name.
        /// </summary>
        /// <param name="entryType">The type of the entry.</param>
        /// <param name="entryName">A string with the path and file name of this entry.</param>
        /// <remarks>
        /// <para>When creating an instance of <see cref="GnuTarEntry"/> using this constructor, the <see cref="AccessTime"/> and <see cref="ChangeTime"/> properties are set to <see langword="default" />, which in the entry header <c>atime</c> and <c>ctime</c> fields is written as null bytes. This ensures compatibility with other tools that are unable to read the <c>atime</c> and <c>ctime</c> in <see cref="TarEntryFormat.Gnu"/> entries, as these two fields are not POSIX compatible because other formats expect the <c>prefix</c> field in the same header location where <see cref="TarEntryFormat.Gnu"/> writes <c>atime</c> and <c>ctime</c>.</para>
        /// <para>When creating an instance using the <see cref="GnuTarEntry(TarEntryType, string)"/> constructor, only the following entry types are supported:</para>
        /// <list type="bullet">
        /// <item>In all platforms: <see cref="TarEntryType.Directory"/>, <see cref="TarEntryType.HardLink"/>, <see cref="TarEntryType.SymbolicLink"/>, <see cref="TarEntryType.RegularFile"/>.</item>
        /// <item>In Unix platforms only: <see cref="TarEntryType.BlockDevice"/>, <see cref="TarEntryType.CharacterDevice"/> and <see cref="TarEntryType.Fifo"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="entryName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><para><paramref name="entryName"/> is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="entryType"/> is not supported in the specified format.</para></exception>
        public GnuTarEntry(TarEntryType entryType, string entryName)
            : base(entryType, entryName, TarEntryFormat.Gnu, isGea: false)
        { }

        /// <summary>
        /// Initializes a new <see cref="GnuTarEntry"/> instance by converting the specified <paramref name="other"/> entry into the GNU format.
        /// </summary>
        /// <remarks>
        /// <para>When creating an instance of <see cref="GnuTarEntry"/> using this constructor, if <paramref name="other"/> is <see cref="TarEntryFormat.Gnu"/> or <see cref="TarEntryFormat.Pax"/>, then the <see cref="AccessTime"/> and <see cref="ChangeTime"/> properties are set to the same values set in <paramref name="other"/>. But if <paramref name="other"/> is of any other format, then <see cref="AccessTime"/> and <see cref="ChangeTime"/> are set to <see langword="default" />, which in the entry header <c>atime</c> and <c>ctime</c> fields is written as null bytes. This ensures compatibility with other tools that are unable to read the <c>atime</c> and <c>ctime</c> in <see cref="TarEntryFormat.Gnu"/> entries, as these two fields are not POSIX compatible because other formats expect the <c>prefix</c> field in the same header location where <see cref="TarEntryFormat.Gnu"/> writes <c>atime</c> and <c>ctime</c>.</para>
        /// </remarks>
        /// <exception cref="ArgumentException"><para><paramref name="other"/> is a <see cref="PaxGlobalExtendedAttributesTarEntry"/> and cannot be converted.</para>
        /// <para>-or-</para>
        /// <para>The entry type of <paramref name="other"/> is not supported for conversion to the GNU format.</para></exception>
        public GnuTarEntry(TarEntry other)
            : base(other, TarEntryFormat.Gnu)
        {
            // Some tools don't accept Gnu entries that have an atime/ctime.
            // We only copy atime/ctime for round-tripping between GnuTarEntries and clear it for other formats.
            if (other is GnuTarEntry gnuOther)
            {
                _header._aTime = gnuOther.AccessTime;
                _header._cTime = gnuOther.ChangeTime;
            }
            else
            {
                Debug.Assert(_header._aTime == default);
                Debug.Assert(_header._cTime == default);
            }
        }

        /// <summary>
        /// A timestamp that represents the last time the file represented by this entry was accessed. Setting a value for this property is not recommended because most TAR reading tools do not support it.
        /// </summary>
        /// <remarks>
        /// <para>In Unix platforms, this timestamp is commonly known as <c>atime</c>.</para>
        /// <para>Setting the value of this property to a value other than <see langword="default"/> may cause problems with other tools that read TAR files, because the <see cref="TarEntryFormat.Gnu"/> format writes the <c>atime</c> field where other formats would normally read and write the <c>prefix</c> field in the header. You should only set this property to something other than <see langword="default"/> if this entry will be read by tools that know how to correctly interpret the <c>atime</c> field of the <see cref="TarEntryFormat.Gnu"/> format.</para>
        /// </remarks>
        public DateTimeOffset AccessTime
        {
            get => _header._aTime;
            set
            {
                _header._aTime = value;
            }
        }

        /// <summary>
        /// A timestamp that represents the last time the metadata of the file represented by this entry was changed. Setting a value for this property is not recommended because most TAR reading tools do not support it.
        /// </summary>
        /// <remarks>
        /// <para>In Unix platforms, this timestamp is commonly known as <c>ctime</c>.</para>
        /// <para>Setting the value of this property to a value other than <see langword="default"/> may cause problems with other tools that read TAR files, because the <see cref="TarEntryFormat.Gnu"/> format writes the <c>ctime</c> field where other formats would normally read and write the <c>prefix</c> field in the header. You should only set this property to something other than <see langword="default"/> if this entry will be read by tools that know how to correctly interpret the <c>ctime</c> field of the <see cref="TarEntryFormat.Gnu"/> format.</para>
        /// </remarks>
        public DateTimeOffset ChangeTime
        {
            get => _header._cTime;
            set
            {
                _header._cTime = value;
            }
        }

        // Determines if the current instance's entry type supports setting a data stream.
        internal override bool IsDataStreamSetterSupported() => EntryType is TarEntryType.RegularFile;
    }
}
