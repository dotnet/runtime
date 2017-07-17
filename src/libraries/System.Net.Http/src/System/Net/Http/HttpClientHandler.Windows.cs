// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    // This implementation uses the System.Net.Http.WinHttpHandler class on Windows.  Other platforms will need to use
    // their own platform specific implementation.
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private readonly WinHttpHandler _winHttpHandler;
        private readonly ManagedHandler _managedHandler;
        private readonly DiagnosticsHandler _diagnosticsHandler;
        private bool _useProxy;

        public HttpClientHandler()
        {
            if (UseManagedHandler)
            {
                _managedHandler = new ManagedHandler();
                _diagnosticsHandler = new DiagnosticsHandler(_managedHandler);
            }
            else
            {
                _winHttpHandler = new WinHttpHandler();
                _diagnosticsHandler = new DiagnosticsHandler(_winHttpHandler);

                // Adjust defaults to match current .NET Desktop HttpClientHandler (based on HWR stack).
                AllowAutoRedirect = true;
                AutomaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
                UseProxy = true;
                UseCookies = true;
                CookieContainer = new CookieContainer();
                _winHttpHandler.DefaultProxyCredentials = null;
                _winHttpHandler.ServerCredentials = null;

                // The existing .NET Desktop HttpClientHandler based on the HWR stack uses only WinINet registry
                // settings for the proxy.  This also includes supporting the "Automatic Detect a proxy" using
                // WPAD protocol and PAC file. So, for app-compat, we will do the same for the default proxy setting.
                _winHttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
                _winHttpHandler.Proxy = null;

                // Since the granular WinHttpHandler timeout properties are not exposed via the HttpClientHandler API,
                // we need to set them to infinite and allow the HttpClient.Timeout property to have precedence.
                _winHttpHandler.ReceiveHeadersTimeout = Timeout.InfiniteTimeSpan;
                _winHttpHandler.ReceiveDataTimeout = Timeout.InfiniteTimeSpan;
                _winHttpHandler.SendTimeout = Timeout.InfiniteTimeSpan;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                ((HttpMessageHandler)_winHttpHandler ?? _managedHandler).Dispose();
            }

            base.Dispose(disposing);
        }

        public virtual bool SupportsAutomaticDecompression => true;
        public virtual bool SupportsProxy => true;
        public virtual bool SupportsRedirectConfiguration => true;

        public bool UseCookies
        {
            get => _winHttpHandler != null ? _winHttpHandler.CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer : _managedHandler.UseCookies;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.CookieUsePolicy = value ? CookieUsePolicy.UseSpecifiedCookieContainer : CookieUsePolicy.IgnoreCookies;
                }
                else
                {
                    _managedHandler.UseCookies = value;
                }
            }
        }

        public CookieContainer CookieContainer
        {
            get => _winHttpHandler != null ? _winHttpHandler.CookieContainer : _managedHandler.CookieContainer;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.CookieContainer = value;
                }
                else
                {
                    _managedHandler.CookieContainer = value;
                }
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get => _winHttpHandler != null ? _winHttpHandler.ClientCertificateOption : _managedHandler.ClientCertificateOptions;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.ClientCertificateOption = value;
                }
                else
                {
                    _managedHandler.ClientCertificateOptions = value;
                }
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => _winHttpHandler != null ? _winHttpHandler.AutomaticDecompression : _managedHandler.AutomaticDecompression;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.AutomaticDecompression = value;
                }
                else
                {
                    _managedHandler.AutomaticDecompression = value;
                }
            }
        }

        public bool UseProxy
        {
            get => _winHttpHandler != null ? _useProxy : _managedHandler.UseProxy;
            set
            {
                if (_winHttpHandler != null)
                {
                    _useProxy = value;
                }
                else
                {
                    _managedHandler.UseProxy = value;
                }
            }
        }

        public IWebProxy Proxy
        {
            get => _winHttpHandler != null ? _winHttpHandler.Proxy : _managedHandler.Proxy;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.Proxy = value;
                }
                else
                {
                    _managedHandler.Proxy = value;
                }
            }
        }

        public ICredentials DefaultProxyCredentials
        {
            get => _winHttpHandler != null ? _winHttpHandler.DefaultProxyCredentials : _managedHandler.DefaultProxyCredentials;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.DefaultProxyCredentials = value;
                }
                else
                {
                    _managedHandler.DefaultProxyCredentials = value;
                }
            }
        }

        public bool PreAuthenticate
        {
            get => _winHttpHandler != null ? _winHttpHandler.PreAuthenticate : _managedHandler.PreAuthenticate;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.PreAuthenticate = value;
                }
                else
                {
                    _managedHandler.PreAuthenticate = value;
                }
            }
        }

        public bool UseDefaultCredentials
        {
            // WinHttpHandler doesn't have a separate UseDefaultCredentials property.  There
            // is just a ServerCredentials property.  So, we need to map the behavior.
            //
            // This property only affect .ServerCredentials and not .DefaultProxyCredentials.

            get => _winHttpHandler != null ? _winHttpHandler.ServerCredentials == CredentialCache.DefaultCredentials : _managedHandler.UseDefaultCredentials;
            set
            {
                if (_winHttpHandler != null)
                {
                    if (value)
                    {
                        _winHttpHandler.ServerCredentials = CredentialCache.DefaultCredentials;
                    }
                    else
                    {
                        if (_winHttpHandler.ServerCredentials == CredentialCache.DefaultCredentials)
                        {
                            // Only clear out the ServerCredentials property if it was a DefaultCredentials.
                            _winHttpHandler.ServerCredentials = null;
                        }
                    }
                }
                else
                {
                    _managedHandler.UseDefaultCredentials = value;
                }
            }
        }

        public ICredentials Credentials
        {
            get => _winHttpHandler != null ? _winHttpHandler.ServerCredentials : _managedHandler.Credentials;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.ServerCredentials = value;
                }
                else
                {
                    _managedHandler.Credentials = value;
                }
            }
        }

        public bool AllowAutoRedirect
        {
            get => _winHttpHandler != null ? _winHttpHandler.AutomaticRedirection : _managedHandler.AllowAutoRedirect;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.AutomaticRedirection = value;
                }
                else
                {
                    _managedHandler.AllowAutoRedirect = value;
                }
            }
        }

        public int MaxAutomaticRedirections
        {
            get => _winHttpHandler != null ? _winHttpHandler.MaxAutomaticRedirections : _managedHandler.MaxAutomaticRedirections;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.MaxAutomaticRedirections = value;
                }
                else
                {
                    _managedHandler.MaxAutomaticRedirections = value;
                }
            }
        }

        public int MaxConnectionsPerServer
        {
            get => _winHttpHandler != null ? _winHttpHandler.MaxConnectionsPerServer : _managedHandler.MaxConnectionsPerServer;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.MaxConnectionsPerServer = value;
                }
                else
                {
                    _managedHandler.MaxConnectionsPerServer = value;
                }
            }
        }

        public int MaxResponseHeadersLength
        {
            get => _winHttpHandler != null ? _winHttpHandler.MaxResponseHeadersLength : _managedHandler.MaxResponseHeadersLength;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.MaxResponseHeadersLength = value;
                }
                else
                {
                    _managedHandler.MaxResponseHeadersLength = value;
                }
            }
        }

        public X509CertificateCollection ClientCertificates => _winHttpHandler != null ?
            _winHttpHandler.ClientCertificates :
            _managedHandler.ClientCertificates;
        
        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get => _winHttpHandler != null ? _winHttpHandler.ServerCertificateValidationCallback : _managedHandler.ServerCertificateCustomValidationCallback;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.ServerCertificateValidationCallback = value;
                }
                else
                {
                    _managedHandler.ServerCertificateCustomValidationCallback = value;
                }
            }
        }

        public bool CheckCertificateRevocationList
        {
            get => _winHttpHandler != null ? _winHttpHandler.CheckCertificateRevocationList : _managedHandler.CheckCertificateRevocationList;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.CheckCertificateRevocationList = value;
                }
                else
                {
                    _managedHandler.CheckCertificateRevocationList = value;
                }
            }
        }

        public SslProtocols SslProtocols
        {
            get => _winHttpHandler != null ? _winHttpHandler.SslProtocols : _managedHandler.SslProtocols;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.SslProtocols = value;
                }
                else
                {
                    _managedHandler.SslProtocols = value;
                }
            }
        }

        public IDictionary<String, object> Properties => _winHttpHandler != null ?
            _winHttpHandler.Properties :
            _managedHandler.Properties;
        

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_winHttpHandler != null)
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

                return DiagnosticsHandler.IsEnabled() ?
                    _diagnosticsHandler.SendAsync(request, cancellationToken) :
                    _winHttpHandler.SendAsync(request, cancellationToken);
            }
            else
            {
                return DiagnosticsHandler.IsEnabled() ?
                    _diagnosticsHandler.SendAsync(request, cancellationToken) :
                    _managedHandler.SendAsync(request, cancellationToken);
            }
        }
    }
}
