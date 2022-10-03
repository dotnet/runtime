// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Formats.Tar
{
    /// <summary>
    /// Represents a Global Extended Attributes TAR entry from an archive of the PAX format.
    /// </summary>
    public sealed class PaxGlobalExtendedAttributesTarEntry : PosixTarEntry
    {
        private ReadOnlyDictionary<string, string>? _readOnlyGlobalExtendedAttributes;

        // Constructor used when reading an existing archive.
        internal PaxGlobalExtendedAttributesTarEntry(TarHeader header, TarReader readerOfOrigin)
            : base(header, readerOfOrigin, TarEntryFormat.Pax)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="PaxGlobalExtendedAttributesTarEntry"/> instance with the specified Global Extended Attributes enumeration.
        /// </summary>
        /// <param name="globalExtendedAttributes">An enumeration of string key-value pairs that represents the metadata to include as Global Extended Attributes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="globalExtendedAttributes"/> is <see langword="null"/>.</exception>
        public PaxGlobalExtendedAttributesTarEntry(IEnumerable<KeyValuePair<string, string>> globalExtendedAttributes)
            : base(TarEntryType.GlobalExtendedAttributes, TarHeader.GlobalHeadFormatPrefix, TarEntryFormat.Pax, isGea: true)
        {
            ArgumentNullException.ThrowIfNull(globalExtendedAttributes);
            _header.InitializeExtendedAttributesWithExisting(globalExtendedAttributes);
        }

        /// <summary>
        /// Returns the global extended attributes stored in this entry.
        /// </summary>
        public IReadOnlyDictionary<string, string> GlobalExtendedAttributes => _readOnlyGlobalExtendedAttributes ??= _header.ExtendedAttributes.AsReadOnly();

        // Determines if the current instance's entry type supports setting a data stream.
        internal override bool IsDataStreamSetterSupported() => false;
    }
}
