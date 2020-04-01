// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    internal sealed partial class TranscodingWriteStream : Stream
    {
        public override void Write(byte[] buffer, int offset, int count)
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

            var bufferSegment = new ArraySegment<byte>(buffer, offset, count);

            int charCount = 0;
            bool decoderCompleted = false;
            while (!decoderCompleted)
            {
                _decoder.Convert(bufferSegment.Array!, bufferSegment.Offset, bufferSegment.Count, _charBuffer, charCount, _charBuffer.Length - charCount,
                    flush: false, out int bytesDecoded, out int charsDecoded, out decoderCompleted);

                charCount += charsDecoded;
                bufferSegment = bufferSegment.Slice(bytesDecoded);

                int charsWritten = 0;
                bool encoderCompleted = false;
                while (!encoderCompleted && charsWritten < charCount)
                {
                    _encoder.Convert(_charBuffer, charsWritten, charCount - charsWritten, _byteBuffer, 0, _byteBuffer.Length,
                        flush: false, out int charsEncoded, out int bytesUsed, out encoderCompleted);

                    _stream.Write(_byteBuffer, 0, bytesUsed);
                    charsWritten += charsEncoded;
                }

                // At this point, we've written all the buffered chars to the underlying Stream.
                charCount = 0;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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

            var bufferSegment = new ArraySegment<byte>(buffer, offset, count);
            return WriteAsyncCore(bufferSegment, cancellationToken);
        }

        private async Task WriteAsyncCore(ArraySegment<byte> bufferSegment, CancellationToken cancellationToken)
        {
            int charCount = 0;
            bool decoderCompleted = false;
            while (!decoderCompleted)
            {
                _decoder.Convert(bufferSegment.Array!, bufferSegment.Offset, bufferSegment.Count, _charBuffer, charCount, _charBuffer.Length - charCount,
                    flush: false, out int bytesDecoded, out int charsDecoded, out decoderCompleted);

                charCount += charsDecoded;
                bufferSegment = bufferSegment.Slice(bytesDecoded);

                int charsWritten = 0;
                bool encoderCompleted = false;
                while (!encoderCompleted && charsWritten < charCount)
                {
                    _encoder.Convert(_charBuffer, charsWritten, charCount - charsWritten, _byteBuffer, 0, _byteBuffer.Length,
                        flush: false, out int charsEncoded, out int bytesUsed, out encoderCompleted);

                    await _stream.WriteAsync(_byteBuffer, 0, bytesUsed, cancellationToken).ConfigureAwait(false);
                    charsWritten += charsEncoded;
                }

                // At this point, we've written all the buffered chars to the underlying Stream.
                charCount = 0;
            }
        }
    }
}
