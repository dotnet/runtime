// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal static class FileHandleHelper
    {
        internal static unsafe (OverlappedValueTaskSource? vts, int errorCode) QueueAsyncReadFile(
            OverlappedValueTaskSource vts, SafeHandle handle, Memory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken, Stream? owner)
        {
            int errorCode = Interop.Errors.ERROR_SUCCESS;
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(buffer, fileOffset, owner);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Queue an async ReadFile operation.
                if (Interop.Kernel32.ReadFile(handle, (byte*)vts._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, nativeOverlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);

                    if (errorCode == Interop.Errors.ERROR_IO_PENDING)
                    {
                        // Common case: IO was initiated, completion will be handled by callback.
                        // Register for cancellation now that the operation has been initiated.
                        vts.RegisterForCancellation(cancellationToken);
                    }
                    else if (IsEndOfFile(errorCode, handle, fileOffset))
                    {
                        // EOF on a pipe. Callback will not be called.
                        // We clear the overlapped status bit for this special case (failure
                        // to do so looks like we are freeing a pending overlapped later).
                        nativeOverlapped->InternalLow = IntPtr.Zero;
                        vts.Dispose();
                        return (null, 0);
                    }
                    else
                    {
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
                    if (owner is not null)
                    {
                        vts.HandleIncomplete(owner, (uint)buffer.Length, 0);
                    }
                }
            }

            // Completion handled by callback.
            vts.FinishedScheduling();
            return (vts, -1);
        }

        internal static unsafe (OverlappedValueTaskSource? vts, int errorCode) QueueAsyncWriteFile(
            OverlappedValueTaskSource vts, SafeHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset,
            CancellationToken cancellationToken, Stream? owner)
        {
            int errorCode = Interop.Errors.ERROR_SUCCESS;
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(buffer, fileOffset, owner);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Queue an async WriteFile operation.
                if (Interop.Kernel32.WriteFile(handle, (byte*)vts._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, nativeOverlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_IO_PENDING:
                            // Common case: IO was initiated, completion will be handled by callback.
                            // Register for cancellation now that the operation has been initiated.
                            vts.RegisterForCancellation(cancellationToken);
                            break;
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
                    if (owner is not null)
                    {
                        vts.HandleIncomplete(owner, (uint)buffer.Length, 0);
                    }
                }
            }

            // Completion handled by callback.
            vts.FinishedScheduling();
            return (vts, -1);
        }

        internal static int GetLastWin32ErrorAndDisposeHandleIfInvalid(SafeHandle handle)
        {
            int errorCode = Marshal.GetLastPInvokeError();

            // If ERROR_INVALID_HANDLE is returned, it doesn't suffice to set
            // the handle as invalid; the handle must also be closed.
            //
            // Marking the handle as invalid but not closing the handle
            // resulted in exceptions during finalization and locked column
            // values (due to invalid but unclosed handle) in SQL Win32FileStream
            // scenarios.
            //
            // A more mainstream scenario involves accessing a file on a
            // network share. ERROR_INVALID_HANDLE may occur because the network
            // connection was dropped and the server closed the handle. However,
            // the client side handle is still open and even valid for certain
            // operations.
            //
            // Note that _parent.Dispose doesn't throw so we don't need to special case.
            // SetHandleAsInvalid only sets _closed field to true (without
            // actually closing handle) so we don't need to call that as well.
            if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
            {
                handle.Dispose();
            }

            return errorCode;
        }

        internal static bool IsEndOfFile(int errorCode, SafeHandle handle, long fileOffset)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_HANDLE_EOF: // logically success with 0 bytes read (read at end of file)
                case Interop.Errors.ERROR_BROKEN_PIPE: // For pipes, ERROR_BROKEN_PIPE is the normal end of the pipe.
                case Interop.Errors.ERROR_PIPE_NOT_CONNECTED: // Named pipe server has disconnected, return 0 to match NamedPipeClientStream behaviour
                case Interop.Errors.ERROR_INVALID_PARAMETER when handle is SafeFileHandle sfh && IsEndOfFileForNoBuffering(sfh, fileOffset):
                    return true;
                default:
                    return false;
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
    }
}
