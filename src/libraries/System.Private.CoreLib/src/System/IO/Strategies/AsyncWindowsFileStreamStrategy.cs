// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed partial class AsyncWindowsFileStreamStrategy : WindowsFileStreamStrategy
    {
        private PreAllocatedOverlapped? _preallocatedOverlapped;     // optimization for async ops to avoid per-op allocations
        private FileStreamAwaitableProvider? _currentOverlappedOwner; // async op currently using the preallocated overlapped

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
                    _fileHandle.Dispose();
                }
            }
        }

        // called by BufferedFileStreamStrategy
        internal override void OnBufferAllocated(byte[] buffer)
        {
            Debug.Assert(_preallocatedOverlapped == null);

            _preallocatedOverlapped = new PreAllocatedOverlapped(FileStreamAwaitableProvider.s_ioCallback, this, buffer);
        }

        internal FileStreamAwaitableProvider? CompareExchangeCurrentOverlappedOwner(FileStreamAwaitableProvider? newSource, FileStreamAwaitableProvider? existingSource)
            => Interlocked.CompareExchange(ref _currentOverlappedOwner, newSource, existingSource);

        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsyncInternal(new Memory<byte>(buffer, offset, count)).AsTask().GetAwaiter().GetResult();

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

            // Create and store async stream class library specific data in the async result
            FileStreamAwaitableProvider provider = FileStreamAwaitableProvider.Create(this, _preallocatedOverlapped, 0, destination);
            return provider.ReadAsync(destination, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
            => WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => WriteAsyncInternal(buffer, cancellationToken);

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask; // no buffering = nothing to flush

        private unsafe ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            Debug.Assert(!_fileHandle.IsClosed, "!_handle.IsClosed");

            // Create and store async stream class library specific data in the async result
            FileStreamAwaitableProvider provider = FileStreamAwaitableProvider.Create(this, _preallocatedOverlapped, 0, source);
            return provider.WriteAsync(source, cancellationToken);
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
