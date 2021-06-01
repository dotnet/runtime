// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            int r = ReadFileNative(handle, buffer, true, &nativeOverlapped, out int errorCode);

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
                r = overlapped != null ?
                    (syncUsingOverlapped
                        ? Interop.Kernel32.ReadFile(handle, p, bytes.Length, out numBytesRead, overlapped)
                        : Interop.Kernel32.ReadFile(handle, p, bytes.Length, IntPtr.Zero, overlapped))
                    : Interop.Kernel32.ReadFile(handle, p, bytes.Length, out numBytesRead, IntPtr.Zero);
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
                r = overlapped != null ?
                    (syncUsingOverlapped
                        ? Interop.Kernel32.WriteFile(handle, p, buffer.Length, out numBytesWritten, overlapped)
                        : Interop.Kernel32.WriteFile(handle, p, buffer.Length, IntPtr.Zero, overlapped))
                    : Interop.Kernel32.WriteFile(handle, p, buffer.Length, out numBytesWritten, IntPtr.Zero);
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

        private static unsafe ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
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
                            return ValueTask.FromException<int>(SafeFileHandle.ValueTaskSource.GetIOError(errorCode, path: null));
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

        private static unsafe ValueTask<int> WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken)
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
