// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    internal sealed class TranscodingWriteStream : Stream
    {
        internal const int MaxCharBufferSize = 4096;
        internal const int MaxByteBufferSize = 4 * MaxCharBufferSize;
        private readonly int _maxByteBufferSize;

        private readonly Stream _stream;
        private readonly Decoder _decoder;
        private readonly Encoder _encoder;
        private readonly char[] _charBuffer;
        private int _charsDecoded;
        private bool _disposed;

        public TranscodingWriteStream(Stream stream, Encoding targetEncoding)
        {
            _stream = stream;

            _charBuffer = ArrayPool<char>.Shared.Rent(MaxCharBufferSize);

            // Attempt to allocate a byte buffer than can tolerate the worst-case scenario for this
            // encoding. This would allow the char -> byte conversion to complete in a single call.
            // However limit the buffer size to prevent an encoding that has a very poor worst-case scenario.
            _maxByteBufferSize = Math.Min(MaxByteBufferSize, targetEncoding.GetMaxByteCount(MaxCharBufferSize));

            _decoder = Encoding.UTF8.GetDecoder();
            _encoder = targetEncoding.GetEncoder();
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get; set; }

        public override void Flush()
            => throw new NotSupportedException();

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowArgumentException(buffer, offset, count);
            ArraySegment<byte> bufferSegment = new ArraySegment<byte>(buffer, offset, count);
            return WriteAsync(bufferSegment, cancellationToken);
        }

        private async Task WriteAsync(
            ArraySegment<byte> bufferSegment,
            CancellationToken cancellationToken)
        {
            bool decoderCompleted = false;

            while (!decoderCompleted)
            {
#if NETSTANDARD2_0
                _decoder.Convert(bufferSegment.Array, bufferSegment.Offset, bufferSegment.Count, _charBuffer, _charsDecoded, _charBuffer.Length - _charsDecoded,
                    flush: false, out int bytesDecoded, out int charsDecoded, out decoderCompleted);
#else
                _decoder.Convert(
                    bufferSegment,
                    _charBuffer.AsSpan(_charsDecoded),
                    flush: false,
                    out int bytesDecoded,
                    out int charsDecoded,
                    out decoderCompleted);
#endif

                _charsDecoded += charsDecoded;
                bufferSegment = bufferSegment.Slice(bytesDecoded);

                if (!decoderCompleted)
                {
                    await WriteBufferAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task WriteBufferAsync(CancellationToken cancellationToken)
        {
            bool encoderCompleted = false;
            int charsWritten = 0;
            byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(_maxByteBufferSize);

            while (!encoderCompleted && charsWritten < _charsDecoded)
            {
#if NETSTANDARD2_0
                _encoder.Convert(_charBuffer, charsWritten, _charsDecoded - charsWritten, byteBuffer, 0, byteBuffer.Length,
                    flush: false, out int charsEncoded, out int bytesUsed, out encoderCompleted);

                await _stream.WriteAsync(byteBuffer, 0, bytesUsed, cancellationToken).ConfigureAwait(false);
#else
                _encoder.Convert(
                    _charBuffer.AsSpan(charsWritten, _charsDecoded - charsWritten),
                    byteBuffer,
                    flush: false,
                    out int charsEncoded,
                    out int bytesUsed,
                    out encoderCompleted);

                await _stream.WriteAsync(byteBuffer.AsMemory(0, bytesUsed), cancellationToken).ConfigureAwait(false);
#endif

                charsWritten += charsEncoded;
            }

            ArrayPool<byte>.Shared.Return(byteBuffer);

            // At this point, we've written all the buffered chars to the underlying Stream.
            _charsDecoded = 0;
        }

        private static void ThrowArgumentException(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                ArrayPool<char>.Shared.Return(_charBuffer);
            }
        }

        public async Task FinalWriteAsync(CancellationToken cancellationToken)
        {
            // First write any buffered content
            await WriteBufferAsync(cancellationToken).ConfigureAwait(false);

            // Now flush the encoder.
            byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(_maxByteBufferSize);
            bool encoderCompleted = false;

            while (!encoderCompleted)
            {
#if NETSTANDARD2_0
                _encoder.Convert(Array.Empty<char>(), 0, 0, byteBuffer, 0, byteBuffer.Length,
                    flush: true, out _, out int bytesUsed, out encoderCompleted);

                await _stream.WriteAsync(byteBuffer, 0, bytesUsed, cancellationToken).ConfigureAwait(false);
#else
                _encoder.Convert(
                    Array.Empty<char>(),
                    byteBuffer,
                    flush: true,
                    out _,
                    out int bytesUsed,
                    out encoderCompleted);

                await _stream.WriteAsync(byteBuffer.AsMemory(0, bytesUsed), cancellationToken).ConfigureAwait(false);
#endif
            }

            ArrayPool<byte>.Shared.Return(byteBuffer);
        }
    }
}
