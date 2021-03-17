// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed partial class AsyncWindowsFileStreamStrategy : WindowsFileStreamStrategy, IFileStreamCompletionSourceStrategy
    {
        private PreAllocatedOverlapped? _preallocatedOverlapped;     // optimization for async ops to avoid per-op allocations
        private FileStreamCompletionSource? _currentOverlappedOwner; // async op currently using the preallocated overlapped

        internal AsyncWindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access)
            : base(handle, access)
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

            _preallocatedOverlapped?.Dispose();

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            // the base class must dispose ThreadPoolBinding and FileHandle
            // before _preallocatedOverlapped is disposed
            base.Dispose(disposing);

            _preallocatedOverlapped?.Dispose();
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
                    Debug.Assert(!_exposedHandle, "Are we closing handle that we exposed/not own, how?");
                    _fileHandle.Dispose();
                }
            }
        }

        // called by BufferedFileStreamStrategy
        internal override void OnBufferAllocated(byte[] buffer)
        {
            Debug.Assert(_preallocatedOverlapped == null);

            _preallocatedOverlapped = new PreAllocatedOverlapped(FileStreamCompletionSource.s_ioCallback, this, buffer);
        }

        SafeFileHandle IFileStreamCompletionSourceStrategy.FileHandle => _fileHandle;

        FileStreamCompletionSource? IFileStreamCompletionSourceStrategy.CurrentOverlappedOwner => _currentOverlappedOwner;

        FileStreamCompletionSource? IFileStreamCompletionSourceStrategy.CompareExchangeCurrentOverlappedOwner(FileStreamCompletionSource? newSource, FileStreamCompletionSource? existingSource)
            => Interlocked.CompareExchange(ref _currentOverlappedOwner, newSource, existingSource);

        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsyncInternal(new Memory<byte>(buffer, offset, count)).GetAwaiter().GetResult();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            => new ValueTask<int>(ReadAsyncInternal(destination, cancellationToken));

        private unsafe Task<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");

            // Create and store async stream class library specific data in the async result
            FileStreamCompletionSource completionSource = FileStreamCompletionSource.Create(this, _preallocatedOverlapped, 0, destination);
            NativeOverlapped* intOverlapped = completionSource.Overlapped;

            // Calculate position in the file we should be at after the read is done
            if (CanSeek)
            {
                long len = Length;

                // Make sure we are reading from the position that we think we are
                VerifyOSHandlePosition();

                if (destination.Length > len - _filePosition)
                {
                    if (_filePosition <= len)
                    {
                        destination = destination.Slice(0, (int)(len - _filePosition));
                    }
                    else
                    {
                        destination = default;
                    }
                }

                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = unchecked((int)_filePosition);
                intOverlapped->OffsetHigh = (int)(_filePosition >> 32);

                // When using overlapped IO, the OS is not supposed to
                // touch the file pointer location at all.  We will adjust it
                // ourselves. This isn't threadsafe.

                // WriteFile should not update the file pointer when writing
                // in overlapped mode, according to MSDN.  But it does update
                // the file pointer when writing to a UNC path!
                // So changed the code below to seek to an absolute
                // location, not a relative one.  ReadFile seems consistent though.
                SeekCore(_fileHandle, destination.Length, SeekOrigin.Current);
            }

            // queue an async ReadFile operation and pass in a packed overlapped
            int r = FileStreamHelpers.ReadFileNative(_fileHandle, destination.Span, intOverlapped, out int errorCode);

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
                if (errorCode == ERROR_BROKEN_PIPE)
                {
                    // Not an error, but EOF.  AsyncFSCallback will NOT be
                    // called.  Call the user callback here.

                    // We clear the overlapped status bit for this special case.
                    // Failure to do so looks like we are freeing a pending overlapped later.
                    intOverlapped->InternalLow = IntPtr.Zero;
                    completionSource.SetCompletedSynchronously(0);
                }
                else if (errorCode != ERROR_IO_PENDING)
                {
                    if (!_fileHandle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
                    {
                        SeekCore(_fileHandle, 0, SeekOrigin.Current);
                    }

                    completionSource.ReleaseNativeResource();

                    if (errorCode == ERROR_HANDLE_EOF)
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
                    completionSource.RegisterForCancellation(cancellationToken);
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

            return completionSource.Task;
        }

        public override void Write(byte[] buffer, int offset, int count)
            => WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => WriteAsyncInternal(buffer, cancellationToken);

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask; // no buffering = nothing to flush

        private ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
            => new ValueTask(WriteAsyncInternalCore(source, cancellationToken));

        private unsafe Task WriteAsyncInternalCore(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");

            // Create and store async stream class library specific data in the async result
            FileStreamCompletionSource completionSource = FileStreamCompletionSource.Create(this, _preallocatedOverlapped, 0, source);
            NativeOverlapped* intOverlapped = completionSource.Overlapped;

            if (CanSeek)
            {
                // Make sure we set the length of the file appropriately.
                long len = Length;

                // Make sure we are writing to the position that we think we are
                VerifyOSHandlePosition();

                if (_filePosition + source.Length > len)
                {
                    SetLengthCore(_filePosition + source.Length);
                }

                // Now set the position to read from in the NativeOverlapped struct
                // For pipes, we should leave the offset fields set to 0.
                intOverlapped->OffsetLow = (int)_filePosition;
                intOverlapped->OffsetHigh = (int)(_filePosition >> 32);

                // When using overlapped IO, the OS is not supposed to
                // touch the file pointer location at all.  We will adjust it
                // ourselves.  This isn't threadsafe.
                SeekCore(_fileHandle, source.Length, SeekOrigin.Current);
            }

            // queue an async WriteFile operation and pass in a packed overlapped
            int r = FileStreamHelpers.WriteFileNative(_fileHandle, source.Span, intOverlapped, out int errorCode);

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
                if (errorCode == ERROR_NO_DATA)
                {
                    // Not an error, but EOF. AsyncFSCallback will NOT be called.
                    // Completing TCS and return cached task allowing the GC to collect TCS.
                    completionSource.SetCompletedSynchronously(0);
                    return Task.CompletedTask;
                }
                else if (errorCode != ERROR_IO_PENDING)
                {
                    if (!_fileHandle.IsClosed && CanSeek)  // Update Position - It could be anywhere.
                    {
                        SeekCore(_fileHandle, 0, SeekOrigin.Current);
                    }

                    completionSource.ReleaseNativeResource();

                    if (errorCode == ERROR_HANDLE_EOF)
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
                    completionSource.RegisterForCancellation(cancellationToken);
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

            return completionSource.Task;
        }

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

            bool canSeek = CanSeek;
            if (canSeek)
            {
                VerifyOSHandlePosition();
            }

            try
            {
                await FileStreamHelpers
                    .AsyncModeCopyToAsync(_fileHandle, _path, canSeek, _filePosition, destination, bufferSize, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                // Make sure the stream's current position reflects where we ended up
                if (!_fileHandle.IsClosed && canSeek)
                {
                    SeekCore(_fileHandle, 0, SeekOrigin.End);
                }
            }
        }
    }
}
