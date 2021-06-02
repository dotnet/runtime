// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    // Wrapper stream to artificially chop reads and writes to smaller chunks.
    public class RandomReadWriteSizeStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly int _maxChunkSize;

        public RandomReadWriteSizeStream(Stream stream, int maxChunkSize = int.MaxValue)
        {
            _innerStream = stream;
            _maxChunkSize = maxChunkSize;
        }

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanTimeout => _innerStream.CanTimeout;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void SetLength(long value) => _innerStream.SetLength(value);

        public override long Length => _innerStream.Length;

        public override void Flush() => _innerStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

        public override int Read(byte[] buffer, int offset, int count) => Read(new Span<byte>(buffer, offset, count));
        public override int Read(Span<byte> buffer)
        {
            if (buffer.Length > 0)
            {
                int readLength = RandomNumberGenerator.GetInt32(1, Math.Min(buffer.Length + 1, _maxChunkSize));
                buffer = buffer.Slice(0, readLength);
            }

            return _innerStream.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(new Memory<byte>(buffer, offset, count)).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.Length > 0)
            {
                int readLength = RandomNumberGenerator.GetInt32(1, Math.Min(buffer.Length + 1, _maxChunkSize));
                buffer = buffer.Slice(0, readLength);
            }
            return _innerStream.ReadAsync(buffer, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(new ReadOnlySpan<byte>(buffer, offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                int writeLength = RandomNumberGenerator.GetInt32(buffer.Length + 1);
                _innerStream.Write(buffer.Slice(0, writeLength));
                buffer = buffer.Slice(writeLength);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count)).AsTask();
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            while (buffer.Length > 0)
            {
                int writeLength = RandomNumberGenerator.GetInt32(buffer.Length + 1);
                await _innerStream.WriteAsync(buffer.Slice(0, writeLength), cancellationToken);
                buffer = buffer.Slice(writeLength);
            }
        }
    }
}
