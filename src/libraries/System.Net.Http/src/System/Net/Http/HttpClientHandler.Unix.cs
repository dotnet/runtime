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
        private readonly ManagedHandler _managedHandler;
        private readonly DiagnosticsHandler _diagnosticsHandler;

        public HttpClientHandler()
        {
            if (UseManagedHandler)
            {
                _managedHandler = new ManagedHandler();
                _diagnosticsHandler = new DiagnosticsHandler(_managedHandler);
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
                ((HttpMessageHandler)_curlHandler ?? _managedHandler).Dispose();
            }
            base.Dispose(disposing);
        }

        public virtual bool SupportsAutomaticDecompression => _curlHandler != null ?
            _curlHandler.SupportsAutomaticDecompression :
            _managedHandler.SupportsAutomaticDecompression;

        public virtual bool SupportsProxy => _curlHandler != null ?
            _curlHandler.SupportsProxy :
            _managedHandler.SupportsProxy;

        public virtual bool SupportsRedirectConfiguration => _curlHandler != null ?
            _curlHandler.SupportsRedirectConfiguration :
            _managedHandler.SupportsRedirectConfiguration;

        public bool UseCookies
        {
            get => _curlHandler != null ? _curlHandler.UseCookies : _managedHandler.UseCookies;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.UseCookies = value;
                }
                else
                {
                    _managedHandler.UseCookies = value;
                }
            }
        }

        public CookieContainer CookieContainer
        {
            get => _curlHandler != null ? _curlHandler.CookieContainer : _managedHandler.CookieContainer;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.CookieContainer = value;
                }
                else
                {
                    _managedHandler.CookieContainer = value;
                }
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get => _curlHandler != null ? _curlHandler.ClientCertificateOptions : _managedHandler.ClientCertificateOptions;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.ClientCertificateOptions = value;
                }
                else
                {
                    _managedHandler.ClientCertificateOptions = value;
                }
            }
        }

        public X509CertificateCollection ClientCertificates => _curlHandler != null ?
            _curlHandler.ClientCertificates :
            _managedHandler.ClientCertificates;

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
        {
            get => _curlHandler != null ? _curlHandler.ServerCertificateCustomValidationCallback : _managedHandler.ServerCertificateCustomValidationCallback;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.ServerCertificateCustomValidationCallback = value;
                }
                else
                {
                    _managedHandler.ServerCertificateCustomValidationCallback = value;
                }
            }
        }

        public bool CheckCertificateRevocationList
        {
            get => _curlHandler != null ? _curlHandler.CheckCertificateRevocationList : _managedHandler.CheckCertificateRevocationList;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.CheckCertificateRevocationList = value;
                }
                else
                {
                    _managedHandler.CheckCertificateRevocationList = value;
                }
            }
        }

        public SslProtocols SslProtocols
        {
            get => _curlHandler != null ? _curlHandler.SslProtocols : _managedHandler.SslProtocols;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.SslProtocols = value;
                }
                else
                {
                    _managedHandler.SslProtocols = value;
                }
            }
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => _curlHandler != null ? _curlHandler.AutomaticDecompression : _managedHandler.AutomaticDecompression;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.AutomaticDecompression = value;
                }
                else
                {
                    _managedHandler.AutomaticDecompression = value;
                }
            }
        }

        public bool UseProxy
        {
            get => _curlHandler != null ? _curlHandler.UseProxy : _managedHandler.UseProxy;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.UseProxy = value;
                }
                else
                {
                    _managedHandler.UseProxy = value;
                }
            }
        }

        public IWebProxy Proxy
        {
            get => _curlHandler != null ? _curlHandler.Proxy : _managedHandler.Proxy;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.Proxy = value;
                }
                else
                {
                    _managedHandler.Proxy = value;
                }
            }
        }

        public ICredentials DefaultProxyCredentials
        {
            get => _curlHandler != null ? _curlHandler.DefaultProxyCredentials : _managedHandler.DefaultProxyCredentials;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.DefaultProxyCredentials = value;
                }
                else
                {
                    _managedHandler.DefaultProxyCredentials = value;
                }
            }
        }

        public bool PreAuthenticate
        {
            get => _curlHandler != null ? _curlHandler.PreAuthenticate : _managedHandler.PreAuthenticate;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.PreAuthenticate = value;
                }
                else
                {
                    _managedHandler.PreAuthenticate = value;
                }
            }
        }

        public bool UseDefaultCredentials
        {
            get => _curlHandler != null ? _curlHandler.UseDefaultCredentials : _managedHandler.UseDefaultCredentials;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.UseDefaultCredentials = value;
                }
                else
                {
                    _managedHandler.UseDefaultCredentials = value;
                }
            }
        }

        public ICredentials Credentials
        {
            get => _curlHandler != null ? _curlHandler.Credentials : _managedHandler.Credentials;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.Credentials = value;
                }
                else
                {
                    _managedHandler.Credentials = value;
                }
            }
        }

        public bool AllowAutoRedirect
        {
            get => _curlHandler != null ? _curlHandler.AllowAutoRedirect : _managedHandler.AllowAutoRedirect;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.AllowAutoRedirect = value;
                }
                else
                {
                    _managedHandler.AllowAutoRedirect = value;
                }
            }
        }

        public int MaxAutomaticRedirections
        {
            get => _curlHandler != null ? _curlHandler.MaxAutomaticRedirections : _managedHandler.MaxAutomaticRedirections;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.MaxAutomaticRedirections = value;
                }
                else
                {
                    _managedHandler.MaxAutomaticRedirections = value;
                }
            }
        }

        public int MaxConnectionsPerServer
        {
            get => _curlHandler != null ? _curlHandler.MaxConnectionsPerServer : _managedHandler.MaxConnectionsPerServer;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.MaxConnectionsPerServer = value;
                }
                else
                {
                    _managedHandler.MaxConnectionsPerServer = value;
                }
            }
        }

        public int MaxResponseHeadersLength
        {
            get => _curlHandler != null ? _curlHandler.MaxResponseHeadersLength : _managedHandler.MaxResponseHeadersLength;
            set
            {
                if (_curlHandler != null)
                {
                    _curlHandler.MaxResponseHeadersLength = value;
                }
                else
                {
                    _managedHandler.MaxResponseHeadersLength = value;
                }
            }
        }

        public IDictionary<String, object> Properties => _curlHandler != null ?
            _curlHandler.Properties :
            _managedHandler.Properties;

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            DiagnosticsHandler.IsEnabled() ? _diagnosticsHandler.SendAsync(request, cancellationToken) :
            _curlHandler != null ? _curlHandler.SendAsync(request, cancellationToken) :
            _managedHandler.SendAsync(request, cancellationToken);
    }
}
