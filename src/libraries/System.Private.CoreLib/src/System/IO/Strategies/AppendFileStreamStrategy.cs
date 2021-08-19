// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // this type serves some basic functionality that is common for native OS File Stream Strategies
    internal sealed class AppendFileStreamStrategy : FileStreamStrategy
    {
        // To write to the end of file:
        // On Windows, we need to pass 0xFFFFFFFFFFFFFFFF as offset to writefile() to append to EOF.
        // On Unix, if the file is opened with O_APPEND the offset is ignored and we use write anyway.
        private const long WriteOffset = unchecked((long)ulong.MaxValue);
        private readonly SafeFileHandle _fileHandle; // only ever null if ctor throws

        internal AppendFileStreamStrategy(SafeFileHandle handle, FileAccess access)
        {
            Debug.Assert(handle.IsAppend);

            handle.EnsureThreadPoolBindingInitialized();
            _fileHandle = handle;
        }

        internal AppendFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            _fileHandle = SafeFileHandle.Open(Path.GetFullPath(path), mode, access, share, options, preallocationSize);
        }

        public override bool CanSeek => _fileHandle.CanSeek;

        public override bool CanRead => false; // this is how FileMode.Append works

        public override bool CanWrite => !_fileHandle.IsClosed;

        public override long Length => RandomAccess.GetFileLength(_fileHandle);

        public override long Position
        {
            get => Length;
            set => Seek(value, SeekOrigin.Begin);
        }

        internal override bool IsAsync => _fileHandle.IsAsync;

        internal override string Name => _fileHandle.Path ?? SR.IO_UnknownFileName;

        internal override bool IsClosed => _fileHandle.IsClosed;

        internal override SafeFileHandle SafeFileHandle => _fileHandle;

        public override ValueTask DisposeAsync()
        {
            if (_fileHandle != null && !_fileHandle.IsClosed)
            {
                _fileHandle.ThreadPoolBinding?.Dispose();
                _fileHandle.Dispose();
            }

            return ValueTask.CompletedTask;
        }

        internal override void DisposeInternal(bool disposing) => Dispose(disposing);

        protected override void Dispose(bool disposing)
        {
            if (disposing && _fileHandle != null && !_fileHandle.IsClosed)
            {
                _fileHandle.ThreadPoolBinding?.Dispose();
                _fileHandle.Dispose();
            }
        }

        public override void Flush() { }  // no buffering = nothing to flush

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask; // no buffering = nothing to flush

        internal override void Flush(bool flushToDisk)
        {
            if (flushToDisk && CanWrite)
            {
                FileStreamHelpers.FlushToDisk(_fileHandle);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin < SeekOrigin.Begin || origin > SeekOrigin.End)
            {
                throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin));
            }
            else if (!CanSeek)
            {
                if (_fileHandle.IsClosed)
                {
                    ThrowHelper.ThrowObjectDisposedException_FileClosed();
                }

                ThrowHelper.ThrowNotSupportedException_UnseekableStream();
            }

            long length = Length;
            long newLength = origin == SeekOrigin.Begin ? offset : length + offset;

            if (newLength < 0)
            {
                // keep throwing the same exception we did when seek was causing actual offset change
                FileStreamHelpers.ThrowInvalidArgument(_fileHandle);
            }

            // Prevent users from overwriting data in a file that was opened in append mode.
            if (newLength < length)
            {
                throw new IOException(SR.IO_SeekAppendOverwrite);
            }

            FileStreamHelpers.SetFileLength(_fileHandle, newLength);

            return newLength;
        }

        internal override void Lock(long position, long length) => FileStreamHelpers.Lock(_fileHandle, CanWrite, position, length);

        internal override void Unlock(long position, long length) => FileStreamHelpers.Unlock(_fileHandle, position, length);

        public override void SetLength(long value)
        {
            if (value < Length)
            {
                throw new IOException(SR.IO_SetLengthAppendTruncate);
            }

            FileStreamHelpers.SetFileLength(_fileHandle, value);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => throw new NotSupportedException(SR.NotSupported_UnreadableStream);

        public override int EndRead(IAsyncResult asyncResult)
            => throw new NotSupportedException(SR.NotSupported_UnreadableStream);

        public override int ReadByte()
            => throw new NotSupportedException(SR.NotSupported_UnreadableStream);

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException(SR.NotSupported_UnreadableStream);

        public override int Read(Span<byte> buffer)
            => throw new NotSupportedException(SR.NotSupported_UnreadableStream);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException(SR.NotSupported_UnreadableStream);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new NotSupportedException(SR.NotSupported_UnreadableStream);

        public override unsafe void WriteByte(byte value)
            => Write(new ReadOnlySpan<byte>(&value, 1));

        public override void Write(byte[] buffer, int offset, int count)
            => Write(new ReadOnlySpan<byte>(buffer, offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
            => RandomAccess.WriteAtOffset(_fileHandle, buffer, WriteOffset);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);

        public override void EndWrite(IAsyncResult asyncResult)
            => TaskToApm.End(asyncResult);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
            => RandomAccess.WriteAtOffsetAsync(_fileHandle, source, WriteOffset, cancellationToken);
    }
}
