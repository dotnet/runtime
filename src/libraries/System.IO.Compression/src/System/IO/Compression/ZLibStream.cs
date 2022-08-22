// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties used to compress and decompress streams by using the zlib data format specification.</summary>
    public sealed class ZLibStream : Stream
    {
        /// <summary>The underlying deflate stream.</summary>
        private DeflateStream _deflateStream;

        /// <summary>Initializes a new instance of the <see cref="ZLibStream"/> class by using the specified stream and compression mode.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        public ZLibStream(Stream stream, CompressionMode mode) : this(stream, mode, leaveOpen: false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ZLibStream"/> class by using the specified stream, compression mode, and whether to leave the <paramref name="stream"/> open.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream object open after disposing the <see cref="ZLibStream"/> object; otherwise, <see langword="false" />.</param>
        public ZLibStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            _deflateStream = new DeflateStream(stream, mode, leaveOpen, ZLibNative.ZLib_DefaultWindowBits);
        }

        /// <summary>Initializes a new instance of the <see cref="ZLibStream"/> class by using the specified stream and compression level.</summary>
        /// <param name="stream">The stream to which compressed data is written.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing data to the stream.</param>
        public ZLibStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, leaveOpen: false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ZLibStream"/> class by using the specified stream, compression level, and whether to leave the <paramref name="stream"/> open.</summary>
        /// <param name="stream">The stream to which compressed data is written.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing data to the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream object open after disposing the <see cref="ZLibStream"/> object; otherwise, <see langword="false" />.</param>
        public ZLibStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            _deflateStream = new DeflateStream(stream, compressionLevel, leaveOpen, ZLibNative.ZLib_DefaultWindowBits);
        }

        /// <summary>Gets a value indicating whether the stream supports reading.</summary>
        public override bool CanRead => _deflateStream?.CanRead ?? false;

        /// <summary>Gets a value indicating whether the stream supports writing.</summary>
        public override bool CanWrite => _deflateStream?.CanWrite ?? false;

        /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
        public override bool CanSeek => false;

        /// <summary>This property is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>This property is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>Flushes the internal buffers.</summary>
        public override void Flush()
        {
            ThrowIfClosed();
            _deflateStream.Flush();
        }

        /// <summary>Asynchronously clears all buffers for this stream, causes any buffered data to be written to the underlying device, and monitors cancellation requests.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            return _deflateStream.FlushAsync(cancellationToken);
        }

        /// <summary>This method is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <summary>This method is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
        /// <returns>The unsigned byte cast to an <see cref="int" />, or -1 if at the end of the stream.</returns>
        public override int ReadByte()
        {
            ThrowIfClosed();
            return _deflateStream.ReadByte();
        }

        /// <summary>Begins an asynchronous read operation.</summary>
        /// <param name="buffer">The byte array to read the data into.</param>
        /// <param name="offset">The byte offset in array at which to begin reading data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="asyncCallback">An optional asynchronous callback, to be called when the read operation is complete.</param>
        /// <param name="asyncState">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An object that represents the asynchronous read operation, which could still be pending.</returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
        {
            ThrowIfClosed();
            return _deflateStream.BeginRead(buffer, offset, count, asyncCallback, asyncState);
        }

        /// <summary>Waits for the pending asynchronous read to complete.</summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The number of bytes that were read into the byte array.</returns>
        public override int EndRead(IAsyncResult asyncResult) =>
            _deflateStream.EndRead(asyncResult);

        /// <summary>Reads a number of decompressed bytes into the specified byte array.</summary>
        /// <param name="buffer">The byte array to read the data into.</param>
        /// <param name="offset">The byte offset in array at which to begin reading data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes that were read into the byte array.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfClosed();
            return _deflateStream.Read(buffer, offset, count);
        }

        /// <summary>Reads a number of decompressed bytes into the specified byte span.</summary>
        /// <param name="buffer">The span to read the data into.</param>
        /// <returns>The number of bytes that were read into the byte span.</returns>
        public override int Read(Span<byte> buffer)
        {
            ThrowIfClosed();
            return _deflateStream.ReadCore(buffer);
        }

        /// <summary>Asynchronously reads a sequence of bytes from the current stream, advances the position within the stream by the number of bytes read, and monitors cancellation requests.</summary>
        /// <param name="buffer">The byte array to read the data into.</param>
        /// <param name="offset">The byte offset in array at which to begin reading data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous completion of the operation.</returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            return _deflateStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>Asynchronously reads a sequence of bytes from the current stream, advances the position within the stream by the number of bytes read, and monitors cancellation requests.</summary>
        /// <param name="buffer">The byte span to read the data into.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous completion of the operation.</returns>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfClosed();
            return _deflateStream.ReadAsyncMemory(buffer, cancellationToken);
        }

        /// <summary>Begins an asynchronous write operation.</summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The byte offset in buffer to begin writing from.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="asyncCallback">An optional asynchronous callback, to be called when the write operation is complete.</param>
        /// <param name="asyncState">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An object that represents the asynchronous write operation, which could still be pending.</returns>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
        {
            ThrowIfClosed();
            return _deflateStream.BeginWrite(buffer, offset, count, asyncCallback, asyncState);
        }

        /// <summary>Ends an asynchronous write operation.</summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        public override void EndWrite(IAsyncResult asyncResult) =>
            _deflateStream.EndWrite(asyncResult);

        /// <summary>Writes compressed bytes to the underlying stream from the specified byte array.</summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The byte offset in buffer to begin writing from.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfClosed();
            _deflateStream.Write(buffer, offset, count);
        }

        /// <summary>Writes compressed bytes to the underlying stream from the specified byte span.</summary>
        /// <param name="buffer">The buffer to write data from.</param>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ThrowIfClosed();
            _deflateStream.WriteCore(buffer);
        }

        /// <summary>Asynchronously writes a sequence of bytes to the current stream, advances the current position within this stream by the number of bytes written, and monitors cancellation requests.</summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The byte offset in buffer to begin writing from.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous completion of the operation.</returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            return _deflateStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>Asynchronously writes a sequence of bytes to the current stream, advances the current position within this stream by the number of bytes written, and monitors cancellation requests.</summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous completion of the operation.</returns>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfClosed();
            return _deflateStream.WriteAsyncMemory(buffer, cancellationToken);
        }

        /// <summary>Writes a byte to the current position in the stream and advances the position within the stream by one byte.</summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            ThrowIfClosed();
            _deflateStream.WriteByte(value);
        }

        /// <summary>Reads the bytes from the current stream and writes them to another stream, using the specified buffer size.</summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">The size of the buffer. This value must be greater than zero.</param>
        public override void CopyTo(Stream destination, int bufferSize)
        {
            ThrowIfClosed();
            _deflateStream.CopyTo(destination, bufferSize);
        }

        /// <summary>Asynchronously reads the bytes from the current stream and writes them to another stream, using a specified buffer size and cancellation token.</summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ThrowIfClosed();
            return _deflateStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <summary>Releases all resources used by the <see cref="Stream"/>.</summary>
        /// <param name="disposing">Whether this method is being called from Dispose.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _deflateStream?.Dispose();
                }
                _deflateStream = null!;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>Asynchronously releases all resources used by the <see cref="Stream"/>.</summary>
        /// <returns>A task that represents the completion of the disposal operation.</returns>
        public override ValueTask DisposeAsync()
        {
            DeflateStream? ds = _deflateStream;
            if (ds is not null)
            {
                _deflateStream = null!;
                return ds.DisposeAsync();
            }

            return default;
        }

        /// <summary>Gets a reference to the underlying stream.</summary>
        public Stream BaseStream => _deflateStream?.BaseStream!;

        /// <summary>Throws an <see cref="ObjectDisposedException"/> if the stream is closed.</summary>
        private void ThrowIfClosed()
        {
            if (_deflateStream is null)
            {
                ThrowClosedException();
            }
        }

        /// <summary>Throws an <see cref="ObjectDisposedException"/>.</summary>
        [DoesNotReturn]
        private static void ThrowClosedException() =>
            throw new ObjectDisposedException(nameof(ZLibStream), SR.ObjectDisposed_StreamClosed);
    }
}
