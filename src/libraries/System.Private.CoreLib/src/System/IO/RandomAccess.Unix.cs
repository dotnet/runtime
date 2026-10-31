// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO.Strategies;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using AsyncResult = System.Threading.UnixHandleAsyncContext.AsyncResult;

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
            while (true)
            {
                if (TryCompleteReadAt(handle, fileOffset, buffer, out int readResult, out Interop.ErrorInfo errorInfo))
                {
                    return CheckFileCall(handle, readResult, errorInfo);
                }

                Interop.Sys.Poll(handle, Interop.PollEvents.POLLIN, timeout: -1, out _);
            }
        }

        internal static bool TryCompleteReadAt(SafeFileHandle handle, long offset, Span<byte> buffer, out int readResult, out Interop.ErrorInfo errorInfo, OSFileStreamStrategy? strategy = null)
        {
            if (handle.SupportsRandomAccess)
            {
                if (TryCompleteReadAt(handle, useOffset: true, offset, buffer, out readResult, out errorInfo, strategy: null))
                {
                    if (readResult == -1 && ShouldFallBackToNonOffsetSyscall(errorInfo))
                    {
                        handle.SupportsRandomAccess = false;
                        return TryCompleteReadAt(handle, useOffset: false, offset, buffer, out readResult, out errorInfo, strategy);
                    }

                    strategy?.OnIncompleteOperation(buffer.Length, Math.Max(readResult, 0));
                    return true;
                }

                return false;
            }

            return TryCompleteReadAt(handle, useOffset: false, offset, buffer, out readResult, out errorInfo, strategy);
        }

        private static unsafe bool TryCompleteReadAt(SafeFileHandle handle, bool useOffset, long offset, Span<byte> buffer, out int readResult, out Interop.ErrorInfo errorInfo, OSFileStreamStrategy? strategy = null)
        {
            if (buffer.Length == 0 && handle.SupportsNonBlocking)
            {
                Interop.Error err = Interop.Sys.Poll(handle, Interop.PollEvents.POLLIN, handle.IsBlocking ? -1 : 0, out Interop.PollEvents events);

                if (err != Interop.Error.SUCCESS)
                {
                    readResult = -1;
                    errorInfo = new Interop.ErrorInfo(err);
                    strategy?.OnIncompleteOperation(buffer.Length, 0);
                    return true;
                }

                readResult = 0;
                errorInfo = default;
                return events != Interop.PollEvents.POLLNONE;
            }

            fixed (byte* p = &MemoryMarshal.GetReference(buffer))
            {
                readResult = useOffset
                    ? Interop.Sys.PRead(handle, p, buffer.Length, offset)
                    : Interop.Sys.Read(handle, p, buffer.Length);
            }

            if (readResult < 0)
            {
                errorInfo = Interop.Sys.GetLastErrorInfo();
                if (IsPending(errorInfo))
                {
                    readResult = 0;
                    return false;
                }
                strategy?.OnIncompleteOperation(buffer.Length, 0);
                return true;
            }

            errorInfo = default;
            strategy?.OnIncompleteOperation(buffer.Length, readResult);
            return true;
        }

        internal static unsafe long ReadScatterAtOffset(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
        {
            while (true)
            {
                if (TryCompleteReadAt(handle, fileOffset, buffers, out long readResult, out Interop.ErrorInfo errorInfo))
                {
                    return CheckFileCall(handle, readResult, errorInfo);
                }

                Interop.Sys.Poll(handle, Interop.PollEvents.POLLIN, timeout: -1, out _);
            }
        }

        internal static bool TryCompleteReadAt(SafeFileHandle handle, long offset, IReadOnlyList<Memory<byte>> buffers, out long readResult, out Interop.ErrorInfo errorInfo)
        {
            if (handle.SupportsRandomAccess)
            {
                if (TryCompleteReadAt(handle, useOffset: true, offset, buffers, out readResult, out errorInfo))
                {
                    if (readResult == -1 && ShouldFallBackToNonOffsetSyscall(errorInfo))
                    {
                        handle.SupportsRandomAccess = false;
                        return TryCompleteReadAt(handle, useOffset: false, offset, buffers, out readResult, out errorInfo);
                    }

                    return true;
                }

                return false;
            }

            return TryCompleteReadAt(handle, useOffset: false, offset, buffers, out readResult, out errorInfo);
        }

        private static unsafe bool TryCompleteReadAt(SafeFileHandle handle, bool useOffset, long offset, IReadOnlyList<Memory<byte>> buffers, out long readResult, out Interop.ErrorInfo errorInfo)
        {
            int count = buffers.Count;
            MemoryHandle[] memHandles = new MemoryHandle[count];
            Span<Interop.Sys.IOVector> vectors = count <= IovStackThreshold
                ? stackalloc Interop.Sys.IOVector[IovStackThreshold].Slice(0, count)
                : new Interop.Sys.IOVector[count];

            try
            {
                for (int i = 0; i < count; i++)
                {
                    Memory<byte> buffer = buffers[i];
                    MemoryHandle mh = buffer.Pin();
                    vectors[i] = new Interop.Sys.IOVector { Base = (byte*)mh.Pointer, Count = (UIntPtr)buffer.Length };
                    memHandles[i] = mh;
                }

                fixed (Interop.Sys.IOVector* pinnedVectors = &MemoryMarshal.GetReference(vectors))
                {
                    long read = useOffset
                        ? Interop.Sys.PReadV(handle, pinnedVectors, count, offset)
                        : Interop.Sys.ReadV(handle, pinnedVectors, count);
                    if (read < 0)
                    {
                        errorInfo = Interop.Sys.GetLastErrorInfo();
                        if (IsPending(errorInfo))
                        {
                            readResult = 0;
                            return false;
                        }
                        readResult = -1;
                        return true;
                    }

                    readResult = read;
                    errorInfo = default;
                    return true;
                }
            }
            finally
            {
                foreach (MemoryHandle mh in memHandles)
                {
                    mh.Dispose();
                }
            }
        }

        internal static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => handle.UseThreadPoolForAsync
                ? ReadAtAsyncThreadPool(handle, fileOffset, buffer, cancellationToken, strategy)
                : ReadAtAsyncPollable(handle, fileOffset, buffer, cancellationToken, strategy);

        private static ValueTask<int> ReadAtAsyncThreadPool(SafeFileHandle handle, long offset, Memory<byte> destination, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            SafeFileHandle.ReadOperation op = handle.RentReadOperation();
            op.Init(offset, destination, cancellationToken, strategy);
            op.QueueToThreadPool();
            return new ValueTask<int>(op, op.Version);
        }

        private static ValueTask<int> ReadAtAsyncPollable(SafeFileHandle handle, long offset, Memory<byte> destination, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            int sequenceNumber;
            if (handle.AsyncContext.IsReadReady(out sequenceNumber))
            {
                if (TryCompleteReadAt(handle, offset, destination.Span, out int readResult, out Interop.ErrorInfo errorInfo, strategy))
                {
                    return new ValueTask<int>(CheckFileCall(handle, readResult, errorInfo));
                }
            }

            SafeFileHandle.ReadOperation op = handle.RentReadOperation();
            op.Init(offset, destination, cancellationToken, strategy);

            AsyncResult result = handle.AsyncContext.StartAsyncRead(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask<int>(op, op.Version);
            }
            else if (result == AsyncResult.Completed) // The operation completed synchronously.
            {
                int completedResult = (int)op.ReadResult;
                Exception? exception = op.Exception;

                handle.ReturnReadOperation(op);

                if (exception is not null)
                {
                    throw exception;
                }
                return new ValueTask<int>(completedResult);
            }

            throw new OperationCanceledException(cancellationToken);
        }

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            => handle.UseThreadPoolForAsync
                ? ReadAtAsyncThreadPool(handle, fileOffset, buffers, cancellationToken)
                : ReadAtAsyncPollable(handle, fileOffset, buffers, cancellationToken);

        private static ValueTask<long> ReadAtAsyncThreadPool(SafeFileHandle handle, long offset, IReadOnlyList<Memory<byte>> buffers, CancellationToken cancellationToken)
        {
            SafeFileHandle.ReadOperation op = handle.RentReadOperation();
            op.Init(offset, buffers, cancellationToken);
            op.QueueToThreadPool();
            return new ValueTask<long>(op, op.Version);
        }

        private static ValueTask<long> ReadAtAsyncPollable(SafeFileHandle handle, long offset, IReadOnlyList<Memory<byte>> buffers, CancellationToken cancellationToken)
        {
            int sequenceNumber;
            if (handle.AsyncContext.IsReadReady(out sequenceNumber) &&
                TryCompleteReadAt(handle, offset, buffers, out long readResult, out Interop.ErrorInfo errorInfo))
            {
                return new ValueTask<long>(CheckFileCall(handle, readResult, errorInfo));
            }

            SafeFileHandle.ReadOperation op = handle.RentReadOperation();
            op.Init(offset, buffers, cancellationToken);

            AsyncResult result = handle.AsyncContext.StartAsyncRead(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask<long>(op, op.Version);
            }
            else if (result == AsyncResult.Completed) // The operation completed synchronously.
            {
                long completedResult = op.ReadResult;
                Exception? exception = op.Exception;

                handle.ReturnReadOperation(op);

                if (exception is not null)
                {
                    throw exception;
                }
                return new ValueTask<long>(completedResult);
            }

            throw new OperationCanceledException(cancellationToken);
        }

        internal static unsafe void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
            => WriteAtOffset(handle, buffer, ref fileOffset);

        internal static unsafe void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, ref long fileOffset)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            while (true)
            {
                if (TryCompleteWriteAt(handle, fileOffset, buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo))
                {
                    fileOffset += bytesWritten;
                    CheckFileCall(handle, errorInfo);
                    return;
                }

                fileOffset += bytesWritten;
                buffer = buffer.Slice(bytesWritten);

                Interop.Sys.Poll(handle, Interop.PollEvents.POLLOUT, timeout: -1, out _);
            }
        }

        internal static bool TryCompleteWriteAt(SafeFileHandle handle, long offset, ReadOnlySpan<byte> buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo, OSFileStreamStrategy? strategy = null)
        {
            if (handle.SupportsRandomAccess)
            {
                if (TryCompleteWriteAt(handle, useOffset: true, offset, buffer, out bytesWritten, out errorInfo, strategy: null))
                {
                    if (bytesWritten == 0 && ShouldFallBackToNonOffsetSyscall(errorInfo))
                    {
                        handle.SupportsRandomAccess = false;
                        return TryCompleteWriteAt(handle, useOffset: false, offset, buffer, out bytesWritten, out errorInfo, strategy);
                    }

                    strategy?.OnIncompleteOperation(buffer.Length, bytesWritten);
                    return true;
                }

                return false;
            }

            return TryCompleteWriteAt(handle, useOffset: false, offset, buffer, out bytesWritten, out errorInfo, strategy);
        }

        private static unsafe bool TryCompleteWriteAt(SafeFileHandle handle, bool useOffset, long offset, ReadOnlySpan<byte> buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo, OSFileStreamStrategy? strategy = null)
        {
            int totalBytesWritten = 0;
            while (true)
            {
                int toWrite = GetNumberOfBytesToWrite(buffer.Length);
                int written;
                fixed (byte* p = &MemoryMarshal.GetReference(buffer))
                {
                    written = useOffset
                        ? Interop.Sys.PWrite(handle, p, toWrite, offset)
                        : Interop.Sys.Write(handle, p, toWrite);
                }

                if (written < 0)
                {
                    errorInfo = Interop.Sys.GetLastErrorInfo();
                    bytesWritten = totalBytesWritten;
                    if (IsPending(errorInfo))
                    {
                        return false;
                    }
                    strategy?.OnIncompleteOperation(buffer.Length, 0);
                    return true;
                }

                totalBytesWritten += written;
                buffer = buffer.Slice(written);
                offset += written;

                if (buffer.Length == 0)
                {
                    errorInfo = default;
                    bytesWritten = totalBytesWritten;
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNumberOfBytesToWrite(int byteCount)
        {
#if DEBUG
            // In debug only, to assist with testing, simulate writing fewer than the requested number of bytes.
            if (byteCount > 1 &&  // ensure we don't turn the write into a zero-byte write
                byteCount < 512)  // avoid on larger buffers that might have a length used to meet an alignment requirement
            {
                byteCount /= 2;
            }
#endif
            return byteCount;
        }

        internal static unsafe void WriteGatherAtOffset(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
        {
            int bufferIndex = 0;
            int bufferOffset = 0;

            while (true)
            {
                if (TryCompleteWriteAt(handle, ref fileOffset, buffers, ref bufferIndex, ref bufferOffset, out Interop.ErrorInfo errorInfo))
                {
                    CheckFileCall(handle, errorInfo);
                    return;
                }

                Interop.Sys.Poll(handle, Interop.PollEvents.POLLOUT, timeout: -1, out _);
            }
        }

        internal static bool TryCompleteWriteAt(SafeFileHandle handle, ref long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, ref int bufferIndex, ref int bufferOffset, out Interop.ErrorInfo errorInfo)
        {
            if (handle.SupportsRandomAccess)
            {
                if (TryCompleteWriteAt(handle, useOffset: true, ref offset, buffers, ref bufferIndex, ref bufferOffset, out errorInfo))
                {
                    if (errorInfo.Error != Interop.Error.SUCCESS && ShouldFallBackToNonOffsetSyscall(errorInfo))
                    {
                        handle.SupportsRandomAccess = false;
                        return TryCompleteWriteAt(handle, useOffset: false, ref offset, buffers, ref bufferIndex, ref bufferOffset, out errorInfo);
                    }

                    return true;
                }

                return false;
            }

            return TryCompleteWriteAt(handle, useOffset: false, ref offset, buffers, ref bufferIndex, ref bufferOffset, out errorInfo);
        }

        private static unsafe bool TryCompleteWriteAt(SafeFileHandle handle, bool useOffset, ref long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, ref int bufferIndex, ref int bufferOffset, out Interop.ErrorInfo errorInfo)
        {
            Span<Interop.Sys.IOVector> stackVectors = stackalloc Interop.Sys.IOVector[IovStackThreshold];
            while (true)
            {
                // Skip zero-length buffers.
                while (bufferIndex < buffers.Count && buffers[bufferIndex].Length == 0)
                {
                    bufferIndex++;
                }

                if (bufferIndex >= buffers.Count)
                {
                    errorInfo = default;
                    return true;
                }

                int remaining = buffers.Count - bufferIndex;
                MemoryHandle[] memHandles = new MemoryHandle[remaining];
                Span<Interop.Sys.IOVector> vectors = remaining <= IovStackThreshold
                    ? stackVectors.Slice(0, remaining)
                    : new Interop.Sys.IOVector[remaining];

                try
                {
                    long totalToWrite = 0;
                    for (int i = 0; i < remaining; i++)
                    {
                        ReadOnlyMemory<byte> buf = buffers[bufferIndex + i];
                        MemoryHandle mh = buf.Pin();
                        byte* ptr = (byte*)mh.Pointer;
                        int len = buf.Length;
                        if (i == 0 && bufferOffset > 0)
                        {
                            ptr += bufferOffset;
                            len -= bufferOffset;
                        }
                        vectors[i] = new Interop.Sys.IOVector { Base = ptr, Count = (UIntPtr)len };
                        memHandles[i] = mh;
                        totalToWrite += len;
                    }

                    fixed (Interop.Sys.IOVector* pinnedVectors = &MemoryMarshal.GetReference(vectors))
                    {
                        long bytesWritten = useOffset
                            ? Interop.Sys.PWriteV(handle, pinnedVectors, remaining, offset)
                            : Interop.Sys.WriteV(handle, pinnedVectors, remaining);
                        if (bytesWritten < 0)
                        {
                            errorInfo = Interop.Sys.GetLastErrorInfo();
                            return !IsPending(errorInfo);
                        }

                        errorInfo = default;
                        offset += bytesWritten;

                        if (bytesWritten == totalToWrite)
                        {
                            return true;
                        }

                        long written = bytesWritten;
                        while (written > 0)
                        {
                            int currentLen = buffers[bufferIndex].Length - bufferOffset;
                            if (written >= currentLen)
                            {
                                written -= currentLen;
                                bufferIndex++;
                                bufferOffset = 0;
                            }
                            else
                            {
                                bufferOffset += (int)written;
                                written = 0;
                            }
                        }
                    }
                }
                finally
                {
                    foreach (MemoryHandle mh in memHandles)
                    {
                        mh.Dispose();
                    }
                }
            }
        }

        internal static ValueTask WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => handle.UseThreadPoolForAsync
                ? WriteAtAsyncThreadPool(handle, fileOffset, buffer, cancellationToken, strategy)
                : WriteAtAsyncPollable(handle, fileOffset, buffer, cancellationToken, strategy);

        private static ValueTask WriteAtAsyncThreadPool(SafeFileHandle handle, long offset, ReadOnlyMemory<byte> source, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            if (source.IsEmpty)
            {
                return default;
            }

            SafeFileHandle.WriteOperation op = handle.RentWriteOperation();
            op.Init(offset, source, cancellationToken, strategy);
            op.QueueToThreadPool();
            return new ValueTask(op, op.Version);
        }

        private static ValueTask WriteAtAsyncPollable(SafeFileHandle handle, long offset, ReadOnlyMemory<byte> source, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            if (source.IsEmpty)
            {
                return default;
            }

            int sequenceNumber;
            if (handle.AsyncContext.IsWriteReady(out sequenceNumber))
            {
                if (TryCompleteWriteAt(handle, offset, source.Span, out int bytesWritten, out Interop.ErrorInfo writeResult, strategy))
                {
                    CheckFileCall(handle, writeResult);
                    return default;
                }

                source = source.Slice(bytesWritten);
                offset += bytesWritten;
            }

            SafeFileHandle.WriteOperation op = handle.RentWriteOperation();
            op.Init(offset, source, cancellationToken, strategy);

            AsyncResult result = handle.AsyncContext.StartAsyncWrite(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask(op, op.Version);
            }
            else if (result == AsyncResult.Completed) // The operation completed synchronously.
            {
                Exception? exception = op.Exception;

                handle.ReturnWriteOperation(op);

                if (exception is not null)
                {
                    throw exception;
                }
                return default;
            }

            throw new OperationCanceledException(cancellationToken);
        }

        private static ValueTask WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            => handle.UseThreadPoolForAsync
                ? WriteAtAsyncThreadPool(handle, fileOffset, buffers, cancellationToken)
                : WriteAtAsyncPollable(handle, fileOffset, buffers, cancellationToken);

        private static ValueTask WriteAtAsyncThreadPool(SafeFileHandle handle, long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken)
        {
            SafeFileHandle.WriteOperation op = handle.RentWriteOperation();
            op.Init(offset, buffers, 0, 0, cancellationToken);
            op.QueueToThreadPool();
            return new ValueTask(op, op.Version);
        }

        private static ValueTask WriteAtAsyncPollable(SafeFileHandle handle, long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken)
        {
            int bufferIndex = 0;
            int bufferOffset = 0;
            int sequenceNumber;
            if (handle.AsyncContext.IsWriteReady(out sequenceNumber))
            {
                if (TryCompleteWriteAt(handle, ref offset, buffers, ref bufferIndex, ref bufferOffset, out Interop.ErrorInfo writeResult))
                {
                    CheckFileCall(handle, writeResult);
                    return default;
                }
            }

            SafeFileHandle.WriteOperation op = handle.RentWriteOperation();
            op.Init(offset, buffers, bufferIndex, bufferOffset, cancellationToken);

            AsyncResult result = handle.AsyncContext.StartAsyncWrite(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask(op, op.Version);
            }
            else if (result == AsyncResult.Completed) // The operation completed synchronously.
            {
                Exception? exception = op.Exception;

                handle.ReturnWriteOperation(op);

                if (exception is not null)
                {
                    throw exception;
                }
                return default;
            }

            throw new OperationCanceledException(cancellationToken);
        }

        private static int CheckFileCall(SafeFileHandle handle, int result, Interop.ErrorInfo errorInfo)
        {
            if (result == -1)
            {
                throw Interop.GetExceptionForIoErrno(errorInfo, handle.Path);
            }
            return result;
        }

        private static long CheckFileCall(SafeFileHandle handle, long result, Interop.ErrorInfo errorInfo)
        {
            if (result == -1)
            {
                throw Interop.GetExceptionForIoErrno(errorInfo, handle.Path);
            }
            return result;
        }

        private static void CheckFileCall(SafeFileHandle handle, Interop.ErrorInfo errorInfo)
        {
            if (errorInfo.Error != Interop.Error.SUCCESS)
            {
                throw Interop.GetExceptionForIoErrno(errorInfo, handle.Path);
            }
        }

        private static bool IsPending(Interop.ErrorInfo errorInfo)
            => errorInfo.Error is Interop.Error.EAGAIN or Interop.Error.EWOULDBLOCK;

        /// <summary>
        /// Checks the last error after a failed pread/pwrite/preadv/pwritev call
        /// and returns true if the error indicates a non-seekable file type (ENXIO or ESPIPE).
        /// </summary>
        private static bool ShouldFallBackToNonOffsetSyscall(Interop.ErrorInfo lastError)
            => lastError.Error is Interop.Error.ENXIO or Interop.Error.ESPIPE;
    }
}
