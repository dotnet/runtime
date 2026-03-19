// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public sealed partial class ZstandardStream
    {
        private ZstandardDecoder? _decoder;
        private bool _nonEmptyInput;

        /// <summary>Initializes a new instance of the <see cref="ZstandardStream" /> class by using the specified stream and decoder instance.</summary>
        /// <param name="stream">The stream from which data to decompress is read.</param>
        /// <param name="decoder">The decoder instance to use for decompression. The stream will not dispose this decoder; instead, it will reset it when the stream is disposed.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZstandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> does not support reading.</exception>
        public ZstandardStream(Stream stream, ZstandardDecoder decoder, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(decoder);

            Init(stream, CompressionMode.Decompress);
            _mode = CompressionMode.Decompress;
            _leaveOpen = leaveOpen;

            _decoder = decoder;
            _encoderOwned = false;
        }

        private bool TryDecompress(Span<byte> destination, out int bytesWritten, out OperationStatus lastResult)
        {
            Debug.Assert(_decoder != null);

            // Decompress any data we may have in our buffer.
            lastResult = _decoder.Decompress(_buffer.ActiveSpan, destination, out int bytesConsumed, out bytesWritten);
            _buffer.Discard(bytesConsumed);

            if (lastResult == OperationStatus.InvalidData)
            {
                throw new InvalidDataException(SR.ZstandardStream_Decompress_InvalidData);
            }

            // If we successfully decompressed any bytes, or if we've reached the end of the decompression, we're done.
            if (bytesWritten != 0 || lastResult == OperationStatus.Done)
            {
                return true;
            }

            if (destination.IsEmpty)
            {
                // The caller provided a zero-byte buffer.  This is typically done in order to avoid allocating/renting
                // a buffer until data is known to be available.  We don't have perfect knowledge here, as _decoder.Decompress
                // will return DestinationTooSmall whether or not more data is required.  As such, we assume that if there's
                // any data in our input buffer, it would have been decompressible into at least one byte of output, and
                // otherwise we need to do a read on the underlying stream.  This isn't perfect, because having input data
                // doesn't necessarily mean it'll decompress into at least one byte of output, but it's a reasonable approximation
                // for the 99% case.  If it's wrong, it just means that a caller using zero-byte reads as a way to delay
                // getting a buffer to use for a subsequent call may end up getting one earlier than otherwise preferred.
                Debug.Assert(lastResult == OperationStatus.DestinationTooSmall);
                if (_buffer.ActiveLength != 0)
                {
                    Debug.Assert(bytesWritten == 0);
                    return true;
                }
            }

            Debug.Assert(
                lastResult == OperationStatus.NeedMoreData ||
                (lastResult == OperationStatus.DestinationTooSmall && destination.IsEmpty && _buffer.ActiveLength == 0), $"{nameof(lastResult)} == {lastResult}, {nameof(destination.Length)} == {destination.Length}");

            return false;
        }

        /// <summary>Reads decompressed bytes from the underlying stream and places them in the specified array.</summary>
        /// <param name="buffer">The byte array to contain the decompressed bytes.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of decompressed bytes to read.</param>
        /// <returns>The number of bytes that were read into the byte array.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <see cref="CompressionMode.Compress"/> when the object was created or concurrent read operations were attempted.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        /// <exception cref="IOException">Failed to decompress data from the underlying stream.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        /// <summary>Reads decompressed bytes from the underlying stream and places them in the specified span.</summary>
        /// <param name="buffer">The span to contain the decompressed bytes.</param>
        /// <returns>The number of bytes that were read into the span.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <see cref="CompressionMode.Compress"/> when the object was created or concurrent read operations were attempted.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        /// <exception cref="IOException">Failed to decompress data from the underlying stream.</exception>
        public override int Read(Span<byte> buffer)
        {
            if (_mode != CompressionMode.Decompress)
            {
                throw new InvalidOperationException(SR.CannotReadFromCompressionStream);
            }

            EnsureNotDisposed();
            BeginRWOperation();

            try
            {
                int bytesWritten;
                OperationStatus lastResult;
                while (!TryDecompress(buffer, out bytesWritten, out lastResult))
                {
                    int bytesRead = _stream.Read(_buffer.AvailableSpan);
                    if (bytesRead <= 0)
                    {
                        if (_nonEmptyInput && !buffer.IsEmpty)
                            ThrowTruncatedInvalidData();
                        break;
                    }

                    _nonEmptyInput = true;

                    if (bytesRead > _buffer.AvailableLength)
                    {
                        ThrowInvalidStream();
                    }

                    _buffer.Commit(bytesRead);
                }

                // When decompression finishes, rewind the stream to the exact end of compressed data
                if (lastResult == OperationStatus.Done && _stream.CanSeek)
                {
                    TryRewindStream(_stream);
                }

                return bytesWritten;
            }
            finally
            {
                EndRWOperation();
            }
        }

        /// <summary>Asynchronously reads decompressed bytes from the underlying stream and places them in the specified array.</summary>
        /// <param name="buffer">The byte array to contain the decompressed bytes.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of decompressed bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the number of bytes read from the underlying stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <see cref="CompressionMode.Compress"/> when the object was created or concurrent read operations were attempted.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        /// <exception cref="IOException">Failed to decompress data from the underlying stream.</exception>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>Asynchronously reads decompressed bytes from the underlying stream and places them in the specified memory.</summary>
        /// <param name="buffer">The memory to contain the decompressed bytes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the number of bytes read from the underlying stream.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <see cref="CompressionMode.Compress"/> when the object was created or concurrent read operations were attempted.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        /// <exception cref="IOException">Failed to decompress data from the underlying stream.</exception>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_mode != CompressionMode.Decompress)
            {
                throw new InvalidOperationException(SR.CannotReadFromCompressionStream);
            }

            EnsureNotDisposed();
            BeginRWOperation();

            try
            {
                int bytesWritten;
                OperationStatus lastResult;
                while (!TryDecompress(buffer.Span, out bytesWritten, out lastResult))
                {
                    int bytesRead = await _stream.ReadAsync(_buffer.AvailableMemory, cancellationToken).ConfigureAwait(false);
                    if (bytesRead <= 0)
                    {
                        if (_nonEmptyInput && !buffer.IsEmpty)
                            ThrowTruncatedInvalidData();
                        break;
                    }

                    _nonEmptyInput = true;

                    if (bytesRead > _buffer.AvailableLength)
                    {
                        ThrowInvalidStream();
                    }

                    _buffer.Commit(bytesRead);
                }

                // When decompression finishes, rewind the stream to the exact end of compressed data
                if (lastResult == OperationStatus.Done && _stream.CanSeek)
                {
                    TryRewindStream(_stream);
                }

                return bytesWritten;
            }
            finally
            {
                EndRWOperation();
            }
        }

        /// <summary>Begins an asynchronous read operation.</summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer"/> at which to begin writing data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the read is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An object that represents the asynchronous read, which could still be pending.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <see cref="CompressionMode.Compress"/> when the object was created or concurrent read operations were attempted.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        /// <exception cref="IOException">Failed to decompress data from the underlying stream.</exception>
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

        /// <summary>Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
        /// <returns>The unsigned byte cast to an <see cref="int"/>, or -1 if at the end of the stream.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <see cref="CompressionMode.Compress"/> when the object was created or concurrent read operations were attempted.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
        /// <exception cref="IOException">Failed to decompress data from the underlying stream.</exception>
        public override int ReadByte()
        {
            Span<byte> singleByte = stackalloc byte[1];
            int bytesRead = Read(singleByte);
            return bytesRead > 0 ? singleByte[0] : -1;
        }

        /// <summary>
        /// Rewinds the underlying stream to the exact end of the compressed data if there are unconsumed bytes.
        /// This is called when decompression finishes to reset the stream position.
        /// </summary>
        private void TryRewindStream(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(_mode == CompressionMode.Decompress);
            Debug.Assert(stream.CanSeek);

            // Check if there are unconsumed bytes in the buffer
            int unconsumedBytes = _buffer.ActiveLength;
            if (unconsumedBytes > 0)
            {
                // Rewind the stream to the exact end of the compressed data
                stream.Seek(-unconsumedBytes, SeekOrigin.Current);
                _buffer.Discard(unconsumedBytes);
            }
        }

        [DoesNotReturn]
        private static void ThrowInvalidStream() =>
            // The stream is either malicious or poorly implemented and returned a number of
            // bytes larger than the buffer supplied to it.
            throw new InvalidDataException(SR.ZstandardStream_Decompress_InvalidStream);

        [DoesNotReturn]
        private static void ThrowTruncatedInvalidData() =>
            throw new InvalidDataException(SR.ZstandardStream_Decompress_TruncatedData);
    }
}
