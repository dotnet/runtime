// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public sealed partial class ZStandardStream
    {
        private ZStandardDecoder _decoder;
        private int _bufferOffset;
        private int _bufferCount;
        private bool _nonEmptyInput;

        private bool TryDecompress(Span<byte> destination, out int bytesWritten)
        {
            // Decompress any data we may have in our buffer.
            OperationStatus lastResult = _decoder.Decompress(new ReadOnlySpan<byte>(_buffer, _bufferOffset, _bufferCount), destination, out int bytesConsumed, out bytesWritten);
            if (lastResult == OperationStatus.InvalidData)
            {
                throw new InvalidOperationException(SR.ZStandardStream_Decompress_InvalidData);
            }

            if (bytesConsumed != 0)
            {
                _bufferOffset += bytesConsumed;
                _bufferCount -= bytesConsumed;
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
                if (_bufferCount != 0)
                {
                    Debug.Assert(bytesWritten == 0);
                    return true;
                }
            }

            Debug.Assert(
                lastResult == OperationStatus.NeedMoreData ||
                (lastResult == OperationStatus.DestinationTooSmall && destination.IsEmpty && _bufferCount == 0), $"{nameof(lastResult)} == {lastResult}, {nameof(destination.Length)} == {destination.Length}");

            // Ensure any left over data is at the beginning of the array so we can fill the remainder.
            if (_bufferCount != 0 && _bufferOffset != 0)
            {
                new ReadOnlySpan<byte>(_buffer, _bufferOffset, _bufferCount).CopyTo(_buffer);
            }
            _bufferOffset = 0;

            return false;
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
            ValidateBufferArguments(buffer, offset, count);
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

            try
            {
                BeginRWOperation();

                int bytesWritten;
                while (!TryDecompress(buffer, out bytesWritten))
                {
                    int bytesRead = _stream.Read(_buffer, _bufferCount, _buffer.Length - _bufferCount);
                    if (bytesRead <= 0)
                    {
                        if (s_useStrictValidation && _nonEmptyInput && !buffer.IsEmpty)
                            ThrowTruncatedInvalidData();
                        break;
                    }

                    _nonEmptyInput = true;
                    _bufferCount += bytesRead;

                    if (_bufferCount > _buffer.Length)
                    {
                        ThrowInvalidStream();
                    }
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
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Compress</c> when the object was created.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.</exception>
        /// <exception cref="ArgumentException">The <paramref name="buffer" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>Asynchronously reads decompressed bytes from the underlying stream and places them in the specified memory.</summary>
        /// <param name="buffer">The memory to contain the decompressed bytes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the number of bytes read from the underlying stream.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode" /> value was <c>Compress</c> when the object was created.</exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.CannotReadFromCompressionStream);

            EnsureNotDisposed();

            try
            {
                BeginRWOperation();

                int bytesWritten;
                while (!TryDecompress(buffer.Span, out bytesWritten))
                {
                    int bytesRead = await _stream.ReadAsync(_buffer.AsMemory(_bufferCount), cancellationToken).ConfigureAwait(false);
                    if (bytesRead <= 0)
                    {
                        if (s_useStrictValidation && _nonEmptyInput && !buffer.IsEmpty)
                            ThrowTruncatedInvalidData();
                        break;
                    }

                    _nonEmptyInput = true;
                    _bufferCount += bytesRead;

                    if (_bufferCount > _buffer.Length)
                    {
                        ThrowInvalidStream();
                    }
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
        public override int ReadByte()
        {
            Span<byte> singleByte = stackalloc byte[1];
            int bytesRead = Read(singleByte);
            return bytesRead > 0 ? singleByte[0] : -1;
        }

        private static readonly bool s_useStrictValidation =
            AppContext.TryGetSwitch("System.IO.Compression.UseStrictValidation", out bool strictValidation) ? strictValidation : false;

        private static void ThrowInvalidStream() =>
            // The stream is either malicious or poorly implemented and returned a number of
            // bytes larger than the buffer supplied to it.
            throw new InvalidDataException(SR.ZStandardStream_Decompress_InvalidStream);

        private static void ThrowTruncatedInvalidData() =>
            throw new InvalidDataException(SR.ZStandardStream_Decompress_TruncatedData);
    }
}
