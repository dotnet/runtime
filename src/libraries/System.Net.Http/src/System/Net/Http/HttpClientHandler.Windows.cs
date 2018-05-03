// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        private readonly SocketsHttpHandler _socketsHttpHandler;
        private readonly DiagnosticsHandler _diagnosticsHandler;
        private bool _useProxy;
        private ClientCertificateOption _clientCertificateOptions;

        public HttpClientHandler() : this(UseSocketsHttpHandler) { }

        private HttpClientHandler(bool useSocketsHttpHandler) // used by parameterless ctor and as hook for testing
        {
            if (useSocketsHttpHandler)
            {
                _socketsHttpHandler = new SocketsHttpHandler();
                _diagnosticsHandler = new DiagnosticsHandler(_socketsHttpHandler);
                ClientCertificateOptions = ClientCertificateOption.Manual;

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
                ((HttpMessageHandler)_winHttpHandler ?? _socketsHttpHandler).Dispose();
            }

            base.Dispose(disposing);
        }

        public virtual bool SupportsAutomaticDecompression => true;
        public virtual bool SupportsProxy => true;
        public virtual bool SupportsRedirectConfiguration => true;

        public bool UseCookies
        {
            get => _winHttpHandler != null ? _winHttpHandler.CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer : _socketsHttpHandler.UseCookies;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.CookieUsePolicy = value ? CookieUsePolicy.UseSpecifiedCookieContainer : CookieUsePolicy.IgnoreCookies;
                }
                else
                {
                    _socketsHttpHandler.UseCookies = value;
                }
            }
        }

        public CookieContainer CookieContainer
        {
            get => _winHttpHandler != null ? _winHttpHandler.CookieContainer : _socketsHttpHandler.CookieContainer;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (_winHttpHandler != null)
                {
                    _winHttpHandler.CookieContainer = value;
                }
                else
                {
                    _socketsHttpHandler.CookieContainer = value;
                }
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => _winHttpHandler != null ? _winHttpHandler.AutomaticDecompression : _socketsHttpHandler.AutomaticDecompression;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.AutomaticDecompression = value;
                }
                else
                {
                    _socketsHttpHandler.AutomaticDecompression = value;
                }
            }
        }

        public bool UseProxy
        {
            get => _winHttpHandler != null ? _useProxy : _socketsHttpHandler.UseProxy;
            set
            {
                if (_winHttpHandler != null)
                {
                    _useProxy = value;
                }
                else
                {
                    _socketsHttpHandler.UseProxy = value;
                }
            }
        }

        public IWebProxy Proxy
        {
            get => _winHttpHandler != null ? _winHttpHandler.Proxy : _socketsHttpHandler.Proxy;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.Proxy = value;
                }
                else
                {
                    _socketsHttpHandler.Proxy = value;
                }
            }
        }

        public ICredentials DefaultProxyCredentials
        {
            get => _winHttpHandler != null ? _winHttpHandler.DefaultProxyCredentials : _socketsHttpHandler.DefaultProxyCredentials;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.DefaultProxyCredentials = value;
                }
                else
                {
                    _socketsHttpHandler.DefaultProxyCredentials = value;
                }
            }
        }

        public bool PreAuthenticate
        {
            get => _winHttpHandler != null ? _winHttpHandler.PreAuthenticate : _socketsHttpHandler.PreAuthenticate;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.PreAuthenticate = value;
                }
                else
                {
                    _socketsHttpHandler.PreAuthenticate = value;
                }
            }
        }

        public bool UseDefaultCredentials
        {
            // WinHttpHandler doesn't have a separate UseDefaultCredentials property.  There
            // is just a ServerCredentials property.  So, we need to map the behavior.
            // Do the same for SocketsHttpHandler.Credentials.
            //
            // This property only affect .ServerCredentials and not .DefaultProxyCredentials.

            get => _winHttpHandler != null ? _winHttpHandler.ServerCredentials == CredentialCache.DefaultCredentials :
                    _socketsHttpHandler.Credentials == CredentialCache.DefaultCredentials;
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
                    if (value)
                    {
                        _socketsHttpHandler.Credentials = CredentialCache.DefaultCredentials;
                    }
                    else
                    {
                        if (_socketsHttpHandler.Credentials == CredentialCache.DefaultCredentials)
                        {
                            // Only clear out the Credentials property if it was a DefaultCredentials.
                            _socketsHttpHandler.Credentials = null;
                        }
                    }
                }
            }
        }

        public ICredentials Credentials
        {
            get => _winHttpHandler != null ? _winHttpHandler.ServerCredentials : _socketsHttpHandler.Credentials;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.ServerCredentials = value;
                }
                else
                {
                    _socketsHttpHandler.Credentials = value;
                }
            }
        }

        public bool AllowAutoRedirect
        {
            get => _winHttpHandler != null ? _winHttpHandler.AutomaticRedirection : _socketsHttpHandler.AllowAutoRedirect;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.AutomaticRedirection = value;
                }
                else
                {
                    _socketsHttpHandler.AllowAutoRedirect = value;
                }
            }
        }

        public int MaxAutomaticRedirections
        {
            get => _winHttpHandler != null ? _winHttpHandler.MaxAutomaticRedirections : _socketsHttpHandler.MaxAutomaticRedirections;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.MaxAutomaticRedirections = value;
                }
                else
                {
                    _socketsHttpHandler.MaxAutomaticRedirections = value;
                }
            }
        }

        public int MaxConnectionsPerServer
        {
            get => _winHttpHandler != null ? _winHttpHandler.MaxConnectionsPerServer : _socketsHttpHandler.MaxConnectionsPerServer;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.MaxConnectionsPerServer = value;
                }
                else
                {
                    _socketsHttpHandler.MaxConnectionsPerServer = value;
                }
            }
        }

        public int MaxResponseHeadersLength
        {
            get => _winHttpHandler != null ? _winHttpHandler.MaxResponseHeadersLength : _socketsHttpHandler.MaxResponseHeadersLength;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.MaxResponseHeadersLength = value;
                }
                else
                {
                    _socketsHttpHandler.MaxResponseHeadersLength = value;
                }
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get
            {
                if (_winHttpHandler != null)
                {
                    return _winHttpHandler.ClientCertificateOption;
                }
                else
                {
                    return _clientCertificateOptions;
                }
            }
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.ClientCertificateOption = value;
                }
                else
                {
                    switch (value)
                    {
                        case ClientCertificateOption.Manual:
                            ThrowForModifiedManagedSslOptionsIfStarted();
                            _clientCertificateOptions = value;
                            _socketsHttpHandler.SslOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => CertificateHelper.GetEligibleClientCertificate(ClientCertificates);
                            break;

                        case ClientCertificateOption.Automatic:
                            ThrowForModifiedManagedSslOptionsIfStarted();
                            _clientCertificateOptions = value;
                            _socketsHttpHandler.SslOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => CertificateHelper.GetEligibleClientCertificate();
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(value));
                    }
                }
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (_winHttpHandler != null)
                {
                    return _winHttpHandler.ClientCertificates;
                }
                else
                {
                    if (ClientCertificateOptions != ClientCertificateOption.Manual)
                    {
                        throw new InvalidOperationException(SR.Format(SR.net_http_invalid_enable_first, nameof(ClientCertificateOptions), nameof(ClientCertificateOption.Manual)));
                    }

                    return _socketsHttpHandler.SslOptions.ClientCertificates ??
                        (_socketsHttpHandler.SslOptions.ClientCertificates = new X509CertificateCollection());
                }
            }
        }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get
            {
                return _winHttpHandler != null ?
                    _winHttpHandler.ServerCertificateValidationCallback :
                    (_socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback?.Target as ConnectHelper.CertificateCallbackMapper)?.FromHttpClientHandler;
            }
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.ServerCertificateValidationCallback = value;
                }
                else
                {
                    ThrowForModifiedManagedSslOptionsIfStarted();
                    _socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = value != null ?
                        new ConnectHelper.CertificateCallbackMapper(value).ForSocketsHttpHandler :
                        null;
                }
            }
        }

        public bool CheckCertificateRevocationList
        {
            get => _winHttpHandler != null ? _winHttpHandler.CheckCertificateRevocationList : _socketsHttpHandler.SslOptions.CertificateRevocationCheckMode == X509RevocationMode.Online;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.CheckCertificateRevocationList = value;
                }
                else
                {
                    ThrowForModifiedManagedSslOptionsIfStarted();
                    _socketsHttpHandler.SslOptions.CertificateRevocationCheckMode = value ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
                }
            }
        }

        public SslProtocols SslProtocols
        {
            get => _winHttpHandler != null ? _winHttpHandler.SslProtocols : _socketsHttpHandler.SslOptions.EnabledSslProtocols;
            set
            {
                if (_winHttpHandler != null)
                {
                    _winHttpHandler.SslProtocols = value;
                }
                else
                {
                    ThrowForModifiedManagedSslOptionsIfStarted();
                    _socketsHttpHandler.SslOptions.EnabledSslProtocols = value;
                }
            }
        }

        public IDictionary<string, object> Properties => _winHttpHandler != null ?
            _winHttpHandler.Properties :
            _socketsHttpHandler.Properties;
        
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
                    _socketsHttpHandler.SendAsync(request, cancellationToken);
            }
        }
    }
}
