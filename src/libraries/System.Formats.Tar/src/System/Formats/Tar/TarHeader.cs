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

        // Global Extended Attribute entries have a special format in the Name field:
        // "{tmpFolder}/GlobalHead.{processId}.{GEAEntryNumber}"
        // Excludes ".{GEAEntryNumber}" because the number gets added on write.
        internal const string GlobalHeadFormatPrefix = "{0}/GlobalHead.{1}";

        internal Stream? _dataStream;

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

        // GNU attributes

        internal DateTimeOffset _aTime;
        internal DateTimeOffset _cTime;

        // If the archive is GNU and the offset, longnames, unused, sparse, isextended and realsize
        // fields have data, we store it to avoid data loss, but we don't yet expose it publicly.
        internal byte[]? _gnuUnusedBytes;

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
        }

        internal void InitializeExtendedAttributesWithExisting(IEnumerable<KeyValuePair<string, string>> existing)
        {
            Debug.Assert(_ea == null);
            _ea = new Dictionary<string, string>(existing);
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
    }
}
