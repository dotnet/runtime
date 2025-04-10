// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    // All blocks.TryReadBlock do a check to see if signature is correct. Generic extra field is slightly different
    // all of the TryReadBlocks will throw if there are not enough bytes in the stream
    internal sealed partial class ZipGenericExtraField
    {
        private const int SizeOfHeader = FieldLengths.Tag + FieldLengths.Size;

        private ushort _tag;
        private ushort _size;
        private byte[]? _data;

        public ushort Tag => _tag;
        // returns size of data, not of the entire block
        public ushort Size => _size;
        public byte[] Data => _data ??= [];

        // assumes that bytes starts at the beginning of an extra field subfield
        public static bool TryReadBlock(ReadOnlySpan<byte> bytes, out int bytesConsumed, out ZipGenericExtraField field)
        {
            field = new();
            bytesConsumed = 0;

            // not enough bytes to read tag + size
            if (bytes.Length < SizeOfHeader)
            {
                return false;
            }

            field._tag = BinaryPrimitives.ReadUInt16LittleEndian(bytes[FieldLocations.Tag..]);
            field._size = BinaryPrimitives.ReadUInt16LittleEndian(bytes[FieldLocations.Size..]);
            bytesConsumed += SizeOfHeader;

            // not enough byte to read the data
            if ((bytes.Length - SizeOfHeader) < field._size)
            {
                return false;
            }

            field._data = bytes.Slice(FieldLocations.DynamicData, field._size).ToArray();
            bytesConsumed += field._size;
            return true;
        }

        public static List<ZipGenericExtraField> ParseExtraField(ReadOnlySpan<byte> extraFieldData)
        {
            List<ZipGenericExtraField> extraFields = new List<ZipGenericExtraField>();
            int totalBytesConsumed = 0;

            while (TryReadBlock(extraFieldData[totalBytesConsumed..], out int currBytesConsumed, out ZipGenericExtraField field))
            {
                totalBytesConsumed += currBytesConsumed;
                extraFields.Add(field);
            }

            return extraFields;
        }

        public static int TotalSize(List<ZipGenericExtraField> fields)
        {
            int size = 0;
            foreach (ZipGenericExtraField field in fields)
            {
                size += field.Size + SizeOfHeader; //size is only size of data
            }
            return size;
        }

        private void WriteBlockCore(Span<byte> extraFieldHeader)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldHeader[FieldLocations.Tag..], _tag);
            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldHeader[FieldLocations.Size..], _size);
        }
    }

    internal sealed partial class Zip64ExtraField
    {
        // Size is size of the record not including the tag or size fields
        // If the extra field is going in the local header, it cannot include only
        // one of uncompressed/compressed size

        public const int OffsetToFirstField = ZipGenericExtraField.FieldLocations.DynamicData;
        private const ushort TagConstant = 1;

        private ushort _size;
        private long? _uncompressedSize;
        private long? _compressedSize;
        private long? _localHeaderOffset;
        private uint? _startDiskNumber;

        public ushort TotalSize => (ushort)(_size + 4);

        public long? UncompressedSize
        {
            get { return _uncompressedSize; }
            set { _uncompressedSize = value; UpdateSize(); }
        }
        public long? CompressedSize
        {
            get { return _compressedSize; }
            set { _compressedSize = value; UpdateSize(); }
        }
        public long? LocalHeaderOffset
        {
            get { return _localHeaderOffset; }
            set { _localHeaderOffset = value; UpdateSize(); }
        }
        public uint? StartDiskNumber => _startDiskNumber;

        private void UpdateSize()
        {
            _size = 0;
            if (_uncompressedSize != null)
            {
                _size += FieldLengths.UncompressedSize;
            }
            if (_compressedSize != null)
            {
                _size += FieldLengths.CompressedSize;
            }
            if (_localHeaderOffset != null)
            {
                _size += FieldLengths.LocalHeaderOffset;
            }
            if (_startDiskNumber != null)
            {
                _size += FieldLengths.StartDiskNumber;
            }
        }

        // There is a small chance that something very weird could happen here. The code calling into this function
        // will ask for a value from the extra field if the field was masked with FF's. It's theoretically possible
        // that a field was FF's legitimately, and the writer didn't decide to write the corresponding extra field.
        // Also, at the same time, other fields were masked with FF's to indicate looking in the zip64 record.
        // Then, the search for the zip64 record will fail because the expected size is wrong,
        // and a nulled out Zip64ExtraField will be returned. Thus, even though there was Zip64 data,
        // it will not be used. It is questionable whether this situation is possible to detect
        // unlike the other functions that have try-pattern semantics, these functions always return a
        // Zip64ExtraField. If a Zip64 extra field actually doesn't exist, all of the fields in the
        // returned struct will be null
        //
        // If there are more than one Zip64 extra fields, we take the first one that has the expected size
        //
        public static Zip64ExtraField GetJustZip64Block(ReadOnlySpan<byte> extraFieldData,
            bool readUncompressedSize, bool readCompressedSize,
            bool readLocalHeaderOffset, bool readStartDiskNumber)
        {
            Zip64ExtraField zip64Field;
            int totalBytesConsumed = 0;

            while (ZipGenericExtraField.TryReadBlock(extraFieldData.Slice(totalBytesConsumed), out int currBytesConsumed, out ZipGenericExtraField currentExtraField))
            {
                totalBytesConsumed += currBytesConsumed;

                if (TryGetZip64BlockFromGenericExtraField(currentExtraField, readUncompressedSize,
                    readCompressedSize, readLocalHeaderOffset, readStartDiskNumber, out zip64Field))
                {
                    return zip64Field;
                }
            }

            zip64Field = new()
            {
                _compressedSize = null,
                _uncompressedSize = null,
                _localHeaderOffset = null,
                _startDiskNumber = null,
            };

            return zip64Field;
        }

        private static bool TryGetZip64BlockFromGenericExtraField(ZipGenericExtraField extraField,
            bool readUncompressedSize, bool readCompressedSize,
            bool readLocalHeaderOffset, bool readStartDiskNumber,
            out Zip64ExtraField zip64Block)
        {
            const int MaximumExtraFieldLength = FieldLengths.UncompressedSize + FieldLengths.CompressedSize + FieldLengths.LocalHeaderOffset + FieldLengths.StartDiskNumber;
            zip64Block = new()
            {
                _compressedSize = null,
                _uncompressedSize = null,
                _localHeaderOffset = null,
                _startDiskNumber = null,
            };

            if (extraField.Tag != TagConstant)
            {
                return false;
            }

            zip64Block._size = extraField.Size;

            ReadOnlySpan<byte> data = extraField.Data;

            // The spec section 4.5.3:
            //      The order of the fields in the zip64 extended
            //      information record is fixed, but the fields MUST
            //      only appear if the corresponding Local or Central
            //      directory record field is set to 0xFFFF or 0xFFFFFFFF.
            // However tools commonly write the fields anyway; the prevailing convention
            // is to respect the size, but only actually use the values if their 32 bit
            // values were all 0xFF.

            if (data.Length < FieldLengths.UncompressedSize)
            {
                return true;
            }

            // Advancing the stream (by reading from it) is possible only when:
            // 1. There is an explicit ask to do that (valid files, corresponding boolean flag(s) set to true).
            // 2. When the size indicates that all the information is available ("slightly invalid files").
            bool readAllFields = extraField.Size >= MaximumExtraFieldLength;

            if (readUncompressedSize)
            {
                zip64Block._uncompressedSize = BinaryPrimitives.ReadInt64LittleEndian(data);
                data = data.Slice(FieldLengths.UncompressedSize);
            }
            else if (readAllFields)
            {
                data = data.Slice(FieldLengths.UncompressedSize);
            }

            if (data.Length < FieldLengths.CompressedSize)
            {
                return true;
            }

            if (readCompressedSize)
            {
                zip64Block._compressedSize = BinaryPrimitives.ReadInt64LittleEndian(data);
                data = data.Slice(FieldLengths.CompressedSize);
            }
            else if (readAllFields)
            {
                data = data.Slice(FieldLengths.CompressedSize);
            }

            if (data.Length < FieldLengths.LocalHeaderOffset)
            {
                return true;
            }

            if (readLocalHeaderOffset)
            {
                zip64Block._localHeaderOffset = BinaryPrimitives.ReadInt64LittleEndian(data);
                data = data.Slice(FieldLengths.LocalHeaderOffset);
            }
            else if (readAllFields)
            {
                data = data.Slice(FieldLengths.LocalHeaderOffset);
            }

            if (data.Length < FieldLengths.StartDiskNumber)
            {
                return true;
            }

            if (readStartDiskNumber)
            {
                zip64Block._startDiskNumber = BinaryPrimitives.ReadUInt32LittleEndian(data);
            }

            // original values are unsigned, so implies value is too big to fit in signed integer
            if (zip64Block._uncompressedSize < 0)
            {
                throw new InvalidDataException(SR.FieldTooBigUncompressedSize);
            }
            if (zip64Block._compressedSize < 0)
            {
                throw new InvalidDataException(SR.FieldTooBigCompressedSize);
            }
            if (zip64Block._localHeaderOffset < 0)
            {
                throw new InvalidDataException(SR.FieldTooBigLocalHeaderOffset);
            }

            return true;
        }

        public static Zip64ExtraField GetAndRemoveZip64Block(List<ZipGenericExtraField> extraFields,
            bool readUncompressedSize, bool readCompressedSize,
            bool readLocalHeaderOffset, bool readStartDiskNumber)
        {
            Zip64ExtraField zip64Field = new()
            {
                _compressedSize = null,
                _uncompressedSize = null,
                _localHeaderOffset = null,
                _startDiskNumber = null,
            };

            bool zip64FieldFound = false;

            extraFields.RemoveAll(ef =>
            {
                if (ef.Tag == TagConstant)
                {
                    if (!zip64FieldFound)
                    {
                        if (TryGetZip64BlockFromGenericExtraField(ef, readUncompressedSize, readCompressedSize,
                                    readLocalHeaderOffset, readStartDiskNumber, out zip64Field))
                        {
                            zip64FieldFound = true;
                        }
                    }
                    return true;
                }

                return false;
            });

            return zip64Field;
        }

        public static void RemoveZip64Blocks(List<ZipGenericExtraField> extraFields)
        {
            extraFields.RemoveAll(field => field.Tag == TagConstant);
        }

        private void WriteBlockCore(Span<byte> extraFieldData)
        {
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
            }
        }
    }

    internal sealed partial class Zip64EndOfCentralDirectoryLocator
    {
        // The Zip File Format Specification references 0x07064B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static readonly byte[] SignatureConstantBytes = [0x50, 0x4B, 0x06, 0x07];

        public static readonly int TotalSize = FieldLocations.TotalNumberOfDisks + FieldLengths.TotalNumberOfDisks;
        public static readonly int SizeOfBlockWithoutSignature = TotalSize - FieldLengths.Signature;

        public uint NumberOfDiskWithZip64EOCD;
        public ulong OffsetOfZip64EOCD;
        public uint TotalNumberOfDisks;

        private static bool TryReadBlockCore(Span<byte> blockContents, int bytesRead, out Zip64EndOfCentralDirectoryLocator zip64EOCDLocator)
        {
            zip64EOCDLocator = new();
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

        private static void WriteBlockCore(Span<byte> blockContents, long zip64EOCDRecordStart)
        {
            SignatureConstantBytes.CopyTo(blockContents[FieldLocations.Signature..]);
            // number of disk with start of zip64 eocd
            BinaryPrimitives.WriteUInt32LittleEndian(blockContents[FieldLocations.NumberOfDiskWithZip64EOCD..], 0);
            BinaryPrimitives.WriteInt64LittleEndian(blockContents[FieldLocations.OffsetOfZip64EOCD..], zip64EOCDRecordStart);
            // total number of disks
            BinaryPrimitives.WriteUInt32LittleEndian(blockContents[FieldLocations.TotalNumberOfDisks..], 1);
        }
    }

    internal sealed partial class Zip64EndOfCentralDirectoryRecord
    {
        // The Zip File Format Specification references 0x06064B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x06, 0x06];

        private const int BlockConstantSectionSize = 56;
        private const ulong NormalSize = 0x2C; // the size of the data excluding the size/signature fields if no extra data included
        public const long TotalSize = (long)NormalSize + 12;    // total size of the entire block

        public ulong SizeOfThisRecord;
        public ushort VersionMadeBy;
        public ushort VersionNeededToExtract;
        public uint NumberOfThisDisk;
        public uint NumberOfDiskWithStartOfCD;
        public ulong NumberOfEntriesOnThisDisk;
        public ulong NumberOfEntriesTotal;
        public ulong SizeOfCentralDirectory;
        public ulong OffsetOfCentralDirectory;

        private static bool TryReadBlockCore(Span<byte> blockContents, int bytesRead, out Zip64EndOfCentralDirectoryRecord zip64EOCDRecord)
        {
            zip64EOCDRecord = new();
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

        private static void WriteBlockCore(Span<byte> blockContents, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory)
        {
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
        }
    }

    internal readonly partial struct ZipLocalFileHeader
    {
        // The Zip File Format Specification references 0x08074B50 and 0x04034B50, these are big endian representations.
        // ZIP files store values in little endian, so these are reversed.
        public static ReadOnlySpan<byte> DataDescriptorSignatureConstantBytes => [0x50, 0x4B, 0x07, 0x08];
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x03, 0x04];
        public const int SizeOfLocalHeader = 30;
    }

    internal sealed partial class ZipCentralDirectoryFileHeader
    {
        // The Zip File Format Specification references 0x02014B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x01, 0x02];

        // These are the minimum possible size, assuming the zip file comments variable section is empty
        public const int BlockConstantSectionSize = 46;

        public byte VersionMadeByCompatibility;
        public byte VersionMadeBySpecification;
        public ushort VersionNeededToExtract;
        public ushort GeneralPurposeBitFlag;
        public ushort CompressionMethod;
        public uint LastModified; // convert this on the fly
        public uint Crc32;
        public long CompressedSize;
        public long UncompressedSize;
        public ushort FilenameLength;
        public ushort ExtraFieldLength;
        public ushort FileCommentLength;
        public uint DiskNumberStart;
        public ushort InternalFileAttributes;
        public uint ExternalFileAttributes;
        public long RelativeOffsetOfLocalHeader;

        public byte[] Filename = [];
        public byte[] FileComment = [];
        public List<ZipGenericExtraField>? ExtraFields;
    }

    internal sealed partial class ZipEndOfCentralDirectoryBlock
    {
        // The Zip File Format Specification references 0x06054B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static readonly byte[] SignatureConstantBytes = [0x50, 0x4B, 0x05, 0x06];

        // This also assumes a zero-length comment.
        public static readonly int TotalSize = FieldLocations.ArchiveCommentLength + FieldLengths.ArchiveCommentLength;
        // These are the minimum possible size, assuming the zip file comments variable section is empty
        public static readonly int SizeOfBlockWithoutSignature = TotalSize - FieldLengths.Signature;

        // The end of central directory can have a variable size zip file comment at the end, but its max length can be 64K
        // The Zip File Format Specification does not explicitly mention a max size for this field, but we are assuming this
        // max size because that is the maximum value an ushort can hold.
        public const int ZipFileCommentMaxLength = ushort.MaxValue;

        public uint Signature;
        public ushort NumberOfThisDisk;
        public ushort NumberOfTheDiskWithTheStartOfTheCentralDirectory;
        public ushort NumberOfEntriesInTheCentralDirectoryOnThisDisk;
        public ushort NumberOfEntriesInTheCentralDirectory;
        public uint SizeOfCentralDirectory;
        public uint OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
        private byte[]? _archiveComment;
        public byte[] ArchiveComment => _archiveComment ??= [];
    }
}
