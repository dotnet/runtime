// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Net
{
    public sealed unsafe partial class HttpListenerResponse : IDisposable
    {
        private CookieCollection _cookies;
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

        public CookieCollection Cookies
        {
            get => _cookies ?? (_cookies = new CookieCollection());
            set => _cookies = value;
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
