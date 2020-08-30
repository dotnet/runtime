// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections.Tests
{
    internal class MockStream : Stream
    {
        public Func<Memory<byte>, CancellationToken, ValueTask<int>> OnReadAsync { get; set; }
        public Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> OnWriteAsync { get; set; }
        public Func<CancellationToken, Task> OnFlushAsync { get; set; }
        public Func<ValueTask> OnDisposeAsync { get; set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public override ValueTask DisposeAsync()
        {
            return OnDisposeAsync();
        }

        public override void Flush()
        {
            FlushAsync().GetAwaiter().GetResult();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return OnFlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return OnReadAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return OnWriteAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToApm.End(asyncResult);
        }
    }
}
