// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using System.Net;

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

        private string[] _accept_types;
        private Encoding _content_encoding;
        private long _content_length;
        private bool _cl_set;
        private CookieCollection _cookies;
        private WebHeaderCollection _headers;
        private string _method;
        private Stream _input_stream;
        private Version _version;
        private NameValueCollection _query_string; // check if null is ok, check if read-only, check case-sensitiveness
        private string _raw_url;
        private Uri _url;
        private Uri _referrer;
        private string[] _user_languages;
        private HttpListenerContext _context;
        private bool _is_chunked;
        private bool _ka_set;
        private bool _keep_alive;
        private delegate X509Certificate2 GCCDelegate();
        private GCCDelegate _gccDelegate;

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

            _raw_url = parts[1];
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
            if (query == null || query.Length == 0)
            {
                _query_string = new NameValueCollection(1);
                return;
            }

            _query_string = new NameValueCollection();
            if (query[0] == '?')
                query = query.Substring(1);
            string[] components = query.Split('&');
            foreach (string kv in components)
            {
                int pos = kv.IndexOf('=');
                if (pos == -1)
                {
                    _query_string.Add(null, WebUtility.UrlDecode(kv));
                }
                else
                {
                    string key = WebUtility.UrlDecode(kv.Substring(0, pos));
                    string val = WebUtility.UrlDecode(kv.Substring(pos + 1));

                    _query_string.Add(key, val);
                }
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
            if (MaybeUri(_raw_url.ToLowerInvariant()) && Uri.TryCreate(_raw_url, UriKind.Absolute, out raw_uri))
                path = raw_uri.PathAndQuery;
            else
                path = _raw_url;

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

            _url = HttpListenerRequestUriBuilder.GetRequestUri(_raw_url, _url.Scheme,
                                _url.Authority, _url.LocalPath, _url.Query);

            if (_version >= HttpVersion.Version11)
            {
                string t_encoding = Headers["Transfer-Encoding"];
                _is_chunked = (t_encoding != null && String.Compare(t_encoding, "chunked", StringComparison.OrdinalIgnoreCase) == 0);
                // 'identity' is not valid!
                if (t_encoding != null && !_is_chunked)
                {
                    _context.Connection.SendError(null, 501);
                    return;
                }
            }

            if (!_is_chunked && !_cl_set)
            {
                if (String.Compare(_method, "POST", StringComparison.OrdinalIgnoreCase) == 0 ||
                    String.Compare(_method, "PUT", StringComparison.OrdinalIgnoreCase) == 0)
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
                    _user_languages = val.Split(','); // yes, only split with a ','
                    break;
                case "accept":
                    _accept_types = val.Split(','); // yes, only split with a ','
                    break;
                case "content-length":
                    try
                    {
                        _content_length = long.Parse(val.Trim());
                        if (_content_length < 0)
                            _context.ErrorMessage = "Invalid Content-Length.";
                        _cl_set = true;
                    }
                    catch
                    {
                        _context.ErrorMessage = "Invalid Content-Length.";
                    }

                    break;
                case "referer":
                    try
                    {
                        _referrer = new Uri(val);
                    }
                    catch
                    {
                        _referrer = new Uri("http://someone.is.screwing.with.the.headers.com/");
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
            if (_content_length > 0)
                length = (int)Math.Min(_content_length, (long)length);

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
                    _input_stream = null;
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
            get { return _accept_types; }
        }

        public int ClientCertificateError
        {
            get
            {
                HttpConnection cnc = _context.Connection;
                if (cnc.ClientCertificate == null)
                    throw new InvalidOperationException(SR.net_no_client_certificate);
                int[] errors = cnc.ClientCertificateErrors;
                if (errors != null && errors.Length > 0)
                    return errors[0];
                return 0;
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                if (_content_encoding == null)
                    _content_encoding = Encoding.Default;
                return _content_encoding;
            }
        }

        public long ContentLength64
        {
            get { return _content_length; }
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
            get { return (_content_length > 0 || _is_chunked); }
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
                if (_input_stream == null)
                {
                    if (_is_chunked || _content_length > 0)
                        _input_stream = _context.Connection.GetRequestStream(_is_chunked, _content_length);
                    else
                        _input_stream = Stream.Null;
                }

                return _input_stream;
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
                if (_ka_set)
                    return _keep_alive;

                _ka_set = true;
                // 1. Connection header
                // 2. Protocol (1.1 == keep-alive by default)
                // 3. Keep-Alive header
                string cnc = _headers["Connection"];
                if (!String.IsNullOrEmpty(cnc))
                {
                    _keep_alive = (0 == String.Compare(cnc, "keep-alive", StringComparison.OrdinalIgnoreCase));
                }
                else if (_version == HttpVersion.Version11)
                {
                    _keep_alive = true;
                }
                else
                {
                    cnc = _headers["keep-alive"];
                    if (!String.IsNullOrEmpty(cnc))
                        _keep_alive = (0 != String.Compare(cnc, "closed", StringComparison.OrdinalIgnoreCase));
                }
                return _keep_alive;
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
            get { return _query_string; }
        }

        public string RawUrl
        {
            get { return _raw_url; }
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
            get { return _user_languages; }
        }

        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            if (_gccDelegate == null)
                _gccDelegate = new GCCDelegate(GetClientCertificate);
            return _gccDelegate.BeginInvoke(requestCallback, state);
        }

        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (_gccDelegate == null)
                throw new InvalidOperationException();

            return _gccDelegate.EndInvoke(asyncResult);
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
                return false;
            }
        }

        public Task<X509Certificate2> GetClientCertificateAsync()
        {
            return Task<X509Certificate2>.Factory.FromAsync(BeginGetClientCertificate, EndGetClientCertificate, null);
        }
    }
}

