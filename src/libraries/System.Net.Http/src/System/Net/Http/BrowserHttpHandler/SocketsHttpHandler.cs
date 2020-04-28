// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public sealed class SocketsHttpHandler : HttpMessageHandler
    {
        private HttpMessageHandler? _handler;
        private bool _disposed;

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SocketsHttpHandler));
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

        public bool UseCookies
        {
            get => throw new PlatformNotSupportedException("Property UseCookies is not supported.");
            set => throw new PlatformNotSupportedException("Property UseCookies is not supported.");
        }

        [AllowNull]
        public CookieContainer CookieContainer
        {
            get => throw new PlatformNotSupportedException("Property CookieContainer is not supported.");
            set => throw new PlatformNotSupportedException("Property CookieContainer is not supported.");
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => throw new PlatformNotSupportedException("Property AutomaticDecompression is not supported.");
            set => throw new PlatformNotSupportedException("Property AutomaticDecompression is not supported.");
        }

        public bool UseProxy
        {
            get => throw new PlatformNotSupportedException("Property UseProxy is not supported.");
            set => throw new PlatformNotSupportedException("Property UseProxy is not supported.");
        }

        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException("Property Proxy is not supported.");
            set => throw new PlatformNotSupportedException("Property Proxy is not supported.");
        }

        public ICredentials? DefaultProxyCredentials
        {
            get => throw new PlatformNotSupportedException("Property Credentials is not supported.");
            set => throw new PlatformNotSupportedException("Property Credentials is not supported.");
        }

        public bool PreAuthenticate
        {
            get => throw new PlatformNotSupportedException("Property PreAuthenticate is not supported.");
            set => throw new PlatformNotSupportedException("Property PreAuthenticate is not supported.");
        }

        public ICredentials? Credentials
        {
            get => throw new PlatformNotSupportedException("Property Credentials is not supported.");
            set => throw new PlatformNotSupportedException("Property Credentials is not supported.");
        }

        public bool AllowAutoRedirect
        {
            get => throw new PlatformNotSupportedException("Property AllowAutoRedirect is not supported.");
            set => throw new PlatformNotSupportedException("Property AllowAutoRedirect is not supported.");
        }

        public int MaxAutomaticRedirections
        {
            get => throw new PlatformNotSupportedException("Property MaxAutomaticRedirections is not supported.");
            set => throw new PlatformNotSupportedException("Property MaxAutomaticRedirections is not supported.");
        }

        public int MaxConnectionsPerServer
        {
            get => throw new PlatformNotSupportedException("Property MaxConnectionsPerServer is not supported.");
            set => throw new PlatformNotSupportedException("Property MaxConnectionsPerServer is not supported.");
        }

        public int MaxResponseDrainSize
        {
            get => throw new PlatformNotSupportedException("Property MaxResponseDrainSize is not supported.");
            set => throw new PlatformNotSupportedException("Property MaxResponseDrainSize is not supported.");
        }

        public TimeSpan ResponseDrainTimeout
        {
            get => throw new PlatformNotSupportedException("Property ResponseDrainTimeout is not supported.");
            set => throw new PlatformNotSupportedException("Property ResponseDrainTimeout is not supported.");
        }

        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException("Property MaxResponseHeadersLength is not supported.");
            set => throw new PlatformNotSupportedException("Property MaxResponseHeadersLength is not supported.");
        }

        [AllowNull]
        public SslClientAuthenticationOptions SslOptions
        {
            get => throw new PlatformNotSupportedException("Property SslOptions is not supported.");
            set => throw new PlatformNotSupportedException("Property SslOptions is not supported.");
        }

        public TimeSpan PooledConnectionLifetime
        {
            get => throw new PlatformNotSupportedException("Property PooledConnectionLifetime is not supported.");
            set => throw new PlatformNotSupportedException("Property PooledConnectionLifetime is not supported.");
        }

        public TimeSpan PooledConnectionIdleTimeout
        {
            get => throw new PlatformNotSupportedException("Property PooledConnectionLifetime is not supported.");
            set => throw new PlatformNotSupportedException("Property PooledConnectionLifetime is not supported.");
        }

        public TimeSpan ConnectTimeout
        {
            get => throw new PlatformNotSupportedException("Property ConnectTimeout is not supported.");
            set => throw new PlatformNotSupportedException("Property ConnectTimeout is not supported.");
        }

        public TimeSpan Expect100ContinueTimeout
        {
            get => throw new PlatformNotSupportedException("Property Expect100ContinueTimeout is not supported.");
            set => throw new PlatformNotSupportedException("Property Expect100ContinueTimeout is not supported.");
        }

        public IDictionary<string, object?> Properties => throw new PlatformNotSupportedException("Property Properties is not supported.");

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _handler?.Dispose();
                _handler = null;
            }

            base.Dispose(disposing);
        }

        private HttpMessageHandler SetupHandlerChain()
        {

            HttpMessageHandler handler = new BrowserHttpMessageHandler();

            // Ensure a single handler is used for all requests.
            if (Interlocked.CompareExchange(ref _handler, handler, null) != null)
            {
                handler.Dispose();
            }

            return _handler;
        }

        protected internal override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CheckDisposed();
            HttpMessageHandler handler = _handler ?? SetupHandlerChain();

            Exception? error = ValidateAndNormalizeRequest(request);
            if (error != null)
            {
                return Task.FromException<HttpResponseMessage>(error);
            }

            return handler.SendAsync(request, cancellationToken);
        }

        private Exception? ValidateAndNormalizeRequest(HttpRequestMessage request)
        {
            if (request.Version.Major == 0)
            {
                return new NotSupportedException(SR.net_http_unsupported_version);
            }

            // Add headers to define content transfer, if not present
            if (request.HasHeaders && request.Headers.TransferEncodingChunked.GetValueOrDefault())
            {
                if (request.Content == null)
                {
                    return new HttpRequestException(SR.net_http_client_execution_error,
                        new InvalidOperationException(SR.net_http_chunked_not_allowed_with_empty_content));
                }

                // Since the user explicitly set TransferEncodingChunked to true, we need to remove
                // the Content-Length header if present, as sending both is invalid.
                request.Content.Headers.ContentLength = null;
            }
            else if (request.Content != null && request.Content.Headers.ContentLength == null)
            {
                // We have content, but neither Transfer-Encoding nor Content-Length is set.
                request.Headers.TransferEncodingChunked = true;
            }

            if (request.Version.Minor == 0 && request.Version.Major == 1 && request.HasHeaders)
            {
                // HTTP 1.0 does not support chunking
                if (request.Headers.TransferEncodingChunked == true)
                {
                    return new NotSupportedException(SR.net_http_unsupported_chunking);
                }

                // HTTP 1.0 does not support Expect: 100-continue; just disable it.
                if (request.Headers.ExpectContinue == true)
                {
                    request.Headers.ExpectContinue = false;
                }
            }

            return null;
        }
    }
}
