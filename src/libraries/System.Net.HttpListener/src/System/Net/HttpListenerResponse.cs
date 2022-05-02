// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace System.Net
{
    public sealed unsafe partial class HttpListenerResponse : IDisposable
    {
        private BoundaryType _boundaryType = BoundaryType.None;
        private CookieCollection? _cookies;
        private readonly HttpListenerContext? _httpContext;
        private bool _keepAlive = true;
        private HttpResponseStream? _responseStream;
        private string? _statusDescription;
        private WebHeaderCollection _webHeaders = new WebHeaderCollection();

        public WebHeaderCollection Headers
        {
            get => _webHeaders;
            set
            {
                _webHeaders = new WebHeaderCollection();
                foreach (string headerName in value.AllKeys)
                {
                    _webHeaders.Add(headerName, value[headerName]);
                }
            }
        }

        public Encoding? ContentEncoding { get; set; }

        public string? ContentType
        {
            get => Headers[HttpKnownHeaderNames.ContentType];
            set
            {
                CheckDisposed();
                if (string.IsNullOrEmpty(value))
                {
                    Headers.Remove(HttpKnownHeaderNames.ContentType);
                }
                else
                {
                    Headers.Set(HttpKnownHeaderNames.ContentType, value);
                }
            }
        }

        private HttpListenerContext HttpListenerContext => _httpContext!;

        private HttpListenerRequest HttpListenerRequest => HttpListenerContext!.Request;

        internal EntitySendFormat EntitySendFormat
        {
            get => (EntitySendFormat)_boundaryType;
            set
            {
                CheckDisposed();
                CheckSentHeaders();
                if (value == EntitySendFormat.Chunked && HttpListenerRequest.ProtocolVersion.Minor == 0)
                {
                    throw new ProtocolViolationException(SR.net_nochunkuploadonhttp10);
                }
                _boundaryType = (BoundaryType)value;
                if (value != EntitySendFormat.ContentLength)
                {
                    _contentLength = -1;
                }
            }
        }

        public bool SendChunked
        {
            get => EntitySendFormat == EntitySendFormat.Chunked;
            set => EntitySendFormat = value ? EntitySendFormat.Chunked : EntitySendFormat.ContentLength;
        }

        // We MUST NOT send message-body when we send responses with these Status codes
        private static readonly int[] s_noResponseBody = { 100, 101, 204, 205, 304 };

        private static bool CanSendResponseBody(int responseCode)
        {
            for (int i = 0; i < s_noResponseBody.Length; i++)
            {
                if (responseCode == s_noResponseBody[i])
                {
                    return false;
                }
            }
            return true;
        }

        public long ContentLength64
        {
            get => _contentLength;
            set
            {
                CheckDisposed();
                CheckSentHeaders();
                if (value >= 0)
                {
                    _contentLength = value;
                    _boundaryType = BoundaryType.ContentLength;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.net_clsmall);
                }
            }
        }

        public CookieCollection Cookies
        {
            get => _cookies ?? (_cookies = new CookieCollection());
            set => _cookies = value;
        }

        public bool KeepAlive
        {
            get => _keepAlive;
            set
            {
                CheckDisposed();
                _keepAlive = value;
            }
        }

        public Stream OutputStream
        {
            get
            {
                CheckDisposed();
                EnsureResponseStream();
                return _responseStream!;
            }
        }

        public string? RedirectLocation
        {
            get => Headers[HttpResponseHeader.Location];
            set
            {
                // note that this doesn't set the status code to a redirect one
                CheckDisposed();
                if (string.IsNullOrEmpty(value))
                {
                    Headers.Remove(HttpKnownHeaderNames.Location);
                }
                else
                {
                    Headers.Set(HttpKnownHeaderNames.Location, value);
                }
            }
        }

        public string StatusDescription
        {
            get
            {
                if (_statusDescription == null)
                {
                    // if the user hasn't set this, generated on the fly, if possible.
                    // We know this one is safe, no need to verify it as in the setter.
                    _statusDescription = HttpStatusDescription.Get(StatusCode);
                }
                if (_statusDescription == null)
                {
                    _statusDescription = string.Empty;
                }
                return _statusDescription;
            }
            set
            {
                CheckDisposed();
                ArgumentNullException.ThrowIfNull(value);

                // Need to verify the status description doesn't contain any control characters except HT.  We mask off the high
                // byte since that's how it's encoded.
                for (int i = 0; i < value.Length; i++)
                {
                    char c = (char)(0x000000ff & (uint)value[i]);
                    if ((c <= 31 && c != (byte)'\t') || c == 127)
                    {
                        throw new ArgumentException(SR.net_WebHeaderInvalidControlChars, nameof(value));
                    }
                }

                _statusDescription = value;
            }
        }

        public void AddHeader(string name, string value)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"name={name}, value={value}");
            Headers.Set(name, value);
        }

        public void AppendHeader(string name, string value)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"name={name}, value={value}");
            Headers.Add(name, value);
        }

        public void AppendCookie(Cookie cookie!!)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"cookie: {cookie}");
            Cookies.Add(cookie);
        }

        private void ComputeCookies()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Entering Set-Cookie: {Headers[HttpResponseHeader.SetCookie]}, Set-Cookie2: {Headers[HttpKnownHeaderNames.SetCookie2]}");

            if (_cookies != null)
            {
                // now go through the collection, and concatenate all the cookies in per-variant strings
                string? setCookie2 = null, setCookie = null;
                for (int index = 0; index < _cookies.Count; index++)
                {
                    Cookie cookie = _cookies[index];
                    string cookieString = cookie.ToServerString();
                    if (cookieString == null || cookieString.Length == 0)
                    {
                        continue;
                    }
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Now looking at index:{index} cookie: {cookie}");
                    if (cookie.IsRfc2965Variant())
                    {
                        setCookie2 = setCookie2 == null ? cookieString : setCookie2 + ", " + cookieString;
                    }
                    else
                    {
                        setCookie = setCookie == null ? cookieString : setCookie + ", " + cookieString;
                    }
                }

                if (!string.IsNullOrEmpty(setCookie))
                {
                    Headers.Set(HttpKnownHeaderNames.SetCookie, setCookie);
                    if (string.IsNullOrEmpty(setCookie2))
                    {
                        Headers.Remove(HttpKnownHeaderNames.SetCookie2);
                    }
                }

                if (!string.IsNullOrEmpty(setCookie2))
                {
                    Headers.Set(HttpKnownHeaderNames.SetCookie2, setCookie2);
                    if (string.IsNullOrEmpty(setCookie))
                    {
                        Headers.Remove(HttpKnownHeaderNames.SetCookie);
                    }
                }
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Exiting Set-Cookie: {Headers[HttpResponseHeader.SetCookie]} Set-Cookie2: {Headers[HttpKnownHeaderNames.SetCookie2]}");
        }

        public void Redirect([StringSyntax(StringSyntaxAttribute.Uri)] string url)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"url={url}");
            Headers[HttpResponseHeader.Location] = url;
            StatusCode = (int)HttpStatusCode.Redirect;
            StatusDescription = HttpStatusDescription.Get(StatusCode)!;
        }

        public void SetCookie(Cookie cookie!!)
        {
            Cookie newCookie = cookie.Clone();
            int added = Cookies.InternalAdd(newCookie, true);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"cookie: {cookie}");

            if (added != 1)
            {
                // The Cookie already existed and couldn't be replaced.
                throw new ArgumentException(SR.net_cookie_exists, nameof(cookie));
            }
        }

        void IDisposable.Dispose() => Dispose();

        private void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
        }

        private void CheckSentHeaders()
        {
            if (SentHeaders)
            {
                throw new InvalidOperationException(SR.net_rspsubmitted);
            }
        }
    }
}
