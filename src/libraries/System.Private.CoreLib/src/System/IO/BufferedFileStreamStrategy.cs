// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // this type exists so we can avoid duplicating the buffering logic in every FileStreamStrategy implementation
    // for simple properties that would just call the wrapped stream properties, we call strategy directly
    // for everything else, we are calling BufferedStream methods that take care of all the buffering work
    internal sealed class BufferedFileStreamStrategy : FileStreamStrategy
    {
        private readonly FileStreamStrategy _strategy;
        private readonly BufferedStream _bufferedStream;

        internal BufferedFileStreamStrategy(FileStreamStrategy strategy, int bufferSize)
        {
            _strategy = strategy;
            _bufferedStream = new BufferedStream(strategy, bufferSize, actLikeFileStream: true);
        }

        ~BufferedFileStreamStrategy() => DisposeInternal(false);

        public override bool CanRead => _strategy.CanRead;

        public override bool CanWrite => _strategy.CanWrite;

        public override bool CanSeek => _strategy.CanSeek;

        public override long Length => _bufferedStream.GetLengthWithoutFlushing();

        public override long Position
        {
            get => _bufferedStream.GetPositionWithoutFlushing();
            set => _bufferedStream.Position = value;
        }

        internal override bool IsAsync => _strategy.IsAsync;

        internal override string Name => _strategy.Name;

        internal override SafeFileHandle SafeFileHandle
        {
            get
            {
                _bufferedStream.Flush();
                return _strategy.SafeFileHandle;
            }
        }

        internal override bool IsClosed => _strategy.IsClosed;

        internal override void Lock(long position, long length) => _strategy.Lock(position, length);

        internal override void Unlock(long position, long length) => _strategy.Unlock(position, length);

        public override long Seek(long offset, SeekOrigin origin) => _bufferedStream.Seek(offset, origin);

        public override void SetLength(long value) => _bufferedStream.SetLength(value);

        public override int ReadByte() => _bufferedStream.ReadByte();

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => _bufferedStream.BeginRead(buffer, offset, count, callback, state);

        public override int EndRead(IAsyncResult asyncResult)
            => _bufferedStream.EndRead(asyncResult);

        public override int Read(byte[] buffer, int offset, int count) => _bufferedStream.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer) => _bufferedStream.Read(buffer);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _bufferedStream.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _bufferedStream.ReadAsync(buffer, cancellationToken);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => _bufferedStream.BeginWrite(buffer, offset, count, callback, state);

        public override void EndWrite(IAsyncResult asyncResult)
            => _bufferedStream.EndWrite(asyncResult);

        public override void WriteByte(byte value) => _bufferedStream.WriteByte(value);

        public override void Write(byte[] buffer, int offset, int count) => _bufferedStream.Write(buffer, offset, count);

        public override void Write(ReadOnlySpan<byte> buffer) => _bufferedStream.Write(buffer);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _bufferedStream.WriteAsync(buffer, offset, count, cancellationToken);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => _bufferedStream.WriteAsync(buffer, cancellationToken);

        public override void Flush() => _bufferedStream.Flush();

        internal override void Flush(bool flushToDisk) => _bufferedStream.Flush(flushToDisk);

        public override Task FlushAsync(CancellationToken cancellationToken)
            =>  _bufferedStream.FlushAsync(cancellationToken);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => _bufferedStream.CopyToAsync(destination, bufferSize, cancellationToken);

        public override ValueTask DisposeAsync()
            => _bufferedStream.DisposeAsync();

        internal override void DisposeInternal(bool disposing)
        {
            try
            {
                // the finalizer must at least try to flush the write buffer
                // so we enforce it by passing always true
                _bufferedStream.DisposeInternal(true);
            }
            catch (Exception e) when (!disposing && FileStream.IsIoRelatedException(e))
            {
                // On finalization, ignore failures from trying to flush the write buffer,
                // e.g. if this stream is wrapping a pipe and the pipe is now broken.
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
