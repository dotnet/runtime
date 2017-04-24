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

        private string[] _acceptTypes;
        private long _contentLength;
        private bool _clSet;
        private CookieCollection _cookies;
        private WebHeaderCollection _headers;
        private string _method;
        private Stream _inputStream;
        private Version _version;
        private NameValueCollection _queryString; // check if null is ok, check if read-only, check case-sensitiveness
        private string _rawUrl;
        private Uri _url;
        private Uri _referrer;
        private string[] _userLanguages;
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

        private void CreateQueryString(string query)
        {
            _queryString = new NameValueCollection();
            Helpers.FillFromString(_queryString, Url.Query, true, ContentEncoding);
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
                return (scheme == "http" || scheme == "https");
            if (c == 'f')
                return (scheme == "file" || scheme == "ftp");

            if (c == 'n')
            {
                c = scheme[1];
                if (c == 'e')
                    return (scheme == "news" || scheme == "net.pipe" || scheme == "net.tcp");
                if (scheme == "nntp")
                    return true;
                return false;
            }
            if ((c == 'g' && scheme == "gopher") || (c == 'm' && scheme == "mailto"))
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

            string base_uri = String.Format("{0}://{1}:{2}",
                                (IsSecureConnection) ? "https" : "http",
                                host, LocalEndPoint.Port);

            if (!Uri.TryCreate(base_uri + path, UriKind.Absolute, out _url))
            {
                _context.ErrorMessage = WebUtility.HtmlEncode("Invalid url: " + base_uri + path);
                return;
            }

            CreateQueryString(_url.Query);

            _url = HttpListenerRequestUriBuilder.GetRequestUri(_rawUrl, _url.Scheme,
                                _url.Authority, _url.LocalPath, _url.Query);

            if (_version >= HttpVersion.Version11)
            {
                string t_encoding = Headers["Transfer-Encoding"];
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

            if (String.Compare(Headers["Expect"], "100-continue", StringComparison.OrdinalIgnoreCase) == 0)
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
                case "accept-language":
                    _userLanguages = Helpers.ParseMultivalueHeader(val);
                    break;
                case "accept":
                    _acceptTypes = Helpers.ParseMultivalueHeader(val);
                    break;
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
                case "referer":
                    try
                    {
                        _referrer = new Uri(val, UriKind.RelativeOrAbsolute);
                    }
                    catch
                    {
                        _referrer = null;
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
        
        public string[] AcceptTypes
        {
            get { return _acceptTypes; }
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

        public string ContentType
        {
            get { return _headers["content-type"]; }
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

        public bool HasEntityBody
        {
            get { return (_contentLength > 0 || _isChunked); }
        }

        public NameValueCollection Headers
        {
            get { return _headers; }
        }

        public string HttpMethod
        {
            get { return _method; }
        }

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

        public bool IsAuthenticated
        {
            get { return false; }
        }

        public bool IsLocal
        {
            get { return LocalEndPoint.Address.Equals(RemoteEndPoint.Address); }
        }

        public bool IsSecureConnection
        {
            get { return _context.Connection.IsSecure; }
        }

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
                string cnc = _headers["Connection"];
                if (!String.IsNullOrEmpty(cnc))
                {
                    _keepAlive = (0 == String.Compare(cnc, "keep-alive", StringComparison.OrdinalIgnoreCase));
                }
                else if (_version == HttpVersion.Version11)
                {
                    _keepAlive = true;
                }
                else
                {
                    cnc = _headers["keep-alive"];
                    if (!String.IsNullOrEmpty(cnc))
                        _keepAlive = (0 != String.Compare(cnc, "closed", StringComparison.OrdinalIgnoreCase));
                }
                return _keepAlive;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get { return _context.Connection.LocalEndPoint; }
        }

        public Version ProtocolVersion
        {
            get { return _version; }
        }

        public NameValueCollection QueryString
        {
            get { return _queryString; }
        }

        public string RawUrl
        {
            get { return _rawUrl; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return _context.Connection.RemoteEndPoint; }
        }

        public Guid RequestTraceIdentifier
        {
            get { return Guid.Empty; }
        }

        public Uri Url
        {
            get { return _url; }
        }

        public Uri UrlReferrer
        {
            get { return _referrer; }
        }

        public string UserAgent
        {
            get { return _headers["user-agent"]; }
        }

        public string UserHostAddress
        {
            get { return LocalEndPoint.ToString(); }
        }

        public string UserHostName
        {
            get { return _headers["host"]; }
        }

        public string[] UserLanguages
        {
            get { return _userLanguages; }
        }

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

        public X509Certificate2 GetClientCertificate()
        {
            return _context.Connection.ClientCertificate;
        }

        public string ServiceName
        {
            get
            {
                return null;
            }
        }

        public TransportContext TransportContext
        {
            get
            {
                return new Context();
            }
        }

        public bool IsWebSocketRequest
        {
            get
            {
                if (string.IsNullOrEmpty(Headers[HttpKnownHeaderNames.Connection]) || string.IsNullOrEmpty(Headers[HttpKnownHeaderNames.Upgrade]))
                {
                    return false;
                }

                bool foundConnectionUpgradeHeader = false;
                foreach (string connection in Headers.GetValues(HttpKnownHeaderNames.Connection))
                {
                    if (string.Equals(connection, HttpKnownHeaderNames.Upgrade, StringComparison.OrdinalIgnoreCase))
                    {
                        foundConnectionUpgradeHeader = true;
                        break;
                    }
                }

                if (!foundConnectionUpgradeHeader)
                {
                    return false;
                }

                foreach (string upgrade in Headers.GetValues(HttpKnownHeaderNames.Upgrade))
                {
                    if (string.Equals(upgrade, HttpWebSocket.WebSocketUpgradeToken, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Task<X509Certificate2> GetClientCertificateAsync()
        {
            return Task<X509Certificate2>.Factory.FromAsync(BeginGetClientCertificate, EndGetClientCertificate, null);
        }
    }
}

