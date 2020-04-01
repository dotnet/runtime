// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Taken from https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/Mvc.Core/src/Formatters/TranscodingWriteStream.cs

using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    /// <summary>
    /// Adds a transcode-from-UTF-8 layer to the write operations on another stream.
    /// </summary>
    internal sealed partial class TranscodingWriteStream : Stream
    {
        // Default size of the char buffer that will hold the passed-in bytes when decoded from UTF-8.
        // The buffer holds them and then they are encoded to the targetEncoding and written to the underlying stream.
        internal const int MaxCharBufferSize = 4096;
        // Upper bound that limits the byte buffer size to prevent an encoding that has a very poor worst-case scenario.
        internal const int MaxByteBufferSize = 4 * MaxCharBufferSize;

        private readonly Stream _stream;
        private readonly Decoder _decoder;
        private readonly Encoder _encoder;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        private bool _disposed;

        public TranscodingWriteStream(Stream stream, Encoding targetEncoding)
        {
            _stream = stream;

            _charBuffer = ArrayPool<char>.Shared.Rent(MaxCharBufferSize);

            // Attempt to allocate a byte buffer than can tolerate the worst-case scenario for this
            // encoding. This would allow the char -> byte conversion to complete in a single call.
            // However limit the buffer size to prevent an encoding that has a very poor worst-case scenario.
            int maxByteBufferSize = Math.Min(MaxByteBufferSize, targetEncoding.GetMaxByteCount(MaxCharBufferSize));
            _byteBuffer = ArrayPool<byte>.Shared.Rent(maxByteBufferSize);

            _decoder = Encoding.UTF8.GetDecoder();
            _encoder = targetEncoding.GetEncoder();
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get; set; }

        public override void Flush()
            => _stream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => _stream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                ArrayPool<char>.Shared.Return(_charBuffer);
                _charBuffer = null!;

                ArrayPool<byte>.Shared.Return(_byteBuffer);
                _byteBuffer = null!;
            }
        }

        public async ValueTask FinalWriteAsync(CancellationToken cancellationToken)
        {
            // Flush the encoder.
            bool encoderCompleted = false;
            while (!encoderCompleted)
            {
                _encoder.Convert(Array.Empty<char>(), 0, 0, _byteBuffer, 0, _byteBuffer.Length,
                    flush: true, out _, out int bytesUsed, out encoderCompleted);

                await _stream.WriteAsync(_byteBuffer, 0, bytesUsed, cancellationToken).ConfigureAwait(false);
            }
        }

        public void FinalWrite()
        {
            // Flush the encoder.
            bool encoderCompleted = false;
            while (!encoderCompleted)
            {
                _encoder.Convert(Array.Empty<char>(), 0, 0, _byteBuffer, 0, _byteBuffer.Length,
                    flush: true, out _, out int bytesUsed, out encoderCompleted);

                _stream.Write(_byteBuffer, 0, bytesUsed);
            }
        }
    }
}
