// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public partial class ZipArchiveEntry
{
    private ForwardReadEntryStream? _forwardStream;
    private long _forwardCompressedRemaining;
    private uint _forwardCrc;
    // Unlike DeflateStream, DeflateDecoder reports exact input consumption and completion,
    // allowing us to verify that the Deflate stream ends at the declared compressed size.
    private DeflateDecoder? _forwardDecoder;
    // The caller's destination can fill before all compressed input is consumed.
    // Keep the remaining input for the next read rather than discarding or rereading it.
    private byte[]? _forwardInput;
    private int _forwardInputOffset;
    private int _forwardInputCount;
    private long _forwardUncompressedRead;

    internal ZipArchiveEntry(ZipArchive archive, ReadOnlySpan<byte> header, byte[] name)
    {
        _archive = archive;
        _originallyInArchive = true;
        _storedEntryNameBytes = name;
        _fileComment = Array.Empty<byte>();
        _versionToExtract = (ZipVersionNeededValues)BinaryPrimitives.ReadUInt16LittleEndian(header[ZipLocalFileHeader.FieldLocations.VersionNeededToExtract..]);
        _generalPurposeBitFlag = (BitFlagValues)BinaryPrimitives.ReadUInt16LittleEndian(header[ZipLocalFileHeader.FieldLocations.GeneralPurposeBitFlags..]);
        _headerCompressionMethod = (ZipCompressionMethod)BinaryPrimitives.ReadUInt16LittleEndian(header[ZipLocalFileHeader.FieldLocations.CompressionMethod..]);
        CompressionMethod = _headerCompressionMethod;
        _storedEntryName = DecodeEntryString(name);
        _lastModified = new DateTimeOffset(ZipHelper.DosTimeToDateTime(BinaryPrimitives.ReadUInt32LittleEndian(header[ZipLocalFileHeader.FieldLocations.LastModified..])));
        _crc32 = BinaryPrimitives.ReadUInt32LittleEndian(header[ZipLocalFileHeader.FieldLocations.Crc32..]);
        _compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(header[ZipLocalFileHeader.FieldLocations.CompressedSize..]);
        _uncompressedSize = BinaryPrimitives.ReadUInt32LittleEndian(header[ZipLocalFileHeader.FieldLocations.UncompressedSize..]);

        // Bits 1 and 2 describe Deflate's compression level, but are not valid for Stored.
        BitFlagValues allowedFlags = BitFlagValues.UnicodeFileNameAndComment;
        if (CompressionMethod == ZipCompressionMethod.Deflate)
        {
            allowedFlags |= (BitFlagValues)0x6;
        }
        if ((_generalPurposeBitFlag & ~allowedFlags) != 0 ||
            CompressionMethod is not (ZipCompressionMethod.Stored or ZipCompressionMethod.Deflate) ||
            _compressedSize == uint.MaxValue || _uncompressedSize == uint.MaxValue)
        {
            throw new NotSupportedException(SR.ForwardReadUnsupportedEntry);
        }
        if (CompressionMethod == ZipCompressionMethod.Stored && _compressedSize != _uncompressedSize)
        {
            throw new InvalidDataException(SR.UnexpectedStreamLength);
        }
        _compressionLevel = MapCompressionLevel(_generalPurposeBitFlag, CompressionMethod);
        _forwardCompressedRemaining = _compressedSize;
    }

    private Stream OpenForwardRead()
    {
        _archive.EnsureCurrentForwardEntry(this);
        if (_forwardStream is not null)
        {
            throw new InvalidOperationException(SR.ForwardReadEntryAlreadyOpened);
        }
        if (CompressionMethod == ZipCompressionMethod.Deflate)
        {
            // Opening allocates decoder state only; input is read by Read/ReadAsync.
            _forwardInput = new byte[4096];
            _forwardDecoder = new DeflateDecoder();
        }
        return _forwardStream = new ForwardReadEntryStream(this);
    }

    internal async ValueTask DrainForwardEntryAsync(Memory<byte> buffer, bool useAsync, CancellationToken cancellationToken)
    {
        // The entry owns the decoder even after its public stream is disposed.
        // Unopened entries have no decoder and skip the raw compressed payload.
        while ((useAsync
            ? await ReadForwardCoreAsync(buffer, cancellationToken).ConfigureAwait(false)
            : ReadForwardCore(buffer.Span)) != 0)
        {
        }
    }

    private int ReadForwardCore(Span<byte> buffer)
    {
        if (buffer.IsEmpty)
        {
            return 0;
        }
        if (_forwardDecoder is not null)
        {
            int written;
            while (!TryReadForwardDeflate(buffer, out written))
            {
                int compressedRead = _archive.ArchiveStream.Read(
                    _forwardInput.AsSpan(0, (int)Math.Min(_forwardInput!.Length, _forwardCompressedRemaining)));
                AccountForwardInput(compressedRead);
            }
            return written;
        }
        if (_forwardCompressedRemaining == 0)
        {
            ValidateForwardCrc();
            return 0;
        }
        int read = _archive.ArchiveStream.Read(buffer[..(int)Math.Min(buffer.Length, _forwardCompressedRemaining)]);
        AccountForwardBytes(buffer[..read]);
        return read;
    }

    private async ValueTask<int> ReadForwardCoreAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (buffer.IsEmpty)
        {
            return 0;
        }
        if (_forwardDecoder is not null)
        {
            int written;
            while (!TryReadForwardDeflate(buffer.Span, out written))
            {
                int compressedRead = await _archive.ArchiveStream.ReadAsync(
                    _forwardInput.AsMemory(0, (int)Math.Min(_forwardInput!.Length, _forwardCompressedRemaining)),
                    cancellationToken).ConfigureAwait(false);
                AccountForwardInput(compressedRead);
            }
            return written;
        }
        if (_forwardCompressedRemaining == 0)
        {
            ValidateForwardCrc();
            return 0;
        }
        int read = await _archive.ArchiveStream.ReadAsync(buffer[..(int)Math.Min(buffer.Length, _forwardCompressedRemaining)], cancellationToken).ConfigureAwait(false);
        AccountForwardBytes(buffer.Span[..read]);
        return read;
    }

    private void AccountForwardBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            throw new InvalidDataException(SR.UnexpectedStreamLength);
        }
        _forwardCompressedRemaining -= bytes.Length;
        if (_forwardStream is not null)
        {
            _forwardCrc = Crc32Helper.UpdateCrc32(_forwardCrc, bytes);
        }
    }

    private void AccountForwardInput(int read)
    {
        if (read == 0)
        {
            throw new InvalidDataException(SR.UnexpectedStreamLength);
        }
        _forwardCompressedRemaining -= read;
        _forwardInputOffset = 0;
        _forwardInputCount = read;
    }

    // Returns false only when another bounded input read is needed.
    private bool TryReadForwardDeflate(Span<byte> buffer, out int written)
    {
        Debug.Assert(_forwardDecoder is not null && !buffer.IsEmpty);
        while (true)
        {
            // Try even with empty input: a full previous destination may have left buffered output.
            OperationStatus status = _forwardDecoder.Decompress(
                _forwardInput.AsSpan(_forwardInputOffset, _forwardInputCount), buffer, out int consumed, out written);
            _forwardInputOffset += consumed;
            _forwardInputCount -= consumed;
            if (status == OperationStatus.InvalidData)
            {
                throw new InvalidDataException(SR.GenericInvalidData);
            }
            _forwardUncompressedRead += written;
            if (_forwardUncompressedRead > _uncompressedSize ||
                (status == OperationStatus.Done && (_forwardCompressedRemaining != 0 || _forwardInputCount != 0)))
            {
                // Completion must consume exactly the declared payload, without trailing junk.
                throw new InvalidDataException(SR.UnexpectedStreamLength);
            }
            _forwardCrc = Crc32Helper.UpdateCrc32(_forwardCrc, buffer[..written]);
            if (written != 0)
            {
                return true;
            }
            if (status == OperationStatus.Done)
            {
                if (_forwardUncompressedRead != _uncompressedSize)
                {
                    throw new InvalidDataException(SR.UnexpectedStreamLength);
                }
                ValidateForwardCrc();
                return true;
            }
            if (consumed != 0)
            {
                continue;
            }
            if (_forwardInputCount != 0 || _forwardCompressedRemaining == 0)
            {
                // Producing all expected bytes is insufficient without the Deflate end marker.
                throw new InvalidDataException(SR.UnexpectedStreamLength);
            }
            return false;
        }
    }

    private void ValidateForwardCrc()
    {
        if (_forwardStream is not null && _forwardCrc != _crc32)
        {
            throw new InvalidDataException(SR.CrcMismatch);
        }
    }

    internal void InvalidateForwardEntry()
    {
        _forwardStream?.Invalidate();
        _forwardDecoder?.Dispose();
        _forwardDecoder = null;
        _forwardInput = null;
    }

    private sealed class ForwardReadEntryStream(ZipArchiveEntry entry) : Stream
    {
        private bool _disposed;
        private bool _invalidated;

        internal void Invalidate() => _invalidated = true;

        public override bool CanRead => !_disposed && !_invalidated;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw CreateUnsupportedOperationException(SR.SeekingNotSupported);
        public override long Position { get => throw CreateUnsupportedOperationException(SR.SeekingNotSupported); set => throw CreateUnsupportedOperationException(SR.SeekingNotSupported); }

        private void ThrowIfCantRead()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_invalidated)
            {
                throw new NotSupportedException(SR.ReadingNotSupported);
            }
        }

        private NotSupportedException CreateUnsupportedOperationException(string message)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return new NotSupportedException(message);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfCantRead();
            ValidateBufferArguments(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        public override int ReadByte()
        {
            byte value = default;
            return Read(new Span<byte>(ref value)) == 0 ? -1 : value;
        }

        public override int Read(Span<byte> buffer)
        {
            ThrowIfCantRead();
            try
            {
                return entry.ReadForwardCore(buffer);
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataException(SR.UnexpectedStreamLength, e);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfCantRead();
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfCantRead();
            return ReadAsyncCore(buffer, cancellationToken);
        }

        private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await entry.ReadForwardCoreAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataException(SR.UnexpectedStreamLength, e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Caller disposal must not prevent the archive from draining this entry.
            _disposed = true;
            base.Dispose(disposing);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override int EndRead(IAsyncResult asyncResult) => TaskToAsyncResult.End<int>(asyncResult);

        public override void Flush() => throw CreateUnsupportedOperationException(SR.WritingNotSupported);
        public override Task FlushAsync(CancellationToken cancellationToken) => throw CreateUnsupportedOperationException(SR.WritingNotSupported);
        public override long Seek(long offset, SeekOrigin origin) => throw CreateUnsupportedOperationException(SR.SeekingNotSupported);
        public override void SetLength(long value) => throw CreateUnsupportedOperationException(SR.SeekingNotSupported);
        public override void Write(byte[] buffer, int offset, int count) => throw CreateUnsupportedOperationException(SR.WritingNotSupported);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw CreateUnsupportedOperationException(SR.WritingNotSupported);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw CreateUnsupportedOperationException(SR.WritingNotSupported);
    }
}
