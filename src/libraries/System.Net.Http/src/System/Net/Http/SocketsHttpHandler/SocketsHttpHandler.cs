// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Diagnostics;

namespace System.Net.Http
{
    [UnsupportedOSPlatform("browser")]
    public sealed class SocketsHttpHandler : HttpMessageHandler
    {
        private readonly HttpConnectionSettings _settings = new HttpConnectionSettings();
        private HttpMessageHandlerStage? _handler;
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

        /// <summary>
        /// Gets a value that indicates whether the handler is supported on the current platform.
        /// </summary>
        [UnsupportedOSPlatformGuard("browser")]
        public static bool IsSupported => !OperatingSystem.IsBrowser();

        public bool UseCookies
        {
            get => _settings._useCookies;
            set
            {
                CheckDisposedOrStarted();
                _settings._useCookies = value;
            }
        }

        [AllowNull]
        public CookieContainer CookieContainer
        {
            get => _settings._cookieContainer ?? (_settings._cookieContainer = new CookieContainer());
            set
            {
                CheckDisposedOrStarted();
                _settings._cookieContainer = value;
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

        public IWebProxy? Proxy
        {
            get => _settings._proxy;
            set
            {
                CheckDisposedOrStarted();
                _settings._proxy = value;
            }
        }

        public ICredentials? DefaultProxyCredentials
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

        public ICredentials? Credentials
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
                if (value <= 0)
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

        public int MaxResponseDrainSize
        {
            get => _settings._maxResponseDrainSize;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_NeedNonNegativeNum);
                }

                CheckDisposedOrStarted();
                _settings._maxResponseDrainSize = value;
            }
        }

        public TimeSpan ResponseDrainTimeout
        {
            get => _settings._maxResponseDrainTime;
            set
            {
                if ((value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) ||
                    (value.TotalMilliseconds > int.MaxValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _settings._maxResponseDrainTime = value;
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

        [AllowNull]
        public SslClientAuthenticationOptions SslOptions
        {
            get => _settings._sslOptions ?? (_settings._sslOptions = new SslClientAuthenticationOptions());
            set
            {
                CheckDisposedOrStarted();
                _settings._sslOptions = value;
            }
        }

        public TimeSpan PooledConnectionLifetime
        {
            get => _settings._pooledConnectionLifetime;
            set
            {
                if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _settings._pooledConnectionLifetime = value;
            }
        }

        public TimeSpan PooledConnectionIdleTimeout
        {
            get => _settings._pooledConnectionIdleTimeout;
            set
            {
                if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _settings._pooledConnectionIdleTimeout = value;
            }
        }

        public TimeSpan ConnectTimeout
        {
            get => _settings._connectTimeout;
            set
            {
                if ((value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) ||
                    (value.TotalMilliseconds > int.MaxValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _settings._connectTimeout = value;
            }
        }

        public TimeSpan Expect100ContinueTimeout
        {
            get => _settings._expect100ContinueTimeout;
            set
            {
                if ((value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) ||
                    (value.TotalMilliseconds > int.MaxValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                CheckDisposedOrStarted();
                _settings._expect100ContinueTimeout = value;
            }
        }

        /// <summary>
        /// Defines the initial HTTP2 stream receive window size for all connections opened by the this <see cref="SocketsHttpHandler"/>.
        /// </summary>
        /// <remarks>
        /// Larger the values may lead to faster download speed, but potentially higher memory footprint.
        /// The property must be set to a value between 65535 and the configured maximum window size, which is 16777216 by default.
        /// </remarks>
        public int InitialHttp2StreamWindowSize
        {
            get => _settings._initialHttp2StreamWindowSize;
            set
            {
                if (value < HttpHandlerDefaults.DefaultInitialHttp2StreamWindowSize || value > GlobalHttpSettings.SocketsHttpHandler.MaxHttp2StreamWindowSize)
                {
                    string message = SR.Format(
                        SR.net_http_http2_invalidinitialstreamwindowsize,
                        HttpHandlerDefaults.DefaultInitialHttp2StreamWindowSize,
                        GlobalHttpSettings.SocketsHttpHandler.MaxHttp2StreamWindowSize);

                    throw new ArgumentOutOfRangeException(nameof(InitialHttp2StreamWindowSize), message);
                }
                CheckDisposedOrStarted();
                _settings._initialHttp2StreamWindowSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the keep alive ping delay. The client will send a keep alive ping to the server if it
        /// doesn't receive any frames on a connection for this period of time. This property is used together with
        /// <see cref="SocketsHttpHandler.KeepAlivePingTimeout"/> to close broken connections.
        /// <para>
        /// Delay value must be greater than or equal to 1 second. Set to <see cref="Timeout.InfiniteTimeSpan"/> to
        /// disable the keep alive ping.
        /// Defaults to <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </para>
        /// </summary>
        public TimeSpan KeepAlivePingDelay
        {
            get => _settings._keepAlivePingDelay;
            set
            {
                if (value.Ticks < TimeSpan.TicksPerSecond && value != Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than_or_equal, value, TimeSpan.FromSeconds(1)));
                }

                CheckDisposedOrStarted();
                _settings._keepAlivePingDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets the keep alive ping timeout. Keep alive pings are sent when a period of inactivity exceeds
        /// the configured <see cref="KeepAlivePingDelay"/> value. The client will close the connection if it
        /// doesn't receive any frames within the timeout.
        /// <para>
        /// Timeout must be greater than or equal to 1 second. Set to <see cref="Timeout.InfiniteTimeSpan"/> to
        /// disable the keep alive ping timeout.
        /// Defaults to 20 seconds.
        /// </para>
        /// </summary>
        public TimeSpan KeepAlivePingTimeout
        {
            get => _settings._keepAlivePingTimeout;
            set
            {
                if (value.Ticks < TimeSpan.TicksPerSecond && value != Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.Format(SR.net_http_value_must_be_greater_than_or_equal, value, TimeSpan.FromSeconds(1)));
                }

                CheckDisposedOrStarted();
                _settings._keepAlivePingTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the keep alive ping behaviour. Keep alive pings are sent when a period of inactivity exceeds
        /// the configured <see cref="KeepAlivePingDelay"/> value.
        /// </summary>
        public HttpKeepAlivePingPolicy KeepAlivePingPolicy
        {
            get => _settings._keepAlivePingPolicy;
            set
            {
                CheckDisposedOrStarted();
                _settings._keepAlivePingPolicy = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether additional HTTP/2 connections can be established to the same server
        /// when the maximum of concurrent streams is reached on all existing connections.
        /// </summary>
        public bool EnableMultipleHttp2Connections
        {
            get => _settings._enableMultipleHttp2Connections;
            set
            {
                CheckDisposedOrStarted();

                _settings._enableMultipleHttp2Connections = value;
            }
        }

        internal const bool SupportsAutomaticDecompression = true;
        internal const bool SupportsProxy = true;
        internal const bool SupportsRedirectConfiguration = true;

        /// <summary>
        /// When non-null, a custom callback used to open new connections.
        /// </summary>
        public Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>? ConnectCallback
        {
            get => _settings._connectCallback;
            set
            {
                CheckDisposedOrStarted();
                _settings._connectCallback = value;
            }
        }

        /// <summary>
        /// Gets or sets a custom callback that provides access to the plaintext HTTP protocol stream.
        /// </summary>
        public Func<SocketsHttpPlaintextStreamFilterContext, CancellationToken, ValueTask<Stream>>? PlaintextStreamFilter
        {
            get => _settings._plaintextStreamFilter;
            set
            {
                CheckDisposedOrStarted();
                _settings._plaintextStreamFilter = value;
            }
        }

        public IDictionary<string, object?> Properties =>
            _settings._properties ?? (_settings._properties = new Dictionary<string, object?>());

        /// <summary>
        /// Gets or sets a callback that returns the <see cref="Encoding"/> to encode the value for the specified request header name,
        /// or <see langword="null"/> to use the default behavior.
        /// </summary>
        public HeaderEncodingSelector<HttpRequestMessage>? RequestHeaderEncodingSelector
        {
            get => _settings._requestHeaderEncodingSelector;
            set
            {
                CheckDisposedOrStarted();
                _settings._requestHeaderEncodingSelector = value;
            }
        }

        /// <summary>
        /// Gets or sets a callback that returns the <see cref="Encoding"/> to decode the value for the specified response header name,
        /// or <see langword="null"/> to use the default behavior.
        /// </summary>
        public HeaderEncodingSelector<HttpRequestMessage>? ResponseHeaderEncodingSelector
        {
            get => _settings._responseHeaderEncodingSelector;
            set
            {
                CheckDisposedOrStarted();
                _settings._responseHeaderEncodingSelector = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="DistributedContextPropagator"/> to use when propagating the distributed trace and context.
        /// Use <see langword="null"/> to disable propagation.
        /// Defaults to <see cref="DistributedContextPropagator.Current"/>.
        /// </summary>
        [CLSCompliant(false)]
        public DistributedContextPropagator? ActivityHeadersPropagator
        {
            get => _settings._activityHeadersPropagator;
            set
            {
                CheckDisposedOrStarted();
                _settings._activityHeadersPropagator = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _handler?.Dispose();
            }

            base.Dispose(disposing);
        }

        private HttpMessageHandlerStage SetupHandlerChain()
        {
            // Clone the settings to get a relatively consistent view that won't change after this point.
            // (This isn't entirely complete, as some of the collections it contains aren't currently deeply cloned.)
            HttpConnectionSettings settings = _settings.CloneAndNormalize();

            HttpConnectionPoolManager poolManager = new HttpConnectionPoolManager(settings);

            HttpMessageHandlerStage handler;

            if (settings._credentials == null)
            {
                handler = new HttpConnectionHandler(poolManager);
            }
            else
            {
                handler = new HttpAuthenticatedConnectionHandler(poolManager);
            }

            // DiagnosticsHandler is inserted before RedirectHandler so that trace propagation is done on redirects as well
            if (DiagnosticsHandler.IsGloballyEnabled() && settings._activityHeadersPropagator is DistributedContextPropagator propagator)
            {
                handler = new DiagnosticsHandler(handler, propagator, settings._allowAutoRedirect);
            }

            if (settings._allowAutoRedirect)
            {
                // Just as with WinHttpHandler, for security reasons, we do not support authentication on redirects
                // if the credential is anything other than a CredentialCache.
                // We allow credentials in a CredentialCache since they are specifically tied to URIs.
                HttpMessageHandlerStage redirectHandler =
                    (settings._credentials == null || settings._credentials is CredentialCache) ?
                    handler :
                    new HttpConnectionHandler(poolManager);        // will not authenticate

                handler = new RedirectHandler(settings._maxAutomaticRedirections, handler, redirectHandler);
            }

            if (settings._automaticDecompression != DecompressionMethods.None)
            {
                handler = new DecompressionHandler(settings._automaticDecompression, handler);
            }

            // Ensure a single handler is used for all requests.
            if (Interlocked.CompareExchange(ref _handler, handler, null) != null)
            {
                handler.Dispose();
            }

            return _handler;
        }

        protected internal override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            if (request.Version.Major >= 2)
            {
                throw new NotSupportedException(SR.Format(SR.net_http_http2_sync_not_supported, GetType()));
            }

            // Do not allow upgrades for synchronous requests, that might lead to asynchronous code-paths.
            if (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
            {
                throw new NotSupportedException(SR.Format(SR.net_http_upgrade_not_enabled_sync, nameof(Send), request.VersionPolicy));
            }

            CheckDisposed();

            cancellationToken.ThrowIfCancellationRequested();

            HttpMessageHandlerStage handler = _handler ?? SetupHandlerChain();

            Exception? error = ValidateAndNormalizeRequest(request);
            if (error != null)
            {
                throw error;
            }

            return handler.Send(request, cancellationToken);
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            CheckDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

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

            Uri? requestUri = request.RequestUri;
            if (requestUri is null || !requestUri.IsAbsoluteUri)
            {
                return new InvalidOperationException(SR.net_http_client_invalid_requesturi);
            }

            if (!HttpUtilities.IsSupportedScheme(requestUri.Scheme))
            {
                return new NotSupportedException(SR.Format(SR.net_http_unsupported_requesturi_scheme, requestUri.Scheme));
            }

            return null;
        }
    }
}
