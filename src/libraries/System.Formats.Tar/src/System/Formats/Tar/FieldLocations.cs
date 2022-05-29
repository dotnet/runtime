// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    // Specifies the position of the first byte of each header field.
    internal static class FieldLocations
    {
        // Common attributes

        internal const ushort Name = 0;
        internal const ushort Mode = FieldLengths.Name;
        internal const ushort Uid = Mode + FieldLengths.Mode;
        internal const ushort Gid = Uid + FieldLengths.Uid;
        internal const ushort Size = Gid + FieldLengths.Gid;
        internal const ushort MTime = Size + FieldLengths.Size;
        internal const ushort Checksum = MTime + FieldLengths.MTime;
        internal const ushort TypeFlag = Checksum + FieldLengths.Checksum;
        internal const ushort LinkName = TypeFlag + FieldLengths.TypeFlag;

        // POSIX and GNU shared attributes

        internal const ushort Magic = LinkName + FieldLengths.LinkName;
        internal const ushort Version = Magic + FieldLengths.Magic;
        internal const ushort UName = Version + FieldLengths.Version;
        internal const ushort GName = UName + FieldLengths.UName;
        internal const ushort DevMajor = GName + FieldLengths.GName;
        internal const ushort DevMinor = DevMajor + FieldLengths.DevMajor;

        // POSIX attributes

        internal const ushort Prefix = DevMinor + FieldLengths.DevMinor;

        // GNU attributes

        internal const ushort ATime = DevMinor + FieldLengths.DevMinor;
        internal const ushort CTime = ATime + FieldLengths.ATime;
        internal const ushort Offset = CTime + FieldLengths.CTime;
        internal const ushort LongNames = Offset + FieldLengths.Offset;
        internal const ushort Unused = LongNames + FieldLengths.LongNames;
        internal const ushort Sparse = Unused + FieldLengths.Unused;
        internal const ushort IsExtended = Sparse + FieldLengths.Sparse;
        internal const ushort RealSize = IsExtended + FieldLengths.IsExtended;

        internal const ushort GnuUnused = CTime + FieldLengths.CTime;

        // Padding lengths depending on format

        internal const ushort V7Padding = LinkName + FieldLengths.LinkName;
        internal const ushort PosixPadding = Prefix + FieldLengths.Prefix;
        internal const ushort GnuPadding = RealSize + FieldLengths.RealSize;
    }
}
