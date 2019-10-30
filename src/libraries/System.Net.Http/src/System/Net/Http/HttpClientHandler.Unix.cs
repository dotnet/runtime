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
    public partial class HttpClientHandler : HttpMessageHandler
    {
        // Only one of these two handlers will be initialized.
        private readonly CurlHandler _curlHandler;
        private readonly SocketsHttpHandler _socketsHttpHandler;
        private readonly DiagnosticsHandler _diagnosticsHandler;
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
                _curlHandler = new CurlHandler();
                _diagnosticsHandler = new DiagnosticsHandler(_curlHandler);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((HttpMessageHandler)_curlHandler ?? _socketsHttpHandler).Dispose();
            }
            base.Dispose(disposing);
        }

        public virtual bool SupportsAutomaticDecompression => _curlHandler == null || _curlHandler.SupportsAutomaticDecompression;

        public virtual bool SupportsProxy => true;

        public virtual bool SupportsRedirectConfiguration => true;

        public bool UseCookies
        {
            get => _curlHandler != null ? _curlHandler.UseCookies : _socketsHttpHandler.UseCookies;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.UseCookies = value;
                }
                else
                {
                    _socketsHttpHandler.UseCookies = value;
                }
            }
        }

        public CookieContainer CookieContainer
        {
            get => _curlHandler != null ? _curlHandler.CookieContainer : _socketsHttpHandler.CookieContainer;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (_curlHandler != null)
                {
                    _curlHandler.CookieContainer = value;
                }
                else
                {
                    _socketsHttpHandler.CookieContainer = value;
                }
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get
            {
                if (_curlHandler != null)
                {
                    return _curlHandler.ClientCertificateOptions;
                }
                else
                {
                    return _clientCertificateOptions;
                }
            }
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.ClientCertificateOptions = value;
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
                if (_curlHandler != null)
                {
                    return _curlHandler.ClientCertificates;
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
                return _curlHandler != null ?
                    _curlHandler.ServerCertificateCustomValidationCallback :
                    (_socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback?.Target as ConnectHelper.CertificateCallbackMapper)?.FromHttpClientHandler;
            }
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.ServerCertificateCustomValidationCallback = value;
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
            get => _curlHandler != null ? _curlHandler.CheckCertificateRevocationList : _socketsHttpHandler.SslOptions.CertificateRevocationCheckMode == X509RevocationMode.Online;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.CheckCertificateRevocationList = value;
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
            get => _curlHandler != null ? _curlHandler.SslProtocols : _socketsHttpHandler.SslOptions.EnabledSslProtocols;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.SslProtocols = value;
                }
                else
                {
                    ThrowForModifiedManagedSslOptionsIfStarted();
                    _socketsHttpHandler.SslOptions.EnabledSslProtocols = value;
                }
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => _curlHandler != null ? _curlHandler.AutomaticDecompression : _socketsHttpHandler.AutomaticDecompression;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.AutomaticDecompression = value;
                }
                else
                {
                    _socketsHttpHandler.AutomaticDecompression = value;
                }
            }
        }

        public bool UseProxy
        {
            get => _curlHandler != null ? _curlHandler.UseProxy : _socketsHttpHandler.UseProxy;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.UseProxy = value;
                }
                else
                {
                    _socketsHttpHandler.UseProxy = value;
                }
            }
        }

        public IWebProxy Proxy
        {
            get => _curlHandler != null ? _curlHandler.Proxy : _socketsHttpHandler.Proxy;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.Proxy = value;
                }
                else
                {
                    _socketsHttpHandler.Proxy = value;
                }
            }
        }

        public ICredentials DefaultProxyCredentials
        {
            get => _curlHandler != null ? _curlHandler.DefaultProxyCredentials : _socketsHttpHandler.DefaultProxyCredentials;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.DefaultProxyCredentials = value;
                }
                else
                {
                    _socketsHttpHandler.DefaultProxyCredentials = value;
                }
            }
        }

        public bool PreAuthenticate
        {
            get => _curlHandler != null ? _curlHandler.PreAuthenticate : _socketsHttpHandler.PreAuthenticate;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.PreAuthenticate = value;
                }
                else
                {
                    _socketsHttpHandler.PreAuthenticate = value;
                }
            }
        }

        public bool UseDefaultCredentials
        {
            // Either read variable from curlHandler or compare .Credentials as socketsHttpHandler does not have separate prop.
            get => _curlHandler != null ? _curlHandler.UseDefaultCredentials : _socketsHttpHandler.Credentials == CredentialCache.DefaultCredentials;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.UseDefaultCredentials = value;
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
            get => _curlHandler != null ? _curlHandler.Credentials : _socketsHttpHandler.Credentials;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.Credentials = value;
                }
                else
                {
                    _socketsHttpHandler.Credentials = value;
                }
            }
        }

        public bool AllowAutoRedirect
        {
            get => _curlHandler != null ? _curlHandler.AllowAutoRedirect : _socketsHttpHandler.AllowAutoRedirect;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.AllowAutoRedirect = value;
                }
                else
                {
                    _socketsHttpHandler.AllowAutoRedirect = value;
                }
            }
        }

        public int MaxAutomaticRedirections
        {
            get => _curlHandler != null ? _curlHandler.MaxAutomaticRedirections : _socketsHttpHandler.MaxAutomaticRedirections;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.MaxAutomaticRedirections = value;
                }
                else
                {
                    _socketsHttpHandler.MaxAutomaticRedirections = value;
                }
            }
        }

        public int MaxConnectionsPerServer
        {
            get => _curlHandler != null ? _curlHandler.MaxConnectionsPerServer : _socketsHttpHandler.MaxConnectionsPerServer;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.MaxConnectionsPerServer = value;
                }
                else
                {
                    _socketsHttpHandler.MaxConnectionsPerServer = value;
                }
            }
        }

        public int MaxResponseHeadersLength
        {
            get => _curlHandler != null ? _curlHandler.MaxResponseHeadersLength : _socketsHttpHandler.MaxResponseHeadersLength;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.MaxResponseHeadersLength = value;
                }
                else
                {
                    _socketsHttpHandler.MaxResponseHeadersLength = value;
                }
            }
        }

        public IDictionary<string, object> Properties => _curlHandler != null ?
            _curlHandler.Properties :
            _socketsHttpHandler.Properties;

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            DiagnosticsHandler.IsEnabled() ? _diagnosticsHandler.SendAsync(request, cancellationToken) :
            _curlHandler != null ? _curlHandler.SendAsync(request, cancellationToken) :
            _socketsHttpHandler.SendAsync(request, cancellationToken);
    }
}
