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
        private const string Http2SupportEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT";
        private const string Http2SupportAppCtxSettingName = "System.Net.Http.SocketsHttpHandler.Http2Support";
        private const string Http3DraftSupportEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP3DRAFTSUPPORT";
        private const string Http3DraftSupportAppCtxSettingName = "System.Net.SocketsHttpHandler.Http3DraftSupport";

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
                _plaintextStreamFilter = _plaintextStreamFilter
            };

            // TODO: Remove if/when QuicImplementationProvider is removed from System.Net.Quic.
            if (HttpConnectionPool.IsHttp3Supported())
            {
                settings._quicImplementationProvider = _quicImplementationProvider;
            }

            return settings;
        }

        private static bool AllowHttp2
        {
            get
            {
                // Default to allowing HTTP/2, but enable that to be overridden by an
                // AppContext switch, or by an environment variable being set to false/0.

                // First check for the AppContext switch, giving it priority over the environment variable.
                if (AppContext.TryGetSwitch(Http2SupportAppCtxSettingName, out bool allowHttp2))
                {
                    return allowHttp2;
                }

                // AppContext switch wasn't used. Check the environment variable.
                string? envVar = Environment.GetEnvironmentVariable(Http2SupportEnvironmentVariableSettingName);
                if (envVar != null && (envVar.Equals("false", StringComparison.OrdinalIgnoreCase) || envVar.Equals("0")))
                {
                    // Disallow HTTP/2 protocol.
                    return false;
                }

                // Default to a maximum of HTTP/2.
                return true;
            }
        }

        private static bool AllowDraftHttp3
        {
            get
            {
                // Default to allowing draft HTTP/3, but enable that to be overridden
                // by an AppContext switch, or by an environment variable being set to false/0.

                // First check for the AppContext switch, giving it priority over the environment variable.
                if (AppContext.TryGetSwitch(Http3DraftSupportAppCtxSettingName, out bool allowHttp3))
                {
                    return allowHttp3;
                }

                // AppContext switch wasn't used. Check the environment variable.
                string? envVar = Environment.GetEnvironmentVariable(Http3DraftSupportEnvironmentVariableSettingName);
                if (envVar != null && (envVar.Equals("false", StringComparison.OrdinalIgnoreCase) || envVar.Equals("0")))
                {
                    // Disallow HTTP/3 protocol for HTTP endpoints.
                    return false;
                }

                // Default to allow.
                return true;
            }
        }

        public bool EnableMultipleHttp2Connections => _enableMultipleHttp2Connections;

        private byte[]? _http3SettingsFrame;

        // TODO: SupportedOSPlatform doesn't work for internal APIs https://github.com/dotnet/runtime/issues/51305
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        internal byte[] Http3SettingsFrame => _http3SettingsFrame ??= Http3Connection.BuildSettingsFrame(this);
    }
}
