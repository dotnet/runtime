// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    // Specifies the expected lengths of all the header fields in the supported formats.
    internal static class FieldLengths
    {
        private const ushort Path = 100;

        // Common attributes

        internal const ushort Name = Path;
        internal const ushort Mode = 8;
        internal const ushort Uid = 8;
        internal const ushort Gid = 8;
        internal const ushort Size = 12;
        internal const ushort MTime = 12;
        internal const ushort Checksum = 8;
        internal const ushort TypeFlag = 1;
        internal const ushort LinkName = Path;

        // POSIX and GNU shared attributes

        internal const ushort Magic = 6;
        internal const ushort Version = 2;
        internal const ushort UName = 32;
        internal const ushort GName = 32;
        internal const ushort DevMajor = 8;
        internal const ushort DevMinor = 8;

        // POSIX attributes

        internal const ushort Prefix = 155;

        // GNU attributes

        internal const ushort ATime = 12;
        internal const ushort CTime = 12;
        internal const ushort Offset = 12;
        internal const ushort LongNames = 4;
        internal const ushort Unused = 1;
        internal const ushort Sparse = 4 * (12 + 12);
        internal const ushort IsExtended = 1;
        internal const ushort RealSize = 12;

        // Padding lengths depending on format

        internal const ushort V7Padding = 255;
        internal const ushort PosixPadding = 12;

        internal const int AllGnuUnused = Offset + LongNames + Unused + Sparse + IsExtended + RealSize;

        internal const ushort GnuPadding = 17;
    }
}
