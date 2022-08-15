// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Net.HttpConnection
//
// Author:
//  Gonzalo Paniagua Javier (gonzalo.mono@gmail.com)
//
// Copyright (c) 2005-2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2012 Xamarin, Inc. (http://xamarin.com)
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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace System.Net
{
    internal sealed class HttpConnection
    {
        private static AsyncCallback s_onreadCallback = new AsyncCallback(OnRead);
        private const int BufferSize = 8192;
        private Socket? _socket;
        private Stream _stream;
        private HttpEndPointListener _epl;
        private MemoryStream? _memoryStream;
        private byte[]? _buffer;
        private HttpListenerContext _context;
        private StringBuilder? _currentLine;
        private ListenerPrefix? _prefix;
        private HttpRequestStream? _requestStream;
        private HttpResponseStream? _responseStream;
        private bool _chunked;
        private int _reuses;
        private bool _contextBound;
        private bool _secure;
        private X509Certificate _cert;
        private int _timeout = 90000; // 90k ms for first request, 15k ms from then on
        private Timer _timer;
        private IPEndPoint? _localEndPoint;
        private HttpListener? _lastListener;
        private int[]? _clientCertErrors;
        private X509Certificate2? _clientCert;
        private SslStream? _sslStream;
        private InputState _inputState = InputState.RequestLine;
        private LineState _lineState = LineState.None;
        private int _position;

        public HttpConnection(Socket sock, HttpEndPointListener epl, bool secure, X509Certificate cert)
        {
            _socket = sock;
            _epl = epl;
            _secure = secure;
            _cert = cert;
            if (secure == false)
            {
                _stream = new NetworkStream(sock, false);
            }
            else
            {
#pragma warning disable CA5359
                _sslStream = HttpListener.CreateSslStream(new NetworkStream(sock, false), false, (t, c, ch, e) =>
                {
                    if (c == null)
                    {
                        return true;
                    }

                    _clientCert = c as X509Certificate2 ?? new X509Certificate2(c.GetRawCertData());
                    _clientCertErrors = new int[] { (int)e };
                    return true;
                });
#pragma warning restore CA5359

                _stream = _sslStream;
            }

            _timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
            _sslStream?.AuthenticateAsServer(_cert, true, (SslProtocols)ServicePointManager.SecurityProtocol, false);
            Init();
        }

        internal int[]? ClientCertificateErrors
        {
            get { return _clientCertErrors; }
        }

        internal X509Certificate2? ClientCertificate
        {
            get { return _clientCert; }
        }

        internal SslStream? SslStream
        {
            get { return _sslStream; }
        }

        [MemberNotNull(nameof(_memoryStream))]
        [MemberNotNull(nameof(_context))]
        private void Init()
        {
            _contextBound = false;
            _requestStream = null;
            _responseStream = null;
            _prefix = null;
            _chunked = false;
            _memoryStream = new MemoryStream();
            _position = 0;
            _inputState = InputState.RequestLine;
            _lineState = LineState.None;
            _context = new HttpListenerContext(this);
        }

        public Stream ConnectedStream => _stream;

        public bool IsClosed
        {
            get { return (_socket == null); }
        }

        public int Reuses
        {
            get { return _reuses; }
        }

        public IPEndPoint? LocalEndPoint
        {
            get
            {
                if (_localEndPoint != null)
                    return _localEndPoint;

                _localEndPoint = (IPEndPoint?)_socket!.LocalEndPoint;
                return _localEndPoint;
            }
        }

        public IPEndPoint? RemoteEndPoint
        {
            get { return (IPEndPoint?)_socket!.RemoteEndPoint; }
        }

        public bool IsSecure
        {
            get { return _secure; }
        }

        public ListenerPrefix? Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        private void OnTimeout(object? unused)
        {
            CloseSocket();
            Unbind();
        }

        public void BeginReadRequest()
        {
            _buffer ??= new byte[BufferSize];
            try
            {
                if (_reuses == 1)
                    _timeout = 15000;
                _timer.Change(_timeout, Timeout.Infinite);
                _stream.BeginRead(_buffer, 0, BufferSize, s_onreadCallback, this);
            }
            catch
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                CloseSocket();
                Unbind();
            }
        }

        public HttpRequestStream GetRequestStream(bool chunked, long contentlength)
        {
            if (_requestStream == null)
            {
                byte[] buffer = _memoryStream!.GetBuffer();
                int length = (int)_memoryStream!.Length;
                _memoryStream = null;
                if (chunked)
                {
                    _chunked = true;
                    _context.Response.SendChunked = true;
                    _requestStream = new ChunkedInputStream(_context, _stream, buffer, _position, length - _position);
                }
                else
                {
                    _requestStream = new HttpRequestStream(_stream, buffer, _position, length - _position, contentlength);
                }
            }
            return _requestStream;
        }

        public HttpResponseStream GetResponseStream()
        {
            if (_responseStream == null)
            {
                HttpListener? listener = _context._listener;

                if (listener == null)
                    return new HttpResponseStream(_stream, _context.Response, true);

                _responseStream = new HttpResponseStream(_stream, _context.Response, listener.IgnoreWriteExceptions);
            }
            return _responseStream;
        }

        private static void OnRead(IAsyncResult ares)
        {
            HttpConnection cnc = (HttpConnection)ares.AsyncState!;
            cnc.OnReadInternal(ares);
        }

        private void OnReadInternal(IAsyncResult ares)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            int nread;
            try
            {
                nread = _stream.EndRead(ares);
                _memoryStream!.Write(_buffer!, 0, nread);
                if (_memoryStream.Length > 32768)
                {
                    SendError(HttpStatusDescription.Get(400), 400);
                    Close(true);
                    return;
                }
            }
            catch
            {
                if (_memoryStream != null && _memoryStream.Length > 0)
                    SendError();
                if (_socket != null)
                {
                    CloseSocket();
                    Unbind();
                }
                return;
            }

            if (nread == 0)
            {
                CloseSocket();
                Unbind();
                return;
            }

            if (ProcessInput(_memoryStream))
            {
                if (!_context.HaveError)
                    _context.Request.FinishInitialization();

                if (_context.HaveError)
                {
                    SendError();
                    Close(true);
                    return;
                }

                if (!_epl.BindContext(_context))
                {
                    const int NotFoundErrorCode = 404;
                    SendError(HttpStatusDescription.Get(NotFoundErrorCode), NotFoundErrorCode);
                    Close(true);
                    return;
                }
                HttpListener listener = _context._listener!;
                if (_lastListener != listener)
                {
                    RemoveConnection();
                    listener.AddConnection(this);
                    _lastListener = listener;
                }

                _contextBound = true;
                listener.RegisterContext(_context);
                return;
            }
            _stream.BeginRead(_buffer!, 0, BufferSize, s_onreadCallback, this);
        }

        private void RemoveConnection()
        {
            if (_lastListener == null)
                _epl.RemoveConnection(this);
            else
                _lastListener.RemoveConnection(this);
        }

        private enum InputState
        {
            RequestLine,
            Headers
        }

        private enum LineState
        {
            None,
            CR,
            LF
        }

        // true -> done processing
        // false -> need more input
        private bool ProcessInput(MemoryStream ms)
        {
            byte[] buffer = ms.GetBuffer();
            int len = (int)ms.Length;
            int used = 0;
            string? line;

            while (true)
            {
                if (_context.HaveError)
                    return true;

                if (_position >= len)
                    break;

                try
                {
                    line = ReadLine(buffer, _position, len - _position, ref used);
                    _position += used;
                }
                catch
                {
                    _context.ErrorMessage = HttpStatusDescription.Get(400)!;
                    _context.ErrorStatus = 400;
                    return true;
                }

                if (line == null)
                    break;

                if (line == "")
                {
                    if (_inputState == InputState.RequestLine)
                        continue;
                    _currentLine = null;
                    return true;
                }

                if (_inputState == InputState.RequestLine)
                {
                    _context.Request.SetRequestLine(line);
                    _inputState = InputState.Headers;
                }
                else
                {
                    try
                    {
                        _context.Request.AddHeader(line);
                    }
                    catch (Exception e)
                    {
                        _context.ErrorMessage = e.Message;
                        _context.ErrorStatus = 400;
                        return true;
                    }
                }
            }

            if (used == len)
            {
                ms.SetLength(0);
                _position = 0;
            }
            return false;
        }

        private string? ReadLine(byte[] buffer, int offset, int len, ref int used)
        {
            _currentLine ??= new StringBuilder(128);
            int last = offset + len;
            used = 0;
            for (int i = offset; i < last && _lineState != LineState.LF; i++)
            {
                used++;
                byte b = buffer[i];
                if (b == 13)
                {
                    _lineState = LineState.CR;
                }
                else if (b == 10)
                {
                    _lineState = LineState.LF;
                }
                else
                {
                    _currentLine.Append((char)b);
                }
            }

            string? result = null;
            if (_lineState == LineState.LF)
            {
                _lineState = LineState.None;
                result = _currentLine.ToString();
                _currentLine.Length = 0;
            }

            return result;
        }

        public void SendError(string? msg, int status)
        {
            try
            {
                HttpListenerResponse response = _context.Response;
                response.StatusCode = status;
                response.ContentType = "text/html";
                string? description = HttpStatusDescription.Get(status);
                string str = msg != null ?
                    $"<h1>{description} ({msg})</h1>" :
                    $"<h1>{description}</h1>";

                byte[] error = Encoding.Default.GetBytes(str);
                response.Close(error, false);
            }
            catch
            {
                // response was already closed
            }
        }

        public void SendError()
        {
            SendError(_context.ErrorMessage, _context.ErrorStatus);
        }

        private void Unbind()
        {
            if (_contextBound)
            {
                HttpEndPointListener.UnbindContext(_context);
                _contextBound = false;
            }
        }

        public void Close()
        {
            Close(false);
        }

        private void CloseSocket()
        {
            if (_socket == null)
                return;

            try
            {
                _socket.Close();
            }
            catch { }
            finally
            {
                _socket = null;
            }

            RemoveConnection();
        }

        internal void Close(bool force)
        {
            if (_socket != null)
            {
                GetResponseStream()?.Close();

                _responseStream = null;
            }

            if (_socket != null)
            {
                force |= !_context.Request.KeepAlive;
                if (!force)
                    force = (_context.Response.Headers[HttpKnownHeaderNames.Connection] == HttpHeaderStrings.Close);

                if (!force && _context.Request.FlushInput())
                {
                    if (_chunked && _context.Response.ForceCloseChunked == false)
                    {
                        // Don't close. Keep working.
                        _reuses++;
                        Unbind();
                        Init();
                        BeginReadRequest();
                        return;
                    }

                    _reuses++;
                    Unbind();
                    Init();
                    BeginReadRequest();
                    return;
                }

                Socket s = _socket;
                _socket = null;
                try
                {
                    s?.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                finally
                {
                    s?.Close();
                }
                Unbind();
                RemoveConnection();
                return;
            }
        }
    }
}
