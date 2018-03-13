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

        internal SslClientAuthenticationOptions _sslOptions;

        internal IDictionary<string, object> _properties;

        public HttpConnectionSettings Clone()
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
            };
        }
    }
}
