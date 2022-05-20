// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
    internal partial struct TarHeader
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
        private const string PaxEaATime = "atime";
        private const string PaxEaCTime = "ctime";
        private const string PaxEaMTime = "mtime";
        private const string PaxEaSize = "size";
        private const string PaxEaDevMajor = "devmajor";
        private const string PaxEaDevMinor = "devminor";

        internal Stream? _dataStream;

        // Position in the stream where the data ends in this header.
        internal long _endOfHeaderAndDataAndBlockAlignment;

        internal TarFormat _format;

        // Common attributes

        internal string _name;
        internal int _mode;
        internal int _uid;
        internal int _gid;
        internal long _size;
        internal DateTimeOffset _mTime;
        internal int _checksum;
        internal TarEntryType _typeFlag;
        internal string _linkName;

        // POSIX and GNU shared attributes

        internal string _magic;
        internal string _version;
        internal string _gName;
        internal string _uName;
        internal int _devMajor;
        internal int _devMinor;

        // POSIX attributes

        internal string _prefix;

        // PAX attributes

        internal Dictionary<string, string> _extendedAttributes;

        // GNU attributes

        internal DateTimeOffset _aTime;
        internal DateTimeOffset _cTime;

        // If the archive is GNU and the offset, longnames, unused, sparse, isextended and realsize
        // fields have data, we store it to avoid data loss, but we don't yet expose it publicly.
        internal byte[]? _gnuUnusedBytes;
    }
}
