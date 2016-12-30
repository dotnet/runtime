// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// System.Net.ChunkedInputStream
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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

using System.IO;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal class ChunkedInputStream : HttpRequestStream
    {
        private bool _disposed;
        private ChunkStream _decoder;
        private HttpListenerContext _context;
        private bool _no_more_data;

        private class ReadBufferState
        {
            public byte[] Buffer;
            public int Offset;
            public int Count;
            public int InitialCount;
            public HttpStreamAsyncResult Ares;
            public ReadBufferState(byte[] buffer, int offset, int count,
                        HttpStreamAsyncResult ares)
            {
                Buffer = buffer;
                Offset = offset;
                Count = count;
                InitialCount = count;
                Ares = ares;
            }
        }

        public ChunkedInputStream(HttpListenerContext context, Stream stream,
                        byte[] buffer, int offset, int length)
                    : base(stream, buffer, offset, length)
        {
            _context = context;
            WebHeaderCollection coll = (WebHeaderCollection)context.Request.Headers;
            _decoder = new ChunkStream(coll);
        }

        public ChunkStream Decoder
        {
            get { return _decoder; }
            set { _decoder = value; }
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            IAsyncResult ares = BeginRead(buffer, offset, count, null, null);
            return EndRead(ares);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback cback, object state)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            int len = buffer.Length;
            if (offset < 0 || offset > len)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.offset_out_of_range);

            if (count < 0 || offset > len - count)
                throw new ArgumentOutOfRangeException(nameof(count), SR.offset_out_of_range);

            HttpStreamAsyncResult ares = new HttpStreamAsyncResult();
            ares._callback = cback;
            ares._state = state;
            if (_no_more_data)
            {
                ares.Complete();
                return ares;
            }
            int nread = _decoder.Read(buffer, offset, count);
            offset += nread;
            count -= nread;
            if (count == 0)
            {
                // got all we wanted, no need to bother the decoder yet
                ares._count = nread;
                ares.Complete();
                return ares;
            }
            if (!_decoder.WantMore)
            {
                _no_more_data = nread == 0;
                ares._count = nread;
                ares.Complete();
                return ares;
            }
            ares._buffer = new byte[8192];
            ares._offset = 0;
            ares._count = 8192;
            ReadBufferState rb = new ReadBufferState(buffer, offset, count, ares);
            rb.InitialCount += nread;
            base.BeginRead(ares._buffer, ares._offset, ares._count, OnRead, rb);
            return ares;
        }

        private void OnRead(IAsyncResult base_ares)
        {
            ReadBufferState rb = (ReadBufferState)base_ares.AsyncState;
            HttpStreamAsyncResult ares = rb.Ares;
            try
            {
                int nread = base.EndRead(base_ares);
                _decoder.Write(ares._buffer, ares._offset, nread);
                nread = _decoder.Read(rb.Buffer, rb.Offset, rb.Count);
                rb.Offset += nread;
                rb.Count -= nread;
                if (rb.Count == 0 || !_decoder.WantMore || nread == 0)
                {
                    _no_more_data = !_decoder.WantMore && nread == 0;
                    ares._count = rb.InitialCount - rb.Count;
                    ares.Complete();
                    return;
                }
                ares._offset = 0;
                ares._count = Math.Min(8192, _decoder.ChunkLeft + 6);
                base.BeginRead(ares._buffer, ares._offset, ares._count, OnRead, rb);
            }
            catch (Exception e)
            {
                _context.Connection.SendError(e.Message, 400);
                ares.Complete(e);
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            HttpStreamAsyncResult ares = asyncResult as HttpStreamAsyncResult;
            if (asyncResult == null)
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));

            if (!asyncResult.IsCompleted)
                asyncResult.AsyncWaitHandle.WaitOne();

            if (ares._error != null)
                throw new HttpListenerException((int)HttpStatusCode.BadRequest, SR.Format(SR.net_io_operation_aborted, ares._error.Message));

            return ares._count;
        }

        public override void Close()
        {
            if (!_disposed)
            {
                _disposed = true;
                base.Close();
            }
        }
    }
}

