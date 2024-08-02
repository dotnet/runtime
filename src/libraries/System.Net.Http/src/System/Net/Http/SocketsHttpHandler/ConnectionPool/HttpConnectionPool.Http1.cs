// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class HttpConnectionPool
    {
        /// <summary>Stack of currently available HTTP/1.1 connections stored in the pool.</summary>
        private readonly ConcurrentStack<HttpConnection> _http11Connections = new();
        /// <summary>Controls whether we can use a fast path when returning connections to the pool and skip calling into <see cref="ProcessHttp11RequestQueue(HttpConnection?)"/>.</summary>
        private bool _http11RequestQueueIsEmptyAndNotDisposed;
        /// <summary>The maximum number of HTTP/1.1 connections allowed to be associated with the pool.</summary>
        private readonly int _maxHttp11Connections;
        /// <summary>The number of HTTP/1.1 connections associated with the pool, including in use, available, and pending.</summary>
        private int _associatedHttp11ConnectionCount;
        /// <summary>The number of HTTP/1.1 connections that are in the process of being established.</summary>
        private int _pendingHttp11ConnectionCount;
        /// <summary>Queue of requests waiting for an HTTP/1.1 connection.</summary>
        private RequestQueue<HttpConnection> _http11RequestQueue;

        /// <summary>For non-proxy connection pools, this is the host name in bytes; for proxies, null.</summary>
        private readonly byte[]? _hostHeaderLineBytes;

        public byte[]? HostHeaderLineBytes => _hostHeaderLineBytes;

        private bool TryGetPooledHttp11Connection(HttpRequestMessage request, bool async, [NotNullWhen(true)] out HttpConnection? connection, [NotNullWhen(false)] out HttpConnectionWaiter<HttpConnection>? waiter)
        {
            while (_http11Connections.TryPop(out connection))
            {
                if (CheckExpirationOnGet(connection))
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Found expired HTTP/1.1 connection in pool.");
                    connection.Dispose();
                    continue;
                }

                if (!connection.PrepareForReuse(async))
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Found invalid HTTP/1.1 connection in pool.");
                    connection.Dispose();
                    continue;
                }

                if (NetEventSource.Log.IsEnabled()) connection.Trace("Found usable HTTP/1.1 connection in pool.");
                waiter = null;
                return true;
            }

            // Slow path - no available connection found.
            // Push the request onto the request queue and check if we should inject a new connection.

            waiter = new HttpConnectionWaiter<HttpConnection>();

            // Technically this block under the lock could be a part of ProcessHttp11RequestQueue to avoid taking the lock twice.
            // It is kept separate to simplify that method (avoid extra arguments that are only relevant for this caller).
            lock (SyncObj)
            {
                _http11RequestQueue.EnqueueRequest(request, waiter);

                // Disable the fast path and force connections returned to the pool to check the request queue first.
                _http11RequestQueueIsEmptyAndNotDisposed = false;
            }

            // Other threads may have added a connection to the pool before we were able to
            // add the request to the queue, so we must check for an available connection again.

            ProcessHttp11RequestQueue(null);
            return false;
        }

        /// <summary>
        /// This method is called:
        /// <br/>- When returning a connection and observing that the request queue is not empty (<see cref="_http11RequestQueueIsEmptyAndNotDisposed"/> is <see langword="false"/>).
        /// <br/>- After adding a request to the queue if we fail to obtain a connection from <see cref="_http11Connections"/>.
        /// <br/>- After scavenging or disposing the pool to ensure that any pending requests are handled or connections disposed.
        /// <para>The method will attempt to match one request from the <see cref="_http11RequestQueue"/> to an available connection.
        /// The <paramref name="connection"/> can either be provided as an argument (when returning a connection to the pool), or one will be rented from <see cref="_http11Connections"/>.
        /// As we'll only process a single request, we are expecting the method to be called every time a request is enqueued, and every time a connection is returned while the request queue is not empty.</para>
        /// <para>If the <see cref="_http11RequestQueue"/> becomes empty, this method will reset the <see cref="_http11RequestQueueIsEmptyAndNotDisposed"/> flag back to <see langword="true"/>,
        /// such that returning connections will use the fast path again and skip calling into this method.</para>
        /// <para>Notably, this method will not be called on the fast path as long as we have enough connections to handle all new requests.</para>
        /// </summary>
        /// <param name="connection">The connection to use for a pending request, or return to the pool.</param>
        private void ProcessHttp11RequestQueue(HttpConnection? connection)
        {
            // Loop in case the request we try to signal was already cancelled or handled by a different connection.
            while (true)
            {
                HttpConnectionWaiter<HttpConnection>? waiter = null;

                lock (SyncObj)
                {
#if DEBUG
                    // Other threads may still interact with the connections stack. Read the count once to keep the assert message accurate.
                    int connectionCount = _http11Connections.Count;
                    Debug.Assert(_associatedHttp11ConnectionCount >= connectionCount + _pendingHttp11ConnectionCount,
                        $"Expected {_associatedHttp11ConnectionCount} >= {connectionCount} + {_pendingHttp11ConnectionCount}");
#endif
                    Debug.Assert(_associatedHttp11ConnectionCount <= _maxHttp11Connections,
                        $"Expected {_associatedHttp11ConnectionCount} <= {_maxHttp11Connections}");
                    Debug.Assert(_associatedHttp11ConnectionCount >= _pendingHttp11ConnectionCount,
                        $"Expected {_associatedHttp11ConnectionCount} >= {_pendingHttp11ConnectionCount}");

                    if (_http11RequestQueue.Count != 0)
                    {
                        if (connection is not null || _http11Connections.TryPop(out connection))
                        {
                            // If the connection is new, this check will always succeed as there is no scavenging task pending.
                            if (!connection.TryOwnScavengingTaskCompletion())
                            {
                                goto DisposeConnection;
                            }

                            // TryDequeueWaiter will prune completed requests from the head of the queue,
                            // so it's possible for it to return false even though we checked that Count != 0.
                            bool success = _http11RequestQueue.TryDequeueWaiter(this, out waiter);
                            Debug.Assert(success == waiter is not null);
                        }
                    }

                    // Update the empty queue flag now.
                    // If the request queue is now empty, returning connections will use the fast path and skip calling into this method.
                    _http11RequestQueueIsEmptyAndNotDisposed = _http11RequestQueue.Count == 0 && !_disposed;

                    if (waiter is null)
                    {
                        // We didn't find a waiter to signal, or there were no connections available.

                        if (connection is not null)
                        {
                            // A connection was provided to this method, or we rented one from the pool.
                            // Return it back to the pool since we're not going to use it yet.

                            // We're returning it while holding the lock to avoid a scenario where
                            // - thread A sees no requests are waiting in the queue (current thread)
                            // - thread B adds a request to the queue, and sees no connections are available
                            // - thread A returns the connection to the pool
                            // We'd have both a connection and a request waiting in the pool, but nothing to pair the two.

                            // The main scenario where we'll reach this branch is when we enqueue a request to the queue
                            // and set the _http11RequestQueueIsEmptyAndNotDisposed flag to false, followed by multiple
                            // returning connections observing the flag and calling into this method before we clear the flag.
                            // This should be a relatively rare case, so the added contention should be minimal.

                            // We took ownership of the scavenging task completion.
                            // If we can't return the completion (the task already completed), we must dispose the connection.
                            if (!connection.TryReturnScavengingTaskCompletionOwnership())
                            {
                                goto DisposeConnection;
                            }

                            _http11Connections.Push(connection);
                        }
                        else
                        {
                            // We may be out of available connections, check if we should inject a new one.
                            CheckForHttp11ConnectionInjection();
                        }

                        break;
                    }
                }

                Debug.Assert(connection is not null);

                if (waiter.TrySignal(connection))
                {
                    // Success. Note that we did not call connection.PrepareForReuse
                    // before signaling the waiter. This is intentional, as the fact that
                    // this method was called indicates that the connection is either new,
                    // or was just returned to the pool and is still in a good state.
                    //
                    // We must, however, take ownership of the scavenging task completion as
                    // there is a small chance that such a task was started if the connection
                    // was briefly returned to the pool.
                    return;
                }

                // The request was already cancelled or handled by a different connection.

                // We took ownership of the scavenging task completion.
                // If we can't return the completion (the task already completed), we must dispose the connection.
                if (!connection.TryReturnScavengingTaskCompletionOwnership())
                {
                    goto DisposeConnection;
                }

                // Loop again to try to find another request to signal, or return the connection.
                continue;

            DisposeConnection:
                // The scavenging task completed before we assigned a request to the connection.
                // We've received EOF/erroneous data and the connection is not usable anymore.
                // Throw it away and try again.
                connection.Dispose();
                connection = null;
            }

            if (_disposed)
            {
                // The pool is being disposed and there are no more requests to handle.
                // Clean up any idle connections still waiting in the pool.
                while (_http11Connections.TryPop(out connection))
                {
                    connection.Dispose();
                }
            }
        }

        private void CheckForHttp11ConnectionInjection()
        {
            Debug.Assert(HasSyncObjLock);

            _http11RequestQueue.PruneCompletedRequestsFromHeadOfQueue(this);

            // Determine if we can and should add a new connection to the pool.
            bool willInject =
                _http11RequestQueue.Count > _pendingHttp11ConnectionCount &&    // More requests queued than pending connections
                _associatedHttp11ConnectionCount < _maxHttp11Connections &&     // Under the connection limit
                _http11RequestQueue.RequestsWithoutAConnectionAttempt > 0;      // There are requests we haven't issued a connection attempt for

            if (NetEventSource.Log.IsEnabled())
            {
                Trace($"Available HTTP/1.1 connections: {_http11Connections.Count}, Requests in the queue: {_http11RequestQueue.Count}, " +
                    $"Requests without a connection attempt: {_http11RequestQueue.RequestsWithoutAConnectionAttempt}, " +
                    $"Pending HTTP/1.1 connections: {_pendingHttp11ConnectionCount}, Total associated HTTP/1.1 connections: {_associatedHttp11ConnectionCount}, " +
                    $"Max HTTP/1.1 connection limit: {_maxHttp11Connections}, " +
                    $"Will inject connection: {willInject}.");
            }

            if (willInject)
            {
                _associatedHttp11ConnectionCount++;
                _pendingHttp11ConnectionCount++;

                RequestQueue<HttpConnection>.QueueItem queueItem = _http11RequestQueue.PeekNextRequestForConnectionAttempt();
                _ = InjectNewHttp11ConnectionAsync(queueItem); // ignore returned task
            }
        }

        private async Task InjectNewHttp11ConnectionAsync(RequestQueue<HttpConnection>.QueueItem queueItem)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("Creating new HTTP/1.1 connection for pool.");

            // Queue the remainder of the work so that this method completes quickly
            // and escapes locks held by the caller.
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            HttpConnectionWaiter<HttpConnection> waiter = queueItem.Waiter;
            HttpConnection? connection = null;
            Exception? connectionException = null;

            CancellationTokenSource cts = GetConnectTimeoutCancellationTokenSource();
            waiter.ConnectionCancellationTokenSource = cts;
            try
            {
                connection = await CreateHttp11ConnectionAsync(queueItem.Request, true, cts.Token).ConfigureAwait(false);
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
                // Add the established connection to the pool.
                AddNewHttp11Connection(connection, queueItem.Waiter);
            }
            else
            {
                Debug.Assert(connectionException is not null);
                HandleHttp11ConnectionFailure(waiter, connectionException);
            }
        }

        internal async ValueTask<HttpConnection> CreateHttp11ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            (Stream stream, TransportContext? transportContext, Activity? activity,  IPEndPoint? remoteEndPoint) = await ConnectAsync(request, async, cancellationToken).ConfigureAwait(false);
            return await ConstructHttp11ConnectionAsync(async, stream, transportContext, request, activity, remoteEndPoint, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<HttpConnection> ConstructHttp11ConnectionAsync(bool async, Stream stream, TransportContext? transportContext, HttpRequestMessage request, Activity? activity, IPEndPoint? remoteEndPoint, CancellationToken cancellationToken)
        {
            Stream newStream = await ApplyPlaintextFilterAsync(async, stream, HttpVersion.Version11, request, cancellationToken).ConfigureAwait(false);
            return new HttpConnection(this, newStream, transportContext, activity, remoteEndPoint);
        }

        private void HandleHttp11ConnectionFailure(HttpConnectionWaiter<HttpConnection>? requestWaiter, Exception e)
        {
            if (NetEventSource.Log.IsEnabled()) Trace($"HTTP/1.1 connection failed: {e}");

            // If this is happening as part of an HTTP/2 => HTTP/1.1 downgrade, we won't have an HTTP/1.1 waiter associated with this request
            // We don't care if this fails; that means the request was previously canceled or handled by a different connection.
            requestWaiter?.TrySetException(e);

            lock (SyncObj)
            {
                Debug.Assert(_associatedHttp11ConnectionCount > 0);
                Debug.Assert(_pendingHttp11ConnectionCount > 0);

                _associatedHttp11ConnectionCount--;
                _pendingHttp11ConnectionCount--;

                CheckForHttp11ConnectionInjection();
            }
        }

        public void RecycleHttp11Connection(HttpConnection connection)
        {
            if (CheckExpirationOnReturn(connection))
            {
                if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP/1.1 connection when returning to pool. Connection lifetime expired.");
                connection.Dispose();
                return;
            }

            ReturnHttp11Connection(connection);
        }

        private void AddNewHttp11Connection(HttpConnection connection, HttpConnectionWaiter<HttpConnection>? initialRequestWaiter)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("");

            lock (SyncObj)
            {
                Debug.Assert(_pendingHttp11ConnectionCount > 0);
                _pendingHttp11ConnectionCount--;

                if (initialRequestWaiter is not null)
                {
                    // If we're about to signal the initial waiter, that request must be removed from the queue if it was at the head to avoid rooting it forever.
                    // Normally, TryDequeueWaiter would handle the removal. TryDequeueSpecificWaiter matches this behavior for the initial request case.
                    // We don't care if this fails; that means the request was previously canceled, handled by a different connection, or not at the head of the queue.
                    _http11RequestQueue.TryDequeueSpecificWaiter(initialRequestWaiter);

                    // There's no need for us to hold the lock while signaling the waiter.
                }
            }

            if (initialRequestWaiter is not null &&
                initialRequestWaiter.TrySignal(connection))
            {
                return;
            }

            ReturnHttp11Connection(connection);
        }

        private void ReturnHttp11Connection(HttpConnection connection)
        {
            connection.MarkConnectionAsIdle();

            // The fast path when there are enough connections and no pending requests
            // is that we'll see _http11RequestQueueIsEmptyAndNotDisposed being true both
            // times, and all we'll have to do as part of returning the connection is
            // a Push call on the concurrent stack.

            if (Volatile.Read(ref _http11RequestQueueIsEmptyAndNotDisposed))
            {
                _http11Connections.Push(connection);

                // When we add a connection to the pool, we must ensure that there are
                // either no pending requests waiting, or that _something_ will pair those
                // requests with the connection we just added.

                // When adding a request to the queue, we'll first check if there's
                // an available connection waiting in the pool that we could use.
                // If there isn't, we'll set the _http11RequestQueueIsEmptyAndNotDisposed
                // flag and check for available connections again.

                // To avoid a race where we add the connection after a request was enqueued,
                // we'll check the flag again and try to process one request from the queue.

                if (!Volatile.Read(ref _http11RequestQueueIsEmptyAndNotDisposed))
                {
                    ProcessHttp11RequestQueue(null);
                }
            }
            else
            {
                // ProcessHttp11RequestQueue is responsible for handing the connection to a pending request,
                // or to return it back to the pool if there aren't any.

                // We hand over the connection directly instead of pushing it on the stack first to ensure
                // that pending requests are processed in a fair (FIFO) order.
                ProcessHttp11RequestQueue(connection);
            }
        }

        /// <summary>
        /// Called when an HttpConnection from this pool is no longer usable.
        /// Note, this is always called from HttpConnection.Dispose, which is a bit different than how HTTP2 works.
        /// </summary>
        public void InvalidateHttp11Connection(HttpConnection connection, bool disposing = true)
        {
            lock (SyncObj)
            {
                Debug.Assert(_associatedHttp11ConnectionCount > 0);
                Debug.Assert(!disposing || Array.IndexOf(_http11Connections.ToArray(), connection) < 0);

                _associatedHttp11ConnectionCount--;

                CheckForHttp11ConnectionInjection();
            }
        }

        private static void ScavengeHttp11ConnectionStack(HttpConnectionPool pool, ConcurrentStack<HttpConnection> connections, ref List<HttpConnectionBase>? toDispose, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout)
        {
            // We can't simply enumerate the connections stack as other threads may still be adding and removing entries.
            // If we want to check the state of a connection, we must take it from the stack first to ensure we own it.

            // We're about to starve the connection pool of all available connections for a moment.
            // We must be holding the lock while doing so to ensure that any new requests that
            // come in during this time will be blocked waiting in ProcessHttp11RequestQueue.
            // If this were not the case, requests would repeatedly call into CheckForHttp11ConnectionInjection
            // and trigger new connection attempts, even if we have enough connections in our copy.
            Debug.Assert(pool.HasSyncObjLock);
            Debug.Assert(connections.Count <= pool._associatedHttp11ConnectionCount);

            HttpConnection[] stackCopy = ArrayPool<HttpConnection>.Shared.Rent(pool._associatedHttp11ConnectionCount);
            int usableConnections = 0;

            while (connections.TryPop(out HttpConnection? connection))
            {
                if (connection.IsUsable(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
                {
                    stackCopy[usableConnections++] = connection;
                }
                else
                {
                    toDispose ??= new List<HttpConnectionBase>();
                    toDispose.Add(connection);
                }
            }

            if (usableConnections > 0)
            {
                // Add them back in reverse to maintain the LIFO order.
                Span<HttpConnection> usable = stackCopy.AsSpan(0, usableConnections);
                usable.Reverse();
                connections.PushRange(stackCopy, 0, usableConnections);
                usable.Clear();
            }

            ArrayPool<HttpConnection>.Shared.Return(stackCopy);
        }
    }
}
