// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Tag);
            writer.Write(Size);
            writer.Write(Data);
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
        private int? _startDiskNumber;

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
        public int? StartDiskNumber => _startDiskNumber;

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
        public static Zip64ExtraField GetJustZip64Block(Stream extraFieldStream,
            bool readUncompressedSize, bool readCompressedSize,
            bool readLocalHeaderOffset, bool readStartDiskNumber)
        {
            Zip64ExtraField zip64Field;
            using (BinaryReader reader = new BinaryReader(extraFieldStream))
            {
                ZipGenericExtraField currentExtraField;
                while (ZipGenericExtraField.TryReadBlock(reader, extraFieldStream.Length, out currentExtraField))
                {
                    if (TryGetZip64BlockFromGenericExtraField(currentExtraField, readUncompressedSize,
                                readCompressedSize, readLocalHeaderOffset, readStartDiskNumber, out zip64Field))
                    {
                        return zip64Field;
                    }
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

            using (MemoryStream ms = new MemoryStream(extraField.Data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                // The spec section 4.5.3:
                //      The order of the fields in the zip64 extended
                //      information record is fixed, but the fields MUST
                //      only appear if the corresponding Local or Central
                //      directory record field is set to 0xFFFF or 0xFFFFFFFF.
                // However tools commonly write the fields anyway; the prevailing convention
                // is to respect the size, but only actually use the values if their 32 bit
                // values were all 0xFF.

                if (extraField.Size < sizeof(long))
                    return true;

                long value64 = reader.ReadInt64();
                if (readUncompressedSize)
                    zip64Block._uncompressedSize = value64;

                if (ms.Position > extraField.Size - sizeof(long))
                    return true;

                value64 = reader.ReadInt64();
                if (readCompressedSize)
                    zip64Block._compressedSize = value64;

                if (ms.Position > extraField.Size - sizeof(long))
                    return true;

                value64 = reader.ReadInt64();
                if (readLocalHeaderOffset)
                    zip64Block._localHeaderOffset = value64;

                if (ms.Position > extraField.Size - sizeof(int))
                    return true;

                int value32 = reader.ReadInt32();
                if (readStartDiskNumber)
                    zip64Block._startDiskNumber = value32;

                // original values are unsigned, so implies value is too big to fit in signed integer
                if (zip64Block._uncompressedSize < 0) throw new InvalidDataException(SR.FieldTooBigUncompressedSize);
                if (zip64Block._compressedSize < 0) throw new InvalidDataException(SR.FieldTooBigCompressedSize);
                if (zip64Block._localHeaderOffset < 0) throw new InvalidDataException(SR.FieldTooBigLocalHeaderOffset);
                if (zip64Block._startDiskNumber < 0) throw new InvalidDataException(SR.FieldTooBigStartDiskNumber);

                return true;
            }
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
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(TagConstant);
            writer.Write(_size);
            if (_uncompressedSize != null) writer.Write(_uncompressedSize.Value);
            if (_compressedSize != null) writer.Write(_compressedSize.Value);
            if (_localHeaderOffset != null) writer.Write(_localHeaderOffset.Value);
            if (_startDiskNumber != null) writer.Write(_startDiskNumber.Value);
        }
    }

    internal struct Zip64EndOfCentralDirectoryLocator
    {
        public const uint SignatureConstant = 0x07064B50;
        public const int SignatureSize = sizeof(uint);

        public const int SizeOfBlockWithoutSignature = 16;

        public uint NumberOfDiskWithZip64EOCD;
        public ulong OffsetOfZip64EOCD;
        public uint TotalNumberOfDisks;

        public static bool TryReadBlock(BinaryReader reader, out Zip64EndOfCentralDirectoryLocator zip64EOCDLocator)
        {
            zip64EOCDLocator = default;

            if (reader.ReadUInt32() != SignatureConstant)
                return false;

            zip64EOCDLocator.NumberOfDiskWithZip64EOCD = reader.ReadUInt32();
            zip64EOCDLocator.OffsetOfZip64EOCD = reader.ReadUInt64();
            zip64EOCDLocator.TotalNumberOfDisks = reader.ReadUInt32();
            return true;
        }

        public static void WriteBlock(Stream stream, long zip64EOCDRecordStart)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(SignatureConstant);
            writer.Write((uint)0); // number of disk with start of zip64 eocd
            writer.Write(zip64EOCDRecordStart);
            writer.Write((uint)1); // total number of disks
        }
    }

    internal struct Zip64EndOfCentralDirectoryRecord
    {
        private const uint SignatureConstant = 0x06064B50;
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

        public static bool TryReadBlock(BinaryReader reader, out Zip64EndOfCentralDirectoryRecord zip64EOCDRecord)
        {
            zip64EOCDRecord = default;

            if (reader.ReadUInt32() != SignatureConstant)
                return false;

            zip64EOCDRecord.SizeOfThisRecord = reader.ReadUInt64();
            zip64EOCDRecord.VersionMadeBy = reader.ReadUInt16();
            zip64EOCDRecord.VersionNeededToExtract = reader.ReadUInt16();
            zip64EOCDRecord.NumberOfThisDisk = reader.ReadUInt32();
            zip64EOCDRecord.NumberOfDiskWithStartOfCD = reader.ReadUInt32();
            zip64EOCDRecord.NumberOfEntriesOnThisDisk = reader.ReadUInt64();
            zip64EOCDRecord.NumberOfEntriesTotal = reader.ReadUInt64();
            zip64EOCDRecord.SizeOfCentralDirectory = reader.ReadUInt64();
            zip64EOCDRecord.OffsetOfCentralDirectory = reader.ReadUInt64();

            return true;
        }

        public static void WriteBlock(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            // write Zip 64 EOCD record
            writer.Write(SignatureConstant);
            writer.Write(NormalSize);
            writer.Write((ushort)ZipVersionNeededValues.Zip64); // version needed is 45 for zip 64 support
            writer.Write((ushort)ZipVersionNeededValues.Zip64); // version made by: high byte is 0 for MS DOS, low byte is version needed
            writer.Write((uint)0); // number of this disk is 0
            writer.Write((uint)0); // number of disk with start of central directory is 0
            writer.Write(numberOfEntries); // number of entries on this disk
            writer.Write(numberOfEntries); // number of entries total
            writer.Write(sizeOfCentralDirectory);
            writer.Write(startOfCentralDirectory);
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
        public const uint SignatureConstant = 0x02014B50;
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
        public int DiskNumberStart;
        public ushort InternalFileAttributes;
        public uint ExternalFileAttributes;
        public long RelativeOffsetOfLocalHeader;

        public byte[] Filename;
        public byte[] FileComment;
        public List<ZipGenericExtraField>? ExtraFields;

        // if saveExtraFieldsAndComments is false, FileComment and ExtraFields will be null
        // in either case, the zip64 extra field info will be incorporated into other fields
        public static bool TryReadBlock(BinaryReader reader, bool saveExtraFieldsAndComments, out ZipCentralDirectoryFileHeader header)
        {
            header = default;

            if (reader.ReadUInt32() != SignatureConstant)
                return false;
            header.VersionMadeBySpecification = reader.ReadByte();
            header.VersionMadeByCompatibility = reader.ReadByte();
            header.VersionNeededToExtract = reader.ReadUInt16();
            header.GeneralPurposeBitFlag = reader.ReadUInt16();
            header.CompressionMethod = reader.ReadUInt16();
            header.LastModified = reader.ReadUInt32();
            header.Crc32 = reader.ReadUInt32();
            uint compressedSizeSmall = reader.ReadUInt32();
            uint uncompressedSizeSmall = reader.ReadUInt32();
            header.FilenameLength = reader.ReadUInt16();
            header.ExtraFieldLength = reader.ReadUInt16();
            header.FileCommentLength = reader.ReadUInt16();
            ushort diskNumberStartSmall = reader.ReadUInt16();
            header.InternalFileAttributes = reader.ReadUInt16();
            header.ExternalFileAttributes = reader.ReadUInt32();
            uint relativeOffsetOfLocalHeaderSmall = reader.ReadUInt32();

            header.Filename = reader.ReadBytes(header.FilenameLength);

            bool uncompressedSizeInZip64 = uncompressedSizeSmall == ZipHelper.Mask32Bit;
            bool compressedSizeInZip64 = compressedSizeSmall == ZipHelper.Mask32Bit;
            bool relativeOffsetInZip64 = relativeOffsetOfLocalHeaderSmall == ZipHelper.Mask32Bit;
            bool diskNumberStartInZip64 = diskNumberStartSmall == ZipHelper.Mask16Bit;

            Zip64ExtraField zip64;

            long endExtraFields = reader.BaseStream.Position + header.ExtraFieldLength;
            using (Stream str = new SubReadStream(reader.BaseStream, reader.BaseStream.Position, header.ExtraFieldLength))
            {
                if (saveExtraFieldsAndComments)
                {
                    header.ExtraFields = ZipGenericExtraField.ParseExtraField(str);
                    zip64 = Zip64ExtraField.GetAndRemoveZip64Block(header.ExtraFields,
                            uncompressedSizeInZip64, compressedSizeInZip64,
                            relativeOffsetInZip64, diskNumberStartInZip64);
                }
                else
                {
                    header.ExtraFields = null;
                    zip64 = Zip64ExtraField.GetJustZip64Block(str,
                            uncompressedSizeInZip64, compressedSizeInZip64,
                            relativeOffsetInZip64, diskNumberStartInZip64);
                }
            }

            // There are zip files that have malformed ExtraField blocks in which GetJustZip64Block() silently bails out without reading all the way to the end
            // of the ExtraField block. Thus we must force the stream's position to the proper place.
            reader.BaseStream.AdvanceToPosition(endExtraFields);

            header.FileComment = reader.ReadBytes(header.FileCommentLength);

            header.UncompressedSize = zip64.UncompressedSize == null
                                                    ? uncompressedSizeSmall
                                                    : zip64.UncompressedSize.Value;
            header.CompressedSize = zip64.CompressedSize == null
                                                    ? compressedSizeSmall
                                                    : zip64.CompressedSize.Value;
            header.RelativeOffsetOfLocalHeader = zip64.LocalHeaderOffset == null
                                                    ? relativeOffsetOfLocalHeaderSmall
                                                    : zip64.LocalHeaderOffset.Value;
            header.DiskNumberStart = zip64.StartDiskNumber == null
                                                    ? diskNumberStartSmall
                                                    : zip64.StartDiskNumber.Value;

            return true;
        }
    }

    internal struct ZipEndOfCentralDirectoryBlock
    {
        public const uint SignatureConstant = 0x06054B50;
        public const int SignatureSize = sizeof(uint);

        // This is the minimum possible size, assuming the zip file comments variable section is empty
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
            BinaryWriter writer = new BinaryWriter(stream);

            ushort numberOfEntriesTruncated = numberOfEntries > ushort.MaxValue ?
                                                        ZipHelper.Mask16Bit : (ushort)numberOfEntries;
            uint startOfCentralDirectoryTruncated = startOfCentralDirectory > uint.MaxValue ?
                                                        ZipHelper.Mask32Bit : (uint)startOfCentralDirectory;
            uint sizeOfCentralDirectoryTruncated = sizeOfCentralDirectory > uint.MaxValue ?
                                                        ZipHelper.Mask32Bit : (uint)sizeOfCentralDirectory;

            writer.Write(SignatureConstant);
            writer.Write((ushort)0); // number of this disk
            writer.Write((ushort)0); // number of disk with start of CD
            writer.Write(numberOfEntriesTruncated); // number of entries on this disk's cd
            writer.Write(numberOfEntriesTruncated); // number of entries in entire CD
            writer.Write(sizeOfCentralDirectoryTruncated);
            writer.Write(startOfCentralDirectoryTruncated);

            // Should be valid because of how we read archiveComment in TryReadBlock:
            Debug.Assert(archiveComment.Length <= ZipFileCommentMaxLength);

            writer.Write((ushort)archiveComment.Length); // zip file comment length
            if (archiveComment.Length > 0)
                writer.Write(archiveComment);
        }

        public static bool TryReadBlock(BinaryReader reader, out ZipEndOfCentralDirectoryBlock eocdBlock)
        {
            eocdBlock = default;
            if (reader.ReadUInt32() != SignatureConstant)
                return false;

            eocdBlock.Signature = SignatureConstant;
            eocdBlock.NumberOfThisDisk = reader.ReadUInt16();
            eocdBlock.NumberOfTheDiskWithTheStartOfTheCentralDirectory = reader.ReadUInt16();
            eocdBlock.NumberOfEntriesInTheCentralDirectoryOnThisDisk = reader.ReadUInt16();
            eocdBlock.NumberOfEntriesInTheCentralDirectory = reader.ReadUInt16();
            eocdBlock.SizeOfCentralDirectory = reader.ReadUInt32();
            eocdBlock.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = reader.ReadUInt32();

            ushort commentLength = reader.ReadUInt16();
            eocdBlock.ArchiveComment = reader.ReadBytes(commentLength);

            return true;
        }
    }
}
