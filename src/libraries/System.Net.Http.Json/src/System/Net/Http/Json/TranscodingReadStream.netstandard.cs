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
        private ArraySegment<byte> _byteBuffer;
        private ArraySegment<char> _charBuffer;
        private ArraySegment<byte> _overflowBuffer;

        internal int ByteBufferCount => _byteBuffer.Count;
        internal int CharBufferCount => _charBuffer.Count;
        internal int OverflowCount => _overflowBuffer.Count;

        private void InitializeBuffers()
        {
            _byteBuffer = new ArraySegment<byte>(_pooledBytes, 0, count: 0);
            _charBuffer = new ArraySegment<char>(_pooledChars, 0, count: 0);
            _overflowBuffer = new ArraySegment<byte>(_pooledOverflowBytes, 0, count: 0);
        }

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

            var readBuffer = new ArraySegment<byte>(buffer, offset, count);
            return ReadAsyncCore(readBuffer, cancellationToken);
        }

        private async Task<int> ReadAsyncCore(ArraySegment<byte> readBuffer, CancellationToken cancellationToken)
        {
            if (readBuffer.Count == 0)
            {
                return 0;
            }

            if (_overflowBuffer.Count > 0)
            {
                int bytesToCopy = Math.Min(readBuffer.Count, _overflowBuffer.Count);
                _overflowBuffer.Slice(0, bytesToCopy).CopyTo(readBuffer);

                _overflowBuffer = _overflowBuffer.Slice(bytesToCopy);

                // If we have any overflow bytes, avoid complicating the remainder of the code, by returning as
                // soon as we copy any content.
                return bytesToCopy;
            }

            bool shouldFlushEncoder = false;
            // Only read more content from the input stream if we have exhausted all the buffered chars.
            if (_charBuffer.Count == 0)
            {
                int bytesRead = await ReadInputChars(cancellationToken).ConfigureAwait(false);
                shouldFlushEncoder = bytesRead == 0 && _byteBuffer.Count == 0;
            }

            bool completed = false;
            int charsRead = default;
            int bytesWritten = default;
            // Since Convert() could fail if the destination buffer cannot fit at least one encoded char.
            // If the destination buffer is smaller than GetMaxByteCount(1), we avoid encoding to the destination and we use the overflow buffer instead.
            if (readBuffer.Count > OverflowBufferSize || _charBuffer.Count == 0)
            {
                _encoder.Convert(_charBuffer.Array!, _charBuffer.Offset, _charBuffer.Count, readBuffer.Array!, readBuffer.Offset, readBuffer.Count,
                    flush: shouldFlushEncoder, out charsRead, out bytesWritten, out completed);
            }

            _charBuffer = _charBuffer.Slice(charsRead);

            if (completed || bytesWritten > 0)
            {
                return bytesWritten;
            }

            _encoder.Convert(_charBuffer.Array!, _charBuffer.Offset, _charBuffer.Count, _overflowBuffer.Array!, byteIndex: 0, _overflowBuffer.Array!.Length,
                flush: shouldFlushEncoder, out int overFlowChars, out int overflowBytes, out completed);

            Debug.Assert(overflowBytes > 0 && overFlowChars > 0, "We expect writes to the overflow buffer to always succeed since it is large enough to accommodate at least one char.");

            _charBuffer = _charBuffer.Slice(overFlowChars);

            // readBuffer: [ 0, 0, ], overflowBuffer: [ 7, 13, 34, ]
            // Fill up the readBuffer to capacity, so the result looks like so:
            // readBuffer: [ 7, 13 ], overflowBuffer: [ 34 ]
            Debug.Assert(readBuffer.Count < overflowBytes);
            _overflowBuffer.Array.AsSpan(0, readBuffer.Count).CopyTo(readBuffer);

            Debug.Assert(_overflowBuffer.Array != null);

            _overflowBuffer = new ArraySegment<byte>(_overflowBuffer.Array, readBuffer.Count, overflowBytes - readBuffer.Count);

            Debug.Assert(_overflowBuffer.Count > 0);

            return readBuffer.Count;
        }

        private async ValueTask<int> ReadInputChars(CancellationToken cancellationToken)
        {
            // If we had left-over bytes from a previous read, move it to the start of the buffer and read content into
            // the segment that follows.
            Debug.Assert(_byteBuffer.Array != null);
            Buffer.BlockCopy(_byteBuffer.Array, _byteBuffer.Offset, _byteBuffer.Array, 0, _byteBuffer.Count);

            int offset = _byteBuffer.Count;
            int count = _byteBuffer.Array.Length - _byteBuffer.Count;

            int bytesRead = await _stream.ReadAsync(_byteBuffer.Array, offset, count, cancellationToken).ConfigureAwait(false);

            _byteBuffer = new ArraySegment<byte>(_byteBuffer.Array, 0, offset + bytesRead);

            Debug.Assert(_byteBuffer.Array != null);
            Debug.Assert(_charBuffer.Array != null);
            Debug.Assert(_charBuffer.Count == 0, "We should only expect to read more input chars once all buffered content is read");

            _decoder.Convert(_byteBuffer.Array, _byteBuffer.Offset, _byteBuffer.Count, _charBuffer.Array, charIndex: 0, _charBuffer.Array.Length,
                flush: bytesRead == 0, out int bytesUsed, out int charsUsed, out _);

            // We flush only when the stream is exhausted and there are no pending bytes in the buffer.
            Debug.Assert(bytesRead != 0 || _byteBuffer.Count - bytesUsed == 0);

            _byteBuffer = _byteBuffer.Slice(bytesUsed);
            _charBuffer = new ArraySegment<char>(_charBuffer.Array, 0, charsUsed);

            return bytesRead;
        }
    }
}
