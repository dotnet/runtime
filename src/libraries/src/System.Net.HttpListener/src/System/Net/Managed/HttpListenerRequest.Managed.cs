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
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace System.Net
{
    public sealed partial class HttpListenerRequest
    {
        private class Context : TransportContext
        {
            public override ChannelBinding GetChannelBinding(ChannelBindingKind kind)
            {
                if (kind != ChannelBindingKind.Endpoint)
                {
                    throw new NotSupportedException(SR.Format(SR.net_listener_invalid_cbt_type, kind.ToString()));
                }

                return null;
            }
        }

        private long _contentLength;
        private bool _clSet;
        private WebHeaderCollection _headers;
        private string _method;
        private Stream _inputStream;
        private HttpListenerContext _context;
        private bool _isChunked;

        private static byte[] s_100continue = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;
            _headers = new WebHeaderCollection();
            _version = HttpVersion.Version10;
        }

        private static readonly char[] s_separators = new char[] { ' ' };

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
            }
            catch
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }

            if (_version.Major < 1)
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }
            if (_version.Major > 1)
            {
                _context.ErrorStatus = (int)HttpStatusCode.HttpVersionNotSupported;
                _context.ErrorMessage = HttpStatusDescription.Get(HttpStatusCode.HttpVersionNotSupported);
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

            if (string.Equals(Headers[HttpKnownHeaderNames.Expect], "100-continue", StringComparison.OrdinalIgnoreCase))
            {
                HttpResponseStream output = _context.Connection.GetResponseStream();
                output.InternalWrite(s_100continue, 0, s_100continue.Length);
            }
        }

        internal static string Unquote(string str)
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
            if (name.Equals("content-length", StringComparison.OrdinalIgnoreCase))
            {
                // To match Windows behavior:
                // Content lengths >= 0 and <= long.MaxValue are accepted as is.
                // Content lengths > long.MaxValue and <= ulong.MaxValue are treated as 0.
                // Content lengths < 0 cause the requests to fail.
                // Other input is a failure, too.
                long parsedContentLength =
                    ulong.TryParse(val, out ulong parsedUlongContentLength) ? (parsedUlongContentLength <= long.MaxValue ? (long)parsedUlongContentLength : 0) :
                    long.Parse(val);
                if (parsedContentLength < 0 || (_clSet && parsedContentLength != _contentLength))
                {
                    _context.ErrorMessage = "Invalid Content-Length.";
                }
                else
                {
                    _contentLength = parsedContentLength;
                    _clSet = true;
                }
            }
            else if (name.Equals("transfer-encoding", StringComparison.OrdinalIgnoreCase))
            {
                if (Headers[HttpKnownHeaderNames.TransferEncoding] != null)
                {
                    _context.ErrorStatus = (int)HttpStatusCode.NotImplemented;
                    _context.ErrorMessage = HttpStatusDescription.Get(HttpStatusCode.NotImplemented);
                }
            }

            if (_context.ErrorMessage == null)
            {
                _headers.Set(name, val);
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

        private X509Certificate2 GetClientCertificateCore() => ClientCertificate = _context.Connection.ClientCertificate;

        private int GetClientCertificateErrorCore()
        {
            HttpConnection cnc = _context.Connection;
            if (cnc.ClientCertificate == null)
                return 0;
            int[] errors = cnc.ClientCertificateErrors;
            if (errors != null && errors.Length > 0)
                return errors[0];
            return 0;
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

        public IPEndPoint LocalEndPoint => _context.Connection.LocalEndPoint;

        public IPEndPoint RemoteEndPoint => _context.Connection.RemoteEndPoint;

        public Guid RequestTraceIdentifier { get; } = Guid.NewGuid();

        private IAsyncResult BeginGetClientCertificateCore(AsyncCallback requestCallback, object state)
        {
            var asyncResult = new GetClientCertificateAsyncResult(this, state, requestCallback);

            // The certificate is already retrieved by the time this method is called. GetClientCertificateCore() evaluates to
            // a simple member access, so this will always complete immediately.
            ClientCertState = ListenerClientCertState.Completed;
            asyncResult.InvokeCallback(GetClientCertificateCore());

            return asyncResult;
        }

        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            GetClientCertificateAsyncResult clientCertAsyncResult = asyncResult as GetClientCertificateAsyncResult;
            if (clientCertAsyncResult == null || clientCertAsyncResult.AsyncObject != this)
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult, nameof(asyncResult));
            }
            if (clientCertAsyncResult.EndCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, nameof(EndGetClientCertificate)));
            }
            clientCertAsyncResult.EndCalled = true;

            return (X509Certificate2)clientCertAsyncResult.Result;
        }

        public string ServiceName => null;

        public TransportContext TransportContext => new Context();

        private Uri RequestUri => _requestUri;
        private bool SupportsWebSockets => true;

        private class GetClientCertificateAsyncResult : LazyAsyncResult
        {
            public GetClientCertificateAsyncResult(object myObject, object myState, AsyncCallback myCallBack) : base(myObject, myState, myCallBack) { }
        }
    }
}
