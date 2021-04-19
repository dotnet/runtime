// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Net.ResponseStream
//
// Author:
//  Gonzalo Paniagua Javier (gonzalo@novell.com)
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
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal sealed partial class HttpResponseStream : Stream
    {
        private HttpListenerResponse _response;
        private bool _ignore_errors;
        private bool _trailer_sent;
        private Stream _stream;

        internal HttpResponseStream(Stream stream, HttpListenerResponse response, bool ignore_errors)
        {
            _response = response;
            _ignore_errors = ignore_errors;
            _stream = stream;
        }

        private void DisposeCore()
        {
            byte[]? bytes = null;
            MemoryStream? ms = GetHeaders(true);
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
                catch (HttpListenerException)
                {
                    // Ignore error due to connection reset by peer
                }
            }
            _response.Close();
        }

        internal async Task WriteWebSocketHandshakeHeadersAsync()
        {
            if (_closed)
                throw new ObjectDisposedException(GetType().ToString());

            if (_stream.CanWrite)
            {
                MemoryStream ms = GetHeaders(closing: false, isWebSocketHandshake: true)!;
                bool chunked = _response.SendChunked;

                long start = ms.Position;
                if (chunked)
                {
                    byte[] bytes = GetChunkSizeBytes(0, true);
                    ms.Position = ms.Length;
                    ms.Write(bytes, 0, bytes.Length);
                }

                await InternalWriteAsync(ms.GetBuffer(), (int)start, (int)(ms.Length - start)).ConfigureAwait(false);
                await _stream.FlushAsync().ConfigureAwait(false);
            }
        }

        private MemoryStream? GetHeaders(bool closing, bool isWebSocketHandshake = false)
        {
            // SendHeaders works on shared headers
            lock (_response._headersLock)
            {
                if (_response.SentHeaders)
                {
                    return null;
                }

                MemoryStream ms = new MemoryStream();
                _response.SendHeaders(closing, ms, isWebSocketHandshake);
                return ms;
            }
        }

        private static byte[] s_crlf = new byte[] { 13, 10 };
        private static byte[] GetChunkSizeBytes(int size, bool final) =>
            Encoding.ASCII.GetBytes($"{size:x}\r\n{(final ? "\r\n" : "")}");

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
                try
                {
                    _stream.Write(buffer, offset, count);
                }
                catch (IOException ex)
                {
                    throw new HttpListenerException(ex.HResult, ex.Message);
                }
            }
        }

        internal Task InternalWriteAsync(byte[] buffer, int offset, int count) =>
            _ignore_errors ? InternalWriteIgnoreErrorsAsync(buffer, offset, count) : _stream.WriteAsync(buffer, offset, count);

        private async Task InternalWriteIgnoreErrorsAsync(byte[] buffer, int offset, int count)
        {
            try { await _stream.WriteAsync(buffer.AsMemory(offset, count)).ConfigureAwait(false); }
            catch { }
        }

        private void WriteCore(byte[] buffer, int offset, int size)
        {
            if (size == 0)
                return;

            byte[]? bytes = null;
            MemoryStream? ms = GetHeaders(false);
            bool chunked = _response.SendChunked;
            if (ms != null)
            {
                long start = ms.Position; // After the possible preamble for the encoding
                ms.Position = ms.Length;
                if (chunked)
                {
                    bytes = GetChunkSizeBytes(size, false);
                    ms.Write(bytes, 0, bytes.Length);
                }

                int new_count = Math.Min(size, 16384 - (int)ms.Position + (int)start);
                ms.Write(buffer, offset, new_count);
                size -= new_count;
                offset += new_count;
                InternalWrite(ms.GetBuffer(), (int)start, (int)(ms.Length - start));
                ms.SetLength(0);
                ms.Capacity = 0; // 'dispose' the buffer in ms.
            }
            else if (chunked)
            {
                bytes = GetChunkSizeBytes(size, false);
                InternalWrite(bytes, 0, bytes.Length);
            }

            if (size > 0)
                InternalWrite(buffer, offset, size);
            if (chunked)
                InternalWrite(s_crlf, 0, 2);
        }

        private IAsyncResult BeginWriteCore(byte[] buffer, int offset, int size, AsyncCallback? cback, object? state)
        {
            if (_closed)
            {
                HttpStreamAsyncResult ares = new HttpStreamAsyncResult(this);
                ares._callback = cback;
                ares._state = state;
                ares.Complete();
                return ares;
            }

            byte[]? bytes = null;
            MemoryStream? ms = GetHeaders(false);
            bool chunked = _response.SendChunked;
            if (ms != null)
            {
                long start = ms.Position;
                ms.Position = ms.Length;
                if (chunked)
                {
                    bytes = GetChunkSizeBytes(size, false);
                    ms.Write(bytes, 0, bytes.Length);
                }
                ms.Write(buffer, offset, size);
                buffer = ms.GetBuffer();
                offset = (int)start;
                size = (int)(ms.Position - start);
            }
            else if (chunked)
            {
                bytes = GetChunkSizeBytes(size, false);
                InternalWrite(bytes, 0, bytes.Length);
            }

            try
            {
                return _stream.BeginWrite(buffer, offset, size, cback, state);
            }
            catch (IOException ex)
            {
                if (_ignore_errors)
                {
                    HttpStreamAsyncResult ares = new HttpStreamAsyncResult(this);
                    ares._callback = cback;
                    ares._state = state;
                    ares.Complete();
                    return ares;
                }
                else
                {
                    throw new HttpListenerException(ex.HResult, ex.Message);
                }
            }
        }

        private void EndWriteCore(IAsyncResult asyncResult)
        {
            if (_closed)
                return;

            if (_ignore_errors)
            {
                try
                {
                    _stream.EndWrite(asyncResult);
                    if (_response.SendChunked)
                        _stream.Write(s_crlf, 0, 2);
                }
                catch { }
            }
            else
            {
                try
                {
                    _stream.EndWrite(asyncResult);
                    if (_response.SendChunked)
                        _stream.Write(s_crlf, 0, 2);
                }
                catch (IOException ex)
                {
                    // NetworkStream wraps exceptions in IOExceptions; if the underlying socket operation
                    // failed because of invalid arguments or usage, propagate that error.  Otherwise
                    // wrap the whole thing in an HttpListenerException.  This is all to match Windows behavior.
                    if (ex.InnerException is ArgumentException || ex.InnerException is InvalidOperationException)
                    {
                        ExceptionDispatchInfo.Throw(ex.InnerException);
                    }

                    throw new HttpListenerException(ex.HResult, ex.Message);
                }
            }
        }
    }
}
