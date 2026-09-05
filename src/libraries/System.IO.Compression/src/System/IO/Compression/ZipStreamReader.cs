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
/// <para>
/// Once enumeration completes (that is, once a <c>GetNextEntry</c> call returns
/// <see langword="null"/>), the reader has consumed into the archive's trailing
/// central directory, so the position of the underlying stream is unspecified.
/// Reading additional data from the stream after enumeration ends is not supported.
/// </para>
/// </remarks>
public sealed class ZipStreamReader : IDisposable, IAsyncDisposable
{
    private const ushort DataDescriptorBitFlag = 0x8;
    private const ushort UnicodeFileNameBitFlag = 0x800;
    private const ushort EncryptionBitFlag = 0x1;
    private const ushort StrongEncryptionBitFlag = 0x40;

    private bool _isDisposed;
    private readonly bool _leaveOpen;
    private readonly Encoding? _entryNameEncoding;
    private ZipArchiveEntry? _previousEntry;
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
    /// The next <see cref="ZipArchiveEntry"/>, or <see langword="null"/> if there are no more entries.
    /// When <see langword="null"/> is returned, the underlying stream has been consumed into the
    /// trailing central directory and its position is unspecified.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
    /// <exception cref="InvalidDataException">The archive stream contains invalid data.</exception>
    public ZipArchiveEntry? GetNextEntry(bool copyData = false)
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

            // A clean end of the archive is either no more bytes at all, or a short trailing
            // end-of-entries record such as the 22-byte end-of-central-directory of an empty
            // archive. Any other non-empty partial read means the stream ended in the middle of
            // a local file header, i.e. the archive is truncated.
            if (bytesRead != 0 &&
                !(bytesRead >= sizeof(uint) && IsKnownEndOfEntriesSignature(headerBytes)))
            {
                throw new InvalidDataException(SR.UnexpectedEndOfStream);
            }

            return null;
        }

        if (!headerBytes.AsSpan().StartsWith(ZipLocalFileHeader.SignatureConstantBytes))
        {
            if (IsKnownEndOfEntriesSignature(headerBytes))
            {
                _reachedEnd = true;
                return null;
            }

            throw new InvalidDataException(SR.ZipStreamInvalidLocalFileHeader);
        }

        int dynamicLength = GetDynamicHeaderLength(headerBytes);
        byte[] dynamicBuffer = new byte[dynamicLength];
        _archiveStream.ReadExactly(dynamicBuffer);

        ParseLocalFileHeader(headerBytes, dynamicBuffer,
            out string fullName, out byte[] fullNameBytes, out ushort versionNeeded, out ushort generalPurposeBitFlags,
            out ushort compressionMethod, out DateTimeOffset lastModified, out uint crc32,
            out long compressedSize, out long uncompressedSize, out bool hasDataDescriptor,
            out ZipEncryptionMethod encryptionMethod, out ushort realCompressionMethod, out ushort aeVersion);

        bool isEncrypted = (generalPurposeBitFlags & EncryptionBitFlag) != 0;

        ZipCompressionMethod method = (ZipCompressionMethod)compressionMethod;
        ZipCompressionMethod realMethod = (ZipCompressionMethod)realCompressionMethod;

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

        ZipArchiveEntry entry = new(
            fullName, fullNameBytes, realMethod, lastModified, crc32,
            compressedSize, uncompressedSize, generalPurposeBitFlags, versionNeeded, dataStream,
            encryptionMethod, method, aeVersion);

        if (copyData && hasDataDescriptor)
        {
            if (crcStream is not null)
            {
                ReadDataDescriptor(entry, crcStream);
            }
            else if (isEncrypted)
            {
                SkipDataDescriptor();
            }
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
    /// The next <see cref="ZipArchiveEntry"/>, or <see langword="null"/> if there are no more entries.
    /// When <see langword="null"/> is returned, the underlying stream has been consumed into the
    /// trailing central directory and its position is unspecified.
    /// </returns>
    public async ValueTask<ZipArchiveEntry?> GetNextEntryAsync(
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

            // A clean end of the archive is either no more bytes at all, or a short trailing
            // end-of-entries record such as the 22-byte end-of-central-directory of an empty
            // archive. Any other non-empty partial read means the stream ended in the middle of
            // a local file header, i.e. the archive is truncated.
            if (bytesRead != 0 &&
                !(bytesRead >= sizeof(uint) && IsKnownEndOfEntriesSignature(headerBytes)))
            {
                throw new InvalidDataException(SR.UnexpectedEndOfStream);
            }

            return null;
        }

        if (!headerBytes.AsSpan().StartsWith(ZipLocalFileHeader.SignatureConstantBytes))
        {
            if (IsKnownEndOfEntriesSignature(headerBytes))
            {
                _reachedEnd = true;
                return null;
            }

            throw new InvalidDataException(SR.ZipStreamInvalidLocalFileHeader);
        }

        int dynamicLength = GetDynamicHeaderLength(headerBytes);
        byte[] dynamicBuffer = new byte[dynamicLength];
        await _archiveStream.ReadExactlyAsync(dynamicBuffer.AsMemory(0, dynamicLength), cancellationToken).ConfigureAwait(false);

        ParseLocalFileHeader(headerBytes, dynamicBuffer,
            out string fullName, out byte[] fullNameBytes, out ushort versionNeeded, out ushort generalPurposeBitFlags,
            out ushort compressionMethod, out DateTimeOffset lastModified, out uint crc32,
            out long compressedSize, out long uncompressedSize, out bool hasDataDescriptor,
            out ZipEncryptionMethod encryptionMethod, out ushort realCompressionMethod, out ushort aeVersion);

        ZipCompressionMethod method = (ZipCompressionMethod)compressionMethod;
        ZipCompressionMethod realMethod = (ZipCompressionMethod)realCompressionMethod;
        bool isEncrypted = (generalPurposeBitFlags & EncryptionBitFlag) != 0;

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

        ZipArchiveEntry entry = new(
            fullName, fullNameBytes, realMethod, lastModified, crc32,
            compressedSize, uncompressedSize, generalPurposeBitFlags, versionNeeded, dataStream,
            encryptionMethod, method, aeVersion);

        if (copyData && hasDataDescriptor)
        {
            if (crcStream is not null)
            {
                await ReadDataDescriptorAsync(entry, crcStream, cancellationToken).ConfigureAwait(false);
            }
            else if (isEncrypted)
            {
                await SkipDataDescriptorAsync(cancellationToken).ConfigureAwait(false);
            }
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

        // Encrypted entries cannot be decompressed without decryption, which requires a password that
        // is not available until the entry is opened. As long as the compressed size is known (present
        // in the local header), the raw bytes can still be bounded so the reader can drain past them and
        // decrypt them later. Only when the compressed size is unknown (a streamed data-descriptor entry
        // whose local header stores a zero size) can the entry boundary not be determined.
        if (isEncrypted)
        {
            if (compressedSize == 0)
            {
                throw new NotSupportedException(SR.ZipStreamEncryptedDataDescriptorNotSupported);
            }

            return CreateBoundedStream(compressedSize);
        }

        Stream source = hasDataDescriptor
            ? _archiveStream
            : CreateBoundedStream(compressedSize);

        Stream decompressed = CreateDecompressionStream(source, compressionMethod, uncompressedSize, leaveOpen: hasDataDescriptor);

        crcStream = hasDataDescriptor
            // Data-descriptor entries: CRC and length are unknown until after the data is read.
            // Use sentinel values to disable validation while still tracking RunningCrc and TotalBytesRead
            // for later verification against the data descriptor.
            ? new CrcValidatingReadStream(decompressed, expectedCrc: 0, expectedLength: long.MaxValue)
            : new CrcValidatingReadStream(decompressed, crc32, uncompressedSize);

        return crcStream;
    }

    // Bounds forward reads to exactly compressedSize bytes so the decompressor (or the
    // encrypted-drain path) cannot read past the entry into the next local file header.
    // SubReadStream supports non-seekable super streams; anchoring the window at the
    // current position keeps its internal position in lockstep with _archiveStream so it
    // never issues a SeekOrigin.Begin (which ReadAheadStream does not support) on a
    // non-seekable source.
    private SubReadStream CreateBoundedStream(long compressedSize)
        => new SubReadStream(_archiveStream, _archiveStream.Position, compressedSize);

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

        ZipArchiveEntry entry = _previousEntry;
        _previousEntry = null;

        DrainStream(entry.ForwardDataStream);

        if (entry.HasDataDescriptor)
        {
            if (entry.ForwardDataStream is CrcValidatingReadStream crcStream)
            {
                ReadDataDescriptor(entry, crcStream);
            }
            else
            {
                // Encrypted entries expose a raw bounded stream with no CRC tracking, so the trailing
                // data descriptor is skipped without validation to reach the next local file header.
                SkipDataDescriptor();
            }
        }

        // The forward-only stream is invalidated once the reader advances, so dispose it here to
        // release the inflater and other native resources instead of waiting for finalization.
        entry.ForwardDataStream?.Dispose();
    }

    private async ValueTask AdvanceDataStreamIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_previousEntry is null)
        {
            return;
        }

        ZipArchiveEntry entry = _previousEntry;
        _previousEntry = null;

        await DrainStreamAsync(entry.ForwardDataStream, cancellationToken).ConfigureAwait(false);

        if (entry.HasDataDescriptor)
        {
            if (entry.ForwardDataStream is CrcValidatingReadStream crcStream)
            {
                await ReadDataDescriptorAsync(entry, crcStream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Encrypted entries expose a raw bounded stream with no CRC tracking, so the trailing
                // data descriptor is skipped without validation to reach the next local file header.
                await SkipDataDescriptorAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        // The forward-only stream is invalidated once the reader advances, so dispose it here to
        // release the inflater and other native resources instead of waiting for finalization.
        if (entry.ForwardDataStream is not null)
        {
            await entry.ForwardDataStream.DisposeAsync().ConfigureAwait(false);
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
        out byte[] fullNameBytes,
        out ushort versionNeeded,
        out ushort generalPurposeBitFlags,
        out ushort compressionMethod,
        out DateTimeOffset lastModified,
        out uint crc32,
        out long compressedSize,
        out long uncompressedSize,
        out bool hasDataDescriptor,
        out ZipEncryptionMethod encryptionMethod,
        out ushort realCompressionMethod,
        out ushort aeVersion)
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

        fullNameBytes = dynamicBuffer[..filenameLength].ToArray();
        fullName = encoding.GetString(dynamicBuffer[..filenameLength]);

        ReadOnlySpan<byte> extraField = dynamicBuffer.Slice(filenameLength, extraFieldLength);

        ResolveEncryption(generalPurposeBitFlags, compressionMethod, extraField,
            out encryptionMethod, out realCompressionMethod, out aeVersion);

        bool compressedSizeInZip64 = compressedSizeSmall == ZipHelper.Mask32Bit;
        bool uncompressedSizeInZip64 = uncompressedSizeSmall == ZipHelper.Mask32Bit;

        if (compressedSizeInZip64 || uncompressedSizeInZip64)
        {
            Zip64ExtraField zip64 = Zip64ExtraField.GetJustZip64Block(
                extraField,
                readUncompressedSize: uncompressedSizeInZip64,
                readCompressedSize: compressedSizeInZip64,
                readLocalHeaderOffset: false,
                readStartDiskNumber: false,
                isInLocalHeader: true);

            compressedSize = zip64.CompressedSize ?? compressedSizeSmall;
            uncompressedSize = zip64.UncompressedSize ?? uncompressedSizeSmall;
        }
        else
        {
            compressedSize = compressedSizeSmall;
            uncompressedSize = uncompressedSizeSmall;
        }
    }

    // Determines the entry's encryption method from the general-purpose bit flag and, for WinZip AES
    // entries, the local extra field (0x9901). The AES extra field also carries the *real* compression
    // method (the local header stores 99 for AES) and the AE version, both required to decrypt and
    // decompress the entry through the standard ZipArchiveEntry.Open(password) pipeline.
    private static void ResolveEncryption(
        ushort generalPurposeBitFlags,
        ushort compressionMethod,
        ReadOnlySpan<byte> extraField,
        out ZipEncryptionMethod encryptionMethod,
        out ushort realCompressionMethod,
        out ushort aeVersion)
    {
        realCompressionMethod = compressionMethod;
        aeVersion = 0;

        if ((generalPurposeBitFlags & EncryptionBitFlag) == 0)
        {
            encryptionMethod = ZipEncryptionMethod.None;
            return;
        }

        if (compressionMethod == ZipArchiveEntry.WinZipAesMethod &&
            WinZipAesExtraField.TryGetFromRawExtraFieldData(extraField, out WinZipAesExtraField aesField))
        {
            realCompressionMethod = aesField.CompressionMethod;
            aeVersion = aesField.VendorVersion;
            encryptionMethod = aesField.AesStrength switch
            {
                1 => ZipEncryptionMethod.Aes128,
                2 => ZipEncryptionMethod.Aes192,
                3 => ZipEncryptionMethod.Aes256,
                _ => throw new InvalidDataException(SR.InvalidAesStrength)
            };
            return;
        }

        // Encrypted but not AES: the strong-encryption flag marks an unsupported scheme; otherwise ZipCrypto.
        encryptionMethod = (generalPurposeBitFlags & StrongEncryptionBitFlag) != 0
            ? ZipEncryptionMethod.Unknown
            : ZipEncryptionMethod.ZipCrypto;
    }

    private void ReadDataDescriptor(ZipArchiveEntry entry, CrcValidatingReadStream crcStream)
    {
        (byte[] buffer, int offset, bool isZip64) = ReadDataDescriptorCore();
        ParseDataDescriptor(buffer, offset, isZip64, entry, crcStream);
    }

    // Advances the archive stream past a trailing data descriptor without parsing or validating it.
    // Used for encrypted entries whose data was drained raw (no CRC stream to validate against).
    private void SkipDataDescriptor() => ReadDataDescriptorCore();

    // Reads a trailing data descriptor into a buffer and leaves the archive stream positioned at the
    // start of the next record. Returns the buffer along with the payload offset (past an optional
    // signature) and whether the descriptor uses 64-bit sizes.
    private (byte[] buffer, int offset, bool isZip64) ReadDataDescriptorCore()
    {
        byte[] buffer = new byte[28]; // Max: sig(4) + crc(4) + sizes64(16) + peek(4)

        _archiveStream.ReadExactly(buffer.AsSpan(0, 4));
        int offset = 0;
        int totalRead = 4;

        if (buffer.AsSpan(0, 4).SequenceEqual(ZipLocalFileHeader.DataDescriptorSignatureConstantBytes))
        {
            offset = 4;
            _archiveStream.ReadExactly(buffer.AsSpan(4, 4));
            totalRead = 8;
        }

        // Read 20 bytes: up to 16 for sizes (64-bit) + 4 to peek at the next signature.
        _archiveStream.ReadExactly(buffer.AsSpan(totalRead, 20));

        // Probe: if 4 bytes after 32-bit sizes form a known ZIP signature,
        // the descriptor uses 32-bit sizes; otherwise assume 64-bit.
        bool isZip64 = !IsKnownZipSignature(buffer.AsSpan(totalRead + 8, 4));
        int sizesBytes = isZip64 ? 16 : 8;

        // Seek back over the bytes we read past the actual sizes.
        int overRead = 20 - sizesBytes;
        _archiveStream.Seek(-overRead, SeekOrigin.Current);

        return (buffer, offset, isZip64);
    }

    private async ValueTask ReadDataDescriptorAsync(
        ZipArchiveEntry entry, CrcValidatingReadStream crcStream, CancellationToken cancellationToken)
    {
        (byte[] buffer, int offset, bool isZip64) = await ReadDataDescriptorCoreAsync(cancellationToken).ConfigureAwait(false);
        ParseDataDescriptor(buffer, offset, isZip64, entry, crcStream);
    }

    private async ValueTask SkipDataDescriptorAsync(CancellationToken cancellationToken) =>
        await ReadDataDescriptorCoreAsync(cancellationToken).ConfigureAwait(false);

    private async ValueTask<(byte[] buffer, int offset, bool isZip64)> ReadDataDescriptorCoreAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[28]; // Max: sig(4) + crc(4) + sizes64(16) + peek(4)

        await _archiveStream.ReadExactlyAsync(buffer.AsMemory(0, 4), cancellationToken).ConfigureAwait(false);
        int offset = 0;
        int totalRead = 4;

        if (buffer.AsSpan(0, 4).SequenceEqual(ZipLocalFileHeader.DataDescriptorSignatureConstantBytes))
        {
            offset = 4;
            await _archiveStream.ReadExactlyAsync(buffer.AsMemory(4, 4), cancellationToken).ConfigureAwait(false);
            totalRead = 8;
        }

        // Read 20 bytes: up to 16 for sizes (64-bit) + 4 to peek at the next signature.
        await _archiveStream.ReadExactlyAsync(buffer.AsMemory(totalRead, 20), cancellationToken).ConfigureAwait(false);

        // Probe: if 4 bytes after 32-bit sizes form a known ZIP signature,
        // the descriptor uses 32-bit sizes; otherwise assume 64-bit.
        bool isZip64 = !IsKnownZipSignature(buffer.AsSpan(totalRead + 8, 4));
        int sizesBytes = isZip64 ? 16 : 8;

        // Seek back over the bytes we read past the actual sizes.
        int overRead = 20 - sizesBytes;
        _archiveStream.Seek(-overRead, SeekOrigin.Current);

        return (buffer, offset, isZip64);
    }

    /// <summary>
    /// Parses the data descriptor fields from an already-read buffer and updates
    /// the entry with the CRC-32, compressed size, and uncompressed size. No I/O.
    /// </summary>
    private static void ParseDataDescriptor(
        ReadOnlySpan<byte> buffer, int offset, bool isZip64,
        ZipArchiveEntry entry, CrcValidatingReadStream crcStream)
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

    /// <summary>
    /// Returns <see langword="true"/> when the first four bytes of
    /// <paramref name="headerBytes"/> match a known end-of-entries signature
    /// (central directory header or end-of-central-directory).
    /// </summary>
    private static bool IsKnownEndOfEntriesSignature(ReadOnlySpan<byte> headerBytes)
    {
        ReadOnlySpan<byte> sig = headerBytes[..4];
        return sig.SequenceEqual(ZipCentralDirectoryFileHeader.SignatureConstantBytes)
            || sig.SequenceEqual(ZipEndOfCentralDirectoryBlock.SignatureConstantBytes)
            || sig.SequenceEqual(Zip64EndOfCentralDirectoryRecord.SignatureConstantBytes);
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="bytes"/> starts with any
    /// recognized ZIP structure signature (local header, central directory, EOCD, or ZIP64 EOCD).
    /// Used to probe the data descriptor format by peeking at the bytes that follow.
    /// </summary>
    private static bool IsKnownZipSignature(ReadOnlySpan<byte> bytes)
    {
        return bytes.StartsWith(ZipLocalFileHeader.SignatureConstantBytes)
            || bytes.StartsWith(ZipCentralDirectoryFileHeader.SignatureConstantBytes)
            || bytes.StartsWith(ZipEndOfCentralDirectoryBlock.SignatureConstantBytes)
            || bytes.StartsWith(Zip64EndOfCentralDirectoryRecord.SignatureConstantBytes);
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
