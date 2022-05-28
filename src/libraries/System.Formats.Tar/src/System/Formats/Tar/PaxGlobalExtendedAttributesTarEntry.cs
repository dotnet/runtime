// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Formats.Tar
{
    /// <summary>
    /// Represents a Global Extended Attributes tar entry from an archive of the PAX format.
    /// </summary>
    public sealed class PaxGlobalExtendedAttributesTarEntry : PosixTarEntry
    {
        private ReadOnlyDictionary<string, string>? _readOnlyGlobalExtendedAttributes;

        // Constructor used when reading an existing archive.
        internal PaxGlobalExtendedAttributesTarEntry(TarHeader header, TarReader readerOfOrigin)
            : base(header, readerOfOrigin)
        {
            _readOnlyGlobalExtendedAttributes = null;
        }

        /// <summary>
        /// Initializes a new <see cref="PaxGlobalExtendedAttributesTarEntry"/> instance with the specified Global Extended Attributes enumeration.
        /// </summary>
        /// <param name="globalExtendedAttributes">An enumeration of string key-value pairs that represents the metadata to include as Global Extended Attributes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="globalExtendedAttributes"/> is <see langword="null"/>.</exception>
        public PaxGlobalExtendedAttributesTarEntry(IEnumerable<KeyValuePair<string, string>> globalExtendedAttributes)
            : this(header: default, readerOfOrigin: null!)
        {
            ArgumentNullException.ThrowIfNull(globalExtendedAttributes);

            _header._extendedAttributes = new Dictionary<string, string>(globalExtendedAttributes);

            _header._name = TarHeader.GlobalHeadFormatPrefix; // Does not contain the sequence number, since that depends on the archive to write
            _header._mode = (int)TarHelpers.DefaultMode;
            _header._typeFlag = TarEntryType.GlobalExtendedAttributes;
            _header._linkName = string.Empty;
            _header._magic = string.Empty;
            _header._version = string.Empty;
            _header._gName = string.Empty;
            _header._uName = string.Empty;
        }

        /// <summary>
        /// Returns the global extended attributes stored in this entry.
        /// </summary>
        public IReadOnlyDictionary<string, string> GlobalExtendedAttributes
        {
            get
            {
                _header._extendedAttributes ??= new Dictionary<string, string>();
                return _readOnlyGlobalExtendedAttributes ??= _header._extendedAttributes.AsReadOnly();
            }
        }

        // Determines if the current instance's entry type supports setting a data stream.
        internal override bool IsDataStreamSetterSupported() => false;
    }
}
