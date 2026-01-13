// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

internal sealed partial class ZipGenericExtraField
{
    public async Task WriteBlockAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] extraFieldHeader = new byte[SizeOfHeader];
        WriteBlockCore(extraFieldHeader);
        await stream.WriteAsync(extraFieldHeader, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(Data, cancellationToken).ConfigureAwait(false);
    }

    public static async Task WriteAllBlocksAsync(List<ZipGenericExtraField>? fields, ReadOnlyMemory<byte> trailingExtraFieldData, Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (fields != null)
        {
            foreach (ZipGenericExtraField field in fields)
            {
                await field.WriteBlockAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }

        if (!trailingExtraFieldData.IsEmpty)
        {
            await stream.WriteAsync(trailingExtraFieldData, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task WriteAllBlocksExcludingTagAsync(List<ZipGenericExtraField>? fields, ReadOnlyMemory<byte> trailingExtraFieldData, Stream stream, ushort excludeTag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (fields != null)
        {
            foreach (ZipGenericExtraField field in fields)
            {
                if (field.Tag != excludeTag)
                {
                    await field.WriteBlockAsync(stream, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (!trailingExtraFieldData.IsEmpty)
        {
            await stream.WriteAsync(trailingExtraFieldData, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal sealed partial class Zip64ExtraField
{
    public ValueTask WriteBlockAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] extraFieldData = new byte[TotalSize];
        WriteBlockCore(extraFieldData);
        return stream.WriteAsync(extraFieldData, cancellationToken);
    }

}

internal sealed partial class Zip64EndOfCentralDirectoryLocator
{
    public static async Task<Zip64EndOfCentralDirectoryLocator> TryReadBlockAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] blockContents = new byte[TotalSize];
        int bytesRead = await stream.ReadAtLeastAsync(blockContents, blockContents.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
        bool zip64eocdLocatorProper = TryReadBlockCore(blockContents, bytesRead, out Zip64EndOfCentralDirectoryLocator? zip64EOCDLocator);

        Debug.Assert(zip64eocdLocatorProper && zip64EOCDLocator != null); // we just found this using the signature finder, so it should be okay

        return zip64EOCDLocator;
    }

    public static ValueTask WriteBlockAsync(Stream stream, long zip64EOCDRecordStart, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] blockContents = new byte[TotalSize];
        WriteBlockCore(blockContents, zip64EOCDRecordStart);
        return stream.WriteAsync(blockContents, cancellationToken);
    }
}

internal sealed partial class Zip64EndOfCentralDirectoryRecord
{
    public static async Task<Zip64EndOfCentralDirectoryRecord> TryReadBlockAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] blockContents = new byte[BlockConstantSectionSize];
        int bytesRead = await stream.ReadAtLeastAsync(blockContents, blockContents.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);

        if (!TryReadBlockCore(blockContents, bytesRead, out Zip64EndOfCentralDirectoryRecord? zip64EOCDRecord))
        {
            throw new InvalidDataException(SR.Zip64EOCDNotWhereExpected);
        }

        return zip64EOCDRecord;
    }

    public static ValueTask WriteBlockAsync(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] blockContents = new byte[BlockConstantSectionSize];
        WriteBlockCore(blockContents, numberOfEntries, startOfCentralDirectory, sizeOfCentralDirectory);
        // write Zip 64 EOCD record
        return stream.WriteAsync(blockContents, cancellationToken);
    }
}

internal readonly partial struct ZipLocalFileHeader
{
    public static async Task<(List<ZipGenericExtraField>, byte[] trailingData)> GetExtraFieldsAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // assumes that TrySkipBlock has already been called, so we don't have to validate twice

        byte[] fixedHeaderBuffer = new byte[FieldLengths.FilenameLength + FieldLengths.ExtraFieldLength];
        GetExtraFieldsInitialize(stream, out int relativeFilenameLengthLocation, out int relativeExtraFieldLengthLocation);
        await stream.ReadExactlyAsync(fixedHeaderBuffer, cancellationToken).ConfigureAwait(false);

        GetExtraFieldsCore(fixedHeaderBuffer, relativeFilenameLengthLocation, relativeExtraFieldLengthLocation, out ushort filenameLength, out ushort extraFieldLength);

        byte[] arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(extraFieldLength);
        Memory<byte> extraFieldBuffer = arrayPoolBuffer.AsMemory(0, extraFieldLength);

        try
        {
            stream.Seek(filenameLength, SeekOrigin.Current);
            await stream.ReadExactlyAsync(extraFieldBuffer, cancellationToken).ConfigureAwait(false);

            List<ZipGenericExtraField> list = GetExtraFieldPostReadWork(extraFieldBuffer.Span, out byte[] trailingData);

            return (list, trailingData);
        }
        finally
        {
            if (arrayPoolBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
            }
        }
    }

    // will not throw end of stream exception
    public static async Task<bool> TrySkipBlockAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] blockBytes = new byte[FieldLengths.Signature];
        long currPosition = stream.Position;
        int bytesRead = await stream.ReadAtLeastAsync(blockBytes, blockBytes.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
        if (!TrySkipBlockCore(stream, blockBytes, bytesRead, currPosition))
        {
            return false;
        }
        bytesRead = await stream.ReadAtLeastAsync(blockBytes, blockBytes.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
        return TrySkipBlockFinalize(stream, blockBytes, bytesRead);
    }

    public static async Task<(bool success, WinZipAesExtraField? aesExtraField)> TrySkipBlockAESAwareAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WinZipAesExtraField? aesExtraField = null;

        // Read the first 4 bytes (local file header signature)
        byte[] signatureBytes = new byte[4];
        await stream.ReadExactlyAsync(signatureBytes, cancellationToken).ConfigureAwait(false);
        if (!signatureBytes.AsSpan().SequenceEqual(SignatureConstantBytes))
        {
            return (false, null); // Not a valid local file header
        }

        // Read fixed-size fields after signature
        // Skip version through mod date (10 bytes), then skip CRC32 + sizes (12 bytes)
        byte[] skipBuffer = new byte[22];
        await stream.ReadExactlyAsync(skipBuffer, cancellationToken).ConfigureAwait(false);

        byte[] lengthBuffer = new byte[4];
        await stream.ReadExactlyAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
        ushort nameLength = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(lengthBuffer.AsSpan(0, 2));
        ushort extraLength = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(lengthBuffer.AsSpan(2, 2));

        // Skip file name
        stream.Seek(nameLength, SeekOrigin.Current);

        // Parse extra fields if present
        if (extraLength > 0)
        {
            long extraStart = stream.Position;
            long extraEnd = extraStart + extraLength;

            byte[] fieldHeader = new byte[4];
            while (stream.Position < extraEnd)
            {
                await stream.ReadExactlyAsync(fieldHeader, cancellationToken).ConfigureAwait(false);
                ushort headerId = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(fieldHeader.AsSpan(0, 2));
                ushort dataSize = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(fieldHeader.AsSpan(2, 2));

                if (headerId == WinZipAesExtraField.HeaderId) // 0x9901
                {
                    // AES extra field structure:
                    // Vendor version (2) + Vendor ID (2) + AES strength (1) + Original compression (2)
                    byte[] aesData = new byte[7];
                    await stream.ReadExactlyAsync(aesData, cancellationToken).ConfigureAwait(false);
                    ushort vendorVersion = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(aesData.AsSpan(0, 2));
                    // Skip vendor ID 'AE' (bytes 2-3)
                    byte aesStrength = aesData[4]; // 1, 2, or 3
                    ushort compressionMethod = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(aesData.AsSpan(5, 2));

                    aesExtraField = new WinZipAesExtraField(vendorVersion, aesStrength, compressionMethod);
                    break;
                }
                else
                {
                    stream.Seek(dataSize, SeekOrigin.Current); // Skip unknown extra field
                }
            }
        }

        return (true, aesExtraField);
    }
}

internal sealed partial class ZipCentralDirectoryFileHeader
{
    // if saveExtraFieldsAndComments is false, FileComment and ExtraFields will be null
    // in either case, the zip64 extra field info will be incorporated into other fields
    public static async Task<(bool, int, ZipCentralDirectoryFileHeader?)> TryReadBlockAsync(ReadOnlyMemory<byte> buffer, Stream furtherReads, bool saveExtraFieldsAndComments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ZipCentralDirectoryFileHeader? header;
        if (!TryReadBlockInitialize(buffer.Span, out header, out int bytesRead, out uint compressedSizeSmall, out uint uncompressedSizeSmall, out ushort diskNumberStartSmall, out uint relativeOffsetOfLocalHeaderSmall))
        {
            return (false, 0, null);
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
                dynamicHeader = buffer.Span[FieldLocations.DynamicData..];
            }
            // Data needs to come from two sources, and we must thus copy data into a single address space.
            else
            {
                if (dynamicHeaderSize > StackAllocationThreshold)
                {
                    arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(dynamicHeaderSize);
                }

                byte[] collatedHeader = dynamicHeaderSize <= StackAllocationThreshold ? new byte[dynamicHeaderSize] : arrayPoolBuffer.AsSpan(0, dynamicHeaderSize).ToArray();

                buffer[FieldLocations.DynamicData..].CopyTo(collatedHeader);

                Debug.Assert(bytesToRead == collatedHeader[remainingBufferLength..].Length);
                int realBytesRead = await furtherReads.ReadAtLeastAsync(collatedHeader.AsMemory(remainingBufferLength..), bytesToRead, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);

                if (realBytesRead != bytesToRead)
                {
                    return (false, bytesRead, null);
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

        return (true, bytesRead, header);
    }
}

internal sealed partial class ZipEndOfCentralDirectoryBlock
{
    public static async Task WriteBlockAsync(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory, byte[] archiveComment, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] blockContents = new byte[TotalSize];

        WriteBlockInitialize(blockContents, numberOfEntries, startOfCentralDirectory, sizeOfCentralDirectory, archiveComment);

        await stream.WriteAsync(blockContents, cancellationToken).ConfigureAwait(false);
        if (archiveComment.Length > 0)
        {
            await stream.WriteAsync(archiveComment, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task<ZipEndOfCentralDirectoryBlock> ReadBlockAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] blockContents = new byte[TotalSize];
        int bytesRead = await stream.ReadAtLeastAsync(blockContents, blockContents.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);

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
