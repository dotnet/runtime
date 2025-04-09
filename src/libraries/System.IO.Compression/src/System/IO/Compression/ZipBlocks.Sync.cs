// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.IO.Compression;

internal sealed partial class ZipGenericExtraField
{
    public void WriteBlock(Stream stream)
    {
        Span<byte> extraFieldHeader = stackalloc byte[SizeOfHeader];

        BinaryPrimitives.WriteUInt16LittleEndian(extraFieldHeader[FieldLocations.Tag..], _tag);
        BinaryPrimitives.WriteUInt16LittleEndian(extraFieldHeader[FieldLocations.Size..], _size);

        stream.Write(extraFieldHeader);
        stream.Write(Data);
    }

    public static void WriteAllBlocks(List<ZipGenericExtraField> fields, Stream stream)
    {
        foreach (ZipGenericExtraField field in fields)
        {
            field.WriteBlock(stream);
        }
    }
}

internal sealed partial class Zip64ExtraField
{
    public void WriteBlock(Stream stream)
    {
        Span<byte> extraFieldData = stackalloc byte[TotalSize];
        int startOffset = ZipGenericExtraField.FieldLocations.DynamicData;

        BinaryPrimitives.WriteUInt16LittleEndian(extraFieldData[FieldLocations.Tag..], TagConstant);
        BinaryPrimitives.WriteUInt16LittleEndian(extraFieldData[FieldLocations.Size..], _size);

        if (_uncompressedSize != null)
        {
            BinaryPrimitives.WriteInt64LittleEndian(extraFieldData[startOffset..], _uncompressedSize.Value);
            startOffset += FieldLengths.UncompressedSize;
        }

        if (_compressedSize != null)
        {
            BinaryPrimitives.WriteInt64LittleEndian(extraFieldData[startOffset..], _compressedSize.Value);
            startOffset += FieldLengths.CompressedSize;
        }

        if (_localHeaderOffset != null)
        {
            BinaryPrimitives.WriteInt64LittleEndian(extraFieldData[startOffset..], _localHeaderOffset.Value);
            startOffset += FieldLengths.LocalHeaderOffset;
        }

        if (_startDiskNumber != null)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(extraFieldData[startOffset..], _startDiskNumber.Value);
            startOffset += FieldLengths.StartDiskNumber;
        }

        stream.Write(extraFieldData);
    }
}

internal sealed partial class Zip64EndOfCentralDirectoryLocator
{
    public static bool TryReadBlock(Stream stream, out Zip64EndOfCentralDirectoryLocator zip64EOCDLocator)
    {
        Span<byte> blockContents = stackalloc byte[TotalSize];
        int bytesRead;

        zip64EOCDLocator = new();
        bytesRead = stream.Read(blockContents);

        if (bytesRead < TotalSize)
        {
            return false;
        }

        if (!blockContents.StartsWith(SignatureConstantBytes))
        {
            return false;
        }

        zip64EOCDLocator.NumberOfDiskWithZip64EOCD = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.NumberOfDiskWithZip64EOCD..]);
        zip64EOCDLocator.OffsetOfZip64EOCD = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.OffsetOfZip64EOCD..]);
        zip64EOCDLocator.TotalNumberOfDisks = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.TotalNumberOfDisks..]);

        return true;
    }

    public static void WriteBlock(Stream stream, long zip64EOCDRecordStart)
    {
        Span<byte> blockContents = stackalloc byte[TotalSize];

        SignatureConstantBytes.CopyTo(blockContents[FieldLocations.Signature..]);
        // number of disk with start of zip64 eocd
        BinaryPrimitives.WriteUInt32LittleEndian(blockContents[FieldLocations.NumberOfDiskWithZip64EOCD..], 0);
        BinaryPrimitives.WriteInt64LittleEndian(blockContents[FieldLocations.OffsetOfZip64EOCD..], zip64EOCDRecordStart);
        // total number of disks
        BinaryPrimitives.WriteUInt32LittleEndian(blockContents[FieldLocations.TotalNumberOfDisks..], 1);

        stream.Write(blockContents);
    }
}

internal sealed partial class Zip64EndOfCentralDirectoryRecord
{
    public static bool TryReadBlock(Stream stream, out Zip64EndOfCentralDirectoryRecord zip64EOCDRecord)
    {
        Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
        int bytesRead;

        zip64EOCDRecord = new();
        bytesRead = stream.Read(blockContents);

        if (bytesRead < BlockConstantSectionSize)
        {
            return false;
        }

        if (!blockContents.StartsWith(SignatureConstantBytes))
        {
            return false;
        }

        zip64EOCDRecord.SizeOfThisRecord = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.SizeOfThisRecord..]);
        zip64EOCDRecord.VersionMadeBy = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.VersionMadeBy..]);
        zip64EOCDRecord.VersionNeededToExtract = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.VersionNeededToExtract..]);
        zip64EOCDRecord.NumberOfThisDisk = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.NumberOfThisDisk..]);
        zip64EOCDRecord.NumberOfDiskWithStartOfCD = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.NumberOfDiskWithStartOfCD..]);
        zip64EOCDRecord.NumberOfEntriesOnThisDisk = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.NumberOfEntriesOnThisDisk..]);
        zip64EOCDRecord.NumberOfEntriesTotal = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.NumberOfEntriesTotal..]);
        zip64EOCDRecord.SizeOfCentralDirectory = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.SizeOfCentralDirectory..]);
        zip64EOCDRecord.OffsetOfCentralDirectory = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.OffsetOfCentralDirectory..]);

        return true;
    }

    public static void WriteBlock(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory)
    {
        Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];

        SignatureConstantBytes.CopyTo(blockContents[FieldLocations.Signature..]);
        BinaryPrimitives.WriteUInt64LittleEndian(blockContents[FieldLocations.SizeOfThisRecord..], NormalSize);
        // version made by: high byte is 0 for MS DOS, low byte is version needed
        BinaryPrimitives.WriteUInt16LittleEndian(blockContents[FieldLocations.VersionMadeBy..], (ushort)ZipVersionNeededValues.Zip64);
        // version needed is 45 for zip 64 support
        BinaryPrimitives.WriteUInt16LittleEndian(blockContents[FieldLocations.VersionNeededToExtract..], (ushort)ZipVersionNeededValues.Zip64);
        // number of this disk is 0
        BinaryPrimitives.WriteUInt32LittleEndian(blockContents[FieldLocations.NumberOfThisDisk..], 0);
        // number of disk with start of central directory is 0
        BinaryPrimitives.WriteUInt32LittleEndian(blockContents[FieldLocations.NumberOfDiskWithStartOfCD..], 0);
        // number of entries on this disk
        BinaryPrimitives.WriteInt64LittleEndian(blockContents[FieldLocations.NumberOfEntriesOnThisDisk..], numberOfEntries);
        // number of entries total
        BinaryPrimitives.WriteInt64LittleEndian(blockContents[FieldLocations.NumberOfEntriesTotal..], numberOfEntries);
        BinaryPrimitives.WriteInt64LittleEndian(blockContents[FieldLocations.SizeOfCentralDirectory..], sizeOfCentralDirectory);
        BinaryPrimitives.WriteInt64LittleEndian(blockContents[FieldLocations.OffsetOfCentralDirectory..], startOfCentralDirectory);

        // write Zip 64 EOCD record
        stream.Write(blockContents);
    }
}

internal readonly partial struct ZipLocalFileHeader
{
    public static List<ZipGenericExtraField> GetExtraFields(Stream stream)
    {
        // assumes that TrySkipBlock has already been called, so we don't have to validate twice

        const int StackAllocationThreshold = 512;

        List<ZipGenericExtraField> result;
        int relativeFilenameLengthLocation = FieldLocations.FilenameLength - FieldLocations.FilenameLength;
        int relativeExtraFieldLengthLocation = FieldLocations.ExtraFieldLength - FieldLocations.FilenameLength;
        Span<byte> fixedHeaderBuffer = stackalloc byte[FieldLengths.FilenameLength + FieldLengths.ExtraFieldLength];

        stream.Seek(FieldLocations.FilenameLength, SeekOrigin.Current);
        stream.ReadExactly(fixedHeaderBuffer);

        ushort filenameLength = BinaryPrimitives.ReadUInt16LittleEndian(fixedHeaderBuffer[relativeFilenameLengthLocation..]);
        ushort extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(fixedHeaderBuffer[relativeExtraFieldLengthLocation..]);
        byte[]? arrayPoolBuffer = extraFieldLength > StackAllocationThreshold ? System.Buffers.ArrayPool<byte>.Shared.Rent(extraFieldLength) : null;
        Span<byte> extraFieldBuffer = extraFieldLength <= StackAllocationThreshold ? stackalloc byte[StackAllocationThreshold].Slice(0, extraFieldLength) : arrayPoolBuffer.AsSpan(0, extraFieldLength);

        try
        {
            stream.Seek(filenameLength, SeekOrigin.Current);
            stream.ReadExactly(extraFieldBuffer);

            result = ZipGenericExtraField.ParseExtraField(extraFieldBuffer);
            Zip64ExtraField.RemoveZip64Blocks(result);

            return result;
        }
        finally
        {
            if (arrayPoolBuffer != null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
            }
        }
    }

    // will not throw end of stream exception
    public static bool TrySkipBlock(Stream stream)
    {
        Span<byte> blockBytes = stackalloc byte[4];
        long currPosition = stream.Position;
        int bytesRead = stream.Read(blockBytes);

        if (bytesRead != FieldLengths.Signature || !blockBytes.SequenceEqual(SignatureConstantBytes))
        {
            return false;
        }

        if (stream.Length < currPosition + FieldLocations.FilenameLength)
        {
            return false;
        }

        // Already read the signature, so make the filename length field location relative to that
        stream.Seek(FieldLocations.FilenameLength - FieldLengths.Signature, SeekOrigin.Current);

        bytesRead = stream.Read(blockBytes);
        if (bytesRead != FieldLengths.FilenameLength + FieldLengths.ExtraFieldLength)
        {
            return false;
        }

        int relativeFilenameLengthLocation = FieldLocations.FilenameLength - FieldLocations.FilenameLength;
        int relativeExtraFieldLengthLocation = FieldLocations.ExtraFieldLength - FieldLocations.FilenameLength;
        ushort filenameLength = BinaryPrimitives.ReadUInt16LittleEndian(blockBytes[relativeFilenameLengthLocation..]);
        ushort extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(blockBytes[relativeExtraFieldLengthLocation..]);

        if (stream.Length < stream.Position + filenameLength + extraFieldLength)
        {
            return false;
        }

        stream.Seek(filenameLength + extraFieldLength, SeekOrigin.Current);

        return true;
    }
}

internal sealed partial class ZipCentralDirectoryFileHeader
{
    // if saveExtraFieldsAndComments is false, FileComment and ExtraFields will be null
    // in either case, the zip64 extra field info will be incorporated into other fields
    public static bool TryReadBlock(ReadOnlySpan<byte> buffer, Stream furtherReads, bool saveExtraFieldsAndComments, out int bytesRead, [NotNullWhen(returnValue: true)] out ZipCentralDirectoryFileHeader? header)
    {
        header = null;

        const int StackAllocationThreshold = 512;

        bytesRead = 0;

        // the buffer will always be large enough for at least the constant section to be verified
        Debug.Assert(buffer.Length >= BlockConstantSectionSize);

        if (!buffer.StartsWith(SignatureConstantBytes))
        {
            return false;
        }

        header = new()
        {
            VersionMadeBySpecification = buffer[FieldLocations.VersionMadeBySpecification],
            VersionMadeByCompatibility = buffer[FieldLocations.VersionMadeByCompatibility],
            VersionNeededToExtract = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.VersionNeededToExtract..]),
            GeneralPurposeBitFlag = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.GeneralPurposeBitFlags..]),
            CompressionMethod = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.CompressionMethod..]),
            LastModified = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.LastModified..]),
            Crc32 = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.Crc32..]),
            FilenameLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.FilenameLength..]),
            ExtraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.ExtraFieldLength..]),
            FileCommentLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.FileCommentLength..]),
            InternalFileAttributes = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.InternalFileAttributes..]),
            ExternalFileAttributes = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.ExternalFileAttributes..])
        };

        uint compressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.CompressedSize..]);
        uint uncompressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.UncompressedSize..]);
        ushort diskNumberStartSmall = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.DiskNumberStart..]);
        uint relativeOffsetOfLocalHeaderSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.RelativeOffsetOfLocalHeader..]);

        // Assemble the dynamic header in a separate buffer. We can't guarantee that it's all in the input buffer,
        // some additional data might need to come from the stream.
        int dynamicHeaderSize = header.FilenameLength + header.ExtraFieldLength + header.FileCommentLength;
        int remainingBufferLength = buffer.Length - FieldLocations.DynamicData;
        int bytesToRead = dynamicHeaderSize - remainingBufferLength;
        scoped ReadOnlySpan<byte> dynamicHeader;
        byte[]? arrayPoolBuffer = null;

        Zip64ExtraField zip64;

        try
        {
            // No need to read extra data from the stream, no need to allocate a new buffer.
            if (bytesToRead <= 0)
            {
                dynamicHeader = buffer[FieldLocations.DynamicData..];
            }
            // Data needs to come from two sources, and we must thus copy data into a single address space.
            else
            {
                if (dynamicHeaderSize > StackAllocationThreshold)
                {
                    arrayPoolBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(dynamicHeaderSize);
                }

                Span<byte> collatedHeader = dynamicHeaderSize <= StackAllocationThreshold ? stackalloc byte[StackAllocationThreshold].Slice(0, dynamicHeaderSize) : arrayPoolBuffer.AsSpan(0, dynamicHeaderSize);

                buffer[FieldLocations.DynamicData..].CopyTo(collatedHeader);
                int realBytesRead = furtherReads.Read(collatedHeader[remainingBufferLength..]);

                if (realBytesRead != bytesToRead)
                {
                    return false;
                }
                dynamicHeader = collatedHeader;
            }

            header.Filename = dynamicHeader[..header.FilenameLength].ToArray();

            bool uncompressedSizeInZip64 = uncompressedSizeSmall == ZipHelper.Mask32Bit;
            bool compressedSizeInZip64 = compressedSizeSmall == ZipHelper.Mask32Bit;
            bool relativeOffsetInZip64 = relativeOffsetOfLocalHeaderSmall == ZipHelper.Mask32Bit;
            bool diskNumberStartInZip64 = diskNumberStartSmall == ZipHelper.Mask16Bit;

            ReadOnlySpan<byte> zipExtraFields = dynamicHeader.Slice(header.FilenameLength, header.ExtraFieldLength);

            zip64 = new();
            if (saveExtraFieldsAndComments)
            {
                header.ExtraFields = ZipGenericExtraField.ParseExtraField(zipExtraFields);
                zip64 = Zip64ExtraField.GetAndRemoveZip64Block(header.ExtraFields,
                            uncompressedSizeInZip64, compressedSizeInZip64,
                            relativeOffsetInZip64, diskNumberStartInZip64);
            }
            else
            {
                header.ExtraFields = null;
                zip64 = Zip64ExtraField.GetJustZip64Block(zipExtraFields,
                            uncompressedSizeInZip64, compressedSizeInZip64,
                            relativeOffsetInZip64, diskNumberStartInZip64);
            }

            header.FileComment = dynamicHeader.Slice(header.FilenameLength + header.ExtraFieldLength, header.FileCommentLength).ToArray();
        }
        finally
        {
            if (arrayPoolBuffer != null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
            }
        }

        bytesRead = FieldLocations.DynamicData + dynamicHeaderSize;

        header.UncompressedSize = zip64.UncompressedSize ?? uncompressedSizeSmall;
        header.CompressedSize = zip64.CompressedSize ?? compressedSizeSmall;
        header.RelativeOffsetOfLocalHeader = zip64.LocalHeaderOffset ?? relativeOffsetOfLocalHeaderSmall;
        header.DiskNumberStart = zip64.StartDiskNumber ?? diskNumberStartSmall;

        return true;
    }
}

internal sealed partial class ZipEndOfCentralDirectoryBlock
{
    public static void WriteBlock(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory, byte[] archiveComment)
    {
        Span<byte> blockContents = stackalloc byte[TotalSize];

        ushort numberOfEntriesTruncated = numberOfEntries > ushort.MaxValue ?
                                                    ZipHelper.Mask16Bit : (ushort)numberOfEntries;
        uint startOfCentralDirectoryTruncated = startOfCentralDirectory > uint.MaxValue ?
                                                    ZipHelper.Mask32Bit : (uint)startOfCentralDirectory;
        uint sizeOfCentralDirectoryTruncated = sizeOfCentralDirectory > uint.MaxValue ?
                                                    ZipHelper.Mask32Bit : (uint)sizeOfCentralDirectory;

        SignatureConstantBytes.CopyTo(blockContents[FieldLocations.Signature..]);
        // number of this disk
        BinaryPrimitives.WriteUInt16LittleEndian(blockContents[FieldLocations.NumberOfThisDisk..], 0);
        // number of disk with start of CD
        BinaryPrimitives.WriteUInt16LittleEndian(blockContents[FieldLocations.NumberOfTheDiskWithTheStartOfTheCentralDirectory..], 0);
        // number of entries on this disk's cd
        BinaryPrimitives.WriteUInt16LittleEndian(blockContents[FieldLocations.NumberOfEntriesInTheCentralDirectoryOnThisDisk..], numberOfEntriesTruncated);
        // number of entries in entire cd
        BinaryPrimitives.WriteUInt16LittleEndian(blockContents[FieldLocations.NumberOfEntriesInTheCentralDirectory..], numberOfEntriesTruncated);
        BinaryPrimitives.WriteUInt32LittleEndian(blockContents[FieldLocations.SizeOfCentralDirectory..], sizeOfCentralDirectoryTruncated);
        BinaryPrimitives.WriteUInt32LittleEndian(blockContents[FieldLocations.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber..], startOfCentralDirectoryTruncated);

        // Should be valid because of how we read archiveComment in TryReadBlock:
        Debug.Assert(archiveComment.Length <= ZipFileCommentMaxLength);

        // zip file comment length
        BinaryPrimitives.WriteUInt16LittleEndian(blockContents[FieldLocations.ArchiveCommentLength..], (ushort)archiveComment.Length);

        stream.Write(blockContents);
        if (archiveComment.Length > 0)
        {
            stream.Write(archiveComment);
        }
    }

    public static bool TryReadBlock(Stream stream, out ZipEndOfCentralDirectoryBlock eocdBlock)
    {
        Span<byte> blockContents = stackalloc byte[TotalSize];
        int bytesRead;

        eocdBlock = new();
        bytesRead = stream.Read(blockContents);

        if (bytesRead < TotalSize)
        {
            return false;
        }

        if (!blockContents.StartsWith(SignatureConstantBytes))
        {
            return false;
        }

        eocdBlock.Signature = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.Signature..]);
        eocdBlock.NumberOfThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.NumberOfThisDisk..]);
        eocdBlock.NumberOfTheDiskWithTheStartOfTheCentralDirectory = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.NumberOfTheDiskWithTheStartOfTheCentralDirectory..]);
        eocdBlock.NumberOfEntriesInTheCentralDirectoryOnThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.NumberOfEntriesInTheCentralDirectoryOnThisDisk..]);
        eocdBlock.NumberOfEntriesInTheCentralDirectory = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.NumberOfEntriesInTheCentralDirectory..]);
        eocdBlock.SizeOfCentralDirectory = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.SizeOfCentralDirectory..]);
        eocdBlock.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber =
            BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber..]);

        ushort commentLength = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.ArchiveCommentLength..]);

        if (stream.Position + commentLength > stream.Length)
        {
            return false;
        }

        if (commentLength == 0)
        {
            eocdBlock._archiveComment = [];
        }
        else
        {
            eocdBlock._archiveComment = new byte[commentLength];
            stream.ReadExactly(eocdBlock._archiveComment);
        }

        return true;
    }
}
