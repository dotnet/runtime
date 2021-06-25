// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.IO;
using System.Net.Quic;
using System.Net.Quic.Implementations;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides a state bag of settings for configuring HTTP connections.</summary>
    internal sealed class HttpConnectionSettings
    {
        internal DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;

        internal bool _useCookies = HttpHandlerDefaults.DefaultUseCookies;
        internal CookieContainer? _cookieContainer;

        internal bool _useProxy = HttpHandlerDefaults.DefaultUseProxy;
        internal IWebProxy? _proxy;
        internal ICredentials? _defaultProxyCredentials;
        internal bool _defaultCredentialsUsedForProxy;
        internal bool _defaultCredentialsUsedForServer;

        internal bool _preAuthenticate = HttpHandlerDefaults.DefaultPreAuthenticate;
        internal ICredentials? _credentials;

        internal bool _allowAutoRedirect = HttpHandlerDefaults.DefaultAutomaticRedirection;
        internal int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;

        internal int _maxConnectionsPerServer = HttpHandlerDefaults.DefaultMaxConnectionsPerServer;
        internal int _maxResponseDrainSize = HttpHandlerDefaults.DefaultMaxResponseDrainSize;
        internal TimeSpan _maxResponseDrainTime = HttpHandlerDefaults.DefaultResponseDrainTimeout;
        internal int _maxResponseHeadersLength = HttpHandlerDefaults.DefaultMaxResponseHeadersLength;

        internal TimeSpan _pooledConnectionLifetime = HttpHandlerDefaults.DefaultPooledConnectionLifetime;
        internal TimeSpan _pooledConnectionIdleTimeout = HttpHandlerDefaults.DefaultPooledConnectionIdleTimeout;
        internal TimeSpan _expect100ContinueTimeout = HttpHandlerDefaults.DefaultExpect100ContinueTimeout;
        internal TimeSpan _keepAlivePingTimeout = HttpHandlerDefaults.DefaultKeepAlivePingTimeout;
        internal TimeSpan _keepAlivePingDelay = HttpHandlerDefaults.DefaultKeepAlivePingDelay;
        internal HttpKeepAlivePingPolicy _keepAlivePingPolicy = HttpHandlerDefaults.DefaultKeepAlivePingPolicy;
        internal TimeSpan _connectTimeout = HttpHandlerDefaults.DefaultConnectTimeout;

        internal HeaderEncodingSelector<HttpRequestMessage>? _requestHeaderEncodingSelector;
        internal HeaderEncodingSelector<HttpRequestMessage>? _responseHeaderEncodingSelector;

        internal Version _maxHttpVersion;

        internal SslClientAuthenticationOptions? _sslOptions;

        internal bool _enableMultipleHttp2Connections;

        internal Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>? _connectCallback;
        internal Func<SocketsHttpPlaintextStreamFilterContext, CancellationToken, ValueTask<Stream>>? _plaintextStreamFilter;

        // !!! NOTE !!! This is temporary and will not ship.
        internal QuicImplementationProvider? _quicImplementationProvider;

        internal IDictionary<string, object?>? _properties;

        // Http2 flow control settings:
        internal bool _disableDynamicHttp2WindowSizing = DisableDynamicHttp2WindowSizing;
        internal int _maxHttp2StreamWindowSize = MaxHttp2StreamWindowSize;
        internal double _http2StreamWindowScaleThresholdMultiplier = Http2StreamWindowScaleThresholdMultiplier;
        internal int _initialHttp2StreamWindowSize = Http2Connection.DefaultInitialWindowSize;

        public HttpConnectionSettings()
        {
            bool allowHttp2 = AllowHttp2;
            _maxHttpVersion =
                AllowDraftHttp3 && allowHttp2 ? HttpVersion.Version30 :
                allowHttp2 ? HttpVersion.Version20 :
                HttpVersion.Version11;
            _defaultCredentialsUsedForProxy = _proxy != null && (_proxy.Credentials == CredentialCache.DefaultCredentials || _defaultProxyCredentials == CredentialCache.DefaultCredentials);
            _defaultCredentialsUsedForServer = _credentials == CredentialCache.DefaultCredentials;
        }

        /// <summary>Creates a copy of the settings but with some values normalized to suit the implementation.</summary>
        public HttpConnectionSettings CloneAndNormalize()
        {
            // Force creation of the cookie container if needed, so the original and clone share the same instance.
            if (_useCookies && _cookieContainer == null)
            {
                _cookieContainer = new CookieContainer();
            }

            var settings = new HttpConnectionSettings()
            {
                _allowAutoRedirect = _allowAutoRedirect,
                _automaticDecompression = _automaticDecompression,
                _cookieContainer = _cookieContainer,
                _connectTimeout = _connectTimeout,
                _credentials = _credentials,
                _defaultProxyCredentials = _defaultProxyCredentials,
                _defaultCredentialsUsedForProxy = _defaultCredentialsUsedForProxy,
                _defaultCredentialsUsedForServer = _defaultCredentialsUsedForServer,
                _expect100ContinueTimeout = _expect100ContinueTimeout,
                _maxAutomaticRedirections = _maxAutomaticRedirections,
                _maxConnectionsPerServer = _maxConnectionsPerServer,
                _maxHttpVersion = _maxHttpVersion,
                _maxResponseDrainSize = _maxResponseDrainSize,
                _maxResponseDrainTime = _maxResponseDrainTime,
                _maxResponseHeadersLength = _maxResponseHeadersLength,
                _pooledConnectionLifetime = _pooledConnectionLifetime,
                _pooledConnectionIdleTimeout = _pooledConnectionIdleTimeout,
                _preAuthenticate = _preAuthenticate,
                _properties = _properties,
                _proxy = _proxy,
                _sslOptions = _sslOptions?.ShallowClone(), // shallow clone the options for basic prevention of mutation issues while processing
                _useCookies = _useCookies,
                _useProxy = _useProxy,
                _keepAlivePingTimeout = _keepAlivePingTimeout,
                _keepAlivePingDelay = _keepAlivePingDelay,
                _keepAlivePingPolicy = _keepAlivePingPolicy,
                _requestHeaderEncodingSelector = _requestHeaderEncodingSelector,
                _responseHeaderEncodingSelector = _responseHeaderEncodingSelector,
                _enableMultipleHttp2Connections = _enableMultipleHttp2Connections,
                _connectCallback = _connectCallback,
                _plaintextStreamFilter = _plaintextStreamFilter,
                _disableDynamicHttp2WindowSizing = _disableDynamicHttp2WindowSizing,
                _maxHttp2StreamWindowSize = _maxHttp2StreamWindowSize,
                _http2StreamWindowScaleThresholdMultiplier = _http2StreamWindowScaleThresholdMultiplier,
                _initialHttp2StreamWindowSize = _initialHttp2StreamWindowSize,
            };

            // TODO: Remove if/when QuicImplementationProvider is removed from System.Net.Quic.
            if (HttpConnectionPool.IsHttp3Supported())
            {
                settings._quicImplementationProvider = _quicImplementationProvider;
            }

            return settings;
        }

        // Default to allowing HTTP/2, but enable that to be overridden by an
        // AppContext switch, or by an environment variable being set to false/0.
        private static bool AllowHttp2 => RuntimeSettingParser.QueryRuntimeSettingSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2Support",
            "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT",
            true);

        // Default to allowing draft HTTP/3, but enable that to be overridden
        // by an AppContext switch, or by an environment variable being set to false/0.
        private static bool AllowDraftHttp3 => RuntimeSettingParser.QueryRuntimeSettingSwitch(
            "System.Net.SocketsHttpHandler.Http3DraftSupport",
            "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP3DRAFTSUPPORT",
            true);

        // Switch to disable the HTTP/2 dynamic window scaling algorithm. Enabled by default.
        private static bool DisableDynamicHttp2WindowSizing => RuntimeSettingParser.QueryRuntimeSettingSwitch(
            "System.Net.SocketsHttpHandler.Http2FlowControl.DisableDynamicWindowSizing",
            "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2FLOWCONTROL_DISABLEDYNAMICWINDOWSIZING",
            false);

        // The maximum size of the HTTP/2 stream receive window. Defaults to 16 MB.
        private static int MaxHttp2StreamWindowSize
        {
            get
            {
                int value = RuntimeSettingParser.ParseInt32EnvironmentVariableValue(
                    "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_MAXSTREAMWINDOWSIZE",
                    HttpHandlerDefaults.DefaultHttp2MaxStreamWindowSize);

                // Disallow small values:
                if (value < Http2Connection.DefaultInitialWindowSize)
                {
                    value = Http2Connection.DefaultInitialWindowSize;
                }
                return value;
            }
        }

        // Defaults to 1.0. Higher values result in shorter window, but slower downloads.
        private static double Http2StreamWindowScaleThresholdMultiplier
        {
            get
            {
                double value = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue(
                    "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_STREAMWINDOWSCALETHRESHOLDMULTIPLIER",
                    HttpHandlerDefaults.DefaultHttp2StreamWindowScaleThresholdMultiplier);

                // Disallow negative values:
                if (value < 0)
                {
                    value = HttpHandlerDefaults.DefaultHttp2StreamWindowScaleThresholdMultiplier;
                }
                return value;
            }
        }

        public bool EnableMultipleHttp2Connections => _enableMultipleHttp2Connections;

        private byte[]? _http3SettingsFrame;

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        internal byte[] Http3SettingsFrame => _http3SettingsFrame ??= Http3Connection.BuildSettingsFrame(this);
    }
}
