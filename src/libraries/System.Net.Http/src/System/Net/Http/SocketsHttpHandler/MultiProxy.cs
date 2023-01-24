// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    /// <summary>
    /// A collection of proxies.
    /// </summary>
    internal struct MultiProxy
    {
        private readonly FailedProxyCache? _failedProxyCache;
        private readonly Uri[]? _uris;
        private readonly string? _proxyConfig;
        private readonly bool _secure;
        private int _currentIndex;
        private Uri? _currentUri;

        private MultiProxy(FailedProxyCache? failedProxyCache, Uri[] uris)
        {
            _failedProxyCache = failedProxyCache;
            _uris = uris;
            _proxyConfig = null;
            _secure = default;
            _currentIndex = 0;
            _currentUri = null;
        }

        private MultiProxy(FailedProxyCache failedProxyCache, string proxyConfig, bool secure)
        {
            _failedProxyCache = failedProxyCache;
            _uris = null;
            _proxyConfig = proxyConfig;
            _secure = secure;
            _currentIndex = 0;
            _currentUri = null;
        }

        public static MultiProxy Empty => new MultiProxy(null, Array.Empty<Uri>());

        /// <summary>
        /// Parses a WinHTTP proxy config into a MultiProxy instance.
        /// </summary>
        /// <param name="failedProxyCache">The cache of failed proxy requests to employ.</param>
        /// <param name="proxyConfig">The WinHTTP proxy config to parse.</param>
        /// <param name="secure">If true, return proxies suitable for use with a secure connection. If false, return proxies suitable for an insecure connection.</param>
        public static MultiProxy Parse(FailedProxyCache failedProxyCache, string? proxyConfig, bool secure)
        {
            Debug.Assert(failedProxyCache != null);

            Uri[] uris = Array.Empty<Uri>();

            ReadOnlySpan<char> span = proxyConfig;
            while (TryParseProxyConfigPart(span, secure, out Uri? uri, out int charactersConsumed))
            {
                int idx = uris.Length;

                // Assume that we will typically not have more than 1...3 proxies, so just
                // grow by 1. This method is currently only used once per process, so the
                // case of an abnormally large config will not be much of a concern anyway.
                Array.Resize(ref uris, idx + 1);
                uris[idx] = uri;

                span = span.Slice(charactersConsumed);
            }

            return new MultiProxy(failedProxyCache, uris);
        }

        /// <summary>
        /// Initializes a MultiProxy instance that lazily parses a given WinHTTP configuration string.
        /// </summary>
        /// <param name="failedProxyCache">The cache of failed proxy requests to employ.</param>
        /// <param name="proxyConfig">The WinHTTP proxy config to parse.</param>
        /// <param name="secure">If true, return proxies suitable for use with a secure connection. If false, return proxies suitable for an insecure connection.</param>
        public static MultiProxy CreateLazy(FailedProxyCache failedProxyCache, string proxyConfig, bool secure)
        {
            Debug.Assert(failedProxyCache != null);

            return string.IsNullOrEmpty(proxyConfig) == false ?
                new MultiProxy(failedProxyCache, proxyConfig, secure) :
                MultiProxy.Empty;
        }

        /// <summary>
        /// Reads the next proxy URI from the MultiProxy.
        /// </summary>
        /// <param name="uri">The next proxy to use for the request.</param>
        /// <param name="isFinalProxy">If true, indicates there are no further proxies to read from the config.</param>
        /// <returns>If there is a proxy available, true. Otherwise, false.</returns>
        public bool ReadNext([NotNullWhen(true)] out Uri? uri, out bool isFinalProxy)
        {
            // Enumerating indicates the previous proxy has failed; mark it as such.
            if (_currentUri != null)
            {
                Debug.Assert(_failedProxyCache != null);
                _failedProxyCache.SetProxyFailed(_currentUri);
            }

            // If no more proxies to read, return out quickly.
            if (!ReadNextHelper(out uri, out isFinalProxy))
            {
                _currentUri = null;
                return false;
            }

            // If this is the first ReadNext() and all proxies are marked as failed, return the proxy that is closest to renewal.
            Uri? oldestFailedProxyUri = null;
            long oldestFailedProxyTicks = long.MaxValue;

            do
            {
                Debug.Assert(_failedProxyCache != null);
                long renewTicks = _failedProxyCache.GetProxyRenewTicks(uri);

                // Proxy hasn't failed recently, return for use.
                if (renewTicks == FailedProxyCache.Immediate)
                {
                    _currentUri = uri;
                    return true;
                }

                if (renewTicks < oldestFailedProxyTicks)
                {
                    oldestFailedProxyUri = uri;
                    oldestFailedProxyTicks = renewTicks;
                }
            }
            while (ReadNextHelper(out uri, out isFinalProxy));

            // All the proxies in the config have failed; in this case, return the proxy that is closest to renewal.
            if (_currentUri == null)
            {
                uri = oldestFailedProxyUri;
                _currentUri = oldestFailedProxyUri;

                if (oldestFailedProxyUri != null)
                {
                    Debug.Assert(uri != null);
                    _failedProxyCache.TryRenewProxy(uri, oldestFailedProxyTicks);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reads the next proxy URI from the MultiProxy, either via parsing a config string or from an array.
        /// </summary>
        private bool ReadNextHelper([NotNullWhen(true)] out Uri? uri, out bool isFinalProxy)
        {
            Debug.Assert(_uris != null || _proxyConfig != null, $"{nameof(ReadNext)} must not be called on a default-initialized {nameof(MultiProxy)}.");

            if (_uris != null)
            {
                if (_currentIndex == _uris.Length)
                {
                    uri = default;
                    isFinalProxy = default;
                    return false;
                }

                uri = _uris[_currentIndex++];
                isFinalProxy = _currentIndex == _uris.Length;
                return true;
            }

            Debug.Assert(_proxyConfig != null);
            if (_currentIndex < _proxyConfig.Length)
            {
                bool hasProxy = TryParseProxyConfigPart(_proxyConfig.AsSpan(_currentIndex), _secure, out uri!, out int charactersConsumed);

                _currentIndex += charactersConsumed;
                Debug.Assert(_currentIndex <= _proxyConfig.Length);

                isFinalProxy = _currentIndex == _proxyConfig.Length;

                return hasProxy;
            }

            uri = default;
            isFinalProxy = default;
            return false;
        }

        /// <summary>
        /// This method is used to parse WinINet Proxy strings, a single proxy at a time.
        /// </summary>
        /// <remarks>
        /// The strings are a semicolon or whitespace separated list, with each entry in the following format:
        /// ([&lt;scheme&gt;=][&lt;scheme&gt;"://"]&lt;server&gt;[":"&lt;port&gt;])
        /// </remarks>
        private static bool TryParseProxyConfigPart(ReadOnlySpan<char> proxyString, bool secure, [NotNullWhen(true)] out Uri? uri, out int charactersConsumed)
        {
            const int SECURE_FLAG = 1;
            const int INSECURE_FLAG = 2;
            const string ProxyDelimiters = "; \n\r\t";

            int wantedFlag = secure ? SECURE_FLAG : INSECURE_FLAG;
            int originalLength = proxyString.Length;

            while (true)
            {
                // Skip any delimiters.
                int iter = 0;
                while (iter < proxyString.Length && ProxyDelimiters.Contains(proxyString[iter]))
                {
                    ++iter;
                }

                if (iter == proxyString.Length)
                {
                    break;
                }

                proxyString = proxyString.Slice(iter);

                // Determine which scheme this part is for.
                // If no schema is defined, use both.
                int proxyType = SECURE_FLAG | INSECURE_FLAG;

                if (proxyString.StartsWith("http="))
                {
                    proxyType = INSECURE_FLAG;
                    proxyString = proxyString.Slice("http=".Length);
                }
                else if (proxyString.StartsWith("https="))
                {
                    proxyType = SECURE_FLAG;
                    proxyString = proxyString.Slice("https=".Length);
                }

                if (proxyString.StartsWith("http://"))
                {
                    proxyType = INSECURE_FLAG;
                    proxyString = proxyString.Slice("http://".Length);
                }
                else if (proxyString.StartsWith("https://"))
                {
                    proxyType = SECURE_FLAG;
                    proxyString = proxyString.Slice("https://".Length);
                }

                // Find the next delimiter, or end of string.
                iter = proxyString.IndexOfAny(ProxyDelimiters);
                if (iter < 0)
                {
                    iter = proxyString.Length;
                }

                // Return URI if it's a match to what we want.
                if ((proxyType & wantedFlag) != 0 && Uri.TryCreate(string.Concat("http://", proxyString.Slice(0, iter)), UriKind.Absolute, out uri))
                {
                    charactersConsumed = originalLength - proxyString.Length + iter;
                    Debug.Assert(charactersConsumed > 0);

                    return true;
                }

                proxyString = proxyString.Slice(iter);
            }

            uri = null;
            charactersConsumed = originalLength;
            return false;
        }
    }
}
