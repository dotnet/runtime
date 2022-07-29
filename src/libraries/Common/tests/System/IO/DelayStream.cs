// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides a stream that delays all read and write operations by a specified period of time.</summary>
    internal sealed class DelayStream : Stream
    {
        private readonly Stream _innerStream;
        private int _millisecondsDelay;

        public DelayStream(Stream innerStream, int millisecondsDelay = 0)
        {
            _innerStream = innerStream;
            _millisecondsDelay = millisecondsDelay;
        }

        protected override void Dispose(bool disposing) { if (disposing) _innerStream.Dispose(); }
        public override ValueTask DisposeAsync() => _innerStream.DisposeAsync();

        public int DelayMilliseconds { get => _millisecondsDelay; set => _millisecondsDelay = value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Thread.Sleep(_millisecondsDelay);
            return _innerStream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            Thread.Sleep(_millisecondsDelay);
            return _innerStream.Read(buffer);
        }

        public override int ReadByte()
        {
            Thread.Sleep(_millisecondsDelay);
            return _innerStream.ReadByte();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);

        public override int EndRead(IAsyncResult asyncResult) => TaskToApm.End<int>(asyncResult);

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(_millisecondsDelay, cancellationToken);
            return await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_millisecondsDelay, cancellationToken);
            return await _innerStream.ReadAsync(buffer, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(_millisecondsDelay, cancellationToken);
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_millisecondsDelay, cancellationToken);
            await _innerStream.WriteAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);

        public override void EndWrite(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        public override void Write(byte[] buffer, int offset, int count)
        {
            Thread.Sleep(_millisecondsDelay);
            _innerStream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Thread.Sleep(_millisecondsDelay);
            _innerStream.Write(buffer);
        }

        public override void WriteByte(byte value)
        {
            Thread.Sleep(_millisecondsDelay);
            _innerStream.WriteByte(value);
        }

        public override void Flush()
        {
            Thread.Sleep(_millisecondsDelay);
            _innerStream.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(_millisecondsDelay, cancellationToken);
            await _innerStream.FlushAsync(cancellationToken);
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanWrite => _innerStream.CanWrite;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanTimeout => _innerStream.CanTimeout;

        public override long Length => _innerStream.Length;
        public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);

        public override int ReadTimeout { get => _innerStream.ReadTimeout; set => _innerStream.ReadTimeout = value; }
        public override int WriteTimeout { get => _innerStream.WriteTimeout; set => _innerStream.WriteTimeout = value; }
    }
}
