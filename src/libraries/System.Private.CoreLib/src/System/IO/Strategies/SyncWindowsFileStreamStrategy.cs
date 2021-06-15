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

            int r = RandomAccess.ReadAtOffset(_fileHandle, destination, _filePosition, _path);
            Debug.Assert(r >= 0, $"RandomAccess.ReadAtOffset returned {r}.");
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

            int r = RandomAccess.WriteAtOffset(_fileHandle, source, _filePosition, _path);
            Debug.Assert(r >= 0, $"RandomAccess.WriteAtOffset returned {r}.");
            _filePosition += r;

            UpdateLengthOnChangePosition();
        }
    }
}
