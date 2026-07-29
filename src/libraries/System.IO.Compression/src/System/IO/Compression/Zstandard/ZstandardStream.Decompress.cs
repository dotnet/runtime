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
        private bool _endOfStream;

        // Length of a Zstandard frame magic number, in bytes.
        private const int ZstdFrameMagicLength = 4;

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

        /// <summary>Initializes a new instance of the <see cref="ZstandardStream" /> class by using the specified stream, decompression options, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream from which data to decompress is read.</param>
        /// <param name="decompressionOptions">The options to use for Zstandard decompression.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after the <see cref="ZstandardStream" /> object is disposed; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="decompressionOptions"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> does not support reading.</exception>
        public ZstandardStream(Stream stream, ZstandardDecompressionOptions decompressionOptions, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(decompressionOptions);

            Init(stream, CompressionMode.Decompress);
            _mode = CompressionMode.Decompress;
            _leaveOpen = leaveOpen;

            _decoder = new ZstandardDecoder(decompressionOptions);
        }

        // Decompresses buffered input for the current frame only. Returns true when there is a result for
        // the caller to act on (output was produced, the frame finished, or a zero-byte read should
        // return), and false when more input is needed to make progress on the current frame. Crossing a
        // frame boundary into the next concatenated frame is handled by Read/ReadAsync once this reports
        // OperationStatus.Done.
        private bool TryDecompress(Span<byte> destination, out int bytesWritten, out OperationStatus lastResult)
        {
            Debug.Assert(_decoder != null);

            OperationStatus status = _decoder.Decompress(_buffer.ActiveSpan, destination, out int bytesConsumed, out bytesWritten);
            _buffer.Discard(bytesConsumed);
            lastResult = status;

            if (status == OperationStatus.InvalidData)
            {
                throw new InvalidDataException(SR.ZstandardStream_Decompress_InvalidData);
            }

            if (status == OperationStatus.Done)
            {
                // Reached the end of a frame. ZSTD_decompressStream reports end-of-frame, not end-of-stream:
                // a zstd stream may be frames concatenated back-to-back (RFC 8878 section 3), so Read/ReadAsync
                // decide whether another frame follows. This may be a zero-output frame (bytesWritten == 0).
                return true;
            }

            if (bytesWritten != 0)
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
                Debug.Assert(status == OperationStatus.DestinationTooSmall);
                if (_buffer.ActiveLength != 0)
                {
                    return true;
                }
            }

            Debug.Assert(
                status == OperationStatus.NeedMoreData ||
                (status == OperationStatus.DestinationTooSmall && destination.IsEmpty && _buffer.ActiveLength == 0), $"{nameof(status)} == {status}, {nameof(destination.Length)} == {destination.Length}");

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
                if (_endOfStream)
                {
                    // A previous read reached the end of the final frame (or rejected trailing data). The
                    // boundary probe may have left the decoder non-resumable, so never re-enter the decode
                    // loop; report end-of-stream to all subsequent reads.
                    return 0;
                }

                while (true)
                {
                    int bytesWritten;
                    OperationStatus lastResult;
                    while (!TryDecompress(buffer, out bytesWritten, out lastResult))
                    {
                        _buffer.EnsureAvailableSpace(1);

                        int bytesRead = _stream.Read(_buffer.AvailableSpan);
                        if (bytesRead <= 0)
                        {
                            // The underlying stream ended in the middle of a frame, so the data is truncated.
                            // A clean end after a completed frame is reported as Done by TryDecompress and is
                            // resolved by the frame-boundary logic below, not here.
                            if (_nonEmptyInput && !buffer.IsEmpty)
                            {
                                ThrowTruncatedInvalidData();
                            }

                            return 0;
                        }

                        _nonEmptyInput = true;

                        if (bytesRead > _buffer.AvailableLength)
                        {
                            ThrowInvalidStream();
                        }

                        _buffer.Commit(bytesRead);
                    }

                    if (lastResult != OperationStatus.Done || bytesWritten != 0)
                    {
                        // Output to hand back, or not at a finished-frame boundary: return to the caller.
                        return bytesWritten;
                    }

                    // A frame finished with no pending output. A zstd stream may be frames concatenated
                    // back-to-back (RFC 8878 section 3), so decide whether another frame follows before
                    // reporting end-of-stream. async: false completes synchronously (it only ever takes the
                    // synchronous read path).
                    ValueTask<bool> advanceTask = AdvanceToNextFrame(async: false, cancellationToken: default);
                    Debug.Assert(advanceTask.IsCompleted, "AdvanceToNextFrame should complete synchronously when async: false");
                    if (!advanceTask.GetAwaiter().GetResult())
                    {
                        return bytesWritten;
                    }
                }
            }
            finally
            {
                EndRWOperation();
            }
        }

        // Called after TryDecompress reports OperationStatus.Done with no pending output (a finished frame).
        // Decides whether another concatenated frame follows by reading up to a frame magic and feeding just
        // those bytes to the decoder: a valid magic is accepted (NeedMoreData) so decoding continues, while
        // bytes that are not a frame magic identify trailing data after the final frame. Returns true (decoder
        // reset and ready) so the caller loops to decode the next frame, or false (stream complete; _endOfStream
        // is set and a seekable base stream is rewound to the end of the compressed data) so the caller returns.
        // Shared by Read (async: false) and ReadAsync (async: true).
        private async ValueTask<bool> AdvanceToNextFrame(bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(_decoder != null);

            if (_buffer.ActiveLength < ZstdFrameMagicLength)
            {
                // Not enough buffered to tell a split next-frame magic from end-of-stream; read just enough
                // to complete the magic. Limiting the read to exactly the missing bytes (rather than filling
                // the available buffer) avoids consuming and hiding trailing bytes past the magic on a
                // non-seekable stream, where they can't be rewound; any following frame's body is read by the
                // Read/ReadAsync loop afterwards.
                int needed = ZstdFrameMagicLength - _buffer.ActiveLength;
                _buffer.EnsureAvailableSpace(needed);
                int peeked = async
                    ? await _stream.ReadAtLeastAsync(_buffer.AvailableMemory.Slice(0, needed), needed, throwOnEndOfStream: false, cancellationToken: cancellationToken).ConfigureAwait(false)
                    : _stream.ReadAtLeast(_buffer.AvailableSpan.Slice(0, needed), needed, throwOnEndOfStream: false);
                if (peeked > 0)
                {
                    _nonEmptyInput = true;
                    _buffer.Commit(peeked);
                }
            }

            if (_buffer.ActiveLength >= ZstdFrameMagicLength)
            {
                // Determine whether another concatenated frame follows by feeding the decoder exactly the next
                // frame magic number. The decoder validates the magic against both standard and skippable
                // frames: a valid magic leaves it asking for more input (NeedMoreData), so decoding continues,
                // while bytes that are not a frame magic produce InvalidData, identifying trailing data after
                // the final frame. Feeding only the magic (not the whole buffer) means a frame whose magic is
                // valid but whose body is corrupt is not mistaken for trailing data here: the magic is accepted
                // and the corrupt body is rejected by the subsequent decode in the Read/ReadAsync loop.
                _decoder.Reset();

                // The magic alone never decodes into output, so the decoder needs no real output space; a single
                // scratch byte borrowed from the buffer's free region (which the decoder won't write to) satisfies
                // the non-empty-destination requirement. A Span local / stackalloc can't be used here because this
                // method is async.
                _buffer.EnsureAvailableSpace(1);
                if (_decoder.Decompress(_buffer.ActiveSpan.Slice(0, ZstdFrameMagicLength), _buffer.AvailableSpan.Slice(0, 1), out int bytesConsumed, out _) == OperationStatus.NeedMoreData)
                {
                    // A valid magic; the decoder has taken the magic bytes into its session to continue decoding
                    // the rest of the frame. Drop them from the buffer and keep decoding.
                    _buffer.Discard(bytesConsumed);
                    return true;
                }

                // Not a frame: leave the magic bytes buffered so they are included in the trailing-data rewind
                // below (on a non-seekable stream they simply remain unconsumed).
            }

            // Trailing non-zstd data or end of input after the final frame: the stream is complete. Mark the
            // stream ended so subsequent reads short-circuit to 0 without re-entering the (now non-resumable)
            // decoder, and leave any trailing bytes on a seekable base stream by rewinding to the end of the
            // compressed data, mirroring how DeflateStream handles data after the last gzip member.
            _endOfStream = true;
            if (_stream.CanSeek)
            {
                TryRewindStream(_stream);
            }

            return false;
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
                if (_endOfStream)
                {
                    // A previous read reached the end of the final frame (or rejected trailing data). The
                    // boundary probe may have left the decoder non-resumable, so never re-enter the decode
                    // loop; report end-of-stream to all subsequent reads.
                    return 0;
                }

                while (true)
                {
                    int bytesWritten;
                    OperationStatus lastResult;
                    while (!TryDecompress(buffer.Span, out bytesWritten, out lastResult))
                    {
                        _buffer.EnsureAvailableSpace(1);

                        int bytesRead = await _stream.ReadAsync(_buffer.AvailableMemory, cancellationToken).ConfigureAwait(false);
                        if (bytesRead <= 0)
                        {
                            // The underlying stream ended in the middle of a frame, so the data is truncated.
                            // A clean end after a completed frame is reported as Done by TryDecompress and is
                            // resolved by the frame-boundary logic below, not here.
                            if (_nonEmptyInput && !buffer.IsEmpty)
                            {
                                ThrowTruncatedInvalidData();
                            }

                            return 0;
                        }

                        _nonEmptyInput = true;

                        if (bytesRead > _buffer.AvailableLength)
                        {
                            ThrowInvalidStream();
                        }

                        _buffer.Commit(bytesRead);
                    }

                    if (lastResult != OperationStatus.Done || bytesWritten != 0)
                    {
                        // Output to hand back, or not at a finished-frame boundary: return to the caller.
                        return bytesWritten;
                    }

                    // A frame finished with no pending output. A zstd stream may be frames concatenated
                    // back-to-back (RFC 8878 section 3), so decide whether another frame follows before
                    // reporting end-of-stream.
                    if (!await AdvanceToNextFrame(async: true, cancellationToken: cancellationToken).ConfigureAwait(false))
                    {
                        return bytesWritten;
                    }
                }
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
            Span<byte> singleByte = [0];
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
