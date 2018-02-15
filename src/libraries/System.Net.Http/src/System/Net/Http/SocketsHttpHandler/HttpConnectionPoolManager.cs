// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides a set of connection pools, each for its own endpoint.</summary>
    internal sealed class HttpConnectionPoolManager : IDisposable
    {
        /// <summary>How frequently an operation should be initiated to clean out old pools and connections in those pools.</summary>
        private const int CleanPoolTimeoutMilliseconds =
#if DEBUG
            1_000;
#else
            30_000;
#endif
        /// <summary>The pools, indexed by endpoint.</summary>
        private readonly ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool> _pools;
        /// <summary>Timer used to initiate cleaning of the pools.</summary>
        private readonly Timer _cleaningTimer;
        /// <summary>The maximum number of connections allowed per pool. <see cref="int.MaxValue"/> indicates unlimited.</summary>
        private readonly int _maxConnectionsPerServer;
        // Temporary
        private readonly HttpConnectionSettings _settings;

        /// <summary>
        /// Keeps track of whether or not the cleanup timer is running. It helps us avoid the expensive
        /// <see cref="ConcurrentDictionary{TKey,TValue}.IsEmpty"/> call.
        /// </summary>
        private bool _timerIsRunning;
        /// <summary>Object used to synchronize access to state in the pool.</summary>
        private object SyncObj => _pools;

        /// <summary>Initializes the pools.</summary>
        /// <param name="maxConnectionsPerServer">The maximum number of connections allowed per pool. <see cref="int.MaxValue"/> indicates unlimited.</param>
        
        public HttpConnectionPoolManager(HttpConnectionSettings settings)
        {
            _settings = settings;
            _maxConnectionsPerServer = settings._maxConnectionsPerServer;
            _pools = new ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>();
            // Start out with the timer not running, since we have no pools.

            // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                _cleaningTimer = new Timer(s => ((HttpConnectionPoolManager)s).RemoveStalePools(), this, Timeout.Infinite, Timeout.Infinite);
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                    ExecutionContext.RestoreFlow();
            }
        }

        public HttpConnectionSettings Settings => _settings;

        private static string ParseHostNameFromHeader(string hostHeader)
        {
            // See if we need to trim off a port.
            int colonPos = hostHeader.IndexOf(':');
            if (colonPos >= 0)
            {
                // There is colon, which could either be a port separator or a separator in
                // an IPv6 address.  See if this is an IPv6 address; if it's not, use everything
                // before the colon as the host name, and if it is, use everything before the last
                // colon iff the last colon is after the end of the IPv6 address (otherwise it's a
                // part of the address).
                int ipV6AddressEnd = hostHeader.IndexOf(']');
                if (ipV6AddressEnd == -1)
                {
                    return hostHeader.Substring(0, colonPos);
                }
                else
                {
                    colonPos = hostHeader.LastIndexOf(':');
                    if (colonPos > ipV6AddressEnd)
                    {
                        return hostHeader.Substring(0, colonPos);
                    }
                }
            }

            return hostHeader;
        }

        private static HttpConnectionKey GetConnectionKey(HttpRequestMessage request, Uri proxyUri)
        {
            Uri uri = request.RequestUri;

            string sslHostName = null;
            if (HttpUtilities.IsSupportedSecureScheme(uri.Scheme))
            {
                string hostHeader = request.Headers.Host;
                if (hostHeader != null)
                {
                    sslHostName = ParseHostNameFromHeader(hostHeader);
                }
                else
                {
                    // No explicit Host header.  Use host from uri.
                    sslHostName = uri.IdnHost;
                }
            }

            if (proxyUri != null)
            {
                Debug.Assert(HttpUtilities.IsSupportedNonSecureScheme(proxyUri.Scheme));
                if (sslHostName == null)
                {
                    // Standard HTTP proxy usage for non-secure requests
                    // The destination host and port are ignored here, since these connections
                    // will be shared across any requests that use the proxy.
                    return new HttpConnectionKey(null, 0, null, proxyUri);
                }
                else
                {
                    // Tunnel SSL connection through proxy to the destination.
                    return new HttpConnectionKey(uri.IdnHost, uri.Port, sslHostName, proxyUri);
                }
            }
            else
            {
                return new HttpConnectionKey(uri.IdnHost, uri.Port, sslHostName, null);
            }
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Uri proxyUri, CancellationToken cancellationToken)
        {
            HttpConnectionKey key = GetConnectionKey(request, proxyUri);

            HttpConnectionPool pool;
            while (!_pools.TryGetValue(key, out pool))
            {
                pool = new HttpConnectionPool(this, key.Host, key.Port, key.SslHostName, key.ProxyUri, _maxConnectionsPerServer);
                if (_pools.TryAdd(key, pool))
                {
                    // We need to ensure the cleanup timer is running if it isn't
                    // already now that we added a new connection pool.
                    lock (SyncObj)
                    {
                        if (!_timerIsRunning)
                        {
                            _cleaningTimer.Change(CleanPoolTimeoutMilliseconds, CleanPoolTimeoutMilliseconds);
                            _timerIsRunning = true;
                        }
                    }
                    break;
                }
            }

            return pool.SendAsync(request, cancellationToken);
        }

        /// <summary>Disposes of the pools, disposing of each individual pool.</summary>
        public void Dispose()
        {
            _cleaningTimer.Dispose();
            foreach (KeyValuePair<HttpConnectionKey, HttpConnectionPool> pool in _pools)
            {
                pool.Value.Dispose();
            }
        }

        /// <summary>Removes unusable connections from each pool, and removes stale pools entirely.</summary>
        private void RemoveStalePools()
        {
            // Iterate through each pool in the set of pools.  For each, ask it to clear out
            // any unusable connections (e.g. those which have expired, those which have been closed, etc.)
            // The pool may detect that it's empty and long unused, in which case it'll dispose of itself,
            // such that any connections returned to the pool to be cached will be disposed of.  In such
            // a case, we also remove the pool from the set of pools to avoid a leak.
            foreach (KeyValuePair<HttpConnectionKey, HttpConnectionPool> entry in _pools)
            {
                if (entry.Value.CleanCacheAndDisposeIfUnused())
                {
                    _pools.TryRemove(entry.Key, out HttpConnectionPool _);
                }
            }

            // Stop running the timer if we don't have any pools to clean up.
            lock (SyncObj)
            {
                if (_pools.IsEmpty)
                {
                    _cleaningTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _timerIsRunning = false;
                }
            }

            // NOTE: There is a possible race condition with regards to a pool getting cleaned up at the same
            // time it's about to be used for another request.  The timer cleanup could start running, see that
            // a pool is empty, and initiate its disposal.  Concurrently, the pools could hand out the pool
            // to a request looking to get a connection, because the pool may not have been removed yet
            // from the pools.  Worst case here is that connection will end up getting returned to an
            // already disposed pool, in which case the connection will also end up getting disposed rather
            // than reused.  This should be a rare occurrence, so for now we don't worry about it.  In the
            // future, there are a variety of possible ways to address it, such as allowing connections to
            // be returned to pools they weren't associated with.
        }

        internal readonly struct HttpConnectionKey : IEquatable<HttpConnectionKey>
        {
            public readonly string Host;
            public readonly int Port;
            public readonly string SslHostName;     // null if not SSL
            public readonly Uri ProxyUri;

            public HttpConnectionKey(string host, int port, string sslHostName, Uri proxyUri)
            {
                Host = host;
                Port = port;
                SslHostName = sslHostName;
                ProxyUri = proxyUri;
            }

            // In the common case, SslHostName (when present) is equal to Host.  If so, don't include in hash.
            public override int GetHashCode() =>
                (SslHostName == Host ?
                    HashCode.Combine(Host, Port, ProxyUri) :
                    HashCode.Combine(Host, Port, SslHostName, ProxyUri));

            public override bool Equals(object obj) =>
                obj != null &&
                obj is HttpConnectionKey &&
                Equals((HttpConnectionKey)obj);

            public bool Equals(HttpConnectionKey other) =>
                Host == other.Host &&
                Port == other.Port &&
                ProxyUri == other.ProxyUri &&
                SslHostName == other.SslHostName;

            public static bool operator ==(HttpConnectionKey key1, HttpConnectionKey key2) => key1.Equals(key2);
            public static bool operator !=(HttpConnectionKey key1, HttpConnectionKey key2) => !key1.Equals(key2);
        }
    }
}
