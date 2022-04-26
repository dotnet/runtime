// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

#pragma warning disable CA1844 // lack of ReadAsync(Memory) override in .NET Standard 2.1 build

namespace System.Net.Http
{
    internal sealed class WinHttpResponseStream : Stream
    {
        private volatile bool _disposed;
        private readonly WinHttpRequestState _state;
        private readonly HttpResponseMessage _responseMessage;
        private SafeWinHttpHandle _requestHandle;
        private bool _readTrailingHeaders;

        internal WinHttpResponseStream(SafeWinHttpHandle requestHandle, WinHttpRequestState state, HttpResponseMessage responseMessage)
        {
            _state = state;
            _responseMessage = responseMessage;
            _requestHandle = requestHandle;
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

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // Validate arguments as would base CopyToAsync
            StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);

            // Check that there are no other pending read operations
            if (_state.AsyncReadInProgress)
            {
                throw new InvalidOperationException(SR.net_http_no_concurrent_io_allowed);
            }

            // Early check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            // Check out a buffer and start the copy
            return CopyToAsyncCore(destination, ArrayPool<byte>.Shared.Rent(bufferSize), cancellationToken);
        }

        private async Task CopyToAsyncCore(Stream destination, byte[] buffer, CancellationToken cancellationToken)
        {
            _state.PinReceiveBuffer(buffer);
            CancellationTokenRegistration ctr = cancellationToken.Register(s => ((WinHttpResponseStream)s!).CancelPendingResponseStreamReadOperation(), this);
            _state.AsyncReadInProgress = true;
            try
            {
                // Loop until there's no more data to be read
                while (true)
                {
                    // Query for data available
                    lock (_state.Lock)
                    {
                        if (!Interop.WinHttp.WinHttpQueryDataAvailable(_requestHandle, IntPtr.Zero))
                        {
                            throw new IOException(SR.net_http_io_read, WinHttpException.CreateExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpQueryDataAvailable)));
                        }
                    }
                    int bytesAvailable = await _state.LifecycleAwaitable;
                    if (bytesAvailable == 0)
                    {
                        ReadResponseTrailers();
                        break;
                    }
                    Debug.Assert(bytesAvailable > 0);

                    // Read the available data
                    cancellationToken.ThrowIfCancellationRequested();
                    lock (_state.Lock)
                    {
                        if (!Interop.WinHttp.WinHttpReadData(_requestHandle, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), (uint)Math.Min(bytesAvailable, buffer.Length), IntPtr.Zero))
                        {
                            throw new IOException(SR.net_http_io_read, WinHttpException.CreateExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpReadData)));
                        }
                    }
                    int bytesRead = await _state.LifecycleAwaitable;
                    if (bytesRead == 0)
                    {
                        ReadResponseTrailers();
                        break;
                    }
                    Debug.Assert(bytesRead > 0);

                    // Write that data out to the output stream
#if NETSTANDARD2_1 || NETCOREAPP
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
#else
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
#endif
                }
            }
            finally
            {
                _state.AsyncReadInProgress = false;
                ctr.Dispose();
                ArrayPool<byte>.Shared.Return(buffer);
            }

            // Leaving buffer pinned as it is in ReadAsync.  It'll get unpinned when another read
            // request is made with a different buffer or when the state is cleared.
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (buffer is null)
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

            if (_state.AsyncReadInProgress)
            {
                throw new InvalidOperationException(SR.net_http_no_concurrent_io_allowed);
            }

            return ReadAsyncCore(buffer, offset, count, token);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        private async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (count == 0)
            {
                return 0;
            }

            _state.PinReceiveBuffer(buffer);
            var ctr = token.Register(s => ((WinHttpResponseStream)s!).CancelPendingResponseStreamReadOperation(), this);
            _state.AsyncReadInProgress = true;
            try
            {
                lock (_state.Lock)
                {
                    Debug.Assert(!_requestHandle.IsInvalid);
                    if (!Interop.WinHttp.WinHttpQueryDataAvailable(_requestHandle, IntPtr.Zero))
                    {
                        throw new IOException(SR.net_http_io_read, WinHttpException.CreateExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpQueryDataAvailable)));
                    }
                }

                int bytesAvailable = await _state.LifecycleAwaitable;

                lock (_state.Lock)
                {
                    Debug.Assert(!_requestHandle.IsInvalid);
                    if (!Interop.WinHttp.WinHttpReadData(
                        _requestHandle,
                        Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset),
                        (uint)Math.Min(bytesAvailable, count),
                        IntPtr.Zero))
                    {
                        throw new IOException(SR.net_http_io_read, WinHttpException.CreateExceptionUsingLastError(nameof(Interop.WinHttp.WinHttpReadData)));
                    }
                }

                int bytesRead = await _state.LifecycleAwaitable;

                if (bytesRead == 0)
                {
                    ReadResponseTrailers();
                }

                return bytesRead;
            }
            finally
            {
                _state.AsyncReadInProgress = false;
                ctr.Dispose();
            }
        }

        private void ReadResponseTrailers()
        {
            // Only load response trailers if:
            // 1. WINHTTP_QUERY_FLAG_TRAILERS is supported by the OS
            // 2. HTTP/2 or later (WINHTTP_QUERY_FLAG_TRAILERS does not work with HTTP/1.1)
            // 3. Response trailers not already loaded
            if (!WinHttpTrailersHelper.OsSupportsTrailers || _responseMessage.Version < WinHttpHandler.HttpVersion20 || _readTrailingHeaders)
            {
                return;
            }

            _readTrailingHeaders = true;

            var bufferLength = WinHttpResponseParser.GetResponseHeaderCharBufferLength(_requestHandle, isTrailingHeaders: true);

            if (bufferLength != 0)
            {
                char[] trailersBuffer = ArrayPool<char>.Shared.Rent(bufferLength);
                try
                {
                    WinHttpResponseParser.ParseResponseTrailers(_requestHandle, _responseMessage, trailersBuffer);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(trailersBuffer);
                }
            }
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

                if (disposing)
                {
                    if (_requestHandle != null)
                    {
                        _requestHandle.Dispose();
                        _requestHandle = null!;
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

        // The only way to abort pending async operations in WinHTTP is to close the request handle.
        // This causes WinHTTP to cancel any pending I/O and accelerating its callbacks on the handle.
        // This causes our related TaskCompletionSource objects to move to a terminal state.
        //
        // We only want to dispose the handle if we are actually waiting for a pending WinHTTP I/O to complete,
        // meaning that we are await'ing for a Task to complete. While we could simply call dispose without
        // a pending operation, it would cause random failures in the other threads when we expect a valid handle.
        private void CancelPendingResponseStreamReadOperation()
        {
            lock (_state.Lock)
            {
                if (_state.AsyncReadInProgress)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info("before dispose");
                    _requestHandle?.Dispose(); // null check necessary to handle race condition between stream disposal and cancellation
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info("after dispose");
                }
            }
        }
    }
}
