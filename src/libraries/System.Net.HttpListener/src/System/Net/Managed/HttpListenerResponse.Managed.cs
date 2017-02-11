// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Net.HttpListenerResponse
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

using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed partial class HttpListenerResponse : IDisposable
    {
        private bool _disposed;
        private Encoding _contentEncoding;
        private long _contentLength;
        private bool _clSet;
        private string _contentType;
        private bool _keepAlive = true;
        private HttpResponseStream _outputStream;
        private Version _version = HttpVersion.Version11;
        private string _location;
        private int _statusCode = 200;
        private string _statusDescription = "OK";
        private bool _chunked;
        private HttpListenerContext _context;
        internal bool _headersSent;
        internal object _headersLock = new object();
        private bool _forceCloseChunked;

        internal HttpListenerResponse(HttpListenerContext context)
        {
            _context = context;
        }

        internal bool ForceCloseChunked
        {
            get { return _forceCloseChunked; }
        }

        public Encoding ContentEncoding
        {
            get
            {
                if (_contentEncoding == null)
                {
                    _contentEncoding = Encoding.Default;
                }

                return _contentEncoding;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException(SR.net_cannot_change_after_headers);

                _contentEncoding = value;
            }
        }

        public long ContentLength64
        {
            get { return _contentLength; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException(SR.net_cannot_change_after_headers);

                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_clsmall);

                _clSet = true;
                _contentLength = value;
            }
        }

        public string ContentType
        {
            get { return _contentType; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException(SR.net_cannot_change_after_headers);

                _contentType = value;
            }
        }

        public bool KeepAlive
        {
            get { return _keepAlive; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException(SR.net_cannot_change_after_headers);

                _keepAlive = value;
            }
        }

        public Stream OutputStream
        {
            get
            {
                if (_outputStream == null)
                    _outputStream = _context.Connection.GetResponseStream();
                return _outputStream;
            }
        }

        public Version ProtocolVersion
        {
            get { return _version; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException(SR.net_cannot_change_after_headers);

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                    throw new ArgumentException(SR.net_wrongversion, nameof(value));

                _version = value;
            }
        }

        public string RedirectLocation
        {
            get { return _location; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException(SR.net_cannot_change_after_headers);

                _location = value;
            }
        }

        public bool SendChunked
        {
            get { return _chunked; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException(SR.net_cannot_change_after_headers);

                _chunked = value;
            }
        }

        public int StatusCode
        {
            get { return _statusCode; }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (_headersSent)
                    throw new InvalidOperationException(SR.net_cannot_change_after_headers);

                if (value < 100 || value > 999)
                    throw new ProtocolViolationException(SR.net_invalidstatus);

                _statusCode = value;
            }
        }

        public string StatusDescription
        {
            get { return _statusDescription; }
            set
            {
                _statusDescription = value;
            }
        }

        private void Dispose()
        {
            Close(true);
        }

        public void Close()
        {
            if (_disposed)
                return;

            Close(false);
        }

        public void Abort()
        {
            if (_disposed)
                return;

            Close(true);
        }

        private void Close(bool force)
        {
            _disposed = true;
            _context.Connection.Close(force);
        }

        public void Close(byte[] responseEntity, bool willBlock)
        {
            if (_disposed)
                return;

            if (responseEntity == null)
                throw new ArgumentNullException(nameof(responseEntity));

            ContentLength64 = responseEntity.Length;
            OutputStream.Write(responseEntity, 0, (int)_contentLength);
            Close(false);
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

        public void Redirect(string url)
        {
            StatusCode = 302; // Found
            _location = url;
        }

        private bool FindCookie(Cookie cookie)
        {
            string name = cookie.Name;
            string domain = cookie.Domain;
            string path = cookie.Path;
            foreach (Cookie c in _cookies)
            {
                if (name != c.Name)
                    continue;
                if (domain != c.Domain)
                    continue;
                if (path == c.Path)
                    return true;
            }

            return false;
        }

        internal void SendHeaders(bool closing, MemoryStream ms, bool isWebSocketHandshake = false)
        {
            Encoding encoding = _contentEncoding;
            if (encoding == null)
                encoding = Encoding.Default;

            if (!isWebSocketHandshake)
            {
                if (_contentType != null)
                {
                    if (_contentEncoding != null && _contentType.IndexOf(HttpHeaderStrings.Charset, StringComparison.Ordinal) == -1)
                    {
                        string enc_name = _contentEncoding.WebName;
                        _webHeaders.Set(HttpKnownHeaderNames.ContentType, _contentType + "; " + HttpHeaderStrings.Charset + enc_name);
                    }
                    else
                    {
                        _webHeaders.Set(HttpKnownHeaderNames.ContentType, _contentType);
                    }
                }

                if (_webHeaders[HttpKnownHeaderNames.Server] == null)
                    _webHeaders.Set(HttpKnownHeaderNames.Server, HttpHeaderStrings.NetCoreServerName);
                CultureInfo inv = CultureInfo.InvariantCulture;
                if (_webHeaders[HttpKnownHeaderNames.Date] == null)
                    _webHeaders.Set(HttpKnownHeaderNames.Date, DateTime.UtcNow.ToString("r", inv));

                if (!_chunked)
                {
                    if (!_clSet && closing)
                    {
                        _clSet = true;
                        _contentLength = 0;
                    }

                    if (_clSet)
                        _webHeaders.Set(HttpKnownHeaderNames.ContentLength, _contentLength.ToString(inv));
                }

                Version v = _context.Request.ProtocolVersion;
                if (!_clSet && !_chunked && v >= HttpVersion.Version11)
                    _chunked = true;

                /* Apache forces closing the connection for these status codes:
                 *	HttpStatusCode.BadRequest 		        400
                 *	HttpStatusCode.RequestTimeout 		    408
                 *	HttpStatusCode.LengthRequired 		    411
                 *	HttpStatusCode.RequestEntityTooLarge 	413
                 *	HttpStatusCode.RequestUriTooLong 	    414
                 *	HttpStatusCode.InternalServerError      500
                 *	HttpStatusCode.ServiceUnavailable 	    503
                 */
                bool conn_close = (_statusCode == (int)HttpStatusCode.BadRequest || _statusCode == (int)HttpStatusCode.RequestTimeout
                        || _statusCode == (int)HttpStatusCode.LengthRequired || _statusCode == (int)HttpStatusCode.RequestEntityTooLarge
                        || _statusCode == (int)HttpStatusCode.RequestUriTooLong || _statusCode == (int)HttpStatusCode.InternalServerError
                        || _statusCode == (int)HttpStatusCode.ServiceUnavailable);

                if (conn_close == false)
                    conn_close = !_context.Request.KeepAlive;

                // They sent both KeepAlive: true and Connection: close
                if (!_keepAlive || conn_close)
                {
                    _webHeaders.Set(HttpKnownHeaderNames.Connection, HttpHeaderStrings.Close);
                    conn_close = true;
                }

                if (_chunked)
                    _webHeaders.Set(HttpKnownHeaderNames.TransferEncoding, HttpHeaderStrings.Chunked);

                int reuses = _context.Connection.Reuses;
                if (reuses >= 100)
                {
                    _forceCloseChunked = true;
                    if (!conn_close)
                    {
                        _webHeaders.Set(HttpKnownHeaderNames.Connection, HttpHeaderStrings.Close);
                        conn_close = true;
                    }
                }

                if (!conn_close)
                {
                    _webHeaders.Set(HttpKnownHeaderNames.KeepAlive, String.Format("timeout=15,max={0}", 100 - reuses));
                    if (_context.Request.ProtocolVersion <= HttpVersion.Version10)
                        _webHeaders.Set(HttpKnownHeaderNames.Connection, HttpHeaderStrings.KeepAlive);
                }

                if (_location != null)
                    _webHeaders.Set(HttpKnownHeaderNames.Location, _location);

                if (_cookies != null)
                {
                    foreach (Cookie cookie in _cookies)
                        _webHeaders.Set(HttpKnownHeaderNames.SetCookie, CookieToClientString(cookie));
                }
            }

            StreamWriter writer = new StreamWriter(ms, encoding, 256);
            writer.Write("HTTP/{0} {1} {2}\r\n", _version, _statusCode, _statusDescription);
            string headers_str = FormatHeaders(_webHeaders);
            writer.Write(headers_str);
            writer.Flush();
            int preamble = encoding.GetPreamble().Length;
            if (_outputStream == null)
                _outputStream = _context.Connection.GetResponseStream();

            /* Assumes that the ms was at position 0 */
            ms.Position = preamble;
            _headersSent = !isWebSocketHandshake;
        }

        private static string FormatHeaders(WebHeaderCollection headers)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < headers.Count; i++)
            {
                string key = headers.GetKey(i);
                string[] values = headers.GetValues(i);
                for (int j = 0; j < values.Length; j++)
                {
                    sb.Append(key).Append(": ").Append(values[j]).Append("\r\n");
                }
            }

            return sb.Append("\r\n").ToString();
        }

        private static string CookieToClientString(Cookie cookie)
        {
            if (cookie.Name.Length == 0)
                return String.Empty;

            StringBuilder result = new StringBuilder(64);

            if (cookie.Version > 0)
                result.Append("Version=").Append(cookie.Version).Append(";");

            result.Append(cookie.Name).Append("=").Append(cookie.Value);

            if (cookie.Path != null && cookie.Path.Length != 0)
                result.Append(";Path=").Append(QuotedString(cookie, cookie.Path));

            if (cookie.Domain != null && cookie.Domain.Length != 0)
                result.Append(";Domain=").Append(QuotedString(cookie, cookie.Domain));

            if (cookie.Port != null && cookie.Port.Length != 0)
                result.Append(";Port=").Append(cookie.Port);

            return result.ToString();
        }

        private static string QuotedString(Cookie cookie, string value)
        {
            if (cookie.Version == 0 || IsToken(value))
                return value;
            else
                return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string s_tspecials = "()<>@,;:\\\"/[]?={} \t";   // from RFC 2965, 2068

        private static bool IsToken(string value)
        {
            int len = value.Length;
            for (int i = 0; i < len; i++)
            {
                char c = value[i];
                if (c < 0x20 || c >= 0x7f || s_tspecials.IndexOf(c) != -1)
                    return false;
            }
            return true;
        }
    }
}

