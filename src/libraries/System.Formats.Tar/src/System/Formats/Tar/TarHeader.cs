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
        private const string PaxEaName = "path";
        private const string PaxEaLinkName = "linkpath";
        private const string PaxEaMode = "mode";
        private const string PaxEaGName = "gname";
        private const string PaxEaUName = "uname";
        private const string PaxEaGid = "gid";
        private const string PaxEaUid = "uid";
        internal const string PaxEaATime = "atime";
        internal const string PaxEaCTime = "ctime";
        private const string PaxEaMTime = "mtime";
        private const string PaxEaSize = "size";
        private const string PaxEaDevMajor = "devmajor";
        private const string PaxEaDevMinor = "devminor";

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
    }
}
