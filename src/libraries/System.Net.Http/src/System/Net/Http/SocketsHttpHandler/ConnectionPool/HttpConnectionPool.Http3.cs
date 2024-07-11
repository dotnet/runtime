// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnectionPool
    {
        /// <summary>
        /// If <see cref="_altSvcBlocklist"/> exceeds this size, Alt-Svc will be disabled entirely for <see cref="AltSvcBlocklistTimeoutInMilliseconds"/> milliseconds.
        /// This is to prevent a failing server from bloating the dictionary beyond a reasonable value.
        /// </summary>
        private const int MaxAltSvcIgnoreListSize = 8;

        /// <summary>The time, in milliseconds, that an authority should remain in <see cref="_altSvcBlocklist"/>.</summary>
        private const int AltSvcBlocklistTimeoutInMilliseconds = 10 * 60 * 1000;

        [SupportedOSPlatformGuard("linux")]
        [SupportedOSPlatformGuard("macOS")]
        [SupportedOSPlatformGuard("windows")]
        internal static bool IsHttp3Supported() => (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()) || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

        /// <summary>List of available HTTP/3 connections stored in the pool.</summary>
        private List<Http3Connection>? _availableHttp3Connections;
        /// <summary>The number of HTTP/3 connections associated with the pool, including in use, available, and pending.</summary>
        private int _associatedHttp3ConnectionCount;
        /// <summary>Indicates whether an HTTP/3 connection is in the process of being established.</summary>
        private bool _pendingHttp3Connection;
        /// <summary>Queue of requests waiting for an HTTP/3 connection.</summary>
        private RequestQueue<Http3Connection?> _http3RequestQueue;

        private bool _http3Enabled;
        internal readonly byte[]? _http3EncodedAuthorityHostHeader;

        /// <summary>Initially set to null, this can be set to enable HTTP/3 based on Alt-Svc.</summary>
        private volatile HttpAuthority? _http3Authority;

        /// <summary>A timer to expire <see cref="_http3Authority"/> and return the pool to <see cref="_originAuthority"/>. Initialized on first use.</summary>
        private Timer? _authorityExpireTimer;

        /// <summary>If true, the <see cref="_http3Authority"/> will persist across a network change. If false, it will be reset to <see cref="_originAuthority"/>.</summary>
        private bool _persistAuthority;

        /// <summary>
        /// When an Alt-Svc authority fails due to 421 Misdirected Request, it is placed in the blocklist to be ignored
        /// for <see cref="AltSvcBlocklistTimeoutInMilliseconds"/> milliseconds. Initialized on first use.
        /// </summary>
        private volatile Dictionary<HttpAuthority, Exception?>? _altSvcBlocklist;
        private CancellationTokenSource? _altSvcBlocklistTimerCancellation;
        private volatile bool _altSvcEnabled = true;

        private bool EnableMultipleHttp3Connections => _poolManager.Settings.EnableMultipleHttp3Connections;

        // Returns null if HTTP3 cannot be used.
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private async ValueTask<HttpResponseMessage?> TrySendUsingHttp3Async(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Debug.Assert(IsHttp3Supported());

            Debug.Assert(_kind == HttpConnectionKind.Https);
            Debug.Assert(_http3Enabled);

            // Loop in case we get a 421 and need to send the request to a different authority.
            while (true)
            {
                if (!TryGetHttp3Authority(request, out HttpAuthority? authority, out Exception? reasonException))
                {
                    if (reasonException is null)
                    {
                        return null;
                    }
                    ThrowGetVersionException(request, 3, reasonException);
                }

                long queueStartingTimestamp = HttpTelemetry.Log.IsEnabled() || Settings._metrics!.RequestsQueueDuration.Enabled ? Stopwatch.GetTimestamp() : 0;

                if (!TryGetPooledHttp3Connection(request, out Http3Connection? connection, out HttpConnectionWaiter<Http3Connection?>? http3ConnectionWaiter))
                {
                    using Activity? waitForConnectionActivity = ConnectionSetupDiagnostics.StartWaitForConnectionActivity(authority);
                    try
                    {
                        connection = await http3ConnectionWaiter.WaitWithCancellationAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ConnectionSetupDiagnostics.ReportError(waitForConnectionActivity, ex);
                        throw;
                    }
                }

                // Request cannot be sent over H/3 connection, try downgrade or report failure.
                // Note that if there's an H/3 suitable origin authority but is unavailable or blocked via Alt-Svc, exception is thrown instead.
                if (connection is null)
                {
                    return null;
                }

                Activity? connectionSetupActivity = connection.ConnectionSetupActivity;
                if (connectionSetupActivity is not null) ConnectionSetupDiagnostics.AddConnectionLinkToRequestActivity(connectionSetupActivity);

                HttpResponseMessage response = await connection.SendAsync(request, queueStartingTimestamp, cancellationToken).ConfigureAwait(false);

                // If an Alt-Svc authority returns 421, it means it can't actually handle the request.
                // An authority is supposed to be able to handle ALL requests to the origin, so this is a server bug.
                // In this case, we blocklist the authority and retry the request at the origin.
                if (response.StatusCode == HttpStatusCode.MisdirectedRequest && connection.Authority != _originAuthority)
                {
                    response.Dispose();
                    BlocklistAuthority(connection.Authority);
                    continue;
                }

                return response;
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private bool TryGetPooledHttp3Connection(HttpRequestMessage request, [NotNullWhen(true)] out Http3Connection? connection, [NotNullWhen(false)] out HttpConnectionWaiter<Http3Connection?>? waiter)
        {
            Debug.Assert(IsHttp3Supported());

            // Look for a usable connection.
            while (true)
            {
                lock (SyncObj)
                {
                    int availableConnectionCount = _availableHttp3Connections?.Count ?? 0;
                    if (availableConnectionCount > 0)
                    {
                        // We have a connection that we can attempt to use.
                        // Validate it below outside the lock, to avoid doing expensive operations while holding the lock.
                        connection = _availableHttp3Connections![availableConnectionCount - 1];
                    }
                    else
                    {
                        // No available connections. Add to the request queue.
                        waiter = _http3RequestQueue.EnqueueRequest(request);

                        CheckForHttp3ConnectionInjection();

                        // There were no available connections. This request has been added to the request queue.
                        if (NetEventSource.Log.IsEnabled()) Trace($"No available HTTP/3 connections; request queued.");
                        connection = null;
                        return false;
                    }
                }

                if (CheckExpirationOnGet(connection))
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Found expired HTTP/3 connection in pool.");

                    InvalidateHttp3Connection(connection);
                    continue;
                }

                // Disable and remove the connection from the pool only if we can open another.
                // If we have only single connection, use the underlying QuicConnection mechanism to wait for available streams.
                if (!connection.TryReserveStream() && EnableMultipleHttp3Connections)
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Found HTTP/3 connection in pool without available streams.");

                    bool found = false;
                    lock (SyncObj)
                    {
                        int index = _availableHttp3Connections.IndexOf(connection);
                        if (index != -1)
                        {
                            found = true;
                            _availableHttp3Connections.RemoveAt(index);
                        }
                    }

                    // If we didn't find the connection, then someone beat us to removing it (or it shut down)
                    if (found)
                    {
                        DisableHttp3Connection(connection);
                    }
                    continue;
                }

                if (NetEventSource.Log.IsEnabled()) connection.Trace("Found usable HTTP/3 connection in pool.");
                waiter = null;
                return true;
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private void CheckForHttp3ConnectionInjection()
        {
            Debug.Assert(IsHttp3Supported());

            Debug.Assert(HasSyncObjLock);

            _http3RequestQueue.PruneCompletedRequestsFromHeadOfQueue(this);

            // Determine if we can and should add a new connection to the pool.
            int availableHttp3ConnectionCount = _availableHttp3Connections?.Count ?? 0;
            bool willInject = availableHttp3ConnectionCount == 0 &&                         // No available connections
                !_pendingHttp3Connection &&                                                 // Only allow one pending HTTP3 connection at a time
                _http3RequestQueue.Count > 0 &&                                             // There are requests left on the queue
                (_associatedHttp3ConnectionCount == 0 || EnableMultipleHttp3Connections) && // We allow multiple connections, or don't have a connection currently
                _http3RequestQueue.RequestsWithoutAConnectionAttempt > 0;                   // There are requests we haven't issued a connection attempt for

            if (NetEventSource.Log.IsEnabled())
            {
                Trace($"Available HTTP/3.0 connections: {availableHttp3ConnectionCount}, " +
                    $"Pending HTTP/3.0 connection: {_pendingHttp3Connection}, " +
                    $"Requests in the queue: {_http3RequestQueue.Count}, " +
                    $"Requests without a connection attempt: {_http3RequestQueue.RequestsWithoutAConnectionAttempt}, " +
                    $"Total associated HTTP/3.0 connections: {_associatedHttp3ConnectionCount}, " +
                    $"Will inject connection: {willInject}.");
            }

            if (willInject)
            {
                _associatedHttp3ConnectionCount++;
                _pendingHttp3Connection = true;

                RequestQueue<Http3Connection?>.QueueItem queueItem = _http3RequestQueue.PeekNextRequestForConnectionAttempt();
                _ = InjectNewHttp3ConnectionAsync(queueItem); // ignore returned task
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private async Task InjectNewHttp3ConnectionAsync(RequestQueue<Http3Connection?>.QueueItem queueItem)
        {
            Debug.Assert(IsHttp3Supported());

            if (NetEventSource.Log.IsEnabled()) Trace("Creating new HTTP/3 connection for pool.");

            // Queue the remainder of the work so that this method completes quickly
            // and escapes locks held by the caller.
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            Http3Connection? connection = null;
            Exception? connectionException = null;
            HttpAuthority? authority = null;
            HttpConnectionWaiter<Http3Connection?> waiter = queueItem.Waiter;

            CancellationTokenSource cts = GetConnectTimeoutCancellationTokenSource();
            waiter.ConnectionCancellationTokenSource = cts;
            Activity? connectionSetupActivity = null;
            try
            {
                if (TryGetHttp3Authority(queueItem.Request, out authority, out Exception? reasonException))
                {
                    connectionSetupActivity = ConnectionSetupDiagnostics.StartConnectionSetupActivity(isSecure: true, authority);
                    // If the authority was sent as an option through alt-svc then include alt-used header.
                    connection = new Http3Connection(this, authority, includeAltUsedHeader: _http3Authority == authority);
                    QuicConnection quicConnection = await ConnectHelper.ConnectQuicAsync(queueItem.Request, new DnsEndPoint(authority.IdnHost, authority.Port), _poolManager.Settings._pooledConnectionIdleTimeout, _sslOptionsHttp3!, connection.StreamCapacityCallback, cts.Token).ConfigureAwait(false);
                    if (quicConnection.NegotiatedApplicationProtocol != SslApplicationProtocol.Http3)
                    {
                        await quicConnection.DisposeAsync().ConfigureAwait(false);
                        throw new HttpRequestException(HttpRequestError.ConnectionError, "QUIC connected but no HTTP/3 indicated via ALPN.", null, RequestRetryType.RetryOnConnectionFailure);
                    }
                    if (connectionSetupActivity is not null) ConnectionSetupDiagnostics.StopConnectionSetupActivity(connectionSetupActivity, null, quicConnection.RemoteEndPoint);
                    connection.InitQuicConnection(quicConnection, connectionSetupActivity);
                }
                else if (reasonException is not null)
                {
                    ThrowGetVersionException(queueItem.Request, 3, reasonException);
                }
            }
            catch (Exception e)
            {
                connectionException = e is OperationCanceledException oce && oce.CancellationToken == cts.Token && !waiter.CancelledByOriginatingRequestCompletion ?
                    CreateConnectTimeoutException(oce) :
                    e;

                Debug.Assert(connectionSetupActivity?.IsStopped is not true);
                if (connectionSetupActivity is not null) ConnectionSetupDiagnostics.StopConnectionSetupActivity(connectionSetupActivity, e, null);

                // If the connection hasn't been initialized with QuicConnection, get rid of it.
                connection?.Dispose();
                connection = null;
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
                ReturnHttp3Connection(connection, isNewConnection: true, waiter);
            }
            else
            {
                // Block list authority only if the connection attempt was not cancelled.
                if (connectionException is not null && connectionException is not OperationCanceledException && authority is not null)
                {
                    // Disables HTTP/3 until server announces it can handle it via Alt-Svc.
                    BlocklistAuthority(authority, connectionException);
                }

                HandleHttp3ConnectionFailure(waiter, connectionException);
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private void HandleHttp3ConnectionFailure(HttpConnectionWaiter<Http3Connection?> requestWaiter, Exception? e)
        {
            Debug.Assert(IsHttp3Supported());

            if (NetEventSource.Log.IsEnabled()) Trace($"HTTP3 connection failed: {e}");

            // We don't care if this fails; that means the request was previously canceled or handled by a different connection.
            if (e is null)
            {
                requestWaiter.TrySetResult(null);
            }
            else
            {
                requestWaiter.TrySetException(e);
            }

            lock (SyncObj)
            {
                Debug.Assert(_associatedHttp3ConnectionCount > 0);
                Debug.Assert(_pendingHttp3Connection);

                _associatedHttp3ConnectionCount--;
                _pendingHttp3Connection = false;

                CheckForHttp3ConnectionInjection();
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private void ReturnHttp3Connection(Http3Connection connection, bool isNewConnection, HttpConnectionWaiter<Http3Connection?>? initialRequestWaiter = null)
        {
            Debug.Assert(IsHttp3Supported());

            if (NetEventSource.Log.IsEnabled()) connection.Trace($"{nameof(isNewConnection)}={isNewConnection}");

            Debug.Assert(!HasSyncObjLock);
            Debug.Assert(isNewConnection || initialRequestWaiter is null, "Shouldn't have a request unless the connection is new");

            if (!isNewConnection && CheckExpirationOnReturn(connection))
            {
                lock (SyncObj)
                {
                    Debug.Assert(_availableHttp3Connections is null || !_availableHttp3Connections.Contains(connection));
                    Debug.Assert(_associatedHttp3ConnectionCount > (_availableHttp3Connections?.Count ?? 0));
                    _associatedHttp3ConnectionCount--;
                }

                if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP3 connection return to pool. Connection lifetime expired.");
                connection.Dispose();
                return;
            }

            bool reserved;
            while ((reserved = connection.TryReserveStream()) || !EnableMultipleHttp3Connections)
            {
                // Loop in case we get a request that has already been canceled or handled by a different connection.
                while (true)
                {
                    HttpConnectionWaiter<Http3Connection?>? waiter = null;
                    bool added = false;
                    lock (SyncObj)
                    {
                        Debug.Assert(_availableHttp3Connections is null || !_availableHttp3Connections.Contains(connection), $"HTTP3 connection already in available list");
                        Debug.Assert(_associatedHttp3ConnectionCount > (_availableHttp3Connections?.Count ?? 0),
                            $"Expected _associatedHttp3ConnectionCount={_associatedHttp3ConnectionCount} > _availableHttp3Connections.Count={(_availableHttp3Connections?.Count ?? 0)}");

                        if (isNewConnection)
                        {
                            Debug.Assert(_pendingHttp3Connection);
                            _pendingHttp3Connection = false;
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
                            _http3RequestQueue.TryDequeueSpecificWaiter(waiter);
                        }
                        else if (_http3RequestQueue.TryDequeueWaiter(this, out waiter))
                        {
                            Debug.Assert((_availableHttp3Connections?.Count ?? 0) == 0, $"With {(_availableHttp3Connections?.Count ?? 0)} available HTTP3 connections, we shouldn't have a waiter.");
                        }
                        else if (_disposed)
                        {
                            // The pool has been disposed. We will dispose this connection below outside the lock.
                            // We do this check after processing the request queue so that any queued requests will be handled by existing connections if possible.
                            _associatedHttp3ConnectionCount--;
                        }
                        else
                        {
                            // Add connection to the pool.
                            added = true;
                            _availableHttp3Connections ??= new List<Http3Connection>();
                            _availableHttp3Connections.Add(connection);
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
                        if (reserved)
                        {
                            connection.ReleaseStream();
                        }
                        if (added)
                        {
                            if (NetEventSource.Log.IsEnabled()) connection.Trace("Put HTTP3 connection in pool.");
                            return;
                        }
                        else
                        {
                            Debug.Assert(_disposed);
                            if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP3 connection returned to pool. Pool was disposed.");
                            connection.Dispose();
                            return;
                        }
                    }
                }
            }

            // Since we only inject one connection at a time, we may want to inject another now.
            lock (SyncObj)
            {
                CheckForHttp3ConnectionInjection();
            }

            // We need to wait until the connection is usable again.
            DisableHttp3Connection(connection);
        }

        /// <summary>
        /// Disable usage of the specified connection because it cannot handle any more streams at the moment.
        /// We will register to be notified when it can handle more streams (or becomes permanently unusable).
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private void DisableHttp3Connection(Http3Connection connection)
        {
            Debug.Assert(IsHttp3Supported());

            if (NetEventSource.Log.IsEnabled()) connection.Trace("");

            _ = DisableHttp3ConnectionAsync(connection); // ignore returned task

            async Task DisableHttp3ConnectionAsync(Http3Connection connection)
            {
                bool usable = await connection.WaitForAvailableStreamsAsync().ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

                if (NetEventSource.Log.IsEnabled()) connection.Trace($"{nameof(connection.WaitForAvailableStreamsAsync)} completed, {nameof(usable)}={usable}");

                if (usable)
                {
                    ReturnHttp3Connection(connection, isNewConnection: false);
                }
                else
                {
                    // Connection has shut down.
                    lock (SyncObj)
                    {
                        Debug.Assert(_availableHttp3Connections is null || !_availableHttp3Connections.Contains(connection));
                        Debug.Assert(_associatedHttp3ConnectionCount > 0);

                        _associatedHttp3ConnectionCount--;

                        CheckForHttp3ConnectionInjection();
                    }

                    if (NetEventSource.Log.IsEnabled()) connection.Trace("HTTP3 connection no longer usable");
                    connection.Dispose();
                }
            };
        }

        /// <summary>
        /// Called when an Http3Connection from this pool is no longer usable.
        /// </summary>
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        public void InvalidateHttp3Connection(Http3Connection connection)
        {
            Debug.Assert(IsHttp3Supported());

            if (NetEventSource.Log.IsEnabled()) connection.Trace("");

            bool found = false;
            lock (SyncObj)
            {
                if (_availableHttp3Connections is not null)
                {
                    Debug.Assert(_associatedHttp3ConnectionCount >= _availableHttp3Connections.Count);

                    int index = _availableHttp3Connections.IndexOf(connection);
                    if (index != -1)
                    {
                        found = true;
                        _availableHttp3Connections.RemoveAt(index);
                        _associatedHttp3ConnectionCount--;
                    }
                }

                CheckForHttp3ConnectionInjection();
            }

            // If we found the connection in the available list, then dispose it now.
            // Otherwise, when we try to put it back in the pool, we will see it is shut down and dispose it (and adjust connection counts).
            if (found)
            {
                connection.Dispose();
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private static int ScavengeHttp3ConnectionList(List<Http3Connection> list, ref List<HttpConnectionBase>? toDispose, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout)
        {
            Debug.Assert(IsHttp3Supported());

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

        private bool TryGetHttp3Authority(HttpRequestMessage request, [NotNullWhen(true)] out HttpAuthority? authority, out Exception? reasonException)
        {
            authority = _http3Authority;

            // If H3 is explicitly requested, assume pre-negotiated H3.
            if (request.Version.Major >= 3 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
            {
                authority ??= _originAuthority;
            }

            if (authority is null)
            {
                reasonException = null;
                return false;
            }

            if (IsAltSvcBlocked(authority, out reasonException))
            {
                return false;
            }

            return true;
        }


        /// <summary>Check for the Alt-Svc header, to upgrade to HTTP/3.</summary>
        private void ProcessAltSvc(HttpResponseMessage response)
        {
            if (_altSvcEnabled && response.Headers.TryGetValues(KnownHeaders.AltSvc.Descriptor, out IEnumerable<string>? altSvcHeaderValues))
            {
                HandleAltSvc(altSvcHeaderValues, response.Headers.Age);
            }
        }

        /// <summary>
        /// Inspects a collection of Alt-Svc headers to find the first eligible upgrade path.
        /// </summary>
        /// <remarks>TODO: common case will likely be a single value. Optimize for that.</remarks>
        internal void HandleAltSvc(IEnumerable<string> altSvcHeaderValues, TimeSpan? responseAge)
        {
            HttpAuthority? nextAuthority = null;
            TimeSpan nextAuthorityMaxAge = default;
            bool nextAuthorityPersist = false;

            foreach (string altSvcHeaderValue in altSvcHeaderValues)
            {
                int parseIdx = 0;

                if (AltSvcHeaderParser.Parser.TryParseValue(altSvcHeaderValue, null, ref parseIdx, out object? parsedValue))
                {
                    var value = (AltSvcHeaderValue?)parsedValue;

                    // 'clear' should be the only value present.
                    if (value == AltSvcHeaderValue.Clear)
                    {
                        lock (SyncObj)
                        {
                            ExpireAltSvcAuthority();
                            Debug.Assert(_authorityExpireTimer != null || _disposed);
                            _authorityExpireTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                            break;
                        }
                    }

                    if (nextAuthority == null && value != null && value.AlpnProtocolName == "h3")
                    {
                        var authority = new HttpAuthority(value.Host ?? _originAuthority.IdnHost, value.Port);
                        if (IsAltSvcBlocked(authority, out _))
                        {
                            // Skip authorities in our blocklist.
                            continue;
                        }

                        TimeSpan authorityMaxAge = value.MaxAge;

                        if (responseAge != null)
                        {
                            authorityMaxAge -= responseAge.GetValueOrDefault();
                        }

                        if (authorityMaxAge > TimeSpan.Zero)
                        {
                            nextAuthority = authority;
                            nextAuthorityMaxAge = authorityMaxAge;
                            nextAuthorityPersist = value.Persist;
                        }
                    }
                }
            }

            // There's a race here in checking _http3Authority outside of the lock,
            // but there's really no bad behavior if _http3Authority changes in the mean time.
            if (nextAuthority != null && !nextAuthority.Equals(_http3Authority))
            {
                // Clamp the max age to 30 days... this is arbitrary but prevents passing a too-large TimeSpan to the Timer.
                if (nextAuthorityMaxAge.Ticks > (30 * TimeSpan.TicksPerDay))
                {
                    nextAuthorityMaxAge = TimeSpan.FromTicks(30 * TimeSpan.TicksPerDay);
                }

                lock (SyncObj)
                {
                    if (_disposed)
                    {
                        // avoid creating or touching _authorityExpireTimer after disposal
                        return;
                    }

                    if (_authorityExpireTimer == null)
                    {
                        var thisRef = new WeakReference<HttpConnectionPool>(this);

                        using (ExecutionContext.SuppressFlow())
                        {
                            _authorityExpireTimer = new Timer(static o =>
                            {
                                var wr = (WeakReference<HttpConnectionPool>)o!;
                                if (wr.TryGetTarget(out HttpConnectionPool? @this))
                                {
                                    lock (@this.SyncObj)
                                    {
                                        @this.ExpireAltSvcAuthority();
                                    }
                                }
                            }, thisRef, nextAuthorityMaxAge, Timeout.InfiniteTimeSpan);
                        }
                    }
                    else
                    {
                        _authorityExpireTimer.Change(nextAuthorityMaxAge, Timeout.InfiniteTimeSpan);
                    }

                    _http3Authority = nextAuthority;
                    _persistAuthority = nextAuthorityPersist;
                }

                if (!nextAuthorityPersist)
                {
#if !ILLUMOS && !SOLARIS
                    _poolManager.StartMonitoringNetworkChanges();
#endif
                }
            }
        }

        /// <summary>
        /// Expires the current Alt-Svc authority, resetting the connection back to origin.
        /// </summary>
        private void ExpireAltSvcAuthority()
        {
            Debug.Assert(HasSyncObjLock);

            // If we ever support prenegotiated HTTP/3, this should be set to origin, not nulled out.
            _http3Authority = null;
        }

        /// <summary>
        /// Checks whether the given <paramref name="authority"/> is on the currext Alt-Svc blocklist.
        /// If it is, then it places the cause in the <paramref name="reasonException"/>
        /// </summary>
        /// <seealso cref="BlocklistAuthority" />
        private bool IsAltSvcBlocked(HttpAuthority authority, out Exception? reasonException)
        {
            if (_altSvcBlocklist != null)
            {
                lock (_altSvcBlocklist)
                {
                    return _altSvcBlocklist.TryGetValue(authority, out reasonException);
                }
            }
            reasonException = null;
            return false;
        }

        /// <summary>
        /// Blocklists an authority and resets the current authority back to origin.
        /// If the number of blocklisted authorities exceeds <see cref="MaxAltSvcIgnoreListSize"/>,
        /// Alt-Svc will be disabled entirely for a period of time.
        /// </summary>
        /// <remarks>
        /// This is called when we get a "421 Misdirected Request" from an alternate authority.
        /// A future strategy would be to retry the individual request on an older protocol, we'd want to have
        /// some logic to blocklist after some number of failures to avoid doubling our request latency.
        ///
        /// For now, the spec states alternate authorities should be able to handle ALL requests, so this
        /// is treated as an exceptional error by immediately blocklisting the authority.
        /// </remarks>
        internal void BlocklistAuthority(HttpAuthority badAuthority, Exception? exception = null)
        {
            Debug.Assert(badAuthority != null);

            Dictionary<HttpAuthority, Exception?>? altSvcBlocklist = _altSvcBlocklist;

            if (altSvcBlocklist == null)
            {
                lock (SyncObj)
                {
                    if (_disposed)
                    {
                        // avoid creating _altSvcBlocklistTimerCancellation after disposal
                        return;
                    }

                    altSvcBlocklist = _altSvcBlocklist;
                    if (altSvcBlocklist == null)
                    {
                        altSvcBlocklist = new Dictionary<HttpAuthority, Exception?>();
                        _altSvcBlocklistTimerCancellation = new CancellationTokenSource();
                        _altSvcBlocklist = altSvcBlocklist;
                    }
                }
            }

            bool added, disabled = false;

            lock (altSvcBlocklist)
            {
                added = altSvcBlocklist.TryAdd(badAuthority, exception);

                if (added && altSvcBlocklist.Count >= MaxAltSvcIgnoreListSize && _altSvcEnabled)
                {
                    _altSvcEnabled = false;
                    disabled = true;
                }
            }

            CancellationToken altSvcBlocklistTimerCt;

            lock (SyncObj)
            {
                if (_disposed)
                {
                    // avoid touching _authorityExpireTimer and _altSvcBlocklistTimerCancellation after disposal
                    return;
                }

                if (_http3Authority == badAuthority)
                {
                    ExpireAltSvcAuthority();
                    Debug.Assert(_authorityExpireTimer != null);
                    _authorityExpireTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }

                Debug.Assert(_altSvcBlocklistTimerCancellation != null);
                altSvcBlocklistTimerCt = _altSvcBlocklistTimerCancellation.Token;
            }

            if (added)
            {
                _ = Task.Delay(AltSvcBlocklistTimeoutInMilliseconds, altSvcBlocklistTimerCt)
                    .ContinueWith(t =>
                    {
                        lock (altSvcBlocklist)
                        {
                            altSvcBlocklist.Remove(badAuthority);
                        }
                    }, altSvcBlocklistTimerCt, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            if (disabled)
            {
                _ = Task.Delay(AltSvcBlocklistTimeoutInMilliseconds, altSvcBlocklistTimerCt)
                    .ContinueWith(t =>
                    {
                        _altSvcEnabled = true;
                    }, altSvcBlocklistTimerCt, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        public void OnNetworkChanged()
        {
            lock (SyncObj)
            {
                if (_http3Authority != null && _persistAuthority == false)
                {
                    ExpireAltSvcAuthority();
                    Debug.Assert(_authorityExpireTimer != null || _disposed);
                    _authorityExpireTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }
    }
}
