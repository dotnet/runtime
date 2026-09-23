// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public partial class ZipArchiveEntry
{
    private ForwardReadEntryStream? _forwardStream;
    private long _forwardRemaining;
    private uint _forwardCrc;

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

        // Stored entries need no option flags other than the UTF-8 name marker.
        if ((_generalPurposeBitFlag & ~BitFlagValues.UnicodeFileNameAndComment) != 0 ||
            CompressionMethod != ZipCompressionMethod.Stored ||
            _compressedSize == uint.MaxValue || _uncompressedSize == uint.MaxValue)
        {
            throw new NotSupportedException(SR.ForwardReadUnsupportedEntry);
        }
        if (_compressedSize != _uncompressedSize)
        {
            throw new InvalidDataException(SR.UnexpectedStreamLength);
        }
        _compressionLevel = CompressionLevel.NoCompression;
        _forwardRemaining = _compressedSize;
    }

    private Stream OpenForwardRead()
    {
        _archive.EnsureCurrentForwardEntry(this);
        if (_forwardStream is not null)
        {
            throw new InvalidOperationException(SR.ForwardReadEntryAlreadyOpened);
        }
        return _forwardStream = new ForwardReadEntryStream(this);
    }

    internal async ValueTask DrainForwardEntryAsync(Memory<byte> buffer, bool useAsync, CancellationToken cancellationToken)
    {
        // The entry, not its public stream handle, owns the remaining payload.
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
        if (_forwardRemaining == 0)
        {
            ValidateForwardCrc();
            return 0;
        }
        int read = _archive.ArchiveStream.Read(buffer[..(int)Math.Min(buffer.Length, _forwardRemaining)]);
        AccountForwardBytes(buffer[..read]);
        return read;
    }

    private async ValueTask<int> ReadForwardCoreAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (buffer.IsEmpty)
        {
            return 0;
        }
        if (_forwardRemaining == 0)
        {
            ValidateForwardCrc();
            return 0;
        }
        int read = await _archive.ArchiveStream.ReadAsync(buffer[..(int)Math.Min(buffer.Length, _forwardRemaining)], cancellationToken).ConfigureAwait(false);
        AccountForwardBytes(buffer.Span[..read]);
        return read;
    }

    private void AccountForwardBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            throw new InvalidDataException(SR.UnexpectedStreamLength);
        }
        _forwardRemaining -= bytes.Length;
        if (_forwardStream is not null)
        {
            _forwardCrc = Crc32Helper.UpdateCrc32(_forwardCrc, bytes);
        }
    }

    private void ValidateForwardCrc()
    {
        if (_forwardStream is not null && _forwardCrc != _crc32)
        {
            throw new InvalidDataException(SR.CrcMismatch);
        }
    }

    internal void InvalidateForwardEntry() => _forwardStream?.Invalidate();

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
