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
    // General flow of requests through the various layers:
    //
    // (1) HttpConnectionPoolManager.SendAsync: Does proxy lookup
    // (2) HttpConnectionPoolManager.SendAsyncCore: Find or create connection pool
    // (3) HttpConnectionPool.SendAsync: Handle basic/digest request auth
    // (4) HttpConnectionPool.SendWithProxyAuthAsync: Handle basic/digest proxy auth
    // (5) HttpConnectionPool.SendWithRetryAsync: Retrieve connection from pool, or create new
    //                                            Also, handle retry for failures on connection reuse
    // (6) HttpConnection.SendAsync: Handle negotiate/ntlm connection auth
    // (7) HttpConnection.SendWithNtProxyAuthAsync: Handle negotiate/ntlm proxy auth
    // (8) HttpConnection.SendAsyncCore: Write request to connection and read response
    //                                   Also, handle cookie processing
    //
    // Redirect and deompression handling are done above HttpConnectionPoolManager,
    // in RedirectHandler and DecompressionHandler respectively.

    /// <summary>Provides a set of connection pools, each for its own endpoint.</summary>
    internal sealed class HttpConnectionPoolManager : IDisposable
    {
        /// <summary>How frequently an operation should be initiated to clean out old pools and connections in those pools.</summary>
        private readonly TimeSpan _cleanPoolTimeout;
        /// <summary>The pools, indexed by endpoint.</summary>
        private readonly ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool> _pools;
        /// <summary>Timer used to initiate cleaning of the pools.</summary>
        private readonly Timer _cleaningTimer;
        /// <summary>The maximum number of connections allowed per pool. <see cref="int.MaxValue"/> indicates unlimited.</summary>
        private readonly int _maxConnectionsPerServer;
        // Temporary
        private readonly HttpConnectionSettings _settings;
        private readonly IWebProxy _proxy;
        private readonly ICredentials _proxyCredentials;

        /// <summary>
        /// Keeps track of whether or not the cleanup timer is running. It helps us avoid the expensive
        /// <see cref="ConcurrentDictionary{TKey,TValue}.IsEmpty"/> call.
        /// </summary>
        private bool _timerIsRunning;
        /// <summary>Object used to synchronize access to state in the pool.</summary>
        private object SyncObj => _pools;

        /// <summary>Initializes the pools.</summary>
        public HttpConnectionPoolManager(HttpConnectionSettings settings)
        {
            _settings = settings;
            _maxConnectionsPerServer = settings._maxConnectionsPerServer;
            _pools = new ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>();

            // As an optimization, we can sometimes avoid the overheads associated with
            // storing connections.  This is possible when we would immediately terminate
            // connections anyway due to either the idle timeout or the lifetime being
            // set to zero, as in that case the timeout effectively immediately expires.
            // However, we can only do such optimizations if we're not also tracking
            // connections per server, as we use data in the associated data structures
            // to do that tracking.
            bool avoidStoringConnections =
                settings._maxConnectionsPerServer == int.MaxValue &&
                (settings._pooledConnectionIdleTimeout == TimeSpan.Zero ||
                 settings._pooledConnectionLifetime == TimeSpan.Zero);

            // Start out with the timer not running, since we have no pools.
            // When it does run, run it with a frequency based on the idle timeout.
            if (!avoidStoringConnections)
            {
                if (settings._pooledConnectionIdleTimeout == Timeout.InfiniteTimeSpan)
                {
                    const int DefaultScavengeSeconds = 30;
                    _cleanPoolTimeout = TimeSpan.FromSeconds(DefaultScavengeSeconds);
                }
                else
                {
                    const int ScavengesPerIdle = 4;
                    const int MinScavengeSeconds = 1;
                    TimeSpan timerPeriod = settings._pooledConnectionIdleTimeout / ScavengesPerIdle;
                    _cleanPoolTimeout = timerPeriod.TotalSeconds >= MinScavengeSeconds ? timerPeriod : TimeSpan.FromSeconds(MinScavengeSeconds);
                }

                bool restoreFlow = false;
                try
                {
                    // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
                    if (!ExecutionContext.IsFlowSuppressed())
                    {
                        ExecutionContext.SuppressFlow();
                        restoreFlow = true;
                    }

                    // Create the timer.  Ensure the Timer has a weak reference to this manager; otherwise, it
                    // can introduce a cycle that keeps the HttpConnectionPoolManager rooted by the Timer
                    // implementation until the handler is Disposed (or indefinitely if it's not).
                    _cleaningTimer = new Timer(s =>
                    {
                        var wr = (WeakReference<HttpConnectionPoolManager>)s;
                        if (wr.TryGetTarget(out HttpConnectionPoolManager thisRef))
                        {
                            thisRef.RemoveStalePools();
                        }
                    }, new WeakReference<HttpConnectionPoolManager>(this), Timeout.Infinite, Timeout.Infinite);
                }
                finally
                {
                    // Restore the current ExecutionContext
                    if (restoreFlow)
                    {
                        ExecutionContext.RestoreFlow();
                    }
                }
            }

            // Figure out proxy stuff.
            if (settings._useProxy)
            {
                _proxy = settings._proxy ?? SystemProxyInfo.ConstructSystemProxy();
                if (_proxy != null)
                {
                    _proxyCredentials = _proxy.Credentials ?? settings._defaultProxyCredentials;
                }
            }
        }

        public HttpConnectionSettings Settings => _settings;
        public ICredentials ProxyCredentials => _proxyCredentials;

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

        private static HttpConnectionKey GetConnectionKey(HttpRequestMessage request, Uri proxyUri, bool isProxyConnect)
        {
            Uri uri = request.RequestUri;

            if (isProxyConnect)
            {
                Debug.Assert(uri == proxyUri);
                return new HttpConnectionKey(HttpConnectionKind.ProxyConnect, uri.IdnHost, uri.Port, null, proxyUri);
            }

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
                    if (HttpUtilities.IsNonSecureWebSocketScheme(uri.Scheme))
                    {
                        // Non-secure websocket connection through proxy to the destination.
                        return new HttpConnectionKey(HttpConnectionKind.ProxyTunnel, uri.IdnHost, uri.Port, null, proxyUri);
                    }
                    else
                    {
                        // Standard HTTP proxy usage for non-secure requests
                        // The destination host and port are ignored here, since these connections
                        // will be shared across any requests that use the proxy.
                        return new HttpConnectionKey(HttpConnectionKind.Proxy, null, 0, null, proxyUri);
                    }
                }
                else
                {
                    // Tunnel SSL connection through proxy to the destination.
                    return new HttpConnectionKey(HttpConnectionKind.SslProxyTunnel, uri.IdnHost, uri.Port, sslHostName, proxyUri);
                }
            }
            else if (sslHostName != null)
            {
                return new HttpConnectionKey(HttpConnectionKind.Https, uri.IdnHost, uri.Port, sslHostName, null);
            }
            else
            {
                return new HttpConnectionKey(HttpConnectionKind.Http, uri.IdnHost, uri.Port, null, null);
            }
        }

        public Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, Uri proxyUri, bool doRequestAuth, bool isProxyConnect, CancellationToken cancellationToken)
        {
            HttpConnectionKey key = GetConnectionKey(request, proxyUri, isProxyConnect);

            HttpConnectionPool pool;
            while (!_pools.TryGetValue(key, out pool))
            {
                // TODO: #28863 Uri.IdnHost is missing '[', ']' characters around IPv6 address.
                // So, we need to add them manually for now.
                bool isNonNullIPv6address = key.Host != null && request.RequestUri.HostNameType == UriHostNameType.IPv6;

                pool = new HttpConnectionPool(this, key.Kind, isNonNullIPv6address ? "[" + key.Host + "]" : key.Host, key.Port, key.SslHostName, key.ProxyUri, _maxConnectionsPerServer);

                if (_cleaningTimer == null)
                {
                    // There's no cleaning timer, which means we're not adding connections into pools, but we still need
                    // the pool object for this request.  We don't need or want to add the pool to the pools, though,
                    // since we don't want it to sit there forever, which it would without the cleaning timer.
                    break;
                }

                if (_pools.TryAdd(key, pool))
                {
                    // We need to ensure the cleanup timer is running if it isn't
                    // already now that we added a new connection pool.
                    lock (SyncObj)
                    {
                        if (!_timerIsRunning)
                        {
                            SetCleaningTimer(_cleanPoolTimeout);
                        }
                    }
                    break;
                }

                // We created a pool and tried to add it to our pools, but some other thread got there before us.
                // We don't need to Dispose the pool, as that's only needed when it contains connections
                // that need to be closed.
            }

            return pool.SendAsync(request, doRequestAuth, cancellationToken);
        }

        public Task<HttpResponseMessage> SendProxyConnectAsync(HttpRequestMessage request, Uri proxyUri, CancellationToken cancellationToken)
        {
            return SendAsyncCore(request, proxyUri, doRequestAuth:false, isProxyConnect:true, cancellationToken);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if (_proxy == null)
            {
                return SendAsyncCore(request, null, doRequestAuth, isProxyConnect:false, cancellationToken);
            }

            // Do proxy lookup.
            Uri proxyUri = null;
            try
            {
                if (!_proxy.IsBypassed(request.RequestUri))
                {
                    proxyUri = _proxy.GetProxy(request.RequestUri);
                }
            }
            catch (Exception ex)
            {
                // Eat any exception from the IWebProxy and just treat it as no proxy.
                // This matches the behavior of other handlers.
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"Exception from IWebProxy.GetProxy({request.RequestUri}): {ex}");
            }

            if (proxyUri != null && proxyUri.Scheme != UriScheme.Http)
            {
                throw new NotSupportedException(SR.net_http_invalid_proxy_scheme);
            }

            return SendAsyncCore(request, proxyUri, doRequestAuth, isProxyConnect:false, cancellationToken);
        }

        /// <summary>Disposes of the pools, disposing of each individual pool.</summary>
        public void Dispose()
        {
            _cleaningTimer?.Dispose();

            foreach (KeyValuePair<HttpConnectionKey, HttpConnectionPool> pool in _pools)
            {
                pool.Value.Dispose();
            }

            if (_proxy is IDisposable obj)
            {
                obj.Dispose();
            }
        }

        /// <summary>Sets <see cref="_cleaningTimer"/> and <see cref="_timerIsRunning"/> based on the specified timeout.</summary>
        private void SetCleaningTimer(TimeSpan timeout)
        {
            try
            {
                _cleaningTimer.Change(timeout, timeout);
                _timerIsRunning = timeout != Timeout.InfiniteTimeSpan;
            }
            catch (ObjectDisposedException)
            {
                // In a rare race condition where the timer callback was queued
                // or executed and then the pool manager was disposed, the timer
                // would be disposed and then calling Change on it could result
                // in an ObjectDisposedException.  We simply eat that.
            }
        }

        /// <summary>Removes unusable connections from each pool, and removes stale pools entirely.</summary>
        private void RemoveStalePools()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            Debug.Assert(_cleaningTimer != null);

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
                    SetCleaningTimer(Timeout.InfiniteTimeSpan);
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

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal readonly struct HttpConnectionKey : IEquatable<HttpConnectionKey>
        {
            public readonly HttpConnectionKind Kind;
            public readonly string Host;
            public readonly int Port;
            public readonly string SslHostName;     // null if not SSL
            public readonly Uri ProxyUri;

            public HttpConnectionKey(HttpConnectionKind kind, string host, int port, string sslHostName, Uri proxyUri)
            {
                Kind = kind;
                Host = host;
                Port = port;
                SslHostName = sslHostName;
                ProxyUri = proxyUri;
            }

            // In the common case, SslHostName (when present) is equal to Host.  If so, don't include in hash.
            public override int GetHashCode() =>
                (SslHostName == Host ?
                    HashCode.Combine(Kind, Host, Port, ProxyUri) :
                    HashCode.Combine(Kind, Host, Port, SslHostName, ProxyUri));

            public override bool Equals(object obj) =>
                obj != null &&
                obj is HttpConnectionKey &&
                Equals((HttpConnectionKey)obj);

            public bool Equals(HttpConnectionKey other) =>
                Kind == other.Kind &&
                Host == other.Host &&
                Port == other.Port &&
                ProxyUri == other.ProxyUri &&
                SslHostName == other.SslHostName;
        }
    }
}
