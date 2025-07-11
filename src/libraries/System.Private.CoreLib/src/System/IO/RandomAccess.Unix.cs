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

        internal static unsafe void SetFileLength(SafeFileHandle handle, long length) =>
            FileStreamHelpers.CheckFileCall(Interop.Sys.FTruncate(handle, length), handle.Path);

        internal static unsafe int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                // The Windows implementation uses ReadFile, which ignores the offset if the handle
                // isn't seekable.  We do the same manually with PRead vs Read, in order to enable
                // the function to be used by FileStream for all the same situations.
                int result;
                if (handle.SupportsRandomAccess)
                {
                    // Try pread for seekable files.
                    result = Interop.Sys.PRead(handle, bufPtr, buffer.Length, fileOffset);
                    if (result == -1)
                    {
                        // We need to fallback to the non-offset version for certain file types
                        // e.g: character devices (such as /dev/tty), pipes, and sockets.
                        Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

                        if (errorInfo.Error == Interop.Error.ENXIO ||
                            errorInfo.Error == Interop.Error.ESPIPE)
                        {
                            handle.SupportsRandomAccess = false;
                            result = Interop.Sys.Read(handle, bufPtr, buffer.Length);
                        }
                    }
                }
                else
                {
                    result = Interop.Sys.Read(handle, bufPtr, buffer.Length);
                }

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

        internal static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => handle.GetThreadPoolValueTaskSource().QueueRead(buffer, fileOffset, cancellationToken, strategy);

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            => handle.GetThreadPoolValueTaskSource().QueueReadScatter(buffers, fileOffset, cancellationToken);

        internal static unsafe void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            while (!buffer.IsEmpty)
            {
                fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
                {
                    // The Windows implementation uses WriteFile, which ignores the offset if the handle
                    // isn't seekable.  We do the same manually with PWrite vs Write, in order to enable
                    // the function to be used by FileStream for all the same situations.
                    int bytesToWrite = GetNumberOfBytesToWrite(buffer.Length);
                    int bytesWritten;
                    if (handle.SupportsRandomAccess)
                    {
                        bytesWritten = Interop.Sys.PWrite(handle, bufPtr, bytesToWrite, fileOffset);
                        if (bytesWritten == -1)
                        {
                            // We need to fallback to the non-offset version for certain file types
                            // e.g: character devices (such as /dev/tty), pipes, and sockets.
                            Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

                            if (errorInfo.Error == Interop.Error.ENXIO ||
                                errorInfo.Error == Interop.Error.ESPIPE)
                            {
                                handle.SupportsRandomAccess = false;
                                bytesWritten = Interop.Sys.Write(handle, bufPtr, bytesToWrite);
                            }
                        }
                    }
                    else
                    {
                        bytesWritten = Interop.Sys.Write(handle, bufPtr, bytesToWrite);
                    }

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
                stackalloc Interop.Sys.IOVector[IovStackThreshold].Slice(0, buffersCount) :
                new Interop.Sys.IOVector[buffersCount];

            try
            {
                long totalBytesToWrite = 0;
                for (int i = 0; i < buffersCount; i++)
                {
                    ReadOnlyMemory<byte> buffer = buffers[i];
                    totalBytesToWrite += buffer.Length;

                    MemoryHandle memoryHandle = buffer.Pin();
                    vectors[i] = new Interop.Sys.IOVector { Base = (byte*)memoryHandle.Pointer, Count = (UIntPtr)buffer.Length };
                    handles[i] = memoryHandle;
                }

                int buffersOffset = 0;
                while (totalBytesToWrite > 0)
                {
                    long bytesWritten;
                    Span<Interop.Sys.IOVector> left = vectors.Slice(buffersOffset);
                    fixed (Interop.Sys.IOVector* pinnedVectors = &MemoryMarshal.GetReference(left))
                    {
                        bytesWritten = Interop.Sys.PWriteV(handle, pinnedVectors, left.Length, fileOffset);
                    }

                    FileStreamHelpers.CheckFileCall(bytesWritten, handle.Path);
                    if (bytesWritten == totalBytesToWrite)
                    {
                        break;
                    }

                    // The write completed successfully but for fewer bytes than requested.
                    // We need to perform next write where the previous one has finished.
                    fileOffset += bytesWritten;
                    totalBytesToWrite -= bytesWritten;
                    // We need to try again for the remainder.
                    while (buffersOffset < buffersCount && bytesWritten > 0)
                    {
                        int n = (int)vectors[buffersOffset].Count;
                        if (n <= bytesWritten)
                        {
                            bytesWritten -= n;
                            buffersOffset++;
                        }
                        else
                        {
                            // A partial read: the vector needs to point to the new offset.
                            // But that offset needs to be relative to the previous attempt.
                            // Example: we have a single buffer with 30 bytes and the first read returned 10.
                            // The next read should try to read the remaining 20 bytes, but in case it also reads just 10,
                            // the third attempt should read last 10 bytes (not 20 again).
                            Interop.Sys.IOVector current = vectors[buffersOffset];
                            vectors[buffersOffset] = new Interop.Sys.IOVector
                            {
                                Base = current.Base + (int)(bytesWritten),
                                Count = current.Count - (UIntPtr)(bytesWritten)
                            };
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

        internal static ValueTask WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => handle.GetThreadPoolValueTaskSource().QueueWrite(buffer, fileOffset, cancellationToken, strategy);

        private static ValueTask WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            => handle.GetThreadPoolValueTaskSource().QueueWriteGather(buffers, fileOffset, cancellationToken);
    }
}
