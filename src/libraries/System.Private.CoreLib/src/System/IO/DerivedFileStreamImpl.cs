// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // this type exists so we can avoid GetType() != typeof(FileStream) checks in FileStream
    // when FileStream was supposed to call base.Method() for such cases, we just call _fileStream.BaseMethod()
    // for everything else we fall back to the actual strategy (like FileStream does)
    //
    // it's crucial to NOT use the "base" keyoword here! everything must be using _fileStream or _impl
    internal sealed class DerivedFileStreamImpl : FileStreamStrategy
    {
        private readonly FileStreamStrategy _impl;

        internal DerivedFileStreamImpl(FileStream fileStream, FileStreamStrategy impl) : base(fileStream) => _impl = impl;

        public override bool CanRead => _impl.CanRead;

        public override bool CanWrite => _impl.CanWrite;

        public override bool CanSeek => _impl.CanSeek;

        public override long Length => _impl.Length;

        public override long Position
        {
            get => _impl.Position;
            set => _impl.Position = value;
        }

        internal override bool IsAsync => _impl.IsAsync;

        internal override string Name => _impl.Name;

        internal override IntPtr Handle => _impl.Handle;

        internal override SafeFileHandle SafeFileHandle => _impl.SafeFileHandle;

        internal override bool IsClosed => _impl.IsClosed;

        internal override void Lock(long position, long length) => _impl.Lock(position, length);

        internal override void Unlock(long position, long length) => _impl.Unlock(position, length);

        public override long Seek(long offset, SeekOrigin origin) => _impl.Seek(offset, origin);

        public override void SetLength(long value) => _impl.SetLength(value);

        public override int ReadByte() => _impl.ReadByte();

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => _impl.BeginRead(buffer, offset, count, callback, state);

        public override int Read(byte[] buffer, int offset, int count) => _impl.Read(buffer, offset, count);

        // This type is derived from FileStream and/or the stream is in async mode.  If this is a
        // derived type, it may have overridden Read(byte[], int, int) prior to this Read(Span<byte>)
        // overload being introduced.  In that case, this Read(Span<byte>) overload should use the behavior
        // of Read(byte[],int,int) overload.  Or if the stream is in async mode, we can't call the
        // synchronous ReadSpan, so we similarly call the base Read, which will turn delegate to
        // Read(byte[],int,int), which will do the right thing if we're in async mode.
        public override int Read(Span<byte> buffer)
            => _fileStream.BaseRead(buffer);

        // If we have been inherited into a subclass, the following implementation could be incorrect
        // since it does not call through to Read() which a subclass might have overridden.
        // To be safe we will only use this implementation in cases where we know it is safe to do so,
        // and delegate to our base class (which will call into Read/ReadAsync) when we are not sure.
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _fileStream.BaseReadAsync(buffer, offset, count, cancellationToken);

        // If this isn't a concrete FileStream, a derived type may have overridden ReadAsync(byte[],...),
        // which was introduced first, so delegate to the base which will delegate to that.
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _fileStream.BaseReadAsync(buffer, cancellationToken);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => _impl.BeginRead(buffer, offset, count, callback, state);

        public override void WriteByte(byte value) => _impl.WriteByte(value);

        public override void Write(byte[] buffer, int offset, int count) => _impl.Write(buffer, offset, count);

        // This type is derived from FileStream and/or the stream is in async mode.  If this is a
        // derived type, it may have overridden Write(byte[], int, int) prior to this Write(ReadOnlySpan<byte>)
        // overload being introduced.  In that case, this Write(ReadOnlySpan<byte>) overload should use the behavior
        // of Write(byte[],int,int) overload.  Or if the stream is in async mode, we can't call the
        // synchronous WriteSpan, so we similarly call the base Write, which will turn delegate to
        // Write(byte[],int,int), which will do the right thing if we're in async mode.
        public override void Write(ReadOnlySpan<byte> buffer)
            => _fileStream.BaseWrite(buffer);

        // If we have been inherited into a subclass, the following implementation could be incorrect
        // since it does not call through to Write() or WriteAsync() which a subclass might have overridden.
        // To be safe we will only use this implementation in cases where we know it is safe to do so,
        // and delegate to our base class (which will call into Write/WriteAsync) when we are not sure.
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _fileStream.BaseWriteAsync(buffer, offset, count, cancellationToken);

        // If this isn't a concrete FileStream, a derived type may have overridden WriteAsync(byte[],...),
        // which was introduced first, so delegate to the base which will delegate to that.
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => _fileStream.BaseWriteAsync(buffer, cancellationToken);

        public override void Flush() => _impl.Flush();

        internal override void Flush(bool flushToDisk) => _impl.Flush(flushToDisk);

        // If we have been inherited into a subclass, the following implementation could be incorrect
        // since it does not call through to Flush() which a subclass might have overridden.  To be safe
        // we will only use this implementation in cases where we know it is safe to do so,
        // and delegate to our base class (which will call into Flush) when we are not sure.
        public override Task FlushAsync(CancellationToken cancellationToken)
            => _fileStream.BaseFlushAsync(cancellationToken);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => _fileStream.BaseCopyToAsync(destination, bufferSize, cancellationToken);

        public override ValueTask DisposeAsync() => _fileStream.BaseDisposeAsync();

        internal override void DisposeInternal(bool disposing) => _impl.DisposeInternal(disposing);
    }
}
