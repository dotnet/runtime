// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public sealed class ZipStreamEntry
{
    private readonly Stream? _archiveStream;
    private readonly bool _hasDataDescriptor;
    private readonly BoundedReadOnlyStream? _boundedStream;
    private Stream? _decompressionStream;
    private uint _crc32;
    private long _compressedLength;
    private long _length;

    internal ZipStreamEntry(
        string fullName,
        ZipCompressionMethod compressionMethod,
        DateTimeOffset lastModified,
        uint crc32,
        long compressedLength,
        long length,
        ushort generalPurposeBitFlags,
        ushort versionNeeded,
        Stream? archiveStream,
        bool hasDataDescriptor)
    {
        FullName = fullName;
        CompressionMethod = compressionMethod;
        LastModified = lastModified;
        _crc32 = crc32;
        _compressedLength = compressedLength;
        _length = length;
        GeneralPurposeBitFlags = generalPurposeBitFlags;
        VersionNeeded = versionNeeded;
        _hasDataDescriptor = hasDataDescriptor;

        if (archiveStream is not null)
        {
            if (hasDataDescriptor)
            {
                _archiveStream = archiveStream;
            }
            else
            {
                _boundedStream = new BoundedReadOnlyStream(archiveStream, compressedLength);
            }
        }
    }

    /// <summary>
    /// Gets the full name (relative path) of the entry, including any directory path.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the file name portion of the entry (the part after the last directory separator).
    /// </summary>
    public string Name => Path.GetFileName(FullName);

    /// <summary>
    /// Gets the compression method used for this entry.
    /// </summary>
    public ZipCompressionMethod CompressionMethod { get; }

    /// <summary>
    /// Gets the last modification date and time of the entry.
    /// </summary>
    public DateTimeOffset LastModified { get; }

    /// <summary>
    /// Gets the CRC-32 checksum of the uncompressed data.
    /// </summary>
    /// <remarks>
    /// When bit 3 (data descriptor) is set in the local header, this value is initially
    /// zero and is populated after the compressed data has been fully read.
    /// </remarks>
    [CLSCompliant(false)]
    public uint Crc32 => _crc32;

    /// <summary>
    /// Gets the compressed size of the entry in bytes.
    /// </summary>
    /// <remarks>
    /// When bit 3 (data descriptor) is set in the local header, this value is initially
    /// zero and is populated after the compressed data has been fully read.
    /// </remarks>
    public long CompressedLength => _compressedLength;

    /// <summary>
    /// Gets the uncompressed size of the entry in bytes.
    /// </summary>
    /// <remarks>
    /// When bit 3 (data descriptor) is set in the local header, this value is initially
    /// zero and is populated after the compressed data has been fully read.
    /// </remarks>
    public long Length => _length;

    /// <summary>
    /// Gets the raw general purpose bit flags from the local file header.
    /// </summary>
    [CLSCompliant(false)]
    public ushort GeneralPurposeBitFlags { get; }

    /// <summary>
    /// Gets a value indicating whether the entry is encrypted.
    /// </summary>
    public bool IsEncrypted => (GeneralPurposeBitFlags & 1) != 0;

    /// <summary>
    /// Gets a value indicating whether the entry represents a directory.
    /// </summary>
    public bool IsDirectory => FullName.Length > 0 && (FullName[^1] is '/' or '\\');

    /// <summary>
    /// Gets the minimum ZIP specification version needed to extract this entry.
    /// </summary>
    [CLSCompliant(false)]
    public ushort VersionNeeded { get; }

    /// <summary>
    /// Reads decompressed data from this entry into the provided buffer.
    /// The data is transparently decompressed based on the entry's compression method.
    /// </summary>
    /// <param name="buffer">The buffer to read decompressed data into.</param>
    /// <returns>The number of bytes read, or 0 if all data has been consumed.</returns>
    /// <exception cref="NotSupportedException">
    /// The entry uses an unsupported compression method, or is a Stored entry with a data descriptor.
    /// </exception>
    public int Read(Span<byte> buffer)
    {
        Stream stream = GetOrCreateDecompressionStream();

        return stream.Read(buffer);
    }

    /// <summary>
    /// Asynchronously reads decompressed data from this entry into the provided buffer.
    /// The data is transparently decompressed based on the entry's compression method.
    /// </summary>
    /// <param name="buffer">The buffer to read decompressed data into.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of bytes read, or 0 if all data has been consumed.</returns>
    /// <exception cref="NotSupportedException">
    /// The entry uses an unsupported compression method, or is a Stored entry with a data descriptor.
    /// </exception>
    public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Stream stream = GetOrCreateDecompressionStream();

        return await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    private Stream GetOrCreateDecompressionStream()
    {
        if (_decompressionStream is not null)
        {
            return _decompressionStream;
        }

        if (_hasDataDescriptor)
        {
            // Data descriptor entries have unknown compressed size in the local header.
            // Deflate/Deflate64 streams are self-terminating, so they can be decompressed
            // without knowing the compressed size. Stored data has no termination marker,
            // so it cannot be decompressed without the size.
            Debug.Assert(_archiveStream is not null);

            _decompressionStream = CompressionMethod switch
            {
                ZipCompressionMethod.Deflate => new DeflateStream(_archiveStream, CompressionMode.Decompress, leaveOpen: true),
                ZipCompressionMethod.Deflate64 => new DeflateManagedStream(_archiveStream, ZipCompressionMethod.Deflate64, uncompressedSize: -1),
                ZipCompressionMethod.Stored => throw new NotSupportedException(SR.ZipStreamStoredDataDescriptorNotSupported),
                _ => throw new NotSupportedException(SR.UnsupportedCompression)
            };
        }
        else if (_boundedStream is not null)
        {
            _decompressionStream = CompressionMethod switch
            {
                ZipCompressionMethod.Deflate => new DeflateStream(_boundedStream, CompressionMode.Decompress, _length),
                ZipCompressionMethod.Deflate64 => new DeflateManagedStream(_boundedStream, ZipCompressionMethod.Deflate64, _length),
                ZipCompressionMethod.Stored => _boundedStream,
                _ => throw new NotSupportedException(SR.UnsupportedCompression)
            };
        }
        else
        {
            // Entry has no data (e.g. empty file or directory).
            _decompressionStream = Stream.Null;
        }

        return _decompressionStream;
    }

    internal bool HasDataDescriptor => _hasDataDescriptor;

    internal void SkipCompressedData()
    {
        // For known-size entries, drain the bounded stream to advance the archive
        // past remaining compressed bytes. For data descriptor entries, drain the
        // decompression stream which detects the end of the self-terminating format.
        Stream? streamToDrain = _boundedStream;

        if (streamToDrain is null && _hasDataDescriptor && _archiveStream is not null)
        {
            streamToDrain = GetOrCreateDecompressionStream();
        }

        if (streamToDrain is null)
        {
            return;
        }

        byte[] skipBuffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            while (streamToDrain.Read(skipBuffer) > 0) { }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(skipBuffer);
        }
    }

    internal async ValueTask SkipCompressedDataAsync(CancellationToken cancellationToken)
    {
        Stream? streamToDrain = _boundedStream;

        if (streamToDrain is null && _hasDataDescriptor && _archiveStream is not null)
        {
            streamToDrain = GetOrCreateDecompressionStream();
        }

        if (streamToDrain is null)
        {
            return;
        }

        byte[] skipBuffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            while (await streamToDrain.ReadAsync(skipBuffer.AsMemory(), cancellationToken).ConfigureAwait(false) > 0) { }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(skipBuffer);
        }
    }

    internal void UpdateDataDescriptor(uint crc32, long compressedLength, long length)
    {
        _crc32 = crc32;
        _compressedLength = compressedLength;
        _length = length;
    }

    /// <summary>
    /// A read-only, forward-only stream that limits the number of bytes
    /// that can be read from an underlying stream without closing it.
    /// </summary>
    private sealed class BoundedReadOnlyStream : Stream
    {
        private readonly Stream _baseStream;
        private long _remaining;

        public BoundedReadOnlyStream(Stream baseStream, long length)
        {
            _baseStream = baseStream;
            _remaining = length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (_remaining <= 0)
            {
                return 0;
            }

            if (buffer.Length > _remaining)
            {
                buffer = buffer.Slice(0, (int)_remaining);
            }

            int bytesRead = _baseStream.Read(buffer);
            _remaining -= bytesRead;

            return bytesRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_remaining <= 0)
            {
                return new ValueTask<int>(0);
            }

            if (buffer.Length > _remaining)
            {
                buffer = buffer.Slice(0, (int)_remaining);
            }

            return ReadAsyncCore(buffer, cancellationToken);
        }

        private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int bytesRead = await _baseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            _remaining -= bytesRead;

            return bytesRead;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _baseStream.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
