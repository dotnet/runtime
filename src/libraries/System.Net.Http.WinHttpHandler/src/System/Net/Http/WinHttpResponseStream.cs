// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal class WinHttpResponseStream : Stream
    {
        private volatile bool _disposed;
        private readonly WinHttpRequestState _state;
        
        // TODO (Issue 2505): temporary pinned buffer caches of 1 item. Will be replaced by PinnableBufferCache.
        private GCHandle _cachedReceivePinnedBuffer = default(GCHandle);

        internal WinHttpResponseStream(WinHttpRequestState state)
        {
            _state = state;
        }

        public override bool CanRead
        {
            get
            {
                return !_disposed;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                CheckDisposed();
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                CheckDisposed();
                throw new NotSupportedException();
            }

            set
            {
                CheckDisposed();
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            // Nothing to do.
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled(cancellationToken) :
                Task.CompletedTask;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count > buffer.Length - offset)
            {
                throw new ArgumentException(SR.net_http_buffer_insufficient_length, nameof(buffer));
            }

            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(token);
            }

            CheckDisposed();

            if (_state.TcsReadFromResponseStream != null && !_state.TcsReadFromResponseStream.Task.IsCompleted)
            {
                throw new InvalidOperationException(SR.net_http_no_concurrent_io_allowed);
            }

            // TODO (Issue 2505): replace with PinnableBufferCache.
            if (!_cachedReceivePinnedBuffer.IsAllocated || _cachedReceivePinnedBuffer.Target != buffer)
            {
                if (_cachedReceivePinnedBuffer.IsAllocated)
                {
                    _cachedReceivePinnedBuffer.Free();
                }

                _cachedReceivePinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            }

            _state.TcsReadFromResponseStream =
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            _state.TcsQueryDataAvailable =
                new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _state.TcsQueryDataAvailable.Task.ContinueWith((previousTask) => 
                {
                    if (previousTask.IsFaulted)
                    {
                        _state.TcsReadFromResponseStream.TrySetException(previousTask.Exception.InnerException);
                    }
                    else if (previousTask.IsCanceled || token.IsCancellationRequested)
                    {
                        _state.TcsReadFromResponseStream.TrySetCanceled(token);
                    }
                    else
                    {
                        int bytesToRead;
                        int bytesAvailable = previousTask.Result;
                        if (bytesAvailable > count)
                        {
                            bytesToRead = count;
                        }
                        else
                        {
                            bytesToRead = bytesAvailable;
                        }
                        
                        lock (_state.Lock)
                        {
                            if (!Interop.WinHttp.WinHttpReadData(
                                _state.RequestHandle,
                                Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset),
                                (uint)bytesToRead,
                                IntPtr.Zero))
                            {
                                _state.TcsReadFromResponseStream.TrySetException(
                                    new IOException(SR.net_http_io_read, WinHttpException.CreateExceptionUsingLastError()));
                            }
                        }
                    }
                }, 
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

            // TODO: Issue #2165. Register callback on cancellation token to cancel WinHTTP operation.
                
            lock (_state.Lock)
            {
                if (!Interop.WinHttp.WinHttpQueryDataAvailable(_state.RequestHandle, IntPtr.Zero))
                {
                    _state.TcsReadFromResponseStream.TrySetException(
                        new IOException(SR.net_http_io_read, WinHttpException.CreateExceptionUsingLastError()));
                }
            }

            return _state.TcsReadFromResponseStream.Task;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                // TODO (Issue 2508): Pinned buffers must be released in the callback, when it is guaranteed no further
                // operations will be made to the send/receive buffers.
                if (_cachedReceivePinnedBuffer.IsAllocated)
                {
                    _cachedReceivePinnedBuffer.Free();
                    _cachedReceivePinnedBuffer = default(GCHandle);
                }

                if (disposing)
                {
                    if (_state.RequestHandle != null)
                    {
                        _state.RequestHandle.Dispose();
                        _state.RequestHandle = null;
                    }
                }
            }

            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
