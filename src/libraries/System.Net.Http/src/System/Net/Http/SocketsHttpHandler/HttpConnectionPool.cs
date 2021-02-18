// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Http.QPack;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace System.Net.Http
{
    /// <summary>Provides a pool of connections to the same endpoint.</summary>
    internal sealed class HttpConnectionPool : IDisposable
    {
        private static readonly bool s_isWindows7Or2008R2 = GetIsWindows7Or2008R2();

        private readonly HttpConnectionPoolManager _poolManager;
        private readonly HttpConnectionKind _kind;
        private readonly Uri? _proxyUri;

        /// <summary>The origin authority used to construct the <see cref="HttpConnectionPool"/>.</summary>
        private readonly HttpAuthority? _originAuthority;

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
        private volatile HashSet<HttpAuthority>? _altSvcBlocklist;
        private CancellationTokenSource? _altSvcBlocklistTimerCancellation;
        private volatile bool _altSvcEnabled = true;

        /// <summary>
        /// If <see cref="_altSvcBlocklist"/> exceeds this size, Alt-Svc will be disabled entirely for <see cref="AltSvcBlocklistTimeoutInMilliseconds"/> milliseconds.
        /// This is to prevent a failing server from bloating the dictionary beyond a reasonable value.
        /// </summary>
        private const int MaxAltSvcIgnoreListSize = 8;

        /// <summary>The time, in milliseconds, that an authority should remain in <see cref="_altSvcBlocklist"/>.</summary>
        private const int AltSvcBlocklistTimeoutInMilliseconds = 10 * 60 * 1000;

        /// <summary>List of idle connections stored in the pool.</summary>
        private readonly List<CachedConnection> _idleConnections = new List<CachedConnection>();
        /// <summary>The maximum number of connections allowed to be associated with the pool.</summary>
        private readonly int _maxConnections;

        private bool _http2Enabled;
        // This array must be treated as immutable. It can only be replaced with a new value in AddHttp2Connection method.
        private volatile Http2Connection[]? _http2Connections;
        private SemaphoreSlim? _http2ConnectionCreateLock;
        private byte[]? _http2AltSvcOriginUri;
        internal readonly byte[]? _http2EncodedAuthorityHostHeader;

        private readonly bool _http3Enabled;
        private Http3Connection? _http3Connection;
        private SemaphoreSlim? _http3ConnectionCreateLock;
        internal readonly byte[]? _http3EncodedAuthorityHostHeader;

        /// <summary>For non-proxy connection pools, this is the host name in bytes; for proxies, null.</summary>
        private readonly byte[]? _hostHeaderValueBytes;
        /// <summary>Options specialized and cached for this pool and its key.</summary>
        private readonly SslClientAuthenticationOptions? _sslOptionsHttp11;
        private readonly SslClientAuthenticationOptions? _sslOptionsHttp2;
        private readonly SslClientAuthenticationOptions? _sslOptionsHttp2Only;
        private readonly SslClientAuthenticationOptions? _sslOptionsHttp3;

        /// <summary>Queue of waiters waiting for a connection.  Created on demand.</summary>
        private Queue<TaskCompletionSourceWithCancellation<HttpConnection?>>? _waiters;

        /// <summary>The number of connections associated with the pool.  Some of these may be in <see cref="_idleConnections"/>, others may be in use.</summary>
        private int _associatedConnectionCount;
        /// <summary>Whether the pool has been used since the last time a cleanup occurred.</summary>
        private bool _usedSinceLastCleanup = true;
        /// <summary>Whether the pool has been disposed.</summary>
        private bool _disposed;

        public const int DefaultHttpPort = 80;
        public const int DefaultHttpsPort = 443;

        /// <summary>Initializes the pool.</summary>
        /// <param name="poolManager">The manager associated with this pool.</param>
        /// <param name="kind">The kind of HTTP connections stored in this pool.</param>
        /// <param name="host">The host with which this pool is associated.</param>
        /// <param name="port">The port with which this pool is associated.</param>
        /// <param name="sslHostName">The SSL host with which this pool is associated.</param>
        /// <param name="proxyUri">The proxy this pool targets (optional).</param>
        /// <param name="maxConnections">The maximum number of connections allowed to be associated with the pool at any given time.</param>
        public HttpConnectionPool(HttpConnectionPoolManager poolManager, HttpConnectionKind kind, string? host, int port, string? sslHostName, Uri? proxyUri, int maxConnections)
        {
            _poolManager = poolManager;
            _kind = kind;
            _proxyUri = proxyUri;
            _maxConnections = maxConnections;

            if (host != null)
            {
                _originAuthority = new HttpAuthority(host, port);
            }

            _http2Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version20;
            _http3Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version30 && (_poolManager.Settings._quicImplementationProvider ?? QuicImplementationProviders.Default).IsSupported;

            switch (kind)
            {
                case HttpConnectionKind.Http:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri == null);

                    _http3Enabled = false;
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
                    _http3Enabled = false;
                    break;

                case HttpConnectionKind.ProxyTunnel:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri != null);

                    _http2Enabled = false;
                    _http3Enabled = false;
                    break;

                case HttpConnectionKind.SslProxyTunnel:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName != null);
                    Debug.Assert(proxyUri != null);

                    _http3Enabled = false; // TODO: how do we tunnel HTTP3?
                    break;

                case HttpConnectionKind.ProxyConnect:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(sslHostName == null);
                    Debug.Assert(proxyUri != null);

                    _http2Enabled = false;
                    _http3Enabled = false;
                    break;

                default:
                    Debug.Fail("Unkown HttpConnectionKind in HttpConnectionPool.ctor");
                    break;
            }

            if (!_http3Enabled)
            {
                // Avoid parsing Alt-Svc headers if they won't be used.
                _altSvcEnabled = false;
            }

            string? hostHeader = null;
            if (_originAuthority != null)
            {
                // Precalculate ASCII bytes for Host header
                // Note that if _host is null, this is a (non-tunneled) proxy connection, and we can't cache the hostname.
                hostHeader =
                    (_originAuthority.Port != (sslHostName == null ? DefaultHttpPort : DefaultHttpsPort)) ?
                    $"{_originAuthority.IdnHost}:{_originAuthority.Port}" :
                    _originAuthority.IdnHost;

                // Note the IDN hostname should always be ASCII, since it's already been IDNA encoded.
                _hostHeaderValueBytes = Encoding.ASCII.GetBytes(hostHeader);
                Debug.Assert(Encoding.ASCII.GetString(_hostHeaderValueBytes) == hostHeader);
                if (sslHostName == null)
                {
                    _http2EncodedAuthorityHostHeader = HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(H2StaticTable.Authority, hostHeader);
                    _http3EncodedAuthorityHostHeader = QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(H3StaticTable.Authority, hostHeader);
                }
            }

            if (sslHostName != null)
            {
                _sslOptionsHttp11 = ConstructSslOptions(poolManager, sslHostName);
                _sslOptionsHttp11.ApplicationProtocols = null;

                if (_http2Enabled)
                {
                    _sslOptionsHttp2 = ConstructSslOptions(poolManager, sslHostName);
                    _sslOptionsHttp2.ApplicationProtocols = s_http2ApplicationProtocols;
                    _sslOptionsHttp2Only = ConstructSslOptions(poolManager, sslHostName);
                    _sslOptionsHttp2Only.ApplicationProtocols = s_http2OnlyApplicationProtocols;

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
                    _http2EncodedAuthorityHostHeader = HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(H2StaticTable.Authority, hostHeader);
                    _http3EncodedAuthorityHostHeader = QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(H3StaticTable.Authority, hostHeader);
                }

                if (_http3Enabled)
                {
                    _sslOptionsHttp3 = ConstructSslOptions(poolManager, sslHostName);
                    _sslOptionsHttp3.ApplicationProtocols = s_http3ApplicationProtocols;
                }
            }

            // Set up for PreAuthenticate.  Access to this cache is guarded by a lock on the cache itself.
            if (_poolManager.Settings._preAuthenticate)
            {
                PreAuthCredentials = new CredentialCache();
            }

            if (NetEventSource.Log.IsEnabled()) Trace($"{this}");
        }

        private static readonly List<SslApplicationProtocol> s_http3ApplicationProtocols = new List<SslApplicationProtocol>() { Http3Connection.Http3ApplicationProtocol31, Http3Connection.Http3ApplicationProtocol30, Http3Connection.Http3ApplicationProtocol29 };
        private static readonly List<SslApplicationProtocol> s_http2ApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 };
        private static readonly List<SslApplicationProtocol> s_http2OnlyApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http2 };

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
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(poolManager, $"Win7OrWin2K8R2 platform, Changing default TLS protocols to {SecurityProtocol.DefaultSecurityProtocols}");
                }
                sslOptions.EnabledSslProtocols = SecurityProtocol.DefaultSecurityProtocols;
            }

            return sslOptions;
        }

        public HttpAuthority? OriginAuthority => _originAuthority;
        public HttpConnectionSettings Settings => _poolManager.Settings;
        public HttpConnectionKind Kind => _kind;
        public bool IsSecure => _kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel;
        public bool AnyProxyKind => (_proxyUri != null);
        public Uri? ProxyUri => _proxyUri;
        public ICredentials? ProxyCredentials => _poolManager.ProxyCredentials;
        public byte[]? HostHeaderValueBytes => _hostHeaderValueBytes;
        public CredentialCache? PreAuthCredentials { get; }

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

                    Debug.Assert(_originAuthority != null);
                    sb
                        .Append(_kind == HttpConnectionKind.Https ? "https://" : "http://")
                        .Append(_originAuthority.IdnHost);

                    if (_originAuthority.Port != (_kind == HttpConnectionKind.Https ? DefaultHttpsPort : DefaultHttpPort))
                    {
                        sb
                            .Append(':')
                            .Append(_originAuthority.Port.ToString(CultureInfo.InvariantCulture));
                    }

                    _http2AltSvcOriginUri = Encoding.ASCII.GetBytes(sb.ToString());
                }

                return _http2AltSvcOriginUri;
            }
        }

        public bool EnableMultipleHttp2Connections => _poolManager.Settings.EnableMultipleHttp2Connections;

        /// <summary>Object used to synchronize access to state in the pool.</summary>
        private object SyncObj => _idleConnections;

        private ValueTask<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>
            GetConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            // Do not even attempt at getting/creating a connection if it's already obvious we cannot provided the one requested.
            if (request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
            {
                if (request.Version.Major == 3 && !_http3Enabled)
                {
                    return ValueTask.FromException<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>(
                        new HttpRequestException(SR.Format(SR.net_http_requested_version_not_enabled, request.Version, request.VersionPolicy, 3)));
                }
                if (request.Version.Major == 2 && !_http2Enabled)
                {
                    return ValueTask.FromException<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>(
                        new HttpRequestException(SR.Format(SR.net_http_requested_version_not_enabled, request.Version, request.VersionPolicy, 2)));
                }
            }

            // Either H3 explicitly requested or secured upgraded allowed.
            if (_http3Enabled && (request.Version.Major >= 3 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)))
            {
                HttpAuthority? authority = _http3Authority;
                // H3 is explicitly requested, assume prenegotiated H3.
                if (request.Version.Major >= 3 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                {
                    authority = authority ?? _originAuthority;
                }
                if (authority != null)
                {
                    if (IsAltSvcBlocked(authority))
                    {
                        return ValueTask.FromException<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>(
                            new HttpRequestException(SR.Format(SR.net_http_requested_version_cannot_establish, request.Version, request.VersionPolicy, 3)));
                    }

                    return GetHttp3ConnectionAsync(request, authority, cancellationToken);
                }
            }

            // If we got here, we cannot provide HTTP/3 connection. Do not continue if downgrade is not allowed.
            if (request.Version.Major >= 3 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
            {
                return ValueTask.FromException<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>(
                    new HttpRequestException(SR.Format(SR.net_http_requested_version_cannot_establish, request.Version, request.VersionPolicy, 3)));
            }

            if (_http2Enabled && (request.Version.Major >= 2 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)) &&
               // If the connection is not secured and downgrade is possible, prefer HTTP/1.1.
               (request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower || IsSecure))
            {
                return GetHttp2ConnectionAsync(request, async, cancellationToken);
            }
            // If we got here, we cannot provide HTTP/2 connection. Do not continue if downgrade is not allowed.
            if (request.Version.Major >= 2 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
            {
                return ValueTask.FromException<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>(
                    new HttpRequestException(SR.Format(SR.net_http_requested_version_cannot_establish, request.Version, request.VersionPolicy, 2)));
            }

            return GetHttpConnectionAsync(request, async, cancellationToken);
        }

        private static bool IsUsableHttp11Connection(HttpConnection connection, long nowTicks, TimeSpan lifetime, bool async)
        {
            if (connection.LifetimeExpired(nowTicks, lifetime))
            {
                return false;
            }

            // Check to see if we've received anything on the connection; if we have, that's
            // either erroneous data (we shouldn't have received anything yet) or the connection
            // has been closed; either way, we can't use it.  If this is an async request, we
            // perform an async read on the stream, since we're going to need to read from it
            // anyway, and in doing so we can avoid the extra syscall.  For sync requests, we
            // try to directly poll the socket rather than doing an async read, so that we can
            // issue an appropriate sync read when we actually need it.  We don't have the
            // underlying socket in all cases, though, so PollRead may fall back to an async
            // read in some cases.
            return async ?
                !connection.EnsureReadAheadAndPollRead() :
                !connection.PollRead();
        }

        private ValueTask<HttpConnection?> GetOrReserveHttp11ConnectionAsync(bool async, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<HttpConnection?>(cancellationToken);
            }

            List<CachedConnection> idleConnections = _idleConnections;
            long nowTicks = Environment.TickCount64;

            // Look for a usable idle connection.
            // If we can't find one, we will either wait for one to become available (if at the connection limit)
            // or just increment the connection count and return null so the caller can create a new connection.
            TaskCompletionSourceWithCancellation<HttpConnection?> waiter;
            while (true)
            {
                HttpConnection connection;
                lock (SyncObj)
                {
                    if (idleConnections.Count > 0)
                    {
                        // We have a cached connection that we can attempt to use.
                        // Test it below outside the lock, to avoid doing expensive validation while holding the lock.
                        connection = idleConnections[idleConnections.Count - 1]._connection;
                        idleConnections.RemoveAt(idleConnections.Count - 1);
                    }
                    else
                    {
                        // No valid cached connections.
                        if (_associatedConnectionCount < _maxConnections)
                        {
                            // We are under the connection limit, so just increment the count and return null
                            // to indicate to the caller that they should create a new connection.
                            IncrementConnectionCountNoLock();
                            return new ValueTask<HttpConnection?>((HttpConnection?)null);
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

                if (IsUsableHttp11Connection(connection, nowTicks, _poolManager.Settings._pooledConnectionLifetime, async))
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Found usable connection in pool.");
                    return new ValueTask<HttpConnection?>(connection);
                }
                else
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Found invalid connection in pool.");
                    connection.Dispose();
                }
            }

            // We are at the connection limit. Wait for an available connection or connection count.
            if (NetEventSource.Log.IsEnabled()) Trace($"{(async ? "As" : "S")}ynchronous request. Connection limit reached, waiting for available connection.");

            if (HttpTelemetry.Log.IsEnabled())
            {
                return WaitOnWaiterWithTelemetryAsync(waiter, async, cancellationToken);
            }
            else
            {
                return waiter.WaitWithCancellationAsync(cancellationToken);
            }

            static async ValueTask<HttpConnection?> WaitOnWaiterWithTelemetryAsync(TaskCompletionSourceWithCancellation<HttpConnection?> waiter, bool async, CancellationToken cancellationToken)
            {
                ValueStopwatch stopwatch = ValueStopwatch.StartNew();

                HttpConnection? connection = await waiter.WaitWithCancellationAsync(cancellationToken).ConfigureAwait(false);

                HttpTelemetry.Log.Http11RequestLeftQueue(stopwatch.GetElapsedTime().TotalMilliseconds);
                return connection;
            }
        }

        private async ValueTask<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>
            GetHttpConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            HttpConnection? connection = await GetOrReserveHttp11ConnectionAsync(async, cancellationToken).ConfigureAwait(false);
            if (connection != null)
            {
                return (connection, false, null);
            }

            if (NetEventSource.Log.IsEnabled()) Trace("Creating new connection for pool.");

            try
            {
                HttpResponseMessage? failureResponse;
                (connection, failureResponse) = await CreateHttp11ConnectionAsync(request, async, cancellationToken).ConfigureAwait(false);
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

        private async ValueTask<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>
            GetHttp2ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel || _kind == HttpConnectionKind.Http);

            // See if we have an HTTP2 connection
            Http2Connection? http2Connection = GetExistingHttp2Connection();

            if (http2Connection != null)
            {
                // Connection exists and it is still good to use.
                if (NetEventSource.Log.IsEnabled()) Trace("Using existing HTTP2 connection.");
                _usedSinceLastCleanup = true;
                return (http2Connection, false, null);
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
            Stream? stream = null;
            SslStream? sslStream = null;
            TransportContext? transportContext = null;

            // Serialize creation attempt
            await _http2ConnectionCreateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                http2Connection = GetExistingHttp2Connection();
                if (http2Connection != null)
                {
                    return (http2Connection, false, null);
                }

                // Recheck if HTTP2 has been disabled by a previous attempt.
                if (_http2Enabled)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace("Attempting new HTTP2 connection.");
                    }

                    HttpResponseMessage? failureResponse;

                    (stream, transportContext, failureResponse) =
                        await ConnectAsync(request, async, cancellationToken).ConfigureAwait(false);

                    if (failureResponse != null)
                    {
                        return (null, true, failureResponse);
                    }

                    Debug.Assert(stream != null);

                    sslStream = stream as SslStream;

                    if (_kind == HttpConnectionKind.Http)
                    {
                        http2Connection = await ConstructHttp2ConnectionAsync(stream, request, cancellationToken).ConfigureAwait(false);

                        if (NetEventSource.Log.IsEnabled())
                        {
                            Trace("New unencrypted HTTP2 connection established.");
                        }

                        return (http2Connection, true, null);
                    }

                    Debug.Assert(sslStream != null);

                    if (sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
                    {
                        // The server accepted our request for HTTP2.

                        if (sslStream.SslProtocol < SslProtocols.Tls12)
                        {
                            sslStream.Dispose();
                            throw new HttpRequestException(SR.Format(SR.net_ssl_http2_requires_tls12, sslStream.SslProtocol));
                        }

                        http2Connection = await ConstructHttp2ConnectionAsync(stream, request, cancellationToken).ConfigureAwait(false);

                        if (NetEventSource.Log.IsEnabled())
                        {
                            Trace("New HTTP2 connection established.");
                        }

                        return (http2Connection, true, null);
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
                if (NetEventSource.Log.IsEnabled())
                {
                    Trace("Server does not support HTTP2; disabling HTTP2 use and proceeding with HTTP/1.1 connection");
                }

                bool canUse = true;
                lock (SyncObj)
                {
                    _http2Enabled = false;

                    if (request.Version.Major >= 2 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                    {
                        sslStream.Close();
                        throw new HttpRequestException(SR.Format(SR.net_http_requested_version_server_refused, request.Version, request.VersionPolicy));
                    }

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
                    return (await ConstructHttp11ConnectionAsync(async, stream!, transportContext, request, cancellationToken).ConfigureAwait(false), true, null);
                }
                else
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace("Discarding downgraded HTTP/1.1 connection because connection limit is exceeded");
                    }

                    stream!.Dispose();
                }
            }

            // If we reach this point, it means we need to fall back to a (new or existing) HTTP/1.1 connection.
            return await GetHttpConnectionAsync(request, async, cancellationToken).ConfigureAwait(false);
        }

        private Http2Connection? GetExistingHttp2Connection()
        {
            Http2Connection[]? localConnections = _http2Connections;

            if (localConnections == null)
            {
                return null;
            }

            for (int i = 0; i < localConnections.Length; i++)
            {
                Http2Connection http2Connection = localConnections[i];

                TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
                if (http2Connection.LifetimeExpired(Environment.TickCount64, pooledConnectionLifetime))
                {
                    // Connection expired.
                    http2Connection.Dispose();
                    InvalidateHttp2Connection(http2Connection);
                }
                else if (!EnableMultipleHttp2Connections || http2Connection.CanAddNewStream)
                {
                    return http2Connection;
                }
            }

            return null;
        }

        private void AddHttp2Connection(Http2Connection newConnection)
        {
            lock (SyncObj)
            {
                Http2Connection[]? localHttp2Connections = _http2Connections;
                int newCollectionSize = localHttp2Connections == null ? 1 : localHttp2Connections.Length + 1;
                Http2Connection[] newHttp2Connections = new Http2Connection[newCollectionSize];
                newHttp2Connections[0] = newConnection;

                if (localHttp2Connections != null)
                {
                    Array.Copy(localHttp2Connections, 0, newHttp2Connections, 1, localHttp2Connections.Length);
                }

                _http2Connections = newHttp2Connections;
            }
        }

        private async ValueTask<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>
            GetHttp3ConnectionAsync(HttpRequestMessage request, HttpAuthority authority, CancellationToken cancellationToken)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https);
            Debug.Assert(_http3Enabled == true);

            Http3Connection? http3Connection = Volatile.Read(ref _http3Connection);

            if (http3Connection != null)
            {
                TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
                if (http3Connection.LifetimeExpired(Environment.TickCount64, pooledConnectionLifetime) || http3Connection.Authority != authority)
                {
                    // Connection expired.
                    http3Connection.Dispose();
                    InvalidateHttp3Connection(http3Connection);
                }
                else
                {
                    // Connection exists and it is still good to use.
                    if (NetEventSource.Log.IsEnabled()) Trace("Using existing HTTP3 connection.");
                    _usedSinceLastCleanup = true;
                    return (http3Connection, false, null);
                }
            }

            // Ensure that the connection creation semaphore is created
            if (_http3ConnectionCreateLock == null)
            {
                lock (SyncObj)
                {
                    if (_http3ConnectionCreateLock == null)
                    {
                        _http3ConnectionCreateLock = new SemaphoreSlim(1);
                    }
                }
            }

            await _http3ConnectionCreateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_http3Connection != null)
                {
                    // Someone beat us to creating the connection.

                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace("Using existing HTTP3 connection.");
                    }

                    return (_http3Connection, false, null);
                }

                if (NetEventSource.Log.IsEnabled())
                {
                    Trace("Attempting new HTTP3 connection.");
                }

                QuicConnection quicConnection;
                try
                {
                    quicConnection = await ConnectHelper.ConnectQuicAsync(Settings._quicImplementationProvider ?? QuicImplementationProviders.Default, new DnsEndPoint(authority.IdnHost, authority.Port), _sslOptionsHttp3, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Disables HTTP/3 until server announces it can handle it via Alt-Svc.
                    BlocklistAuthority(authority);
                    throw;
                }

                //TODO: NegotiatedApplicationProtocol not yet implemented.
#if false
                if (quicConnection.NegotiatedApplicationProtocol != SslApplicationProtocol.Http3)
                {
                    BlocklistAuthority(authority);
                    throw new HttpRequestException("QUIC connected but no HTTP/3 indicated via ALPN.", null, RequestRetryType.RetryOnSameOrNextProxy);
                }
#endif

                http3Connection = new Http3Connection(this, _originAuthority, authority, quicConnection);
                _http3Connection = http3Connection;

                if (NetEventSource.Log.IsEnabled())
                {
                    Trace("New HTTP3 connection established.");
                }

                return (http3Connection, true, null);
            }
            finally
            {
                _http3ConnectionCreateLock.Release();
            }
        }

        public async ValueTask<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            while (true)
            {
                // Loop on connection failures and retry if possible.

                (HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse) = await GetConnectionAsync(request, async, cancellationToken).ConfigureAwait(false);
                if (failureResponse != null)
                {
                    // Proxy tunnel failure; return proxy response
                    Debug.Assert(isNewConnection);
                    Debug.Assert(connection == null);
                    return failureResponse;
                }

                HttpResponseMessage response;

                try
                {
                    if (connection is HttpConnection)
                    {
                        ((HttpConnection)connection).Acquire();
                        try
                        {
                            response = await (doRequestAuth && Settings._credentials != null ?
                                AuthenticationHelper.SendWithNtConnectionAuthAsync(request, async, Settings._credentials, (HttpConnection)connection, this, cancellationToken) :
                                SendWithNtProxyAuthAsync((HttpConnection)connection, request, async, cancellationToken)).ConfigureAwait(false);
                        }
                        finally
                        {
                            ((HttpConnection)connection).Release();
                        }
                    }
                    else
                    {
                        response = await connection!.SendAsync(request, async, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (HttpRequestException e) when (e.AllowRetry == RequestRetryType.RetryOnLowerHttpVersion)
                {
                    // Throw since fallback is not allowed by the version policy.
                    if (request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                    {
                        throw new HttpRequestException(SR.Format(SR.net_http_requested_version_server_refused, request.Version, request.VersionPolicy), e);
                    }

                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Retrying request after exception on existing connection: {e}");
                    }

                    // Eat exception and try again on a lower protocol version.
                    Debug.Assert(connection is HttpConnection == false, $"{nameof(RequestRetryType.RetryOnLowerHttpVersion)} should not be thrown by HTTP/1 connections.");
                    request.Version = HttpVersion.Version11;
                    continue;
                }
                catch (HttpRequestException e) when (!isNewConnection && e.AllowRetry == RequestRetryType.RetryOnSameOrNextProxy)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Retrying request after exception on existing connection: {e}");
                    }

                    // Eat exception and try again.
                    continue;
                }
                catch (HttpRequestException e) when (e.AllowRetry == RequestRetryType.RetryOnNextConnection)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Retrying request on another HTTP/2 connection after active streams limit is reached on existing one: {e}");
                    }

                    // Eat exception and try again.
                    continue;
                }

                // Check for the Alt-Svc header, to upgrade to HTTP/3.
                if (_altSvcEnabled && response.Headers.TryGetValues(KnownHeaders.AltSvc.Descriptor, out IEnumerable<string>? altSvcHeaderValues))
                {
                    HandleAltSvc(altSvcHeaderValues, response.Headers.Age);
                }

                // If an Alt-Svc authority returns 421, it means it can't actually handle the request.
                // An authority is supposed to be able to handle ALL requests to the origin, so this is a server bug.
                // In this case, we blocklist the authority and retry the request at the origin.
                if (response.StatusCode == HttpStatusCode.MisdirectedRequest && connection is Http3Connection h3Connection && h3Connection.Authority != _originAuthority)
                {
                    response.Dispose();
                    BlocklistAuthority(h3Connection.Authority);
                    continue;
                }

                return response;
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
                        ExpireAltSvcAuthority();
                        Debug.Assert(_authorityExpireTimer != null);
                        _authorityExpireTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        break;
                    }

                    if (nextAuthority == null && value != null && value.AlpnProtocolName == "h3")
                    {
                        var authority = new HttpAuthority(value.Host!, value.Port);

                        if (IsAltSvcBlocked(authority))
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
                    if (_authorityExpireTimer == null)
                    {
                        var thisRef = new WeakReference<HttpConnectionPool>(this);

                        bool restoreFlow = false;
                        try
                        {
                            if (!ExecutionContext.IsFlowSuppressed())
                            {
                                ExecutionContext.SuppressFlow();
                                restoreFlow = true;
                            }

                            _authorityExpireTimer = new Timer(static o =>
                            {
                                var wr = (WeakReference<HttpConnectionPool>)o!;
                                if (wr.TryGetTarget(out HttpConnectionPool? @this))
                                {
                                    @this.ExpireAltSvcAuthority();
                                }
                            }, thisRef, nextAuthorityMaxAge, Timeout.InfiniteTimeSpan);
                        }
                        finally
                        {
                            if (restoreFlow) ExecutionContext.RestoreFlow();
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
                    _poolManager.StartMonitoringNetworkChanges();
                }
            }
        }

        /// <summary>
        /// Expires the current Alt-Svc authority, resetting the connection back to origin.
        /// </summary>
        private void ExpireAltSvcAuthority()
        {
            // If we ever support prenegotiated HTTP/3, this should be set to origin, not nulled out.
            _http3Authority = null;
        }

        /// <summary>
        /// Checks whether the given <paramref name="authority"/> is on the currext Alt-Svc blocklist.
        /// </summary>
        /// <seealso cref="BlocklistAuthority" />
        private bool IsAltSvcBlocked(HttpAuthority authority)
        {
            if (_altSvcBlocklist != null)
            {
                lock (_altSvcBlocklist)
                {
                    return _altSvcBlocklist.Contains(authority);
                }
            }
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
        internal void BlocklistAuthority(HttpAuthority badAuthority)
        {
            Debug.Assert(badAuthority != null);

            HashSet<HttpAuthority>? altSvcBlocklist = _altSvcBlocklist;

            if (altSvcBlocklist == null)
            {
                lock (SyncObj)
                {
                    altSvcBlocklist = _altSvcBlocklist;
                    if (altSvcBlocklist == null)
                    {
                        altSvcBlocklist = new HashSet<HttpAuthority>();
                        _altSvcBlocklistTimerCancellation = new CancellationTokenSource();
                        _altSvcBlocklist = altSvcBlocklist;
                    }
                }
            }

            bool added, disabled = false;

            lock (altSvcBlocklist)
            {
                added = altSvcBlocklist.Add(badAuthority);

                if (added && altSvcBlocklist.Count >= MaxAltSvcIgnoreListSize && _altSvcEnabled)
                {
                    _altSvcEnabled = false;
                    disabled = true;
                }
            }

            lock (SyncObj)
            {
                if (_http3Authority == badAuthority)
                {
                    ExpireAltSvcAuthority();
                    Debug.Assert(_authorityExpireTimer != null);
                    _authorityExpireTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }

            Debug.Assert(_altSvcBlocklistTimerCancellation != null);
            if (added)
            {
               _ = Task.Delay(AltSvcBlocklistTimeoutInMilliseconds)
                    .ContinueWith(t =>
                    {
                        lock (altSvcBlocklist)
                        {
                            altSvcBlocklist.Remove(badAuthority);
                        }
                    }, _altSvcBlocklistTimerCancellation.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            if (disabled)
            {
                _ = Task.Delay(AltSvcBlocklistTimeoutInMilliseconds)
                    .ContinueWith(t =>
                    {
                        _altSvcEnabled = true;
                    }, _altSvcBlocklistTimerCancellation.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        public void OnNetworkChanged()
        {
            lock (SyncObj)
            {
                if (_http3Authority != null && _persistAuthority == false)
                {
                    ExpireAltSvcAuthority();
                    Debug.Assert(_authorityExpireTimer != null);
                    _authorityExpireTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        public async Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpConnection connection, HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            connection.Acquire();
            try
            {
                if (doRequestAuth && Settings._credentials != null)
                {
                    return await AuthenticationHelper.SendWithNtConnectionAuthAsync(request, async, Settings._credentials, connection, this, cancellationToken).ConfigureAwait(false);
                }

                return await SendWithNtProxyAuthAsync(connection, request, async, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                connection.Release();
            }
        }

        public Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpConnection connection, HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (AnyProxyKind && ProxyCredentials != null)
            {
                return AuthenticationHelper.SendWithNtProxyAuthAsync(request, ProxyUri!, async, ProxyCredentials, connection, this, cancellationToken);
            }

            return connection.SendAsync(request, async, cancellationToken);
        }


        public ValueTask<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if ((_kind == HttpConnectionKind.Proxy || _kind == HttpConnectionKind.ProxyConnect) &&
                _poolManager.ProxyCredentials != null)
            {
                return AuthenticationHelper.SendWithProxyAuthAsync(request, _proxyUri!, async, _poolManager.ProxyCredentials, doRequestAuth, this, cancellationToken);
            }

            return SendWithRetryAsync(request, async, doRequestAuth, cancellationToken);
        }

        public ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if (doRequestAuth && Settings._credentials != null)
            {
                return AuthenticationHelper.SendWithRequestAuthAsync(request, async, Settings._credentials, Settings._preAuthenticate, this, cancellationToken);
            }

            return SendWithProxyAuthAsync(request, async, doRequestAuth, cancellationToken);
        }

        private async ValueTask<(Stream?, TransportContext?, HttpResponseMessage?)> ConnectAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            // If a non-infinite connect timeout has been set, create and use a new CancellationToken that will be canceled
            // when either the original token is canceled or a connect timeout occurs.
            CancellationTokenSource? cancellationWithConnectTimeout = null;
            if (Settings._connectTimeout != Timeout.InfiniteTimeSpan)
            {
                cancellationWithConnectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationWithConnectTimeout.CancelAfter(Settings._connectTimeout);
                cancellationToken = cancellationWithConnectTimeout.Token;
            }

            try
            {
                Stream? stream = null;
                switch (_kind)
                {
                    case HttpConnectionKind.Http:
                    case HttpConnectionKind.Https:
                    case HttpConnectionKind.ProxyConnect:
                        Debug.Assert(_originAuthority != null);
                        stream = await ConnectToTcpHostAsync(_originAuthority.IdnHost, _originAuthority.Port, request, async, cancellationToken).ConfigureAwait(false);
                        break;

                    case HttpConnectionKind.Proxy:
                        stream = await ConnectToTcpHostAsync(_proxyUri!.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(false);
                        break;

                    case HttpConnectionKind.ProxyTunnel:
                    case HttpConnectionKind.SslProxyTunnel:
                        HttpResponseMessage? response;
                        (stream, response) = await EstablishProxyTunnelAsync(async, request.HasHeaders ? request.Headers : null, cancellationToken).ConfigureAwait(false);
                        if (response != null)
                        {
                            // Return non-success response from proxy.
                            response.RequestMessage = request;
                            return (null, null, response);
                        }
                        break;
                }

                Debug.Assert(stream != null);

                TransportContext? transportContext = null;
                if (IsSecure)
                {
                    SslStream sslStream = await ConnectHelper.EstablishSslConnectionAsync(GetSslOptionsForRequest(request), request, async, stream, cancellationToken).ConfigureAwait(false);
                    transportContext = sslStream.TransportContext;
                    stream = sslStream;
                }

                return (stream, transportContext, null);
            }
            finally
            {
                cancellationWithConnectTimeout?.Dispose();
            }
        }

        private async ValueTask<Stream> ConnectToTcpHostAsync(string host, int port, HttpRequestMessage initialRequest, bool async, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var endPoint = new DnsEndPoint(host, port);
            Socket? socket = null;
            try
            {
                // If a ConnectCallback was supplied, use that to establish the connection.
                if (Settings._connectCallback != null)
                {
                    ValueTask<Stream> streamTask = Settings._connectCallback(new SocketsHttpConnectionContext(endPoint, initialRequest), cancellationToken);

                    if (!async && !streamTask.IsCompleted)
                    {
                        // User-provided ConnectCallback is completing asynchronously but the user is making a synchronous request; if the user cares, they should
                        // set it up so that synchronous requests are made on a handler with a synchronously-completing ConnectCallback supplied. If in the future,
                        // we could add a Boolean to SocketsHttpConnectionContext (https://github.com/dotnet/runtime/issues/44876) to let the callback know whether
                        // this request is sync or async.
                        Trace($"{nameof(SocketsHttpHandler.ConnectCallback)} completing asynchronously for a synchronous request.");
                    }

                    return await streamTask.ConfigureAwait(false) ?? throw new HttpRequestException(SR.net_http_null_from_connect_callback);
                }
                else
                {
                    // Otherwise, create and connect a socket using default settings.
                    socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                    if (async)
                    {
                        await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        using (cancellationToken.UnsafeRegister(static s => ((Socket)s!).Dispose(), socket))
                        {
                            socket.Connect(endPoint);
                        }
                    }

                    return new NetworkStream(socket, ownsSocket: true);
                }
            }
            catch (Exception ex)
            {
                socket?.Dispose();
                throw ex is OperationCanceledException oce && oce.CancellationToken == cancellationToken ?
                    CancellationHelper.CreateOperationCanceledException(innerException: null, cancellationToken) :
                    ConnectHelper.CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
            }
        }

        internal async ValueTask<(HttpConnection?, HttpResponseMessage?)> CreateHttp11ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            (Stream? stream, TransportContext? transportContext, HttpResponseMessage? failureResponse) =
                await ConnectAsync(request, async, cancellationToken).ConfigureAwait(false);

            if (failureResponse != null)
            {
                return (null, failureResponse);
            }

            return (await ConstructHttp11ConnectionAsync(async, stream!, transportContext, request, cancellationToken).ConfigureAwait(false), null);
        }

        private SslClientAuthenticationOptions GetSslOptionsForRequest(HttpRequestMessage request)
        {
            if (_http2Enabled)
            {
                if (request.Version.Major >= 2 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                {
                    return _sslOptionsHttp2Only!;
                }

                if (request.Version.Major >= 2 || request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
                {
                    return _sslOptionsHttp2!;
                }
            }
            return _sslOptionsHttp11!;
        }

        private async ValueTask<Stream> ApplyPlaintextFilterAsync(bool async, Stream stream, Version httpVersion, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Settings._plaintextStreamFilter is null)
            {
                return stream;
            }

            Stream newStream;
            try
            {
                ValueTask<Stream> streamTask = Settings._plaintextStreamFilter(new SocketsHttpPlaintextStreamFilterContext(stream, httpVersion, request), cancellationToken);

                if (!async && !streamTask.IsCompleted)
                {
                    // User-provided PlaintextStreamFilter is completing asynchronously but the user is making a synchronous request; if the user cares, they should
                    // set it up so that synchronous requests are made on a handler with a synchronously-completing PlaintextStreamFilter supplied. If in the future,
                    // we could add a Boolean to SocketsHttpPlaintextStreamFilterContext (https://github.com/dotnet/runtime/issues/44876) to let the callback know whether
                    // this request is sync or async.
                    Trace($"{nameof(SocketsHttpHandler.PlaintextStreamFilter)} completing asynchronously for a synchronous request.");
                }

                newStream = await streamTask.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                stream.Dispose();
                throw new HttpRequestException(SR.net_http_exception_during_plaintext_filter, e);
            }

            if (newStream == null)
            {
                stream.Dispose();
                throw new HttpRequestException(SR.net_http_null_from_plaintext_filter);
            }

            return newStream;
        }

        private async ValueTask<HttpConnection> ConstructHttp11ConnectionAsync(bool async, Stream stream, TransportContext? transportContext, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            stream = await ApplyPlaintextFilterAsync(async, stream, HttpVersion.Version11, request, cancellationToken).ConfigureAwait(false);
            return new HttpConnection(this, stream, transportContext);
        }

        private async ValueTask<Http2Connection> ConstructHttp2ConnectionAsync(Stream stream, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            stream = await ApplyPlaintextFilterAsync(async: true, stream, HttpVersion.Version20, request, cancellationToken).ConfigureAwait(false);

            Http2Connection http2Connection = new Http2Connection(this, stream);
            await http2Connection.SetupAsync().ConfigureAwait(false);

            AddHttp2Connection(http2Connection);

            return http2Connection;
        }


        // Returns the established stream or an HttpResponseMessage from the proxy indicating failure.
        private async ValueTask<(Stream?, HttpResponseMessage?)> EstablishProxyTunnelAsync(bool async, HttpRequestHeaders? headers, CancellationToken cancellationToken)
        {
            Debug.Assert(_originAuthority != null);
            // Send a CONNECT request to the proxy server to establish a tunnel.
            HttpRequestMessage tunnelRequest = new HttpRequestMessage(HttpMethod.Connect, _proxyUri);
            tunnelRequest.Headers.Host = $"{_originAuthority.IdnHost}:{_originAuthority.Port}";    // This specifies destination host/port to connect to

            if (headers != null && headers.TryGetValues(HttpKnownHeaderNames.UserAgent, out IEnumerable<string>? values))
            {
                tunnelRequest.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.UserAgent, values);
            }

            HttpResponseMessage tunnelResponse = await _poolManager.SendProxyConnectAsync(tunnelRequest, _proxyUri!, async, cancellationToken).ConfigureAwait(false);

            if (tunnelResponse.StatusCode != HttpStatusCode.OK)
            {
                return (null, tunnelResponse);
            }

            Stream stream = tunnelResponse.Content.ReadAsStream(cancellationToken);

            return (stream, null);
        }

        /// <summary>Enqueues a waiter to the waiters list.</summary>
        private TaskCompletionSourceWithCancellation<HttpConnection?> EnqueueWaiter()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));
            Debug.Assert(Settings._maxConnectionsPerServer != int.MaxValue);
            Debug.Assert(_idleConnections.Count == 0, $"With {_idleConnections.Count} idle connections, we shouldn't have a waiter.");

            if (_waiters == null)
            {
                _waiters = new Queue<TaskCompletionSourceWithCancellation<HttpConnection?>>();
            }

            var waiter = new TaskCompletionSourceWithCancellation<HttpConnection?>();
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
        private TaskCompletionSourceWithCancellation<HttpConnection?> DequeueWaiter()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));
            Debug.Assert(Settings._maxConnectionsPerServer != int.MaxValue);
            Debug.Assert(_idleConnections.Count == 0, $"With {_idleConnections.Count} idle connections, we shouldn't have a waiter.");

            return _waiters!.Dequeue();
        }

        private void IncrementConnectionCountNoLock()
        {
            Debug.Assert(Monitor.IsEntered(SyncObj), $"Expected to be holding {nameof(SyncObj)}");

            if (NetEventSource.Log.IsEnabled()) Trace(null);
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

        private bool TransferConnection(HttpConnection? connection)
        {
            Debug.Assert(Monitor.IsEntered(SyncObj));

            while (HasWaiter())
            {
                TaskCompletionSource<HttpConnection?> waiter = DequeueWaiter();

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
            if (NetEventSource.Log.IsEnabled()) Trace(null);
            lock (SyncObj)
            {
                Debug.Assert(_associatedConnectionCount > 0 && _associatedConnectionCount <= _maxConnections,
                    $"Expected 0 < {_associatedConnectionCount} <= {_maxConnections}");

                // Mark the pool as not being stale.
                _usedSinceLastCleanup = true;

                if (TransferConnection(null))
                {
                    if (NetEventSource.Log.IsEnabled()) Trace("Transferred connection count to waiter.");
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
                            if (NetEventSource.Log.IsEnabled()) connection.Trace("Transferred connection to waiter.");
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
                        if (NetEventSource.Log.IsEnabled()) connection.Trace("Stored connection in pool.");
                        return;
                    }
                }
            }

            // The connection could be not be reused.  Dispose of it.
            // Disposing it will alert any waiters that a connection slot has become available.
            if (NetEventSource.Log.IsEnabled())
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
                Http2Connection[]? localHttp2Connections = _http2Connections;

                if (localHttp2Connections == null)
                {
                    return;
                }

                if (localHttp2Connections.Length == 1)
                {
                    // Fast shortcut for the most common case.
                    if (localHttp2Connections[0] == connection)
                    {
                        _http2Connections = null;
                    }
                    return;
                }

                int invalidatedIndex = Array.IndexOf(localHttp2Connections, connection);
                if (invalidatedIndex >= 0)
                {
                    Http2Connection[] newHttp2Connections = new Http2Connection[localHttp2Connections.Length - 1];

                    if (invalidatedIndex > 0)
                    {
                        Array.Copy(localHttp2Connections, newHttp2Connections, invalidatedIndex);
                    }

                    if (invalidatedIndex < localHttp2Connections.Length - 1)
                    {
                        Array.Copy(localHttp2Connections, invalidatedIndex + 1, newHttp2Connections, invalidatedIndex, newHttp2Connections.Length - invalidatedIndex);
                    }

                    _http2Connections = newHttp2Connections;
                }
            }
        }

        public void InvalidateHttp3Connection(Http3Connection connection)
        {
            lock (SyncObj)
            {
                if (_http3Connection == connection)
                {
                    _http3Connection = null;
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
                    if (NetEventSource.Log.IsEnabled()) Trace("Disposing pool.");
                    _disposed = true;
                    list.ForEach(c => c._connection.Dispose());
                    list.Clear();

                    if (_http2Connections != null)
                    {
                        for (int i = 0; i < _http2Connections.Length; i++)
                        {
                            _http2Connections[i].Dispose();
                        }
                        _http2Connections = null;
                    }

                    if (_authorityExpireTimer != null)
                    {
                        _authorityExpireTimer.Dispose();
                        _authorityExpireTimer = null;
                    }

                    if (_altSvcBlocklistTimerCancellation != null)
                    {
                        _altSvcBlocklistTimerCancellation.Cancel();
                        _altSvcBlocklistTimerCancellation.Dispose();
                        _altSvcBlocklistTimerCancellation = null;
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
            List<HttpConnection>? toDispose = null;
            bool tookLock = false;

            try
            {
                if (NetEventSource.Log.IsEnabled()) Trace("Cleaning pool.");
                Monitor.Enter(SyncObj, ref tookLock);

                // Get the current time.  This is compared against each connection's last returned
                // time to determine whether a connection is too old and should be closed.
                long nowTicks = Environment.TickCount64;
                // Copy the reference to a local variable to simplify the removal logic below.
                Http2Connection[]? localHttp2Connections = _http2Connections;

                if (localHttp2Connections != null)
                {
                    Http2Connection[]? newHttp2Connections = null;
                    int newIndex = 0;
                    for (int i = 0; i < localHttp2Connections.Length; i++)
                    {
                        Http2Connection http2Connection = localHttp2Connections[i];
                        if (http2Connection.IsExpired(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
                        {
                            http2Connection.Dispose();

                            if (newHttp2Connections == null)
                            {
                                newHttp2Connections = new Http2Connection[localHttp2Connections.Length];
                                if (i > 0)
                                {
                                    // Copy valid connections residing at the beggining of the current collection.
                                    Array.Copy(localHttp2Connections, newHttp2Connections, i);
                                    newIndex = i;
                                }
                            }
                        }
                        else if (newHttp2Connections != null)
                        {
                            newHttp2Connections[newIndex] = localHttp2Connections[i];
                            newIndex++;
                        }
                    }

                    if (newHttp2Connections != null)
                    {
                        //Some connections have been removed, so _http2Connections must be replaced.
                        if (newIndex > 0)
                        {
                            Array.Resize(ref newHttp2Connections, newIndex);
                            _http2Connections = newHttp2Connections;
                        }
                        else
                        {
                            // All connections expired.
                            _http2Connections = null;
                        }
                    }
                }

                // Find the first item which needs to be removed.
                int freeIndex = 0;
                while (freeIndex < list.Count && list[freeIndex].IsUsable(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
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
                        while (current < list.Count && !list[current].IsUsable(nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
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
                    if (_associatedConnectionCount == 0 && !_usedSinceLastCleanup && _http2Connections == null)
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

        internal void HeartBeat()
        {
            Http2Connection[]? localHttp2Connections = _http2Connections;
            if (localHttp2Connections != null)
            {
                foreach (Http2Connection http2Connection in localHttp2Connections)
                {
                    http2Connection.HeartBeat();
                }
            }
        }


        // For diagnostic purposes
        public override string ToString() =>
            $"{nameof(HttpConnectionPool)} " +
            (_proxyUri == null ?
                (_sslOptionsHttp11 == null ?
                    $"http://{_originAuthority}" :
                    $"https://{_originAuthority}" + (_sslOptionsHttp11.TargetHost != _originAuthority!.IdnHost ? $", SSL TargetHost={_sslOptionsHttp11.TargetHost}" : null)) :
                (_sslOptionsHttp11 == null ?
                    $"Proxy {_proxyUri}" :
                    $"https://{_originAuthority}/ tunnelled via Proxy {_proxyUri}" + (_sslOptionsHttp11.TargetHost != _originAuthority!.IdnHost ? $", SSL TargetHost={_sslOptionsHttp11.TargetHost}" : null)));

        private void Trace(string? message, [CallerMemberName] string? memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                GetHashCode(),               // pool ID
                0,                           // connection ID
                0,                           // request ID
                memberName,                  // method name
                message);                    // message

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
                TimeSpan pooledConnectionIdleTimeout)
            {
                // Validate that the connection hasn't been idle in the pool for longer than is allowed.
                if ((pooledConnectionIdleTimeout != Timeout.InfiniteTimeSpan) &&
                    ((nowTicks - _returnedTickCount) > pooledConnectionIdleTimeout.TotalMilliseconds))
                {
                    if (NetEventSource.Log.IsEnabled()) _connection.Trace($"Connection no longer usable. Idle {TimeSpan.FromMilliseconds((nowTicks - _returnedTickCount))} > {pooledConnectionIdleTimeout}.");
                    return false;
                }

                return IsUsableHttp11Connection(_connection, nowTicks, pooledConnectionLifetime, false);
            }

            public bool Equals(CachedConnection other) => ReferenceEquals(other._connection, _connection);
            public override bool Equals([NotNullWhen(true)] object? obj) => obj is CachedConnection && Equals((CachedConnection)obj);
            public override int GetHashCode() => _connection?.GetHashCode() ?? 0;
        }
    }
}
