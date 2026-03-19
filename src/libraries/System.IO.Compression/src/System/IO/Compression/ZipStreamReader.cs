// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public sealed class ZipStreamReader : IDisposable, IAsyncDisposable
{
    private const ushort DataDescriptorBitFlag = 0x8;
    private const ushort UnicodeFileNameBitFlag = 0x800;

    private bool _isDisposed;
    private readonly bool _leaveOpen;
    private readonly Encoding? _entryNameEncoding;
    private ZipStreamEntry? _currentEntry;
    private readonly Stream _archiveStream;
    private bool _reachedEnd;

    public ZipStreamReader(Stream archiveStream, bool leaveOpen = false, Encoding? entryNameEncoding = null)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);

        if (!archiveStream.CanRead)
        {
            throw new ArgumentException(SR.NotSupported_UnreadableStream, nameof(archiveStream));
        }

        _archiveStream = archiveStream;
        _leaveOpen = leaveOpen;
        _entryNameEncoding = entryNameEncoding;
    }

    /// <summary>
    /// Reads the next entry from the ZIP archive stream by parsing the local file header.
    /// </summary>
    /// <returns>
    /// The next <see cref="ZipStreamEntry"/>, or <see langword="null"/> if there are no more entries.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
    /// <exception cref="InvalidDataException">The archive stream contains invalid data.</exception>
    public ZipStreamEntry? GetNextEntry()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_reachedEnd)
        {
            return null;
        }

        AdvancePastCurrentEntry();

        Span<byte> headerBytes = stackalloc byte[ZipLocalFileHeader.SizeOfLocalHeader];
        int bytesRead = _archiveStream.ReadAtLeast(headerBytes, headerBytes.Length, throwOnEndOfStream: false);

        if (bytesRead < ZipLocalFileHeader.SizeOfLocalHeader)
        {
            _reachedEnd = true;
            return null;
        }

        if (!headerBytes.StartsWith(ZipLocalFileHeader.SignatureConstantBytes))
        {
            _reachedEnd = true;
            return null;
        }

        ReadLocalFileHeader(headerBytes, out string fullName, out ushort versionNeeded, out ushort generalPurposeBitFlags,
            out ushort compressionMethod, out DateTimeOffset lastModified, out uint crc32,
            out long compressedSize, out long uncompressedSize, out bool hasDataDescriptor);

        _currentEntry = new ZipStreamEntry(
            fullName, (ZipCompressionMethod)compressionMethod, lastModified, crc32,
            compressedSize, uncompressedSize, generalPurposeBitFlags, versionNeeded,
            _archiveStream, hasDataDescriptor);

        return _currentEntry;
    }

    /// <summary>
    /// Asynchronously reads the next entry from the ZIP archive stream by parsing the local file header.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// The next <see cref="ZipStreamEntry"/>, or <see langword="null"/> if there are no more entries.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
    /// <exception cref="InvalidDataException">The archive stream contains invalid data.</exception>
    public async ValueTask<ZipStreamEntry?> GetNextEntryAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_reachedEnd)
        {
            return null;
        }

        await AdvancePastCurrentEntryAsync(cancellationToken).ConfigureAwait(false);

        byte[] headerBytes = ArrayPool<byte>.Shared.Rent(ZipLocalFileHeader.SizeOfLocalHeader);
        try
        {
            int bytesRead = await _archiveStream.ReadAtLeastAsync(
                headerBytes.AsMemory(0, ZipLocalFileHeader.SizeOfLocalHeader),
                ZipLocalFileHeader.SizeOfLocalHeader,
                throwOnEndOfStream: false,
                cancellationToken).ConfigureAwait(false);

            if (bytesRead < ZipLocalFileHeader.SizeOfLocalHeader)
            {
                _reachedEnd = true;
                return null;
            }

            if (!headerBytes.AsSpan().StartsWith(ZipLocalFileHeader.SignatureConstantBytes))
            {
                _reachedEnd = true;
                return null;
            }

            await ReadLocalFileHeaderAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBytes);
        }

        return _currentEntry;
    }

    private void AdvancePastCurrentEntry()
    {
        if (_currentEntry is null)
        {
            return;
        }

        _currentEntry.SkipCompressedData();

        if (_currentEntry.HasDataDescriptor)
        {
            ReadDataDescriptor(_currentEntry);
        }

        _currentEntry = null;
    }

    private async ValueTask AdvancePastCurrentEntryAsync(CancellationToken cancellationToken)
    {
        if (_currentEntry is null)
        {
            return;
        }

        await _currentEntry.SkipCompressedDataAsync(cancellationToken).ConfigureAwait(false);

        if (_currentEntry.HasDataDescriptor)
        {
            await ReadDataDescriptorAsync(_currentEntry, cancellationToken).ConfigureAwait(false);
        }

        _currentEntry = null;
    }

    private void ReadLocalFileHeader(
        ReadOnlySpan<byte> headerBytes,
        out string fullName,
        out ushort versionNeeded,
        out ushort generalPurposeBitFlags,
        out ushort compressionMethod,
        out DateTimeOffset lastModified,
        out uint crc32,
        out long compressedSize,
        out long uncompressedSize,
        out bool hasDataDescriptor)
    {
        versionNeeded = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.VersionNeededToExtract..]);
        generalPurposeBitFlags = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.GeneralPurposeBitFlags..]);
        compressionMethod = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.CompressionMethod..]);
        uint lastModifiedRaw = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.LastModified..]);
        crc32 = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.Crc32..]);
        uint compressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.CompressedSize..]);
        uint uncompressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.UncompressedSize..]);
        ushort filenameLength = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.FilenameLength..]);
        ushort extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.ExtraFieldLength..]);

        lastModified = new DateTimeOffset(ZipHelper.DosTimeToDateTime(lastModifiedRaw));
        hasDataDescriptor = (generalPurposeBitFlags & DataDescriptorBitFlag) != 0;

        int dynamicLength = filenameLength + extraFieldLength;
        byte[]? rentedBuffer = null;
        Span<byte> dynamicBuffer = dynamicLength <= 512
            ? stackalloc byte[512].Slice(0, dynamicLength)
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(dynamicLength)).AsSpan(0, dynamicLength);

        try
        {
            _archiveStream.ReadExactly(dynamicBuffer);

            Encoding encoding = (generalPurposeBitFlags & UnicodeFileNameBitFlag) != 0
                ? Encoding.UTF8
                : _entryNameEncoding ?? Encoding.UTF8;

            fullName = encoding.GetString(dynamicBuffer[..filenameLength]);

            // Handle Zip64 extra field for sizes
            bool compressedSizeInZip64 = compressedSizeSmall == ZipHelper.Mask32Bit;
            bool uncompressedSizeInZip64 = uncompressedSizeSmall == ZipHelper.Mask32Bit;

            if (compressedSizeInZip64 || uncompressedSizeInZip64)
            {
                Zip64ExtraField zip64 = Zip64ExtraField.GetJustZip64Block(
                    dynamicBuffer.Slice(filenameLength, extraFieldLength),
                    readUncompressedSize: uncompressedSizeInZip64,
                    readCompressedSize: compressedSizeInZip64,
                    readLocalHeaderOffset: false,
                    readStartDiskNumber: false);

                compressedSize = zip64.CompressedSize ?? compressedSizeSmall;
                uncompressedSize = zip64.UncompressedSize ?? uncompressedSizeSmall;
            }
            else
            {
                compressedSize = compressedSizeSmall;
                uncompressedSize = uncompressedSizeSmall;
            }
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    private async ValueTask ReadLocalFileHeaderAsync(byte[] headerBytes, CancellationToken cancellationToken)
    {
        ushort versionNeeded = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.VersionNeededToExtract));
        ushort generalPurposeBitFlags = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.GeneralPurposeBitFlags));
        ushort compressionMethod = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.CompressionMethod));
        uint lastModifiedRaw = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.LastModified));
        uint crc32 = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.Crc32));
        uint compressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.CompressedSize));
        uint uncompressedSizeSmall = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.UncompressedSize));
        ushort filenameLength = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.FilenameLength));
        ushort extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes.AsSpan(ZipLocalFileHeader.FieldLocations.ExtraFieldLength));

        DateTimeOffset lastModified = new DateTimeOffset(ZipHelper.DosTimeToDateTime(lastModifiedRaw));
        bool hasDataDescriptor = (generalPurposeBitFlags & DataDescriptorBitFlag) != 0;

        int dynamicLength = filenameLength + extraFieldLength;
        byte[] dynamicBuffer = ArrayPool<byte>.Shared.Rent(dynamicLength);

        try
        {
            await _archiveStream.ReadExactlyAsync(dynamicBuffer.AsMemory(0, dynamicLength), cancellationToken).ConfigureAwait(false);

            Encoding encoding = (generalPurposeBitFlags & UnicodeFileNameBitFlag) != 0
                ? Encoding.UTF8
                : _entryNameEncoding ?? Encoding.UTF8;

            string fullName = encoding.GetString(dynamicBuffer.AsSpan(0, filenameLength));

            bool compressedSizeInZip64 = compressedSizeSmall == ZipHelper.Mask32Bit;
            bool uncompressedSizeInZip64 = uncompressedSizeSmall == ZipHelper.Mask32Bit;
            long compressedSize;
            long uncompressedSize;

            if (compressedSizeInZip64 || uncompressedSizeInZip64)
            {
                Zip64ExtraField zip64 = Zip64ExtraField.GetJustZip64Block(
                    dynamicBuffer.AsSpan(filenameLength, extraFieldLength),
                    readUncompressedSize: uncompressedSizeInZip64,
                    readCompressedSize: compressedSizeInZip64,
                    readLocalHeaderOffset: false,
                    readStartDiskNumber: false);

                compressedSize = zip64.CompressedSize ?? compressedSizeSmall;
                uncompressedSize = zip64.UncompressedSize ?? uncompressedSizeSmall;
            }
            else
            {
                compressedSize = compressedSizeSmall;
                uncompressedSize = uncompressedSizeSmall;
            }

            _currentEntry = new ZipStreamEntry(
                fullName, (ZipCompressionMethod)compressionMethod, lastModified, crc32,
                compressedSize, uncompressedSize, generalPurposeBitFlags, versionNeeded,
                _archiveStream, hasDataDescriptor);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(dynamicBuffer);
        }
    }

    private void ReadDataDescriptor(ZipStreamEntry entry)
    {
        // Data descriptor layout (signature is optional):
        //   [signature 4B] + CRC-32 4B + compressed size (4B or 8B) + uncompressed size (4B or 8B)
        // Read incrementally to avoid consuming bytes from the next entry.
        Span<byte> buffer = stackalloc byte[24];

        _archiveStream.ReadExactly(buffer[..4]);
        int offset = 0;
        int totalRead = 4;

        if (buffer[..4].SequenceEqual(ZipLocalFileHeader.DataDescriptorSignatureConstantBytes))
        {
            offset = 4;
            _archiveStream.ReadExactly(buffer.Slice(4, 4));
            totalRead = 8;
        }

        bool isZip64 = entry.VersionNeeded >= (ushort)ZipVersionNeededValues.Zip64;
        int sizesBytes = isZip64 ? 16 : 8;
        _archiveStream.ReadExactly(buffer.Slice(totalRead, sizesBytes));

        uint crc32 = BinaryPrimitives.ReadUInt32LittleEndian(buffer[offset..]);
        int sizesOffset = offset + 4;

        if (isZip64)
        {
            entry.UpdateDataDescriptor(
                crc32,
                compressedLength: BinaryPrimitives.ReadInt64LittleEndian(buffer[sizesOffset..]),
                length: BinaryPrimitives.ReadInt64LittleEndian(buffer[(sizesOffset + 8)..]));
        }
        else
        {
            entry.UpdateDataDescriptor(
                crc32,
                compressedLength: BinaryPrimitives.ReadUInt32LittleEndian(buffer[sizesOffset..]),
                length: BinaryPrimitives.ReadUInt32LittleEndian(buffer[(sizesOffset + 4)..]));
        }
    }

    private async ValueTask ReadDataDescriptorAsync(ZipStreamEntry entry, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[24];

        await _archiveStream.ReadExactlyAsync(buffer.AsMemory(0, 4), cancellationToken).ConfigureAwait(false);
        int offset = 0;
        int totalRead = 4;

        if (buffer.AsSpan(0, 4).SequenceEqual(ZipLocalFileHeader.DataDescriptorSignatureConstantBytes))
        {
            offset = 4;
            await _archiveStream.ReadExactlyAsync(buffer.AsMemory(4, 4), cancellationToken).ConfigureAwait(false);
            totalRead = 8;
        }

        bool isZip64 = entry.VersionNeeded >= (ushort)ZipVersionNeededValues.Zip64;
        int sizesBytes = isZip64 ? 16 : 8;
        await _archiveStream.ReadExactlyAsync(buffer.AsMemory(totalRead, sizesBytes), cancellationToken).ConfigureAwait(false);

        uint crc32 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset));
        int sizesOffset = offset + 4;

        if (isZip64)
        {
            entry.UpdateDataDescriptor(
                crc32,
                compressedLength: BinaryPrimitives.ReadInt64LittleEndian(buffer.AsSpan(sizesOffset)),
                length: BinaryPrimitives.ReadInt64LittleEndian(buffer.AsSpan(sizesOffset + 8)));
        }
        else
        {
            entry.UpdateDataDescriptor(
                crc32,
                compressedLength: BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(sizesOffset)),
                length: BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(sizesOffset + 4)));
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (!_leaveOpen)
            {
                _archiveStream.Dispose();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (!_leaveOpen)
            {
                await _archiveStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

}
