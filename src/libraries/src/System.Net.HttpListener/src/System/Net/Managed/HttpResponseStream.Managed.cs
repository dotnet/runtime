// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Net.ResponseStream
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
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Net
{
    internal class HttpResponseStream : Stream
    {
        private HttpListenerResponse _response;
        private bool _ignore_errors;
        private bool _disposed;
        private bool _trailer_sent;
        private Stream _stream;

        internal HttpResponseStream(Stream stream, HttpListenerResponse response, bool ignore_errors)
        {
            _response = response;
            _ignore_errors = ignore_errors;
            _stream = stream;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get { throw new NotSupportedException(SR.net_noseek); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(SR.net_noseek); }
            set { throw new NotSupportedException(SR.net_noseek); }
        }

        public override void Close()
        {
            if (_disposed == false)
            {
                _disposed = true;
                byte[] bytes = null;
                MemoryStream ms = GetHeaders(true);
                bool chunked = _response.SendChunked;
                if (_stream.CanWrite)
                {
                    try
                    {
                        if (ms != null)
                        {
                            long start = ms.Position;
                            if (chunked && !_trailer_sent)
                            {
                                bytes = GetChunkSizeBytes(0, true);
                                ms.Position = ms.Length;
                                ms.Write(bytes, 0, bytes.Length);
                            }
                            InternalWrite(ms.GetBuffer(), (int)start, (int)(ms.Length - start));
                            _trailer_sent = true;
                        }
                        else if (chunked && !_trailer_sent)
                        {
                            bytes = GetChunkSizeBytes(0, true);
                            InternalWrite(bytes, 0, bytes.Length);
                            _trailer_sent = true;
                        }
                    }
                    catch (IOException)
                    {
                        // Ignore error due to connection reset by peer
                    }
                }
                _response.Close();
            }
        }

        internal void WriteHeaders()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            byte[] bytes = null;
            MemoryStream ms = GetHeaders(false, true);
            bool chunked = _response.SendChunked;
            if (_stream.CanWrite)
            {
                long start = ms.Position;
                if (chunked)
                {
                    bytes = GetChunkSizeBytes(0, true);
                    ms.Position = ms.Length;
                    ms.Write(bytes, 0, bytes.Length);
                }

                InternalWrite(ms.GetBuffer(), (int)start, (int)(ms.Length - start));
                _stream.Flush();
            }
        }

        private MemoryStream GetHeaders(bool closing, bool isWebSocketHandshake = false)
        {
            // SendHeaders works on shared headers
            lock (_response._headersLock)
            {
                if (_response._headersSent)
                {
                    return null;
                }

                MemoryStream ms = new MemoryStream();
                _response.SendHeaders(closing, ms, isWebSocketHandshake);
                return ms;
            }
        }

        public override void Flush()
        {
        }

        private static byte[] s_crlf = new byte[] { 13, 10 };
        private static byte[] GetChunkSizeBytes(int size, bool final)
        {
            string str = String.Format("{0:x}\r\n{1}", size, final ? "\r\n" : "");
            return Encoding.ASCII.GetBytes(str);
        }

        internal void InternalWrite(byte[] buffer, int offset, int count)
        {
            if (_ignore_errors)
            {
                try
                {
                    _stream.Write(buffer, offset, count);
                }
                catch { }
            }
            else
            {
                _stream.Write(buffer, offset, count);
            }
        }

        internal async Task InternalWriteAsync(byte[] buffer, int offset, int count)
        {
            if (_ignore_errors)
            {
                try
                {
                    await _stream.WriteAsync(buffer, offset, count);
                }
                catch { }
            }
            else
            {
                await _stream.WriteAsync(buffer, offset, count);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());
            if (count == 0)
                return;

            byte[] bytes = null;
            MemoryStream ms = GetHeaders(false);
            bool chunked = _response.SendChunked;
            if (ms != null)
            {
                long start = ms.Position; // After the possible preamble for the encoding
                ms.Position = ms.Length;
                if (chunked)
                {
                    bytes = GetChunkSizeBytes(count, false);
                    ms.Write(bytes, 0, bytes.Length);
                }

                int new_count = Math.Min(count, 16384 - (int)ms.Position + (int)start);
                ms.Write(buffer, offset, new_count);
                count -= new_count;
                offset += new_count;
                InternalWrite(ms.GetBuffer(), (int)start, (int)(ms.Length - start));
                ms.SetLength(0);
                ms.Capacity = 0; // 'dispose' the buffer in ms.
            }
            else if (chunked)
            {
                bytes = GetChunkSizeBytes(count, false);
                InternalWrite(bytes, 0, bytes.Length);
            }

            if (count > 0)
                InternalWrite(buffer, offset, count);
            if (chunked)
                InternalWrite(s_crlf, 0, 2);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback cback, object state)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            byte[] bytes = null;
            MemoryStream ms = GetHeaders(false);
            bool chunked = _response.SendChunked;
            if (ms != null)
            {
                long start = ms.Position;
                ms.Position = ms.Length;
                if (chunked)
                {
                    bytes = GetChunkSizeBytes(count, false);
                    ms.Write(bytes, 0, bytes.Length);
                }
                ms.Write(buffer, offset, count);
                buffer = ms.GetBuffer();
                offset = (int)start;
                count = (int)(ms.Position - start);
            }
            else if (chunked)
            {
                bytes = GetChunkSizeBytes(count, false);
                InternalWrite(bytes, 0, bytes.Length);
            }

            return _stream.BeginWrite(buffer, offset, count, cback, state);
        }

        public override void EndWrite(IAsyncResult ares)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (_ignore_errors)
            {
                try
                {
                    _stream.EndWrite(ares);
                    if (_response.SendChunked)
                        _stream.Write(s_crlf, 0, 2);
                }
                catch { }
            }
            else
            {
                _stream.EndWrite(ares);
                if (_response.SendChunked)
                    _stream.Write(s_crlf, 0, 2);
            }
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(SR.net_writeonlystream);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count,
                            AsyncCallback cback, object state)
        {
            throw new NotSupportedException(SR.net_writeonlystream);
        }

        public override int EndRead(IAsyncResult ares)
        {
            throw new NotSupportedException(SR.net_writeonlystream);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.net_noseek);
        }
    }
}

