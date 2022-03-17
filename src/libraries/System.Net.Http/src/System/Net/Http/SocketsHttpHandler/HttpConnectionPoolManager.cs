// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

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
        /// <summary>
        /// WeakReferences to all managers in the process. The value is a dummy with no functional significance.
        /// This collection is iterated every second in HeartBeatAndCleanAllPools and collected instances are pruned.
        /// </summary>
        public static readonly ConcurrentDictionary<WeakReference<HttpConnectionPoolManager>, byte> AllManagers = new();

        private const int GlobalHeartBeatTimerMs = 1000;
        private static readonly Timer s_globalHeartBeatTimer = new(static _ => HeartBeatAndCleanAllPools(), null, GlobalHeartBeatTimerMs, Timeout.Infinite);

        private static int s_listeningToNetworkChanges;

        /// <summary>The pools, indexed by endpoint.</summary>
        private readonly ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>? _pools;

        /// <summary>How frequently an operation should be initiated to clean out old pools and connections in those pools.</summary>
        private readonly TimeSpan _cleanPoolInterval;
        private long _lastCleanPoolTimestamp;

        /// <summary>Heart beats are currently used for Http2 pings only.</summary>
        private readonly TimeSpan _heartBeatInterval;
        private long _lastHeartBeatTimestamp;

        private readonly WeakReference<HttpConnectionPoolManager> _weakThisRef;
        private readonly HttpConnectionSettings _settings;
        private readonly IWebProxy? _proxy;
        private readonly ICredentials? _proxyCredentials;

        private bool _interestedInNetworkChanges;

        /// <summary>Initializes the pools.</summary>
        public HttpConnectionPoolManager(HttpConnectionSettings settings)
        {
            _settings = settings;

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

            if (!avoidStoringConnections)
            {
                _pools = new ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>();

                _cleanPoolInterval = _heartBeatInterval = TimeSpan.MaxValue;
                _lastCleanPoolTimestamp = _lastHeartBeatTimestamp = Stopwatch.GetTimestamp();

                if (settings._pooledConnectionIdleTimeout == Timeout.InfiniteTimeSpan)
                {
                    const int DefaultScavengeSeconds = 30;
                    _cleanPoolInterval = TimeSpan.FromSeconds(DefaultScavengeSeconds);
                }
                else
                {
                    const int ScavengesPerIdle = 4;
                    const int MinScavengeSeconds = 1;
                    Debug.Assert(GlobalHeartBeatTimerMs <= MinScavengeSeconds * 1000);
                    TimeSpan timerPeriod = settings._pooledConnectionIdleTimeout / ScavengesPerIdle;
                    _cleanPoolInterval = timerPeriod.TotalSeconds >= MinScavengeSeconds ? timerPeriod : TimeSpan.FromSeconds(MinScavengeSeconds);
                }

                // For now heart beat is used only for ping functionality.
                if (settings._keepAlivePingDelay != Timeout.InfiniteTimeSpan)
                {
                    const int MinHeartBeatMs = 1000;
                    Debug.Assert(GlobalHeartBeatTimerMs <= MinHeartBeatMs);
                    long heartBeatIntervalMs = (long)Math.Max(MinHeartBeatMs, Math.Min(settings._keepAlivePingDelay.TotalMilliseconds, settings._keepAlivePingTimeout.TotalMilliseconds) / 4);
                    _heartBeatInterval = TimeSpan.FromMilliseconds(heartBeatIntervalMs);
                }
            }

            // Figure out proxy stuff.
            if (settings._useProxy)
            {
                _proxy = settings._proxy ?? HttpClient.DefaultProxy;
                if (_proxy != null)
                {
                    _proxyCredentials = _proxy.Credentials ?? settings._defaultProxyCredentials;
                }
            }

            _weakThisRef = new WeakReference<HttpConnectionPoolManager>(this);
            bool success = AllManagers.TryAdd(_weakThisRef, 0);
            Debug.Assert(success);
        }

        private static void HeartBeatAndCleanAllPools()
        {
            long startTimestamp = Stopwatch.GetTimestamp();

            int managersRemoved = 0;

            foreach ((WeakReference<HttpConnectionPoolManager> managerReference, _) in AllManagers)
            {
                if (managerReference.TryGetTarget(out HttpConnectionPoolManager? manager))
                {
                    manager.HeartBeatAndCleanPools();
                }
                else
                {
                    // These are only non-disposed instances that the GC collected
                    AllManagers.TryRemove(managerReference, out _);
                    managersRemoved++;
                }
            }

            TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            int dueTimeMs = (int)(GlobalHeartBeatTimerMs - Math.Min(GlobalHeartBeatTimerMs - 1, (ulong)elapsed.TotalMilliseconds));

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null,
                $"ConnectionPoolManager heartbeat took {(int)elapsed.TotalMilliseconds} ms, " +
                $"restarting timer in {dueTimeMs} ms. " +
                $"Non-disposed managers removed: {managersRemoved}, managers left: {AllManagers.Count}.");

            s_globalHeartBeatTimer.Change(dueTimeMs, Timeout.Infinite);
        }

        /// <summary>
        /// Starts monitoring for network changes. Upon a change, <see cref="HttpConnectionPool.OnNetworkChanged"/> will be
        /// called for every <see cref="HttpConnectionPool"/> in the <see cref="HttpConnectionPoolManager"/>.
        /// </summary>
        public void StartMonitoringNetworkChanges()
        {
            if (_pools is null)
            {
                return;
            }

            _interestedInNetworkChanges = true;

            if (Interlocked.Exchange(ref s_listeningToNetworkChanges, 1) != 0)
            {
                // We are already subscribed to NetworkAddressChanged
                return;
            }

            // Monitor network changes to invalidate Alt-Svc headers.
            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                NetworkChange.NetworkAddressChanged += static delegate
                {
                    foreach ((WeakReference<HttpConnectionPoolManager> managerReference, _) in AllManagers)
                    {
                        if (managerReference.TryGetTarget(out HttpConnectionPoolManager? manager) && manager._interestedInNetworkChanges)
                        {
                            Debug.Assert(manager._pools is not null, $"{nameof(_interestedInNetworkChanges)} shouldn't be set if we are not tracking pools");
                            foreach ((_, HttpConnectionPool pool) in manager._pools)
                            {
                                pool.OnNetworkChanged();
                            }
                        }
                    }
                };
            }
            finally
            {
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        }

        public HttpConnectionSettings Settings => _settings;
        public ICredentials? ProxyCredentials => _proxyCredentials;

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

        private HttpConnectionKey GetConnectionKey(HttpRequestMessage request, Uri? proxyUri, bool isProxyConnect)
        {
            Uri? uri = request.RequestUri;
            Debug.Assert(uri != null);

            if (isProxyConnect)
            {
                Debug.Assert(uri == proxyUri);
                return new HttpConnectionKey(HttpConnectionKind.ProxyConnect, uri.IdnHost, uri.Port, null, proxyUri, GetIdentityIfDefaultCredentialsUsed(_settings._defaultCredentialsUsedForProxy));
            }

            string? sslHostName = null;
            if (HttpUtilities.IsSupportedSecureScheme(uri.Scheme))
            {
                string? hostHeader = request.Headers.Host;
                if (hostHeader != null)
                {
                    sslHostName = ParseHostNameFromHeader(hostHeader);
                }
                else
                {
                    // No explicit Host header. Use host from uri.
                    sslHostName = uri.IdnHost;
                }
            }

            string identity = GetIdentityIfDefaultCredentialsUsed(proxyUri != null ? _settings._defaultCredentialsUsedForProxy : _settings._defaultCredentialsUsedForServer);

            if (proxyUri != null)
            {
                Debug.Assert(HttpUtilities.IsSupportedProxyScheme(proxyUri.Scheme));
                if (HttpUtilities.IsSocksScheme(proxyUri.Scheme))
                {
                    // Socks proxy
                    if (sslHostName != null)
                    {
                        return new HttpConnectionKey(HttpConnectionKind.SslSocksTunnel, uri.IdnHost, uri.Port, sslHostName, proxyUri, identity);
                    }
                    else
                    {
                        return new HttpConnectionKey(HttpConnectionKind.SocksTunnel, uri.IdnHost, uri.Port, null, proxyUri, identity);
                    }
                }
                else if (sslHostName == null)
                {
                    if (HttpUtilities.IsNonSecureWebSocketScheme(uri.Scheme))
                    {
                        // Non-secure websocket connection through proxy to the destination.
                        return new HttpConnectionKey(HttpConnectionKind.ProxyTunnel, uri.IdnHost, uri.Port, null, proxyUri, identity);
                    }
                    else
                    {
                        // Standard HTTP proxy usage for non-secure requests
                        // The destination host and port are ignored here, since these connections
                        // will be shared across any requests that use the proxy.
                        return new HttpConnectionKey(HttpConnectionKind.Proxy, null, 0, null, proxyUri, identity);
                    }
                }
                else
                {
                    // Tunnel SSL connection through proxy to the destination.
                    return new HttpConnectionKey(HttpConnectionKind.SslProxyTunnel, uri.IdnHost, uri.Port, sslHostName, proxyUri, identity);
                }
            }
            else if (sslHostName != null)
            {
                return new HttpConnectionKey(HttpConnectionKind.Https, uri.IdnHost, uri.Port, sslHostName, null, identity);
            }
            else
            {
                return new HttpConnectionKey(HttpConnectionKind.Http, uri.IdnHost, uri.Port, null, null, identity);
            }
        }

        public ValueTask<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, Uri? proxyUri, bool async, bool doRequestAuth, bool isProxyConnect, CancellationToken cancellationToken)
        {
            HttpConnectionKey key = GetConnectionKey(request, proxyUri, isProxyConnect);

            HttpConnectionPool? pool;
            while (_pools is null || !_pools.TryGetValue(key, out pool))
            {
                pool = new HttpConnectionPool(this, key.Kind, key.Host, key.Port, key.SslHostName, key.ProxyUri);

                if (_pools is null)
                {
                    // We are not storing connections into pools, but we still need the pool object for this request.
                    break;
                }

                if (_pools.TryAdd(key, pool))
                {
                    break;
                }

                // We created a pool and tried to add it to our pools, but some other thread got there before us.
                // We don't need to Dispose the pool, as that's only needed when it contains connections
                // that need to be closed.
            }

            return pool.SendAsync(request, async, doRequestAuth, cancellationToken);
        }

        public ValueTask<HttpResponseMessage> SendProxyConnectAsync(HttpRequestMessage request, Uri proxyUri, bool async, CancellationToken cancellationToken)
        {
            return SendAsyncCore(request, proxyUri, async, doRequestAuth: false, isProxyConnect: true, cancellationToken);
        }

        public ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if (_proxy == null)
            {
                return SendAsyncCore(request, null, async, doRequestAuth, isProxyConnect: false, cancellationToken);
            }

            // Do proxy lookup.
            Uri? proxyUri = null;
            try
            {
                Debug.Assert(request.RequestUri != null);
                if (!_proxy.IsBypassed(request.RequestUri))
                {
                    if (_proxy is IMultiWebProxy multiWebProxy)
                    {
                        MultiProxy multiProxy = multiWebProxy.GetMultiProxy(request.RequestUri);

                        if (multiProxy.ReadNext(out proxyUri, out bool isFinalProxy) && !isFinalProxy)
                        {
                            return SendAsyncMultiProxy(request, async, doRequestAuth, multiProxy, proxyUri, cancellationToken);
                        }
                    }
                    else
                    {
                        proxyUri = _proxy.GetProxy(request.RequestUri);
                    }
                }
            }
            catch (Exception ex)
            {
                // Eat any exception from the IWebProxy and just treat it as no proxy.
                // This matches the behavior of other handlers.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Exception from {_proxy.GetType().Name}.GetProxy({request.RequestUri}): {ex}");
            }

            if (proxyUri != null && !HttpUtilities.IsSupportedProxyScheme(proxyUri.Scheme))
            {
                throw new NotSupportedException(SR.net_http_invalid_proxy_scheme);
            }

            return SendAsyncCore(request, proxyUri, async, doRequestAuth, isProxyConnect: false, cancellationToken);
        }

        /// <summary>
        /// Iterates a request over a set of proxies until one works, or all proxies have failed.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="async">Whether to execute the request synchronously or asynchronously.</param>
        /// <param name="doRequestAuth">Whether to perform request authentication.</param>
        /// <param name="multiProxy">The set of proxies to use.</param>
        /// <param name="firstProxy">The first proxy try.</param>
        /// <param name="cancellationToken">The cancellation token to use for the operation.</param>
        private async ValueTask<HttpResponseMessage> SendAsyncMultiProxy(HttpRequestMessage request, bool async, bool doRequestAuth, MultiProxy multiProxy, Uri? firstProxy, CancellationToken cancellationToken)
        {
            HttpRequestException rethrowException;

            do
            {
                try
                {
                    return await SendAsyncCore(request, firstProxy, async, doRequestAuth, isProxyConnect: false, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException ex) when (ex.AllowRetry != RequestRetryType.NoRetry)
                {
                    rethrowException = ex;
                }
            }
            while (multiProxy.ReadNext(out firstProxy, out _));

            ExceptionDispatchInfo.Throw(rethrowException);
            return null; // should never be reached: VS doesn't realize Throw() never returns.
        }

        /// <summary>Disposes of the pools, disposing of each individual pool.</summary>
        public void Dispose()
        {
            AllManagers.TryRemove(_weakThisRef, out _);

            if (_pools is not null)
            {
                foreach ((_, HttpConnectionPool pool) in _pools)
                {
                    pool.Dispose();
                }
            }
        }

        private void HeartBeatAndCleanPools()
        {
            if (_pools is null)
            {
                return;
            }

            long currentTimestamp = Stopwatch.GetTimestamp();

            bool cleanPools = false;
            if (Stopwatch.GetElapsedTime(_lastCleanPoolTimestamp, currentTimestamp) > _cleanPoolInterval)
            {
                cleanPools = true;
                _lastCleanPoolTimestamp = currentTimestamp;
            }

            bool doHeartBeat = false;
            if (Stopwatch.GetElapsedTime(_lastHeartBeatTimestamp, currentTimestamp) > _heartBeatInterval)
            {
                doHeartBeat = true;
                _lastHeartBeatTimestamp = currentTimestamp;
            }

            if (cleanPools || doHeartBeat)
            {
                // Iterate through each pool in the set of pools.  For each, ask it to clear out
                // any unusable connections (e.g. those which have expired, those which have been closed, etc.)
                // The pool may detect that it's empty and long unused, in which case it'll dispose of itself,
                // such that any connections returned to the pool to be cached will be disposed of.  In such
                // a case, we also remove the pool from the set of pools to avoid a leak.
                foreach (KeyValuePair<HttpConnectionKey, HttpConnectionPool> entry in _pools)
                {
                    if (cleanPools && entry.Value.CleanCacheAndDisposeIfUnused())
                    {
                        _pools.TryRemove(entry.Key, out HttpConnectionPool _);
                        continue;
                    }

                    if (doHeartBeat)
                    {
                        entry.Value.HeartBeat();
                    }
                }
            }
        }

        private static string GetIdentityIfDefaultCredentialsUsed(bool defaultCredentialsUsed)
        {
            return defaultCredentialsUsed ? CurrentUserIdentityProvider.GetIdentity() : string.Empty;
        }

        internal readonly struct HttpConnectionKey : IEquatable<HttpConnectionKey>
        {
            public readonly HttpConnectionKind Kind;
            public readonly string? Host;
            public readonly int Port;
            public readonly string? SslHostName;     // null if not SSL
            public readonly Uri? ProxyUri;
            public readonly string Identity;

            public HttpConnectionKey(HttpConnectionKind kind, string? host, int port, string? sslHostName, Uri? proxyUri, string identity)
            {
                Kind = kind;
                Host = host;
                Port = port;
                SslHostName = sslHostName;
                ProxyUri = proxyUri;
                Identity = identity;
            }

            // In the common case, SslHostName (when present) is equal to Host.  If so, don't include in hash.
            public override int GetHashCode() =>
                (SslHostName == Host ?
                    HashCode.Combine(Kind, Host, Port, ProxyUri, Identity) :
                    HashCode.Combine(Kind, Host, Port, SslHostName, ProxyUri, Identity));

            public override bool Equals([NotNullWhen(true)] object? obj) =>
                obj is HttpConnectionKey hck &&
                Equals(hck);

            public bool Equals(HttpConnectionKey other) =>
                Kind == other.Kind &&
                Host == other.Host &&
                Port == other.Port &&
                ProxyUri == other.ProxyUri &&
                SslHostName == other.SslHostName &&
                Identity == other.Identity;
        }
    }
}
