// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Taken from https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/Mvc.Core/src/Formatters/TranscodingReadStream.cs

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json
{
    /// <summary>
    /// Adds a transcode-to-UTF-8 layer to the read operations on another stream.
    /// </summary>
    internal sealed partial class TranscodingReadStream : Stream
    {
        private static readonly int OverflowBufferSize = Encoding.UTF8.GetMaxByteCount(1); // The most number of bytes used to represent a single UTF char

        // Default size of the buffer that will hold the bytes from the underlying stream.
        // Those bytes are expected to be encoded in the sourceEncoding passed into the .ctor.
        internal const int MaxByteBufferSize = 4096;

        private readonly Stream _stream;
        private readonly Decoder _decoder;
        private readonly Encoder _encoder;

        private byte[] _pooledBytes;
        private byte[] _pooledOverflowBytes;
        private char[] _pooledChars;

        private bool _disposed;

        public TranscodingReadStream(Stream input, Encoding sourceEncoding)
        {
            _stream = input;

            // The "count" in the buffer is the size of any content from a previous read.
            // Initialize them to 0 since nothing has been read so far.
            _pooledBytes = ArrayPool<byte>.Shared.Rent(MaxByteBufferSize);

            // Attempt to allocate a char buffer than can tolerate the worst-case scenario for this
            // encoding. This would allow the byte -> char conversion to complete in a single call.
            // The conversion process is tolerant of char buffer that is not large enough to convert all the bytes at once.
            int maxCharBufferSize = sourceEncoding.GetMaxCharCount(MaxByteBufferSize);
            _pooledChars = ArrayPool<char>.Shared.Rent(maxCharBufferSize);

            _pooledOverflowBytes = ArrayPool<byte>.Shared.Rent(OverflowBufferSize);

            InitializeBuffers();

            _decoder = sourceEncoding.GetDecoder();
            _encoder = Encoding.UTF8.GetEncoder();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override void Flush()
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                Debug.Assert(_pooledChars != null);
                ArrayPool<char>.Shared.Return(_pooledChars);
                _pooledChars = null!;

                Debug.Assert(_pooledBytes != null);
                ArrayPool<byte>.Shared.Return(_pooledBytes);
                _pooledBytes = null!;

                Debug.Assert(_pooledOverflowBytes != null);
                ArrayPool<byte>.Shared.Return(_pooledOverflowBytes);
                _pooledOverflowBytes = null!;

                _stream.Dispose();
            }
        }
    }
}
