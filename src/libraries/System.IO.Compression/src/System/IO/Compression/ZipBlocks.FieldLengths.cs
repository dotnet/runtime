// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal partial struct ZipGenericExtraField
    {
        private static class FieldLengths
        {
            public const int Tag = sizeof(ushort);
            public const int Size = sizeof(ushort);
        }
    }

    internal partial struct Zip64ExtraField
    {
        internal static class FieldLengths
        {
            public const int UncompressedSize = sizeof(long);
            public const int CompressedSize = sizeof(long);
            public const int LocalHeaderOffset = sizeof(long);
            public const int StartDiskNumber = sizeof(uint);
        }
    }

    internal partial struct Zip64EndOfCentralDirectoryLocator
    {
        internal static class FieldLengths
        {
            public static readonly int Signature = SignatureConstantBytes.Length;
            public const int NumberOfDiskWithZip64EOCD = sizeof(uint);
            public const int OffsetOfZip64EOCD = sizeof(ulong);
            public const int TotalNumberOfDisks = sizeof(uint);
        }
    }

    internal partial struct Zip64EndOfCentralDirectoryRecord
    {
        private static class FieldLengths
        {
            public static readonly int Signature = SignatureConstantBytes.Length;
            public const int SizeOfThisRecord = sizeof(ulong);
            public const int VersionMadeBy = sizeof(ushort);
            public const int VersionNeededToExtract = sizeof(ushort);
            public const int NumberOfThisDisk = sizeof(uint);
            public const int NumberOfDiskWithStartOfCD = sizeof(uint);
            public const int NumberOfEntriesOnThisDisk = sizeof(ulong);
            public const int NumberOfEntriesTotal = sizeof(ulong);
            public const int SizeOfCentralDirectory = sizeof(ulong);
            public const int OffsetOfCentralDirectory = sizeof(ulong);
        }
    }

    internal readonly partial struct ZipLocalFileHeader
    {
        internal static class FieldLengths
        {
            public static readonly int Signature = SignatureConstantBytes.Length;
            public const int VersionNeededToExtract = sizeof(ushort);
            public const int GeneralPurposeBitFlags = sizeof(ushort);
            public const int CompressionMethod = sizeof(ushort);
            public const int LastModified = sizeof(ushort) + sizeof(ushort);
            public const int Crc32 = sizeof(uint);
            public const int CompressedSize = sizeof(uint);
            public const int UncompressedSize = sizeof(uint);
            public const int FilenameLength = sizeof(ushort);
            public const int ExtraFieldLength = sizeof(ushort);
        }

        internal readonly partial struct ZipDataDescriptor
        {
            internal static class FieldLengths
            {
                public static readonly int Signature = DataDescriptorSignatureConstantBytes.Length;
                public const int Crc32 = sizeof(uint);
                public const int CompressedSize = sizeof(uint);
                public const int UncompressedSize = sizeof(uint);
            }
        }

        internal readonly partial struct Zip64DataDescriptor
        {
            internal static class FieldLengths
            {
                public static readonly int Signature = DataDescriptorSignatureConstantBytes.Length;
                public const int Crc32 = sizeof(uint);
                public const int CompressedSize = sizeof(long);
                public const int UncompressedSize = sizeof(long);
            }
        }
    }

    internal partial struct ZipCentralDirectoryFileHeader
    {
        internal static class FieldLengths
        {
            public static readonly int Signature = SignatureConstantBytes.Length;
            public const int VersionMadeBySpecification = sizeof(byte);
            public const int VersionMadeByCompatibility = sizeof(byte);
            public const int VersionNeededToExtract = sizeof(ushort);
            public const int GeneralPurposeBitFlags = sizeof(ushort);
            public const int CompressionMethod = sizeof(ushort);
            public const int LastModified = sizeof(ushort) + sizeof(ushort);
            public const int Crc32 = sizeof(uint);
            public const int CompressedSize = sizeof(uint);
            public const int UncompressedSize = sizeof(uint);
            public const int FilenameLength = sizeof(ushort);
            public const int ExtraFieldLength = sizeof(ushort);
            public const int FileCommentLength = sizeof(ushort);
            public const int DiskNumberStart = sizeof(ushort);
            public const int InternalFileAttributes = sizeof(ushort);
            public const int ExternalFileAttributes = sizeof(uint);
            public const int RelativeOffsetOfLocalHeader = sizeof(uint);
        }
    }

    internal partial struct ZipEndOfCentralDirectoryBlock
    {
        internal static class FieldLengths
        {
            public static readonly int Signature = SignatureConstantBytes.Length;
            public const int NumberOfThisDisk = sizeof(ushort);
            public const int NumberOfTheDiskWithTheStartOfTheCentralDirectory = sizeof(ushort);
            public const int NumberOfEntriesInTheCentralDirectoryOnThisDisk = sizeof(ushort);
            public const int NumberOfEntriesInTheCentralDirectory = sizeof(ushort);
            public const int SizeOfCentralDirectory = sizeof(uint);
            public const int OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = sizeof(uint);
            public const int ArchiveCommentLength = sizeof(ushort);
        }
    }
}
