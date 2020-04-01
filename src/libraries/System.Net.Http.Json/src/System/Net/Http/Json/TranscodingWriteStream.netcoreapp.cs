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

            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Span<char> charBuffer = _charBuffer;
            ReadOnlySpan<byte> bufferCopy = buffer;

            bool decoderCompleted = false;
            while (!decoderCompleted)
            {
                _decoder.Convert(bufferCopy, charBuffer,
                    flush: false, out int bytesDecoded, out int charCount, out decoderCompleted);

                bufferCopy = bufferCopy.Slice(bytesDecoded);

                ReadOnlySpan<char> encoderCharBuffer = charBuffer.Slice(0, charCount);
                Span<byte> encoderByteBuffer = _byteBuffer;

                bool encoderCompleted = false;
                while (!encoderCompleted && encoderCharBuffer.Length > 0)
                {
                    _encoder.Convert(encoderCharBuffer, encoderByteBuffer,
                        flush: false, out int charsUsed, out int bytesUsed, out encoderCompleted);

                    _stream.Write(encoderByteBuffer.Slice(0, bytesUsed));
                    encoderCharBuffer = encoderCharBuffer.Slice(charsUsed);
                }
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

            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Memory<char> charBuffer = _charBuffer;
            ReadOnlyMemory<byte> bufferCopy = buffer;

            bool decoderCompleted = false;
            while (!decoderCompleted)
            {
                _decoder.Convert(bufferCopy.Span, charBuffer.Span,
                    flush: false, out int bytesDecoded, out int charCount, out decoderCompleted);

                bufferCopy = bufferCopy.Slice(bytesDecoded);

                ReadOnlyMemory<char> encoderCharBuffer = charBuffer.Slice(0, charCount);
                Memory<byte> encoderByteBuffer = _byteBuffer;

                bool encoderCompleted = false;
                while (!encoderCompleted && encoderCharBuffer.Length > 0)
                {
                    _encoder.Convert(encoderCharBuffer.Span, encoderByteBuffer.Span,
                        flush: false, out int charsUsed, out int bytesUsed, out encoderCompleted);

                    await _stream.WriteAsync(encoderByteBuffer.Slice(0, bytesUsed), cancellationToken).ConfigureAwait(false);
                    encoderCharBuffer = encoderCharBuffer.Slice(charsUsed);
                }
            }
        }
    }
}
