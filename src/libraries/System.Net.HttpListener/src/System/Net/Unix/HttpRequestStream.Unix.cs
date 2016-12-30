// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// System.Net.RequestStream
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal class HttpRequestStream : Stream
    {
        private byte[] _buffer;
        private int _offset;
        private int _length;
        private long _remainingBody;
        private bool _disposed;
        private Stream _stream;

        internal HttpRequestStream(Stream stream, byte[] buffer, int offset, int length)
            : this(stream, buffer, offset, length, -1)
        {
        }

        internal HttpRequestStream(Stream stream, byte[] buffer, int offset, int length, long contentlength)
        {
            _stream = stream;
            _buffer = buffer;
            _offset = offset;
            _length = length;
            _remainingBody = contentlength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length
        {
            get { throw new NotSupportedException(SR.net_noseek); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(SR.net_noseek); }
            set { throw new NotSupportedException(SR.net_noseek); }
        }

        public override void Close() => _disposed = true;

        public override void Flush()
        {
        }


        // Returns 0 if we can keep reading from the base stream,
        // > 0 if we read something from the buffer.
        // -1 if we had a content length set and we finished reading that many bytes.
        private int FillFromBuffer(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            int len = buffer.Length;
            if (offset > len)
                throw new ArgumentException(nameof(offset), SR.offset_out_of_range);
            if (offset > len - count)
                throw new ArgumentException(nameof(count), SR.offset_out_of_range);

            if (_remainingBody == 0)
                return -1;

            if (_length == 0)
                return 0;

            int size = Math.Min(_length, count);
            if (_remainingBody > 0)
                size = (int)Math.Min(size, _remainingBody);

            if (_offset > _buffer.Length - size)
            {
                size = Math.Min(size, _buffer.Length - _offset);
            }
            if (size == 0)
                return 0;

            Buffer.BlockCopy(_buffer, _offset, buffer, offset, size);
            _offset += size;
            _length -= size;
            if (_remainingBody > 0)
                _remainingBody -= size;
            return size;
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(HttpRequestStream).ToString());

            // Call FillFromBuffer to check for buffer boundaries even when remaining_body is 0
            int nread = FillFromBuffer(buffer, offset, count);
            if (nread == -1)
            { // No more bytes available (Content-Length)
                return 0;
            }
            else if (nread > 0)
            {
                return nread;
            }

            nread = _stream.Read(buffer, offset, count);
            if (nread > 0 && _remainingBody > 0)
                _remainingBody -= nread;
            return nread;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback cback, object state)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(HttpRequestStream).ToString());

            int nread = FillFromBuffer(buffer, offset, count);
            if (nread > 0 || nread == -1)
            {
                HttpStreamAsyncResult ares = new HttpStreamAsyncResult();
                ares._buffer = buffer;
                ares._offset = offset;
                ares._count = count;
                ares._callback = cback;
                ares._state = state;
                ares._synchRead = Math.Max(0, nread);
                ares.Complete();
                return ares;
            }

            // Avoid reading past the end of the request to allow
            // for HTTP pipelining
            if (_remainingBody >= 0 && count > _remainingBody)
            {
                count = (int)Math.Min(int.MaxValue, _remainingBody);
            }

            return _stream.BeginRead(buffer, offset, count, cback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(HttpRequestStream).ToString());

            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (asyncResult is HttpStreamAsyncResult)
            {
                HttpStreamAsyncResult r = (HttpStreamAsyncResult)asyncResult;
                if (!asyncResult.IsCompleted)
                    asyncResult.AsyncWaitHandle.WaitOne();
                return r._synchRead;
            }

            int nread = _stream.EndRead(asyncResult);
            if (_remainingBody > 0 && nread > 0)
            {
                _remainingBody -= nread;
            }

            return nread;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(SR.net_readonlystream);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count,
                            AsyncCallback cback, object state)
        {
            throw new NotSupportedException(SR.net_readonlystream);
        }

        public override void EndWrite(IAsyncResult async_result)
        {
            throw new NotSupportedException(SR.net_readonlystream);
        }
    }
}

