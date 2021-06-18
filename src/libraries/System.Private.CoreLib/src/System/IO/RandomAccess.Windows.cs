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
        internal static unsafe long GetFileLength(SafeFileHandle handle, string? path)
        {
            Interop.Kernel32.FILE_STANDARD_INFO info;

            if (!Interop.Kernel32.GetFileInformationByHandleEx(handle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(path);
            }

            return info.EndOfFile;
        }

        internal static unsafe int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset, string? path = null)
        {
            NativeOverlapped nativeOverlapped = GetNativeOverlapped(fileOffset);
            int r = ReadFileNative(handle, buffer, syncUsingOverlapped: true, &nativeOverlapped, out int errorCode);

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
                    {
                        ThrowHelper.ThrowArgumentException_HandleNotSync(nameof(handle));
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
                }
            }

            return r;
        }

        internal static unsafe int WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset, string? path = null)
        {
            NativeOverlapped nativeOverlapped = GetNativeOverlapped(fileOffset);
            int r = WriteFileNative(handle, buffer, true, &nativeOverlapped, out int errorCode);

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
                    {
                        throw new IOException(SR.IO_FileTooLongOrHandleNotSync);
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
                }
            }

            return r;
        }

        internal static unsafe int ReadFileNative(SafeFileHandle handle, Span<byte> bytes, bool syncUsingOverlapped, NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert(handle != null, "handle != null");

            int r;
            int numBytesRead = 0;

            fixed (byte* p = &MemoryMarshal.GetReference(bytes))
            {
                r = overlapped == null || syncUsingOverlapped ?
                        Interop.Kernel32.ReadFile(handle, p, bytes.Length, out numBytesRead, overlapped) :
                        Interop.Kernel32.ReadFile(handle, p, bytes.Length, IntPtr.Zero, overlapped);
            }

            if (r == 0)
            {
                errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);

                if (syncUsingOverlapped && errorCode == Interop.Errors.ERROR_HANDLE_EOF)
                {
                    // https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-readfile#synchronization-and-file-position :
                    // "If lpOverlapped is not NULL, then when a synchronous read operation reaches the end of a file,
                    // ReadFile returns FALSE and GetLastError returns ERROR_HANDLE_EOF"
                    return numBytesRead;
                }

                return -1;
            }
            else
            {
                errorCode = 0;
                return numBytesRead;
            }
        }

        internal static unsafe int WriteFileNative(SafeFileHandle handle, ReadOnlySpan<byte> buffer, bool syncUsingOverlapped, NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert(handle != null, "handle != null");

            int numBytesWritten = 0;
            int r;

            fixed (byte* p = &MemoryMarshal.GetReference(buffer))
            {
                r = overlapped == null || syncUsingOverlapped ?
                        Interop.Kernel32.WriteFile(handle, p, buffer.Length, out numBytesWritten, overlapped) :
                        Interop.Kernel32.WriteFile(handle, p, buffer.Length, IntPtr.Zero, overlapped);
            }

            if (r == 0)
            {
                errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                return -1;
            }
            else
            {
                errorCode = 0;
                return numBytesWritten;
            }
        }

        private static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
            => Map(QueueAsyncReadFile(handle, buffer, fileOffset, cancellationToken));

        private static ValueTask<int> Map((SafeFileHandle.ValueTaskSource? vts, int errorCode) tuple)
            => tuple.vts != null
                ? new ValueTask<int>(tuple.vts, tuple.vts.Version)
                : tuple.errorCode == 0 ? ValueTask.FromResult(0) : ValueTask.FromException<int>(Win32Marshal.GetExceptionForWin32Error(tuple.errorCode));

        internal static unsafe (SafeFileHandle.ValueTaskSource? vts, int errorCode) QueueAsyncReadFile(
            SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
        {
            SafeFileHandle.ValueTaskSource vts = handle.GetValueTaskSource();
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(buffer, fileOffset);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Queue an async ReadFile operation.
                if (Interop.Kernel32.ReadFile(handle, (byte*)vts._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, nativeOverlapped) == 0)
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

            // Completion handled by callback.
            vts.FinishedScheduling();
            return (vts, -1);
        }

        private static ValueTask<int> WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
           => Map(QueueAsyncWriteFile(handle, buffer, fileOffset, cancellationToken));

        internal static unsafe (SafeFileHandle.ValueTaskSource? vts, int errorCode) QueueAsyncWriteFile(
            SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
        {
            SafeFileHandle.ValueTaskSource vts = handle.GetValueTaskSource();
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

        private static long ReadScatterAtOffset(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
        {
            long total = 0;

            // ReadFileScatter does not support sync handles, so we just call ReadFile in a loop
            for (int i = 0; i < buffers.Count; i++)
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

        private static long WriteGatherAtOffset(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
        {
            long total = 0;

            // WriteFileGather does not support sync handles, so we just call WriteFile in a loop
            for (int i = 0; i < buffers.Count; i++)
            {
                ReadOnlySpan<byte> span = buffers[i].Span;
                int written = WriteAtOffset(handle, span, fileOffset + total);
                total += written;

                // We stop on the first incomplete write.
                // Most probably the disk became full and the next write is going to throw.
                if (written != span.Length)
                {
                    break;
                }
            }

            return total;
        }

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
        {
            if (CanUseScatterGatherWindowsAPIs(handle))
            {
                long totalBytes = 0;
                for (int i = 0; i < buffers.Count; i++)
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

        private static async ValueTask<long> ReadScatterAtOffsetSingleSyscallAsync(SafeFileHandle handle,
            IReadOnlyList<Memory<byte>> buffers, long fileOffset, int totalBytes, CancellationToken cancellationToken)
        {
            if (buffers.Count == 1)
            {
                // we have to await it because we can't cast a VT<int> to VT<long>
                return await ReadAtOffsetAsync(handle, buffers[0], fileOffset, cancellationToken).ConfigureAwait(false);
            }

            // "The array must contain enough elements to store nNumberOfBytesToWrite bytes of data, and one element for the terminating NULL. "
            long[] fileSegments = new long[buffers.Count + 1];
            fileSegments[buffers.Count] = 0;

            MemoryHandle[] memoryHandles = new MemoryHandle[buffers.Count];
            MemoryHandle pinnedSegments = fileSegments.AsMemory().Pin();

            try
            {
                for (int i = 0; i < buffers.Count; i++)
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

        private static unsafe ValueTask<int> ReadFileScatterAsync(SafeFileHandle handle, MemoryHandle pinnedSegments,
            int bytesToRead, long fileOffset, CancellationToken cancellationToken)
        {
            SafeFileHandle.ValueTaskSource vts = handle.GetValueTaskSource();
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
                            return ValueTask.FromException<int>(Win32Marshal.GetExceptionForWin32Error(errorCode));
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

        private static async ValueTask<long> ReadScatterAtOffsetMultipleSyscallsAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
        {
            long total = 0;

            for (int i = 0; i < buffers.Count; i++)
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

        private static ValueTask<long> WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
        {
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

        private static async ValueTask<long> WriteGatherAtOffsetMultipleSyscallsAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers,
            long fileOffset, CancellationToken cancellationToken)
        {
            long total = 0;

            for (int i = 0; i < buffers.Count; i++)
            {
                ReadOnlyMemory<byte> buffer = buffers[i];
                int written = await WriteAtOffsetAsync(handle, buffer, fileOffset + total, cancellationToken).ConfigureAwait(false);
                total += written;

                if (written != buffer.Length)
                {
                    break;
                }
            }

            return total;
        }

        private static async ValueTask<long> WriteGatherAtOffsetSingleSyscallAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers,
            long fileOffset, int totalBytes, CancellationToken cancellationToken)
        {
            if (buffers.Count == 1)
            {
                return await WriteAtOffsetAsync(handle, buffers[0], fileOffset, cancellationToken).ConfigureAwait(false);
            }

            // "The array must contain enough elements to store nNumberOfBytesToWrite bytes of data, and one element for the terminating NULL. "
            long[] fileSegments = new long[buffers.Count + 1];
            fileSegments[buffers.Count] = 0;

            MemoryHandle[] memoryHandles = new MemoryHandle[buffers.Count];
            MemoryHandle pinnedSegments = fileSegments.AsMemory().Pin();

            try
            {
                for (int i = 0; i < buffers.Count; i++)
                {
                    ReadOnlyMemory<byte> buffer = buffers[i];
                    MemoryHandle memoryHandle = buffer.Pin();
                    memoryHandles[i] = memoryHandle;

                    unsafe // awaits can't be in an unsafe context
                    {
                        fileSegments[i] = new IntPtr(memoryHandle.Pointer).ToInt64();
                    }
                }

                return await WriteFileGatherAsync(handle, pinnedSegments, totalBytes, fileOffset, cancellationToken).ConfigureAwait(false);
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

        private static unsafe ValueTask<int> WriteFileGatherAsync(SafeFileHandle handle, MemoryHandle pinnedSegments,
            int bytesToWrite, long fileOffset, CancellationToken cancellationToken)
        {
            SafeFileHandle.ValueTaskSource vts = handle.GetValueTaskSource();
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
                            ? ValueTask.FromResult<int>(0)
                            : ValueTask.FromException<int>(SafeFileHandle.ValueTaskSource.GetIOError(errorCode, path: null));
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

        private static NativeOverlapped GetNativeOverlapped(long fileOffset)
        {
            NativeOverlapped nativeOverlapped = default;
            // For pipes the offsets are ignored by the OS
            nativeOverlapped.OffsetLow = unchecked((int)fileOffset);
            nativeOverlapped.OffsetHigh = (int)(fileOffset >> 32);

            return nativeOverlapped;
        }
    }
}
