// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides a pool of connections to the same endpoint.</summary>
    internal sealed class HttpConnectionPool : IDisposable
    {
        private static readonly bool s_isWindows7Or2008R2 = GetIsWindows7Or2008R2();

        private readonly HttpConnectionPoolManager _poolManager;
        private readonly HttpConnectionKind _kind;
        private readonly string _host;
        private readonly int _port;
        private readonly Uri _proxyUri;
        internal readonly byte[] _encodedAuthorityHostHeader;

        /// <summary>List of idle connections stored in the pool.</summary>
        private readonly List<CachedConnection> _idleConnections = new List<CachedConnection>();
        /// <summary>The maximum number of connections allowed to be associated with the pool.</summary>
        private readonly int _maxConnections;

        private bool _http2Enabled;
        private Http2Connection _http2Connection;
        private SemaphoreSlim _http2ConnectionCreateLock;

        /// <summary>For non-proxy connection pools, this is the host name in bytes; for proxies, null.</summary>
        private readonly byte[] _hostHeaderValueBytes;
        /// <summary>Options specialized and cached for this pool and its key.</summary>
        private readonly SslClientAuthenticationOptions _sslOptionsHttp11;
        private readonly SslClientAuthenticationOptions _sslOptionsHttp2;

        /// <summary>Queue of waiters waiting for a connection.  Created on demand.</summary>
        private Queue<TaskCompletionSourceWithCancellation<HttpConnection>> _waiters;

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
        public HttpConnectionPool(HttpConnectionPoolManager poolManager, HttpConnectionKind kind, string host, int port, string sslHostName, Uri proxyUri, int maxConnections)
        {
            _poolManager = poolManager;
            _kind = kind;
            _host = host;
            _port = port;
            _proxyUri = proxyUri;
            _maxConnections = maxConnections;

            _http2Enabled = (_poolManager.Settings._maxHttpVersion == HttpVersion.Version20);

            switch (kind)
            {
                case HttpConnectionKind.Http:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri == null);
                    _http2Enabled = _poolManager.Settings._allowUnencryptedHttp2;
                    break;

                case HttpConnectionKind.Https:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName != null);
                    Debug.Assert(proxyUri == null);
                    break;

                case HttpConnectionKind.Proxy:
                    Debug.Assert(host == null);
                    Debug.Assert(port == 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri != null);

                    _http2Enabled = false;
                    break;

                case HttpConnectionKind.ProxyTunnel:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri != null);

                    _http2Enabled = false;
                    break;

                case HttpConnectionKind.SslProxyTunnel:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName != null);
                    Debug.Assert(proxyUri != null);
                    break;

                case HttpConnectionKind.ProxyConnect:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri != null);

                    _http2Enabled = false;
                    break;

                default:
                    Debug.Fail("Unkown HttpConnectionKind in HttpConnectionPool.ctor");
                    break;
            }

            string hostHeader = null;
            if (_host != null)
            {
                // Precalculate ASCII bytes for Host header
                // Note that if _host is null, this is a (non-tunneled) proxy connection, and we can't cache the hostname.
                hostHeader =
                    (_port != (sslHostName == null ? DefaultHttpPort : DefaultHttpsPort)) ?
                    $"{_host}:{_port}" :
                    _host;

                // Note the IDN hostname should always be ASCII, since it's already been IDNA encoded.
                _hostHeaderValueBytes = Encoding.ASCII.GetBytes(hostHeader);
                Debug.Assert(Encoding.ASCII.GetString(_hostHeaderValueBytes) == hostHeader);
                if (sslHostName == null)
                {
                    _encodedAuthorityHostHeader = HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(StaticTable.Authority, hostHeader);
                }
            }

            if (sslHostName != null)
            {
                _sslOptionsHttp11 = ConstructSslOptions(poolManager, sslHostName);
                _sslOptionsHttp11.ApplicationProtocols = null;

                if (_http2Enabled)
                {
                    _sslOptionsHttp2 = ConstructSslOptions(poolManager, sslHostName);
                    _sslOptionsHttp2.ApplicationProtocols = Http2ApplicationProtocols;
                    
                    // Note:
                    // The HTTP/2 specification states:
                    //   "A deployment of HTTP/2 over TLS 1.2 MUST disable renegotiation.
                    //    An endpoint MUST treat a TLS renegotiation as a connection error (Section 5.4.1)
                    //    of type PROTOCOL_ERROR."
                    // which suggests we should do:
                    //   _sslOptionsHttp2.AllowRenegotiation = false;
                    // However, if AllowRenegotiation is set to false, that will also prevent
                    // renegotation if the server denies the HTTP/2 request and causes a
                    // downgrade to HTTP/1.1, and the current APIs don't provide a mechanism
                    // by which AllowRenegotiation could be set back to true in that case.
                    // For now, if an HTTP/2 server erroneously issues a renegotiation, we'll
                    // allow it.

                    Debug.Assert(hostHeader != null);
                    _encodedAuthorityHostHeader = HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(StaticTable.Authority, hostHeader);
                }
            }
            
            // Set up for PreAuthenticate.  Access to this cache is guarded by a lock on the cache itself.
            if (_poolManager.Settings._preAuthenticate)
            {
                PreAuthCredentials = new CredentialCache();
            }
        }

        private static readonly List<SslApplicationProtocol> Http2ApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 };

        private static SslClientAuthenticationOptions ConstructSslOptions(HttpConnectionPoolManager poolManager, string sslHostName)
        {
            Debug.Assert(sslHostName != null);

            SslClientAuthenticationOptions sslOptions = poolManager.Settings._sslOptions?.ShallowClone() ?? new SslClientAuthenticationOptions();

            // Set TargetHost for SNI
            sslOptions.TargetHost = sslHostName;

            // Windows 7 and Windows 2008 R2 support TLS 1.1 and 1.2, but for legacy reasons by default those protocols
            // are not enabled when a developer elects to use the system default.  However, in .NET Core 2.0 and earlier,
            // HttpClientHandler would enable them, due to being a wrapper for WinHTTP, which enabled them.  Both for
            // compatibility and because we prefer those higher protocols whenever possible, SocketsHttpHandler also
            // pretends they're part of the default when running on Win7/2008R2.
            if (s_isWindows7Or2008R2 && sslOptions.EnabledSslProtocols == SslProtocols.None)
            {
                if (NetEventSource.IsEnabled)
                {
                    NetEventSource.Info(poolManager, $"Win7OrWin2K8R2 platform, Changing default TLS protocols to {SecurityProtocol.DefaultSecurityProtocols}");
                }
                sslOptions.EnabledSslProtocols = SecurityProtocol.DefaultSecurityProtocols;
            }

            return sslOptions;
        }

        public HttpConnectionSettings Settings => _poolManager.Settings;
        public bool IsSecure => _sslOptionsHttp11 != null;
        public HttpConnectionKind Kind => _kind;
        public bool AnyProxyKind => (_proxyUri != null);
        public Uri ProxyUri => _proxyUri;
        public ICredentials ProxyCredentials => _poolManager.ProxyCredentials;
        public byte[] HostHeaderValueBytes => _hostHeaderValueBytes;
        public CredentialCache PreAuthCredentials { get; }

        /// <summary>Object used to synchronize access to state in the pool.</summary>
        private object SyncObj => _idleConnections;

        private ValueTask<(HttpConnectionBase connection, bool isNewConnection, HttpResponseMessage failureResponse)> 
            GetConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_http2Enabled && request.Version.Major >= 2)
            {
                return GetHttp2ConnectionAsync(request, cancellationToken);
            }

            return GetHttpConnectionAsync(request, cancellationToken);
        }

        private ValueTask<HttpConnection> GetOrReserveHttp11ConnectionAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<HttpConnection>(Task.FromCanceled<HttpConnection>(cancellationToken));
            }

            TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
            TimeSpan pooledConnectionIdleTimeout = _poolManager.Settings._pooledConnectionIdleTimeout;
            long nowTicks = Environment.TickCount64;
            List<CachedConnection> list = _idleConnections;

            // Try to find a usable cached connection.
            // If we can't find one, we will either wait for one to become available (if at the connection limit)
            // or just increment the connection count and return null so the caller can create a new connection.
            TaskCompletionSourceWithCancellation<HttpConnection> waiter;
            while (true)
            {
                CachedConnection cachedConnection;
                lock (SyncObj)
                {
                    if (list.Count > 0)
                    {
                        // We have a cached connection that we can attempt to use.
                        // Test it below outside the lock, to avoid doing expensive validation while holding the lock.
                        cachedConnection = list[list.Count - 1];
                        list.RemoveAt(list.Count - 1);
                    }
                    else
                    {
                        // No valid cached connections.
                        if (_associatedConnectionCount < _maxConnections)
                        {
                            // We are under the connection limit, so just increment the count and return null
                            // to indicate to the caller that they should create a new connection.
                            IncrementConnectionCountNoLock();
                            return new ValueTask<HttpConnection>((HttpConnection)null);
                        }
                        else
                        {
                            // We've reached the connection limit and need to wait for an existing connection
                            // to become available, or to be closed so that we can create a new connection.
                            // Enqueue a waiter that will be signalled when this happens.
                            // Break out of the loop and then do the actual wait below.
                            waiter = EnqueueWaiter();
                            break;
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

                HttpConnection conn = cachedConnection._connection;
                if (!conn.LifetimeExpired(nowTicks, pooledConnectionLifetime) &&
                    !conn.EnsureReadAheadAndPollRead())
                {
                    // We found a valid connection.  Return it.
                    if (NetEventSource.IsEnabled) conn.Trace("Found usable connection in pool.");
                    return new ValueTask<HttpConnection>(conn);
                }

                // We got a connection, but it was already closed by the server or the
                // server sent unexpected data or the connection is too old.  In any case,
                // we can't use the connection, so get rid of it and loop around to try again.
                if (NetEventSource.IsEnabled) conn.Trace("Found invalid connection in pool.");
                conn.Dispose();
            }

            // We are at the connection limit. Wait for an available connection or connection count (indicated by null).
            if (NetEventSource.IsEnabled) Trace("Connection limit reached, waiting for available connection.");
            return new ValueTask<HttpConnection>(waiter.WaitWithCancellationAsync(cancellationToken));
        }

        private async ValueTask<(HttpConnectionBase connection, bool isNewConnection, HttpResponseMessage failureResponse)> 
            GetHttpConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpConnection connection = await GetOrReserveHttp11ConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection != null)
            {
                return (connection, false, null);
            }

            if (NetEventSource.IsEnabled) Trace("Creating new connection for pool.");

            try
            {
                HttpResponseMessage failureResponse;
                (connection, failureResponse) = await CreateHttp11ConnectionAsync(request, cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    Debug.Assert(failureResponse != null);
                    DecrementConnectionCount();
                }
                return (connection, true, failureResponse);
            }
            catch
            {
                DecrementConnectionCount();
                throw;
            }
        }

        private async ValueTask<(HttpConnectionBase connection, bool isNewConnection, HttpResponseMessage failureResponse)>
            GetHttp2ConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel || _kind == HttpConnectionKind.Http);

            // See if we have an HTTP2 connection
            Http2Connection http2Connection = _http2Connection;

            if (http2Connection != null)
            {
                TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
                if (http2Connection.LifetimeExpired(Environment.TickCount64, pooledConnectionLifetime))
                {
                    // Connection expired.
                    http2Connection.Dispose();
                    InvalidateHttp2Connection(http2Connection);
                }
                else
                {
                    // Connection exist and it is still good to use.
                    if (NetEventSource.IsEnabled) Trace("Using existing HTTP2 connection.");
                    _usedSinceLastCleanup = true;
                    return (http2Connection, false, null);
                }
            }

            // Ensure that the connection creation semaphore is created
            if (_http2ConnectionCreateLock == null)
            {
                lock (SyncObj)
                {
                    if (_http2ConnectionCreateLock == null)
                    {
                        _http2ConnectionCreateLock = new SemaphoreSlim(1);
                    }
                }
            }

            // Try to establish an HTTP2 connection
            Socket socket = null;
            SslStream sslStream = null;
            TransportContext transportContext = null;

            // Serialize creation attempt
            await _http2ConnectionCreateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_http2Connection != null)
                {
                    // Someone beat us to it

                    if (NetEventSource.IsEnabled)
                    {
                        Trace("Using existing HTTP2 connection.");
                    }

                    return (_http2Connection, false, null);
                }

                // Recheck if HTTP2 has been disabled by a previous attempt.
                if (_http2Enabled)
                {
                    if (NetEventSource.IsEnabled)
                    {
                        Trace("Attempting new HTTP2 connection.");
                    }

                    Stream stream;
                    HttpResponseMessage failureResponse;
                    (socket, stream, transportContext, failureResponse) =
                        await ConnectAsync(request, true, cancellationToken).ConfigureAwait(false);
                    if (failureResponse != null)
                    {
                        return (null, true, failureResponse);
                    }

                    if (_kind == HttpConnectionKind.Http)
                    {
                        http2Connection = new Http2Connection(this, stream);
                        await http2Connection.SetupAsync().ConfigureAwait(false);

                        Debug.Assert(_http2Connection == null);
                        _http2Connection = http2Connection;

                        if (NetEventSource.IsEnabled)
                        {
                            Trace("New unencrypted HTTP2 connection established.");
                        }

                        return (_http2Connection, true, null);
                    }

                    sslStream = (SslStream)stream;
                    if (sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
                    {
                        // The server accepted our request for HTTP2.

                        if (sslStream.SslProtocol < SslProtocols.Tls12)
                        {
                            throw new HttpRequestException(SR.Format(SR.net_ssl_http2_requires_tls12, sslStream.SslProtocol));
                        }

                        http2Connection = new Http2Connection(this, sslStream);
                        await http2Connection.SetupAsync().ConfigureAwait(false);

                        Debug.Assert(_http2Connection == null);
                        _http2Connection = http2Connection;

                        if (NetEventSource.IsEnabled)
                        {
                            Trace("New HTTP2 connection established.");
                        }

                        return (_http2Connection, true, null);
                    }
                }
            }
            finally
            {
                _http2ConnectionCreateLock.Release();
            }

            if (sslStream != null)
            {
                // We established an SSL connection, but the server denied our request for HTTP2.
                // Continue as an HTTP/1.1 connection.
                if (NetEventSource.IsEnabled)
                {
                    Trace("Server does not support HTTP2; disabling HTTP2 use and proceeding with HTTP/1.1 connection");
                }

                bool canUse = true;
                lock (SyncObj)
                {
                    _http2Enabled = false;

                    if (_associatedConnectionCount < _maxConnections)
                    {
                        IncrementConnectionCountNoLock();
                    }
                    else
                    {
                        // We are in the weird situation of having established a new HTTP 1.1 connection
                        // when we were already at the maximum for HTTP 1.1 connections.
                        // Just discard this connection and get another one from the pool.
                        // This should be a really rare situation to get into, since it would require 
                        // the user to make multiple HTTP 1.1-only requests first before attempting an
                        // HTTP2 request, and the server failing to accept HTTP2.
                        canUse = false;
                    }
                }

                if (canUse)
                {
                    return (ConstructHttp11Connection(socket, sslStream, transportContext), true, null);
                }
                else
                {
                    if (NetEventSource.IsEnabled)
                    {
                        Trace("Discarding downgraded HTTP/1.1 connection because connection limit is exceeded");
                    }

                    sslStream.Close();
                }
            }

            // If we reach this point, it means we need to fall back to a (new or existing) HTTP/1.1 connection.
            return await GetHttpConnectionAsync(request, cancellationToken);
        }

        public async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
        {
            while (true)
            {
                // Loop on connection failures and retry if possible.

                (HttpConnectionBase connection, bool isNewConnection, HttpResponseMessage failureResponse) = await GetConnectionAsync(request, cancellationToken).ConfigureAwait(false);
                if (failureResponse != null)
                {
                    // Proxy tunnel failure; return proxy response
                    Debug.Assert(isNewConnection);
                    Debug.Assert(connection == null);
                    return failureResponse;
                }

                try
                {
                    if (connection is HttpConnection)
                    {
                        return await SendWithNtConnectionAuthAsync((HttpConnection)connection, request, doRequestAuth, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        return await connection.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (HttpRequestException e) when (!isNewConnection && e.AllowRetry)
                {
                    if (NetEventSource.IsEnabled)
                    {
                        Trace($"Retrying request after exception on existing connection: {e}");
                    }

                    // Eat exception and try again.
                }
            }
        }

        public async Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpConnection connection, HttpRequestMessage request, bool doRequestAuth, CancellationToken cancellationToken)
        {
            connection.Acquire();
            try
            {
                if (doRequestAuth && Settings._credentials != null)
                {
                    return await AuthenticationHelper.SendWithNtConnectionAuthAsync(request, Settings._credentials, connection, this, cancellationToken).ConfigureAwait(false);
                }

                return await SendWithNtProxyAuthAsync(connection, request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                connection.Release();
            }
        }

        public Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpConnection connection, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (AnyProxyKind && ProxyCredentials != null)
            {
                return AuthenticationHelper.SendWithNtProxyAuthAsync(request, ProxyUri, ProxyCredentials, connection, this, cancellationToken);
            }

            return connection.SendAsync(request, cancellationToken);
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

        private async ValueTask<(Socket, Stream, TransportContext, HttpResponseMessage)> ConnectAsync(HttpRequestMessage request, bool allowHttp2, CancellationToken cancellationToken)
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
                Stream stream = null;
                switch (_kind)
                {
                    case HttpConnectionKind.Http:
                    case HttpConnectionKind.Https:
                    case HttpConnectionKind.ProxyConnect:
                        stream = await ConnectHelper.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
                        break;

                    case HttpConnectionKind.Proxy:
                        stream = await ConnectHelper.ConnectAsync(_proxyUri.IdnHost, _proxyUri.Port, cancellationToken).ConfigureAwait(false);
                        break;

                    case HttpConnectionKind.ProxyTunnel:
                    case HttpConnectionKind.SslProxyTunnel:
                        HttpResponseMessage response;
                        (stream, response) = await EstablishProxyTunnel(request.HasHeaders ? request.Headers : null, cancellationToken).ConfigureAwait(false);
                        if (response != null)
                        {
                            // Return non-success response from proxy.
                            response.RequestMessage = request;
                            return (null, null, null, response);
                        }
                        break;
                }

                Socket socket = (stream as ExposedSocketNetworkStream)?.Socket; // TODO: Use NetworkStream when https://github.com/dotnet/corefx/issues/35410 is available.

                TransportContext transportContext = null;
                if (_kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel)
                {
                    SslStream sslStream = await ConnectHelper.EstablishSslConnectionAsync(allowHttp2 ? _sslOptionsHttp2 : _sslOptionsHttp11, request, stream, cancellationToken).ConfigureAwait(false);
                    stream = sslStream;
                    transportContext = sslStream.TransportContext;
                }

                return (socket, stream, transportContext, null);
            }
            finally
            {
                cancellationWithConnectTimeout?.Dispose();
            }
        }

        internal async ValueTask<(HttpConnection, HttpResponseMessage)> CreateHttp11ConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            (Socket socket, Stream stream, TransportContext transportContext, HttpResponseMessage failureResponse) =
                await ConnectAsync(request, false, cancellationToken).ConfigureAwait(false);

            if (failureResponse != null)
            {
                return (null, failureResponse);
            }

            return (ConstructHttp11Connection(socket, stream, transportContext), null);
        }

        private HttpConnection ConstructHttp11Connection(Socket socket, Stream stream, TransportContext transportContext)
        {
            return _maxConnections == int.MaxValue ?
                new HttpConnection(this, socket, stream, transportContext) :
                new HttpConnectionWithFinalizer(this, socket, stream, transportContext); // finalizer needed to signal the pool when a connection is dropped
        }

        // Returns the established stream or an HttpResponseMessage from the proxy indicating failure.
        private async ValueTask<(Stream, HttpResponseMessage)> EstablishProxyTunnel(HttpRequestHeaders headers, CancellationToken cancellationToken)
        {
            // Send a CONNECT request to the proxy server to establish a tunnel.
            HttpRequestMessage tunnelRequest = new HttpRequestMessage(HttpMethod.Connect, _proxyUri);
            tunnelRequest.Headers.Host = $"{_host}:{_port}";    // This specifies destination host/port to connect to

            if (headers != null && headers.TryGetValues(HttpKnownHeaderNames.UserAgent, out IEnumerable<string> values))
            {
                tunnelRequest.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.UserAgent, values);
            }

            HttpResponseMessage tunnelResponse = await _poolManager.SendProxyConnectAsync(tunnelRequest, _proxyUri, cancellationToken).ConfigureAwait(false);

            if (tunnelResponse.StatusCode != HttpStatusCode.OK)
            {
                return (null, tunnelResponse);
            }

            return (await tunnelResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), null);
        }

        /// <summary>Enqueues a waiter to the waiters list.</summary>
        private TaskCompletionSourceWithCancellation<HttpConnection> EnqueueWaiter()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));
            Debug.Assert(Settings._maxConnectionsPerServer != int.MaxValue);
            Debug.Assert(_idleConnections.Count == 0, $"With {_idleConnections.Count} idle connections, we shouldn't have a waiter.");

            if (_waiters == null)
            {
                _waiters = new Queue<TaskCompletionSourceWithCancellation<HttpConnection>>();
            }

            var waiter = new TaskCompletionSourceWithCancellation<HttpConnection>();
            _waiters.Enqueue(waiter);
            return waiter;
        }

        private bool HasWaiter()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));

            return (_waiters != null && _waiters.Count > 0);
        }

        /// <summary>Dequeues a waiter from the waiters list.  The list must not be empty.</summary>
        /// <returns>The dequeued waiter.</returns>
        private TaskCompletionSourceWithCancellation<HttpConnection> DequeueWaiter()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));
            Debug.Assert(Settings._maxConnectionsPerServer != int.MaxValue);
            Debug.Assert(_idleConnections.Count == 0, $"With {_idleConnections.Count} idle connections, we shouldn't have a waiter.");

            return _waiters.Dequeue();
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

        internal void IncrementConnectionCount()
        {
            lock (SyncObj)
            {
                IncrementConnectionCountNoLock();
            }
        }

        private bool TransferConnection(HttpConnection connection)
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));

            while (HasWaiter())
            {
                TaskCompletionSource<HttpConnection> waiter = DequeueWaiter();

                // Try to complete the task. If it's been cancelled already, this will fail.
                if (waiter.TrySetResult(connection))
                {
                    return true;
                }

                // Couldn't transfer to that waiter because it was cancelled. Try again.
                Debug.Assert(waiter.Task.IsCanceled);
            }

            return false;
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

                if (TransferConnection(null))
                {
                    if (NetEventSource.IsEnabled) Trace("Transferred connection count to waiter.");
                    return;
                }

                // There are no waiters to which the count should logically be transferred,
                // so simply decrement the count.
                _associatedConnectionCount--;
            }
        }

        /// <summary>Returns the connection to the pool for subsequent reuse.</summary>
        /// <param name="connection">The connection to return.</param>
        public void ReturnConnection(HttpConnection connection)
        {
            bool lifetimeExpired = connection.LifetimeExpired(Environment.TickCount64, _poolManager.Settings._pooledConnectionLifetime);

            if (!lifetimeExpired)
            {
                List<CachedConnection> list = _idleConnections;
                lock (SyncObj)
                {
                    Debug.Assert(list.Count <= _maxConnections, $"Expected {list.Count} <= {_maxConnections}");

                    // Mark the pool as still being active.
                    _usedSinceLastCleanup = true;

                    // If there's someone waiting for a connection and this one's still valid, simply transfer this one to them rather than pooling it.
                    // Note that while we checked connection lifetime above, we don't check idle timeout, as even if idle timeout
                    // is zero, we consider a connection that's just handed from one use to another to never actually be idle.
                    bool receivedUnexpectedData = false;
                    if (HasWaiter())
                    {
                        receivedUnexpectedData = connection.EnsureReadAheadAndPollRead();
                        if (!receivedUnexpectedData && TransferConnection(connection))
                        {
                            if (NetEventSource.IsEnabled) connection.Trace("Transferred connection to waiter.");
                            return;
                        }
                    }

                    // If the connection is still valid, add it to the list.
                    // If the pool has been disposed of, dispose the connection being returned,
                    // as the pool is being deactivated. We do this after the above in order to
                    // use pooled connections to satisfy any requests that pended before the
                    // the pool was disposed of.  We also dispose of connections if connection
                    // timeouts are such that the connection would immediately expire, anyway, as
                    // well as for connections that have unexpectedly received extraneous data / EOF.
                    if (!receivedUnexpectedData &&
                        !_disposed &&
                        _poolManager.Settings._pooledConnectionIdleTimeout != TimeSpan.Zero)
                    {
                        // Pool the connection by adding it to the list.
                        list.Add(new CachedConnection(connection));
                        if (NetEventSource.IsEnabled) connection.Trace("Stored connection in pool.");
                        return;
                    }
                }
            }

            // The connection could be not be reused.  Dispose of it.
            // Disposing it will alert any waiters that a connection slot has become available.
            if (NetEventSource.IsEnabled)
            {
                connection.Trace(
                    lifetimeExpired ? "Disposing connection return to pool. Connection lifetime expired." :
                    _poolManager.Settings._pooledConnectionIdleTimeout == TimeSpan.Zero ? "Disposing connection returned to pool. Zero idle timeout." :
                    _disposed ? "Disposing connection returned to pool. Pool was disposed." :
                    "Disposing connection returned to pool. Read-ahead unexpectedly completed.");
            }
            connection.Dispose();
        }

        public void InvalidateHttp2Connection(Http2Connection connection)
        {
            lock (SyncObj)
            {
                if (_http2Connection == connection)
                {
                    _http2Connection = null;
                }
            }
        }

        /// <summary>
        /// Disposes the connection pool.  This is only needed when the pool currently contains
        /// or has associated connections.
        /// </summary>
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

                    if (_http2Connection != null)
                    {
                        _http2Connection.Dispose();
                        _http2Connection = null;
                    }
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
                long nowTicks = Environment.TickCount64;
                Http2Connection http2Connection = _http2Connection;

                if (http2Connection != null)
                {
                    if (http2Connection.IsExpired(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
                    {
                        http2Connection.Dispose();
                        // We can set _http2Connection directly while holding lock instead of calling InvalidateHttp2Connection().
                        _http2Connection = null;
                    }
                }

                // Find the first item which needs to be removed.
                int freeIndex = 0;
                while (freeIndex < list.Count && list[freeIndex].IsUsable(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout, poll: true))
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
                        while (current < list.Count && !list[current].IsUsable(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout, poll: true))
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
                    if (_associatedConnectionCount == 0 && !_usedSinceLastCleanup && _http2Connection == null)
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

        /// <summary>Gets whether we're running on Windows 7 or Windows 2008 R2.</summary>
        private static bool GetIsWindows7Or2008R2()
        {
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT)
            {
                // Both Windows 7 and Windows 2008 R2 report version 6.1.
                Version v = os.Version;
                return v.Major == 6 && v.Minor == 1;
            }
            return false;
        }

        // For diagnostic purposes
        public override string ToString() =>
            $"{nameof(HttpConnectionPool)} " +
            (_proxyUri == null ?
                (_sslOptionsHttp11 == null ?
                    $"http://{_host}:{_port}" :
                    $"https://{_host}:{_port}" + (_sslOptionsHttp11.TargetHost != _host ? $", SSL TargetHost={_sslOptionsHttp11.TargetHost}" : null)) :
                (_sslOptionsHttp11 == null ?
                    $"Proxy {_proxyUri}" :
                    $"https://{_host}:{_port}/ tunnelled via Proxy {_proxyUri}" + (_sslOptionsHttp11.TargetHost != _host ? $", SSL TargetHost={_sslOptionsHttp11.TargetHost}" : null)));

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
            /// <summary>The last tick count at which the connection was used.</summary>
            internal readonly long _returnedTickCount;

            /// <summary>Initializes the cached connection and its associated metadata.</summary>
            /// <param name="connection">The connection.</param>
            public CachedConnection(HttpConnection connection)
            {
                Debug.Assert(connection != null);
                _connection = connection;
                _returnedTickCount = Environment.TickCount64;
            }

            /// <summary>Gets whether the connection is currently usable.</summary>
            /// <param name="nowTicks">The current tick count.  Passed in to amortize the cost of calling Environment.TickCount.</param>
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
                long nowTicks,
                TimeSpan pooledConnectionLifetime,
                TimeSpan pooledConnectionIdleTimeout,
                bool poll = false)
            {
                // Validate that the connection hasn't been idle in the pool for longer than is allowed.
                if ((pooledConnectionIdleTimeout != Timeout.InfiniteTimeSpan) &&
                    ((nowTicks - _returnedTickCount) > pooledConnectionIdleTimeout.TotalMilliseconds))
                {
                    if (NetEventSource.IsEnabled) _connection.Trace($"Connection no longer usable. Idle {TimeSpan.FromMilliseconds((nowTicks - _returnedTickCount))} > {pooledConnectionIdleTimeout}.");
                    return false;
                }

                // Validate that the connection hasn't been alive for longer than is allowed.
                if (_connection.LifetimeExpired(nowTicks, pooledConnectionLifetime))
                {
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
    }
}
