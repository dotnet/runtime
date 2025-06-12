// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    // Wrapper stream to manually chop writes. This can be useful for simulating partial network transfers
    public class ManualChunkingStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly StreamBuffer _writeBuffer = new StreamBuffer();
        private bool _chunkWrite;

        public ManualChunkingStream(Stream stream, bool chunkWrite)
        {
            _innerStream = stream;
            _chunkWrite = chunkWrite;
        }

        public int PendingWriteLength => _writeBuffer.ReadBytesAvailable;

        public async ValueTask CommitWriteAsync(int length)
        {
            Debug.Assert(length <= PendingWriteLength && length > 0);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(length);

            int read = await _writeBuffer.ReadAsync(buffer.AsMemory(0, length));
            Debug.Assert(read == length);
            await _innerStream.WriteAsync(buffer, 0, read);
            await _innerStream.FlushAsync();

            ArrayPool<byte>.Shared.Return(buffer);
        }

        public void SetWriteChunking(bool chunking)
        {
            _chunkWrite = chunking;
            if (!_chunkWrite && PendingWriteLength > 0)
            {
                // flush pending writes
                byte[] buffer = ArrayPool<byte>.Shared.Rent(PendingWriteLength);

                int read = _writeBuffer.Read(buffer.AsSpan(0, PendingWriteLength));
                Debug.Assert(PendingWriteLength == 0);
                _innerStream.Write(buffer, 0, read);
                _innerStream.Flush();

                ArrayPool<byte>.Shared.Return(buffer);
            }
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

        public override int Read(Span<byte> buffer) => _innerStream.Read(buffer);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(new Memory<byte>(buffer, offset, count)).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _innerStream.ReadAsync(buffer, cancellationToken);

        public override void Write(byte[] buffer, int offset, int count) => Write(new ReadOnlySpan<byte>(buffer, offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_chunkWrite)
                _writeBuffer.Write(buffer);
            else
                _innerStream.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count)).AsTask();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _chunkWrite ? _writeBuffer.WriteAsync(buffer, cancellationToken) : _innerStream.WriteAsync(buffer, cancellationToken);
    }
}
