// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Strategies;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class RandomAccess
    {
        private static readonly IOCompletionCallback s_callback = AllocateCallback();

        internal static unsafe void SetFileLength(SafeFileHandle handle, long length)
        {
            var eofInfo = new Interop.Kernel32.FILE_END_OF_FILE_INFO
            {
                EndOfFile = length
            };

            if (!Interop.Kernel32.SetFileInformationByHandle(
                handle,
                Interop.Kernel32.FileEndOfFileInfo,
                &eofInfo,
                (uint)sizeof(Interop.Kernel32.FILE_END_OF_FILE_INFO)))
            {
                int errorCode = Marshal.GetLastPInvokeError();

                throw errorCode == Interop.Errors.ERROR_INVALID_PARAMETER
                    ? new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_FileLengthTooBig)
                    : Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path);
            }
        }

        internal static unsafe int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            if (handle.IsAsync)
            {
                return ReadSyncUsingAsyncHandle(handle, buffer, fileOffset);
            }

            NativeOverlapped overlapped = GetNativeOverlappedForSyncHandle(handle, fileOffset);
            fixed (byte* pinned = &MemoryMarshal.GetReference(buffer))
            {
                if (Interop.Kernel32.ReadFile(handle, pinned, buffer.Length, out int numBytesRead, &overlapped) != 0)
                {
                    return numBytesRead;
                }

                int errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                switch (errorCode)
                {
                    case Interop.Errors.ERROR_HANDLE_EOF:
                        // https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile#synchronization-and-file-position :
                        // "If lpOverlapped is not NULL, then when a synchronous read operation reaches the end of a file,
                        // ReadFile returns FALSE and GetLastError returns ERROR_HANDLE_EOF"
                        return numBytesRead;
                    case Interop.Errors.ERROR_BROKEN_PIPE: // For pipes, ERROR_BROKEN_PIPE is the normal end of the pipe.
                    case Interop.Errors.ERROR_INVALID_PARAMETER when IsEndOfFileForNoBuffering(handle, fileOffset):
                        return 0;
                    default:
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path);
                }
            }
        }

        private static unsafe int ReadSyncUsingAsyncHandle(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            handle.EnsureThreadPoolBindingInitialized();

            CallbackResetEvent resetEvent = new CallbackResetEvent(handle.ThreadPoolBinding!);
            NativeOverlapped* overlapped = null;

            try
            {
                overlapped = GetNativeOverlappedForAsyncHandle(handle, fileOffset, resetEvent);

                fixed (byte* pinned = &MemoryMarshal.GetReference(buffer))
                {
                    Interop.Kernel32.ReadFile(handle, pinned, buffer.Length, IntPtr.Zero, overlapped);

                    int errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    if (errorCode == Interop.Errors.ERROR_IO_PENDING)
                    {
                        resetEvent.WaitOne();
                        errorCode = Interop.Errors.ERROR_SUCCESS;
                    }

                    if (errorCode == Interop.Errors.ERROR_SUCCESS)
                    {
                        int result = 0;
                        if (Interop.Kernel32.GetOverlappedResult(handle, overlapped, ref result, bWait: false))
                        {
                            Debug.Assert(result >= 0 && result <= buffer.Length, $"GetOverlappedResult returned {result} for {buffer.Length} bytes request");
                            return result;
                        }

                        errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    }
                    else
                    {
                        // The initial errorCode was neither ERROR_IO_PENDING nor ERROR_SUCCESS, so the operation
                        // failed with an error and the callback won't be invoked.  We thus need to decrement the
                        // ref count on the resetEvent that was initialized to a value under the expectation that
                        // the callback would be invoked and decrement it.
                        resetEvent.ReleaseRefCount(overlapped);
                    }

                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_HANDLE_EOF: // logically success with 0 bytes read (read at end of file)
                        case Interop.Errors.ERROR_BROKEN_PIPE:
                        case Interop.Errors.ERROR_INVALID_PARAMETER when IsEndOfFileForNoBuffering(handle, fileOffset):
                            // EOF on a pipe. Callback will not be called.
                            // We clear the overlapped status bit for this special case (failure
                            // to do so looks like we are freeing a pending overlapped later).
                            overlapped->InternalLow = IntPtr.Zero;
                            return 0;

                        default:
                            throw Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path);
                    }
                }
            }
            finally
            {
                if (overlapped != null)
                {
                    resetEvent.ReleaseRefCount(overlapped);
                }

                resetEvent.Dispose();
            }
        }

        internal static unsafe void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            if (handle.IsAsync)
            {
                WriteSyncUsingAsyncHandle(handle, buffer, fileOffset);
                return;
            }

            NativeOverlapped overlapped = GetNativeOverlappedForSyncHandle(handle, fileOffset);
            fixed (byte* pinned = &MemoryMarshal.GetReference(buffer))
            {
                if (Interop.Kernel32.WriteFile(handle, pinned, buffer.Length, out int numBytesWritten, &overlapped) != 0)
                {
                    Debug.Assert(numBytesWritten == buffer.Length);
                    return;
                }

                int errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                switch (errorCode)
                {
                    case Interop.Errors.ERROR_NO_DATA: // EOF on a pipe
                        return;
                    default:
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path);
                }
            }
        }

        private static unsafe void WriteSyncUsingAsyncHandle(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            handle.EnsureThreadPoolBindingInitialized();

            CallbackResetEvent resetEvent = new CallbackResetEvent(handle.ThreadPoolBinding!);
            NativeOverlapped* overlapped = null;

            try
            {
                overlapped = GetNativeOverlappedForAsyncHandle(handle, fileOffset, resetEvent);

                fixed (byte* pinned = &MemoryMarshal.GetReference(buffer))
                {
                    Interop.Kernel32.WriteFile(handle, pinned, buffer.Length, IntPtr.Zero, overlapped);

                    int errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    if (errorCode == Interop.Errors.ERROR_IO_PENDING)
                    {
                        resetEvent.WaitOne();
                        errorCode = Interop.Errors.ERROR_SUCCESS;
                    }

                    if (errorCode == Interop.Errors.ERROR_SUCCESS)
                    {
                        int result = 0;
                        if (Interop.Kernel32.GetOverlappedResult(handle, overlapped, ref result, bWait: false))
                        {
                            Debug.Assert(result == buffer.Length, $"GetOverlappedResult returned {result} for {buffer.Length} bytes request");
                            return;
                        }

                        errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    }
                    else
                    {
                        // The initial errorCode was neither ERROR_IO_PENDING nor ERROR_SUCCESS, so the operation
                        // failed with an error and the callback won't be invoked.  We thus need to decrement the
                        // ref count on the resetEvent that was initialized to a value under the expectation that
                        // the callback would be invoked and decrement it.
                        resetEvent.ReleaseRefCount(overlapped);
                    }

                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_NO_DATA:
                            // For pipes, ERROR_NO_DATA is not an error, but the pipe is closing.
                            return;

                        case Interop.Errors.ERROR_INVALID_PARAMETER:
                            // ERROR_INVALID_PARAMETER may be returned for writes
                            // where the position is too large or for synchronous writes
                            // to a handle opened asynchronously.
                            throw new IOException(SR.IO_FileTooLong);

                        default:
                            throw Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path);
                    }
                }
            }
            finally
            {
                if (overlapped != null)
                {
                    resetEvent.ReleaseRefCount(overlapped);
                }

                resetEvent.Dispose();
            }
        }

        internal static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
        {
            if (handle.IsAsync)
            {
                (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) = QueueAsyncReadFile(handle, buffer, fileOffset, cancellationToken, strategy);

                if (vts is not null)
                {
                    return new ValueTask<int>(vts, vts.Version);
                }

                if (errorCode == 0)
                {
                    return ValueTask.FromResult(0);
                }

                return ValueTask.FromException<int>(Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path));
            }

            return ScheduleSyncReadAtOffsetAsync(handle, buffer, fileOffset, cancellationToken, strategy);
        }

        private static unsafe (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) QueueAsyncReadFile(SafeFileHandle handle, Memory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            handle.EnsureThreadPoolBindingInitialized();

            SafeFileHandle.OverlappedValueTaskSource vts = handle.GetOverlappedValueTaskSource();
            int errorCode = Interop.Errors.ERROR_SUCCESS;
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(buffer, fileOffset, strategy);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Queue an async ReadFile operation.
                if (Interop.Kernel32.ReadFile(handle, (byte*)vts._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, nativeOverlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_IO_PENDING:
                            // Common case: IO was initiated, completion will be handled by callback.
                            // Register for cancellation now that the operation has been initiated.
                            vts.RegisterForCancellation(cancellationToken);
                            break;

                        case Interop.Errors.ERROR_HANDLE_EOF: // logically success with 0 bytes read (read at end of file)
                        case Interop.Errors.ERROR_BROKEN_PIPE:
                        case Interop.Errors.ERROR_INVALID_PARAMETER when IsEndOfFileForNoBuffering(handle, fileOffset):
                            // EOF on a pipe. Callback will not be called.
                            // We clear the overlapped status bit for this special case (failure
                            // to do so looks like we are freeing a pending overlapped later).
                            nativeOverlapped->InternalLow = IntPtr.Zero;
                            vts.Dispose();
                            return (null, 0);

                        default:
                            // Error. Callback will not be called.
                            vts.Dispose();
                            return (null, errorCode);
                    }
                }
            }
            catch
            {
                vts.Dispose();
                throw;
            }
            finally
            {
                if (errorCode != Interop.Errors.ERROR_IO_PENDING && errorCode != Interop.Errors.ERROR_SUCCESS)
                {
                    strategy?.OnIncompleteOperation(buffer.Length, 0);
                }
            }

            // Completion handled by callback.
            vts.FinishedScheduling();
            return (vts, -1);
        }

        internal static ValueTask WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
        {
            if (handle.IsAsync)
            {
                (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) = QueueAsyncWriteFile(handle, buffer, fileOffset, cancellationToken, strategy);

                if (vts is not null)
                {
                    return new ValueTask(vts, vts.Version);
                }

                if (errorCode == 0)
                {
                    return ValueTask.CompletedTask;
                }

                return ValueTask.FromException(Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path));
            }

            return ScheduleSyncWriteAtOffsetAsync(handle, buffer, fileOffset, cancellationToken, strategy);
        }

        private static unsafe (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) QueueAsyncWriteFile(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            handle.EnsureThreadPoolBindingInitialized();

            SafeFileHandle.OverlappedValueTaskSource vts = handle.GetOverlappedValueTaskSource();
            int errorCode = Interop.Errors.ERROR_SUCCESS;
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(buffer, fileOffset, strategy);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Queue an async WriteFile operation.
                if (Interop.Kernel32.WriteFile(handle, (byte*)vts._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, nativeOverlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_IO_PENDING:
                            // Common case: IO was initiated, completion will be handled by callback.
                            // Register for cancellation now that the operation has been initiated.
                            vts.RegisterForCancellation(cancellationToken);
                            break;
                        case Interop.Errors.ERROR_NO_DATA: // EOF on a pipe. IO callback will not be called.
                            vts.Dispose();
                            return (null, 0);
                        default:
                            // Error. Callback will not be invoked.
                            vts.Dispose();
                            return (null, errorCode);
                    }
                }
            }
            catch
            {
                vts.Dispose();
                throw;
            }
            finally
            {
                if (errorCode != Interop.Errors.ERROR_IO_PENDING && errorCode != Interop.Errors.ERROR_SUCCESS)
                {
                    strategy?.OnIncompleteOperation(buffer.Length, 0);
                }
            }

            // Completion handled by callback.
            vts.FinishedScheduling();
            return (vts, -1);
        }

        internal static long ReadScatterAtOffset(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
        {
            long total = 0;

            // ReadFileScatter does not support sync handles, so we just call ReadFile in a loop
            int buffersCount = buffers.Count;
            for (int i = 0; i < buffersCount; i++)
            {
                Span<byte> span = buffers[i].Span;
                int read = ReadAtOffset(handle, span, fileOffset + total);
                total += read;

                // We stop on the first incomplete read.
                // Most probably there is no more data available and the next read is going to return 0 (EOF).
                if (read != span.Length)
                {
                    break;
                }
            }

            return total;
        }

        internal static void WriteGatherAtOffset(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
        {
            // WriteFileGather does not support sync handles, so we just call WriteFile in a loop
            int bytesWritten = 0;
            int buffersCount = buffers.Count;
            for (int i = 0; i < buffersCount; i++)
            {
                ReadOnlySpan<byte> span = buffers[i].Span;
                WriteAtOffset(handle, span, fileOffset + bytesWritten);
                bytesWritten += span.Length;
            }
        }

        // From https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfilescatter:
        // "The file handle must be created with [...] the FILE_FLAG_OVERLAPPED and FILE_FLAG_NO_BUFFERING flags."
        private static bool CanUseScatterGatherWindowsAPIs(SafeFileHandle handle)
            => handle.IsAsync && ((handle.GetFileOptions() & SafeFileHandle.NoBuffering) != 0);

        // From the same source:
        // "Each buffer must be at least the size of a system memory page and must be aligned on a system
        // memory page size boundary. The system reads/writes one system memory page of data into/from each buffer."
        // This method returns true if the buffers can be used by
        // the Windows scatter/gather API, which happens when they are:
        // 1. aligned at page size boundaries
        // 2. exactly one page long each (our own requirement to prevent partial reads)
        // 3. not bigger than 2^32 - 1 in total
        // This function is also responsible for pinning the buffers if they
        // are suitable and they must be unpinned after the I/O operation completes.
        // It also returns a pointer with the segments to be passed to the
        // Windows API, and the total size of the buffers that is needed as well.
        // The pinned MemoryHandles and the pointer to the segments must be cleaned-up
        // with the CleanupScatterGatherBuffers method.
        private static unsafe bool TryPrepareScatterGatherBuffers<T, THandler>(IReadOnlyList<T> buffers,
            [NotNullWhen(true)] out MemoryHandle[]? handlesToDispose, out IntPtr segmentsPtr, out int totalBytes)
            where T : struct
            where THandler : struct, IMemoryHandler<T>
        {
            int pageSize = Environment.SystemPageSize;
            Debug.Assert(BitOperations.IsPow2(pageSize), "Page size is not a power of two.");
            // We take advantage of the fact that the page size is
            // a power of two to avoid an expensive modulo operation.
            long alignedAtPageSizeMask = pageSize - 1;

            int buffersCount = buffers.Count;
            handlesToDispose = null;
            segmentsPtr = IntPtr.Zero;
            totalBytes = 0;

            long* segments = null;

            bool success = false;
            try
            {
                long totalBytes64 = 0;
                for (int i = 0; i < buffersCount; i++)
                {
                    T buffer = buffers[i];
                    int length = THandler.GetLength(in buffer);
                    totalBytes64 += length;
                    if (length != pageSize || totalBytes64 > int.MaxValue)
                    {
                        return false;
                    }

                    MemoryHandle handle = THandler.Pin(in buffer);
                    long ptr = (long)handle.Pointer;
                    if ((ptr & alignedAtPageSizeMask) != 0)
                    {
                        handle.Dispose();
                        return false;
                    }

                    // We avoid allocations if there are no
                    // buffers or the first one is unacceptable.
                    (handlesToDispose ??= new MemoryHandle[buffersCount])[i] = handle;
                    if (segments == null)
                    {
                        // "The array must contain enough elements to store nNumberOfBytesToWrite
                        // bytes of data, and one element for the terminating NULL."
                        segments = (long*)NativeMemory.Alloc((nuint)buffersCount + 1, sizeof(long));
                        segments[buffersCount] = 0;
                    }
                    segments[i] = ptr;
                }

                segmentsPtr = (IntPtr)segments;
                totalBytes = (int)totalBytes64;
                success = true;
                return handlesToDispose != null;
            }
            finally
            {
                if (!success)
                {
                    CleanupScatterGatherBuffers(handlesToDispose, (IntPtr)segments);
                }
            }
        }

        private static unsafe void CleanupScatterGatherBuffers(MemoryHandle[]? handlesToDispose, IntPtr segmentsPtr)
        {
            if (handlesToDispose != null)
            {
                foreach (MemoryHandle handle in handlesToDispose)
                {
                    handle.Dispose();
                }
            }

            if (segmentsPtr != IntPtr.Zero)
            {
                NativeMemory.Free((void*)segmentsPtr);
            }
        }

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
        {
            if (!handle.IsAsync)
            {
                return ScheduleSyncReadScatterAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);
            }

            if (CanUseScatterGatherWindowsAPIs(handle)
                && TryPrepareScatterGatherBuffers<Memory<byte>, MemoryHandler>(buffers, out MemoryHandle[]? handlesToDispose, out IntPtr segmentsPtr, out int totalBytes))
            {
                return ReadScatterAtOffsetSingleSyscallAsync(handle, handlesToDispose, segmentsPtr, fileOffset, totalBytes, cancellationToken);
            }

            return ReadScatterAtOffsetMultipleSyscallsAsync(handle, buffers, fileOffset, cancellationToken);
        }

        private static async ValueTask<long> ReadScatterAtOffsetSingleSyscallAsync(SafeFileHandle handle, MemoryHandle[] handlesToDispose, IntPtr segmentsPtr, long fileOffset, int totalBytes, CancellationToken cancellationToken)
        {
            try
            {
                return await ReadFileScatterAsync(handle, segmentsPtr, totalBytes, fileOffset, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CleanupScatterGatherBuffers(handlesToDispose, segmentsPtr);
            }
        }

        private static unsafe ValueTask<int> ReadFileScatterAsync(SafeFileHandle handle, IntPtr segmentsPtr, int bytesToRead, long fileOffset, CancellationToken cancellationToken)
        {
            handle.EnsureThreadPoolBindingInitialized();

            SafeFileHandle.OverlappedValueTaskSource vts = handle.GetOverlappedValueTaskSource();
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(Memory<byte>.Empty, fileOffset);
                Debug.Assert(segmentsPtr != IntPtr.Zero);

                if (Interop.Kernel32.ReadFileScatter(handle, (long*)segmentsPtr, bytesToRead, IntPtr.Zero, nativeOverlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    int errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_IO_PENDING:
                            // Common case: IO was initiated, completion will be handled by callback.
                            // Register for cancellation now that the operation has been initiated.
                            vts.RegisterForCancellation(cancellationToken);
                            break;

                        case Interop.Errors.ERROR_HANDLE_EOF: // logically success with 0 bytes read (read at end of file)
                        case Interop.Errors.ERROR_BROKEN_PIPE:
                            // EOF on a pipe. Callback will not be called.
                            // We clear the overlapped status bit for this special case (failure
                            // to do so looks like we are freeing a pending overlapped later).
                            nativeOverlapped->InternalLow = IntPtr.Zero;
                            vts.Dispose();
                            return ValueTask.FromResult(0);

                        default:
                            // Error. Callback will not be called.
                            vts.Dispose();
                            return ValueTask.FromException<int>(Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path));
                    }
                }
            }
            catch
            {
                vts.Dispose();
                throw;
            }

            // Completion handled by callback.
            vts.FinishedScheduling();
            return new ValueTask<int>(vts, vts.Version);
        }

        private static async ValueTask<long> ReadScatterAtOffsetMultipleSyscallsAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
        {
            long total = 0;

            int buffersCount = buffers.Count;
            for (int i = 0; i < buffersCount; i++)
            {
                Memory<byte> buffer = buffers[i];
                int read = await ReadAtOffsetAsync(handle, buffer, fileOffset + total, cancellationToken).ConfigureAwait(false);
                total += read;

                if (read != buffer.Length)
                {
                    break;
                }
            }

            return total;
        }

        private static ValueTask WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
        {
            if (!handle.IsAsync)
            {
                return ScheduleSyncWriteGatherAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);
            }

            if (CanUseScatterGatherWindowsAPIs(handle)
                && TryPrepareScatterGatherBuffers<ReadOnlyMemory<byte>, ReadOnlyMemoryHandler>(buffers, out MemoryHandle[]? handlesToDispose, out IntPtr segmentsPtr, out int totalBytes))
            {
                return WriteGatherAtOffsetSingleSyscallAsync(handle, handlesToDispose, segmentsPtr, fileOffset, totalBytes, cancellationToken);
            }

            return WriteGatherAtOffsetMultipleSyscallsAsync(handle, buffers, fileOffset, cancellationToken);
        }

        private static async ValueTask WriteGatherAtOffsetSingleSyscallAsync(SafeFileHandle handle, MemoryHandle[] handlesToDispose, IntPtr segmentsPtr, long fileOffset, int totalBytes, CancellationToken cancellationToken)
        {
            try
            {
                await WriteFileGatherAsync(handle, segmentsPtr, totalBytes, fileOffset, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CleanupScatterGatherBuffers(handlesToDispose, segmentsPtr);
            }
        }

        private static unsafe ValueTask WriteFileGatherAsync(SafeFileHandle handle, IntPtr segmentsPtr, int bytesToWrite, long fileOffset, CancellationToken cancellationToken)
        {
            handle.EnsureThreadPoolBindingInitialized();

            SafeFileHandle.OverlappedValueTaskSource vts = handle.GetOverlappedValueTaskSource();
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(ReadOnlyMemory<byte>.Empty, fileOffset);
                Debug.Assert(segmentsPtr != IntPtr.Zero);

                // Queue an async WriteFile operation.
                if (Interop.Kernel32.WriteFileGather(handle, (long*)segmentsPtr, bytesToWrite, IntPtr.Zero, nativeOverlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    int errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    if (errorCode == Interop.Errors.ERROR_IO_PENDING)
                    {
                        // Common case: IO was initiated, completion will be handled by callback.
                        // Register for cancellation now that the operation has been initiated.
                        vts.RegisterForCancellation(cancellationToken);
                    }
                    else
                    {
                        // Error. Callback will not be invoked.
                        vts.Dispose();
                        return errorCode == Interop.Errors.ERROR_NO_DATA // EOF on a pipe. IO callback will not be called.
                            ? ValueTask.CompletedTask
                            : ValueTask.FromException(SafeFileHandle.OverlappedValueTaskSource.GetIOError(errorCode, path: null));
                    }
                }
            }
            catch
            {
                vts.Dispose();
                throw;
            }

            // Completion handled by callback.
            vts.FinishedScheduling();
            return new ValueTask(vts, vts.Version);
        }

        private static async ValueTask WriteGatherAtOffsetMultipleSyscallsAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
        {
            int buffersCount = buffers.Count;
            for (int i = 0; i < buffersCount; i++)
            {
                ReadOnlyMemory<byte> rom = buffers[i];
                await WriteAtOffsetAsync(handle, rom, fileOffset, cancellationToken).ConfigureAwait(false);
                fileOffset += rom.Length;
            }
        }

        private static unsafe NativeOverlapped* GetNativeOverlappedForAsyncHandle(SafeFileHandle handle, long fileOffset, CallbackResetEvent resetEvent)
        {
            // After SafeFileHandle is bound to ThreadPool, we need to use ThreadPoolBinding
            // to allocate a native overlapped and provide a valid callback.
            NativeOverlapped* result = handle.ThreadPoolBinding!.UnsafeAllocateNativeOverlapped(s_callback, resetEvent, null);

            if (handle.CanSeek)
            {
                result->OffsetLow = unchecked((int)fileOffset);
                result->OffsetHigh = (int)(fileOffset >> 32);
            }

            // From https://docs.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-getoverlappedresult:
            // "If the hEvent member of the OVERLAPPED structure is NULL, the system uses the state of the hFile handle to signal when the operation has been completed.
            // Use of file, named pipe, or communications-device handles for this purpose is discouraged.
            // It is safer to use an event object because of the confusion that can occur when multiple simultaneous overlapped operations
            // are performed on the same file, named pipe, or communications device.
            // In this situation, there is no way to know which operation caused the object's state to be signaled."
            // Since we want RandomAccess APIs to be thread-safe, we provide a dedicated wait handle.
            result->EventHandle = resetEvent.SafeWaitHandle.DangerousGetHandle();

            return result;
        }

        private static NativeOverlapped GetNativeOverlappedForSyncHandle(SafeFileHandle handle, long fileOffset)
        {
            Debug.Assert(!handle.IsAsync);

            NativeOverlapped result = default;
            if (handle.CanSeek)
            {
                result.OffsetLow = unchecked((int)fileOffset);
                result.OffsetHigh = (int)(fileOffset >> 32);
            }
            return result;
        }

        private static unsafe IOCompletionCallback AllocateCallback()
        {
            return new IOCompletionCallback(Callback);

            static unsafe void Callback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
            {
                CallbackResetEvent state = (CallbackResetEvent)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped)!;
                state.ReleaseRefCount(pOverlapped);
            }
        }

        // From https://docs.microsoft.com/en-us/windows/win32/fileio/file-buffering:
        // "File access sizes, including the optional file offset in the OVERLAPPED structure,
        // if specified, must be for a number of bytes that is an integer multiple of the volume sector size."
        // So if buffer and physical sector size is 4096 and the file size is 4097:
        // the read from offset=0 reads 4096 bytes
        // the read from offset=4096 reads 1 byte
        // the read from offset=4097 fails with ERROR_INVALID_PARAMETER (the offset is not a multiple of sector size)
        // Based on feedback received from customers (https://github.com/dotnet/runtime/issues/62851),
        // it was decided to not throw, but just return 0.
        private static bool IsEndOfFileForNoBuffering(SafeFileHandle fileHandle, long fileOffset)
            => fileHandle.IsNoBuffering && fileHandle.CanSeek && fileOffset >= fileHandle.GetFileLength();

        // We need to store the reference count (see the comment in ReleaseRefCount) and an EventHandle to signal the completion.
        // We could keep these two things separate, but since ManualResetEvent is sealed and we want to avoid any extra allocations, this type has been created.
        // It's basically ManualResetEvent with reference count.
        private sealed class CallbackResetEvent : EventWaitHandle
        {
            private readonly ThreadPoolBoundHandle _threadPoolBoundHandle;
            private int _freeWhenZero = 2; // one for the callback and another for the method that calls GetOverlappedResult

            internal CallbackResetEvent(ThreadPoolBoundHandle threadPoolBoundHandle) : base(initialState: false, EventResetMode.ManualReset)
            {
                _threadPoolBoundHandle = threadPoolBoundHandle;
            }

            internal unsafe void ReleaseRefCount(NativeOverlapped* pOverlapped)
            {
                // Each SafeFileHandle opened for async IO is bound to ThreadPool.
                // It requires us to provide a callback even if we want to use EventHandle and use GetOverlappedResult to obtain the result.
                // There can be a race condition between the call to GetOverlappedResult and the callback invocation,
                // so we need to track the number of references, and when it drops to zero, then free the native overlapped.
                if (Interlocked.Decrement(ref _freeWhenZero) == 0)
                {
                    _threadPoolBoundHandle.FreeNativeOverlapped(pOverlapped);
                }
            }
        }

        // Abstracts away the type signature incompatibility between Memory and ReadOnlyMemory.
        private interface IMemoryHandler<T>
        {
            static abstract int GetLength(in T memory);
            static abstract MemoryHandle Pin(in T memory);
        }

        private readonly struct MemoryHandler : IMemoryHandler<Memory<byte>>
        {
            public static int GetLength(in Memory<byte> memory) => memory.Length;
            public static MemoryHandle Pin(in Memory<byte> memory) => memory.Pin();
        }

        private readonly struct ReadOnlyMemoryHandler : IMemoryHandler<ReadOnlyMemory<byte>>
        {
            public static int GetLength(in ReadOnlyMemory<byte> memory) => memory.Length;
            public static MemoryHandle Pin(in ReadOnlyMemory<byte> memory) => memory.Pin();
        }
    }
}
