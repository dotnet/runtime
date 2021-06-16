// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // this type defines a set of stateless FileStream/FileStreamStrategy helper methods
    internal static partial class FileStreamHelpers
    {
        // Async completion/return codes shared by:
        // - AsyncWindowsFileStreamStrategy.ValueTaskSource
        // - Net5CompatFileStreamStrategy.CompletionSource
        internal static class TaskSourceCodes
        {
            internal const long NoResult = 0;
            internal const long ResultSuccess = (long)1 << 32;
            internal const long ResultError = (long)2 << 32;
            internal const long RegisteringCancellation = (long)4 << 32;
            internal const long CompletedCallback = (long)8 << 32;
            internal const ulong ResultMask = ((ulong)uint.MaxValue) << 32;
        }

        private static FileStreamStrategy ChooseStrategyCore(SafeFileHandle handle, FileAccess access, FileShare share, int bufferSize, bool isAsync)
        {
            if (UseNet5CompatStrategy)
            {
                // The .NET 5 Compat strategy does not support bufferSize == 0.
                // To minimize the risk of introducing bugs to it, we just pass 1 to disable the buffering.
                return new Net5CompatFileStreamStrategy(handle, access, bufferSize == 0 ? 1 : bufferSize, isAsync);
            }

            WindowsFileStreamStrategy strategy = isAsync
                ? new AsyncWindowsFileStreamStrategy(handle, access, share)
                : new SyncWindowsFileStreamStrategy(handle, access, share);

            return EnableBufferingIfNeeded(strategy, bufferSize);
        }

        private static FileStreamStrategy ChooseStrategyCore(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
        {
            if (UseNet5CompatStrategy)
            {
                return new Net5CompatFileStreamStrategy(path, mode, access, share, bufferSize == 0 ? 1 : bufferSize, options, preallocationSize);
            }

            WindowsFileStreamStrategy strategy = (options & FileOptions.Asynchronous) != 0
                ? new AsyncWindowsFileStreamStrategy(path, mode, access, share, options, preallocationSize)
                : new SyncWindowsFileStreamStrategy(path, mode, access, share, options, preallocationSize);

            return EnableBufferingIfNeeded(strategy, bufferSize);
        }

        internal static FileStreamStrategy EnableBufferingIfNeeded(WindowsFileStreamStrategy strategy, int bufferSize)
            => bufferSize > 1 ? new BufferedFileStreamStrategy(strategy, bufferSize) : strategy;

        internal static void FlushToDisk(SafeFileHandle handle, string? path)
        {
            if (!Interop.Kernel32.FlushFileBuffers(handle))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(path);
            }
        }

        internal static long Seek(SafeFileHandle handle, string? path, long offset, SeekOrigin origin, bool closeInvalidHandle = false)
        {
            Debug.Assert(origin >= SeekOrigin.Begin && origin <= SeekOrigin.End, "origin >= SeekOrigin.Begin && origin <= SeekOrigin.End");

            if (!Interop.Kernel32.SetFilePointerEx(handle, offset, out long ret, (uint)origin))
            {
                if (closeInvalidHandle)
                {
                    throw Win32Marshal.GetExceptionForWin32Error(GetLastWin32ErrorAndDisposeHandleIfInvalid(handle), path);
                }
                else
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error(path);
                }
            }

            return ret;
        }

        internal static int GetLastWin32ErrorAndDisposeHandleIfInvalid(SafeFileHandle handle)
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

        internal static void Lock(SafeFileHandle handle, string? path, long position, long length)
        {
            int positionLow = unchecked((int)(position));
            int positionHigh = unchecked((int)(position >> 32));
            int lengthLow = unchecked((int)(length));
            int lengthHigh = unchecked((int)(length >> 32));

            if (!Interop.Kernel32.LockFile(handle, positionLow, positionHigh, lengthLow, lengthHigh))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(path);
            }
        }

        internal static void Unlock(SafeFileHandle handle, string? path, long position, long length)
        {
            int positionLow = unchecked((int)(position));
            int positionHigh = unchecked((int)(position >> 32));
            int lengthLow = unchecked((int)(length));
            int lengthHigh = unchecked((int)(length >> 32));

            if (!Interop.Kernel32.UnlockFile(handle, positionLow, positionHigh, lengthLow, lengthHigh))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(path);
            }
        }

        internal static void ValidateFileTypeForNonExtendedPaths(SafeFileHandle handle, string originalPath)
        {
            if (!PathInternal.IsExtended(originalPath))
            {
                // To help avoid stumbling into opening COM/LPT ports by accident, we will block on non file handles unless
                // we were explicitly passed a path that has \\?\. GetFullPath() will turn paths like C:\foo\con.txt into
                // \\.\CON, so we'll only allow the \\?\ syntax.

                int fileType = handle.GetFileType();
                if (fileType != Interop.Kernel32.FileTypes.FILE_TYPE_DISK)
                {
                    int errorCode = fileType == Interop.Kernel32.FileTypes.FILE_TYPE_UNKNOWN
                        ? Marshal.GetLastPInvokeError()
                        : Interop.Errors.ERROR_SUCCESS;

                    handle.Dispose();

                    if (errorCode != Interop.Errors.ERROR_SUCCESS)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                    }
                    throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
                }
            }
        }

        internal static unsafe void SetFileLength(SafeFileHandle handle, string? path, long length)
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
                if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                    throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_FileLengthTooBig);
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            }
        }



        internal static async Task AsyncModeCopyToAsync(SafeFileHandle handle, string? path, bool canSeek, long filePosition, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // For efficiency, we avoid creating a new task and associated state for each asynchronous read.
            // Instead, we create a single reusable awaitable object that will be triggered when an await completes
            // and reset before going again.
            var readAwaitable = new AsyncCopyToAwaitable(handle);

            // Make sure we are reading from the position that we think we are.
            // Only set the position in the awaitable if we can seek (e.g. not for pipes).
            if (canSeek)
            {
                readAwaitable._position = filePosition;
            }

            // Get the buffer to use for the copy operation, as the base CopyToAsync does. We don't try to use
            // _buffer here, even if it's not null, as concurrent operations are allowed, and another operation may
            // actually be using the buffer already. Plus, it'll be rare for _buffer to be non-null, as typically
            // CopyToAsync is used as the only operation performed on the stream, and the buffer is lazily initialized.
            // Further, typically the CopyToAsync buffer size will be larger than that used by the FileStream, such that
            // we'd likely be unable to use it anyway.  Instead, we rent the buffer from a pool.
            byte[] copyBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            // Allocate an Overlapped we can use repeatedly for all operations
            var awaitableOverlapped = new PreAllocatedOverlapped(AsyncCopyToAwaitable.s_callback, readAwaitable, copyBuffer);
            var cancellationReg = default(CancellationTokenRegistration);
            try
            {
                // Register for cancellation.  We do this once for the whole copy operation, and just try to cancel
                // whatever read operation may currently be in progress, if there is one.  It's possible the cancellation
                // request could come in between operations, in which case we flag that with explicit calls to ThrowIfCancellationRequested
                // in the read/write copy loop.
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationReg = cancellationToken.UnsafeRegister(static s =>
                    {
                        Debug.Assert(s is AsyncCopyToAwaitable);
                        var innerAwaitable = (AsyncCopyToAwaitable)s;
                        unsafe
                        {
                            lock (innerAwaitable.CancellationLock) // synchronize with cleanup of the overlapped
                            {
                                if (innerAwaitable._nativeOverlapped != null)
                                {
                                    // Try to cancel the I/O.  We ignore the return value, as cancellation is opportunistic and we
                                    // don't want to fail the operation because we couldn't cancel it.
                                    Interop.Kernel32.CancelIoEx(innerAwaitable._fileHandle, innerAwaitable._nativeOverlapped);
                                }
                            }
                        }
                    }, readAwaitable);
                }

                // Repeatedly read from this FileStream and write the results to the destination stream.
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    readAwaitable.ResetForNextOperation();

                    try
                    {
                        bool synchronousSuccess;
                        int errorCode;
                        unsafe
                        {
                            // Allocate a native overlapped for our reusable overlapped, and set position to read based on the next
                            // desired address stored in the awaitable.  (This position may be 0, if either we're at the beginning or
                            // if the stream isn't seekable.)
                            readAwaitable._nativeOverlapped = handle.ThreadPoolBinding!.AllocateNativeOverlapped(awaitableOverlapped);
                            if (canSeek)
                            {
                                readAwaitable._nativeOverlapped->OffsetLow = unchecked((int)readAwaitable._position);
                                readAwaitable._nativeOverlapped->OffsetHigh = (int)(readAwaitable._position >> 32);
                            }

                            // Kick off the read.
                            synchronousSuccess = RandomAccess.ReadFileNative(handle, copyBuffer, false, readAwaitable._nativeOverlapped, out errorCode) >= 0;
                        }

                        // If the operation did not synchronously succeed, it either failed or initiated the asynchronous operation.
                        if (!synchronousSuccess)
                        {
                            switch (errorCode)
                            {
                                case Interop.Errors.ERROR_IO_PENDING:
                                    // Async operation in progress.
                                    break;
                                case Interop.Errors.ERROR_BROKEN_PIPE:
                                case Interop.Errors.ERROR_HANDLE_EOF:
                                    // We're at or past the end of the file, and the overlapped callback
                                    // won't be raised in these cases. Mark it as completed so that the await
                                    // below will see it as such.
                                    readAwaitable.MarkCompleted();
                                    break;
                                default:
                                    // Everything else is an error (and there won't be a callback).
                                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
                            }
                        }

                        // Wait for the async operation (which may or may not have already completed), then throw if it failed.
                        await readAwaitable;
                        switch (readAwaitable._errorCode)
                        {
                            case 0: // success
                                break;
                            case Interop.Errors.ERROR_BROKEN_PIPE: // logically success with 0 bytes read (write end of pipe closed)
                            case Interop.Errors.ERROR_HANDLE_EOF:  // logically success with 0 bytes read (read at end of file)
                                Debug.Assert(readAwaitable._numBytes == 0, $"Expected 0 bytes read, got {readAwaitable._numBytes}");
                                break;
                            case Interop.Errors.ERROR_OPERATION_ABORTED: // canceled
                                throw new OperationCanceledException(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
                            default: // error
                                throw Win32Marshal.GetExceptionForWin32Error((int)readAwaitable._errorCode, path);
                        }

                        // Successful operation.  If we got zero bytes, we're done: exit the read/write loop.
                        int numBytesRead = (int)readAwaitable._numBytes;
                        if (numBytesRead == 0)
                        {
                            break;
                        }

                        // Otherwise, update the read position for next time accordingly.
                        if (canSeek)
                        {
                            readAwaitable._position += numBytesRead;
                        }
                    }
                    finally
                    {
                        // Free the resources for this read operation
                        unsafe
                        {
                            NativeOverlapped* overlapped;
                            lock (readAwaitable.CancellationLock) // just an Exchange, but we need this to be synchronized with cancellation, so using the same lock
                            {
                                overlapped = readAwaitable._nativeOverlapped;
                                readAwaitable._nativeOverlapped = null;
                            }
                            if (overlapped != null)
                            {
                                handle.ThreadPoolBinding!.FreeNativeOverlapped(overlapped);
                            }
                        }
                    }

                    // Write out the read data.
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(copyBuffer, 0, (int)readAwaitable._numBytes), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                // Cleanup from the whole copy operation
                cancellationReg.Dispose();
                awaitableOverlapped.Dispose();

                ArrayPool<byte>.Shared.Return(copyBuffer);
            }
        }

        /// <summary>Used by AsyncWindowsFileStreamStrategy and Net5CompatFileStreamStrategy CopyToAsync to enable awaiting the result of an overlapped I/O operation with minimal overhead.</summary>
        private sealed unsafe class AsyncCopyToAwaitable : ICriticalNotifyCompletion
        {
            /// <summary>Sentinel object used to indicate that the I/O operation has completed before being awaited.</summary>
            private static readonly Action s_sentinel = () => { };
            /// <summary>Cached delegate to IOCallback.</summary>
            internal static readonly IOCompletionCallback s_callback = IOCallback;

            internal readonly SafeFileHandle _fileHandle;

            /// <summary>Tracked position representing the next location from which to read.</summary>
            internal long _position;
            /// <summary>The current native overlapped pointer.  This changes for each operation.</summary>
            internal NativeOverlapped* _nativeOverlapped;
            /// <summary>
            /// null if the operation is still in progress,
            /// s_sentinel if the I/O operation completed before the await,
            /// s_callback if it completed after the await yielded.
            /// </summary>
            internal Action? _continuation;
            /// <summary>Last error code from completed operation.</summary>
            internal uint _errorCode;
            /// <summary>Last number of read bytes from completed operation.</summary>
            internal uint _numBytes;

            /// <summary>Lock object used to protect cancellation-related access to _nativeOverlapped.</summary>
            internal object CancellationLock => this;

            /// <summary>Initialize the awaitable.</summary>
            internal AsyncCopyToAwaitable(SafeFileHandle fileHandle) => _fileHandle = fileHandle;

            /// <summary>Reset state to prepare for the next read operation.</summary>
            internal void ResetForNextOperation()
            {
                Debug.Assert(_position >= 0, $"Expected non-negative position, got {_position}");
                _continuation = null;
                _errorCode = 0;
                _numBytes = 0;
            }

            /// <summary>Overlapped callback: store the results, then invoke the continuation delegate.</summary>
            internal static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOVERLAP)
            {
                var awaitable = (AsyncCopyToAwaitable?)ThreadPoolBoundHandle.GetNativeOverlappedState(pOVERLAP);
                Debug.Assert(awaitable != null);

                Debug.Assert(!ReferenceEquals(awaitable._continuation, s_sentinel), "Sentinel must not have already been set as the continuation");
                awaitable._errorCode = errorCode;
                awaitable._numBytes = numBytes;

                (awaitable._continuation ?? Interlocked.CompareExchange(ref awaitable._continuation, s_sentinel, null))?.Invoke();
            }

            /// <summary>
            /// Called when it's known that the I/O callback for an operation will not be invoked but we'll
            /// still be awaiting the awaitable.
            /// </summary>
            internal void MarkCompleted()
            {
                Debug.Assert(_continuation == null, "Expected null continuation");
                _continuation = s_sentinel;
            }

            public AsyncCopyToAwaitable GetAwaiter() => this;
            public bool IsCompleted => ReferenceEquals(_continuation, s_sentinel);
            public void GetResult() { }
            public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation)
            {
                if (ReferenceEquals(_continuation, s_sentinel) ||
                    Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
                {
                    Debug.Assert(ReferenceEquals(_continuation, s_sentinel), $"Expected continuation set to s_sentinel, got ${_continuation}");
                    Task.Run(continuation);
                }
            }
        }
    }
}
