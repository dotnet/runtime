// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

/// <summary>
/// Provides a forward-only reader for ZIP archives that reads entries sequentially
/// from a stream without requiring the stream to be seekable.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="ZipArchive"/>, which reads the central directory at the end
/// of the archive, <see cref="ZipStreamReader"/> walks local file headers in order
/// and decompresses data on the fly. This makes it suitable for network streams,
/// pipes, and other non-seekable sources.
/// </para>
/// <para>
/// This mirrors the <c>TarReader</c> / <c>TarEntry</c> pattern in
/// <c>System.Formats.Tar</c>.
/// </para>
/// </remarks>
public sealed class ZipStreamReader : IDisposable, IAsyncDisposable
{
    private const ushort DataDescriptorBitFlag = 0x8;
    private const ushort UnicodeFileNameBitFlag = 0x800;

    private bool _isDisposed;
    private readonly bool _leaveOpen;
    private readonly Encoding? _entryNameEncoding;
    private ZipForwardReadEntry? _previousEntry;
    private readonly Stream _archiveStream;
    private bool _reachedEnd;

    /// <summary>
    /// Initializes a new <see cref="ZipStreamReader"/> that reads from the specified stream.
    /// </summary>
    /// <param name="stream">The archive stream to read from.</param>
    /// <param name="leaveOpen">
    /// <see langword="true"/> to leave the stream open after the reader is disposed;
    /// otherwise, <see langword="false"/>.
    /// </param>
    public ZipStreamReader(Stream stream, bool leaveOpen = false)
        : this(stream, entryNameEncoding: null, leaveOpen)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ZipStreamReader"/> that reads from the specified stream
    /// using the given encoding for entry names.
    /// </summary>
    /// <param name="stream">The archive stream to read from.</param>
    /// <param name="entryNameEncoding">
    /// The encoding to use when reading entry names that do not have the UTF-8 bit flag set,
    /// or <see langword="null"/> to use UTF-8.
    /// </param>
    /// <param name="leaveOpen">
    /// <see langword="true"/> to leave the stream open after the reader is disposed;
    /// otherwise, <see langword="false"/>.
    /// </param>
    public ZipStreamReader(Stream stream, Encoding? entryNameEncoding, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException(SR.NotSupported_UnreadableStream, nameof(stream));
        }

        // ReadAheadStream makes non-seekable streams appear seekable so that
        // DeflateStream.TryRewindStream can push back unconsumed input after
        // decompression finishes. Already-seekable streams need no wrapper.
        _archiveStream = stream.CanSeek ? stream : new ReadAheadStream(stream);
        _leaveOpen = leaveOpen;
        _entryNameEncoding = entryNameEncoding;
    }

    /// <summary>
    /// Reads the next entry from the ZIP archive stream by parsing the local file header.
    /// </summary>
    /// <param name="copyData">
    /// <see langword="true"/> to copy the entry's decompressed data into a <see cref="MemoryStream"/>
    /// that remains valid after the reader advances; <see langword="false"/> to read directly
    /// from the archive stream (invalidated on the next <see cref="GetNextEntry"/> call).
    /// </param>
    /// <returns>
    /// The next <see cref="ZipForwardReadEntry"/>, or <see langword="null"/> if there are no more entries.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
    /// <exception cref="InvalidDataException">The archive stream contains invalid data.</exception>
    public ZipForwardReadEntry? GetNextEntry(bool copyData = false)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_reachedEnd)
        {
            return null;
        }

        AdvanceDataStreamIfNeeded();

        byte[] headerBytes = new byte[ZipLocalFileHeader.SizeOfLocalHeader];
        int bytesRead = _archiveStream.ReadAtLeast(headerBytes, headerBytes.Length, throwOnEndOfStream: false);

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

        int dynamicLength = GetDynamicHeaderLength(headerBytes);
        byte[] dynamicBuffer = new byte[dynamicLength];
        _archiveStream.ReadExactly(dynamicBuffer);

        ParseLocalFileHeader(headerBytes, dynamicBuffer,
            out string fullName, out ushort versionNeeded, out ushort generalPurposeBitFlags,
            out ushort compressionMethod, out DateTimeOffset lastModified, out uint crc32,
            out long compressedSize, out long uncompressedSize, out bool hasDataDescriptor);

        bool isEncrypted = (generalPurposeBitFlags & 1) != 0;

        ZipCompressionMethod method = (ZipCompressionMethod)compressionMethod;

        Stream? dataStream = CreateDataStream(
            method, compressedSize, uncompressedSize,
            crc32, hasDataDescriptor, isEncrypted, out CrcValidatingReadStream? crcStream);

        Stream? originalDataStream = null;
        if (copyData && dataStream is not null)
        {
            originalDataStream = dataStream;
            MemoryStream ms = new();
            dataStream.CopyTo(ms);
            ms.Position = 0;
            dataStream = ms;
        }

        ZipForwardReadEntry entry = new(
            fullName, method, lastModified, crc32,
            compressedSize, uncompressedSize, generalPurposeBitFlags, versionNeeded,
            hasDataDescriptor, dataStream);

        if (copyData && hasDataDescriptor && crcStream is not null)
        {
            ReadDataDescriptor(entry, crcStream);
        }

        // Dispose the original decompression/CRC stream after copying (and after
        // reading the data descriptor when applicable) to release inflater resources.
        originalDataStream?.Dispose();

        if (!copyData)
        {
            _previousEntry = entry;
        }

        return entry;
    }

    /// <summary>
    /// Asynchronously reads the next entry from the ZIP archive stream.
    /// </summary>
    /// <param name="copyData">
    /// <see langword="true"/> to copy the entry's decompressed data into a <see cref="MemoryStream"/>
    /// that remains valid after the reader advances; <see langword="false"/> to read directly
    /// from the archive stream (invalidated on the next <see cref="GetNextEntryAsync"/> call).
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// The next <see cref="ZipForwardReadEntry"/>, or <see langword="null"/> if there are no more entries.
    /// </returns>
    public async ValueTask<ZipForwardReadEntry?> GetNextEntryAsync(
        bool copyData = false, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_reachedEnd)
        {
            return null;
        }

        await AdvanceDataStreamIfNeededAsync(cancellationToken).ConfigureAwait(false);

        byte[] headerBytes = new byte[ZipLocalFileHeader.SizeOfLocalHeader];
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

        int dynamicLength = GetDynamicHeaderLength(headerBytes);
        byte[] dynamicBuffer = new byte[dynamicLength];
        await _archiveStream.ReadExactlyAsync(dynamicBuffer.AsMemory(0, dynamicLength), cancellationToken).ConfigureAwait(false);

        ParseLocalFileHeader(headerBytes, dynamicBuffer,
            out string fullName, out ushort versionNeeded, out ushort generalPurposeBitFlags,
            out ushort compressionMethod, out DateTimeOffset lastModified, out uint crc32,
            out long compressedSize, out long uncompressedSize, out bool hasDataDescriptor);

        ZipCompressionMethod method = (ZipCompressionMethod)compressionMethod;
        bool isEncrypted = (generalPurposeBitFlags & 1) != 0;

        Stream? dataStream = CreateDataStream(
            method, compressedSize, uncompressedSize, crc32,
            hasDataDescriptor, isEncrypted, out CrcValidatingReadStream? crcStream);

        Stream? originalDataStream = null;
        if (copyData && dataStream is not null)
        {
            originalDataStream = dataStream;
            MemoryStream ms = new();
            await dataStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Position = 0;
            dataStream = ms;
        }

        ZipForwardReadEntry entry = new(
            fullName, method, lastModified, crc32,
            compressedSize, uncompressedSize, generalPurposeBitFlags, versionNeeded,
            hasDataDescriptor, dataStream);

        if (copyData && hasDataDescriptor && crcStream is not null)
        {
            await ReadDataDescriptorAsync(entry, crcStream, cancellationToken).ConfigureAwait(false);
        }

        // Dispose the original decompression/CRC stream after copying (and after
        // reading the data descriptor when applicable) to release inflater resources.
        if (originalDataStream is not null)
        {
            await originalDataStream.DisposeAsync().ConfigureAwait(false);
        }

        if (!copyData)
        {
            _previousEntry = entry;
        }

        return entry;
    }

    private Stream? CreateDataStream(
        ZipCompressionMethod compressionMethod,
        long compressedSize,
        long uncompressedSize,
        uint crc32,
        bool hasDataDescriptor,
        bool isEncrypted,
        out CrcValidatingReadStream? crcStream)
    {
        crcStream = null;

        if (!hasDataDescriptor && compressedSize == 0)
        {
            return null;
        }

        // Encrypted entries cannot be decompressed without decryption.
        // When the compressed size is known (no data descriptor), return a bounded
        // stream so the reader can drain past the encrypted bytes and find the next
        // local file header. When a data descriptor is present the compressed size
        // is unknown, so we cannot determine the entry boundary.
        if (isEncrypted)
        {
            if (hasDataDescriptor)
            {
                throw new NotSupportedException(SR.ZipStreamEncryptedDataDescriptorNotSupported);
            }

            return new BoundedReadOnlyStream(_archiveStream, compressedSize);
        }

        Stream source = hasDataDescriptor
            ? _archiveStream
            : new BoundedReadOnlyStream(_archiveStream, compressedSize);

        Stream decompressed = CreateDecompressionStream(source, compressionMethod, uncompressedSize, leaveOpen: hasDataDescriptor);

        crcStream = hasDataDescriptor
            // Data-descriptor entries: CRC and length are unknown until after the data is read.
            // Use sentinel values to disable validation while still tracking RunningCrc and TotalBytesRead
            // for later verification against the data descriptor.
            ? new CrcValidatingReadStream(decompressed, expectedCrc: 0, expectedLength: long.MaxValue)
            : new CrcValidatingReadStream(decompressed, crc32, uncompressedSize);

        return crcStream;
    }

    /// <summary>
    /// Creates the appropriate decompression stream for the given compression method.
    /// </summary>
    private static Stream CreateDecompressionStream(
        Stream source, ZipCompressionMethod compressionMethod, long uncompressedSize, bool leaveOpen)
    {
        return compressionMethod switch
        {
            ZipCompressionMethod.Deflate when leaveOpen =>
                new DeflateStream(source, CompressionMode.Decompress, leaveOpen: true),
            ZipCompressionMethod.Deflate =>
                new DeflateStream(source, CompressionMode.Decompress, uncompressedSize),
            ZipCompressionMethod.Deflate64 =>
                new DeflateManagedStream(source, ZipCompressionMethod.Deflate64, leaveOpen ? -1 : uncompressedSize),
            ZipCompressionMethod.Stored when leaveOpen =>
                throw new NotSupportedException(SR.ZipStreamStoredDataDescriptorNotSupported),
            ZipCompressionMethod.Stored => source,
            _ => throw new NotSupportedException(SR.UnsupportedCompression)
        };
    }

    private void AdvanceDataStreamIfNeeded()
    {
        if (_previousEntry is null)
        {
            return;
        }

        ZipForwardReadEntry entry = _previousEntry;
        _previousEntry = null;

        DrainStream(entry.DataStream);

        if (entry.HasDataDescriptor && entry.DataStream is CrcValidatingReadStream crcStream)
        {
            ReadDataDescriptor(entry, crcStream);
        }
    }

    private async ValueTask AdvanceDataStreamIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_previousEntry is null)
        {
            return;
        }

        ZipForwardReadEntry entry = _previousEntry;
        _previousEntry = null;

        await DrainStreamAsync(entry.DataStream, cancellationToken).ConfigureAwait(false);

        if (entry.HasDataDescriptor && entry.DataStream is CrcValidatingReadStream crcStream)
        {
            await ReadDataDescriptorAsync(entry, crcStream, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void DrainStream(Stream? stream)
    {
        if (stream is not null)
        {
            stream.CopyTo(Stream.Null);
        }
    }

    private static async ValueTask DrainStreamAsync(Stream? stream, CancellationToken cancellationToken)
    {
        if (stream is not null)
        {
            await stream.CopyToAsync(Stream.Null, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Returns the combined length of the filename and extra field from the fixed local file header,
    /// so the caller can read exactly that many bytes via sync or async I/O.
    /// </summary>
    private static int GetDynamicHeaderLength(ReadOnlySpan<byte> headerBytes)
    {
        ushort filenameLength = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.FilenameLength..]);
        ushort extraFieldLength = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes[ZipLocalFileHeader.FieldLocations.ExtraFieldLength..]);
        return filenameLength + extraFieldLength;
    }

    /// <summary>
    /// Parses all local file header fields from the fixed header bytes and the already-read
    /// dynamic buffer (filename + extra field). This method performs no I/O.
    /// </summary>
    private void ParseLocalFileHeader(
        ReadOnlySpan<byte> headerBytes,
        ReadOnlySpan<byte> dynamicBuffer,
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

        Encoding encoding = (generalPurposeBitFlags & UnicodeFileNameBitFlag) != 0
            ? Encoding.UTF8
            : _entryNameEncoding ?? Encoding.UTF8;

        fullName = encoding.GetString(dynamicBuffer[..filenameLength]);

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

    private void ReadDataDescriptor(ZipForwardReadEntry entry, CrcValidatingReadStream crcStream)
    {
        byte[] buffer = new byte[24];

        _archiveStream.ReadExactly(buffer.AsSpan(0, 4));
        int offset = 0;
        int totalRead = 4;

        if (buffer.AsSpan(0, 4).SequenceEqual(ZipLocalFileHeader.DataDescriptorSignatureConstantBytes))
        {
            offset = 4;
            _archiveStream.ReadExactly(buffer.AsSpan(4, 4));
            totalRead = 8;
        }

        bool isZip64 = entry.VersionNeeded >= (ushort)ZipVersionNeededValues.Zip64;
        int sizesBytes = isZip64 ? 16 : 8;
        _archiveStream.ReadExactly(buffer.AsSpan(totalRead, sizesBytes));

        ParseDataDescriptor(buffer, offset, isZip64, entry, crcStream);
    }

    private async ValueTask ReadDataDescriptorAsync(
        ZipForwardReadEntry entry, CrcValidatingReadStream crcStream, CancellationToken cancellationToken)
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

        ParseDataDescriptor(buffer, offset, isZip64, entry, crcStream);
    }

    /// <summary>
    /// Parses the data descriptor fields from an already-read buffer and updates
    /// the entry with the CRC-32, compressed size, and uncompressed size. No I/O.
    /// </summary>
    private static void ParseDataDescriptor(
        ReadOnlySpan<byte> buffer, int offset, bool isZip64,
        ZipForwardReadEntry entry, CrcValidatingReadStream crcStream)
    {
        uint crc32 = BinaryPrimitives.ReadUInt32LittleEndian(buffer[offset..]);
        int sizesOffset = offset + 4;

        if (isZip64)
        {
            entry.UpdateDataDescriptor(
                crc32,
                compressedLength: BinaryPrimitives.ReadInt64LittleEndian(buffer[sizesOffset..]),
                length: BinaryPrimitives.ReadInt64LittleEndian(buffer[(sizesOffset + 8)..]),
                crcStream.RunningCrc, crcStream.TotalBytesRead);
        }
        else
        {
            entry.UpdateDataDescriptor(
                crc32,
                compressedLength: BinaryPrimitives.ReadUInt32LittleEndian(buffer[sizesOffset..]),
                length: BinaryPrimitives.ReadUInt32LittleEndian(buffer[(sizesOffset + 4)..]),
                crcStream.RunningCrc, crcStream.TotalBytesRead);
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
