// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.CodeSign.Blobs
{
    [GenerateReaderWriter]
    [BigEndian]
    internal sealed partial class CodeDirectoryBaselineHeader
    {
        public BlobMagic Magic;
        public uint Size;
        public CodeDirectoryVersion Version;
        public CodeDirectoryFlags Flags;
        public uint HashesOffset;
        public uint IdentifierOffset;
        public uint SpecialSlotCount;
        public uint CodeSlotCount;
        public uint ExecutableLength;
        public byte HashSize;
        public HashType HashType;
        public byte Platform;
        public byte Log2PageSize;
        public uint Reserved;
        // I could not find documentation on why extra padding is present in the output of 'codesign', but it is
        public uint _UnknownPadding;
    }
}
