// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO.Strategies;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class RandomAccess
    {
        // IovStackThreshold matches Linux's UIO_FASTIOV, which is the number of 'struct iovec'
        // that get stackalloced in the Linux kernel.
        private const int IovStackThreshold = 8;

        internal static long GetFileLength(SafeFileHandle handle, string? path)
        {
            int result = Interop.Sys.FStat(handle, out Interop.Sys.FileStatus status);
            FileStreamHelpers.CheckFileCall(result, path);
            return status.Size;
        }

        private static unsafe int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                int result = Interop.Sys.PRead(handle, bufPtr, buffer.Length, fileOffset);
                FileStreamHelpers.CheckFileCall(result, path: null);
                return result;
            }
        }

        private static unsafe long ReadScatterAtOffset(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
        {
            MemoryHandle[] handles = new MemoryHandle[buffers.Count];
            Span<Interop.Sys.IOVector> vectors = buffers.Count <= IovStackThreshold ? stackalloc Interop.Sys.IOVector[IovStackThreshold] : new Interop.Sys.IOVector[buffers.Count];

            long result;
            try
            {
                int buffersCount = buffers.Count;
                for (int i = 0; i < buffersCount; i++)
                {
                    Memory<byte> buffer = buffers[i];
                    MemoryHandle memoryHandle = buffer.Pin();
                    vectors[i] = new Interop.Sys.IOVector { Base = (byte*)memoryHandle.Pointer, Count = (UIntPtr)buffer.Length };
                    handles[i] = memoryHandle;
                }

                fixed (Interop.Sys.IOVector* pinnedVectors = &MemoryMarshal.GetReference(vectors))
                {
                    result = Interop.Sys.PReadV(handle, pinnedVectors, buffers.Count, fileOffset);
                }
            }
            finally
            {
                foreach (MemoryHandle memoryHandle in handles)
                {
                    memoryHandle.Dispose();
                }
            }

            return FileStreamHelpers.CheckFileCall(result, path: null);
        }

        private static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken)
        {
            return new ValueTask<int>(Task.Factory.StartNew(static state =>
            {
                var args = ((SafeFileHandle handle, Memory<byte> buffer, long fileOffset))state!;
                return ReadAtOffset(args.handle, args.buffer.Span, args.fileOffset);
            }, (handle, buffer, fileOffset), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
        }

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
        {
            return new ValueTask<long>(Task.Factory.StartNew(static state =>
            {
                var args = ((SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset))state!;
                return ReadScatterAtOffset(args.handle, args.buffers, args.fileOffset);
            }, (handle, buffers, fileOffset), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
        }

        private static unsafe int WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                int result = Interop.Sys.PWrite(handle, bufPtr, buffer.Length, fileOffset);
                FileStreamHelpers.CheckFileCall(result, path: null);
                return  result;
            }
        }

        private static unsafe long WriteGatherAtOffset(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
        {
            MemoryHandle[] handles = new MemoryHandle[buffers.Count];
            Span<Interop.Sys.IOVector> vectors = buffers.Count <= IovStackThreshold ? stackalloc Interop.Sys.IOVector[IovStackThreshold] : new Interop.Sys.IOVector[buffers.Count ];

            long result;
            try
            {
                int buffersCount = buffers.Count;
                for (int i = 0; i < buffersCount; i++)
                {
                    ReadOnlyMemory<byte> buffer = buffers[i];
                    MemoryHandle memoryHandle = buffer.Pin();
                    vectors[i] = new Interop.Sys.IOVector { Base = (byte*)memoryHandle.Pointer, Count = (UIntPtr)buffer.Length };
                    handles[i] = memoryHandle;
                }

                fixed (Interop.Sys.IOVector* pinnedVectors = &MemoryMarshal.GetReference(vectors))
                {
                    result = Interop.Sys.PWriteV(handle, pinnedVectors, buffers.Count, fileOffset);
                }
            }
            finally
            {
                foreach (MemoryHandle memoryHandle in handles)
                {
                    memoryHandle.Dispose();
                }
            }

            return FileStreamHelpers.CheckFileCall(result, path: null);
        }

        private static ValueTask<int> WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken)
        {
            return new ValueTask<int>(Task.Factory.StartNew(static state =>
            {
                var args = ((SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset))state!;
                return WriteAtOffset(args.handle, args.buffer.Span, args.fileOffset);
            }, (handle, buffer, fileOffset), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
        }

        private static ValueTask<long> WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
        {
            return new ValueTask<long>(Task.Factory.StartNew(static state =>
            {
                var args = ((SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset))state!;
                return WriteGatherAtOffset(args.handle, args.buffers, args.fileOffset);
            }, (handle, buffers, fileOffset), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default));
        }
    }
}
