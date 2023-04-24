// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Net.HttpListenerResponse
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

using System.Globalization;
using System.IO;
using System.Text;

namespace System.Net
{
    public sealed partial class HttpListenerResponse : IDisposable
    {
        private long _contentLength;
        private Version _version = HttpVersion.Version11;
        private int _statusCode = 200;
        internal object _headersLock = new object();
        private bool _forceCloseChunked;

        internal HttpListenerResponse(HttpListenerContext context)
        {
            _httpContext = context;
        }

        internal bool ForceCloseChunked => _forceCloseChunked;

        private void EnsureResponseStream()
        {
            _responseStream ??= _httpContext!.Connection.GetResponseStream();
        }

        public Version ProtocolVersion
        {
            get => _version;
            set
            {
                CheckDisposed();
                ArgumentNullException.ThrowIfNull(value);
                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                {
                    throw new ArgumentException(SR.net_wrongversion, nameof(value));
                }

                _version = new Version(value.Major, value.Minor); // match Windows behavior, trimming to just Major.Minor
            }
        }

        public int StatusCode
        {
            get => _statusCode;
            set
            {
                CheckDisposed();

                if (value < 100 || value > 999)
                    throw new ProtocolViolationException(SR.net_invalidstatus);

                _statusCode = value;
            }
        }

        private void Dispose() => Close(true);

        public void Close()
        {
            if (Disposed)
                return;

            Close(false);
        }

        public void Abort()
        {
            if (Disposed)
                return;

            Close(true);
        }

        private void Close(bool force)
        {
            Disposed = true;
            _httpContext!.Connection.Close(force);
        }

        public void Close(byte[] responseEntity, bool willBlock)
        {
            CheckDisposed();

            ArgumentNullException.ThrowIfNull(responseEntity);

            if (!SentHeaders && _boundaryType != BoundaryType.Chunked)
            {
                ContentLength64 = responseEntity.Length;
            }

            if (willBlock)
            {
                try
                {
                    OutputStream.Write(responseEntity, 0, responseEntity.Length);
                }
                finally
                {
                    Close(false);
                }
            }
            else
            {
                OutputStream.BeginWrite(responseEntity, 0, responseEntity.Length, iar =>
                {
                    var thisRef = (HttpListenerResponse)iar.AsyncState!;
                    try
                    {
                        thisRef.OutputStream.EndWrite(iar);
                    }
                    finally
                    {
                        thisRef.Close(false);
                    }
                }, this);
            }
        }

        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            _webHeaders.Clear();
            _webHeaders.Add(templateResponse._webHeaders);
            _contentLength = templateResponse._contentLength;
            _statusCode = templateResponse._statusCode;
            _statusDescription = templateResponse._statusDescription;
            _keepAlive = templateResponse._keepAlive;
            _version = templateResponse._version;
        }

        internal void SendHeaders(bool closing, MemoryStream ms, bool isWebSocketHandshake = false)
        {
            if (!isWebSocketHandshake)
            {
                if (_webHeaders[HttpKnownHeaderNames.Server] == null)
                {
                    _webHeaders.Set(HttpKnownHeaderNames.Server, HttpHeaderStrings.NetCoreServerName);
                }

                if (_webHeaders[HttpKnownHeaderNames.Date] == null)
                {
                    _webHeaders.Set(HttpKnownHeaderNames.Date, DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));
                }

                if (_boundaryType == BoundaryType.None)
                {
                    if (HttpListenerRequest.ProtocolVersion <= HttpVersion.Version10)
                    {
                        _keepAlive = false;
                    }
                    else
                    {
                        _boundaryType = BoundaryType.Chunked;
                    }

                    if (CanSendResponseBody(_httpContext!.Response.StatusCode))
                    {
                        _contentLength = -1;
                    }
                    else
                    {
                        _boundaryType = BoundaryType.ContentLength;
                        _contentLength = 0;
                    }
                }

                if (_boundaryType != BoundaryType.Chunked)
                {
                    if (_boundaryType != BoundaryType.ContentLength && closing)
                    {
                        _contentLength = CanSendResponseBody(_httpContext!.Response.StatusCode) ? -1 : 0;
                    }

                    if (_boundaryType == BoundaryType.ContentLength)
                    {
                        _webHeaders.Set(HttpKnownHeaderNames.ContentLength, _contentLength.ToString("D", CultureInfo.InvariantCulture));
                    }
                }

                /* Apache forces closing the connection for these status codes:
                 *    HttpStatusCode.BadRequest                 400
                 *    HttpStatusCode.RequestTimeout             408
                 *    HttpStatusCode.LengthRequired             411
                 *    HttpStatusCode.RequestEntityTooLarge     413
                 *    HttpStatusCode.RequestUriTooLong         414
                 *    HttpStatusCode.InternalServerError      500
                 *    HttpStatusCode.ServiceUnavailable         503
                 */
                bool conn_close = (_statusCode == (int)HttpStatusCode.BadRequest || _statusCode == (int)HttpStatusCode.RequestTimeout
                        || _statusCode == (int)HttpStatusCode.LengthRequired || _statusCode == (int)HttpStatusCode.RequestEntityTooLarge
                        || _statusCode == (int)HttpStatusCode.RequestUriTooLong || _statusCode == (int)HttpStatusCode.InternalServerError
                        || _statusCode == (int)HttpStatusCode.ServiceUnavailable);

                if (!conn_close)
                {
                    conn_close = !_httpContext!.Request.KeepAlive;
                }

                // They sent both KeepAlive: true and Connection: close
                if (!_keepAlive || conn_close)
                {
                    _webHeaders.Set(HttpKnownHeaderNames.Connection, HttpHeaderStrings.Close);
                    conn_close = true;
                }

                if (SendChunked)
                {
                    _webHeaders.Set(HttpKnownHeaderNames.TransferEncoding, HttpHeaderStrings.Chunked);
                }

                int reuses = _httpContext!.Connection.Reuses;
                if (reuses >= 100)
                {
                    _forceCloseChunked = true;
                    if (!conn_close)
                    {
                        _webHeaders.Set(HttpKnownHeaderNames.Connection, HttpHeaderStrings.Close);
                        conn_close = true;
                    }
                }

                if (HttpListenerRequest.ProtocolVersion <= HttpVersion.Version10)
                {
                    if (_keepAlive)
                    {
                        Headers[HttpResponseHeader.KeepAlive] = "true";
                    }

                    if (!conn_close)
                    {
                        _webHeaders.Set(HttpKnownHeaderNames.Connection, HttpHeaderStrings.KeepAlive);
                    }
                }

                ComputeCookies();
            }

            Encoding encoding = Encoding.Default;
            StreamWriter writer = new StreamWriter(ms, encoding, 256);
            writer.Write("HTTP/1.1 {0} ", _statusCode); // "1.1" matches Windows implementation, which ignores the response version
            writer.Flush();
            byte[] statusDescriptionBytes = WebHeaderEncoding.GetBytes(StatusDescription);
            ms.Write(statusDescriptionBytes, 0, statusDescriptionBytes.Length);
            writer.Write("\r\n");

            writer.Write(FormatHeaders(_webHeaders));
            writer.Flush();
            int preamble = encoding.Preamble.Length;
            EnsureResponseStream();

            /* Assumes that the ms was at position 0 */
            ms.Position = preamble;
            SentHeaders = !isWebSocketHandshake;
        }

        private static bool HeaderCanHaveEmptyValue(string name) =>
            !string.Equals(name, HttpKnownHeaderNames.Location, StringComparison.OrdinalIgnoreCase);

        private static string FormatHeaders(WebHeaderCollection headers)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < headers.Count; i++)
            {
                string key = headers.GetKey(i);
                string[] values = headers.GetValues(i)!;

                int startingLength = sb.Length;

                sb.Append(key).Append(": ");
                bool anyValues = false;
                for (int j = 0; j < values.Length; j++)
                {
                    string value = values[j];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (anyValues)
                        {
                            if (key.Equals(HttpKnownHeaderNames.SetCookie, StringComparison.OrdinalIgnoreCase) ||
                                key.Equals(HttpKnownHeaderNames.SetCookie2, StringComparison.OrdinalIgnoreCase))
                            {
                                sb.Append("\r\n").Append(key).Append(": ");
                            }
                            else
                            {
                                sb.Append(", ");
                            }
                        }
                        sb.Append(value);
                        anyValues = true;
                    }
                }

                if (anyValues || HeaderCanHaveEmptyValue(key))
                {
                    // Complete the header
                    sb.Append("\r\n");
                }
                else
                {
                    // Empty header; remove it.
                    sb.Length = startingLength;
                }
            }

            return sb.Append("\r\n").ToString();
        }

        private bool Disposed { get; set; }
        internal bool SentHeaders { get; set; }
    }
}
