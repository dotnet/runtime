// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnectionPool
    {
        /// <summary>List of available HTTP/2 connections stored in the pool.</summary>
        private List<Http2Connection>? _availableHttp2Connections;
        /// <summary>The number of HTTP/2 connections associated with the pool, including in use, available, and pending.</summary>
        private int _associatedHttp2ConnectionCount;
        /// <summary>Indicates whether an HTTP/2 connection is in the process of being established.</summary>
        private bool _pendingHttp2Connection;
        /// <summary>Queue of requests waiting for an HTTP/2 connection.</summary>
        private RequestQueue<Http2Connection?> _http2RequestQueue;

        private bool _http2Enabled;
        private byte[]? _http2AltSvcOriginUri;
        internal readonly byte[]? _http2EncodedAuthorityHostHeader;

        /// <summary>
        /// An ASCII origin string per RFC 6454 Section 6.2, in format &lt;scheme&gt;://&lt;host&gt;[:&lt;port&gt;]
        /// </summary>
        /// <remarks>
        /// Used by <see cref="Http2Connection"/> to test ALTSVC frames for our origin.
        /// </remarks>
        public byte[] Http2AltSvcOriginUri
        {
            get
            {
                if (_http2AltSvcOriginUri == null)
                {
                    var sb = new StringBuilder();

                    sb.Append(IsSecure ? "https://" : "http://")
                      .Append(_originAuthority.IdnHost);

                    if (!IsDefaultPort)
                    {
                        sb.Append(CultureInfo.InvariantCulture, $":{_originAuthority.Port}");
                    }

                    _http2AltSvcOriginUri = Encoding.ASCII.GetBytes(sb.ToString());
                }

                return _http2AltSvcOriginUri;
            }
        }

        private bool EnableMultipleHttp2Connections => _poolManager.Settings.EnableMultipleHttp2Connections;

        private bool TryGetPooledHttp2Connection(HttpRequestMessage request, [NotNullWhen(true)] out Http2Connection? connection, out HttpConnectionWaiter<Http2Connection?>? waiter)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel || _kind == HttpConnectionKind.Http || _kind == HttpConnectionKind.SocksTunnel || _kind == HttpConnectionKind.SslSocksTunnel);

            // Look for a usable connection.
            while (true)
            {
                lock (SyncObj)
                {
                    if (!_http2Enabled)
                    {
                        waiter = null;
                        connection = null;
                        return false;
                    }

                    int availableConnectionCount = _availableHttp2Connections?.Count ?? 0;
                    if (availableConnectionCount > 0)
                    {
                        // We have a connection that we can attempt to use.
                        // Validate it below outside the lock, to avoid doing expensive operations while holding the lock.
                        connection = _availableHttp2Connections![availableConnectionCount - 1];
                    }
                    else
                    {
                        // No available connections. Add to the request queue.
                        waiter = _http2RequestQueue.EnqueueRequest(request);

                        CheckForHttp2ConnectionInjection();

                        // There were no available connections. This request has been added to the request queue.
                        if (NetEventSource.Log.IsEnabled()) Trace($"No available HTTP/2 connections; request queued.");
                        connection = null;
                        return false;
                    }
                }

                if (CheckExpirationOnGet(connection))
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Found expired HTTP/2 connection in pool.");

                    InvalidateHttp2Connection(connection);
                    continue;
                }

                if (!connection.TryReserveStream())
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Found HTTP/2 connection in pool without available streams.");

                    bool found = false;
                    lock (SyncObj)
                    {
                        int index = _availableHttp2Connections.IndexOf(connection);
                        if (index != -1)
                        {
                            found = true;
                            _availableHttp2Connections.RemoveAt(index);
                        }
                    }

                    // If we didn't find the connection, then someone beat us to removing it (or it shut down)
                    if (found)
                    {
                        DisableHttp2Connection(connection);
                    }
                    continue;
                }

                if (NetEventSource.Log.IsEnabled()) connection.Trace("Found usable HTTP/2 connection in pool.");
                waiter = null;
                return true;
            }
        }

        private void CheckForHttp2ConnectionInjection()
        {
            Debug.Assert(HasSyncObjLock);

            _http2RequestQueue.PruneCompletedRequestsFromHeadOfQueue(this);

            // Determine if we can and should add a new connection to the pool.
            int availableHttp2ConnectionCount = _availableHttp2Connections?.Count ?? 0;
            bool willInject = availableHttp2ConnectionCount == 0 &&                         // No available connections
                !_pendingHttp2Connection &&                                                 // Only allow one pending HTTP2 connection at a time
                _http2RequestQueue.Count > 0 &&                                             // There are requests left on the queue
                (_associatedHttp2ConnectionCount == 0 || EnableMultipleHttp2Connections) && // We allow multiple connections, or don't have a connection currently
                _http2RequestQueue.RequestsWithoutAConnectionAttempt > 0;                   // There are requests we haven't issued a connection attempt for

            if (NetEventSource.Log.IsEnabled())
            {
                Trace($"Available HTTP/2.0 connections: {availableHttp2ConnectionCount}, " +
                    $"Pending HTTP/2.0 connection: {_pendingHttp2Connection}, " +
                    $"Requests in the queue: {_http2RequestQueue.Count}, " +
                    $"Requests without a connection attempt: {_http2RequestQueue.RequestsWithoutAConnectionAttempt}, " +
                    $"Total associated HTTP/2.0 connections: {_associatedHttp2ConnectionCount}, " +
                    $"Will inject connection: {willInject}.");
            }

            if (willInject)
            {
                _associatedHttp2ConnectionCount++;
                _pendingHttp2Connection = true;

                RequestQueue<Http2Connection?>.QueueItem queueItem = _http2RequestQueue.PeekNextRequestForConnectionAttempt();
                _ = InjectNewHttp2ConnectionAsync(queueItem); // ignore returned task
            }
        }

        private async Task InjectNewHttp2ConnectionAsync(RequestQueue<Http2Connection?>.QueueItem queueItem)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("Creating new HTTP/2 connection for pool.");

            // Queue the remainder of the work so that this method completes quickly
            // and escapes locks held by the caller.
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            Http2Connection? connection = null;
            Exception? connectionException = null;
            HttpConnectionWaiter<Http2Connection?> waiter = queueItem.Waiter;

            CancellationTokenSource cts = GetConnectTimeoutCancellationTokenSource();
            waiter.ConnectionCancellationTokenSource = cts;
            try
            {
                (Stream stream, TransportContext? transportContext, IPEndPoint? remoteEndPoint) = await ConnectAsync(queueItem.Request, true, cts.Token).ConfigureAwait(false);

                if (IsSecure)
                {
                    SslStream sslStream = (SslStream)stream;

                    if (sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
                    {
                        // The server accepted our request for HTTP2.

                        if (sslStream.SslProtocol < SslProtocols.Tls12)
                        {
                            stream.Dispose();
                            connectionException = new HttpRequestException(SR.Format(SR.net_ssl_http2_requires_tls12, sslStream.SslProtocol));
                        }
                        else
                        {
                            connection = await ConstructHttp2ConnectionAsync(stream, queueItem.Request, remoteEndPoint, cts.Token).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // We established an SSL connection, but the server denied our request for HTTP2.
                        await HandleHttp11Downgrade(queueItem.Request, stream, transportContext, remoteEndPoint, cts.Token).ConfigureAwait(false);
                        return;
                    }
                }
                else
                {
                    connection = await ConstructHttp2ConnectionAsync(stream, queueItem.Request, remoteEndPoint, cts.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                connectionException = e is OperationCanceledException oce && oce.CancellationToken == cts.Token && !waiter.CancelledByOriginatingRequestCompletion ?
                    CreateConnectTimeoutException(oce) :
                    e;
            }
            finally
            {
                lock (waiter)
                {
                    waiter.ConnectionCancellationTokenSource = null;
                    cts.Dispose();
                }
            }

            if (connection is not null)
            {
                // Add the new connection to the pool.
                ReturnHttp2Connection(connection, isNewConnection: true, queueItem.Waiter);
            }
            else
            {
                Debug.Assert(connectionException is not null);
                HandleHttp2ConnectionFailure(waiter, connectionException);
            }
        }

        private async ValueTask<Http2Connection> ConstructHttp2ConnectionAsync(Stream stream, HttpRequestMessage request, IPEndPoint? remoteEndPoint, CancellationToken cancellationToken)
        {
            stream = await ApplyPlaintextFilterAsync(async: true, stream, HttpVersion.Version20, request, cancellationToken).ConfigureAwait(false);

            Http2Connection http2Connection = new Http2Connection(this, stream, remoteEndPoint);
            try
            {
                await http2Connection.SetupAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Note, SetupAsync will dispose the connection if there is an exception.
                if (e is OperationCanceledException oce && oce.CancellationToken == cancellationToken)
                {
                    // Note, AddHttp2ConnectionAsync handles this OCE separately so don't wrap it.
                    throw;
                }

                throw new HttpRequestException(SR.net_http_client_execution_error, e);
            }

            return http2Connection;
        }

        private void HandleHttp2ConnectionFailure(HttpConnectionWaiter<Http2Connection?> requestWaiter, Exception e)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"HTTP2 connection failed: {e}");

            // We don't care if this fails; that means the request was previously canceled or handled by a different connection.
            requestWaiter.TrySetException(e);

            lock (SyncObj)
            {
                Debug.Assert(_associatedHttp2ConnectionCount > 0);
                Debug.Assert(_pendingHttp2Connection);

                _associatedHttp2ConnectionCount--;
                _pendingHttp2Connection = false;

                CheckForHttp2ConnectionInjection();
            }
        }

        private async Task HandleHttp11Downgrade(HttpRequestMessage request, Stream stream, TransportContext? transportContext, IPEndPoint? remoteEndPoint, CancellationToken cancellationToken)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("Server does not support HTTP2; disabling HTTP2 use and proceeding with HTTP/1.1 connection");

            bool canUse = true;
            HttpConnectionWaiter<Http2Connection?>? waiter = null;
            lock (SyncObj)
            {
                Debug.Assert(_pendingHttp2Connection);
                Debug.Assert(_associatedHttp2ConnectionCount > 0);

                // Server does not support HTTP2. Disable further HTTP2 attempts.
                _http2Enabled = false;
                _associatedHttp2ConnectionCount--;
                _pendingHttp2Connection = false;

                if (_associatedHttp11ConnectionCount < _maxHttp11Connections)
                {
                    _associatedHttp11ConnectionCount++;
                    _pendingHttp11ConnectionCount++;
                }
                else
                {
                    // We are already at the limit for HTTP/1.1 connections, so do not proceed with this connection.
                    canUse = false;
                }

                _http2RequestQueue.TryDequeueWaiter(this, out waiter);
            }

            // Signal to any queued HTTP2 requests that they must downgrade.
            while (waiter is not null)
            {
                if (NetEventSource.Log.IsEnabled()) Trace("Downgrading queued HTTP2 request to HTTP/1.1");

                // We are done with the HTTP2 connection attempt, no point to cancel it.
                Volatile.Write(ref waiter.ConnectionCancellationTokenSource, null);

                // We don't care if this fails; that means the request was previously canceled or handled by a different connection.
                waiter.TrySetResult(null);

                lock (SyncObj)
                {
                    _http2RequestQueue.TryDequeueWaiter(this, out waiter);
                }
            }

            if (!canUse)
            {
                if (NetEventSource.Log.IsEnabled()) Trace("Discarding downgraded HTTP/1.1 connection because HTTP/1.1 connection limit is exceeded");
                stream.Dispose();
                return;
            }

            HttpConnection http11Connection;
            try
            {
                // Note, the same CancellationToken from the original HTTP2 connection establishment still applies here.
                http11Connection = await ConstructHttp11ConnectionAsync(true, stream, transportContext, request, remoteEndPoint, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
            {
                HandleHttp11ConnectionFailure(requestWaiter: null, CreateConnectTimeoutException(oce));
                return;
            }
            catch (Exception e)
            {
                HandleHttp11ConnectionFailure(requestWaiter: null, e);
                return;
            }

            AddNewHttp11Connection(http11Connection, initialRequestWaiter: null);
        }

        private void ReturnHttp2Connection(Http2Connection connection, bool isNewConnection, HttpConnectionWaiter<Http2Connection?>? initialRequestWaiter = null)
        {
            if (NetEventSource.Log.IsEnabled()) connection.Trace($"{nameof(isNewConnection)}={isNewConnection}");

            Debug.Assert(!HasSyncObjLock);
            Debug.Assert(isNewConnection || initialRequestWaiter is null, "Shouldn't have a request unless the connection is new");

            if (!isNewConnection && CheckExpirationOnReturn(connection))
            {
                lock (SyncObj)
                {
                    Debug.Assert(_availableHttp2Connections is null || !_availableHttp2Connections.Contains(connection));
                    Debug.Assert(_associatedHttp2ConnectionCount > (_availableHttp2Connections?.Count ?? 0));
                    _associatedHttp2ConnectionCount--;
                }

                if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP2 connection return to pool. Connection lifetime expired.");
                connection.Dispose();
                return;
            }

            while (connection.TryReserveStream())
            {
                // Loop in case we get a request that has already been canceled or handled by a different connection.
                while (true)
                {
                    HttpConnectionWaiter<Http2Connection?>? waiter = null;
                    bool added = false;
                    lock (SyncObj)
                    {
                        Debug.Assert(_availableHttp2Connections is null || !_availableHttp2Connections.Contains(connection), $"HTTP2 connection already in available list");
                        Debug.Assert(_associatedHttp2ConnectionCount > (_availableHttp2Connections?.Count ?? 0),
                            $"Expected _associatedHttp2ConnectionCount={_associatedHttp2ConnectionCount} > _availableHttp2Connections.Count={(_availableHttp2Connections?.Count ?? 0)}");

                        if (isNewConnection)
                        {
                            Debug.Assert(_pendingHttp2Connection);
                            _pendingHttp2Connection = false;
                            isNewConnection = false;
                        }

                        if (initialRequestWaiter is not null)
                        {
                            // Try to handle the request that we initiated the connection for first
                            waiter = initialRequestWaiter;
                            initialRequestWaiter = null;

                            // If this method found a request to service, that request must be removed from the queue if it was at the head to avoid rooting it forever.
                            // Normally, TryDequeueWaiter would handle the removal. TryDequeueSpecificWaiter matches this behavior for the initial request case.
                            // We don't care if this fails; that means the request was previously canceled, handled by a different connection, or not at the head of the queue.
                            _http2RequestQueue.TryDequeueSpecificWaiter(waiter);
                        }
                        else if (_http2RequestQueue.TryDequeueWaiter(this, out waiter))
                        {
                            Debug.Assert((_availableHttp2Connections?.Count ?? 0) == 0, $"With {(_availableHttp2Connections?.Count ?? 0)} available HTTP2 connections, we shouldn't have a waiter.");
                        }
                        else if (_disposed)
                        {
                            // The pool has been disposed. We will dispose this connection below outside the lock.
                            // We do this check after processing the request queue so that any queued requests will be handled by existing connections if possible.
                            _associatedHttp2ConnectionCount--;
                        }
                        else
                        {
                            // Add connection to the pool.
                            added = true;
                            _availableHttp2Connections ??= new List<Http2Connection>();
                            _availableHttp2Connections.Add(connection);
                        }
                    }

                    if (waiter is not null)
                    {
                        Debug.Assert(!added);

                        if (waiter.TrySignal(connection))
                        {
                            break;
                        }

                        // Loop and process the queue again
                    }
                    else
                    {
                        connection.ReleaseStream();
                        if (added)
                        {
                            if (NetEventSource.Log.IsEnabled()) connection.Trace("Put HTTP2 connection in pool.");
                            return;
                        }
                        else
                        {
                            Debug.Assert(_disposed);
                            if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP2 connection returned to pool. Pool was disposed.");
                            connection.Dispose();
                            return;
                        }
                    }
                }
            }

            if (isNewConnection)
            {
                Debug.Assert(initialRequestWaiter is not null, "Expect request for a new connection");

                // The new connection could not handle even one request, either because it shut down before we could use it for any requests,
                // or because it immediately set the max concurrent streams limit to 0.
                // We don't want to get stuck in a loop where we keep trying to create new connections for the same request.
                // So, treat this as a connection failure.

                if (NetEventSource.Log.IsEnabled()) connection.Trace("New HTTP2 connection is unusable due to no available streams.");
                connection.Dispose();

                HttpRequestException hre = new HttpRequestException(SR.net_http_http2_connection_not_established);
                ExceptionDispatchInfo.SetCurrentStackTrace(hre);
                HandleHttp2ConnectionFailure(initialRequestWaiter, hre);
            }
            else
            {
                // Since we only inject one connection at a time, we may want to inject another now.
                lock (SyncObj)
                {
                    CheckForHttp2ConnectionInjection();
                }

                // We need to wait until the connection is usable again.
                DisableHttp2Connection(connection);
            }
        }

        /// <summary>
        /// Disable usage of the specified connection because it cannot handle any more streams at the moment.
        /// We will register to be notified when it can handle more streams (or becomes permanently unusable).
        /// </summary>
        private void DisableHttp2Connection(Http2Connection connection)
        {
            if (NetEventSource.Log.IsEnabled()) connection.Trace("");

            _ = DisableHttp2ConnectionAsync(connection); // ignore returned task

            async Task DisableHttp2ConnectionAsync(Http2Connection connection)
            {
                bool usable = await connection.WaitForAvailableStreamsAsync().ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

                if (NetEventSource.Log.IsEnabled()) connection.Trace($"{nameof(connection.WaitForAvailableStreamsAsync)} completed, {nameof(usable)}={usable}");

                if (usable)
                {
                    ReturnHttp2Connection(connection, isNewConnection: false);
                }
                else
                {
                    // Connection has shut down.
                    lock (SyncObj)
                    {
                        Debug.Assert(_availableHttp2Connections is null || !_availableHttp2Connections.Contains(connection));
                        Debug.Assert(_associatedHttp2ConnectionCount > 0);

                        _associatedHttp2ConnectionCount--;

                        CheckForHttp2ConnectionInjection();
                    }

                    if (NetEventSource.Log.IsEnabled()) connection.Trace("HTTP2 connection no longer usable");
                    connection.Dispose();
                }
            };
        }

        /// <summary>
        /// Called when an Http2Connection from this pool is no longer usable.
        /// </summary>
        public void InvalidateHttp2Connection(Http2Connection connection)
        {
            if (NetEventSource.Log.IsEnabled()) connection.Trace("");

            bool found = false;
            lock (SyncObj)
            {
                if (_availableHttp2Connections is not null)
                {
                    Debug.Assert(_associatedHttp2ConnectionCount >= _availableHttp2Connections.Count);

                    int index = _availableHttp2Connections.IndexOf(connection);
                    if (index != -1)
                    {
                        found = true;
                        _availableHttp2Connections.RemoveAt(index);
                        _associatedHttp2ConnectionCount--;
                    }
                }

                CheckForHttp2ConnectionInjection();
            }

            // If we found the connection in the available list, then dispose it now.
            // Otherwise, when we try to put it back in the pool, we will see it is shut down and dispose it (and adjust connection counts).
            if (found)
            {
                connection.Dispose();
            }
        }

        public void HeartBeat()
        {
            Http2Connection[]? localHttp2Connections;
            lock (SyncObj)
            {
                localHttp2Connections = _availableHttp2Connections?.ToArray();
            }

            // Avoid calling HeartBeat under the lock, as it may call back into HttpConnectionPool.InvalidateHttp2Connection.
            if (localHttp2Connections is not null)
            {
                foreach (Http2Connection http2Connection in localHttp2Connections)
                {
                    http2Connection.HeartBeat();
                }
            }
        }

        private static int ScavengeHttp2ConnectionList(List<Http2Connection> list, ref List<HttpConnectionBase>? toDispose, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout)
        {
            int freeIndex = 0;
            while (freeIndex < list.Count && list[freeIndex].IsUsable(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
            {
                freeIndex++;
            }

            // If freeIndex == list.Count, nothing needs to be removed.
            // But if it's < list.Count, at least one connection needs to be purged.
            int removed = 0;
            if (freeIndex < list.Count)
            {
                // We know the connection at freeIndex is unusable, so dispose of it.
                toDispose ??= new List<HttpConnectionBase>();
                toDispose.Add(list[freeIndex]);

                // Find the first item after the one to be removed that should be kept.
                int current = freeIndex + 1;
                while (current < list.Count)
                {
                    // Look for the first item to be kept.  Along the way, any
                    // that shouldn't be kept are disposed of.
                    while (current < list.Count && !list[current].IsUsable(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
                    {
                        toDispose.Add(list[current]);
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
                removed = list.Count - freeIndex;
                list.RemoveRange(freeIndex, removed);
            }

            return removed;
        }
    }
}
