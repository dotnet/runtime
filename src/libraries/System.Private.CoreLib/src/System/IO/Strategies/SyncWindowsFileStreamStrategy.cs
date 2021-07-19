// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed class SyncWindowsFileStreamStrategy : OSFileStreamStrategy
    {
        internal SyncWindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access, FileShare share) : base(handle, access, share)
        {
        }

        internal SyncWindowsFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
            : base(path, mode, access, share, options, preallocationSize)
        {
        }

        internal override bool IsAsync => false;

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
            // Read is invoked asynchronously.  But we can do so using the base Stream's internal helper
            // that bypasses delegating to BeginRead, since we already know this is FileStream rather
            // than something derived from it and what our BeginRead implementation is going to do.
            return BeginReadInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
            // Read is invoked asynchronously.  But if we have a byte[], we can do so using the base Stream's
            // internal helper that bypasses delegating to BeginRead, since we already know this is FileStream
            // rather than something derived from it and what our BeginRead implementation is going to do.
            return MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment) ?
                new ValueTask<int>(BeginReadInternal(segment.Array!, segment.Offset, segment.Count, null, null, serializeAsynchronously: true, apm: false)) :
                base.ReadAsync(buffer, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
            // Write is invoked asynchronously.  But we can do so using the base Stream's internal helper
            // that bypasses delegating to BeginWrite, since we already know this is FileStream rather
            // than something derived from it and what our BeginWrite implementation is going to do.
            return BeginWriteInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
            // Write is invoked asynchronously.  But if we have a byte[], we can do so using the base Stream's
            // internal helper that bypasses delegating to BeginWrite, since we already know this is FileStream
            // rather than something derived from it and what our BeginWrite implementation is going to do.
            return MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment) ?
                new ValueTask(BeginWriteInternal(segment.Array!, segment.Offset, segment.Count, null, null, serializeAsynchronously: true, apm: false)) :
                base.WriteAsync(buffer, cancellationToken);
        }
    }
}
