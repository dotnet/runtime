// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal class WinHttpResponseStream : Stream
    {
        private volatile bool _disposed = false;
        private SafeWinHttpHandle _sessionHandle = null;
        private SafeWinHttpHandle _connectHandle = null;
        private SafeWinHttpHandle _requestHandle = null;

        internal WinHttpResponseStream(
            SafeWinHttpHandle sessionHandle,
            SafeWinHttpHandle connectHandle,
            SafeWinHttpHandle requestHandle)
        {
            // While we only use the requestHandle to do actual reads of the response body,
            // we need to keep the parent handles (connection, session) alive as well.
            bool ignore = false;
            sessionHandle.DangerousAddRef(ref ignore);
            connectHandle.DangerousAddRef(ref ignore);
            requestHandle.DangerousAddRef(ref ignore);
            _sessionHandle = sessionHandle;
            _connectHandle = connectHandle;
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (count > buffer.Length - offset)
            {
                throw new ArgumentException("buffer");
            }

            CheckDisposed();

            uint bytesRead = 0;
            GCHandle pinnedHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            bool result = Interop.WinHttp.WinHttpReadData(
                _requestHandle,
                Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset),
                (uint)count,
                out bytesRead);
            pinnedHandle.Free();
            if (!result)
            {
                throw new IOException(SR.net_http_io_read, WinHttpException.CreateExceptionUsingLastError());
            }

            return (int)bytesRead;
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
            if (disposing && !_disposed)
            {
                _disposed = true;

                _requestHandle.DangerousRelease();
                _connectHandle.DangerousRelease();
                _sessionHandle.DangerousRelease();

                SafeWinHttpHandle.DisposeAndClearHandle(ref _requestHandle);
                SafeWinHttpHandle.DisposeAndClearHandle(ref _connectHandle);
                SafeWinHttpHandle.DisposeAndClearHandle(ref _sessionHandle);
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
