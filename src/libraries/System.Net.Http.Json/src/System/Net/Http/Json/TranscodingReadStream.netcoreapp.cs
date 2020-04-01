// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    internal sealed partial class TranscodingReadStream : Stream
    {
        private Memory<byte> _byteBuffer;
        private Memory<char> _charBuffer;
        private Memory<byte> _overflowBuffer;

        internal int ByteBufferCount => _byteBuffer.Length;
        internal int CharBufferCount => _charBuffer.Length;
        internal int OverflowCount => _overflowBuffer.Length;

        private void InitializeBuffers() { }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public async override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.IsEmpty)
            {
                return 0;
            }

            if (!_overflowBuffer.IsEmpty)
            {
                int bytesToCopy = Math.Min(buffer.Length, _overflowBuffer.Length);

                _overflowBuffer.Slice(0, bytesToCopy).CopyTo(buffer);
                _overflowBuffer = _overflowBuffer.Slice(bytesToCopy);

                // If we have any overflow bytes, avoid complicating the remainder of the code, by returning as
                // soon as we copy any content.
                return bytesToCopy;
            }

            bool shouldFlushEncoder = false;
            // Only read more content from the input stream if we have exhausted all the buffered chars.
            if (_charBuffer.IsEmpty)
            {
                int bytesRead = await ReadInputChars(cancellationToken).ConfigureAwait(false);
                shouldFlushEncoder = bytesRead == 0 && _byteBuffer.Length == 0;
            }

            bool completed = false;
            int charsRead = default;
            int bytesWritten = default;
            // Since Convert() could fail if the destination buffer cannot fit at least one encoded char.
            // If the destination buffer is smaller than GetMaxByteCount(1), we avoid encoding to the destination and we use the overflow buffer instead.
            if (buffer.Length > OverflowBufferSize || _charBuffer.IsEmpty)
            {
                _encoder.Convert(_charBuffer.Span, buffer.Span, flush: shouldFlushEncoder, out charsRead, out bytesWritten, out completed);
            }

            _charBuffer = _charBuffer.Slice(charsRead);

            if (completed || bytesWritten > 0)
            {
                return bytesWritten;
            }

            // If the buffer was too small, transcode to the overflow buffer.
            _overflowBuffer = new Memory<byte>(_pooledOverflowBytes);
            _encoder.Convert(_charBuffer.Span, _overflowBuffer.Span, flush: shouldFlushEncoder, out charsRead, out bytesWritten, out _);
            Debug.Assert(bytesWritten > 0 && charsRead > 0, "We expect writes to the overflow buffer to always succeed since it is large enough to accommodate at least one char.");

            _charBuffer = _charBuffer.Slice(charsRead);
            _overflowBuffer = _overflowBuffer.Slice(0, bytesWritten);

            Debug.Assert(buffer.Length < bytesWritten);
            _overflowBuffer.Slice(0, buffer.Length).CopyTo(buffer);

            _overflowBuffer = _overflowBuffer.Slice(buffer.Length);

            return buffer.Length;
        }

        private async ValueTask<int> ReadInputChars(CancellationToken cancellationToken)
        {
            // If we had left-over bytes from a previous read, move it to the start of the buffer and read content into
            // the space that follows.
            ReadOnlyMemory<byte> previousBytes = _byteBuffer;
            _byteBuffer = new Memory<byte>(_pooledBytes);
            previousBytes.CopyTo(_byteBuffer);

            int bytesRead = await _stream.ReadAsync(_byteBuffer.Slice(previousBytes.Length), cancellationToken).ConfigureAwait(false);

            Debug.Assert(_charBuffer.IsEmpty, "We should only expect to read more input chars once all buffered content is read");

            _charBuffer = new Memory<char>(_pooledChars);
            _byteBuffer = _byteBuffer.Slice(0, previousBytes.Length + bytesRead);

            _decoder.Convert(_byteBuffer.Span, _charBuffer.Span, flush: bytesRead == 0, out int bytesUsed, out int charsUsed, out _);

            // We flush only when the stream is exhausted and there are no pending bytes in the buffer.
            Debug.Assert(bytesRead != 0 || _byteBuffer.Length - bytesUsed == 0);

            _byteBuffer = _byteBuffer.Slice(bytesUsed);
            _charBuffer = _charBuffer.Slice(0, charsUsed);

            return bytesRead;
        }
    }
}
