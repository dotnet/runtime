// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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

        public void WriteBlock(Stream stream)
        {
            Span<byte> extraFieldHeader = stackalloc byte[SizeOfHeader];
            WriteBlockCore(extraFieldHeader);
            stream.Write(extraFieldHeader);
            stream.Write(Data);
        }

        private void WriteBlockCore(Span<byte> extraFieldHeader)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldHeader[FieldLocations.Tag..], _tag);
            BinaryPrimitives.WriteUInt16LittleEndian(extraFieldHeader[FieldLocations.Size..], _size);
        }

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

            // not enough byte to read the data
            if ((bytes.Length - SizeOfHeader) < field._size)
            {
                return false;
            }

            field._data = bytes.Slice(FieldLocations.DynamicData, field._size).ToArray();
            bytesConsumed = field.Size + SizeOfHeader;
            return true;
        }

        public static List<ZipGenericExtraField> ParseExtraField(ReadOnlySpan<byte> extraFieldData, out ReadOnlySpan<byte> trailingExtraFieldData)
        {
            List<ZipGenericExtraField> extraFields = new List<ZipGenericExtraField>();
            int totalBytesConsumed = 0;

            while (TryReadBlock(extraFieldData[totalBytesConsumed..], out int currBytesConsumed, out ZipGenericExtraField field))
            {
                totalBytesConsumed += currBytesConsumed;
                extraFields.Add(field);
            }
            // It's possible for some ZIP files to contain extra data which isn't a well-formed extra field. zipalign does this, padding the extra data
            // field with zeroes to align the start of the file data to an X-byte boundary. We need to preserve this data so that the recorded "extra data length"
            // fields in the central directory and local file headers are valid.
            // We need to account for this because zipalign is part of the build process for Android applications, and failing to do this will result in
            // .NET for Android generating corrupted .apk files and failing to build.
            trailingExtraFieldData = extraFieldData[totalBytesConsumed..];

            return extraFields;
        }

        public static int TotalSize(List<ZipGenericExtraField>? fields, int trailingDataLength)
        {
            int size = trailingDataLength;

            if (fields != null)
            {
                foreach (ZipGenericExtraField field in fields)
                {
                    size += field.Size + SizeOfHeader; //size is only size of data
                }
            }
            return size;
        }

        public static void WriteAllBlocks(List<ZipGenericExtraField>? fields, ReadOnlySpan<byte> trailingExtraFieldData, Stream stream)
        {
            if (fields != null)
            {
                foreach (ZipGenericExtraField field in fields)
                {
                    field.WriteBlock(stream);
                }
            }

            if (!trailingExtraFieldData.IsEmpty)
            {
                stream.Write(trailingExtraFieldData);
            }
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

        public void WriteBlockCore(Span<byte> extraFieldData)
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

        public void WriteBlock(Stream stream)
        {
            Span<byte> extraFieldData = stackalloc byte[TotalSize];
            WriteBlockCore(extraFieldData);
            stream.Write(extraFieldData);
        }
    }

    internal sealed partial class Zip64EndOfCentralDirectoryLocator
    {
        // The Zip File Format Specification references 0x07064B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static readonly byte[] SignatureConstantBytes = [0x50, 0x4B, 0x06, 0x07];

        public const int TotalSize = FieldLocations.TotalNumberOfDisks + FieldLengths.TotalNumberOfDisks;
        public const int SizeOfBlockWithoutSignature = TotalSize - FieldLengths.Signature;

        public uint NumberOfDiskWithZip64EOCD;
        public ulong OffsetOfZip64EOCD;
        public uint TotalNumberOfDisks;

        private static bool TryReadBlockCore(Span<byte> blockContents, int bytesRead, [NotNullWhen(returnValue: true)] out Zip64EndOfCentralDirectoryLocator? zip64EOCDLocator)
        {
            zip64EOCDLocator = null;
            if (bytesRead < TotalSize || !blockContents.StartsWith(SignatureConstantBytes))
            {
                return false;
            }

            zip64EOCDLocator = new()
            {
                NumberOfDiskWithZip64EOCD = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.NumberOfDiskWithZip64EOCD..]),
                OffsetOfZip64EOCD = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.OffsetOfZip64EOCD..]),
                TotalNumberOfDisks = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.TotalNumberOfDisks..])
            };

            return true;
        }

        public static Zip64EndOfCentralDirectoryLocator TryReadBlock(Stream stream)
        {
            Span<byte> blockContents = stackalloc byte[TotalSize];
            int bytesRead = stream.ReadAtLeast(blockContents, blockContents.Length, throwOnEndOfStream: false);
            bool zip64eocdLocatorProper = TryReadBlockCore(blockContents, bytesRead, out Zip64EndOfCentralDirectoryLocator? zip64EOCDLocator);

            Debug.Assert(zip64eocdLocatorProper && zip64EOCDLocator != null); // we just found this using the signature finder, so it should be okay

            return zip64EOCDLocator;
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

        public static void WriteBlock(Stream stream, long zip64EOCDRecordStart)
        {
            Span<byte> blockContents = stackalloc byte[TotalSize];
            WriteBlockCore(blockContents, zip64EOCDRecordStart);
            stream.Write(blockContents);
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

        private static bool TryReadBlockCore(Span<byte> blockContents, int bytesRead, [NotNullWhen(returnValue: true)] out Zip64EndOfCentralDirectoryRecord? zip64EOCDRecord)
        {
            zip64EOCDRecord = null;
            if (bytesRead < BlockConstantSectionSize)
            {
                return false;
            }

            if (!blockContents.StartsWith(SignatureConstantBytes))
            {
                return false;
            }

            zip64EOCDRecord = new Zip64EndOfCentralDirectoryRecord()
            {
                SizeOfThisRecord = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.SizeOfThisRecord..]),
                VersionMadeBy = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.VersionMadeBy..]),
                VersionNeededToExtract = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.VersionNeededToExtract..]),
                NumberOfThisDisk = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.NumberOfThisDisk..]),
                NumberOfDiskWithStartOfCD = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.NumberOfDiskWithStartOfCD..]),
                NumberOfEntriesOnThisDisk = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.NumberOfEntriesOnThisDisk..]),
                NumberOfEntriesTotal = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.NumberOfEntriesTotal..]),
                SizeOfCentralDirectory = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.SizeOfCentralDirectory..]),
                OffsetOfCentralDirectory = BinaryPrimitives.ReadUInt64LittleEndian(blockContents[FieldLocations.OffsetOfCentralDirectory..])
            };

            return true;
        }

        public static Zip64EndOfCentralDirectoryRecord TryReadBlock(Stream stream)
        {
            Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
            int bytesRead = stream.ReadAtLeast(blockContents, blockContents.Length, throwOnEndOfStream: false);
            if (!TryReadBlockCore(blockContents, bytesRead, out Zip64EndOfCentralDirectoryRecord? zip64EOCDRecord))
            {
                throw new InvalidDataException(SR.Zip64EOCDNotWhereExpected);
            }

            return zip64EOCDRecord;
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

        public static void WriteBlock(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory)
        {
            Span<byte> blockContents = stackalloc byte[BlockConstantSectionSize];
            WriteBlockCore(blockContents, numberOfEntries, startOfCentralDirectory, sizeOfCentralDirectory);
            // write Zip 64 EOCD record
            stream.Write(blockContents);
        }
    }

    internal readonly partial struct ZipLocalFileHeader
    {
        // The Zip File Format Specification references 0x08074B50 and 0x04034B50, these are big endian representations.
        // ZIP files store values in little endian, so these are reversed.
        public static ReadOnlySpan<byte> DataDescriptorSignatureConstantBytes => [0x50, 0x4B, 0x07, 0x08];
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x03, 0x04];
        public const int SizeOfLocalHeader = 30;

        private static void GetExtraFieldsInitialize(Stream stream, out int relativeFilenameLengthLocation, out int relativeExtraFieldLengthLocation)
        {
            relativeFilenameLengthLocation = FieldLocations.FilenameLength - FieldLocations.FilenameLength;
            relativeExtraFieldLengthLocation = FieldLocations.ExtraFieldLength - FieldLocations.FilenameLength;
            stream.Seek(FieldLocations.FilenameLength, SeekOrigin.Current);
        }

        private static void GetExtraFieldsCore(Span<byte> fixedHeaderBuffer, int relativeFilenameLengthLocation, int relativeExtraFieldLengthLocation, out ushort filenameLength, out ushort extraFieldLength)
        {
            filenameLength = BinaryPrimitives.ReadUInt16LittleEndian(fixedHeaderBuffer[relativeFilenameLengthLocation..]);
            extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(fixedHeaderBuffer[relativeExtraFieldLengthLocation..]);
        }

        private static List<ZipGenericExtraField> GetExtraFieldPostReadWork(Span<byte> extraFieldBuffer, out byte[] trailingData)
        {
            List<ZipGenericExtraField> list = ZipGenericExtraField.ParseExtraField(extraFieldBuffer, out ReadOnlySpan<byte> trailingDataSpan);
            Zip64ExtraField.RemoveZip64Blocks(list);
            trailingData = trailingDataSpan.ToArray();
            return list;
        }

        public static List<ZipGenericExtraField> GetExtraFields(Stream stream, out byte[] trailingData)
        {
            // assumes that TrySkipBlock has already been called, so we don't have to validate twice

            Span<byte> fixedHeaderBuffer = stackalloc byte[FieldLengths.FilenameLength + FieldLengths.ExtraFieldLength];
            GetExtraFieldsInitialize(stream, out int relativeFilenameLengthLocation, out int relativeExtraFieldLengthLocation);
            stream.ReadExactly(fixedHeaderBuffer);

            GetExtraFieldsCore(fixedHeaderBuffer, relativeFilenameLengthLocation, relativeExtraFieldLengthLocation, out ushort filenameLength, out ushort extraFieldLength);

            const int StackAllocationThreshold = 512;

            byte[]? arrayPoolBuffer = extraFieldLength > StackAllocationThreshold ? ArrayPool<byte>.Shared.Rent(extraFieldLength) : null;
            Span<byte> extraFieldBuffer = extraFieldLength <= StackAllocationThreshold ? stackalloc byte[StackAllocationThreshold].Slice(0, extraFieldLength) : arrayPoolBuffer.AsSpan(0, extraFieldLength);
            try
            {
                stream.Seek(filenameLength, SeekOrigin.Current);
                stream.ReadExactly(extraFieldBuffer);

                return GetExtraFieldPostReadWork(extraFieldBuffer, out trailingData);
            }
            finally
            {
                if (arrayPoolBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
                }
            }
        }

        private static bool TrySkipBlockCore(Stream stream, Span<byte> blockBytes, int bytesRead, long currPosition)
        {
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

            // Reuse blockBytes to read the filename length and the extra field length - these two consecutive
            // fields fit inside blockBytes.
            Debug.Assert(blockBytes.Length == FieldLengths.FilenameLength + FieldLengths.ExtraFieldLength);

            return true;
        }

        private static bool TrySkipBlockFinalize(Stream stream, Span<byte> blockBytes, int bytesRead)
        {
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

        // will not throw end of stream exception
        public static bool TrySkipBlock(Stream stream)
        {
            Span<byte> blockBytes = stackalloc byte[FieldLengths.Signature];
            long currPosition = stream.Position;
            int bytesRead = stream.ReadAtLeast(blockBytes, blockBytes.Length, throwOnEndOfStream: false);
            if (!TrySkipBlockCore(stream, blockBytes, bytesRead, currPosition))
            {
                return false;
            }
            bytesRead = stream.ReadAtLeast(blockBytes, blockBytes.Length, throwOnEndOfStream: false);
            return TrySkipBlockFinalize(stream, blockBytes, bytesRead);
        }
    }

    internal sealed partial class ZipCentralDirectoryFileHeader
    {
        // The Zip File Format Specification references 0x02014B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static ReadOnlySpan<byte> SignatureConstantBytes => [0x50, 0x4B, 0x01, 0x02];

        private const int StackAllocationThreshold = 512;

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
        public byte[]? TrailingExtraFieldData;

        private static bool TryReadBlockInitialize(ReadOnlySpan<byte> buffer, [NotNullWhen(returnValue: true)] out ZipCentralDirectoryFileHeader? header, out int bytesRead, out uint compressedSizeSmall, out uint uncompressedSizeSmall, out ushort diskNumberStartSmall, out uint relativeOffsetOfLocalHeaderSmall)
        {
            // the buffer will always be large enough for at least the constant section to be verified
            Debug.Assert(buffer.Length >= BlockConstantSectionSize);

            header = null;
            bytesRead = 0;
            compressedSizeSmall = 0;
            uncompressedSizeSmall = 0;
            diskNumberStartSmall = 0;
            relativeOffsetOfLocalHeaderSmall = 0;
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

            compressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.CompressedSize..]);
            uncompressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.UncompressedSize..]);
            diskNumberStartSmall = BinaryPrimitives.ReadUInt16LittleEndian(buffer[FieldLocations.DiskNumberStart..]);
            relativeOffsetOfLocalHeaderSmall = BinaryPrimitives.ReadUInt32LittleEndian(buffer[FieldLocations.RelativeOffsetOfLocalHeader..]);

            return true;
        }

        private static void TryReadBlockFinalize(ZipCentralDirectoryFileHeader header, ReadOnlySpan<byte> dynamicHeader, int dynamicHeaderSize, uint uncompressedSizeSmall, uint compressedSizeSmall, ushort diskNumberStartSmall, uint relativeOffsetOfLocalHeaderSmall, bool saveExtraFieldsAndComments, ref int bytesRead, out Zip64ExtraField zip64)
        {
            header.Filename = dynamicHeader[..header.FilenameLength].ToArray();

            bool uncompressedSizeInZip64 = uncompressedSizeSmall == ZipHelper.Mask32Bit;
            bool compressedSizeInZip64 = compressedSizeSmall == ZipHelper.Mask32Bit;
            bool relativeOffsetInZip64 = relativeOffsetOfLocalHeaderSmall == ZipHelper.Mask32Bit;
            bool diskNumberStartInZip64 = diskNumberStartSmall == ZipHelper.Mask16Bit;

            ReadOnlySpan<byte> zipExtraFields = dynamicHeader.Slice(header.FilenameLength, header.ExtraFieldLength);

            if (saveExtraFieldsAndComments)
            {
                header.ExtraFields = ZipGenericExtraField.ParseExtraField(zipExtraFields, out ReadOnlySpan<byte> trailingDataSpan);
                zip64 = Zip64ExtraField.GetAndRemoveZip64Block(header.ExtraFields,
                            uncompressedSizeInZip64, compressedSizeInZip64,
                            relativeOffsetInZip64, diskNumberStartInZip64);
                header.TrailingExtraFieldData = trailingDataSpan.ToArray();
            }
            else
            {
                header.ExtraFields = null;
                header.TrailingExtraFieldData = null;
                zip64 = Zip64ExtraField.GetJustZip64Block(zipExtraFields,
                            uncompressedSizeInZip64, compressedSizeInZip64,
                            relativeOffsetInZip64, diskNumberStartInZip64);
            }

            header.FileComment = dynamicHeader.Slice(header.FilenameLength + header.ExtraFieldLength, header.FileCommentLength).ToArray();

            bytesRead = FieldLocations.DynamicData + dynamicHeaderSize;

            header.UncompressedSize = zip64.UncompressedSize ?? uncompressedSizeSmall;
            header.CompressedSize = zip64.CompressedSize ?? compressedSizeSmall;
            header.RelativeOffsetOfLocalHeader = zip64.LocalHeaderOffset ?? relativeOffsetOfLocalHeaderSmall;
            header.DiskNumberStart = zip64.StartDiskNumber ?? diskNumberStartSmall;
        }

        // if saveExtraFieldsAndComments is false, FileComment and ExtraFields will be null
        // in either case, the zip64 extra field info will be incorporated into other fields
        public static bool TryReadBlock(ReadOnlySpan<byte> buffer, Stream furtherReads, bool saveExtraFieldsAndComments, out int bytesRead, [NotNullWhen(returnValue: true)] out ZipCentralDirectoryFileHeader? header)
        {
            if (!TryReadBlockInitialize(buffer, out header, out bytesRead, out uint compressedSizeSmall, out uint uncompressedSizeSmall, out ushort diskNumberStartSmall, out uint relativeOffsetOfLocalHeaderSmall))
            {
                return false;
            }

            byte[]? arrayPoolBuffer = null;
            try
            {
                // Assemble the dynamic header in a separate buffer. We can't guarantee that it's all in the input buffer,
                // some additional data might need to come from the stream.
                int dynamicHeaderSize = header.FilenameLength + header.ExtraFieldLength + header.FileCommentLength;
                int remainingBufferLength = buffer.Length - FieldLocations.DynamicData;
                int bytesToRead = dynamicHeaderSize - remainingBufferLength;
                scoped ReadOnlySpan<byte> dynamicHeader;

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
                        arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(dynamicHeaderSize);
                    }

                    Span<byte> collatedHeader = dynamicHeaderSize <= StackAllocationThreshold ? stackalloc byte[StackAllocationThreshold].Slice(0, dynamicHeaderSize) : arrayPoolBuffer.AsSpan(0, dynamicHeaderSize);

                    buffer[FieldLocations.DynamicData..].CopyTo(collatedHeader);

                    Debug.Assert(bytesToRead == collatedHeader[remainingBufferLength..].Length);
                    int realBytesRead = furtherReads.ReadAtLeast(collatedHeader[remainingBufferLength..], bytesToRead, throwOnEndOfStream: false);

                    if (realBytesRead != bytesToRead)
                    {
                        return false;
                    }
                    dynamicHeader = collatedHeader;
                }

                TryReadBlockFinalize(header, dynamicHeader, dynamicHeaderSize, uncompressedSizeSmall, compressedSizeSmall, diskNumberStartSmall, relativeOffsetOfLocalHeaderSmall, saveExtraFieldsAndComments, ref bytesRead, out Zip64ExtraField zip64);
            }
            finally
            {
                if (arrayPoolBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
                }
            }

            return true;
        }
    }

    internal sealed partial class ZipEndOfCentralDirectoryBlock
    {
        // The Zip File Format Specification references 0x06054B50, this is a big endian representation.
        // ZIP files store values in little endian, so this is reversed.
        public static readonly byte[] SignatureConstantBytes = [0x50, 0x4B, 0x05, 0x06];

        // This also assumes a zero-length comment.
        public const int TotalSize = FieldLocations.ArchiveCommentLength + FieldLengths.ArchiveCommentLength;
        // These are the minimum possible size, assuming the zip file comments variable section is empty
        public const int SizeOfBlockWithoutSignature = TotalSize - FieldLengths.Signature;

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

        private static void WriteBlockInitialize(Span<byte> blockContents, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory, byte[] archiveComment)
        {
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
        }

        public static void WriteBlock(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory, byte[] archiveComment)
        {
            Span<byte> blockContents = stackalloc byte[TotalSize];

            WriteBlockInitialize(blockContents, numberOfEntries, startOfCentralDirectory, sizeOfCentralDirectory, archiveComment);

            stream.Write(blockContents);
            if (archiveComment.Length > 0)
            {
                stream.Write(archiveComment);
            }
        }

        private static bool TryReadBlockInitialize(Stream stream, Span<byte> blockContents, int bytesRead, [NotNullWhen(returnValue: true)] out ZipEndOfCentralDirectoryBlock? eocdBlock, out bool readComment)
        {
            readComment = false;

            eocdBlock = null;
            if (bytesRead < TotalSize)
            {
                return false;
            }

            if (!blockContents.StartsWith(SignatureConstantBytes))
            {
                return false;
            }
            eocdBlock = new()
            {
                Signature = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.Signature..]),
                NumberOfThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.NumberOfThisDisk..]),
                NumberOfTheDiskWithTheStartOfTheCentralDirectory = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.NumberOfTheDiskWithTheStartOfTheCentralDirectory..]),
                NumberOfEntriesInTheCentralDirectoryOnThisDisk = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.NumberOfEntriesInTheCentralDirectoryOnThisDisk..]),
                NumberOfEntriesInTheCentralDirectory = BinaryPrimitives.ReadUInt16LittleEndian(blockContents[FieldLocations.NumberOfEntriesInTheCentralDirectory..]),
                SizeOfCentralDirectory = BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.SizeOfCentralDirectory..]),
                OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber =
                    BinaryPrimitives.ReadUInt32LittleEndian(blockContents[FieldLocations.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber..])
            };

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
                readComment = true;
            }

            return true;
        }

        public static ZipEndOfCentralDirectoryBlock ReadBlock(Stream stream)
        {
            Span<byte> blockContents = stackalloc byte[TotalSize];
            int bytesRead = stream.ReadAtLeast(blockContents, blockContents.Length, throwOnEndOfStream: false);

            if (!TryReadBlockInitialize(stream, blockContents, bytesRead, out ZipEndOfCentralDirectoryBlock? eocdBlock, out bool readComment))
            {
                // // We shouldn't get here becasue we found the eocd block using the signature finder
                throw new InvalidDataException(SR.EOCDNotFound);
            }
            else if (readComment)
            {
                stream.ReadExactly(eocdBlock._archiveComment);
            }

            return eocdBlock;
        }
    }
}
