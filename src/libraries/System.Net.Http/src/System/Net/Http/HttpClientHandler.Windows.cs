// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    // This implementation uses the System.Net.Http.WinHttpHandler class on Windows.  Other platforms will need to use
    // their own platform specific implementation.
    public class HttpClientHandler : HttpMessageHandler
    {
        #region Properties

        public virtual bool SupportsAutomaticDecompression
        {
            get { return true; }
        }

        public virtual bool SupportsProxy
        {
            get { return true; }
        }

        public virtual bool SupportsRedirectConfiguration
        {
            get { return true; }
        }

        public bool UseCookies
        {
            get { return (_winHttpHandler.CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer); }
            set { _winHttpHandler.CookieUsePolicy = value ? CookieUsePolicy.UseSpecifiedCookieContainer : CookieUsePolicy.IgnoreCookies; }
        }

        public CookieContainer CookieContainer
        {
            get { return _winHttpHandler.CookieContainer; }
            set { _winHttpHandler.CookieContainer = value; }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get { return _winHttpHandler.ClientCertificateOption; }
            set { _winHttpHandler.ClientCertificateOption = value; }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get { return _winHttpHandler.AutomaticDecompression; }
            set { _winHttpHandler.AutomaticDecompression = value; }
        }

        public bool UseProxy
        {
            get { return _useProxy; }
            set { _useProxy = value; }
        }

        public IWebProxy Proxy
        {
            get { return _winHttpHandler.Proxy; }
            set { _winHttpHandler.Proxy = value; }
        }

        public bool PreAuthenticate
        {
            get { return _winHttpHandler.PreAuthenticate; }
            set { _winHttpHandler.PreAuthenticate = value; }
        }

        public bool UseDefaultCredentials
        {
            // WinHttpHandler doesn't have a separate UseDefaultCredentials property.  There
            // is just a ServerCredentials property.  So, we need to map the behavior.

            get { return (_winHttpHandler.ServerCredentials == CredentialCache.DefaultCredentials); }

            set
            {
                if (value)
                {
                    _winHttpHandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                    _winHttpHandler.ServerCredentials = CredentialCache.DefaultCredentials;
                }
                else
                {
                    _winHttpHandler.DefaultProxyCredentials = null;

                    if (_winHttpHandler.ServerCredentials == CredentialCache.DefaultCredentials)
                    {
                        // Only clear out the ServerCredentials property if it was a DefaultCredentials.
                        _winHttpHandler.ServerCredentials = null;
                    }
                }
            }
        }

        public ICredentials Credentials
        {
            get { return _winHttpHandler.ServerCredentials; }
            set { _winHttpHandler.ServerCredentials = value; }
        }

        public bool AllowAutoRedirect
        {
            get
            {
                return _winHttpHandler.AutomaticRedirection;
            }
            set
            {
                _winHttpHandler.AutomaticRedirection = value;
            }
        }

        public int MaxAutomaticRedirections
        {
            get { return _winHttpHandler.MaxAutomaticRedirections; }
            set { _winHttpHandler.MaxAutomaticRedirections = value; }
        }

        public long MaxRequestContentBufferSize
        {
            // This property has been deprecated. In the .NET Desktop it was only used when the handler needed to 
            // automatically buffer the request content. That only happened if neither 'Content-Length' nor 
            // 'Transfer-Encoding: chunked' request headers were specified. So, the handler thus needed to buffer
            // in the request content to determine its length and then would choose 'Content-Length' semantics when
            // POST'ing. In CoreCLR and .NETNative, the handler will resolve the ambiguity by always choosing
            // 'Transfer-Encoding: chunked'. The handler will never automatically buffer in the request content.
            get { return 0; }
            
            // TODO: Add message/link to exception explaining the deprecation. 
            // Update corresponding exception in HttpClientHandler.Unix.cs if/when this is updated.
            set { throw new PlatformNotSupportedException(); }
        }

        #endregion Properties

        #region De/Constructors

        public HttpClientHandler()
        {
            _winHttpHandler = new WinHttpHandler();

            // Adjust defaults to match current .NET Desktop HttpClientHandler (based on HWR stack).
            AllowAutoRedirect = true;
            UseProxy = true;
            UseCookies = true;
            CookieContainer = new CookieContainer();

            // The existing .NET Desktop HttpClientHandler based on the HWR stack uses only WinINet registry
            // settings for the proxy.  This also includes supporting the "Automatic Detect a proxy" using
            // WPAD protocol and PAC file. So, for app-compat, we will do the same for the default proxy setting.
            _winHttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
            _winHttpHandler.Proxy = null;
            
            // Since the granular WinHttpHandler timeout properties are not exposed via the HttpClientHandler API,
            // we need to set them to infinite and allow the HttpClient.Timeout property to have precedence.
            _winHttpHandler.ConnectTimeout = Timeout.InfiniteTimeSpan;
            _winHttpHandler.ReceiveHeadersTimeout = Timeout.InfiniteTimeSpan;
            _winHttpHandler.ReceiveDataTimeout = Timeout.InfiniteTimeSpan;
            _winHttpHandler.SendTimeout = Timeout.InfiniteTimeSpan;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                // Release WinHttp session handle.
                _winHttpHandler.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion De/Constructors

        #region Request Execution

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Get current value of WindowsProxyUsePolicy.  Only call its WinHttpHandler
            // property setter if the value needs to change.
            var oldProxyUsePolicy = _winHttpHandler.WindowsProxyUsePolicy;

            if (_useProxy)
            {
                if (_winHttpHandler.Proxy == null)
                {
                    if (oldProxyUsePolicy != WindowsProxyUsePolicy.UseWinInetProxy)
                    {
                        _winHttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                    }
                }
                else
                {
                    if (oldProxyUsePolicy != WindowsProxyUsePolicy.UseCustomProxy)
                    {
                        _winHttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
                    }
                }
            }
            else
            {
                if (oldProxyUsePolicy != WindowsProxyUsePolicy.DoNotUseProxy)
                {
                    _winHttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.DoNotUseProxy;
                }
            }

            return _winHttpHandler.SendAsync(request, cancellationToken);
        }

        #endregion Request Execution

        #region Private

        private WinHttpHandler _winHttpHandler;
        private bool _useProxy;
        private volatile bool _disposed;
        #endregion Private

    }
}
