// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private readonly SocketsHttpHandler _socketsHttpHandler;
        private readonly DiagnosticsHandler _diagnosticsHandler;
        private ClientCertificateOption _clientCertificateOptions;

        public HttpClientHandler()
        {
            _socketsHttpHandler = new SocketsHttpHandler();
            _diagnosticsHandler = new DiagnosticsHandler(_socketsHttpHandler);
            ClientCertificateOptions = ClientCertificateOption.Manual;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _socketsHttpHandler.Dispose();
            }

            base.Dispose(disposing);
        }

        public virtual bool SupportsAutomaticDecompression => true;
        public virtual bool SupportsProxy => true;
        public virtual bool SupportsRedirectConfiguration => true;

        public bool UseCookies
        {
            get => _socketsHttpHandler.UseCookies;
            set => _socketsHttpHandler.UseCookies = value;
        }

        public CookieContainer CookieContainer
        {
            get => _socketsHttpHandler.CookieContainer;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _socketsHttpHandler.CookieContainer = value;
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => _socketsHttpHandler.AutomaticDecompression;
            set => _socketsHttpHandler.AutomaticDecompression = value;
        }

        public bool UseProxy
        {
            get => _socketsHttpHandler.UseProxy;
            set => _socketsHttpHandler.UseProxy = value;
        }

        public IWebProxy? Proxy
        {
            get => _socketsHttpHandler.Proxy;
            set => _socketsHttpHandler.Proxy = value;
        }

        public ICredentials? DefaultProxyCredentials
        {
            get => _socketsHttpHandler.DefaultProxyCredentials;
            set => _socketsHttpHandler.DefaultProxyCredentials = value;
        }

        public bool PreAuthenticate
        {
            get => _socketsHttpHandler.PreAuthenticate;
            set => _socketsHttpHandler.PreAuthenticate = value;
        }

        public bool UseDefaultCredentials
        {
            // SocketsHttpHandler doesn't have a separate UseDefaultCredentials property.  There
            // is just a Credentials property.  So, we need to map the behavior.
            get => _socketsHttpHandler.Credentials == CredentialCache.DefaultCredentials;
            set
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

        public ICredentials? Credentials
        {
            get => _socketsHttpHandler.Credentials;
            set => _socketsHttpHandler.Credentials = value;
        }

        public bool AllowAutoRedirect
        {
            get => _socketsHttpHandler.AllowAutoRedirect;
            set => _socketsHttpHandler.AllowAutoRedirect = value;
        }

        public int MaxAutomaticRedirections
        {
            get => _socketsHttpHandler.MaxAutomaticRedirections;
            set => _socketsHttpHandler.MaxAutomaticRedirections = value;
        }

        public int MaxConnectionsPerServer
        {
            get => _socketsHttpHandler.MaxConnectionsPerServer;
            set => _socketsHttpHandler.MaxConnectionsPerServer = value;
        }

        public int MaxResponseHeadersLength
        {
            get => _socketsHttpHandler.MaxResponseHeadersLength;
            set => _socketsHttpHandler.MaxResponseHeadersLength = value;
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get => _clientCertificateOptions;
            set
            {
                switch (value)
                {
                    case ClientCertificateOption.Manual:
                        ThrowForModifiedManagedSslOptionsIfStarted();
                        _clientCertificateOptions = value;
                        _socketsHttpHandler.SslOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => CertificateHelper.GetEligibleClientCertificate(ClientCertificates)!;
                        break;

                    case ClientCertificateOption.Automatic:
                        ThrowForModifiedManagedSslOptionsIfStarted();
                        _clientCertificateOptions = value;
                        _socketsHttpHandler.SslOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => CertificateHelper.GetEligibleClientCertificate()!;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (ClientCertificateOptions != ClientCertificateOption.Manual)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_http_invalid_enable_first, nameof(ClientCertificateOptions), nameof(ClientCertificateOption.Manual)));
                }

                return _socketsHttpHandler.SslOptions.ClientCertificates ??
                    (_socketsHttpHandler.SslOptions.ClientCertificates = new X509CertificateCollection());
            }
        }

        public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback
        {
            get => (_socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback?.Target as ConnectHelper.CertificateCallbackMapper)?.FromHttpClientHandler;
            set
            {
                ThrowForModifiedManagedSslOptionsIfStarted();
                _socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = value != null ?
                    new ConnectHelper.CertificateCallbackMapper(value).ForSocketsHttpHandler :
                    null;
            }
        }

        public bool CheckCertificateRevocationList
        {
            get => _socketsHttpHandler.SslOptions.CertificateRevocationCheckMode == X509RevocationMode.Online;
            set
            {
                ThrowForModifiedManagedSslOptionsIfStarted();
                _socketsHttpHandler.SslOptions.CertificateRevocationCheckMode = value ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
            }
        }

        public SslProtocols SslProtocols
        {
            get => _socketsHttpHandler.SslOptions.EnabledSslProtocols;
            set
            {
                ThrowForModifiedManagedSslOptionsIfStarted();
                _socketsHttpHandler.SslOptions.EnabledSslProtocols = value;
            }
        }

        public IDictionary<string, object?> Properties => _socketsHttpHandler.Properties;

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return DiagnosticsHandler.IsEnabled() ?
                _diagnosticsHandler.SendAsync(request, cancellationToken) :
                _socketsHttpHandler.SendAsync(request, cancellationToken);
        }

        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator { get; } = delegate { return true; };

        private void ThrowForModifiedManagedSslOptionsIfStarted()
        {
            // Hack to trigger an InvalidOperationException if a property that's stored on
            // SslOptions is changed, since SslOptions itself does not do any such checks.
            _socketsHttpHandler.SslOptions = _socketsHttpHandler.SslOptions;
        }
    }
}
