// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed partial class AsyncWindowsFileStreamStrategy : OSFileStreamStrategy
    {
        internal AsyncWindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access) : base(handle, access)
        {
        }

        internal AsyncWindowsFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
            : base(path, mode, access, share, options, preallocationSize)
        {
        }

        internal override bool IsAsync => true;

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            if (!CanSeek)
            {
                return RandomAccess.ReadAtOffsetAsync(_fileHandle, destination, fileOffset: -1, cancellationToken);
            }

            if (LengthCachingSupported && _length >= 0 && Volatile.Read(ref _filePosition) >= _length)
            {
                // We know for sure that the file length can be safely cached and it has already been obtained.
                // If we have reached EOF we just return here and avoid a sys-call.
                return ValueTask.FromResult(0);
            }

            // This implementation updates the file position before the operation starts and updates it after incomplete read.
            // This is done to keep backward compatibility for concurrent reads.
            // It uses Interlocked as there can be multiple concurrent incomplete reads updating position at the same time.
            long readOffset = Interlocked.Add(ref _filePosition, destination.Length) - destination.Length;

            (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) = RandomAccess.QueueAsyncReadFile(_fileHandle, destination, readOffset, cancellationToken, this);
            return vts != null
                ? new ValueTask<int>(vts, vts.Version)
                : (errorCode == 0) ? ValueTask.FromResult(0) : ValueTask.FromException<int>(HandleIOError(readOffset, errorCode));
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            long writeOffset = CanSeek ? Interlocked.Add(ref _filePosition, buffer.Length) - buffer.Length : -1;

            (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) = RandomAccess.QueueAsyncWriteFile(_fileHandle, buffer, writeOffset, cancellationToken);
            return vts != null
                ? new ValueTask(vts, vts.Version)
                : (errorCode == 0) ? ValueTask.CompletedTask : ValueTask.FromException(HandleIOError(writeOffset, errorCode));
        }

        private Exception HandleIOError(long positionBefore, int errorCode)
        {
            if (_fileHandle.CanSeek)
            {
                // Update Position... it could be anywhere.
                Interlocked.Exchange(ref _filePosition, positionBefore);
            }

            return SafeFileHandle.OverlappedValueTaskSource.GetIOError(errorCode, _fileHandle.Path);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
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
                    .AsyncModeCopyToAsync(_fileHandle, CanSeek, _filePosition, destination, bufferSize, cancellationToken)
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
