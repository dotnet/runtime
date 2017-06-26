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
    internal sealed class ManagedHandler : HttpMessageHandler
    {
        // Configuration settings
        private bool _useCookies = HttpHandlerDefaults.DefaultUseCookies;
        private CookieContainer _cookieContainer;
        private ClientCertificateOption _clientCertificateOptions = HttpHandlerDefaults.DefaultClientCertificateOption;
        private DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;
        private bool _useProxy = HttpHandlerDefaults.DefaultUseProxy;
        private IWebProxy _proxy;
        private ICredentials _defaultProxyCredentials;
        private bool _preAuthenticate = HttpHandlerDefaults.DefaultPreAuthenticate;
        private bool _useDefaultCredentials = HttpHandlerDefaults.DefaultUseDefaultCredentials;
        private ICredentials _credentials;
        private bool _allowAutoRedirect = HttpHandlerDefaults.DefaultAutomaticRedirection;
        private int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;
        private int _maxResponseHeadersLength = HttpHandlerDefaults.DefaultMaxResponseHeadersLength;
        private int _maxConnectionsPerServer = HttpHandlerDefaults.DefaultMaxConnectionsPerServer;
        private X509CertificateCollection _clientCertificates;
        private Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _serverCertificateCustomValidationCallback;
        private bool _checkCertificateRevocationList = false;
        private SslProtocols _sslProtocols = SslProtocols.None;
        private IDictionary<string, object> _properties;

        private HttpMessageHandler _handler;
        private bool _disposed;

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ManagedHandler));
            }
        }

        private void CheckDisposedOrStarted()
        {
            CheckDisposed();
            if (_handler != null)
            {
                throw new InvalidOperationException(SR.net_http_operation_started);
            }
        }

        public bool SupportsAutomaticDecompression => true;

        public bool SupportsProxy => true;

        public bool SupportsRedirectConfiguration => true;

        public bool UseCookies
        {
            get => _useCookies;
            set
            {
                CheckDisposedOrStarted();
                _useCookies = value;
            }
        }

        public CookieContainer CookieContainer
        {
            get => _cookieContainer ?? (_cookieContainer = new CookieContainer());
            set
            {
                CheckDisposedOrStarted();
                _cookieContainer = value;
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get => _clientCertificateOptions;
            set
            {
                if (value != ClientCertificateOption.Manual &&
                    value != ClientCertificateOption.Automatic)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _clientCertificateOptions = value;
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => _automaticDecompression;
            set
            {
                CheckDisposedOrStarted();
                _automaticDecompression = value;
            }
        }

        public bool UseProxy
        {
            get => _useProxy;
            set
            {
                CheckDisposedOrStarted();
                _useProxy = value;
            }
        }

        public IWebProxy Proxy
        {
            get => _proxy;
            set
            {
                CheckDisposedOrStarted();
                _proxy = value;
            }
        }

        public ICredentials DefaultProxyCredentials
        {
            get => _defaultProxyCredentials;
            set
            {
                CheckDisposedOrStarted();
                _defaultProxyCredentials = value;
            }
        }

        public bool PreAuthenticate
        {
            get => _preAuthenticate;
            set
            {
                CheckDisposedOrStarted();
                _preAuthenticate = value;
            }
        }

        public bool UseDefaultCredentials
        {
            get => _useDefaultCredentials;
            set
            {
                CheckDisposedOrStarted();
                _useDefaultCredentials = value;
            }
        }

        public ICredentials Credentials
        {
            get => _credentials;
            set
            {
                CheckDisposedOrStarted();
                _credentials = value;
            }
        }

        public bool AllowAutoRedirect
        {
            get => _allowAutoRedirect;
            set
            {
                CheckDisposedOrStarted();
                _allowAutoRedirect = value;
            }
        }

        public int MaxAutomaticRedirections
        {
            get => _maxAutomaticRedirections;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxAutomaticRedirections = value;
            }
        }

        public int MaxConnectionsPerServer
        {
            get => _maxConnectionsPerServer;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxConnectionsPerServer = value;
            }
        }

        public int MaxResponseHeadersLength
        {
            get => _maxResponseHeadersLength;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _maxResponseHeadersLength = value;
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (_clientCertificateOptions != ClientCertificateOption.Manual)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_http_invalid_enable_first, nameof(ClientCertificateOptions), nameof(ClientCertificateOption.Manual)));
                }

                return _clientCertificates ?? (_clientCertificates = new X509Certificate2Collection());
            }
        }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get => _serverCertificateCustomValidationCallback;
            set
            {
                CheckDisposedOrStarted();
                _serverCertificateCustomValidationCallback = value;
            }
        }

        public bool CheckCertificateRevocationList
        {
            get => _checkCertificateRevocationList;
            set
            {
                CheckDisposedOrStarted();
                _checkCertificateRevocationList = value;
            }
        }

        public SslProtocols SslProtocols
        {
            get => _sslProtocols;
            set
            {
                SecurityProtocol.ThrowOnNotAllowed(value, allowNone: true);
                CheckDisposedOrStarted();
                _sslProtocols = value;
            }
        }

        public IDictionary<string, object> Properties =>
            _properties ?? (_properties = new Dictionary<string, object>());

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _handler?.Dispose();
            }

            base.Dispose(disposing);
        }

        private HttpMessageHandler SetupHandlerChain()
        {
            HttpMessageHandler handler = new HttpConnectionHandler(
                _clientCertificates,
                _serverCertificateCustomValidationCallback,
                _checkCertificateRevocationList,
                _sslProtocols);

            if (_useProxy && _proxy != null)
            {
                handler = new HttpProxyConnectionHandler(_proxy, handler);
            }

            if (_credentials != null)
            {
                handler = new AuthenticationHandler(_preAuthenticate, _credentials, handler);
            }

            if (_useCookies)
            {
                handler = new CookieHandler(CookieContainer, handler);
            }

            if (_allowAutoRedirect)
            {
                handler = new AutoRedirectHandler(_maxAutomaticRedirections, handler);
            }

            if (_automaticDecompression != DecompressionMethods.None)
            {
                handler = new DecompressionHandler(_automaticDecompression, handler);
            }

            if (Interlocked.CompareExchange(ref _handler, handler, null) == null)
            {
                return handler;
            }
            else
            {
                handler.Dispose();
                return _handler;
            }
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CheckDisposed();
            HttpMessageHandler handler = _handler ?? SetupHandlerChain();
            return handler.SendAsync(request, cancellationToken);
        }
    }
}
