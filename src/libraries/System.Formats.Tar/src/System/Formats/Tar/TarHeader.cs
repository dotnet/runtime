// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Formats.Tar
{
    // Describes the header attributes from a tar archive entry.
    // Supported formats:
    // - 1979 Version 7 AT&T Unix Tar Command Format (v7).
    // - POSIX IEEE 1003.1-1988 Unix Standard Tar Format (ustar).
    // - POSIX IEEE 1003.1-2001 ("POSIX.1") Pax Interchange Tar Format (pax).
    // - GNU Tar Format (gnu).
    // Documentation: https://www.freebsd.org/cgi/man.cgi?query=tar&sektion=5
    internal sealed partial class TarHeader
    {
        // POSIX fields (shared by Ustar and PAX)
        private const string UstarMagic = "ustar\0";
        private const string UstarVersion = "00";

        // GNU-specific fields
        private const string GnuMagic = "ustar ";
        private const string GnuVersion = " \0";

        // Names of PAX extended attributes commonly found fields
        internal const string PaxEaName = "path";
        internal const string PaxEaLinkName = "linkpath";
        internal const string PaxEaMode = "mode";
        internal const string PaxEaGName = "gname";
        internal const string PaxEaUName = "uname";
        internal const string PaxEaGid = "gid";
        internal const string PaxEaUid = "uid";
        internal const string PaxEaATime = "atime";
        internal const string PaxEaCTime = "ctime";
        internal const string PaxEaMTime = "mtime";
        internal const string PaxEaSize = "size";
        internal const string PaxEaDevMajor = "devmajor";
        internal const string PaxEaDevMinor = "devminor";

        // Names of GNU sparse extended attributes (used with GNU sparse format 1.0 encoded via PAX)
        private const string PaxEaGnuSparseName = "GNU.sparse.name";
        private const string PaxEaGnuSparseRealSize = "GNU.sparse.realsize";
        private const string PaxEaGnuSparseMajor = "GNU.sparse.major";
        private const string PaxEaGnuSparseMinor = "GNU.sparse.minor";

        internal Stream? _dataStream;
        internal long _dataOffset;

        // Position in the stream where the data ends in this header.
        internal long _endOfHeaderAndDataAndBlockAlignment;

        internal TarEntryFormat _format;

        // Common attributes

        internal string _name;
        internal int _mode;
        internal int _uid;
        internal int _gid;
        internal long _size;
        internal DateTimeOffset _mTime;
        internal int _checksum;
        internal TarEntryType _typeFlag;
        internal string? _linkName;

        // POSIX and GNU shared attributes

        internal string _magic;
        internal string _version;
        internal string? _gName;
        internal string? _uName;
        internal int _devMajor;
        internal int _devMinor;

        // POSIX attributes

        internal string? _prefix;

        // PAX attributes

        private Dictionary<string, string>? _ea;
        internal Dictionary<string, string> ExtendedAttributes => _ea ??= new Dictionary<string, string>();

        // When a GNU sparse 1.0 PAX entry is read, the real (expanded) file size is stored here.
        // This is separate from _size which holds the archive data size and is used for data stream reading.
        internal long _gnuSparseRealSize;

        // Set to true when GNU.sparse.major=1 is present in the PAX extended attributes,
        // indicating this is a GNU sparse format 1.0 entry whose data section contains an
        // embedded sparse map followed by the packed data segments.
        internal bool _isGnuSparse10;

        // When _isGnuSparse10 is true, this wraps _dataStream and presents the expanded virtual
        // file content. _dataStream remains the raw (condensed) stream so that TarWriter can
        // round-trip the original sparse data and AdvanceDataStreamIfNeeded works without
        // special-casing.
        internal GnuSparseStream? _gnuSparseDataStream;

        // GNU attributes

        internal DateTimeOffset _aTime;
        internal DateTimeOffset _cTime;

        // Constructor called when creating an entry with default common fields.
        internal TarHeader(TarEntryFormat format, string name = "", int mode = 0, DateTimeOffset mTime = default, TarEntryType typeFlag = TarEntryType.RegularFile)
        {
            _format = format;
            _name = name;
            _mode = mode;
            _mTime = mTime;
            _typeFlag = typeFlag;
            _magic = GetMagicForFormat(format);
            _version = GetVersionForFormat(format);
            _dataOffset = -1;
        }

        // Constructor called when creating an entry using the common fields from another entry.
        // The *TarEntry constructor calling this should take care of setting any format-specific fields.
        internal TarHeader(TarEntryFormat format, TarEntryType typeFlag, TarHeader other)
            : this(format, other._name, other._mode, other._mTime, typeFlag)
        {
            _uid = other._uid;
            _gid = other._gid;
            _size = other._size;
            _checksum = other._checksum;
            _linkName = other._linkName;
            _dataStream = other._dataStream;
            _gnuSparseRealSize = other._gnuSparseRealSize;
            _isGnuSparse10 = other._isGnuSparse10;
            _gnuSparseDataStream = other._gnuSparseDataStream;
        }

        internal void AddExtendedAttributes(IEnumerable<KeyValuePair<string, string>> existing)
        {
            Debug.Assert(_ea == null);
            Debug.Assert(existing != null);

            using IEnumerator<KeyValuePair<string, string>> enumerator = existing.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, string> kvp = enumerator.Current;

                int index = kvp.Key.AsSpan().IndexOfAny('=', '\n');
                if (index >= 0)
                {
                    throw new ArgumentException(SR.Format(SR.TarExtAttrDisallowedKeyChar, kvp.Key, kvp.Key[index] == '\n' ? "\\n" : kvp.Key[index]));
                }
                if (kvp.Value.Contains('\n'))
                {
                    throw new ArgumentException(SR.Format(SR.TarExtAttrDisallowedValueChar, kvp.Key, "\\n"));
                }

                _ea ??= new Dictionary<string, string>();

                _ea.Add(kvp.Key, kvp.Value);
            }
        }

        private static string GetMagicForFormat(TarEntryFormat format) => format switch
        {
            TarEntryFormat.Ustar or TarEntryFormat.Pax => UstarMagic,
            TarEntryFormat.Gnu => GnuMagic,
            _ => string.Empty,
        };

        private static string GetVersionForFormat(TarEntryFormat format) => format switch
        {
            TarEntryFormat.Ustar or TarEntryFormat.Pax => UstarVersion,
            TarEntryFormat.Gnu => GnuVersion,
            _ => string.Empty,
        };

        // Stores the archive stream's position where we know the current entry's data section begins,
        // if the archive stream is seekable. Otherwise, -1.
        private static void SetDataOffset(TarHeader header, Stream archiveStream) =>
            header._dataOffset = archiveStream.CanSeek ? archiveStream.Position : -1;

        // Synchronizes the extended attributes dictionary with the value of a property.
        // Only updates if the format is PAX and the ExtendedAttributes dictionary has been initialized.
        // When maxUtf8ByteLength is 0 (default), the value is always added to EA if non-empty.
        // This is intentional for "path" and "linkpath" which always belong in extended attributes.
        internal void SyncStringExtendedAttribute(string key, string? value, int maxUtf8ByteLength = 0)
        {
            if (_format == TarEntryFormat.Pax && _ea is not null)
            {
                if (!string.IsNullOrEmpty(value) && GetUtf8TextLength(value) > maxUtf8ByteLength)
                {
                    _ea[key] = value;
                }
                else
                {
                    _ea.Remove(key);
                }
            }
        }

        // Synchronizes the extended attributes dictionary with a timestamp property.
        // Only updates if the format is PAX and the ExtendedAttributes dictionary has been initialized.
        internal void SyncTimestampExtendedAttribute(string key, DateTimeOffset value)
        {
            if (_format == TarEntryFormat.Pax && _ea is not null)
            {
                _ea[key] = TarHelpers.GetTimestampStringFromDateTimeOffset(value);
            }
        }

        // Synchronizes the extended attributes dictionary with a numeric property.
        // Only updates if the format is PAX and the ExtendedAttributes dictionary has been initialized.
        // Uses the same logic as CollectExtendedAttributesFromStandardFieldsIfNeeded to determine
        // whether to add or remove the attribute.
        internal void SyncNumericExtendedAttribute(string key, int value, int maxNonextendedValue)
        {
            if (_format == TarEntryFormat.Pax && _ea is not null)
            {
                if (value > maxNonextendedValue)
                {
                    _ea[key] = value.ToString();
                }
                else
                {
                    _ea.Remove(key);
                }
            }
        }

        internal Dictionary<string, string> GetPopulatedExtendedAttributes()
        {
            PopulateExtendedAttributesFromStandardFields(ExtendedAttributes);
            return ExtendedAttributes;
        }

        // Ensures standard fields are present in the extended attributes dictionary
        // without removing any existing entries. Used for populating the EA dictionary
        // for read access. Delegates to the shared helper with removeIfUnneeded: false.
        private void PopulateExtendedAttributesFromStandardFields(Dictionary<string, string> ea)
        {
            AddOrUpdateStandardFieldExtendedAttributes(ea, removeIfUnneeded: false);
        }

        // Shared helper that adds standard header field values to extended attributes.
        // When removeIfUnneeded is true (write-time), entries that fit in standard fields
        // are removed from the dictionary. When false (read-time/populate), only entries
        // that exceed standard field capacity are added — existing keys are never removed.
        private void AddOrUpdateStandardFieldExtendedAttributes(Dictionary<string, string> ea, bool removeIfUnneeded)
        {
            ea[PaxEaName] = _name;
            ea[PaxEaMTime] = TarHelpers.GetTimestampStringFromDateTimeOffset(_mTime);

            AddOrRemoveStringField(ea, PaxEaGName, _gName, FieldLengths.GName, removeIfUnneeded);
            AddOrRemoveStringField(ea, PaxEaUName, _uName, FieldLengths.UName, removeIfUnneeded);

            if (!string.IsNullOrEmpty(_linkName))
            {
                // The LinkName is stored unconditionally (not doing so might
                // break users depending on existing behavior).
                Debug.Assert(_typeFlag is TarEntryType.SymbolicLink or TarEntryType.HardLink);
                ea[PaxEaLinkName] = _linkName;
            }

            AddOrRemoveNumericField(ea, PaxEaSize, _size, Octal12ByteFieldMaxValue, removeIfUnneeded);
            AddOrRemoveNumericField(ea, PaxEaUid, _uid, Octal8ByteFieldMaxValue, removeIfUnneeded);
            AddOrRemoveNumericField(ea, PaxEaGid, _gid, Octal8ByteFieldMaxValue, removeIfUnneeded);
            AddOrRemoveNumericField(ea, PaxEaDevMajor, _devMajor, Octal8ByteFieldMaxValue, removeIfUnneeded);
            AddOrRemoveNumericField(ea, PaxEaDevMinor, _devMinor, Octal8ByteFieldMaxValue, removeIfUnneeded);

            static void AddOrRemoveStringField(Dictionary<string, string> ea, string key, string? value, int maxUtf8ByteLength, bool removeIfUnneeded)
            {
                if (!string.IsNullOrEmpty(value) && GetUtf8TextLength(value) > maxUtf8ByteLength)
                {
                    ea[key] = value;
                }
                else if (removeIfUnneeded)
                {
                    ea.Remove(key);
                }
            }

            static void AddOrRemoveNumericField(Dictionary<string, string> ea, string key, long value, long maxNonextendedValue, bool removeIfUnneeded)
            {
                if (value > maxNonextendedValue)
                {
                    ea[key] = value.ToString();
                }
                else if (removeIfUnneeded)
                {
                    ea.Remove(key);
                }
            }
        }
    }
}
