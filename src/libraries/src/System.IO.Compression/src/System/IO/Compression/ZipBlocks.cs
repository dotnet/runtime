// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.IO.Compression
{
    //All blocks.TryReadBlock do a check to see if signature is correct. Generic extra field is slightly different
    //all of the TryReadBlocks will throw if there are not enough bytes in the stream

    internal struct ZipGenericExtraField
    {
        private const Int32 SizeOfHeader = 4;

        private UInt16 _tag;
        private UInt16 _size;
        private Byte[] _data;

        public UInt16 Tag { get { return _tag; } }
        //returns size of data, not of the entire block
        public UInt16 Size { get { return _size; } }
        public Byte[] Data { get { return _data; } }

        public void WriteBlock(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Tag);
            writer.Write(Size);
            writer.Write(Data);
        }

        //shouldn't ever read the byte at position endExtraField
        //assumes we are positioned at the beginning of an extra field subfield
        public static Boolean TryReadBlock(BinaryReader reader, Int64 endExtraField, out ZipGenericExtraField field)
        {
            field = new ZipGenericExtraField();

            //not enough bytes to read tag + size
            if (endExtraField - reader.BaseStream.Position < 4)
                return false;

            field._tag = reader.ReadUInt16();
            field._size = reader.ReadUInt16();

            //not enough bytes to read the data
            if (endExtraField - reader.BaseStream.Position < field._size)
                return false;

            field._data = reader.ReadBytes(field._size);
            return true;
        }

        //shouldn't ever read the byte at position endExtraField
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

        public static Int32 TotalSize(List<ZipGenericExtraField> fields)
        {
            Int32 size = 0;
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
        /* Size is size of the record not including the tag or size fields
         * If the extra field is going in the local header, it cannot include only
         * one of uncompressed/compressed size
         * */

        public const Int32 OffsetToFirstField = 4;
        private const UInt16 TagConstant = 1;

        private UInt16 _size;
        private Int64? _uncompressedSize;
        private Int64? _compressedSize;
        private Int64? _localHeaderOffset;
        private Int32? _startDiskNumber;

        public UInt16 TotalSize
        {
            get { return (UInt16)(_size + 4); }
        }

        public Int64? UncompressedSize
        {
            get { return _uncompressedSize; }
            set { _uncompressedSize = value; UpdateSize(); }
        }
        public Int64? CompressedSize
        {
            get { return _compressedSize; }
            set { _compressedSize = value; UpdateSize(); }
        }
        public Int64? LocalHeaderOffset
        {
            get { return _localHeaderOffset; }
            set { _localHeaderOffset = value; UpdateSize(); }
        }
        public Int32? StartDiskNumber
        {
            get { return _startDiskNumber; }
        }

        private void UpdateSize()
        {
            _size = 0;
            if (_uncompressedSize != null) _size += 8;
            if (_compressedSize != null) _size += 8;
            if (_localHeaderOffset != null) _size += 8;
            if (_startDiskNumber != null) _size += 4;
        }

        /* There is a small chance that something very weird could happen here. The code calling into this function
         * will ask for a value from the extra field if the field was masked with FF's. It's theoretically possible
         * that a field was FF's legitimately, and the writer didn't decide to write the corresponding extra field.
         * Also, at the same time, other fields were masked with FF's to indicate looking in the zip64 record.
         * Then, the search for the zip64 record will fail because the expected size is wrong,
         * and a nulled out Zip64ExtraField will be returned. Thus, even though there was Zip64 data,
         * it will not be used. It is questionable whether this situation is possible to detect
         */

        /* unlike the other functions that have try-pattern semantics, these functions always return a
         * Zip64ExtraField. If a Zip64 extra field actually doesn't exist, all of the fields in the 
         * returned struct will be null
         * 
         * If there are more than one Zip64 extra fields, we take the first one that has the expected size
         */
        public static Zip64ExtraField GetJustZip64Block(Stream extraFieldStream,
            Boolean readUncompressedSize, Boolean readCompressedSize,
            Boolean readLocalHeaderOffset, Boolean readStartDiskNumber)
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

            zip64Field = new Zip64ExtraField();

            zip64Field._compressedSize = null;
            zip64Field._uncompressedSize = null;
            zip64Field._localHeaderOffset = null;
            zip64Field._startDiskNumber = null;

            return zip64Field;
        }

        private static Boolean TryGetZip64BlockFromGenericExtraField(ZipGenericExtraField extraField,
            Boolean readUncompressedSize, Boolean readCompressedSize,
            Boolean readLocalHeaderOffset, Boolean readStartDiskNumber,
            out Zip64ExtraField zip64Block)
        {
            zip64Block = new Zip64ExtraField();

            zip64Block._compressedSize = null;
            zip64Block._uncompressedSize = null;
            zip64Block._localHeaderOffset = null;
            zip64Block._startDiskNumber = null;

            if (extraField.Tag != TagConstant)
                return false;

            //this pattern needed because nested using blocks trigger CA2202
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(extraField.Data);
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    ms = null;

                    zip64Block._size = extraField.Size;

                    UInt16 expectedSize = 0;

                    if (readUncompressedSize) expectedSize += 8;
                    if (readCompressedSize) expectedSize += 8;
                    if (readLocalHeaderOffset) expectedSize += 8;
                    if (readStartDiskNumber) expectedSize += 4;

                    //if it is not the expected size, perhaps there is another extra field that matches
                    if (expectedSize != zip64Block._size)
                        return false;

                    if (readUncompressedSize) zip64Block._uncompressedSize = reader.ReadInt64();
                    if (readCompressedSize) zip64Block._compressedSize = reader.ReadInt64();
                    if (readLocalHeaderOffset) zip64Block._localHeaderOffset = reader.ReadInt64();
                    if (readStartDiskNumber) zip64Block._startDiskNumber = reader.ReadInt32();

                    //original values are unsigned, so implies value is too big to fit in signed integer
                    if (zip64Block._uncompressedSize < 0) throw new InvalidDataException(SR.FieldTooBigUncompressedSize);
                    if (zip64Block._compressedSize < 0) throw new InvalidDataException(SR.FieldTooBigCompressedSize);
                    if (zip64Block._localHeaderOffset < 0) throw new InvalidDataException(SR.FieldTooBigLocalHeaderOffset);
                    if (zip64Block._startDiskNumber < 0) throw new InvalidDataException(SR.FieldTooBigStartDiskNumber);

                    return true;
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
        }

        public static Zip64ExtraField GetAndRemoveZip64Block(List<ZipGenericExtraField> extraFields,
            Boolean readUncompressedSize, Boolean readCompressedSize,
            Boolean readLocalHeaderOffset, Boolean readStartDiskNumber)
        {
            Zip64ExtraField zip64Field = new Zip64ExtraField();

            zip64Field._compressedSize = null;
            zip64Field._uncompressedSize = null;
            zip64Field._localHeaderOffset = null;
            zip64Field._startDiskNumber = null;

            List<ZipGenericExtraField> markedForDelete = new List<ZipGenericExtraField>();
            Boolean zip64FieldFound = false;

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
        public const UInt32 SignatureConstant = 0x07064B50;
        public const Int32 SizeOfBlockWithoutSignature = 16;

        public UInt32 NumberOfDiskWithZip64EOCD;
        public UInt64 OffsetOfZip64EOCD;
        public UInt32 TotalNumberOfDisks;

        public static Boolean TryReadBlock(BinaryReader reader, out Zip64EndOfCentralDirectoryLocator zip64EOCDLocator)
        {
            zip64EOCDLocator = new Zip64EndOfCentralDirectoryLocator();

            if (reader.ReadUInt32() != SignatureConstant)
                return false;

            zip64EOCDLocator.NumberOfDiskWithZip64EOCD = reader.ReadUInt32();
            zip64EOCDLocator.OffsetOfZip64EOCD = reader.ReadUInt64();
            zip64EOCDLocator.TotalNumberOfDisks = reader.ReadUInt32();
            return true;
        }

        public static void WriteBlock(Stream stream, Int64 zip64EOCDRecordStart)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(SignatureConstant);
            writer.Write((UInt32)0);    //number of disk with start of zip64 eocd
            writer.Write(zip64EOCDRecordStart);
            writer.Write((UInt32)1);    //total number of disks
        }
    }

    internal struct Zip64EndOfCentralDirectoryRecord
    {
        private const UInt32 SignatureConstant = 0x06064B50;
        private const UInt64 NormalSize = 0x2C; //the size of the data excluding the size/signature fields if no extra data included

        public UInt64 SizeOfThisRecord;
        public UInt16 VersionMadeBy;
        public UInt16 VersionNeededToExtract;
        public UInt32 NumberOfThisDisk;
        public UInt32 NumberOfDiskWithStartOfCD;
        public UInt64 NumberOfEntriesOnThisDisk;
        public UInt64 NumberOfEntriesTotal;
        public UInt64 SizeOfCentralDirectory;
        public UInt64 OffsetOfCentralDirectory;

        public static Boolean TryReadBlock(BinaryReader reader, out Zip64EndOfCentralDirectoryRecord zip64EOCDRecord)
        {
            zip64EOCDRecord = new Zip64EndOfCentralDirectoryRecord();

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

        public static void WriteBlock(Stream stream, Int64 numberOfEntries, Int64 startOfCentralDirectory, Int64 sizeOfCentralDirectory)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            //write Zip 64 EOCD record
            writer.Write(SignatureConstant);
            writer.Write(NormalSize);
            writer.Write((UInt16)ZipVersionNeededValues.Zip64);   //version needed is 45 for zip 64 support
            writer.Write((UInt16)ZipVersionNeededValues.Zip64);   //version made by: high byte is 0 for MS DOS, low byte is version needed
            writer.Write((UInt32)0);    //number of this disk is 0
            writer.Write((UInt32)0);    //number of disk with start of central directory is 0
            writer.Write(numberOfEntries); //number of entries on this disk
            writer.Write(numberOfEntries); //number of entries total
            writer.Write(sizeOfCentralDirectory);
            writer.Write(startOfCentralDirectory);
        }
    }

    internal struct ZipLocalFileHeader
    {
        public const UInt32 DataDescriptorSignature = 0x08074B50;
        public const UInt32 SignatureConstant = 0x04034B50;
        public const Int32 OffsetToCrcFromHeaderStart = 14;
        public const Int32 OffsetToBitFlagFromHeaderStart = 6;
        public const Int32 SizeOfLocalHeader = 30;

        static public List<ZipGenericExtraField> GetExtraFields(BinaryReader reader)
        {
            //assumes that TrySkipBlock has already been called, so we don't have to validate twice

            List<ZipGenericExtraField> result;

            const Int32 OffsetToFilenameLength = 26; //from the point before the signature

            reader.BaseStream.Seek(OffsetToFilenameLength, SeekOrigin.Current);

            UInt16 filenameLength = reader.ReadUInt16();
            UInt16 extraFieldLength = reader.ReadUInt16();

            reader.BaseStream.Seek(filenameLength, SeekOrigin.Current);


            using (Stream str = new SubReadStream(reader.BaseStream, reader.BaseStream.Position, extraFieldLength))
            {
                result = ZipGenericExtraField.ParseExtraField(str);
            }
            Zip64ExtraField.RemoveZip64Blocks(result);

            return result;
        }

        //will not throw end of stream exception
        static public Boolean TrySkipBlock(BinaryReader reader)
        {
            const Int32 OffsetToFilenameLength = 22; //from the point after the signature

            if (reader.ReadUInt32() != SignatureConstant)
                return false;


            if (reader.BaseStream.Length < reader.BaseStream.Position + OffsetToFilenameLength)
                return false;

            reader.BaseStream.Seek(OffsetToFilenameLength, SeekOrigin.Current);

            UInt16 filenameLength = reader.ReadUInt16();
            UInt16 extraFieldLength = reader.ReadUInt16();

            if (reader.BaseStream.Length < reader.BaseStream.Position + filenameLength + extraFieldLength)
                return false;

            reader.BaseStream.Seek(filenameLength + extraFieldLength, SeekOrigin.Current);

            return true;
        }
    }

    internal struct ZipCentralDirectoryFileHeader
    {
        public const UInt32 SignatureConstant = 0x02014B50;
        public UInt16 VersionMadeBy;
        public UInt16 VersionNeededToExtract;
        public UInt16 GeneralPurposeBitFlag;
        public UInt16 CompressionMethod;
        public UInt32 LastModified; //convert this on the fly
        public UInt32 Crc32;
        public Int64 CompressedSize;
        public Int64 UncompressedSize;
        public UInt16 FilenameLength;
        public UInt16 ExtraFieldLength;
        public UInt16 FileCommentLength;
        public Int32 DiskNumberStart;
        public UInt16 InternalFileAttributes;
        public UInt32 ExternalFileAttributes;
        public Int64 RelativeOffsetOfLocalHeader;

        public Byte[] Filename;
        public Byte[] FileComment;
        public List<ZipGenericExtraField> ExtraFields;

        //if saveExtraFieldsAndComments is false, FileComment and ExtraFields will be null
        //in either case, the zip64 extra field info will be incorporated into other fields
        static public Boolean TryReadBlock(BinaryReader reader, Boolean saveExtraFieldsAndComments, out ZipCentralDirectoryFileHeader header)
        {
            header = new ZipCentralDirectoryFileHeader();

            if (reader.ReadUInt32() != SignatureConstant)
                return false;

            header.VersionMadeBy = reader.ReadUInt16();
            header.VersionNeededToExtract = reader.ReadUInt16();
            header.GeneralPurposeBitFlag = reader.ReadUInt16();
            header.CompressionMethod = reader.ReadUInt16();
            header.LastModified = reader.ReadUInt32();
            header.Crc32 = reader.ReadUInt32();
            UInt32 compressedSizeSmall = reader.ReadUInt32();
            UInt32 uncompressedSizeSmall = reader.ReadUInt32();
            header.FilenameLength = reader.ReadUInt16();
            header.ExtraFieldLength = reader.ReadUInt16();
            header.FileCommentLength = reader.ReadUInt16();
            UInt16 diskNumberStartSmall = reader.ReadUInt16();
            header.InternalFileAttributes = reader.ReadUInt16();
            header.ExternalFileAttributes = reader.ReadUInt32();
            UInt32 relativeOffsetOfLocalHeaderSmall = reader.ReadUInt32();

            header.Filename = reader.ReadBytes(header.FilenameLength);

            Boolean uncompressedSizeInZip64 = uncompressedSizeSmall == ZipHelper.Mask32Bit;
            Boolean compressedSizeInZip64 = compressedSizeSmall == ZipHelper.Mask32Bit;
            Boolean relativeOffsetInZip64 = relativeOffsetOfLocalHeaderSmall == ZipHelper.Mask32Bit;
            Boolean diskNumberStartInZip64 = diskNumberStartSmall == ZipHelper.Mask16Bit;

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

            if (saveExtraFieldsAndComments)
                header.FileComment = reader.ReadBytes(header.FileCommentLength);
            else
            {
                reader.BaseStream.Position += header.FileCommentLength;
                header.FileComment = null;
            }

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
        public const UInt32 SignatureConstant = 0x06054B50;
        public const Int32 SizeOfBlockWithoutSignature = 18;
        public UInt32 Signature;
        public UInt16 NumberOfThisDisk;
        public UInt16 NumberOfTheDiskWithTheStartOfTheCentralDirectory;
        public UInt16 NumberOfEntriesInTheCentralDirectoryOnThisDisk;
        public UInt16 NumberOfEntriesInTheCentralDirectory;
        public UInt32 SizeOfCentralDirectory;
        public UInt32 OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
        public Byte[] ArchiveComment;

        public static void WriteBlock(Stream stream, Int64 numberOfEntries, Int64 startOfCentralDirectory, Int64 sizeOfCentralDirectory, Byte[] archiveComment)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            UInt16 numberOfEntriesTruncated = numberOfEntries > UInt16.MaxValue ?
                                                        ZipHelper.Mask16Bit : (UInt16)numberOfEntries;
            UInt32 startOfCentralDirectoryTruncated = startOfCentralDirectory > UInt32.MaxValue ?
                                                        ZipHelper.Mask32Bit : (UInt32)startOfCentralDirectory;
            UInt32 sizeOfCentralDirectoryTruncated = sizeOfCentralDirectory > UInt32.MaxValue ?
                                                        ZipHelper.Mask32Bit : (UInt32)sizeOfCentralDirectory;

            writer.Write(SignatureConstant);
            writer.Write((UInt16)0);            //number of this disk
            writer.Write((UInt16)0);            //number of disk with start of CD
            writer.Write(numberOfEntriesTruncated); //number of entries on this disk's cd
            writer.Write(numberOfEntriesTruncated); //number of entries in entire CD
            writer.Write(sizeOfCentralDirectoryTruncated);
            writer.Write(startOfCentralDirectoryTruncated);

            //Should be valid because of how we read archiveComment in TryReadBlock:
            Debug.Assert((archiveComment == null) || (archiveComment.Length < UInt16.MaxValue));

            writer.Write(archiveComment != null ? (UInt16)archiveComment.Length : (UInt16)0);    //zip file comment length
            if (archiveComment != null)
                writer.Write(archiveComment);
        }

        public static Boolean TryReadBlock(BinaryReader reader, out ZipEndOfCentralDirectoryBlock eocdBlock)
        {
            eocdBlock = new ZipEndOfCentralDirectoryBlock();
            if (reader.ReadUInt32() != SignatureConstant)
                return false;

            eocdBlock.Signature = SignatureConstant;
            eocdBlock.NumberOfThisDisk = reader.ReadUInt16();
            eocdBlock.NumberOfTheDiskWithTheStartOfTheCentralDirectory = reader.ReadUInt16();
            eocdBlock.NumberOfEntriesInTheCentralDirectoryOnThisDisk = reader.ReadUInt16();
            eocdBlock.NumberOfEntriesInTheCentralDirectory = reader.ReadUInt16();
            eocdBlock.SizeOfCentralDirectory = reader.ReadUInt32();
            eocdBlock.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = reader.ReadUInt32();

            UInt16 commentLength = reader.ReadUInt16();
            eocdBlock.ArchiveComment = reader.ReadBytes(commentLength);

            return true;
        }
    }
}
