// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Strategies;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class RandomAccess
    {
        private static readonly IOCompletionCallback s_callback = AllocateCallback();

        internal static unsafe long GetFileLength(SafeFileHandle handle)
        {
            Interop.Kernel32.FILE_STANDARD_INFO info;

            if (!Interop.Kernel32.GetFileInformationByHandleEx(handle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(handle.Path);
            }

            return info.EndOfFile;
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
                    case Interop.Errors.ERROR_BROKEN_PIPE:
                        // For pipes, ERROR_BROKEN_PIPE is the normal end of the pipe.
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
                overlapped = GetNativeOverlappedForAsyncHandle(handle.ThreadPoolBinding!, fileOffset, resetEvent);

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

                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_HANDLE_EOF: // logically success with 0 bytes read (read at end of file)
                        case Interop.Errors.ERROR_BROKEN_PIPE:
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
                    resetEvent.FreeNativeOverlapped(overlapped);
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
                overlapped = GetNativeOverlappedForAsyncHandle(handle.ThreadPoolBinding!, fileOffset, resetEvent);

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
                    resetEvent.FreeNativeOverlapped(overlapped);
                }

                resetEvent.Dispose();
            }
        }

        internal static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
        {
            if (handle.IsAsync)
            {
                (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) = QueueAsyncReadFile(handle, buffer, fileOffset, cancellationToken);

                if (vts is not null)
                {
                    return new ValueTask<int>(vts, vts.Version);
                }

                if (errorCode == 0)
                {
                    return ValueTask.FromResult(0);
                }

                return ValueTask.FromException<int>(Win32Marshal.GetExceptionForWin32Error(errorCode));
            }

            return ScheduleSyncReadAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);
        }

        internal static unsafe (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) QueueAsyncReadFile(SafeFileHandle handle, Memory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken, AsyncWindowsFileStreamStrategy? strategy = null)
        {
            handle.EnsureThreadPoolBindingInitialized();

            SafeFileHandle.OverlappedValueTaskSource vts = handle.GetOverlappedValueTaskSource();
            int errorCode = 0;
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
                    strategy?.OnIncompleteRead(buffer.Length, 0);
                }
            }

            // Completion handled by callback.
            vts.FinishedScheduling();
            return (vts, -1);
        }

        internal static ValueTask WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
        {
            if (handle.IsAsync)
            {
                (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) = QueueAsyncWriteFile(handle, buffer, fileOffset, cancellationToken);

                if (vts is not null)
                {
                    return new ValueTask(vts, vts.Version);
                }

                if (errorCode == 0)
                {
                    return ValueTask.CompletedTask;
                }

                return ValueTask.FromException(Win32Marshal.GetExceptionForWin32Error(errorCode));
            }

            return ScheduleSyncWriteAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);
        }

        internal static unsafe (SafeFileHandle.OverlappedValueTaskSource? vts, int errorCode) QueueAsyncWriteFile(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
        {
            handle.EnsureThreadPoolBindingInitialized();

            SafeFileHandle.OverlappedValueTaskSource vts = handle.GetOverlappedValueTaskSource();
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(buffer, fileOffset);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Queue an async WriteFile operation.
                if (Interop.Kernel32.WriteFile(handle, (byte*)vts._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, nativeOverlapped) == 0)
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

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
        {
            if (!handle.IsAsync)
            {
                return ScheduleSyncReadScatterAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);
            }

            if (CanUseScatterGatherWindowsAPIs(handle))
            {
                long totalBytes = 0;
                int buffersCount = buffers.Count;
                for (int i = 0; i < buffersCount; i++)
                {
                    totalBytes += buffers[i].Length;
                }

                if (totalBytes <= int.MaxValue) // the ReadFileScatter API uses int, not long
                {
                    return ReadScatterAtOffsetSingleSyscallAsync(handle, buffers, fileOffset, (int)totalBytes, cancellationToken);
                }
            }

            return ReadScatterAtOffsetMultipleSyscallsAsync(handle, buffers, fileOffset, cancellationToken);
        }

        // From https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfilescatter:
        // "The file handle must be created with the GENERIC_READ right, and the FILE_FLAG_OVERLAPPED and FILE_FLAG_NO_BUFFERING flags."
        private static bool CanUseScatterGatherWindowsAPIs(SafeFileHandle handle)
            => handle.IsAsync && ((handle.GetFileOptions() & SafeFileHandle.NoBuffering) != 0);

        private static async ValueTask<long> ReadScatterAtOffsetSingleSyscallAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, int totalBytes, CancellationToken cancellationToken)
        {
            int buffersCount = buffers.Count;
            if (buffersCount == 1)
            {
                // we have to await it because we can't cast a VT<int> to VT<long>
                return await ReadAtOffsetAsync(handle, buffers[0], fileOffset, cancellationToken).ConfigureAwait(false);
            }

            // "The array must contain enough elements to store nNumberOfBytesToWrite bytes of data, and one element for the terminating NULL. "
            long[] fileSegments = new long[buffersCount + 1];
            fileSegments[buffersCount] = 0;

            MemoryHandle[] memoryHandles = new MemoryHandle[buffersCount];
            MemoryHandle pinnedSegments = fileSegments.AsMemory().Pin();

            try
            {
                for (int i = 0; i < buffersCount; i++)
                {
                    Memory<byte> buffer = buffers[i];
                    MemoryHandle memoryHandle = buffer.Pin();
                    memoryHandles[i] = memoryHandle;

                    unsafe // awaits can't be in an unsafe context
                    {
                        fileSegments[i] = new IntPtr(memoryHandle.Pointer).ToInt64();
                    }
                }

                return await ReadFileScatterAsync(handle, pinnedSegments, totalBytes, fileOffset, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                foreach (MemoryHandle memoryHandle in memoryHandles)
                {
                    memoryHandle.Dispose();
                }
                pinnedSegments.Dispose();
            }
        }

        private static unsafe ValueTask<int> ReadFileScatterAsync(SafeFileHandle handle, MemoryHandle pinnedSegments, int bytesToRead, long fileOffset, CancellationToken cancellationToken)
        {
            handle.EnsureThreadPoolBindingInitialized();

            SafeFileHandle.OverlappedValueTaskSource vts = handle.GetOverlappedValueTaskSource();
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(Memory<byte>.Empty, fileOffset);
                Debug.Assert(pinnedSegments.Pointer != null);

                if (Interop.Kernel32.ReadFileScatter(handle, (long*)pinnedSegments.Pointer, bytesToRead, IntPtr.Zero, nativeOverlapped) == 0)
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

            if (CanUseScatterGatherWindowsAPIs(handle))
            {
                long totalBytes = 0;
                for (int i = 0; i < buffers.Count; i++)
                {
                    totalBytes += buffers[i].Length;
                }

                if (totalBytes <= int.MaxValue) // the ReadFileScatter API uses int, not long
                {
                    return WriteGatherAtOffsetSingleSyscallAsync(handle, buffers, fileOffset, (int)totalBytes, cancellationToken);
                }
            }

            return WriteGatherAtOffsetMultipleSyscallsAsync(handle, buffers, fileOffset, cancellationToken);
        }

        private static async ValueTask WriteGatherAtOffsetMultipleSyscallsAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
        {
            long bytesWritten = 0;
            int buffersCount = buffers.Count;
            for (int i = 0; i < buffersCount; i++)
            {
                ReadOnlyMemory<byte> rom = buffers[i];
                await WriteAtOffsetAsync(handle, rom, fileOffset + bytesWritten, cancellationToken).ConfigureAwait(false);
                bytesWritten += rom.Length;
            }
        }

        private static ValueTask WriteGatherAtOffsetSingleSyscallAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, int totalBytes, CancellationToken cancellationToken)
        {
            if (buffers.Count == 1)
            {
                return WriteAtOffsetAsync(handle, buffers[0], fileOffset, cancellationToken);
            }

            return Core(handle, buffers, fileOffset, totalBytes, cancellationToken);

            static async ValueTask Core(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, int totalBytes, CancellationToken cancellationToken)
            {
                // "The array must contain enough elements to store nNumberOfBytesToWrite bytes of data, and one element for the terminating NULL. "
                int buffersCount = buffers.Count;
                long[] fileSegments = new long[buffersCount + 1];
                fileSegments[buffersCount] = 0;

                MemoryHandle[] memoryHandles = new MemoryHandle[buffersCount];
                MemoryHandle pinnedSegments = fileSegments.AsMemory().Pin();

                try
                {
                    for (int i = 0; i < buffersCount; i++)
                    {
                        ReadOnlyMemory<byte> buffer = buffers[i];
                        MemoryHandle memoryHandle = buffer.Pin();
                        memoryHandles[i] = memoryHandle;

                        unsafe // awaits can't be in an unsafe context
                        {
                            fileSegments[i] = new IntPtr(memoryHandle.Pointer).ToInt64();
                        }
                    }

                    await WriteFileGatherAsync(handle, pinnedSegments, totalBytes, fileOffset, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    foreach (MemoryHandle memoryHandle in memoryHandles)
                    {
                        memoryHandle.Dispose();
                    }
                    pinnedSegments.Dispose();
                }
            }
        }

        private static unsafe ValueTask WriteFileGatherAsync(SafeFileHandle handle, MemoryHandle pinnedSegments, int bytesToWrite, long fileOffset, CancellationToken cancellationToken)
        {
            handle.EnsureThreadPoolBindingInitialized();

            SafeFileHandle.OverlappedValueTaskSource vts = handle.GetOverlappedValueTaskSource();
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(ReadOnlyMemory<byte>.Empty, fileOffset);
                Debug.Assert(pinnedSegments.Pointer != null);

                // Queue an async WriteFile operation.
                if (Interop.Kernel32.WriteFileGather(handle, (long*)pinnedSegments.Pointer, bytesToWrite, IntPtr.Zero, nativeOverlapped) == 0)
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

        private static unsafe NativeOverlapped* GetNativeOverlappedForAsyncHandle(ThreadPoolBoundHandle threadPoolBinding, long fileOffset, CallbackResetEvent resetEvent)
        {
            // After SafeFileHandle is bound to ThreadPool, we need to use ThreadPoolBinding
            // to allocate a native overlapped and provide a valid callback.
            NativeOverlapped* result = threadPoolBinding.AllocateNativeOverlapped(s_callback, resetEvent, null);

            // For pipes the offsets are ignored by the OS
            result->OffsetLow = unchecked((int)fileOffset);
            result->OffsetHigh = (int)(fileOffset >> 32);

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
                state.FreeNativeOverlapped(pOverlapped);
            }
        }

        // We need to store the reference count (see the comment in FreeNativeOverlappedIfItIsSafe) and an EventHandle to signal the completion.
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

            internal unsafe void FreeNativeOverlapped(NativeOverlapped* pOverlapped)
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
    }
}
