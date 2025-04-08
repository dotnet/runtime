// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal partial struct ZipGenericExtraField
    {
        internal static class FieldLocations
        {
            public const int Tag = 0;
            public const int Size = Tag + FieldLengths.Tag;
            public const int DynamicData = Size + FieldLengths.Size;
        }
    }

    internal partial struct Zip64ExtraField
    {
        internal static class FieldLocations
        {
            public const int Tag = ZipGenericExtraField.FieldLocations.Tag;
            public const int Size = ZipGenericExtraField.FieldLocations.Size;
            public const int UncompressedSize = ZipGenericExtraField.FieldLocations.DynamicData;
            public const int CompressedSize = UncompressedSize + FieldLengths.UncompressedSize;
            public const int LocalHeaderOffset = CompressedSize + FieldLengths.CompressedSize;
            public const int StartDiskNumber = LocalHeaderOffset + FieldLengths.LocalHeaderOffset;
        }
    }

    internal partial struct Zip64EndOfCentralDirectoryLocator
    {
        private static class FieldLocations
        {
            public const int Signature = 0;
            public static readonly int NumberOfDiskWithZip64EOCD = Signature + FieldLengths.Signature;
            public static readonly int OffsetOfZip64EOCD = NumberOfDiskWithZip64EOCD + FieldLengths.NumberOfDiskWithZip64EOCD;
            public static readonly int TotalNumberOfDisks = OffsetOfZip64EOCD + FieldLengths.OffsetOfZip64EOCD;
        }
    }

    internal partial struct Zip64EndOfCentralDirectoryRecord
    {
        private static class FieldLocations
        {
            public const int Signature = 0;
            public static readonly int SizeOfThisRecord = Signature + FieldLengths.Signature;
            public static readonly int VersionMadeBy = SizeOfThisRecord + FieldLengths.SizeOfThisRecord;
            public static readonly int VersionNeededToExtract = VersionMadeBy + FieldLengths.VersionMadeBy;
            public static readonly int NumberOfThisDisk = VersionNeededToExtract + FieldLengths.VersionNeededToExtract;
            public static readonly int NumberOfDiskWithStartOfCD = NumberOfThisDisk + FieldLengths.NumberOfThisDisk;
            public static readonly int NumberOfEntriesOnThisDisk = NumberOfDiskWithStartOfCD + FieldLengths.NumberOfDiskWithStartOfCD;
            public static readonly int NumberOfEntriesTotal = NumberOfEntriesOnThisDisk + FieldLengths.NumberOfEntriesOnThisDisk;
            public static readonly int SizeOfCentralDirectory = NumberOfEntriesTotal + FieldLengths.NumberOfEntriesTotal;
            public static readonly int OffsetOfCentralDirectory = SizeOfCentralDirectory + FieldLengths.SizeOfCentralDirectory;
        }
    }

    internal readonly partial struct ZipLocalFileHeader
    {
        internal static class FieldLocations
        {
            public const int Signature = 0;
            public static readonly int VersionNeededToExtract = Signature + FieldLengths.Signature;
            public static readonly int GeneralPurposeBitFlags = VersionNeededToExtract + FieldLengths.VersionNeededToExtract;
            public static readonly int CompressionMethod = GeneralPurposeBitFlags + FieldLengths.GeneralPurposeBitFlags;
            public static readonly int LastModified = CompressionMethod + FieldLengths.CompressionMethod;
            public static readonly int Crc32 = LastModified + FieldLengths.LastModified;
            public static readonly int CompressedSize = Crc32 + FieldLengths.Crc32;
            public static readonly int UncompressedSize = CompressedSize + FieldLengths.CompressedSize;
            public static readonly int FilenameLength = UncompressedSize + FieldLengths.UncompressedSize;
            public static readonly int ExtraFieldLength = FilenameLength + FieldLengths.FilenameLength;
            public static readonly int DynamicData = ExtraFieldLength + FieldLengths.ExtraFieldLength;
        }

        internal readonly partial struct ZipDataDescriptor
        {
            internal static class FieldLocations
            {
                public const int Signature = 0;
                public static readonly int Crc32 = Signature + FieldLengths.Signature;
                public static readonly int CompressedSize = Crc32 + FieldLengths.Crc32;
                public static readonly int UncompressedSize = CompressedSize + FieldLengths.CompressedSize;
            }
        }

        internal readonly partial struct Zip64DataDescriptor
        {
            internal static class FieldLocations
            {
                public const int Signature = 0;
                public static readonly int Crc32 = Signature + FieldLengths.Signature;
                public static readonly int CompressedSize = Crc32 + FieldLengths.Crc32;
                public static readonly int UncompressedSize = CompressedSize + FieldLengths.CompressedSize;
            }
        }
    }

    internal partial struct ZipCentralDirectoryFileHeader
    {
        internal static class FieldLocations
        {
            public const int Signature = 0;
            public static readonly int VersionMadeBySpecification = Signature + FieldLengths.Signature;
            public static readonly int VersionMadeByCompatibility = VersionMadeBySpecification + FieldLengths.VersionMadeBySpecification;
            public static readonly int VersionNeededToExtract = VersionMadeByCompatibility + FieldLengths.VersionMadeByCompatibility;
            public static readonly int GeneralPurposeBitFlags = VersionNeededToExtract + FieldLengths.VersionNeededToExtract;
            public static readonly int CompressionMethod = GeneralPurposeBitFlags + FieldLengths.GeneralPurposeBitFlags;
            public static readonly int LastModified = CompressionMethod + FieldLengths.CompressionMethod;
            public static readonly int Crc32 = LastModified + FieldLengths.LastModified;
            public static readonly int CompressedSize = Crc32 + FieldLengths.Crc32;
            public static readonly int UncompressedSize = CompressedSize + FieldLengths.CompressedSize;
            public static readonly int FilenameLength = UncompressedSize + FieldLengths.UncompressedSize;
            public static readonly int ExtraFieldLength = FilenameLength + FieldLengths.FilenameLength;
            public static readonly int FileCommentLength = ExtraFieldLength + FieldLengths.ExtraFieldLength;
            public static readonly int DiskNumberStart = FileCommentLength + FieldLengths.FileCommentLength;
            public static readonly int InternalFileAttributes = DiskNumberStart + FieldLengths.DiskNumberStart;
            public static readonly int ExternalFileAttributes = InternalFileAttributes + FieldLengths.InternalFileAttributes;
            public static readonly int RelativeOffsetOfLocalHeader = ExternalFileAttributes + FieldLengths.ExternalFileAttributes;
            public static readonly int DynamicData = RelativeOffsetOfLocalHeader + FieldLengths.RelativeOffsetOfLocalHeader;
        }
    }

    internal partial struct ZipEndOfCentralDirectoryBlock
    {
        private static class FieldLocations
        {
            public const int Signature = 0;
            public static readonly int NumberOfThisDisk = Signature + FieldLengths.Signature;
            public static readonly int NumberOfTheDiskWithTheStartOfTheCentralDirectory = NumberOfThisDisk + FieldLengths.NumberOfThisDisk;
            public static readonly int NumberOfEntriesInTheCentralDirectoryOnThisDisk = NumberOfTheDiskWithTheStartOfTheCentralDirectory + FieldLengths.NumberOfTheDiskWithTheStartOfTheCentralDirectory;
            public static readonly int NumberOfEntriesInTheCentralDirectory = NumberOfEntriesInTheCentralDirectoryOnThisDisk + FieldLengths.NumberOfEntriesInTheCentralDirectoryOnThisDisk;
            public static readonly int SizeOfCentralDirectory = NumberOfEntriesInTheCentralDirectory + FieldLengths.NumberOfEntriesInTheCentralDirectory;
            public static readonly int OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = SizeOfCentralDirectory + FieldLengths.SizeOfCentralDirectory;
            public static readonly int ArchiveCommentLength = OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber + FieldLengths.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
            public static readonly int DynamicData = ArchiveCommentLength + FieldLengths.ArchiveCommentLength;
        }
    }
}
