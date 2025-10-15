// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public sealed partial class ZstandardStream
    {
        private ZstandardEncoder _encoder;

        /// <summary>Initializes a new instance of the <see cref="ZstandardStream" /> class by using the specified stream and compression level.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        public ZstandardStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, leaveOpen: false) { }

        /// <summary>Initializes a new instance of the <see cref="ZstandardStream" /> class by using the specified stream and compression level, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to compress data to the stream or decompress data from the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZstandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        public ZstandardStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : this(stream, CompressionMode.Compress, leaveOpen)
        {
            _encoder.SetQuality(ZstandardUtils.GetQualityFromCompressionLevel(compressionLevel));
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardStream" /> class by using the specified stream, options, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to which compressed data is written or from which data to decompress is read.</param>
        /// <param name="compressionOptions">The options to use for Zstandard compression or decompression.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZstandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        public ZstandardStream(Stream stream, ZstandardCompressionOptions compressionOptions, bool leaveOpen = false) : this(stream, CompressionMode.Compress, leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(compressionOptions);

            if (compressionOptions.Dictionary is ZstandardDictionary dictionary)
            {
                _encoder = new ZstandardEncoder(dictionary, ZstandardUtils.WindowBits_Default);
            }
            else
            {
                _encoder.SetQuality(compressionOptions.Quality);
            }
        }

        /// <summary>Initializes a new instance of the <see cref="ZstandardStream" /> class by using the specified stream and encoder instance.</summary>
        /// <param name="stream">The stream to which compressed data is written.</param>
        /// <param name="encoder">The encoder instance to use for compression. The stream will not dispose this encoder; instead, it will reset it when the stream is disposed.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZstandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> does not support writing.</exception>
        public ZstandardStream(Stream stream, ZstandardEncoder encoder, bool leaveOpen = false) : this(stream, CompressionMode.Compress, leaveOpen)
        {
            _encoder = encoder;
            _encoderOwned = false;
        }

        private void WriteCore(ReadOnlySpan<byte> buffer, bool isFinalBlock = false, bool flush = false, bool checkActiveRWOps = true)
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.CannotWriteToDecompressionStream);
            EnsureNotDisposed();

            try
            {
                if (checkActiveRWOps)
                {
                    BeginRWOperation();
                }

                OperationStatus lastResult = OperationStatus.DestinationTooSmall;
                Span<byte> output = new Span<byte>(_buffer);
                while (lastResult == OperationStatus.DestinationTooSmall)
                {
                    int bytesWritten;
                    int bytesConsumed;

                    if (flush)
                    {
                        Debug.Assert(buffer.Length == 0);
                        bytesConsumed = 0;
                        lastResult = _encoder.Flush(output, out bytesWritten);
                    }
                    else
                    {
                        lastResult = _encoder.Compress(buffer, output, out bytesConsumed, out bytesWritten, isFinalBlock);
                    }

                    if (lastResult == OperationStatus.InvalidData)
                        throw new InvalidOperationException(SR.ZstandardStream_Compress_InvalidData);
                    if (bytesWritten > 0)
                        _stream.Write(output.Slice(0, bytesWritten));
                    if (bytesConsumed > 0)
                        buffer = buffer.Slice(bytesConsumed);
                }
            }
            finally
            {
                if (checkActiveRWOps)
                {
                    EndRWOperation();
                }
            }
        }

        private async ValueTask WriteCoreAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, bool isFinalBlock = false, bool flush = false, bool checkActiveRWOps = true)
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.CannotWriteToDecompressionStream);
            EnsureNotDisposed();

            try
            {
                if (checkActiveRWOps)
                {
                    BeginRWOperation();
                }

                OperationStatus lastResult = OperationStatus.DestinationTooSmall;
                Memory<byte> output = _buffer.AsMemory();
                while (lastResult == OperationStatus.DestinationTooSmall)
                {
                    int bytesWritten;
                    int bytesConsumed;

                    if (flush)
                    {
                        Debug.Assert(buffer.Length == 0);
                        bytesConsumed = 0;
                        lastResult = _encoder.Flush(output.Span, out bytesWritten);
                    }
                    else
                    {
                        lastResult = _encoder.Compress(buffer.Span, output.Span, out bytesConsumed, out bytesWritten, isFinalBlock);
                    }

                    if (lastResult == OperationStatus.InvalidData)
                        throw new InvalidOperationException(SR.ZstandardStream_Compress_InvalidData);
                    if (bytesWritten > 0)
                        await _stream.WriteAsync(output.Slice(0, bytesWritten), cancellationToken).ConfigureAwait(false);
                    if (bytesConsumed > 0)
                        buffer = buffer.Slice(bytesConsumed);
                }
            }
            finally
            {
                if (checkActiveRWOps)
                {
                    EndRWOperation();
                }
            }
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
            ValidateBufferArguments(buffer, offset, count);
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        /// <summary>Writes compressed bytes to the underlying stream from the specified span.</summary>
        /// <param name="buffer">The span that contains the data to compress.</param>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Decompress</c> when the object was created.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            WriteCore(buffer);
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
            ValidateBufferArguments(buffer, offset, count);
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

            return cancellationToken.IsCancellationRequested ?
                ValueTask.FromCanceled(cancellationToken) :
                WriteCoreAsync(buffer, cancellationToken);
        }

        /// <summary>Begins an asynchronous write operation.</summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer"/> from which to begin writing.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the write is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An object that represents the asynchronous write, which could still be pending.</returns>
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

        /// <summary>Flushes the internal buffer.</summary>
        public override void Flush()
        {
            EnsureNotDisposed();
            EnsureNoActiveRWOperation();

            if (_mode == CompressionMode.Compress)
            {
                WriteCore(Span<byte>.Empty, flush: true);
                _stream.Flush();
            }
        }

        /// <summary>Asynchronously flushes the internal buffer.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();
            EnsureNoActiveRWOperation();

            if (_mode == CompressionMode.Compress)
            {
                return FlushAsyncInternal(cancellationToken);
            }

            return Task.CompletedTask;

            async Task FlushAsyncInternal(CancellationToken cancellationToken)
            {
                await WriteCoreAsync(Memory<byte>.Empty, cancellationToken, flush: true).ConfigureAwait(false);
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
