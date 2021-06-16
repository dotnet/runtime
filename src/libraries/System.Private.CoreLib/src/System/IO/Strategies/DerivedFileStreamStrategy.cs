// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // this type exists so we can avoid GetType() != typeof(FileStream) checks in FileStream
    // when FileStream was supposed to call base.Method() for such cases, we just call _fileStream.BaseMethod()
    // for everything else we fall back to the actual strategy (like FileStream does)
    //
    // it's crucial to NOT use the "base" keyoword here! everything must be using _fileStream or _strategy
    internal sealed class DerivedFileStreamStrategy : FileStreamStrategy
    {
        private readonly FileStreamStrategy _strategy;
        private readonly FileStream _fileStream;

        internal DerivedFileStreamStrategy(FileStream fileStream, FileStreamStrategy strategy)
        {
            _fileStream = fileStream;
            _strategy = strategy;
        }

        ~DerivedFileStreamStrategy()
        {
            // Preserved for compatibility since FileStream has defined a
            // finalizer in past releases and derived classes may depend
            // on Dispose(false) call.
            _fileStream.DisposeInternal(false);
        }

        public override bool CanRead => _strategy.CanRead;

        public override bool CanWrite => _strategy.CanWrite;

        public override bool CanSeek => _strategy.CanSeek;

        public override long Length => _strategy.Length;

        public override long Position
        {
            get => _strategy.Position;
            set => _strategy.Position = value;
        }

        internal override bool IsAsync => _strategy.IsAsync;

        internal override string Name => _strategy.Name;

        internal override SafeFileHandle SafeFileHandle
        {
            get
            {
                _fileStream.Flush(false);
                return _strategy.SafeFileHandle;
            }
        }

        internal override bool IsClosed => _strategy.IsClosed;

        internal override void Lock(long position, long length) => _strategy.Lock(position, length);

        internal override void Unlock(long position, long length) => _strategy.Unlock(position, length);

        public override long Seek(long offset, SeekOrigin origin) => _strategy.Seek(offset, origin);

        public override void SetLength(long value) => _strategy.SetLength(value);

        public override int ReadByte() => _strategy.ReadByte();

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => _strategy.IsAsync
                ? _strategy.BeginRead(buffer, offset, count, callback, state)
                : _fileStream.BaseBeginRead(buffer, offset, count, callback, state);

        public override int EndRead(IAsyncResult asyncResult)
            => _strategy.IsAsync ? _strategy.EndRead(asyncResult) : _fileStream.BaseEndRead(asyncResult);

        public override int Read(byte[] buffer, int offset, int count) => _strategy.Read(buffer, offset, count);

        // If this is a derived type, it may have overridden Read(byte[], int, int) prior to this Read(Span<byte>)
        // overload being introduced.  In that case, this Read(Span<byte>) overload should use the behavior
        // of Read(byte[],int,int) overload.
        public override int Read(Span<byte> buffer)
            => _fileStream.BaseRead(buffer);

        // If we have been inherited into a subclass, the Strategy implementation could be incorrect
        // since it does not call through to Read() which a subclass might have overridden.
        // To be safe we will only use this implementation in cases where we know it is safe to do so,
        // and delegate to FileStream base class (which will call into Read/ReadAsync) when we are not sure.
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _fileStream.BaseReadAsync(buffer, offset, count, cancellationToken);

        // If this isn't a concrete FileStream, a derived type may have overridden ReadAsync(byte[],...),
        // which was introduced first, so delegate to the base which will delegate to that.
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _fileStream.BaseReadAsync(buffer, cancellationToken);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => _strategy.IsAsync
                ? _strategy.BeginWrite(buffer, offset, count, callback, state)
                : _fileStream.BaseBeginWrite(buffer, offset, count, callback, state);

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (_strategy.IsAsync)
            {
                _strategy.EndWrite(asyncResult);
            }
            else
            {
                _fileStream.BaseEndWrite(asyncResult);
            }
        }

        public override void WriteByte(byte value) => _strategy.WriteByte(value);

        public override void Write(byte[] buffer, int offset, int count) => _strategy.Write(buffer, offset, count);

        // If this is a derived type, it may have overridden Write(byte[], int, int) prior to this Write(ReadOnlySpan<byte>)
        // overload being introduced. In that case, this Write(ReadOnlySpan<byte>) overload should use the behavior
        // of Write(byte[],int,int) overload.
        public override void Write(ReadOnlySpan<byte> buffer)
            => _fileStream.BaseWrite(buffer);

        // If we have been inherited into a subclass, the Strategy implementation could be incorrect
        // since it does not call through to Write() or WriteAsync() which a subclass might have overridden.
        // To be safe we will only use this implementation in cases where we know it is safe to do so,
        // and delegate to our base class (which will call into Write/WriteAsync) when we are not sure.
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _fileStream.BaseWriteAsync(buffer, offset, count, cancellationToken);

        // If this isn't a concrete FileStream, a derived type may have overridden WriteAsync(byte[],...),
        // which was introduced first, so delegate to the base which will delegate to that.
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => _fileStream.BaseWriteAsync(buffer, cancellationToken);

        public override void Flush() => throw new InvalidOperationException("FileStream should never call this method.");

        internal override void Flush(bool flushToDisk) => _strategy.Flush(flushToDisk);

        // If we have been inherited into a subclass, the following implementation could be incorrect
        // since it does not call through to Flush() which a subclass might have overridden.  To be safe
        // we will only use this implementation in cases where we know it is safe to do so,
        // and delegate to our base class (which will call into Flush) when we are not sure.
        public override Task FlushAsync(CancellationToken cancellationToken)
            => _fileStream.BaseFlushAsync(cancellationToken);

        // We also need to take this path if this is a derived
        // instance from FileStream, as a derived type could have overridden ReadAsync, in which
        // case our custom CopyToAsync implementation isn't necessarily correct.
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => _fileStream.BaseCopyToAsync(destination, bufferSize, cancellationToken);

        public override ValueTask DisposeAsync() => _fileStream.BaseDisposeAsync();

        internal override void DisposeInternal(bool disposing)
        {
            _strategy.DisposeInternal(disposing);

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
