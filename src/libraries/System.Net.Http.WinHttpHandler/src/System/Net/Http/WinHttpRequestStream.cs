// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal class WinHttpRequestStream : Stream
    {
        private static byte[] s_crLfTerminator = new byte[] { 0x0d, 0x0a }; // "\r\n"
        private static byte[] s_endChunk = new byte[] { 0x30, 0x0d, 0x0a, 0x0d, 0x0a }; // "0\r\n\r\n"

        private volatile bool _disposed = false;
        private SafeWinHttpHandle _requestHandle = null;
        private bool _chunkedMode = false;

        internal WinHttpRequestStream(SafeWinHttpHandle requestHandle, bool chunkedMode)
        {
            _requestHandle = requestHandle;
            _chunkedMode = chunkedMode;
        }

        public override bool CanRead
        {
            get
            {
                return false;
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
                return !_disposed;
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
        }

        public override void Write(byte[] buffer, int offset, int count)
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

            WriteInternal(buffer, offset, count);
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        internal void EndUpload()
        {
            if (_chunkedMode)
            {
                WriteData(s_endChunk, 0, s_endChunk.Length);
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
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

        private void WriteInternal(byte[] buffer, int offset, int count)
        {
            if (_chunkedMode)
            {
                string chunkSizeString = String.Format("{0:x}\r\n", count);
                byte[] chunkSize = Encoding.UTF8.GetBytes(chunkSizeString);

                WriteData(chunkSize, 0, chunkSize.Length);

                WriteData(buffer, offset, count);
                WriteData(s_crLfTerminator, 0, s_crLfTerminator.Length);
            }
            else
            {
                WriteData(buffer, offset, count);
            }
        }

        private void WriteData(byte[] buffer, int offset, int count)
        {
            uint bytesWritten = 0;
            GCHandle pinnedHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            bool result = Interop.WinHttp.WinHttpWriteData(
                _requestHandle,
                Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset),
                (uint)count,
                out bytesWritten);
            pinnedHandle.Free();
            if (!result)
            {
                throw new IOException(SR.net_http_io_read, WinHttpException.CreateExceptionUsingLastError());
            }
        }
    }
}
