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
        private readonly HttpConnectionSettings _settings = new HttpConnectionSettings();
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
            get => _settings._useCookies;
            set
            {
                CheckDisposedOrStarted();
                _settings._useCookies = value;
            }
        }

        public CookieContainer CookieContainer
        {
            get => _settings._cookieContainer ?? (_settings._cookieContainer = new CookieContainer());
            set
            {
                CheckDisposedOrStarted();
                _settings._cookieContainer = value;
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get => _settings._clientCertificateOptions;
            set
            {
                if (value != ClientCertificateOption.Manual &&
                    value != ClientCertificateOption.Automatic)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _settings._clientCertificateOptions = value;
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => _settings._automaticDecompression;
            set
            {
                CheckDisposedOrStarted();
                _settings._automaticDecompression = value;
            }
        }

        public bool UseProxy
        {
            get => _settings._useProxy;
            set
            {
                CheckDisposedOrStarted();
                _settings._useProxy = value;
            }
        }

        public IWebProxy Proxy
        {
            get => _settings._proxy;
            set
            {
                CheckDisposedOrStarted();
                _settings._proxy = value;
            }
        }

        public ICredentials DefaultProxyCredentials
        {
            get => _settings._defaultProxyCredentials;
            set
            {
                CheckDisposedOrStarted();
                _settings._defaultProxyCredentials = value;
            }
        }

        public bool PreAuthenticate
        {
            get => _settings._preAuthenticate;
            set
            {
                CheckDisposedOrStarted();
                _settings._preAuthenticate = value;
            }
        }

        public bool UseDefaultCredentials
        {
            get => _settings._useDefaultCredentials;
            set
            {
                CheckDisposedOrStarted();
                _settings._useDefaultCredentials = value;
            }
        }

        public ICredentials Credentials
        {
            get => _settings._credentials;
            set
            {
                CheckDisposedOrStarted();
                _settings._credentials = value;
            }
        }

        public bool AllowAutoRedirect
        {
            get => _settings._allowAutoRedirect;
            set
            {
                CheckDisposedOrStarted();
                _settings._allowAutoRedirect = value;
            }
        }

        public int MaxAutomaticRedirections
        {
            get => _settings._maxAutomaticRedirections;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _settings._maxAutomaticRedirections = value;
            }
        }

        public int MaxConnectionsPerServer
        {
            get => _settings._maxConnectionsPerServer;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _settings._maxConnectionsPerServer = value;
            }
        }

        public int MaxResponseHeadersLength
        {
            get => _settings._maxResponseHeadersLength;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than, 0));
                }

                CheckDisposedOrStarted();
                _settings._maxResponseHeadersLength = value;
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (_settings._clientCertificateOptions != ClientCertificateOption.Manual)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_http_invalid_enable_first, nameof(ClientCertificateOptions), nameof(ClientCertificateOption.Manual)));
                }

                return _settings._clientCertificates ?? (_settings._clientCertificates = new X509Certificate2Collection());
            }
        }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get => _settings._serverCertificateCustomValidationCallback;
            set
            {
                CheckDisposedOrStarted();
                _settings._serverCertificateCustomValidationCallback = value;
            }
        }

        public bool CheckCertificateRevocationList
        {
            get => _settings._checkCertificateRevocationList;
            set
            {
                CheckDisposedOrStarted();
                _settings._checkCertificateRevocationList = value;
            }
        }

        public SslProtocols SslProtocols
        {
            get => _settings._sslProtocols;
            set
            {
                SecurityProtocol.ThrowOnNotAllowed(value, allowNone: true);
                CheckDisposedOrStarted();
                _settings._sslProtocols = value;
            }
        }

        public IDictionary<string, object> Properties =>
            _settings._properties ?? (_settings._properties = new Dictionary<string, object>());

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
            HttpMessageHandler handler = new HttpConnectionHandler(_settings);

            if (_settings._useProxy &&
                (_settings._proxy != null || HttpProxyConnectionHandler.EnvironmentProxyConfigured))
            {
                handler = new HttpProxyConnectionHandler(_settings, handler);
            }

            if (_settings._useCookies)
            {
                handler = new CookieHandler(CookieContainer, handler);
            }

            if (_settings._credentials != null || _settings._allowAutoRedirect)
            {
                handler = new AuthenticateAndRedirectHandler(_settings._preAuthenticate, _settings._credentials, _settings._allowAutoRedirect, _settings._maxAutomaticRedirections, handler);
            }

            if (_settings._automaticDecompression != DecompressionMethods.None)
            {
                handler = new DecompressionHandler(_settings._automaticDecompression, handler);
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

        protected internal override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CheckDisposed();
            HttpMessageHandler handler = _handler ?? SetupHandlerChain();
            return handler.SendAsync(request, cancellationToken);
        }
    }
}
