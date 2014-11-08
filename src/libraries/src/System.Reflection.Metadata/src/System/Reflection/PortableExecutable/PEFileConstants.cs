// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.PortableExecutable
{
    internal static class PEFileConstants
    {
        internal const ushort DosSignature = 0x5A4D;     // MZ
        internal const int PESignatureOffsetLocation = 0x3C;
        internal const uint PESignature = 0x00004550;    // PE00
        internal const int BasicPEHeaderSize = PESignatureOffsetLocation;
        internal const int SizeofCOFFFileHeader = 20;
        internal const int SizeofOptionalHeaderStandardFields32 = 28;
        internal const int SizeofOptionalHeaderStandardFields64 = 24;
        internal const int SizeofOptionalHeaderNTAdditionalFields32 = 68;
        internal const int SizeofOptionalHeaderNTAdditionalFields64 = 88;
        internal const int NumberofOptionalHeaderDirectoryEntries = 16;
        internal const int SizeofOptionalHeaderDirectoriesEntries = 16 * 8;
        internal const int SizeofSectionHeader = 40;
        internal const int SizeofSectionName = 8;
        internal const int SizeofResourceDirectory = 16;
        internal const int SizeofResourceDirectoryEntry = 8;
    }
}