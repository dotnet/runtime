// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1822 // TODO: Remove this

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties used to compress and decompress streams by using the ZStandard data format specification.</summary>
    public sealed partial class ZStandardStream : Stream
    {
        private const int DefaultInternalBufferSize = 65536; // 64KB default buffer
        private Stream _stream;
        private byte[] _buffer;
        private readonly bool _leaveOpen;
        private readonly CompressionMode _mode;

        /// <summary>Initializes a new instance of the <see cref="ZStandardStream" /> class by using the specified stream and compression level.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        public ZStandardStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, leaveOpen: false) { }

        /// <summary>Initializes a new instance of the <see cref="ZStandardStream" /> class by using the specified stream and compression level, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZStandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        public ZStandardStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : this(stream, CompressionMode.Compress, leaveOpen)
        {
            // TODO: set compression level
            throw new NotImplementedException();
        }

        /// <summary>Initializes a new instance of the <see cref="ZStandardStream" /> class by using the specified stream and compression mode.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        public ZStandardStream(Stream stream, CompressionMode mode) : this(stream, mode, leaveOpen: false) { }

        /// <summary>Initializes a new instance of the <see cref="ZStandardStream" /> class by using the specified stream and compression mode, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZStandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        public ZStandardStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _mode = mode;
            _leaveOpen = leaveOpen;

            switch (mode)
            {
                case CompressionMode.Compress:
                    if (!stream.CanWrite)
                    {
                        throw new ArgumentException(SR.Stream_FalseCanWrite, nameof(stream));
                    }
                    break;

                case CompressionMode.Decompress:
                    if (!stream.CanRead)
                    {
                        throw new ArgumentException(SR.Stream_FalseCanRead, nameof(stream));
                    }
                    break;

                default:
                    throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(mode));
            }

            _stream = stream;
            _buffer = ArrayPool<byte>.Shared.Rent(DefaultInternalBufferSize);


        }

        /// <summary>Initializes a new instance of the <see cref="ZStandardStream" /> class by using the specified stream, options, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="compressionOptions">The options to use for ZStandard compression or decompression.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZStandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        public ZStandardStream(Stream stream, ZStandardCompressionOptions compressionOptions, bool leaveOpen = false) : this(stream, CompressionMode.Compress, leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(compressionOptions);

            //TODO: set quality
            throw new NotImplementedException();
        }

        /// <summary>Gets a reference to the underlying stream.</summary>
        /// <value>A stream object that represents the underlying stream.</value>
        public Stream BaseStream => _stream;

        /// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
        /// <value><see langword="true" /> if the <see cref="CompressionMode" /> value is <c>Decompress,</c> and the underlying stream supports reading and is not closed; otherwise, <see langword="false" />.</value>
        public override bool CanRead => _mode == CompressionMode.Decompress && _stream?.CanRead == true;

        /// <summary>Gets a value indicating whether the stream supports writing.</summary>
        /// <value><see langword="true" /> if the <see cref="CompressionMode" /> value is <c>Compress,</c> and the underlying stream supports writing and is not closed; otherwise, <see langword="false" />.</value>
        public override bool CanWrite => _mode == CompressionMode.Compress && _stream?.CanWrite == true;

        /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
        /// <value><see langword="false" /> in all cases.</value>
        public override bool CanSeek => false;

        /// <summary>This property is not supported and always throws a <see cref="NotSupportedException" />.</summary>
        /// <value>A long value.</value>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Length => throw new NotSupportedException();

        /// <summary>This property is not supported and always throws a <see cref="NotSupportedException" />.</summary>
        /// <value>A long value.</value>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>This operation is not supported and always throws a <see cref="NotSupportedException" />.</summary>
        /// <param name="offset">The byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">One of the <see cref="SeekOrigin" /> values that indicates the reference point used to obtain the new position.</param>
        /// <returns>The new position within the stream.</returns>
        /// <exception cref="NotSupportedException">This method is not supported on this stream.</exception>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <summary>This operation is not supported and always throws a <see cref="NotSupportedException" />.</summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="NotSupportedException">This method is not supported on this stream.</exception>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>Flushes the internal buffer.</summary>
        public override void Flush()
        {
            EnsureNotDisposed();
            if (_mode == CompressionMode.Compress)
            {
                FlushBuffers();
            }
        }

        /// <summary>Asynchronously flushes the internal buffer.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();
            return _mode == CompressionMode.Compress ? FlushBuffersAsync(cancellationToken) : Task.CompletedTask;
        }

        /// <summary>Reads decompressed bytes from the underlying stream and places them in the specified array.</summary>
        /// <param name="buffer">The byte array to contain the decompressed bytes.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of decompressed bytes to read.</param>
        /// <returns>The number of bytes that were read into the byte array.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Compress</c> when the object was created.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateParameters(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        /// <summary>Reads decompressed bytes from the underlying stream and places them in the specified span.</summary>
        /// <param name="buffer">The span to contain the decompressed bytes.</param>
        /// <returns>The number of bytes that were read into the span.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Compress</c> when the object was created.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override int Read(Span<byte> buffer)
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.CannotReadFromCompressionStream);

            EnsureNotDisposed();

            // TODO: Implement ZStandard decompression
            throw new NotImplementedException("ZStandard decompression not yet implemented");
        }

        /// <summary>Writes compressed bytes to the underlying stream from the specified byte array.</summary>
        /// <param name="buffer">The buffer that contains the data to compress.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> from which the bytes will be read.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Decompress</c> when the object was created.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateParameters(buffer, offset, count);
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        /// <summary>Writes compressed bytes to the underlying stream from the specified span.</summary>
        /// <param name="buffer">The span that contains the data to compress.</param>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Decompress</c> when the object was created.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.CannotWriteToDecompressionStream);

            EnsureNotDisposed();

            // TODO: Implement ZStandard compression
            throw new NotImplementedException("ZStandard compression not yet implemented");
        }

        /// <summary>Writes a byte to the current position in the stream and advances the position within the stream by one byte.</summary>
        /// <param name="value">The byte to write to the stream.</param>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Decompress</c> when the object was created.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override void WriteByte(byte value)
        {
            Span<byte> singleByte = stackalloc byte[1] { value };
            Write(singleByte);
        }

        /// <summary>Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Compress</c> when the object was created.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override int ReadByte()
        {
            Span<byte> singleByte = stackalloc byte[1];
            int bytesRead = Read(singleByte);
            return bytesRead == 1 ? singleByte[0] : -1;
        }

        /// <summary>Begins an asynchronous read operation.</summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which to begin writing data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the read is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An <see cref="IAsyncResult" /> that represents the asynchronous read, which could still be pending.</returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
        }

        /// <summary>Waits for the pending asynchronous read to complete.</summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The number of bytes read from the stream, between zero (0) and the number of bytes you requested. Streams return zero (0) only at the end of the stream, otherwise, they should block until at least one byte is available.</returns>
        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<int>(asyncResult);
        }

        /// <summary>Begins an asynchronous write operation.</summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> from which to begin writing.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the write is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An <see cref="IAsyncResult" /> that represents the asynchronous write, which could still be pending.</returns>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
        }

        /// <summary>Ends an asynchronous write operation.</summary>
        /// <param name="asyncResult">A reference to the outstanding asynchronous I/O request.</param>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        /// <summary>Asynchronously reads decompressed bytes from the underlying stream and places them in the specified array.</summary>
        /// <param name="buffer">The byte array to contain the decompressed bytes.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of decompressed bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the number of bytes read from the underlying stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Compress</c> when the object was created.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>Asynchronously reads decompressed bytes from the underlying stream and places them in the specified memory.</summary>
        /// <param name="buffer">The memory to contain the decompressed bytes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the number of bytes read from the underlying stream.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Compress</c> when the object was created.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.CannotReadFromCompressionStream);

            EnsureNotDisposed();

            // TODO: Implement async ZStandard decompression
            throw new NotImplementedException("Async ZStandard decompression not yet implemented");
        }

        /// <summary>Asynchronously writes compressed bytes to the underlying stream from the specified array.</summary>
        /// <param name="buffer">The buffer that contains the data to compress.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> from which the bytes will be read.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Decompress</c> when the object was created.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>Asynchronously writes compressed bytes to the underlying stream from the specified memory.</summary>
        /// <param name="buffer">The memory that contains the data to compress.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Decompress</c> when the object was created.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.CannotWriteToDecompressionStream);

            EnsureNotDisposed();

            // TODO: Implement async ZStandard compression
            throw new NotImplementedException("Async ZStandard compression not yet implemented");
        }

        /// <summary>Releases the unmanaged resources used by the <see cref="ZStandardStream" /> and optionally releases the managed resources.</summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _stream != null)
                {
                    if (_mode == CompressionMode.Compress)
                    {
                        FlushBuffers();
                    }

                    if (!_leaveOpen)
                    {
                        _stream.Dispose();
                    }
                }
            }
            finally
            {
                _stream = null!;
                if (_buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = null!;
                }

                base.Dispose(disposing);
            }
        }

        /// <summary>Asynchronously releases the unmanaged resources used by the <see cref="ZStandardStream" />.</summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public override async ValueTask DisposeAsync()
        {
            if (_stream != null)
            {
                if (_mode == CompressionMode.Compress)
                {
                    await FlushBuffersAsync(CancellationToken.None).ConfigureAwait(false);
                }

                if (!_leaveOpen)
                {
                    await _stream.DisposeAsync().ConfigureAwait(false);
                }

                _stream = null!;
            }

            if (_buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null!;
            }
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_stream == null, this);
        }

        private static void ValidateParameters(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length - count);
        }

        private void FlushBuffers()
        {
            // TODO: Implement buffer flushing for compression
        }

        private Task FlushBuffersAsync(CancellationToken cancellationToken)
        {
            // TODO: Implement async buffer flushing for compression
            return Task.CompletedTask;
        }
    }
}
