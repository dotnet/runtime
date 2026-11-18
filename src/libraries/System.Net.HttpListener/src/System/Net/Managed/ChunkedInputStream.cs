// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Net.ChunkedInputStream
//
// Authors:
//  Gonzalo Paniagua Javier (gonzalo@novell.com)
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
    internal sealed class ChunkedInputStream : HttpRequestStream
    {
        private ChunkStream _decoder;
        private readonly HttpListenerContext _context;
        private bool _no_more_data;

        private sealed class ReadBufferState
        {
            public byte[] Buffer;
            public int Offset;
            public int Count;
            public int InitialCount;
            public HttpStreamAsyncResult Ares;
            public ReadBufferState(byte[] buffer, int offset, int count, HttpStreamAsyncResult ares)
            {
                Buffer = buffer;
                Offset = offset;
                Count = count;
                InitialCount = count;
                Ares = ares;
            }
        }

        public ChunkedInputStream(HttpListenerContext context, Stream stream, byte[] buffer, int offset, int length)
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

        protected override int ReadCore(byte[] buffer, int offset, int count)
        {
            IAsyncResult ares = BeginReadCore(buffer, offset, count, null, null);
            return EndRead(ares);
        }

        protected override IAsyncResult BeginReadCore(byte[] buffer, int offset, int size, AsyncCallback? cback, object? state)
        {
            HttpStreamAsyncResult ares = new HttpStreamAsyncResult(this);
            ares._callback = cback;
            ares._state = state;
            if (_no_more_data || size == 0 || _closed)
            {
                ares.Complete();
                return ares;
            }
            int nread = _decoder.Read(buffer, offset, size);
            offset += nread;
            size -= nread;
            if (size == 0)
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
            ReadBufferState rb = new ReadBufferState(buffer, offset, size, ares);
            rb.InitialCount += nread;
            ReadNextChunkFragment(rb);
            return ares;
        }

        // Issues the inner base-stream read in a loop. base.BeginReadCore's real-network-read path
        // (NetworkStream/SslStream) reliably sets IAsyncResult.CompletedSynchronously to true
        // *before* invoking the callback when the read completes inline; its buffered/short-circuit
        // paths instead return a HttpStreamAsyncResult whose Complete() always dispatches the
        // callback via Task.Run (i.e. always on a fresh stack) and whose CompletedSynchronously is
        // always false. Either way, it's safe to let a "CompletedSynchronously == false" result be
        // handled by OnRead: that only happens when there's genuinely no risk of unbounded stack
        // growth. "CompletedSynchronously == true" results are processed right here, in a loop,
        // instead of recursing into OnRead -> BeginReadCore -> OnRead -> ..., which could otherwise
        // grow the stack without bound for a chunked body made of many immediately-available chunks.
        private void ReadNextChunkFragment(ReadBufferState rb)
        {
            while (true)
            {
                IAsyncResult baseAres;
                try
                {
                    baseAres = base.BeginReadCore(rb.Ares._buffer!, rb.Ares._offset, rb.Ares._count, OnRead, rb);
                }
                catch (Exception e)
                {
                    _context.Connection.SendError(e.Message, 400);
                    rb.Ares.Complete(e);
                    return;
                }

                if (!baseAres.CompletedSynchronously)
                {
                    // OnRead will process this once the real read completes.
                    return;
                }

                if (!ProcessChunkFragment(baseAres, rb))
                {
                    return;
                }
            }
        }

        private void OnRead(IAsyncResult base_ares)
        {
            if (base_ares.CompletedSynchronously)
            {
                // The ReadNextChunkFragment loop that issued this read is still on the stack and
                // will process the result itself; returning here (instead of processing again)
                // avoids double-processing and avoids recursing back into BeginReadCore from this
                // callback.
                return;
            }

            ReadBufferState rb = (ReadBufferState)base_ares.AsyncState!;
            if (ProcessChunkFragment(base_ares, rb))
            {
                ReadNextChunkFragment(rb);
            }
        }

        // Returns true if another inner read should be issued (either by looping, if called from
        // there, or via the fresh call above); false once the outer HttpStreamAsyncResult has been
        // completed (successfully or with an error) and no further reads are needed.
        private bool ProcessChunkFragment(IAsyncResult base_ares, ReadBufferState rb)
        {
            HttpStreamAsyncResult ares = rb.Ares;
            try
            {
                int nread = base.EndRead(base_ares);
                if (nread == 0)
                {
                    _no_more_data = true;
                    ares._count = rb.InitialCount - rb.Count;
                    ares.Complete();
                    return false;
                }

                _decoder.Write(ares._buffer!, ares._offset, nread);
                nread = _decoder.Read(rb.Buffer, rb.Offset, rb.Count);
                rb.Offset += nread;
                rb.Count -= nread;
                if (rb.Count == 0 || !_decoder.WantMore)
                {
                    _no_more_data = !_decoder.WantMore && nread == 0;
                    ares._count = rb.InitialCount - rb.Count;
                    ares.Complete();
                    return false;
                }
                ares._offset = 0;
                ares._count = Math.Min(8192, _decoder.ChunkLeft + 6);
                return true;
            }
            catch (Exception e)
            {
                _context.Connection.SendError(e.Message, 400);
                ares.Complete(e);
                return false;
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            ArgumentNullException.ThrowIfNull(asyncResult);

            HttpStreamAsyncResult? ares = asyncResult as HttpStreamAsyncResult;
            if (ares == null || !ReferenceEquals(this, ares._parent))
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }
            if (ares._endCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, nameof(EndRead)));
            }
            ares._endCalled = true;

            if (!asyncResult.IsCompleted)
                asyncResult.AsyncWaitHandle.WaitOne();

            if (ares._error != null)
                throw new HttpListenerException((int)HttpStatusCode.BadRequest, SR.Format(SR.net_io_operation_aborted, ares._error.Message));

            return ares._count;
        }
    }
}
