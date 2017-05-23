// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Net
{
    public sealed unsafe partial class HttpListenerResponse : IDisposable
    {
        private CookieCollection _cookies;
        private bool _keepAlive = true;
        private HttpResponseStream _responseStream;
        private string _statusDescription;
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

        public Encoding ContentEncoding { get; set; }

        public string ContentType
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
                return _responseStream;
            }
        }

        public string RedirectLocation
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
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Need to verify the status description doesn't contain any control characters except HT.  We mask off the high
                // byte since that's how it's encoded.
                for (int i = 0; i < value.Length; i++)
                {
                    char c = (char)(0x000000ff & (uint)value[i]);
                    if ((c <= 31 && c != (byte)'\t') || c == 127)
                    {
                        throw new ArgumentException(SR.net_WebHeaderInvalidControlChars, "name");
                    }
                }

                _statusDescription = value;
            }
        }

        public void AddHeader(string name, string value)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"name={name}, value={value}");
            Headers.Set(name, value);
        }

        public void AppendHeader(string name, string value)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"name={name}, value={value}");
            Headers.Add(value);
        }

        public void AppendCookie(Cookie cookie)
        {
            if (cookie == null)
            {
                throw new ArgumentNullException(nameof(cookie));
            }
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"cookie: {cookie}");
            Cookies.Add(cookie);
        }

        public void Redirect(string url)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"url={url}");
            Headers[HttpResponseHeader.Location] = url;
            StatusCode = (int)HttpStatusCode.Redirect;
            StatusDescription = HttpStatusDescription.Get(StatusCode);
        }

        public void SetCookie(Cookie cookie)
        {
            if (cookie == null)
            {
                throw new ArgumentNullException(nameof(cookie));
            }
            bool added = false;
            try
            {
                Cookies.Add(cookie);
                added = true;
            }
            catch (CookieException)
            {
                Debug.Assert(!added);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"cookie: {cookie}");

            if (!added)
            {
                // cookie already existed and couldn't be replaced
                throw new ArgumentException(SR.net_cookie_exists, nameof(cookie));
            }
        }

        void IDisposable.Dispose() => Dispose();

        private void CheckDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
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
