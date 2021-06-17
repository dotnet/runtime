// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed partial class AsyncWindowsFileStreamStrategy : WindowsFileStreamStrategy
    {
        internal AsyncWindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access, FileShare share)
            : base(handle, access, share)
        {
        }

        internal AsyncWindowsFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
            : base(path, mode, access, share, options, preallocationSize)
        {
        }

        internal override bool IsAsync => true;

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValueTask<int> vt = ReadAsyncInternal(new Memory<byte>(buffer, offset, count), CancellationToken.None);
            return vt.IsCompleted ?
                vt.Result :
                vt.AsTask().GetAwaiter().GetResult();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            => ReadAsyncInternal(destination, cancellationToken);

        private unsafe ValueTask<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            long positionBefore = _filePosition;
            if (CanSeek)
            {
                long len = Length;
                if (positionBefore + destination.Length > len)
                {
                    destination = positionBefore <= len ?
                        destination.Slice(0, (int)(len - positionBefore)) :
                        default;
                }

                // When using overlapped IO, the OS is not supposed to
                // touch the file pointer location at all.  We will adjust it
                // ourselves, but only in memory. This isn't threadsafe.
                _filePosition += destination.Length;
            }

            (SafeFileHandle.ValueTaskSource? vts, int errorCode) = RandomAccess.QueueAsyncReadFile(_fileHandle, destination, positionBefore, cancellationToken);
            return vts != null
                ? new ValueTask<int>(vts, vts.Version)
                : (errorCode == 0) ? ValueTask.FromResult(0) : ValueTask.FromException<int>(HandleIOError(positionBefore, errorCode));
        }

        public override void Write(byte[] buffer, int offset, int count)
            => WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => WriteAsyncInternal(buffer, cancellationToken);

        private unsafe ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            long positionBefore = _filePosition;
            if (CanSeek)
            {
                // When using overlapped IO, the OS is not supposed to
                // touch the file pointer location at all.  We will adjust it
                // ourselves, but only in memory.  This isn't threadsafe.
                _filePosition += source.Length;
                UpdateLengthOnChangePosition();
            }

            (SafeFileHandle.ValueTaskSource? vts, int errorCode) = RandomAccess.QueueAsyncWriteFile(_fileHandle, source, positionBefore, cancellationToken);
            return vts != null
                ? new ValueTask(vts, vts.Version)
                : (errorCode == 0) ? ValueTask.CompletedTask : ValueTask.FromException(HandleIOError(positionBefore, errorCode));
        }

        private Exception HandleIOError(long positionBefore, int errorCode)
        {
            if (!_fileHandle.IsClosed && CanSeek)
            {
                // Update Position... it could be anywhere.
                _filePosition = positionBefore;
            }

            return SafeFileHandle.ValueTaskSource.GetIOError(errorCode, _path);
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask; // no buffering = nothing to flush

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);

            // Fail if the file was closed
            if (_fileHandle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            // Bail early for cancellation if cancellation has been requested
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            return AsyncModeCopyToAsync(destination, bufferSize, cancellationToken);
        }

        private async Task AsyncModeCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(CanRead, "_parent.CanRead");

            try
            {
                await FileStreamHelpers
                    .AsyncModeCopyToAsync(_fileHandle, _path, CanSeek, _filePosition, destination, bufferSize, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                // Make sure the stream's current position reflects where we ended up
                if (!_fileHandle.IsClosed && CanSeek)
                {
                    _filePosition = Length;
                }
            }
        }
    }
}
