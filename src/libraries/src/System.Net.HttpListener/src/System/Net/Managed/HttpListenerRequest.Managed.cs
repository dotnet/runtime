// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Net.HttpListenerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo.mono@gmail.com)
//	Marek Safar (marek.safar@gmail.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2011-2012 Xamarin, Inc. (http://xamarin.com)
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

using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed partial class HttpListenerRequest
    {
        private class Context : TransportContext
        {
            public override ChannelBinding GetChannelBinding(ChannelBindingKind kind)
            {
                throw new NotImplementedException();
            }
        }

        private long _contentLength;
        private bool _clSet;
        private CookieCollection _cookies;
        private WebHeaderCollection _headers;
        private string _method;
        private Stream _inputStream;
        private HttpListenerContext _context;
        private bool _isChunked;
        private bool _kaSet;
        private bool _keepAlive;

        private static byte[] s_100continue = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;
            _headers = new WebHeaderCollection();
            _version = HttpVersion.Version10;
        }

        private static char[] s_separators = new char[] { ' ' };

        internal void SetRequestLine(string req)
        {
            string[] parts = req.Split(s_separators, 3);
            if (parts.Length != 3)
            {
                _context.ErrorMessage = "Invalid request line (parts).";
                return;
            }

            _method = parts[0];
            foreach (char c in _method)
            {
                int ic = (int)c;

                if ((ic >= 'A' && ic <= 'Z') ||
                    (ic > 32 && c < 127 && c != '(' && c != ')' && c != '<' &&
                     c != '<' && c != '>' && c != '@' && c != ',' && c != ';' &&
                     c != ':' && c != '\\' && c != '"' && c != '/' && c != '[' &&
                     c != ']' && c != '?' && c != '=' && c != '{' && c != '}'))
                    continue;

                _context.ErrorMessage = "(Invalid verb)";
                return;
            }

            _rawUrl = parts[1];
            if (parts[2].Length != 8 || !parts[2].StartsWith("HTTP/"))
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }

            try
            {
                _version = new Version(parts[2].Substring(5));
                if (_version.Major < 1)
                    throw new Exception();
            }
            catch
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }
        }

        private static bool MaybeUri(string s)
        {
            int p = s.IndexOf(':');
            if (p == -1)
                return false;

            if (p >= 10)
                return false;

            return IsPredefinedScheme(s.Substring(0, p));
        }

        private static bool IsPredefinedScheme(string scheme)
        {
            if (scheme == null || scheme.Length < 3)
                return false;

            char c = scheme[0];
            if (c == 'h')
                return (scheme == UriScheme.Http ||  scheme == UriScheme.Https);
            if (c == 'f')
                return (scheme == UriScheme.File || scheme == UriScheme.Ftp);

            if (c == 'n')
            {
                c = scheme[1];
                if (c == 'e')
                    return (scheme == UriScheme.News || scheme == UriScheme.NetPipe || scheme == UriScheme.NetTcp);
                if (scheme == UriScheme.Nntp)
                    return true;
                return false;
            }
            if ((c == 'g' && scheme == UriScheme.Gopher) || (c == 'm' && scheme == UriScheme.Mailto))
                return true;

            return false;
        }

        internal void FinishInitialization()
        {
            string host = UserHostName;
            if (_version > HttpVersion.Version10 && (host == null || host.Length == 0))
            {
                _context.ErrorMessage = "Invalid host name";
                return;
            }

            string path;
            Uri raw_uri = null;
            if (MaybeUri(_rawUrl.ToLowerInvariant()) && Uri.TryCreate(_rawUrl, UriKind.Absolute, out raw_uri))
                path = raw_uri.PathAndQuery;
            else
                path = _rawUrl;

            if ((host == null || host.Length == 0))
                host = UserHostAddress;

            if (raw_uri != null)
                host = raw_uri.Host;

            int colon = host.IndexOf(':');
            if (colon >= 0)
                host = host.Substring(0, colon);

            string base_uri = string.Format("{0}://{1}:{2}", RequestScheme, host, LocalEndPoint.Port);

            if (!Uri.TryCreate(base_uri + path, UriKind.Absolute, out _requestUri))
            {
                _context.ErrorMessage = WebUtility.HtmlEncode("Invalid url: " + base_uri + path);
                return;
            }

            _requestUri = HttpListenerRequestUriBuilder.GetRequestUri(_rawUrl, _requestUri.Scheme,
                                _requestUri.Authority, _requestUri.LocalPath, _requestUri.Query);

            if (_version >= HttpVersion.Version11)
            {
                string t_encoding = Headers[HttpKnownHeaderNames.TransferEncoding];
                _isChunked = (t_encoding != null && string.Equals(t_encoding, "chunked", StringComparison.OrdinalIgnoreCase));
                // 'identity' is not valid!
                if (t_encoding != null && !_isChunked)
                {
                    _context.Connection.SendError(null, 501);
                    return;
                }
            }

            if (!_isChunked && !_clSet)
            {
                if (string.Equals(_method, "POST", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_method, "PUT", StringComparison.OrdinalIgnoreCase))
                {
                    _context.Connection.SendError(null, 411);
                    return;
                }
            }

            if (String.Compare(Headers[HttpKnownHeaderNames.Expect], "100-continue", StringComparison.OrdinalIgnoreCase) == 0)
            {
                HttpResponseStream output = _context.Connection.GetResponseStream();
                output.InternalWrite(s_100continue, 0, s_100continue.Length);
            }
        }

        internal static string Unquote(String str)
        {
            int start = str.IndexOf('\"');
            int end = str.LastIndexOf('\"');
            if (start >= 0 && end >= 0)
                str = str.Substring(start + 1, end - 1);
            return str.Trim();
        }

        internal void AddHeader(string header)
        {
            int colon = header.IndexOf(':');
            if (colon == -1 || colon == 0)
            {
                _context.ErrorMessage = HttpStatusDescription.Get(400);
                _context.ErrorStatus = 400;
                return;
            }

            string name = header.Substring(0, colon).Trim();
            string val = header.Substring(colon + 1).Trim();
            string lower = name.ToLower(CultureInfo.InvariantCulture);
            _headers.Set(name, val);
            switch (lower)
            {
                case "content-length":
                    try
                    {
                        _contentLength = long.Parse(val.Trim());
                        if (_contentLength < 0)
                            _context.ErrorMessage = "Invalid Content-Length.";
                        _clSet = true;
                    }
                    catch
                    {
                        _context.ErrorMessage = "Invalid Content-Length.";
                    }

                    break;
                case "cookie":
                    if (_cookies == null)
                        _cookies = new CookieCollection();

                    string[] cookieStrings = val.Split(new char[] { ',', ';' });
                    Cookie current = null;
                    int version = 0;
                    foreach (string cookieString in cookieStrings)
                    {
                        string str = cookieString.Trim();
                        if (str.Length == 0)
                            continue;
                        if (str.StartsWith("$Version"))
                        {
                            version = Int32.Parse(Unquote(str.Substring(str.IndexOf('=') + 1)));
                        }
                        else if (str.StartsWith("$Path"))
                        {
                            if (current != null)
                                current.Path = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else if (str.StartsWith("$Domain"))
                        {
                            if (current != null)
                                current.Domain = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else if (str.StartsWith("$Port"))
                        {
                            if (current != null)
                                current.Port = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else
                        {
                            if (current != null)
                            {
                                _cookies.Add(current);
                            }
                            current = new Cookie();
                            int idx = str.IndexOf('=');
                            if (idx > 0)
                            {
                                current.Name = str.Substring(0, idx).Trim();
                                current.Value = str.Substring(idx + 1).Trim();
                            }
                            else
                            {
                                current.Name = str.Trim();
                                current.Value = String.Empty;
                            }
                            current.Version = version;
                        }
                    }
                    if (current != null)
                    {
                        _cookies.Add(current);
                    }
                    break;
            }
        }

        // returns true is the stream could be reused.
        internal bool FlushInput()
        {
            if (!HasEntityBody)
                return true;

            int length = 2048;
            if (_contentLength > 0)
                length = (int)Math.Min(_contentLength, (long)length);

            byte[] bytes = new byte[length];
            while (true)
            {
                try
                {
                    IAsyncResult ares = InputStream.BeginRead(bytes, 0, length, null, null);
                    if (!ares.IsCompleted && !ares.AsyncWaitHandle.WaitOne(1000))
                        return false;
                    if (InputStream.EndRead(ares) <= 0)
                        return true;
                }
                catch (ObjectDisposedException)
                {
                    _inputStream = null;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public int ClientCertificateError
        {
            get
            {
                HttpConnection cnc = _context.Connection;
                if (cnc.ClientCertificate == null)
                    return 0;
                int[] errors = cnc.ClientCertificateErrors;
                if (errors != null && errors.Length > 0)
                    return errors[0];
                return 0;
            }
        }

        public long ContentLength64
        {
            get
            {
                if (_isChunked)
                    _contentLength = -1;

                return _contentLength;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = new CookieCollection();
                return _cookies;
            }
        }

        public bool HasEntityBody => (_contentLength > 0 || _isChunked);

        public NameValueCollection Headers => _headers;

        public string HttpMethod => _method;

        public Stream InputStream
        {
            get
            {
                if (_inputStream == null)
                {
                    if (_isChunked || _contentLength > 0)
                        _inputStream = _context.Connection.GetRequestStream(_isChunked, _contentLength);
                    else
                        _inputStream = Stream.Null;
                }

                return _inputStream;
            }
        }

        public bool IsAuthenticated => false;

        public bool IsSecureConnection => _context.Connection.IsSecure;

        public bool KeepAlive
        {
            get
            {
                if (_kaSet)
                    return _keepAlive;

                _kaSet = true;
                // 1. Connection header
                // 2. Protocol (1.1 == keep-alive by default)
                // 3. Keep-Alive header
                string cnc = Headers[HttpKnownHeaderNames.Connection];
                if (!String.IsNullOrEmpty(cnc))
                {
                    _keepAlive = string.Equals(cnc, "keep-alive", StringComparison.OrdinalIgnoreCase);
                }
                else if (_version == HttpVersion.Version11)
                {
                    _keepAlive = true;
                }
                else
                {
                    cnc = Headers[HttpKnownHeaderNames.KeepAlive];
                    if (!String.IsNullOrEmpty(cnc))
                        _keepAlive = !string.Equals(cnc, "closed", StringComparison.OrdinalIgnoreCase);
                }
                return _keepAlive;
            }
        }

        public IPEndPoint LocalEndPoint => _context.Connection.LocalEndPoint;

        public IPEndPoint RemoteEndPoint => _context.Connection.RemoteEndPoint;

        public Guid RequestTraceIdentifier => Guid.Empty;

        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            Task<X509Certificate2> getClientCertificate = new Task<X509Certificate2>(() => GetClientCertificate());
            return TaskToApm.Begin(getClientCertificate, requestCallback, state);
        }

        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));
            
            return TaskToApm.End<X509Certificate2>(asyncResult);
        }

        public X509Certificate2 GetClientCertificate() => _context.Connection.ClientCertificate;

        public string ServiceName => null;

        public TransportContext TransportContext => new Context();

        public Task<X509Certificate2> GetClientCertificateAsync()
        {
            return Task<X509Certificate2>.Factory.FromAsync(BeginGetClientCertificate, EndGetClientCertificate, null);
        }

        private Uri RequestUri => _requestUri;
        private bool SupportsWebSockets => true;
    }
}
