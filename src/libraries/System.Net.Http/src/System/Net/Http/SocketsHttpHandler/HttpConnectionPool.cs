// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides a pool of connections to the same endpoint.</summary>
    internal sealed class HttpConnectionPool : IDisposable
    {
        private readonly HttpConnectionPoolManager _poolManager;
        private readonly HttpConnectionKind _kind;
        private readonly string _host;
        private readonly int _port;
        private readonly Uri _proxyUri;

        /// <summary>List of idle connections stored in the pool.</summary>
        private readonly List<CachedConnection> _idleConnections = new List<CachedConnection>();
        /// <summary>The maximum number of connections allowed to be associated with the pool.</summary>
        private readonly int _maxConnections;

        /// <summary>For non-proxy connection pools, this is the host name in bytes; for proxies, null.</summary>
        private readonly byte[] _hostHeaderValueBytes;
        /// <summary>Options specialized and cached for this pool and its <see cref="_key"/>.</summary>
        private readonly SslClientAuthenticationOptions _sslOptions;

        /// <summary>The head of a list of waiters waiting for a connection.  Null if no one's waiting.</summary>
        private ConnectionWaiter _waitersHead;
        /// <summary>The tail of a list of waiters waiting for a connection.  Null if no one's waiting.</summary>
        private ConnectionWaiter _waitersTail;

        /// <summary>The number of connections associated with the pool.  Some of these may be in <see cref="_idleConnections"/>, others may be in use.</summary>
        private int _associatedConnectionCount;
        /// <summary>Whether the pool has been used since the last time a cleanup occurred.</summary>
        private bool _usedSinceLastCleanup = true;
        /// <summary>Whether the pool has been disposed.</summary>
        private bool _disposed;

        private const int DefaultHttpPort = 80;
        private const int DefaultHttpsPort = 443;

        /// <summary>Initializes the pool.</summary>
        /// <param name="maxConnections">The maximum number of connections allowed to be associated with the pool at any given time.</param>
        /// 
        public HttpConnectionPool(HttpConnectionPoolManager poolManager, HttpConnectionKind kind, string host, int port, string sslHostName, Uri proxyUri, int maxConnections)
        {
            _poolManager = poolManager;
            _kind = kind;
            _host = host;
            _port = port;
            _proxyUri = proxyUri;
            _maxConnections = maxConnections;

            switch (kind)
            {
                case HttpConnectionKind.Http:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri == null);
                    break;

                case HttpConnectionKind.Https:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName != null);
                    Debug.Assert(proxyUri == null);

                    _sslOptions = ConstructSslOptions(poolManager, sslHostName);
                    break;

                case HttpConnectionKind.Proxy:
                    Debug.Assert(host == null);
                    Debug.Assert(port == 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri != null);
                    break;

                case HttpConnectionKind.ProxyTunnel:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri != null);
                    break;

                case HttpConnectionKind.SslProxyTunnel:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName != null);
                    Debug.Assert(proxyUri != null);

                    _sslOptions = ConstructSslOptions(poolManager, sslHostName);
                    break;

                case HttpConnectionKind.ProxyConnect:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri != null);
                    Debug.Assert(proxyUri.IdnHost == host && proxyUri.Port == port);
                    break;

                default:
                    Debug.Fail("Unkown HttpConnectionKind in HttpConnectionPool.ctor");
                    break;
            }

            if (_host != null)
            {
                // Precalculate ASCII bytes for Host header
                // Note that if _host is null, this is a (non-tunneled) proxy connection, and we can't cache the hostname.
                string hostHeader =
                    (_port != (_sslOptions == null ? DefaultHttpPort : DefaultHttpsPort)) ?
                    $"{_host}:{_port}" :
                    _host;

                // Note the IDN hostname should always be ASCII, since it's already been IDNA encoded.
                _hostHeaderValueBytes = Encoding.ASCII.GetBytes(hostHeader);
                Debug.Assert(Encoding.ASCII.GetString(_hostHeaderValueBytes) == hostHeader);
            }
            
            // Set up for PreAuthenticate.  Access to this cache is guarded by a lock on the cache itself.
            if (_poolManager.Settings._preAuthenticate)
            {
                PreAuthCredentials = new CredentialCache();
            }
        }

        private static SslClientAuthenticationOptions ConstructSslOptions(HttpConnectionPoolManager poolManager, string sslHostName)
        {
            Debug.Assert(sslHostName != null);

            SslClientAuthenticationOptions sslOptions = poolManager.Settings._sslOptions?.ShallowClone() ?? new SslClientAuthenticationOptions();
            sslOptions.ApplicationProtocols = null; // explicitly ignore any ApplicationProtocols set
            sslOptions.TargetHost = sslHostName; // always use the key's name rather than whatever was specified

            return sslOptions;
        }

        public HttpConnectionSettings Settings => _poolManager.Settings;
        public bool IsSecure => _sslOptions != null;
        public bool UsingProxy => _kind == HttpConnectionKind.Proxy;        // Tunnel doesn't count, only direct proxy usage
        public Uri ProxyUri => _proxyUri;
        public ICredentials ProxyCredentials => _poolManager.ProxyCredentials;
        public byte[] HostHeaderValueBytes => _hostHeaderValueBytes;
        public CredentialCache PreAuthCredentials { get; }

        /// <summary>Object used to synchronize access to state in the pool.</summary>
        private object SyncObj => _idleConnections;

        private ValueTask<(HttpConnection, HttpResponseMessage)> GetConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<(HttpConnection, HttpResponseMessage)>(Task.FromCanceled<(HttpConnection, HttpResponseMessage)>(cancellationToken));
            }

            TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
            TimeSpan pooledConnectionIdleTimeout = _poolManager.Settings._pooledConnectionIdleTimeout;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            List<CachedConnection> list = _idleConnections;
            lock (SyncObj)
            {
                // Try to return a cached connection.  We need to loop in case the connection
                // we get from the list is unusable.
                while (list.Count > 0)
                {
                    CachedConnection cachedConnection = list[list.Count - 1];
                    HttpConnection conn = cachedConnection._connection;

                    list.RemoveAt(list.Count - 1);
                    if (cachedConnection.IsUsable(now, pooledConnectionLifetime, pooledConnectionIdleTimeout) &&
                        !conn.EnsureReadAheadAndPollRead())
                    {
                        // We found a valid connection.  Return it.
                        if (NetEventSource.IsEnabled) conn.Trace("Found usable connection in pool.");
                        return new ValueTask<(HttpConnection, HttpResponseMessage)>((conn, null));
                    }

                    // We got a connection, but it was already closed by the server or the
                    // server sent unexpected data or the connection is too old.  In any case,
                    // we can't use the connection, so get rid of it and try again.
                    if (NetEventSource.IsEnabled) conn.Trace("Found invalid connection in pool.");
                    conn.Dispose();
                }

                // No valid cached connections, so we need to create a new one.  If
                // there's no limit on the number of connections associated with this
                // pool, or if we haven't reached such a limit, simply create a new
                // connection.
                if (_associatedConnectionCount < _maxConnections)
                {
                    if (NetEventSource.IsEnabled) Trace("Creating new connection for pool.");
                    IncrementConnectionCountNoLock();
                    return WaitForCreatedConnectionAsync(CreateConnectionAsync(request, cancellationToken));
                }
                else
                {
                    // There is a limit, and we've reached it, which means we need to
                    // wait for a connection to be returned to the pool or for a connection
                    // associated with the pool to be dropped before we can create a
                    // new one.  Create a waiter object and register it with the pool; it'll
                    // be signaled with the created connection when one is returned or
                    // space is available and the provided creation func has successfully
                    // created the connection to be used.
                    if (NetEventSource.IsEnabled) Trace("Limit reached.  Waiting to create new connection.");
                    var waiter = new ConnectionWaiter(this, request, cancellationToken);
                    EnqueueWaiter(waiter);
                    if (cancellationToken.CanBeCanceled)
                    {
                        // If cancellation could be requested, register a callback for it that'll cancel
                        // the waiter and remove the waiter from the queue.  Note that this registration needs
                        // to happen under the reentrant lock and after enqueueing the waiter.
                        waiter._cancellationTokenRegistration = cancellationToken.Register(s =>
                        {
                            var innerWaiter = (ConnectionWaiter)s;
                            lock (innerWaiter._pool.SyncObj)
                            {
                                // If it's in the list, remove it and cancel it.
                                if (innerWaiter._pool.RemoveWaiterForCancellation(innerWaiter))
                                {
                                    bool canceled = innerWaiter.TrySetCanceled(innerWaiter._cancellationToken);
                                    Debug.Assert(canceled);
                                }
                            }
                        }, waiter);
                    }
                    return new ValueTask<(HttpConnection, HttpResponseMessage)>(waiter.Task);
                }

                // Note that we don't check for _disposed.  We may end up disposing the
                // created connection when it's returned, but we don't want to block use
                // of the pool if it's already been disposed, as there's a race condition
                // between getting a pool and someone disposing of it, and we don't want
                // to complicate the logic about trying to get a different pool when the
                // retrieved one has been disposed of.  In the future we could alternatively
                // try returning such connections to whatever pool is currently considered
                // current for that endpoint, if there is one.
            }
        }

        public async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
        {
            while (true)
            { 
                // Loop on connection failures and retry if possible.

                (HttpConnection connection, HttpResponseMessage response) = await GetConnectionAsync(request, cancellationToken).ConfigureAwait(false);
                if (response != null)
                {
                    // Proxy tunnel failure; return proxy response
                    return response;
                }

                bool isNewConnection = connection.IsNewConnection;

                connection.Acquire();
                try
                {
                    return await connection.SendAsync(request, doRequestAuth, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (!isNewConnection && e.InnerException is IOException && connection.CanRetry)
                {
                    // Eat exception and try again.
                }
                finally
                {
                    connection.Release();
                }
            }
        }

        public Task<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if ((_kind == HttpConnectionKind.Proxy || _kind == HttpConnectionKind.ProxyConnect) &&
                _poolManager.ProxyCredentials != null)
            {
                return AuthenticationHelper.SendWithProxyAuthAsync(request, _proxyUri, _poolManager.ProxyCredentials, doRequestAuth, this, cancellationToken);
            }

            return SendWithRetryAsync(request, doRequestAuth, cancellationToken);
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if (doRequestAuth && Settings._credentials != null)
            {
                return AuthenticationHelper.SendWithRequestAuthAsync(request, Settings._credentials, Settings._preAuthenticate, this, cancellationToken);
            }

            return SendWithProxyAuthAsync(request, doRequestAuth, cancellationToken);
        }

        private async ValueTask<(HttpConnection, HttpResponseMessage)> CreateConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // If a non-infinite connect timeout has been set, create and use a new CancellationToken that'll be canceled
            // when either the original token is canceled or a connect timeout occurs.
            CancellationTokenSource cancellationWithConnectTimeout = null;
            if (Settings._connectTimeout != Timeout.InfiniteTimeSpan)
            {
                cancellationWithConnectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, default);
                cancellationWithConnectTimeout.CancelAfter(Settings._connectTimeout);
                cancellationToken = cancellationWithConnectTimeout.Token;
            }

            try
            {
                Socket socket = null;
                Stream stream = null;
                switch (_kind)
                {
                    case HttpConnectionKind.Http:
                    case HttpConnectionKind.Https:
                    case HttpConnectionKind.ProxyConnect:
                        (socket, stream) = await ConnectHelper.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
                        break;

                    case HttpConnectionKind.Proxy:
                        (socket, stream) = await ConnectHelper.ConnectAsync(_proxyUri.IdnHost, _proxyUri.Port, cancellationToken).ConfigureAwait(false);
                        break;

                    case HttpConnectionKind.ProxyTunnel:
                    case HttpConnectionKind.SslProxyTunnel:
                        HttpResponseMessage response;
                        (stream, response) = await EstablishProxyTunnel(cancellationToken).ConfigureAwait(false);
                        if (response != null)
                        {
                            // Return non-success response from proxy.
                            response.RequestMessage = request;
                            return (null, response);
                        }
                        break;
                }

                TransportContext transportContext = null;
                if (_sslOptions != null)
                {
                    SslStream sslStream = await ConnectHelper.EstablishSslConnectionAsync(_sslOptions, request, stream, cancellationToken).ConfigureAwait(false);
                    stream = sslStream;
                    transportContext = sslStream.TransportContext;
                }

                HttpConnection connection = _maxConnections == int.MaxValue ?
                    new HttpConnection(this, socket, stream, transportContext) :
                    new HttpConnectionWithFinalizer(this, socket, stream, transportContext); // finalizer needed to signal the pool when a connection is dropped
                return (connection, null);
            }
            finally
            {
                cancellationWithConnectTimeout?.Dispose();
            }
        }

        // Returns the established stream or an HttpResponseMessage from the proxy indicating failure.
        private async ValueTask<(Stream, HttpResponseMessage)> EstablishProxyTunnel(CancellationToken cancellationToken)
        {
            // Send a CONNECT request to the proxy server to establish a tunnel.
            HttpRequestMessage tunnelRequest = new HttpRequestMessage(HttpMethod.Connect, _proxyUri);
            tunnelRequest.Headers.Host = $"{_host}:{_port}";    // This specifies destination host/port to connect to

            HttpResponseMessage tunnelResponse = await _poolManager.SendProxyConnectAsync(tunnelRequest, _proxyUri, cancellationToken);

            if (tunnelResponse.StatusCode != HttpStatusCode.OK)
            {
                return (null, tunnelResponse);
            }

            return (await tunnelResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), null);
        }

        /// <summary>Enqueues a waiter to the waiters list.</summary>
        /// <param name="waiter">The waiter to add.</param>
        private void EnqueueWaiter(ConnectionWaiter waiter)
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));
            Debug.Assert(waiter != null);
            Debug.Assert(waiter._next == null);
            Debug.Assert(waiter._prev == null);

            waiter._next = _waitersHead;
            if (_waitersHead != null)
            {
                _waitersHead._prev = waiter;
            }
            else
            {
                Debug.Assert(_waitersTail == null);
                _waitersTail = waiter;
            }
            _waitersHead = waiter;
        }

        /// <summary>Dequeues a waiter from the waiters list.  The list must not be empty.</summary>
        /// <returns>The dequeued waiter.</returns>
        private ConnectionWaiter DequeueWaiter()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));
            Debug.Assert(_waitersTail != null);

            ConnectionWaiter waiter = _waitersTail;
            _waitersTail = waiter._prev;

            if (_waitersTail != null)
            {
                _waitersTail._next = null;
            }
            else
            {
                Debug.Assert(_waitersHead == waiter);
                _waitersHead = null;
            }

            waiter._next = null;
            waiter._prev = null;

            return waiter;
        }

        /// <summary>Removes the specified waiter from the waiters list as part of a cancellation request.</summary>
        /// <param name="waiter">The waiter to remove.</param>
        /// <returns>true if the waiter was in the list; otherwise, false.</returns>
        private bool RemoveWaiterForCancellation(ConnectionWaiter waiter)
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));
            Debug.Assert(waiter != null);
            Debug.Assert(waiter._cancellationToken.IsCancellationRequested);

            bool inList = waiter._next != null || waiter._prev != null || _waitersHead == waiter || _waitersTail == waiter;

            if (waiter._next != null) waiter._next._prev = waiter._prev;
            if (waiter._prev != null) waiter._prev._next = waiter._next;

            if (_waitersHead == waiter && _waitersTail == waiter)
            {
                _waitersHead = _waitersTail = null;
            }
            else if (_waitersHead == waiter)
            {
                _waitersHead = waiter._next;
            }
            else if (_waitersTail == waiter)
            {
                _waitersTail = waiter._prev;
            }

            waiter._next = null;
            waiter._prev = null;

            return inList;
        }

        /// <summary>Waits for and returns the created connection, decrementing the associated connection count if it fails.</summary>
        private async ValueTask<(HttpConnection, HttpResponseMessage)> WaitForCreatedConnectionAsync(ValueTask<(HttpConnection, HttpResponseMessage)> creationTask)
        {
            try
            {
                (HttpConnection connection, HttpResponseMessage response) = await creationTask.ConfigureAwait(false);
                if (connection == null)
                {
                    DecrementConnectionCount();
                }
                return (connection, response);
            }
            catch
            {
                DecrementConnectionCount();
                throw;
            }
        }

        /// <summary>
        /// Increments the count of connections associated with the pool.  This is invoked
        /// any time a new connection is created for the pool.
        /// </summary>
        public void IncrementConnectionCount()
        {
            lock (SyncObj) IncrementConnectionCountNoLock();
        }

        private void IncrementConnectionCountNoLock()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj), $"Expected to be holding {nameof(SyncObj)}");

            if (NetEventSource.IsEnabled) Trace(null);
            _usedSinceLastCleanup = true;

            Debug.Assert(
                _associatedConnectionCount >= 0 && _associatedConnectionCount < _maxConnections,
                $"Expected 0 <= {_associatedConnectionCount} < {_maxConnections}");
            _associatedConnectionCount++;
        }

        /// <summary>
        /// Decrements the number of connections associated with the pool.
        /// If there are waiters on the pool due to having reached the maximum,
        /// this will instead try to transfer the count to one of them.
        /// </summary>
        public void DecrementConnectionCount()
        {
            if (NetEventSource.IsEnabled) Trace(null);
            lock (SyncObj)
            {
                Debug.Assert(_associatedConnectionCount > 0 && _associatedConnectionCount <= _maxConnections,
                    $"Expected 0 < {_associatedConnectionCount} <= {_maxConnections}");

                // Mark the pool as not being stale.
                _usedSinceLastCleanup = true;

                if (_waitersHead == null)
                {
                    // There are no waiters to which the count should logically be transferred,
                    // so simply decrement the count.
                    _associatedConnectionCount--;
                }
                else
                {
                    // There's at least one waiter to which we should try to logically transfer
                    // the associated count.  Get the waiter.
                    Debug.Assert(_idleConnections.Count == 0, $"With {_idleConnections} connections, we shouldn't have a waiter.");
                    ConnectionWaiter waiter = DequeueWaiter();
                    Debug.Assert(waiter != null, "Expected non-null waiter");
                    Debug.Assert(waiter.Task.Status == TaskStatus.WaitingForActivation, $"Expected {waiter.Task.Status} == {nameof(TaskStatus.WaitingForActivation)}");
                    waiter._cancellationTokenRegistration.Dispose();

                    // Having a waiter means there must not be any idle connections, so we need to create
                    // one, and we do so using the logic associated with the waiter.
                    ValueTask<(HttpConnection, HttpResponseMessage)> connectionTask = waiter.CreateConnectionAsync();
                    if (connectionTask.IsCompletedSuccessfully)
                    {
                        // We synchronously and successfully created a connection (this is rare).
                        // Transfer the connection to the waiter.  Since we already have a count
                        // that's inflated due to the connection being disassociated, we don't
                        // need to change the count here.
                        waiter.SetResult(connectionTask.Result);
                    }
                    else
                    {
                        // We initiated a connection creation.  When it completes, transfer the result to the waiter.
                        connectionTask.AsTask().ContinueWith((innerConnectionTask, state) =>
                        {
                            var innerWaiter = (ConnectionWaiter)state;
                            try
                            {
                                // Get the resulting connection.
                                (HttpConnection result, HttpResponseMessage response) = innerConnectionTask.GetAwaiter().GetResult();

                                if (response != null)
                                {
                                    // Proxy tunnel connect failed, so decrement the connection count.
                                    innerWaiter._pool.DecrementConnectionCount();
                                }

                                // Store the resulting connection into the waiter. As in the synchronous case,
                                // since we already have a count that's inflated due to the connection being
                                // disassociated, we don't need to change the count here.
                                innerWaiter.SetResult(innerConnectionTask.Result);
                            }
                            catch (Exception e)
                            {
                                // The creation operation failed.  Store the exception into the waiter.
                                innerWaiter.SetException(e);

                                // At this point, a connection was dropped and we failed to replace it,
                                // which means our connection count still needs to be decremented.
                                innerWaiter._pool.DecrementConnectionCount();
                            }
                        }, waiter, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    }
                }
            }
        }
        
        /// <summary>Returns the connection to the pool for subsequent reuse.</summary>
        /// <param name="connection">The connection to return.</param>
        public void ReturnConnection(HttpConnection connection)
        {
            List<CachedConnection> list = _idleConnections;
            lock (SyncObj)
            {
                Debug.Assert(list.Count <= _maxConnections, $"Expected {list.Count} <= {_maxConnections}");

                // Mark the pool as still being active.
                _usedSinceLastCleanup = true;

                // If there's someone waiting for a connection, simply
                // transfer this one to them rather than pooling it.
                if (_waitersTail != null && !connection.EnsureReadAheadAndPollRead())
                {
                    ConnectionWaiter waiter = DequeueWaiter();
                    waiter._cancellationTokenRegistration.Dispose();

                    if (NetEventSource.IsEnabled) connection.Trace("Transferring connection returned to pool.");
                    waiter.SetResult((connection, null));

                    return;
                }

                // If the pool has been disposed of, dispose the connection being returned,
                // as the pool is being deactivated. We do this after the above in order to
                // use pooled connections to satisfy any requests that pended before the
                // the pool was disposed of.
                if (_disposed)
                {
                    if (NetEventSource.IsEnabled) connection.Trace("Disposing connection returned to disposed pool.");
                    connection.Dispose();
                    return;
                }

                // Pool the connection by adding it to the list.
                list.Add(new CachedConnection(connection));
                if (NetEventSource.IsEnabled) connection.Trace("Stored connection in pool.");
            }
        }

        /// <summary>Disposes the </summary>
        public void Dispose()
        {
            List<CachedConnection> list = _idleConnections;
            lock (SyncObj)
            {
                if (!_disposed)
                {
                    if (NetEventSource.IsEnabled) Trace("Disposing pool.");
                    _disposed = true;
                    list.ForEach(c => c._connection.Dispose());
                    list.Clear();
                }
                Debug.Assert(list.Count == 0, $"Expected {nameof(list)}.{nameof(list.Count)} == 0");
            }
        }

        /// <summary>
        /// Removes any unusable connections from the pool, and if the pool
        /// is then empty and stale, disposes of it.
        /// </summary>
        /// <returns>
        /// true if the pool disposes of itself; otherwise, false.
        /// </returns>
        public bool CleanCacheAndDisposeIfUnused()
        {
            TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
            TimeSpan pooledConnectionIdleTimeout = _poolManager.Settings._pooledConnectionIdleTimeout;

            List<CachedConnection> list = _idleConnections;
            List<HttpConnection> toDispose = null;
            bool tookLock = false;
            try
            {
                if (NetEventSource.IsEnabled) Trace("Cleaning pool.");
                Monitor.Enter(SyncObj, ref tookLock);

                // Get the current time.  This is compared against each connection's last returned
                // time to determine whether a connection is too old and should be closed.
                DateTimeOffset now = DateTimeOffset.Now;

                // Find the first item which needs to be removed.
                int freeIndex = 0;
                while (freeIndex < list.Count && list[freeIndex].IsUsable(now, pooledConnectionLifetime, pooledConnectionIdleTimeout, poll: true))
                {
                    freeIndex++;
                }

                // If freeIndex == list.Count, nothing needs to be removed.
                // But if it's < list.Count, at least one connection needs to be purged.
                if (freeIndex < list.Count)
                {
                    // We know the connection at freeIndex is unusable, so dispose of it.
                    toDispose = new List<HttpConnection> { list[freeIndex]._connection };

                    // Find the first item after the one to be removed that should be kept.
                    int current = freeIndex + 1;
                    while (current < list.Count)
                    {
                        // Look for the first item to be kept.  Along the way, any
                        // that shouldn't be kept are disposed of.
                        while (current < list.Count && !list[current].IsUsable(now, pooledConnectionLifetime, pooledConnectionIdleTimeout, poll: true))
                        {
                            toDispose.Add(list[current]._connection);
                            current++;
                        }

                        // If we found something to keep, copy it down to the known free slot.
                        if (current < list.Count)
                        {
                            // copy item to the free slot
                            list[freeIndex++] = list[current++];
                        }

                        // Keep going until there are no more good items.
                    }

                    // At this point, good connections have been moved below freeIndex, and garbage connections have
                    // been added to the dispose list, so clear the end of the list past freeIndex.
                    list.RemoveRange(freeIndex, list.Count - freeIndex);

                    // If there are now no connections associated with this pool, we can dispose of it. We
                    // avoid aggressively cleaning up pools that have recently been used but currently aren't;
                    // if a pool was used since the last time we cleaned up, give it another chance. New pools
                    // start out saying they've recently been used, to give them a bit of breathing room and time
                    // for the initial collection to be added to it.
                    if (_associatedConnectionCount == 0 && !_usedSinceLastCleanup)
                    {
                        Debug.Assert(list.Count == 0, $"Expected {nameof(list)}.{nameof(list.Count)} == 0");
                        _disposed = true;
                        return true; // Pool is disposed of.  It should be removed.
                    }
                }

                // Reset the cleanup flag.  Any pools that are empty and not used since the last cleanup
                // will be purged next time around.
                _usedSinceLastCleanup = false;
            }
            finally
            {
                if (tookLock)
                {
                    Monitor.Exit(SyncObj);
                }

                // Dispose the stale connections outside the pool lock.
                toDispose?.ForEach(c => c.Dispose());
            }

            // Pool is active.  Should not be removed.
            return false;
        }

        // For diagnostic purposes
        public override string ToString() =>
            $"{nameof(HttpConnectionPool)}" +
            (_proxyUri == null ?
                (_sslOptions == null ?
                    $"http://{_host}:{_port}" :
                    $"https://{_host}:{_port}" + (_sslOptions.TargetHost != _host ? $", SSL TargetHost={_sslOptions.TargetHost}" : null)) :
                (_sslOptions == null ?
                    $"Proxy {_proxyUri}" :
                    $"https://{_host}:{_port}/ tunnelled via Proxy {_proxyUri}" + (_sslOptions.TargetHost != _host ? $", SSL TargetHost={_sslOptions.TargetHost}" : null)));

        private void Trace(string message, [CallerMemberName] string memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                GetHashCode(),               // pool ID
                0,                           // connection ID
                0,                           // request ID
                memberName,                  // method name
                ToString() + ":" + message); // message

        /// <summary>A cached idle connection and metadata about it.</summary>
        [StructLayout(LayoutKind.Auto)]
        private readonly struct CachedConnection : IEquatable<CachedConnection>
        {
            /// <summary>The cached connection.</summary>
            internal readonly HttpConnection _connection;
            /// <summary>The last time the connection was used.</summary>
            internal readonly DateTimeOffset _returnedTime;

            /// <summary>Initializes the cached connection and its associated metadata.</summary>
            /// <param name="connection">The connection.</param>
            public CachedConnection(HttpConnection connection)
            {
                Debug.Assert(connection != null);
                _connection = connection;
                _returnedTime = DateTimeOffset.UtcNow;
            }

            /// <summary>Gets whether the connection is currently usable.</summary>
            /// <param name="now">The current time.  Passed in to amortize the cost of calling DateTime.UtcNow.</param>
            /// <param name="pooledConnectionLifetime">How long a connection can be open to be considered reusable.</param>
            /// <param name="pooledConnectionIdleTimeout">How long a connection can have been idle in the pool to be considered reusable.</param>
            /// <returns>
            /// true if we believe the connection can be reused; otherwise, false.  There is an inherent race condition here,
            /// in that the server could terminate the connection or otherwise make it unusable immediately after we check it,
            /// but there's not much difference between that and starting to use the connection and then having the server
            /// terminate it, which would be considered a failure, so this race condition is largely benign and inherent to
            /// the nature of connection pooling.
            /// </returns>
            public bool IsUsable(
                DateTimeOffset now,
                TimeSpan pooledConnectionLifetime,
                TimeSpan pooledConnectionIdleTimeout,
                bool poll = false)
            {
                // Validate that the connection hasn't been idle in the pool for longer than is allowed.
                if ((pooledConnectionIdleTimeout != Timeout.InfiniteTimeSpan) && (now - _returnedTime > pooledConnectionIdleTimeout))
                {
                    if (NetEventSource.IsEnabled) _connection.Trace($"Connection no longer usable. Idle {now - _returnedTime} > {pooledConnectionIdleTimeout}.");
                    return false;
                }

                // Validate that the connection hasn't been alive for longer than is allowed.
                if ((pooledConnectionLifetime != Timeout.InfiniteTimeSpan) && (now - _connection.CreationTime > pooledConnectionLifetime))
                {
                    if (NetEventSource.IsEnabled) _connection.Trace($"Connection no longer usable. Alive {now - _connection.CreationTime} > {pooledConnectionLifetime}.");
                    return false;
                }

                // Validate that the connection hasn't received any stray data while in the pool.
                if (poll && _connection.PollRead())
                {
                    if (NetEventSource.IsEnabled) _connection.Trace($"Connection no longer usable. Unexpected data received.");
                    return false;
                }

                // The connection is usable.
                return true;
            }

            public bool Equals(CachedConnection other) => ReferenceEquals(other._connection, _connection);
            public override bool Equals(object obj) => obj is CachedConnection && Equals((CachedConnection)obj);
            public override int GetHashCode() => _connection?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Provides a waiter object that's used when we've reached the limit on connections
        /// associated with the pool.  When a connection is available or created, it's stored
        /// into the waiter as a result, and if no connection is available from the pool,
        /// this waiter's logic is used to create the connection.
        /// </summary>
        private class ConnectionWaiter : TaskCompletionSource<(HttpConnection, HttpResponseMessage)>
        {
            /// <summary>The pool with which this waiter is associated.</summary>
            internal readonly HttpConnectionPool _pool;
            /// <summary>Request to use to create the connection.</summary>
            private readonly HttpRequestMessage _request;

            /// <summary>Cancellation token for the waiter.</summary>
            internal readonly CancellationToken _cancellationToken;
            /// <summary>Registration that removes the waiter from the registration list.</summary>
            internal CancellationTokenRegistration _cancellationTokenRegistration;
            /// <summary>Next waiter in the list.</summary>
            internal ConnectionWaiter _next;
            /// <summary>Previous waiter in the list.</summary>
            internal ConnectionWaiter _prev;

            /// <summary>Initializes the waiter.</summary>
            public ConnectionWaiter(HttpConnectionPool pool, HttpRequestMessage request, CancellationToken cancellationToken) : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                Debug.Assert(pool != null, "Expected non-null pool");
                _pool = pool;
                _request = request;
                _cancellationToken = cancellationToken;
            }

            /// <summary>Creates a connection.</summary>
            public ValueTask<(HttpConnection, HttpResponseMessage)> CreateConnectionAsync()
            {
                try
                {
                    return _pool.CreateConnectionAsync(_request, _cancellationToken);
                }
                catch (Exception e)
                {
                    return new ValueTask<(HttpConnection, HttpResponseMessage)>(Threading.Tasks.Task.FromException<(HttpConnection, HttpResponseMessage)>(e));
                }
            }
        }
    }
}
