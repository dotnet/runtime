// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.IO.Compression
{
    // All blocks.TryReadBlock do a check to see if signature is correct. Generic extra field is slightly different
    // all of the TryReadBlocks will throw if there are not enough bytes in the stream

    internal struct ZipGenericExtraField
    {
        private const int SizeOfHeader = 4;

        private ushort _tag;
        private ushort _size;
        private byte[] _data;

        public ushort Tag => _tag;
        // returns size of data, not of the entire block
        public ushort Size => _size;
        public byte[] Data => _data;

        public void WriteBlock(Stream stream)
        {
            Span<byte> extraFieldHeader = stackalloc byte[SizeOfHeader];

            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldHeader, _tag);
            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldHeader.Slice(sizeof(ushort)), _size);

            stream.Write(extraFieldHeader);
            stream.Write(Data);
        }

        // shouldn't ever read the byte at position endExtraField
        // assumes we are positioned at the beginning of an extra field subfield
        public static bool TryReadBlock(BinaryReader reader, long endExtraField, out ZipGenericExtraField field)
        {
            field = default;

            // not enough bytes to read tag + size
            if (endExtraField - reader.BaseStream.Position < 4)
                return false;

            field._tag = reader.ReadUInt16();
            field._size = reader.ReadUInt16();

            // not enough bytes to read the data
            if (endExtraField - reader.BaseStream.Position < field._size)
                return false;

            field._data = reader.ReadBytes(field._size);
            return true;
        }

        // assumes that bytes starts at the beginning of an extra field subfield
        public static bool TryReadBlock(ReadOnlySpan<byte> bytes, out int bytesConsumed, out ZipGenericExtraField field)
        {
            field = default;
            bytesConsumed = 0;

            // not enough bytes to read tag + size
            if (bytes.Length < sizeof(ushort) + sizeof(ushort))
            {
                return false;
            }

            field._tag = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
            bytesConsumed += sizeof(ushort);

            field._size = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(bytesConsumed));
            bytesConsumed += sizeof(ushort);

            // not enough byte to read the data
            if ((bytes.Length - sizeof(ushort) - sizeof(ushort)) < field._size)
            {
                return false;
            }

            field._data = bytes.Slice(bytesConsumed, field._size).ToArray();
            bytesConsumed += field._size;
            return true;
        }

        public static List<ZipGenericExtraField> ParseExtraField(ReadOnlySpan<byte> extraFieldData)
        {
            List<ZipGenericExtraField> extraFields = new List<ZipGenericExtraField>();
            int totalBytesConsumed = 0;

            while (TryReadBlock(extraFieldData.Slice(totalBytesConsumed), out int currBytesConsumed, out ZipGenericExtraField field))
            {
                totalBytesConsumed += currBytesConsumed;
                extraFields.Add(field);
            }

            return extraFields;
        }

        // shouldn't ever read the byte at position endExtraField
        public static List<ZipGenericExtraField> ParseExtraField(Stream extraFieldData)
        {
            List<ZipGenericExtraField> extraFields = new List<ZipGenericExtraField>();

            using (BinaryReader reader = new BinaryReader(extraFieldData))
            {
                ZipGenericExtraField field;
                while (TryReadBlock(reader, extraFieldData.Length, out field))
                {
                    extraFields.Add(field);
                }
            }

            return extraFields;
        }

        public static int TotalSize(List<ZipGenericExtraField> fields)
        {
            int size = 0;
            foreach (ZipGenericExtraField field in fields)
                size += field.Size + SizeOfHeader; //size is only size of data
            return size;
        }

        public static void WriteAllBlocks(List<ZipGenericExtraField> fields, Stream stream)
        {
            foreach (ZipGenericExtraField field in fields)
                field.WriteBlock(stream);
        }
    }

    internal struct Zip64ExtraField
    {
        // Size is size of the record not including the tag or size fields
        // If the extra field is going in the local header, it cannot include only
        // one of uncompressed/compressed size

        public const int OffsetToFirstField = 4;
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
            if (_uncompressedSize != null) _size += 8;
            if (_compressedSize != null) _size += 8;
            if (_localHeaderOffset != null) _size += 8;
            if (_startDiskNumber != null) _size += 4;
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

            zip64Field = default;

            zip64Field._compressedSize = null;
            zip64Field._uncompressedSize = null;
            zip64Field._localHeaderOffset = null;
            zip64Field._startDiskNumber = null;

            return zip64Field;
        }

        private static bool TryGetZip64BlockFromGenericExtraField(ZipGenericExtraField extraField,
            bool readUncompressedSize, bool readCompressedSize,
            bool readLocalHeaderOffset, bool readStartDiskNumber,
            out Zip64ExtraField zip64Block)
        {
            zip64Block = default;

            zip64Block._compressedSize = null;
            zip64Block._uncompressedSize = null;
            zip64Block._localHeaderOffset = null;
            zip64Block._startDiskNumber = null;

            if (extraField.Tag != TagConstant)
                return false;

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

            if (data.Length < sizeof(long))
                return true;

            // Advancing the stream (by reading from it) is possible only when:
            // 1. There is an explicit ask to do that (valid files, corresponding boolean flag(s) set to true).
            // 2. When the size indicates that all the information is available ("slightly invalid files").
            bool readAllFields = extraField.Size >= sizeof(long) + sizeof(long) + sizeof(long) + sizeof(int);

            if (readUncompressedSize)
            {
                zip64Block._uncompressedSize = BinaryPrimitives.ReadInt64LittleEndian(data);
                data = data.Slice(sizeof(long));
            }
            else if (readAllFields)
            {
                data = data.Slice(sizeof(long));
            }

            if (data.Length < sizeof(long))
                return true;

            if (readCompressedSize)
            {
                zip64Block._compressedSize = BinaryPrimitives.ReadInt64LittleEndian(data);
                data = data.Slice(sizeof(long));
            }
            else if (readAllFields)
            {
                data = data.Slice(sizeof(long));
            }

            if (data.Length < sizeof(long))
                return true;

            if (readLocalHeaderOffset)
            {
                zip64Block._localHeaderOffset = BinaryPrimitives.ReadInt64LittleEndian(data);
                data = data.Slice(sizeof(long));
            }
            else if (readAllFields)
            {
                data = data.Slice(sizeof(long));
            }

            if (data.Length < sizeof(int))
                return true;

            if (readStartDiskNumber)
            {
                zip64Block._startDiskNumber = BinaryPrimitives.ReadUInt32LittleEndian(data);
            }

            // original values are unsigned, so implies value is too big to fit in signed integer
            if (zip64Block._uncompressedSize < 0) throw new InvalidDataException(SR.FieldTooBigUncompressedSize);
            if (zip64Block._compressedSize < 0) throw new InvalidDataException(SR.FieldTooBigCompressedSize);
            if (zip64Block._localHeaderOffset < 0) throw new InvalidDataException(SR.FieldTooBigLocalHeaderOffset);

            return true;

        }

        public static Zip64ExtraField GetAndRemoveZip64Block(List<ZipGenericExtraField> extraFields,
            bool readUncompressedSize, bool readCompressedSize,
            bool readLocalHeaderOffset, bool readStartDiskNumber)
        {
            Zip64ExtraField zip64Field = default;

            zip64Field._compressedSize = null;
            zip64Field._uncompressedSize = null;
            zip64Field._localHeaderOffset = null;
            zip64Field._startDiskNumber = null;

            List<ZipGenericExtraField> markedForDelete = new List<ZipGenericExtraField>();
            bool zip64FieldFound = false;

            foreach (ZipGenericExtraField ef in extraFields)
            {
                if (ef.Tag == TagConstant)
                {
                    markedForDelete.Add(ef);
                    if (!zip64FieldFound)
                    {
                        if (TryGetZip64BlockFromGenericExtraField(ef, readUncompressedSize, readCompressedSize,
                                    readLocalHeaderOffset, readStartDiskNumber, out zip64Field))
                        {
                            zip64FieldFound = true;
                        }
                    }
                }
            }

            foreach (ZipGenericExtraField ef in markedForDelete)
                extraFields.Remove(ef);

            return zip64Field;
        }

        public static void RemoveZip64Blocks(List<ZipGenericExtraField> extraFields)
        {
            List<ZipGenericExtraField> markedForDelete = new List<ZipGenericExtraField>();
            foreach (ZipGenericExtraField field in extraFields)
                if (field.Tag == TagConstant)
                    markedForDelete.Add(field);

            foreach (ZipGenericExtraField field in markedForDelete)
                extraFields.Remove(field);
        }

        public void WriteBlock(Stream stream)
        {
            Span<byte> extraFieldData = stackalloc byte[TotalSize];
            int offset = 0;

            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldData, TagConstant);
            offset += sizeof(ushort);

            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldData.Slice(offset), _size);
            offset += sizeof(ushort);

            if (_uncompressedSize != null)
            {
                BinaryPrimitives.WriteInt64LittleEndian(extraFieldData.Slice(offset), _uncompressedSize.Value);
                offset += sizeof(long);
            }

            if (_compressedSize != null)
            {
                BinaryPrimitives.WriteInt64LittleEndian(extraFieldData.Slice(offset), _compressedSize.Value);
                offset += sizeof(long);
            }

            if (_localHeaderOffset != null)
            {
                BinaryPrimitives.WriteInt64LittleEndian(extraFieldData.Slice(offset), _localHeaderOffset.Value);
                offset += sizeof(long);
            }

            if (_startDiskNumber != null)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(extraFieldData.Slice(offset), _startDiskNumber.Value);
                offset += sizeof(long);
            }

            stream.Write(extraFieldData);
        }
    }

    internal struct Zip64EndOfCentralDirectoryLocator
    {
        public const uint SignatureConstant = 0x07064B50;
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x06, 0x07];
        public const int SignatureSize = sizeof(uint);

        public const int BlockConstantSectionSize = 20;
        public const int SizeOfBlockWithoutSignature = 16;

        public uint NumberOfDiskWithZip64EOCD;
        public ulong OffsetOfZip64EOCD;
        public uint TotalNumberOfDisks;

        public static bool TryReadBlock(Stream stream, out Zip64EndOfCentralDirectoryLocator zip64EOCDLocator)
        {
            Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
            int bytesRead;

            zip64EOCDLocator = default;
            bytesRead = stream.Read(blockContents);

            if (bytesRead < BlockConstantSectionSize)
                return false;

            if (!blockContents.StartsWith(SignatureConstantBytes))
                return false;

            blockContents = blockContents.Slice(SignatureConstantBytes.Length);

            zip64EOCDLocator.NumberOfDiskWithZip64EOCD = BinaryPrimitives.ReadUInt32LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(uint));

            zip64EOCDLocator.OffsetOfZip64EOCD = BinaryPrimitives.ReadUInt64LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ulong));

            zip64EOCDLocator.TotalNumberOfDisks = BinaryPrimitives.ReadUInt32LittleEndian(blockContents);
            return true;
        }

        public static void WriteBlock(Stream stream, long zip64EOCDRecordStart)
        {
            Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
            Span<byte> writeSegment = blockContents;

            SignatureConstantBytes.CopyTo(writeSegment);
            writeSegment = writeSegment.Slice(SignatureConstantBytes.Length);

            // number of disk with start of zip64 eocd
            BinaryPrimitives.WriteUInt32LittleEndian(writeSegment, 0);
            writeSegment = writeSegment.Slice(sizeof(uint));

            BinaryPrimitives.WriteInt64LittleEndian(writeSegment, zip64EOCDRecordStart);
            writeSegment = writeSegment.Slice(sizeof(long));

            // total number of disks
            BinaryPrimitives.WriteUInt32LittleEndian(writeSegment, 1);

            stream.Write(blockContents);
        }
    }

    internal struct Zip64EndOfCentralDirectoryRecord
    {
        // The Zip File Format Specification references 0x06054B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x06, 0x06];

        public const int BlockConstantSectionSize = 56;
        private const ulong NormalSize = 0x2C; // the size of the data excluding the size/signature fields if no extra data included

        public ulong SizeOfThisRecord;
        public ushort VersionMadeBy;
        public ushort VersionNeededToExtract;
        public uint NumberOfThisDisk;
        public uint NumberOfDiskWithStartOfCD;
        public ulong NumberOfEntriesOnThisDisk;
        public ulong NumberOfEntriesTotal;
        public ulong SizeOfCentralDirectory;
        public ulong OffsetOfCentralDirectory;

        public static bool TryReadBlock(Stream stream, out Zip64EndOfCentralDirectoryRecord zip64EOCDRecord)
        {
            Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
            int bytesRead;

            zip64EOCDRecord = default;
            bytesRead = stream.Read(blockContents);

            if (bytesRead < BlockConstantSectionSize)
                return false;

            if (!blockContents.StartsWith(SignatureConstantBytes))
                return false;

            blockContents = blockContents.Slice(SignatureConstantBytes.Length);

            zip64EOCDRecord.SizeOfThisRecord = BinaryPrimitives.ReadUInt64LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ulong));

            zip64EOCDRecord.VersionMadeBy = BinaryPrimitives.ReadUInt16LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ushort));

            zip64EOCDRecord.VersionNeededToExtract = BinaryPrimitives.ReadUInt16LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ushort));

            zip64EOCDRecord.NumberOfThisDisk = BinaryPrimitives.ReadUInt32LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(uint));

            zip64EOCDRecord.NumberOfDiskWithStartOfCD = BinaryPrimitives.ReadUInt32LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(uint));

            zip64EOCDRecord.NumberOfEntriesOnThisDisk = BinaryPrimitives.ReadUInt64LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ulong));

            zip64EOCDRecord.NumberOfEntriesTotal = BinaryPrimitives.ReadUInt64LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ulong));

            zip64EOCDRecord.SizeOfCentralDirectory = BinaryPrimitives.ReadUInt64LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ulong));

            zip64EOCDRecord.OffsetOfCentralDirectory = BinaryPrimitives.ReadUInt64LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ulong));

            return true;
        }

        public static void WriteBlock(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory)
        {
            Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
            Span<byte> writeSegment = blockContents;

            SignatureConstantBytes.CopyTo(blockContents);
            writeSegment = writeSegment.Slice(SignatureConstantBytes.Length);

            BinaryPrimitives.WriteUInt64LittleEndian(writeSegment, NormalSize);
            writeSegment = writeSegment.Slice(sizeof(ulong));

            // version needed is 45 for zip 64 support
            BinaryPrimitives.WriteUInt16LittleEndian(writeSegment, (ushort)ZipVersionNeededValues.Zip64);
            writeSegment = writeSegment.Slice(sizeof(ushort));

            // version made by: high byte is 0 for MS DOS, low byte is version needed
            BinaryPrimitives.WriteUInt16LittleEndian(writeSegment, (ushort)ZipVersionNeededValues.Zip64);
            writeSegment = writeSegment.Slice(sizeof(ushort));

            // number of this disk is 0
            BinaryPrimitives.WriteUInt32LittleEndian(writeSegment, 0);
            writeSegment = writeSegment.Slice(sizeof(uint));

            // number of disk with start of central directory is 0
            BinaryPrimitives.WriteUInt32LittleEndian(writeSegment, 0);
            writeSegment = writeSegment.Slice(sizeof(uint));

            // number of entries on this disk
            BinaryPrimitives.WriteInt64LittleEndian(writeSegment, numberOfEntries);
            writeSegment = writeSegment.Slice(sizeof(long));

            // number of entries total
            BinaryPrimitives.WriteInt64LittleEndian(writeSegment, numberOfEntries);
            writeSegment = writeSegment.Slice(sizeof(long));

            BinaryPrimitives.WriteInt64LittleEndian(writeSegment, sizeOfCentralDirectory);
            writeSegment = writeSegment.Slice(sizeof(long));

            BinaryPrimitives.WriteInt64LittleEndian(writeSegment, startOfCentralDirectory);

            // write Zip 64 EOCD record
            stream.Write(blockContents);
        }
    }

    internal readonly struct ZipLocalFileHeader
    {
        public const uint DataDescriptorSignature = 0x08074B50;
        public const uint SignatureConstant = 0x04034B50;
        public const int OffsetToCrcFromHeaderStart = 14;
        public const int OffsetToVersionFromHeaderStart = 4;
        public const int OffsetToBitFlagFromHeaderStart = 6;
        public const int SizeOfLocalHeader = 30;

        public static List<ZipGenericExtraField> GetExtraFields(BinaryReader reader)
        {
            // assumes that TrySkipBlock has already been called, so we don't have to validate twice

            List<ZipGenericExtraField> result;

            const int OffsetToFilenameLength = 26; // from the point before the signature

            reader.BaseStream.Seek(OffsetToFilenameLength, SeekOrigin.Current);

            ushort filenameLength = reader.ReadUInt16();
            ushort extraFieldLength = reader.ReadUInt16();

            reader.BaseStream.Seek(filenameLength, SeekOrigin.Current);


            using (Stream str = new SubReadStream(reader.BaseStream, reader.BaseStream.Position, extraFieldLength))
            {
                result = ZipGenericExtraField.ParseExtraField(str);
            }
            Zip64ExtraField.RemoveZip64Blocks(result);

            return result;
        }

        // will not throw end of stream exception
        public static bool TrySkipBlock(BinaryReader reader)
        {
            const int OffsetToFilenameLength = 22; // from the point after the signature

            if (reader.ReadUInt32() != SignatureConstant)
                return false;

            if (reader.BaseStream.Length < reader.BaseStream.Position + OffsetToFilenameLength)
                return false;

            reader.BaseStream.Seek(OffsetToFilenameLength, SeekOrigin.Current);

            ushort filenameLength = reader.ReadUInt16();
            ushort extraFieldLength = reader.ReadUInt16();

            if (reader.BaseStream.Length < reader.BaseStream.Position + filenameLength + extraFieldLength)
                return false;

            reader.BaseStream.Seek(filenameLength + extraFieldLength, SeekOrigin.Current);

            return true;
        }
    }

    internal struct ZipCentralDirectoryFileHeader
    {
        // The Zip File Format Specification references 0x06054B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x01, 0x02];
        public const uint SignatureConstant = 0x02014B50;

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

        public byte[] Filename;
        public byte[] FileComment;
        public List<ZipGenericExtraField>? ExtraFields;

        // if saveExtraFieldsAndComments is false, FileComment and ExtraFields will be null
        // in either case, the zip64 extra field info will be incorporated into other fields
        public static bool TryReadBlock(ReadOnlySpan<byte> buffer, Stream furtherReads, bool saveExtraFieldsAndComments, out int bytesRead, out ZipCentralDirectoryFileHeader header)
        {
            header = default;
            bytesRead = 0;

            // the buffer will always be large enough for at least the constant section to be verified
            Debug.Assert(buffer.Length >= BlockConstantSectionSize);

            bytesRead += SignatureConstantBytes.Length;
            if (!buffer.StartsWith(SignatureConstantBytes))
            {
                return false;
            }

            header.VersionMadeBySpecification = buffer[bytesRead++];
            header.VersionMadeByCompatibility = buffer[bytesRead++];

            header.VersionNeededToExtract = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(ushort);

            header.GeneralPurposeBitFlag = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(ushort);

            header.CompressionMethod = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(ushort);

            header.LastModified = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(uint);

            header.Crc32 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(uint);

            uint compressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(uint);

            uint uncompressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(uint);

            header.FilenameLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(ushort);

            header.ExtraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(ushort);

            header.FileCommentLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(ushort);

            ushort diskNumberStartSmall = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(ushort);

            header.InternalFileAttributes = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(ushort);

            header.ExternalFileAttributes = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(uint);

            uint relativeOffsetOfLocalHeaderSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(bytesRead));
            bytesRead += sizeof(uint);

            // Assemble the dynamic header in a separate buffer. We can't guarantee that it's all in the input buffer,
            // some additional data might need to come from the stream.
            int dynamicHeaderSize = header.FilenameLength + header.ExtraFieldLength + header.FileCommentLength;
            int bytesToRead = dynamicHeaderSize - (buffer.Length - bytesRead);
            scoped ReadOnlySpan<byte> dynamicHeader;

            // No need to read extra data from the stream, no need to allocate a new buffer.
            if (bytesToRead <= 0)
            {
                dynamicHeader = buffer.Slice(bytesRead);
            }
            // Data needs to come from two sources, and we must thus copy data into a single address space.
            else
            {
                Span<byte> collatedHeader = dynamicHeaderSize <= 512 ? stackalloc byte[512].Slice(dynamicHeaderSize) : new byte[dynamicHeaderSize];

                buffer.Slice(bytesRead).CopyTo(collatedHeader);
                int realBytesRead = furtherReads.Read(collatedHeader.Slice(buffer.Length - bytesRead));

                bytesRead = buffer.Length + realBytesRead;
                if (realBytesRead != bytesToRead)
                {
                    return false;
                }
                dynamicHeader = collatedHeader;
            }

            header.Filename = dynamicHeader.Slice(0, header.FilenameLength).ToArray();

            bool uncompressedSizeInZip64 = uncompressedSizeSmall == ZipHelper.Mask32Bit;
            bool compressedSizeInZip64 = compressedSizeSmall == ZipHelper.Mask32Bit;
            bool relativeOffsetInZip64 = relativeOffsetOfLocalHeaderSmall == ZipHelper.Mask32Bit;
            bool diskNumberStartInZip64 = diskNumberStartSmall == ZipHelper.Mask16Bit;

            Zip64ExtraField zip64;
            ReadOnlySpan<byte> zipExtraFields = dynamicHeader.Slice(header.FilenameLength, header.ExtraFieldLength);

            zip64 = default;
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

            bytesRead += dynamicHeaderSize;

            header.UncompressedSize = zip64.UncompressedSize ?? uncompressedSizeSmall;
            header.CompressedSize = zip64.CompressedSize ?? compressedSizeSmall;
            header.RelativeOffsetOfLocalHeader = zip64.LocalHeaderOffset ?? relativeOffsetOfLocalHeaderSmall;
            header.DiskNumberStart = zip64.StartDiskNumber ?? diskNumberStartSmall;

            return true;
        }
    }

    internal struct ZipEndOfCentralDirectoryBlock
    {
        // The Zip File Format Specification references 0x06054B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x05, 0x06];

        // These are the minimum possible size, assuming the zip file comments variable section is empty
        public const int BlockConstantSectionSize = 22;
        public const int SizeOfBlockWithoutSignature = 18;

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
        public byte[] ArchiveComment;

        public static void WriteBlock(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory, byte[] archiveComment)
        {
            Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
            Span<byte> writeSegment = blockContents;

            ushort numberOfEntriesTruncated = numberOfEntries > ushort.MaxValue ?
                                                        ZipHelper.Mask16Bit : (ushort)numberOfEntries;
            uint startOfCentralDirectoryTruncated = startOfCentralDirectory > uint.MaxValue ?
                                                        ZipHelper.Mask32Bit : (uint)startOfCentralDirectory;
            uint sizeOfCentralDirectoryTruncated = sizeOfCentralDirectory > uint.MaxValue ?
                                                        ZipHelper.Mask32Bit : (uint)sizeOfCentralDirectory;

            SignatureConstantBytes.CopyTo(blockContents);
            writeSegment = writeSegment.Slice(SignatureConstantBytes.Length);

            // number of this disk
            BinaryPrimitives.WriteUInt16LittleEndian(writeSegment, 0);
            writeSegment = writeSegment.Slice(sizeof(ushort));

            // number of disk with start of CD
            BinaryPrimitives.WriteUInt16LittleEndian(writeSegment, 0);
            writeSegment = writeSegment.Slice(sizeof(ushort));

            // number of entries on this disk's cd
            BinaryPrimitives.WriteUInt16LittleEndian(writeSegment, numberOfEntriesTruncated);
            writeSegment = writeSegment.Slice(sizeof(ushort));

            // number of entries in entire cd
            BinaryPrimitives.WriteUInt16LittleEndian(writeSegment, numberOfEntriesTruncated);
            writeSegment = writeSegment.Slice(sizeof(ushort));

            BinaryPrimitives.WriteUInt32LittleEndian(writeSegment, sizeOfCentralDirectoryTruncated);
            writeSegment = writeSegment.Slice(sizeof(uint));

            BinaryPrimitives.WriteUInt32LittleEndian(writeSegment, startOfCentralDirectoryTruncated);
            writeSegment = writeSegment.Slice(sizeof(uint));

            // zip file comment length
            BinaryPrimitives.WriteUInt16LittleEndian(writeSegment, (ushort)archiveComment.Length);

            stream.Write(blockContents);
            if (archiveComment.Length > 0)
                stream.Write(archiveComment);
        }

        public static bool TryReadBlock(Stream stream, out ZipEndOfCentralDirectoryBlock eocdBlock)
        {
            Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
            int bytesRead;

            eocdBlock = default;
            bytesRead = stream.Read(blockContents);

            if (bytesRead < BlockConstantSectionSize)
                return false;

            if (!blockContents.StartsWith(SignatureConstantBytes))
                return false;

            eocdBlock.Signature = BinaryPrimitives.ReadUInt32LittleEndian(blockContents);
            blockContents = blockContents.Slice(SignatureConstantBytes.Length);

            eocdBlock.NumberOfThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ushort));

            eocdBlock.NumberOfTheDiskWithTheStartOfTheCentralDirectory = BinaryPrimitives.ReadUInt16LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ushort));

            eocdBlock.NumberOfEntriesInTheCentralDirectoryOnThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ushort));

            eocdBlock.NumberOfEntriesInTheCentralDirectory = BinaryPrimitives.ReadUInt16LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(ushort));

            eocdBlock.SizeOfCentralDirectory = BinaryPrimitives.ReadUInt32LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(uint));

            eocdBlock.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = BinaryPrimitives.ReadUInt32LittleEndian(blockContents);
            blockContents = blockContents.Slice(sizeof(uint));

            ushort commentLength = BinaryPrimitives.ReadUInt16LittleEndian(blockContents);

            if (stream.Position + commentLength > stream.Length)
                return false;

            if (commentLength == 0)
            {
                eocdBlock.ArchiveComment = Array.Empty<byte>();
            }
            else
            {
                eocdBlock.ArchiveComment = new byte[commentLength];
                stream.ReadExactly(eocdBlock.ArchiveComment);
            }

            return true;
        }
    }
}
