// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed partial class AsyncWindowsFileStreamStrategy : WindowsFileStreamStrategy
    {
        private ValueTaskSource? _reusableValueTaskSource; // reusable ValueTaskSource that is currently NOT being used

        internal AsyncWindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access, FileShare share)
            : base(handle, access, share)
        {
        }

        internal AsyncWindowsFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
            : base(path, mode, access, share, options)
        {
        }

        internal override bool IsAsync => true;

        public override ValueTask DisposeAsync()
        {
            // the base class must dispose ThreadPoolBinding and FileHandle
            // before _preallocatedOverlapped is disposed
            ValueTask result = base.DisposeAsync();
            Debug.Assert(result.IsCompleted, "the method must be sync, as it performs no flushing");

            Interlocked.Exchange(ref _reusableValueTaskSource, null)?.Dispose();

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            // the base class must dispose ThreadPoolBinding and FileHandle
            // before _preallocatedOverlapped is disposed
            base.Dispose(disposing);

            Interlocked.Exchange(ref _reusableValueTaskSource, null)?.Dispose();
        }

        protected override void OnInitFromHandle(SafeFileHandle handle)
        {
            // This is necessary for async IO using IO Completion ports via our
            // managed Threadpool API's.  This calls the OS's
            // BindIoCompletionCallback method, and passes in a stub for the
            // LPOVERLAPPED_COMPLETION_ROUTINE.  This stub looks at the Overlapped
            // struct for this request and gets a delegate to a managed callback
            // from there, which it then calls on a threadpool thread.  (We allocate
            // our native OVERLAPPED structs 2 pointers too large and store EE
            // state & a handle to a delegate there.)
            //
            // If, however, we've already bound this file handle to our completion port,
            // don't try to bind it again because it will fail.  A handle can only be
            // bound to a single completion port at a time.
            if (handle.IsAsync != true)
            {
                try
                {
                    handle.ThreadPoolBinding = ThreadPoolBoundHandle.BindHandle(handle);
                }
                catch (Exception ex)
                {
                    // If you passed in a synchronous handle and told us to use
                    // it asynchronously, throw here.
                    throw new ArgumentException(SR.Arg_HandleNotAsync, nameof(handle), ex);
                }
            }
        }

        protected override void OnInit()
        {
            // This is necessary for async IO using IO Completion ports via our
            // managed Threadpool API's.  This (theoretically) calls the OS's
            // BindIoCompletionCallback method, and passes in a stub for the
            // LPOVERLAPPED_COMPLETION_ROUTINE.  This stub looks at the Overlapped
            // struct for this request and gets a delegate to a managed callback
            // from there, which it then calls on a threadpool thread.  (We allocate
            // our native OVERLAPPED structs 2 pointers too large and store EE state
            // & GC handles there, one to an IAsyncResult, the other to a delegate.)
            try
            {
                _fileHandle.ThreadPoolBinding = ThreadPoolBoundHandle.BindHandle(_fileHandle);
            }
            catch (ArgumentException ex)
            {
                throw new IOException(SR.IO_BindHandleFailed, ex);
            }
            finally
            {
                if (_fileHandle.ThreadPoolBinding == null)
                {
                    // We should close the handle so that the handle is not open until SafeFileHandle GC
                    _fileHandle.Dispose();
                }
            }
        }

        private void TryToReuse(ValueTaskSource source)
        {
            source._source.Reset();

            if (Interlocked.CompareExchange(ref _reusableValueTaskSource, source, null) is not null)
            {
                source._preallocatedOverlapped.Dispose();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValueTask<int> vt = ReadAsyncInternal(new Memory<byte>(buffer, offset, count), CancellationToken.None);
            return vt.IsCompleted ?
                vt.Result :
                vt.AsTask().GetAwaiter().GetResult();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            => ReadAsyncInternal(destination, cancellationToken);

        private unsafe ValueTask<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            // Rent the reusable ValueTaskSource, or create a new one to use if we couldn't get one (which
            // should only happen on first use or if the FileStream is being used concurrently).
            ValueTaskSource vts = Interlocked.Exchange(ref _reusableValueTaskSource, null) ?? new ValueTaskSource(this);
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(destination);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                // Calculate position in the file we should be at after the read is done
                long positionBefore = _filePosition;
                if (CanSeek)
                {
                    long len = Length;

                    if (positionBefore + destination.Length > len)
                    {
                        destination = positionBefore <= len ?
                            destination.Slice(0, (int)(len - positionBefore)) :
                            default;
                    }

                    // Now set the position to read from in the NativeOverlapped struct
                    // For pipes, we should leave the offset fields set to 0.
                    nativeOverlapped->OffsetLow = unchecked((int)positionBefore);
                    nativeOverlapped->OffsetHigh = (int)(positionBefore >> 32);

                    // When using overlapped IO, the OS is not supposed to
                    // touch the file pointer location at all.  We will adjust it
                    // ourselves, but only in memory. This isn't threadsafe.
                    _filePosition += destination.Length;
                }

                // Queue an async ReadFile operation.
                if (Interop.Kernel32.ReadFile(_fileHandle, (byte*)vts._memoryHandle.Pointer, destination.Length, IntPtr.Zero, nativeOverlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    int errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(_fileHandle);
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
                            return ValueTask.FromException<int>(HandleIOError(positionBefore, errorCode));
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

        public override void Write(byte[] buffer, int offset, int count)
            => WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => WriteAsyncInternal(buffer, cancellationToken);

        private unsafe ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            // Rent the reusable ValueTaskSource, or create a new one to use if we couldn't get one (which
            // should only happen on first use or if the FileStream is being used concurrently).
            ValueTaskSource vts = Interlocked.Exchange(ref _reusableValueTaskSource, null) ?? new ValueTaskSource(this);
            try
            {
                NativeOverlapped* nativeOverlapped = vts.PrepareForOperation(source);
                Debug.Assert(vts._memoryHandle.Pointer != null);

                long positionBefore = _filePosition;
                if (CanSeek)
                {
                    // Now set the position to read from in the NativeOverlapped struct
                    // For pipes, we should leave the offset fields set to 0.
                    nativeOverlapped->OffsetLow = (int)positionBefore;
                    nativeOverlapped->OffsetHigh = (int)(positionBefore >> 32);

                    // When using overlapped IO, the OS is not supposed to
                    // touch the file pointer location at all.  We will adjust it
                    // ourselves, but only in memory.  This isn't threadsafe.
                    _filePosition += source.Length;
                    UpdateLengthOnChangePosition();
                }

                // Queue an async WriteFile operation.
                if (Interop.Kernel32.WriteFile(_fileHandle, (byte*)vts._memoryHandle.Pointer, source.Length, IntPtr.Zero, nativeOverlapped) == 0)
                {
                    // The operation failed, or it's pending.
                    int errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(_fileHandle);
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
                        return errorCode == Interop.Errors.ERROR_NO_DATA ? // EOF on a pipe. IO callback will not be called.
                            ValueTask.CompletedTask :
                            ValueTask.FromException(HandleIOError(positionBefore, errorCode));
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

        private Exception HandleIOError(long positionBefore, int errorCode)
        {
            if (!_fileHandle.IsClosed && CanSeek)
            {
                // Update Position... it could be anywhere.
                _filePosition = positionBefore;
            }

            return errorCode == Interop.Errors.ERROR_HANDLE_EOF ?
                ThrowHelper.CreateEndOfFileException() :
                Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask; // no buffering = nothing to flush

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);

            // Fail if the file was closed
            if (_fileHandle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            // Bail early for cancellation if cancellation has been requested
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            return AsyncModeCopyToAsync(destination, bufferSize, cancellationToken);
        }

        private async Task AsyncModeCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");
            Debug.Assert(CanRead, "_parent.CanRead");

            try
            {
                await FileStreamHelpers
                    .AsyncModeCopyToAsync(_fileHandle, _path, CanSeek, _filePosition, destination, bufferSize, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                // Make sure the stream's current position reflects where we ended up
                if (!_fileHandle.IsClosed && CanSeek)
                {
                    _filePosition = Length;
                }
            }
        }
    }
}
