// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed class SyncWindowsFileStreamStrategy : WindowsFileStreamStrategy
    {
        internal SyncWindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access, FileShare share) : base(handle, access, share)
        {
        }

        internal SyncWindowsFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
            : base(path, mode, access, share, options, preallocationSize)
        {
        }

        internal override bool IsAsync => false;

        protected override void OnInitFromHandle(SafeFileHandle handle)
        {
            // As we can accurately check the handle type when we have access to NtQueryInformationFile we don't need to skip for
            // any particular file handle type.

            // If the handle was passed in without an explicit async setting, we already looked it up in GetDefaultIsAsync
            if (!handle.IsAsync.HasValue)
                return;

            // If we can't check the handle, just assume it is ok.
            if (!(FileStreamHelpers.IsHandleSynchronous(handle, ignoreInvalid: false) ?? true))
                ThrowHelper.ThrowArgumentException_HandleNotSync(nameof(handle));
        }

        public override int Read(byte[] buffer, int offset, int count) => ReadSpan(new Span<byte>(buffer, offset, count));

        public override int Read(Span<byte> buffer) => ReadSpan(buffer);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
            // Read is invoked asynchronously.  But we can do so using the base Stream's internal helper
            // that bypasses delegating to BeginRead, since we already know this is FileStream rather
            // than something derived from it and what our BeginRead implementation is going to do.
            return (Task<int>)BeginReadInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
            // Read is invoked asynchronously.  But if we have a byte[], we can do so using the base Stream's
            // internal helper that bypasses delegating to BeginRead, since we already know this is FileStream
            // rather than something derived from it and what our BeginRead implementation is going to do.
            return MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment) ?
                new ValueTask<int>((Task<int>)BeginReadInternal(segment.Array!, segment.Offset, segment.Count, null, null, serializeAsynchronously: true, apm: false)) :
                base.ReadAsync(buffer, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
            => WriteSpan(new ReadOnlySpan<byte>(buffer, offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_fileHandle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            WriteSpan(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
            // Write is invoked asynchronously.  But we can do so using the base Stream's internal helper
            // that bypasses delegating to BeginWrite, since we already know this is FileStream rather
            // than something derived from it and what our BeginWrite implementation is going to do.
            return (Task)BeginWriteInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
            // Write is invoked asynchronously.  But if we have a byte[], we can do so using the base Stream's
            // internal helper that bypasses delegating to BeginWrite, since we already know this is FileStream
            // rather than something derived from it and what our BeginWrite implementation is going to do.
            return MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment) ?
                new ValueTask((Task)BeginWriteInternal(segment.Array!, segment.Offset, segment.Count, null, null, serializeAsynchronously: true, apm: false)) :
                base.WriteAsync(buffer, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask; // no buffering = nothing to flush

        private unsafe int ReadSpan(Span<byte> destination)
        {
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");

            NativeOverlapped nativeOverlapped = GetNativeOverlappedForCurrentPosition();
            int r = FileStreamHelpers.ReadFileNative(_fileHandle, destination, true, &nativeOverlapped, out int errorCode);

            if (r == -1)
            {
                // For pipes, ERROR_BROKEN_PIPE is the normal end of the pipe.
                if (errorCode == Interop.Errors.ERROR_BROKEN_PIPE)
                {
                    r = 0;
                }
                else
                {
                    if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                        ThrowHelper.ThrowArgumentException_HandleNotSync(nameof(_fileHandle));

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                }
            }
            Debug.Assert(r >= 0, "FileStream's ReadNative is likely broken.");
            _filePosition += r;

            return r;
        }

        private unsafe void WriteSpan(ReadOnlySpan<byte> source)
        {
            if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");

            NativeOverlapped nativeOverlapped = GetNativeOverlappedForCurrentPosition();
            int r = FileStreamHelpers.WriteFileNative(_fileHandle, source, true, &nativeOverlapped, out int errorCode);

            if (r == -1)
            {
                // For pipes, ERROR_NO_DATA is not an error, but the pipe is closing.
                if (errorCode == Interop.Errors.ERROR_NO_DATA)
                {
                    r = 0;
                }
                else
                {
                    // ERROR_INVALID_PARAMETER may be returned for writes
                    // where the position is too large or for synchronous writes
                    // to a handle opened asynchronously.
                    if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                        throw new IOException(SR.IO_FileTooLongOrHandleNotSync);
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                }
            }
            Debug.Assert(r >= 0, "FileStream's WriteCore is likely broken.");
            _filePosition += r;
            UpdateLengthOnChangePosition();
        }

        private NativeOverlapped GetNativeOverlappedForCurrentPosition()
        {
            NativeOverlapped nativeOverlapped = default;
            // For pipes the offsets are ignored by the OS
            nativeOverlapped.OffsetLow = unchecked((int)_filePosition);
            nativeOverlapped.OffsetHigh = (int)(_filePosition >> 32);

            return nativeOverlapped;
        }
    }
}
