// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Strategies;
using System.Runtime.CompilerServices;
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

        internal static long GetFileLength(SafeFileHandle handle)
        {
            int result = Interop.Sys.FStat(handle, out Interop.Sys.FileStatus status);
            FileStreamHelpers.CheckFileCall(result, handle.Path);
            return status.Size;
        }

        internal static unsafe int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                // The Windows implementation uses ReadFile, which ignores the offset if the handle
                // isn't seekable.  We do the same manually with PRead vs Read, in order to enable
                // the function to be used by FileStream for all the same situations.
                int result = handle.CanSeek ?
                    Interop.Sys.PRead(handle, bufPtr, buffer.Length, fileOffset) :
                    Interop.Sys.Read(handle, bufPtr, buffer.Length);
                FileStreamHelpers.CheckFileCall(result, handle.Path);
                return result;
            }
        }

        internal static unsafe long ReadScatterAtOffset(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
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

            return FileStreamHelpers.CheckFileCall(result, handle.Path);
        }

        internal static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
            => ScheduleSyncReadAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
            => ScheduleSyncReadScatterAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);

        internal static unsafe void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            while (!buffer.IsEmpty)
            {
                fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
                {
                    // The Windows implementation uses WriteFile, which ignores the offset if the handle
                    // isn't seekable.  We do the same manually with PWrite vs Write, in order to enable
                    // the function to be used by FileStream for all the same situations.
                    int bytesWritten = handle.CanSeek ?
                        Interop.Sys.PWrite(handle, bufPtr, GetNumberOfBytesToWrite(buffer.Length), fileOffset) :
                        Interop.Sys.Write(handle, bufPtr, GetNumberOfBytesToWrite(buffer.Length));

                    FileStreamHelpers.CheckFileCall(bytesWritten, handle.Path);
                    if (bytesWritten == buffer.Length)
                    {
                        break;
                    }

                    // The write completed successfully but for fewer bytes than requested.
                    // We need to try again for the remainder.
                    buffer = buffer.Slice(bytesWritten);
                    fileOffset += bytesWritten;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNumberOfBytesToWrite(int byteCount)
        {
#if DEBUG
            // In debug only, to assist with testing, simulate writing fewer than the requested number of bytes.
            if (byteCount > 1 &&  // ensure we don't turn the read into a zero-byte read
                byteCount < 512)  // avoid on larger buffers that might have a length used to meet an alignment requirement
            {
                byteCount /= 2;
            }
#endif
            return byteCount;
        }

        internal static unsafe void WriteGatherAtOffset(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
        {
            int buffersCount = buffers.Count;
            if (buffersCount == 0)
            {
                return;
            }

            var handles = new MemoryHandle[buffersCount];
            Span<Interop.Sys.IOVector> vectors = buffersCount <= IovStackThreshold ?
                stackalloc Interop.Sys.IOVector[IovStackThreshold] :
                new Interop.Sys.IOVector[buffersCount];

            try
            {
                int buffersOffset = 0, firstBufferOffset = 0;
                while (true)
                {
                    long totalBytesToWrite = 0;

                    for (int i = buffersOffset; i < buffersCount; i++)
                    {
                        ReadOnlyMemory<byte> buffer = buffers[i];
                        totalBytesToWrite += buffer.Length;

                        MemoryHandle memoryHandle = buffer.Pin();
                        vectors[i] = new Interop.Sys.IOVector { Base = firstBufferOffset + (byte*)memoryHandle.Pointer, Count = (UIntPtr)(buffer.Length - firstBufferOffset) };
                        handles[i] = memoryHandle;

                        firstBufferOffset = 0;
                    }

                    if (totalBytesToWrite == 0)
                    {
                        break;
                    }

                    long bytesWritten;
                    fixed (Interop.Sys.IOVector* pinnedVectors = &MemoryMarshal.GetReference(vectors))
                    {
                        bytesWritten = Interop.Sys.PWriteV(handle, pinnedVectors, buffersCount, fileOffset);
                    }

                    FileStreamHelpers.CheckFileCall(bytesWritten, handle.Path);
                    if (bytesWritten == totalBytesToWrite)
                    {
                        break;
                    }

                    // The write completed successfully but for fewer bytes than requested.
                    // We need to try again for the remainder.
                    for (int i = 0; i < buffersCount; i++)
                    {
                        int n = buffers[i].Length;
                        if (n <= bytesWritten)
                        {
                            buffersOffset++;
                            bytesWritten -= n;
                            if (bytesWritten == 0)
                            {
                                break;
                            }
                        }
                        else
                        {
                            firstBufferOffset = (int)(bytesWritten - n);
                            break;
                        }
                    }
                }
            }
            finally
            {
                foreach (MemoryHandle memoryHandle in handles)
                {
                    memoryHandle.Dispose();
                }
            }
        }

        internal static ValueTask WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
            => ScheduleSyncWriteAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);

        private static ValueTask WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
            => ScheduleSyncWriteGatherAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);
    }
}
