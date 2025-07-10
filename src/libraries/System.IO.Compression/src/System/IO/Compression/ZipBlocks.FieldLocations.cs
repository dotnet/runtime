// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal sealed partial class ZipGenericExtraField
    {
        internal static class FieldLocations
        {
            public const int Tag = 0;
            public const int Size = Tag + FieldLengths.Tag;
            public const int DynamicData = Size + FieldLengths.Size;
        }
    }

    internal sealed partial class Zip64ExtraField
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

    internal sealed partial class Zip64EndOfCentralDirectoryLocator
    {
        private static class FieldLocations
        {
            public const int Signature = 0;
            public const int NumberOfDiskWithZip64EOCD = Signature + FieldLengths.Signature;
            public const int OffsetOfZip64EOCD = NumberOfDiskWithZip64EOCD + FieldLengths.NumberOfDiskWithZip64EOCD;
            public const int TotalNumberOfDisks = OffsetOfZip64EOCD + FieldLengths.OffsetOfZip64EOCD;
        }
    }

    internal sealed partial class Zip64EndOfCentralDirectoryRecord
    {
        private static class FieldLocations
        {
            public const int Signature = 0;
            public const int SizeOfThisRecord = Signature + FieldLengths.Signature;
            public const int VersionMadeBy = SizeOfThisRecord + FieldLengths.SizeOfThisRecord;
            public const int VersionNeededToExtract = VersionMadeBy + FieldLengths.VersionMadeBy;
            public const int NumberOfThisDisk = VersionNeededToExtract + FieldLengths.VersionNeededToExtract;
            public const int NumberOfDiskWithStartOfCD = NumberOfThisDisk + FieldLengths.NumberOfThisDisk;
            public const int NumberOfEntriesOnThisDisk = NumberOfDiskWithStartOfCD + FieldLengths.NumberOfDiskWithStartOfCD;
            public const int NumberOfEntriesTotal = NumberOfEntriesOnThisDisk + FieldLengths.NumberOfEntriesOnThisDisk;
            public const int SizeOfCentralDirectory = NumberOfEntriesTotal + FieldLengths.NumberOfEntriesTotal;
            public const int OffsetOfCentralDirectory = SizeOfCentralDirectory + FieldLengths.SizeOfCentralDirectory;
        }
    }

    internal readonly partial struct ZipLocalFileHeader
    {
        internal static class FieldLocations
        {
            public const int Signature = 0;
            public const int VersionNeededToExtract = Signature + FieldLengths.Signature;
            public const int GeneralPurposeBitFlags = VersionNeededToExtract + FieldLengths.VersionNeededToExtract;
            public const int CompressionMethod = GeneralPurposeBitFlags + FieldLengths.GeneralPurposeBitFlags;
            public const int LastModified = CompressionMethod + FieldLengths.CompressionMethod;
            public const int Crc32 = LastModified + FieldLengths.LastModified;
            public const int CompressedSize = Crc32 + FieldLengths.Crc32;
            public const int UncompressedSize = CompressedSize + FieldLengths.CompressedSize;
            public const int FilenameLength = UncompressedSize + FieldLengths.UncompressedSize;
            public const int ExtraFieldLength = FilenameLength + FieldLengths.FilenameLength;
            public const int DynamicData = ExtraFieldLength + FieldLengths.ExtraFieldLength;
        }

        internal sealed partial class ZipDataDescriptor
        {
            internal static class FieldLocations
            {
                public const int Signature = 0;
                public const int Crc32 = Signature + FieldLengths.Signature;
                public const int CompressedSize = Crc32 + FieldLengths.Crc32;
                public const int UncompressedSize = CompressedSize + FieldLengths.CompressedSize;
            }
        }

        internal sealed partial class Zip64DataDescriptor
        {
            internal static class FieldLocations
            {
                public const int Signature = 0;
                public const int Crc32 = Signature + FieldLengths.Signature;
                public const int CompressedSize = Crc32 + FieldLengths.Crc32;
                public const int UncompressedSize = CompressedSize + FieldLengths.CompressedSize;
            }
        }
    }

    internal sealed partial class ZipCentralDirectoryFileHeader
    {
        internal static class FieldLocations
        {
            public const int Signature = 0;
            public const int VersionMadeBySpecification = Signature + FieldLengths.Signature;
            public const int VersionMadeByCompatibility = VersionMadeBySpecification + FieldLengths.VersionMadeBySpecification;
            public const int VersionNeededToExtract = VersionMadeByCompatibility + FieldLengths.VersionMadeByCompatibility;
            public const int GeneralPurposeBitFlags = VersionNeededToExtract + FieldLengths.VersionNeededToExtract;
            public const int CompressionMethod = GeneralPurposeBitFlags + FieldLengths.GeneralPurposeBitFlags;
            public const int LastModified = CompressionMethod + FieldLengths.CompressionMethod;
            public const int Crc32 = LastModified + FieldLengths.LastModified;
            public const int CompressedSize = Crc32 + FieldLengths.Crc32;
            public const int UncompressedSize = CompressedSize + FieldLengths.CompressedSize;
            public const int FilenameLength = UncompressedSize + FieldLengths.UncompressedSize;
            public const int ExtraFieldLength = FilenameLength + FieldLengths.FilenameLength;
            public const int FileCommentLength = ExtraFieldLength + FieldLengths.ExtraFieldLength;
            public const int DiskNumberStart = FileCommentLength + FieldLengths.FileCommentLength;
            public const int InternalFileAttributes = DiskNumberStart + FieldLengths.DiskNumberStart;
            public const int ExternalFileAttributes = InternalFileAttributes + FieldLengths.InternalFileAttributes;
            public const int RelativeOffsetOfLocalHeader = ExternalFileAttributes + FieldLengths.ExternalFileAttributes;
            public const int DynamicData = RelativeOffsetOfLocalHeader + FieldLengths.RelativeOffsetOfLocalHeader;
        }
    }

    internal sealed partial class ZipEndOfCentralDirectoryBlock
    {
        private static class FieldLocations
        {
            public const int Signature = 0;
            public const int NumberOfThisDisk = Signature + FieldLengths.Signature;
            public const int NumberOfTheDiskWithTheStartOfTheCentralDirectory = NumberOfThisDisk + FieldLengths.NumberOfThisDisk;
            public const int NumberOfEntriesInTheCentralDirectoryOnThisDisk = NumberOfTheDiskWithTheStartOfTheCentralDirectory + FieldLengths.NumberOfTheDiskWithTheStartOfTheCentralDirectory;
            public const int NumberOfEntriesInTheCentralDirectory = NumberOfEntriesInTheCentralDirectoryOnThisDisk + FieldLengths.NumberOfEntriesInTheCentralDirectoryOnThisDisk;
            public const int SizeOfCentralDirectory = NumberOfEntriesInTheCentralDirectory + FieldLengths.NumberOfEntriesInTheCentralDirectory;
            public const int OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = SizeOfCentralDirectory + FieldLengths.SizeOfCentralDirectory;
            public const int ArchiveCommentLength = OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber + FieldLengths.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
            public const int DynamicData = ArchiveCommentLength + FieldLengths.ArchiveCommentLength;
        }
    }
}
