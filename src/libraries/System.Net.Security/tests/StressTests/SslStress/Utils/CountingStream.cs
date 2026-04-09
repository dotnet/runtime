// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SslStress.Utils
{
    public class StreamCounter
    {
        public long BytesWritten = 0L;
        public long BytesRead = 0L;

        public void Reset()
        {
            BytesWritten = 0L;
            BytesRead = 0L;
        }

        public StreamCounter Append(StreamCounter that)
        {
            BytesRead += that.BytesRead;
            BytesWritten += that.BytesWritten;
            return this;
        }

        public StreamCounter Clone() => new StreamCounter() { BytesRead = BytesRead, BytesWritten = BytesWritten };
    }

    public class CountingStream : Stream
    {
        private readonly Stream _stream;
        private readonly StreamCounter _counter;

        public CountingStream(Stream stream, StreamCounter counters)
        {
            _stream = stream;
            _counter = counters;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            Interlocked.Add(ref _counter.BytesWritten, count);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _stream.Read(buffer, offset, count);
            Interlocked.Add(ref _counter.BytesRead, read);
            return read;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _stream.WriteAsync(buffer, cancellationToken);
            Interlocked.Add(ref _counter.BytesWritten, buffer.Length);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await _stream.ReadAsync(buffer, cancellationToken);
            Interlocked.Add(ref _counter.BytesRead, read);
            return read;
        }

        public override void WriteByte(byte value)
        {
            _stream.WriteByte(value);
            Interlocked.Increment(ref _counter.BytesRead);
        }

        // route everything else to the inner stream

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public override void Flush() => _stream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Close() => _stream.Close();
    }
}
