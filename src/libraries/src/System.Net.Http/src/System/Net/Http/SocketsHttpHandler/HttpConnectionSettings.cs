// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Security;

namespace System.Net.Http
{
    /// <summary>Provides a state bag of settings for configuring HTTP connections.</summary>
    internal sealed class HttpConnectionSettings
    {
        private const string Http2SupportEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT";
        private const string Http2SupportAppCtxSettingName = "System.Net.Http.SocketsHttpHandler.Http2Support";
        private const string Http2UnencryptedSupportEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT";
        private const string Http2UnencryptedSupportAppCtxSettingName = "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport";

        internal DecompressionMethods _automaticDecompression = HttpHandlerDefaults.DefaultAutomaticDecompression;

        internal bool _useCookies = HttpHandlerDefaults.DefaultUseCookies;
        internal CookieContainer _cookieContainer;

        internal bool _useProxy = HttpHandlerDefaults.DefaultUseProxy;
        internal IWebProxy _proxy;
        internal ICredentials _defaultProxyCredentials;

        internal bool _preAuthenticate = HttpHandlerDefaults.DefaultPreAuthenticate;
        internal ICredentials _credentials;

        internal bool _allowAutoRedirect = HttpHandlerDefaults.DefaultAutomaticRedirection;
        internal int _maxAutomaticRedirections = HttpHandlerDefaults.DefaultMaxAutomaticRedirections;

        internal int _maxConnectionsPerServer = HttpHandlerDefaults.DefaultMaxConnectionsPerServer;
        internal int _maxResponseDrainSize = HttpHandlerDefaults.DefaultMaxResponseDrainSize;
        internal TimeSpan _maxResponseDrainTime = HttpHandlerDefaults.DefaultResponseDrainTimeout;
        internal int _maxResponseHeadersLength = HttpHandlerDefaults.DefaultMaxResponseHeadersLength;

        internal TimeSpan _pooledConnectionLifetime = HttpHandlerDefaults.DefaultPooledConnectionLifetime;
        internal TimeSpan _pooledConnectionIdleTimeout = HttpHandlerDefaults.DefaultPooledConnectionIdleTimeout;
        internal TimeSpan _expect100ContinueTimeout = HttpHandlerDefaults.DefaultExpect100ContinueTimeout;
        internal TimeSpan _connectTimeout = HttpHandlerDefaults.DefaultConnectTimeout;

        internal Version _maxHttpVersion;

        internal bool _allowUnencryptedHttp2;

        internal SslClientAuthenticationOptions _sslOptions;

        internal IDictionary<string, object> _properties;

        public HttpConnectionSettings()
        {
            bool allowHttp2 = AllowHttp2;
            _maxHttpVersion = allowHttp2 ? HttpVersion.Version20 : HttpVersion.Version11;
            _allowUnencryptedHttp2 = allowHttp2 && AllowUnencryptedHttp2;
        }

        /// <summary>Creates a copy of the settings but with some values normalized to suit the implementation.</summary>
        public HttpConnectionSettings CloneAndNormalize()
        {
            // Force creation of the cookie container if needed, so the original and clone share the same instance.
            if (_useCookies && _cookieContainer == null)
            {
                _cookieContainer = new CookieContainer();
            }

            return new HttpConnectionSettings()
            {
                _allowAutoRedirect = _allowAutoRedirect,
                _automaticDecompression = _automaticDecompression,
                _cookieContainer = _cookieContainer,
                _connectTimeout = _connectTimeout,
                _credentials = _credentials,
                _defaultProxyCredentials = _defaultProxyCredentials,
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
                _allowUnencryptedHttp2 = _allowUnencryptedHttp2,
            };
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
                string envVar = Environment.GetEnvironmentVariable(Http2SupportEnvironmentVariableSettingName);
                if (envVar != null && (envVar.Equals("false", StringComparison.OrdinalIgnoreCase) || envVar.Equals("0")))
                {
                    // Disallow HTTP/2 protocol.
                    return false;
                }

                // Default to a maximum of HTTP/2.
                return true;
            }
        }

        private static bool AllowUnencryptedHttp2
        {
            get
            {
                // Default to not allowing unencrypted HTTP/2, but enable that to be overridden
                // by an AppContext switch, or by an environment variable being to to true/1.

                // First check for the AppContext switch, giving it priority over the environment variable.
                if (AppContext.TryGetSwitch(Http2UnencryptedSupportAppCtxSettingName, out bool allowHttp2))
                {
                    return allowHttp2;
                }

                // AppContext switch wasn't used. Check the environment variable.
                string envVar = Environment.GetEnvironmentVariable(Http2UnencryptedSupportEnvironmentVariableSettingName);
                if (envVar != null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1")))
                {
                    // Allow HTTP/2.0 protocol for HTTP endpoints.
                    return true;
                }

                // Default to a maximum of HTTP/1.1.
                return false;
            }
        }
    }
}
