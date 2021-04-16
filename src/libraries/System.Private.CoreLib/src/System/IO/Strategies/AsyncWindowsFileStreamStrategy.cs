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

            Interlocked.Exchange(ref _reusableValueTaskSource, null)?._preallocatedOverlapped.Dispose();

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            // the base class must dispose ThreadPoolBinding and FileHandle
            // before _preallocatedOverlapped is disposed
            base.Dispose(disposing);

            Interlocked.Exchange(ref _reusableValueTaskSource, null)?._preallocatedOverlapped.Dispose();
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

            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");

            // valueTaskSource is not null when:
            // - First time calling ReadAsync in buffered mode
            // - Second+ time calling ReadAsync, both buffered or unbuffered
            // - On buffered flush, when source memory is also the internal buffer
            // valueTaskSource is null when:
            // - First time calling ReadAsync in unbuffered mode
            ValueTaskSource valueTaskSource = Interlocked.Exchange(ref _reusableValueTaskSource, null) ?? new ValueTaskSource(this);
            NativeOverlapped* intOverlapped = valueTaskSource.Configure(destination);

            // Calculate position in the file we should be at after the read is done
            long positionBefore = _filePosition;
            if (CanSeek)
            {
                long len = Length;

                if (positionBefore + destination.Length > len)
                {
                    if (positionBefore <= len)
                    {
                        destination = destination.Slice(0, (int)(len - positionBefore));
                    }
                    else
                    {
                        destination = default;
                    }
                }

                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = unchecked((int)positionBefore);
                intOverlapped->OffsetHigh = (int)(positionBefore >> 32);

                // When using overlapped IO, the OS is not supposed to
                // touch the file pointer location at all.  We will adjust it
                // ourselves, but only in memory. This isn't threadsafe.
                _filePosition += destination.Length;
            }

            // queue an async ReadFile operation and pass in a packed overlapped
            int r = FileStreamHelpers.ReadFileNative(_fileHandle, destination.Span, false, intOverlapped, out int errorCode);

            // ReadFile, the OS version, will return 0 on failure.  But
            // my ReadFileNative wrapper returns -1.  My wrapper will return
            // the following:
            // On error, r==-1.
            // On async requests that are still pending, r==-1 w/ errorCode==ERROR_IO_PENDING
            // on async requests that completed sequentially, r==0
            // You will NEVER RELIABLY be able to get the number of bytes
            // read back from this call when using overlapped structures!  You must
            // not pass in a non-null lpNumBytesRead to ReadFile when using
            // overlapped structures!  This is by design NT behavior.
            if (r == -1)
            {
                // For pipes, when they hit EOF, they will come here.
                if (errorCode == Interop.Errors.ERROR_BROKEN_PIPE)
                {
                    // Not an error, but EOF.  AsyncFSCallback will NOT be
                    // called.  Call the user callback here.

                    // We clear the overlapped status bit for this special case.
                    // Failure to do so looks like we are freeing a pending overlapped later.
                    intOverlapped->InternalLow = IntPtr.Zero;
                    valueTaskSource.ReleaseNativeResource();
                    TryToReuse(valueTaskSource);
                    return new ValueTask<int>(0);
                }
                else if (errorCode != Interop.Errors.ERROR_IO_PENDING)
                {
                    if (!_fileHandle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
                    {
                        _filePosition = positionBefore;
                    }

                    valueTaskSource.ReleaseNativeResource();
                    TryToReuse(valueTaskSource);

                    if (errorCode == Interop.Errors.ERROR_HANDLE_EOF)
                    {
                        ThrowHelper.ThrowEndOfFileException();
                    }
                    else
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                    }
                }
                else if (cancellationToken.CanBeCanceled) // ERROR_IO_PENDING
                {
                    // Only once the IO is pending do we register for cancellation
                    valueTaskSource.RegisterForCancellation(cancellationToken);
                }
            }
            else
            {
                // Due to a workaround for a race condition in NT's ReadFile &
                // WriteFile routines, we will always be returning 0 from ReadFileNative
                // when we do async IO instead of the number of bytes read,
                // irregardless of whether the operation completed
                // synchronously or asynchronously.  We absolutely must not
                // set asyncResult._numBytes here, since will never have correct
                // results.
            }

            return new ValueTask<int>(valueTaskSource, valueTaskSource.Version);
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

            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");

            // valueTaskSource is not null when:
            // - First time calling WriteAsync in buffered mode
            // - Second+ time calling WriteAsync, both buffered or unbuffered
            // - On buffered flush, when source memory is also the internal buffer
            // valueTaskSource is null when:
            // - First time calling WriteAsync in unbuffered mode
            ValueTaskSource valueTaskSource = Interlocked.Exchange(ref _reusableValueTaskSource, null) ?? new ValueTaskSource(this);
            NativeOverlapped* intOverlapped = valueTaskSource.Configure(source);

            long positionBefore = _filePosition;
            if (CanSeek)
            {
                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = (int)positionBefore;
                intOverlapped->OffsetHigh = (int)(positionBefore >> 32);

                // When using overlapped IO, the OS is not supposed to
                // touch the file pointer location at all.  We will adjust it
                // ourselves, but only in memory.  This isn't threadsafe.
                _filePosition += source.Length;
                UpdateLengthOnChangePosition();
            }

            // queue an async WriteFile operation and pass in a packed overlapped
            int r = FileStreamHelpers.WriteFileNative(_fileHandle, source.Span, false, intOverlapped, out int errorCode);

            // WriteFile, the OS version, will return 0 on failure.  But
            // my WriteFileNative wrapper returns -1.  My wrapper will return
            // the following:
            // On error, r==-1.
            // On async requests that are still pending, r==-1 w/ errorCode==ERROR_IO_PENDING
            // On async requests that completed sequentially, r==0
            // You will NEVER RELIABLY be able to get the number of bytes
            // written back from this call when using overlapped IO!  You must
            // not pass in a non-null lpNumBytesWritten to WriteFile when using
            // overlapped structures!  This is ByDesign NT behavior.
            if (r == -1)
            {
                // For pipes, when they are closed on the other side, they will come here.
                if (errorCode == Interop.Errors.ERROR_NO_DATA)
                {
                    // Not an error, but EOF. AsyncFSCallback will NOT be called.
                    // Completing TCS and return cached task allowing the GC to collect TCS.
                    valueTaskSource.ReleaseNativeResource();
                    TryToReuse(valueTaskSource);
                    return ValueTask.CompletedTask;
                }
                else if (errorCode != Interop.Errors.ERROR_IO_PENDING)
                {
                    if (!_fileHandle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
                    {
                        _filePosition = positionBefore;
                    }

                    valueTaskSource.ReleaseNativeResource();
                    TryToReuse(valueTaskSource);

                    if (errorCode == Interop.Errors.ERROR_HANDLE_EOF)
                    {
                        ThrowHelper.ThrowEndOfFileException();
                    }
                    else
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, _path);
                    }
                }
                else if (cancellationToken.CanBeCanceled) // ERROR_IO_PENDING
                {
                    // Only once the IO is pending do we register for cancellation
                    valueTaskSource.RegisterForCancellation(cancellationToken);
                }
            }
            else
            {
                // Due to a workaround for a race condition in NT's ReadFile &
                // WriteFile routines, we will always be returning 0 from WriteFileNative
                // when we do async IO instead of the number of bytes written,
                // irregardless of whether the operation completed
                // synchronously or asynchronously.  We absolutely must not
                // set asyncResult._numBytes here, since will never have correct
                // results.
            }

            return new ValueTask(valueTaskSource, valueTaskSource.Version);
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
