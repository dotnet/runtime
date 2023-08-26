// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Http.Metrics;
using System.Net.Http.QPack;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
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
        private readonly Uri? _proxyUri;

        /// <summary>The origin authority used to construct the <see cref="HttpConnectionPool"/>.</summary>
        private readonly HttpAuthority _originAuthority;

        /// <summary>Initially set to null, this can be set to enable HTTP/3 based on Alt-Svc.</summary>
        private volatile HttpAuthority? _http3Authority;

        /// <summary>A timer to expire <see cref="_http3Authority"/> and return the pool to <see cref="_originAuthority"/>. Initialized on first use.</summary>
        private Timer? _authorityExpireTimer;

        /// <summary>If true, the <see cref="_http3Authority"/> will persist across a network change. If false, it will be reset to <see cref="_originAuthority"/>.</summary>
        private bool _persistAuthority;

        /// <summary>The User-Agent header to use when creating a CONNECT tunnel.</summary>
        private string? _connectTunnelUserAgent;

        /// <summary>
        /// When an Alt-Svc authority fails due to 421 Misdirected Request, it is placed in the blocklist to be ignored
        /// for <see cref="AltSvcBlocklistTimeoutInMilliseconds"/> milliseconds. Initialized on first use.
        /// </summary>
        private volatile Dictionary<HttpAuthority, Exception?>? _altSvcBlocklist;
        private CancellationTokenSource? _altSvcBlocklistTimerCancellation;
        private volatile bool _altSvcEnabled = true;

        /// <summary>The maximum number of times to retry a request after a failure on an established connection.</summary>
        private const int MaxConnectionFailureRetries = 3;

        /// <summary>
        /// If <see cref="_altSvcBlocklist"/> exceeds this size, Alt-Svc will be disabled entirely for <see cref="AltSvcBlocklistTimeoutInMilliseconds"/> milliseconds.
        /// This is to prevent a failing server from bloating the dictionary beyond a reasonable value.
        /// </summary>
        private const int MaxAltSvcIgnoreListSize = 8;

        /// <summary>The time, in milliseconds, that an authority should remain in <see cref="_altSvcBlocklist"/>.</summary>
        private const int AltSvcBlocklistTimeoutInMilliseconds = 10 * 60 * 1000;

        // HTTP/1.1 connection pool

        /// <summary>List of available HTTP/1.1 connections stored in the pool.</summary>
        private readonly List<HttpConnection> _availableHttp11Connections = new List<HttpConnection>();
        /// <summary>The maximum number of HTTP/1.1 connections allowed to be associated with the pool.</summary>
        private readonly int _maxHttp11Connections;
        /// <summary>The number of HTTP/1.1 connections associated with the pool, including in use, available, and pending.</summary>
        private int _associatedHttp11ConnectionCount;
        /// <summary>The number of HTTP/1.1 connections that are in the process of being established.</summary>
        private int _pendingHttp11ConnectionCount;
        /// <summary>Queue of requests waiting for an HTTP/1.1 connection.</summary>
        private RequestQueue<HttpConnection> _http11RequestQueue;

        // HTTP/2 connection pool

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
        private bool _http3Enabled;
        private Http3Connection? _http3Connection;
        private SemaphoreSlim? _http3ConnectionCreateLock;
        internal readonly byte[]? _http3EncodedAuthorityHostHeader;

        // These settings are advertised by the server via SETTINGS_MAX_HEADER_LIST_SIZE and SETTINGS_MAX_FIELD_SECTION_SIZE.
        // If we had previous connections to the same host in this pool, memorize the last value seen.
        // This value is used as an initial value for new connections before they have a chance to observe the SETTINGS frame.
        // Doing so avoids immediately exceeding the server limit on the first request, potentially causing the connection to be torn down.
        // 0 means there were no previous connections, or they hadn't advertised this limit.
        // There is no need to lock when updating these values - we're only interested in saving _a_ value, not necessarily the min/max/last.
        internal uint _lastSeenHttp2MaxHeaderListSize;
        internal uint _lastSeenHttp3MaxHeaderListSize;

        /// <summary>For non-proxy connection pools, this is the host name in bytes; for proxies, null.</summary>
        private readonly byte[]? _hostHeaderLineBytes;
        /// <summary>Options specialized and cached for this pool and its key.</summary>
        private readonly SslClientAuthenticationOptions? _sslOptionsHttp11;
        private readonly SslClientAuthenticationOptions? _sslOptionsHttp2;
        private readonly SslClientAuthenticationOptions? _sslOptionsHttp2Only;
        private SslClientAuthenticationOptions? _sslOptionsHttp3;
        private SslClientAuthenticationOptions? _sslOptionsProxy;

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
        public HttpConnectionPool(HttpConnectionPoolManager poolManager, HttpConnectionKind kind, string? host, int port, string? sslHostName, Uri? proxyUri)
        {
            _poolManager = poolManager;
            _kind = kind;
            _proxyUri = proxyUri;
            _maxHttp11Connections = Settings._maxConnectionsPerServer;

            // The only case where 'host' will not be set is if this is a Proxy connection pool.
            Debug.Assert(host is not null || (kind == HttpConnectionKind.Proxy && proxyUri is not null));
            _originAuthority = new HttpAuthority(host ?? proxyUri!.IdnHost, port);

            _http2Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version20;

            if (IsHttp3Supported())
            {
                _http3Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version30;
            }

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

                    // Don't enforce the max connections limit on proxy tunnels; this would mean that connections to different origin servers
                    // would compete for the same limited number of connections.
                    // We will still enforce this limit on the user of the tunnel (i.e. ProxyTunnel or SslProxyTunnel).
                    _maxHttp11Connections = int.MaxValue;

                    _http2Enabled = false;
                    _http3Enabled = false;
                    break;

                case HttpConnectionKind.SocksTunnel:
                case HttpConnectionKind.SslSocksTunnel:
                    Debug.Assert(host != null);
                    Debug.Assert(port != 0);
                    Debug.Assert(proxyUri != null);

                    _http3Enabled = false; // TODO: SOCKS supports UDP and may be used for HTTP3
                    break;

                default:
                    Debug.Fail("Unknown HttpConnectionKind in HttpConnectionPool.ctor");
                    break;
            }

            if (!_http3Enabled)
            {
                // Avoid parsing Alt-Svc headers if they won't be used.
                _altSvcEnabled = false;
            }

            string? hostHeader = null;
            if (host is not null)
            {
                // Precalculate ASCII bytes for Host header
                // Note that if _host is null, this is a (non-tunneled) proxy connection, and we can't cache the hostname.
                hostHeader = IsDefaultPort
                    ? _originAuthority.HostValue
                    : $"{_originAuthority.HostValue}:{_originAuthority.Port}";

                // Note the IDN hostname should always be ASCII, since it's already been IDNA encoded.
                byte[] hostHeaderLine = new byte[6 + hostHeader.Length + 2]; // Host: foo\r\n
                "Host: "u8.CopyTo(hostHeaderLine);
                Encoding.ASCII.GetBytes(hostHeader, hostHeaderLine.AsSpan(6));
                hostHeaderLine[^2] = (byte)'\r';
                hostHeaderLine[^1] = (byte)'\n';
                _hostHeaderLineBytes = hostHeaderLine;

                Debug.Assert(Encoding.ASCII.GetString(_hostHeaderLineBytes) == $"Host: {hostHeader}\r\n");

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
            }

            // Set up for PreAuthenticate.  Access to this cache is guarded by a lock on the cache itself.
            if (_poolManager.Settings._preAuthenticate)
            {
                PreAuthCredentials = new CredentialCache();
            }

            _http11RequestQueue = new RequestQueue<HttpConnection>();
            if (_http2Enabled)
            {
                _http2RequestQueue = new RequestQueue<Http2Connection?>();
            }

            if (_proxyUri != null && HttpUtilities.IsSupportedSecureScheme(_proxyUri.Scheme))
            {
                _sslOptionsProxy = ConstructSslOptions(poolManager, _proxyUri.IdnHost);
                _sslOptionsProxy.ApplicationProtocols = null;
            }

            if (NetEventSource.Log.IsEnabled()) Trace($"{this}");
        }

        [SupportedOSPlatformGuard("linux")]
        [SupportedOSPlatformGuard("macOS")]
        [SupportedOSPlatformGuard("Windows")]
        internal static bool IsHttp3Supported() => (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()) || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

        private static readonly List<SslApplicationProtocol> s_http3ApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http3 };
        private static readonly List<SslApplicationProtocol> s_http2ApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 };
        private static readonly List<SslApplicationProtocol> s_http2OnlyApplicationProtocols = new List<SslApplicationProtocol>() { SslApplicationProtocol.Http2 };

        private static SslClientAuthenticationOptions ConstructSslOptions(HttpConnectionPoolManager poolManager, string sslHostName)
        {
            Debug.Assert(sslHostName != null);

            SslClientAuthenticationOptions sslOptions = poolManager.Settings._sslOptions?.ShallowClone() ?? new SslClientAuthenticationOptions();

            // This is only set if we are underlying handler for HttpClientHandler
            if (poolManager.Settings._clientCertificateOptions == ClientCertificateOption.Manual && sslOptions.LocalCertificateSelectionCallback != null &&
                    (sslOptions.ClientCertificates == null || sslOptions.ClientCertificates.Count == 0))
            {
                // If we have no client certificates do not set callback when internal selection is used.
                // It breaks TLS resume on Linux
                sslOptions.LocalCertificateSelectionCallback = null;
            }

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

        public HttpAuthority OriginAuthority => _originAuthority;
        public HttpConnectionSettings Settings => _poolManager.Settings;
        public HttpConnectionKind Kind => _kind;
        public bool IsSecure => _kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel || _kind == HttpConnectionKind.SslSocksTunnel;
        public Uri? ProxyUri => _proxyUri;
        public ICredentials? ProxyCredentials => _poolManager.ProxyCredentials;
        public byte[]? HostHeaderLineBytes => _hostHeaderLineBytes;
        public CredentialCache? PreAuthCredentials { get; }
        public bool IsDefaultPort => OriginAuthority.Port == (IsSecure ? DefaultHttpsPort : DefaultHttpPort);

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

        /// <summary>Object used to synchronize access to state in the pool.</summary>
        private object SyncObj
        {
            get
            {
                Debug.Assert(!Monitor.IsEntered(_availableHttp11Connections));
                return _availableHttp11Connections;
            }
        }

        private bool HasSyncObjLock => Monitor.IsEntered(_availableHttp11Connections);

        // Overview of connection management (mostly HTTP version independent):
        //
        // Each version of HTTP (1.1, 2, 3) has its own connection pool, and each of these work in a similar manner,
        // allowing for differences between the versions (most notably, HTTP/1.1 is not multiplexed.)
        //
        // When a request is submitted for a particular version (e.g. HTTP/1.1), we first look in the pool for available connections.
        // An "available" connection is one that is (hopefully) usable for a new request.
        //      For HTTP/1.1, this is just an idle connection.
        //      For HTTP2/3, this is a connection that (hopefully) has available streams to use for new requests.
        // If we find an available connection, we will attempt to validate it and then use it.
        //      We check the lifetime of the connection and discard it if the lifetime is exceeded.
        //      We check that the connection has not shut down; if so we discard it.
        //      For HTTP2/3, we reserve a stream on the connection. If this fails, we cannot use the connection right now.
        // If validation fails, we will attempt to find a different available connection.
        //
        // Once we have found a usable connection, we use it to process the request.
        //      For HTTP/1.1, a connection can handle only a single request at a time, thus it is immediately removed from the list of available connections.
        //      For HTTP2/3, a connection is only removed from the available list when it has no more available streams.
        //      In either case, the connection still counts against the total associated connection count for the pool.
        //
        // If we cannot find a usable available connection, then the request is added the to the request queue for the appropriate version.
        //
        // Whenever a request is queued, or an existing connection shuts down, we will check to see if we should inject a new connection.
        // Injection policy depends on both user settings and some simple heuristics.
        // See comments on the relevant routines for details on connection injection policy.
        //
        // When a new connection is successfully created, or an existing unavailable connection becomes available again,
        // we will attempt to use this connection to handle any queued requests (subject to lifetime restrictions on existing connections).
        // This may result in the connection becoming unavailable again, because it cannot handle any more requests at the moment.
        // If not, we will return the connection to the pool as an available connection for use by new requests.
        //
        // When a connection shuts down, either gracefully (e.g. GOAWAY) or abortively (e.g. IOException),
        // we will remove it from the list of available connections, if it is present there.
        // If not, then it must be unavailable at the moment; we will detect this and ensure it is not added back to the available pool.

        [DoesNotReturn]
        private static void ThrowGetVersionException(HttpRequestMessage request, int desiredVersion, Exception? inner = null)
        {
            Debug.Assert(desiredVersion == 2 || desiredVersion == 3);

            HttpRequestException ex = new(HttpRequestError.VersionNegotiationError, SR.Format(SR.net_http_requested_version_cannot_establish, request.Version, request.VersionPolicy, desiredVersion), inner);
            if (request.IsExtendedConnectRequest && desiredVersion == 2)
            {
                ex.Data["HTTP2_ENABLED"] = false;
            }

            throw ex;
        }

        private bool CheckExpirationOnGet(HttpConnectionBase connection)
        {
            TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
            if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
            {
                return connection.GetLifetimeTicks(Environment.TickCount64) > pooledConnectionLifetime.TotalMilliseconds;
            }

            return false;
        }

        private static Exception CreateConnectTimeoutException(OperationCanceledException oce)
        {
            // The pattern for request timeouts (on HttpClient) is to throw an OCE with an inner exception of TimeoutException.
            // Do the same for ConnectTimeout-based timeouts.
            TimeoutException te = new TimeoutException(SR.net_http_connect_timedout, oce.InnerException);
            Exception newException = CancellationHelper.CreateOperationCanceledException(te, oce.CancellationToken);
            ExceptionDispatchInfo.SetCurrentStackTrace(newException);
            return newException;
        }

        private async Task AddHttp11ConnectionAsync(RequestQueue<HttpConnection>.QueueItem queueItem)
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
                ReturnHttp11Connection(connection, isNewConnection: true, queueItem.Waiter);
            }
            else
            {
                Debug.Assert(connectionException is not null);
                HandleHttp11ConnectionFailure(waiter, connectionException);
            }
        }

        private void CheckForHttp11ConnectionInjection()
        {
            Debug.Assert(HasSyncObjLock);

            _http11RequestQueue.PruneCompletedRequestsFromHeadOfQueue(this);

            // Determine if we can and should add a new connection to the pool.
            bool willInject = _availableHttp11Connections.Count == 0 &&             // No available connections
                _http11RequestQueue.Count > _pendingHttp11ConnectionCount &&        // More requests queued than pending connections
                _associatedHttp11ConnectionCount < _maxHttp11Connections &&         // Under the connection limit
                _http11RequestQueue.RequestsWithoutAConnectionAttempt > 0;          // There are requests we haven't issued a connection attempt for

            if (NetEventSource.Log.IsEnabled())
            {
                Trace($"Available HTTP/1.1 connections: {_availableHttp11Connections.Count}, Requests in the queue: {_http11RequestQueue.Count}, " +
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
                _ = AddHttp11ConnectionAsync(queueItem); // ignore returned task
            }
        }

        private bool TryGetPooledHttp11Connection(HttpRequestMessage request, bool async, [NotNullWhen(true)] out HttpConnection? connection, [NotNullWhen(false)] out HttpConnectionWaiter<HttpConnection>? waiter)
        {
            while (true)
            {
                lock (SyncObj)
                {
                    _usedSinceLastCleanup = true;

                    int availableConnectionCount = _availableHttp11Connections.Count;
                    if (availableConnectionCount > 0)
                    {
                        // We have a connection that we can attempt to use.
                        // Validate it below outside the lock, to avoid doing expensive operations while holding the lock.
                        connection = _availableHttp11Connections[availableConnectionCount - 1];
                        _availableHttp11Connections.RemoveAt(availableConnectionCount - 1);
                    }
                    else
                    {
                        // No available connections. Add to the request queue.
                        waiter = _http11RequestQueue.EnqueueRequest(request);

                        CheckForHttp11ConnectionInjection();

                        // There were no available idle connections. This request has been added to the request queue.
                        if (NetEventSource.Log.IsEnabled()) Trace($"No available HTTP/1.1 connections; request queued.");
                        connection = null;
                        return false;
                    }
                }

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

            ReturnHttp11Connection(http11Connection, isNewConnection: true);
        }

        private async Task AddHttp2ConnectionAsync(RequestQueue<Http2Connection?>.QueueItem queueItem)
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
                    $"Pending HTTP/2.0 connection: {_pendingHttp2Connection}" +
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
                _ = AddHttp2ConnectionAsync(queueItem); // ignore returned task
            }
        }

        private bool TryGetPooledHttp2Connection(HttpRequestMessage request, [NotNullWhen(true)] out Http2Connection? connection, out HttpConnectionWaiter<Http2Connection?>? waiter)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel || _kind == HttpConnectionKind.Http || _kind == HttpConnectionKind.SocksTunnel || _kind == HttpConnectionKind.SslSocksTunnel);

            // Look for a usable connection.
            while (true)
            {
                lock (SyncObj)
                {
                    _usedSinceLastCleanup = true;

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

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private async ValueTask<Http3Connection> GetHttp3ConnectionAsync(HttpRequestMessage request, HttpAuthority authority, CancellationToken cancellationToken)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https);
            Debug.Assert(_http3Enabled);

            Http3Connection? http3Connection = Volatile.Read(ref _http3Connection);

            if (http3Connection != null)
            {
                if (CheckExpirationOnGet(http3Connection) || http3Connection.Authority != authority)
                {
                    // Connection expired.
                    if (NetEventSource.Log.IsEnabled()) http3Connection.Trace("Found expired HTTP3 connection.");
                    http3Connection.Dispose();
                    InvalidateHttp3Connection(http3Connection);
                }
                else
                {
                    // Connection exists and it is still good to use.
                    if (NetEventSource.Log.IsEnabled()) Trace("Using existing HTTP3 connection.");
                    _usedSinceLastCleanup = true;
                    return http3Connection;
                }
            }

            // Ensure that the connection creation semaphore is created
            if (_http3ConnectionCreateLock == null)
            {
                lock (SyncObj)
                {
                    _http3ConnectionCreateLock ??= new SemaphoreSlim(1);
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

                    return _http3Connection;
                }

                if (NetEventSource.Log.IsEnabled())
                {
                    Trace("Attempting new HTTP3 connection.");
                }

                QuicConnection quicConnection;
                try
                {
                    quicConnection = await ConnectHelper.ConnectQuicAsync(request, new DnsEndPoint(authority.IdnHost, authority.Port), _poolManager.Settings._pooledConnectionIdleTimeout, _sslOptionsHttp3!, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"QUIC connection failed: {ex}");

                    // Block list authority only if the connection attempt was not cancelled.
                    if (ex is not OperationCanceledException oce || !cancellationToken.IsCancellationRequested || oce.CancellationToken != cancellationToken)
                    {
                        // Disables HTTP/3 until server announces it can handle it via Alt-Svc.
                        BlocklistAuthority(authority, ex);
                    }
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
                // if the authority was sent as an option through alt-svc then include alt-used header
                http3Connection = new Http3Connection(this, authority, quicConnection, includeAltUsedHeader: _http3Authority == authority);
                _http3Connection = http3Connection;

                if (NetEventSource.Log.IsEnabled())
                {
                    Trace("New HTTP3 connection established.");
                }

                return http3Connection;
            }
            finally
            {
                _http3ConnectionCreateLock.Release();
            }
        }

        // Returns null if HTTP3 cannot be used.
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private async ValueTask<HttpResponseMessage?> TrySendUsingHttp3Async(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Loop in case we get a 421 and need to send the request to a different authority.
            while (true)
            {
                HttpAuthority? authority = _http3Authority;

                // If H3 is explicitly requested, assume prenegotiated H3.
                if (request.Version.Major >= 3 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                {
                    authority ??= _originAuthority;
                }

                if (authority == null)
                {
                    return null;
                }

                Exception? reasonException;
                if (IsAltSvcBlocked(authority, out reasonException))
                {
                    ThrowGetVersionException(request, 3, reasonException);
                }

                long queueStartingTimestamp = HttpTelemetry.Log.IsEnabled() || Settings._metrics!.RequestsQueueDuration.Enabled ? Stopwatch.GetTimestamp() : 0;

                ValueTask<Http3Connection> connectionTask = GetHttp3ConnectionAsync(request, authority, cancellationToken);

                Http3Connection connection = await connectionTask.ConfigureAwait(false);

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

        /// <summary>Check for the Alt-Svc header, to upgrade to HTTP/3.</summary>
        private void ProcessAltSvc(HttpResponseMessage response)
        {
            if (_altSvcEnabled && response.Headers.TryGetValues(KnownHeaders.AltSvc.Descriptor, out IEnumerable<string>? altSvcHeaderValues))
            {
                HandleAltSvc(altSvcHeaderValues, response.Headers.Age);
            }
        }

        public async ValueTask<HttpResponseMessage> SendWithVersionDetectionAndRetryAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            // Loop on connection failures (or other problems like version downgrade) and retry if possible.
            int retryCount = 0;
            while (true)
            {
                HttpConnectionWaiter<HttpConnection>? http11ConnectionWaiter = null;
                HttpConnectionWaiter<Http2Connection?>? http2ConnectionWaiter = null;
                try
                {
                    HttpResponseMessage? response = null;

                    // Use HTTP/3 if possible.
                    if (IsHttp3Supported() && // guard to enable trimming HTTP/3 support
                        _http3Enabled &&
                        (request.Version.Major >= 3 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)) &&
                        !request.IsExtendedConnectRequest)
                    {
                        Debug.Assert(async);
                        if (QuicConnection.IsSupported)
                        {
                            if (_sslOptionsHttp3 == null)
                            {
                                // deferred creation. We use atomic exchange to be sure all threads point to single object to mimic ctor behavior.
                                SslClientAuthenticationOptions sslOptionsHttp3 = ConstructSslOptions(_poolManager, _sslOptionsHttp11!.TargetHost!);
                                sslOptionsHttp3.ApplicationProtocols = s_http3ApplicationProtocols;
                                Interlocked.CompareExchange(ref _sslOptionsHttp3, sslOptionsHttp3, null);
                            }

                            response = await TrySendUsingHttp3Async(request, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            _altSvcEnabled = false;
                            _http3Enabled = false;
                        }
                    }

                    if (response is null)
                    {
                        // We could not use HTTP/3. Do not continue if downgrade is not allowed.
                        if (request.Version.Major >= 3 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                        {
                            ThrowGetVersionException(request, 3);
                        }

                        // Use HTTP/2 if possible.
                        if (_http2Enabled &&
                            (request.Version.Major >= 2 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)) &&
                            (request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower || IsSecure)) // prefer HTTP/1.1 if connection is not secured and downgrade is possible
                        {
                            if (!TryGetPooledHttp2Connection(request, out Http2Connection? connection, out http2ConnectionWaiter) &&
                                http2ConnectionWaiter != null)
                            {
                                connection = await http2ConnectionWaiter.WaitForConnectionAsync(request, this, async, cancellationToken).ConfigureAwait(false);
                            }

                            Debug.Assert(connection is not null || !_http2Enabled);
                            if (connection is not null)
                            {
                                if (request.IsExtendedConnectRequest)
                                {
                                    await connection.InitialSettingsReceived.WaitWithCancellationAsync(cancellationToken).ConfigureAwait(false);
                                    if (!connection.IsConnectEnabled)
                                    {
                                        HttpRequestException exception = new(HttpRequestError.ExtendedConnectNotSupported, SR.net_unsupported_extended_connect);
                                        exception.Data["SETTINGS_ENABLE_CONNECT_PROTOCOL"] = false;
                                        throw exception;
                                    }
                                }

                                response = await connection.SendAsync(request, async, cancellationToken).ConfigureAwait(false);
                            }
                        }

                        if (response is null)
                        {
                            // We could not use HTTP/2. Do not continue if downgrade is not allowed.
                            if (request.Version.Major >= 2 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                            {
                                ThrowGetVersionException(request, 2);
                            }

                            // Use HTTP/1.x.
                            if (!TryGetPooledHttp11Connection(request, async, out HttpConnection? connection, out http11ConnectionWaiter))
                            {
                                connection = await http11ConnectionWaiter.WaitForConnectionAsync(request, this, async, cancellationToken).ConfigureAwait(false);
                            }

                            connection.Acquire(); // In case we are doing Windows (i.e. connection-based) auth, we need to ensure that we hold on to this specific connection while auth is underway.
                            try
                            {
                                response = await SendWithNtConnectionAuthAsync(connection, request, async, doRequestAuth, cancellationToken).ConfigureAwait(false);
                            }
                            finally
                            {
                                connection.Release();
                            }
                        }
                    }

                    ProcessAltSvc(response);
                    return response;
                }
                catch (HttpRequestException e) when (e.AllowRetry == RequestRetryType.RetryOnConnectionFailure)
                {
                    Debug.Assert(retryCount >= 0 && retryCount <= MaxConnectionFailureRetries);

                    if (retryCount == MaxConnectionFailureRetries)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            Trace($"MaxConnectionFailureRetries limit of {MaxConnectionFailureRetries} hit. Retryable request will not be retried. Exception: {e}");
                        }

                        throw;
                    }

                    retryCount++;

                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Retry attempt {retryCount} after connection failure. Connection exception: {e}");
                    }

                    // Eat exception and try again.
                }
                catch (HttpRequestException e) when (e.AllowRetry == RequestRetryType.RetryOnLowerHttpVersion)
                {
                    // Throw if fallback is not allowed by the version policy.
                    if (request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                    {
                        throw new HttpRequestException(HttpRequestError.VersionNegotiationError, SR.Format(SR.net_http_requested_version_server_refused, request.Version, request.VersionPolicy), e);
                    }

                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Retrying request because server requested version fallback: {e}");
                    }

                    // Eat exception and try again on a lower protocol version.
                    request.Version = HttpVersion.Version11;
                }
                catch (HttpRequestException e) when (e.AllowRetry == RequestRetryType.RetryOnStreamLimitReached)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Retrying request on another HTTP/2 connection after active streams limit is reached on existing one: {e}");
                    }

                    // Eat exception and try again.
                }
                finally
                {
                    // We never cancel both attempts at the same time. When downgrade happens, it's possible that both waiters are non-null,
                    // but in that case http2ConnectionWaiter.ConnectionCancellationTokenSource shall be null.
                    Debug.Assert(http11ConnectionWaiter is null || http2ConnectionWaiter?.ConnectionCancellationTokenSource is null);
                    CancelIfNecessary(http11ConnectionWaiter, cancellationToken.IsCancellationRequested);
                    CancelIfNecessary(http2ConnectionWaiter, cancellationToken.IsCancellationRequested);
                }
            }
        }

        private void CancelIfNecessary<T>(HttpConnectionWaiter<T>? waiter, bool requestCancelled)
            where T : HttpConnectionBase?
        {
            int timeout = GlobalHttpSettings.SocketsHttpHandler.PendingConnectionTimeoutOnRequestCompletion;
            if (waiter?.ConnectionCancellationTokenSource is null ||
                timeout == Timeout.Infinite ||
                Settings._connectTimeout != Timeout.InfiniteTimeSpan && timeout > (int)Settings._connectTimeout.TotalMilliseconds) // Do not override shorter ConnectTimeout
            {
                return;
            }

            lock (waiter)
            {
                if (waiter.ConnectionCancellationTokenSource is null)
                {
                    return;
                }

                if (NetEventSource.Log.IsEnabled())
                {
                    Trace($"Initiating cancellation of a pending connection attempt with delay of {timeout} ms, " +
                        $"Reason: {(requestCancelled ? "Request cancelled" : "Request served by another connection")}.");
                }

                waiter.CancelledByOriginatingRequestCompletion = true;
                if (timeout > 0)
                {
                    // Cancel after the specified timeout. This cancellation will not fire if the connection
                    // succeeds within the delay and the CTS becomes disposed.
                    waiter.ConnectionCancellationTokenSource.CancelAfter(timeout);
                }
                else
                {
                    // Cancel immediately if no timeout specified.
                    waiter.ConnectionCancellationTokenSource.Cancel();
                }
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
                                    @this.ExpireAltSvcAuthority();
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

        public Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpConnection connection, HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if (doRequestAuth && Settings._credentials != null)
            {
                return AuthenticationHelper.SendWithNtConnectionAuthAsync(request, async, Settings._credentials, connection, this, cancellationToken);
            }

            return SendWithNtProxyAuthAsync(connection, request, async, cancellationToken);
        }

        private bool DoProxyAuth => (_kind == HttpConnectionKind.Proxy || _kind == HttpConnectionKind.ProxyConnect);

        public Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpConnection connection, HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (DoProxyAuth && ProxyCredentials is not null)
            {
                return AuthenticationHelper.SendWithNtProxyAuthAsync(request, ProxyUri!, async, ProxyCredentials, connection, this, cancellationToken);
            }

            return connection.SendAsync(request, async, cancellationToken);
        }

        public ValueTask<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if (DoProxyAuth && ProxyCredentials is not null)
            {
                return AuthenticationHelper.SendWithProxyAuthAsync(request, _proxyUri!, async, ProxyCredentials, doRequestAuth, this, cancellationToken);
            }

            return SendWithVersionDetectionAndRetryAsync(request, async, doRequestAuth, cancellationToken);
        }

        public ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            // We need the User-Agent header when we send a CONNECT request to the proxy.
            // We must read the header early, before we return the ownership of the request back to the user.
            if ((Kind is HttpConnectionKind.ProxyTunnel or HttpConnectionKind.SslProxyTunnel) &&
                request.HasHeaders &&
                request.Headers.NonValidated.TryGetValues(HttpKnownHeaderNames.UserAgent, out HeaderStringValues userAgent))
            {
                _connectTunnelUserAgent = userAgent.ToString();
            }

            if (doRequestAuth && Settings._credentials != null)
            {
                return AuthenticationHelper.SendWithRequestAuthAsync(request, async, Settings._credentials, Settings._preAuthenticate, this, cancellationToken);
            }

            return SendWithProxyAuthAsync(request, async, doRequestAuth, cancellationToken);
        }

        private CancellationTokenSource GetConnectTimeoutCancellationTokenSource() => new CancellationTokenSource(Settings._connectTimeout);

        private async ValueTask<(Stream, TransportContext?, IPEndPoint?)> ConnectAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Stream? stream = null;
            IPEndPoint? remoteEndPoint = null;
            switch (_kind)
            {
                case HttpConnectionKind.Http:
                case HttpConnectionKind.Https:
                case HttpConnectionKind.ProxyConnect:
                    stream = await ConnectToTcpHostAsync(_originAuthority.IdnHost, _originAuthority.Port, request, async, cancellationToken).ConfigureAwait(false);
                    // remoteEndPoint is returned for diagnostic purposes.
                    remoteEndPoint = GetRemoteEndPoint(stream);
                    if (_kind == HttpConnectionKind.ProxyConnect && _sslOptionsProxy != null)
                    {
                        stream = await ConnectHelper.EstablishSslConnectionAsync(_sslOptionsProxy, request, async, stream, cancellationToken).ConfigureAwait(false);
                    }
                    break;

                case HttpConnectionKind.Proxy:
                    stream = await ConnectToTcpHostAsync(_proxyUri!.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(false);
                    // remoteEndPoint is returned for diagnostic purposes.
                    remoteEndPoint = GetRemoteEndPoint(stream);
                    if (_sslOptionsProxy != null)
                    {
                        stream = await ConnectHelper.EstablishSslConnectionAsync(_sslOptionsProxy, request, async, stream, cancellationToken).ConfigureAwait(false);
                    }
                    break;

                case HttpConnectionKind.ProxyTunnel:
                case HttpConnectionKind.SslProxyTunnel:
                    stream = await EstablishProxyTunnelAsync(async, cancellationToken).ConfigureAwait(false);

                    if (stream is HttpContentStream contentStream && contentStream._connection?._stream is Stream innerStream)
                    {
                        remoteEndPoint = GetRemoteEndPoint(innerStream);
                    }

                    break;

                case HttpConnectionKind.SocksTunnel:
                case HttpConnectionKind.SslSocksTunnel:
                    stream = await EstablishSocksTunnel(request, async, cancellationToken).ConfigureAwait(false);
                    // remoteEndPoint is returned for diagnostic purposes.
                    remoteEndPoint = GetRemoteEndPoint(stream);
                    break;
            }

            Debug.Assert(stream != null);

            TransportContext? transportContext = null;
            if (IsSecure)
            {
                SslStream? sslStream = stream as SslStream;
                if (sslStream == null)
                {
                    sslStream = await ConnectHelper.EstablishSslConnectionAsync(GetSslOptionsForRequest(request), request, async, stream, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        Trace($"Connected with custom SslStream: alpn='${sslStream.NegotiatedApplicationProtocol}'");
                    }
                }
                transportContext = sslStream.TransportContext;
                stream = sslStream;
            }

            static IPEndPoint? GetRemoteEndPoint(Stream stream) => (stream as NetworkStream)?.Socket?.RemoteEndPoint as IPEndPoint;

            return (stream, transportContext, remoteEndPoint);
        }

        private async ValueTask<Stream> ConnectToTcpHostAsync(string host, int port, HttpRequestMessage initialRequest, bool async, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var endPoint = new DnsEndPoint(host, port);
            Stream? stream = null;
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

                    stream = await streamTask.ConfigureAwait(false) ?? throw new HttpRequestException(SR.net_http_null_from_connect_callback);
                }
                else
                {
                    // Otherwise, create and connect a socket using default settings.
                    Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    try
                    {
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

                        stream = new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }

                return stream;
            }
            catch (Exception ex)
            {
                throw ex is OperationCanceledException oce && oce.CancellationToken == cancellationToken ?
                    CancellationHelper.CreateOperationCanceledException(innerException: null, cancellationToken) :
                    ConnectHelper.CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
            }
        }

        internal async ValueTask<HttpConnection> CreateHttp11ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            (Stream stream, TransportContext? transportContext, IPEndPoint? remoteEndPoint) = await ConnectAsync(request, async, cancellationToken).ConfigureAwait(false);
            return await ConstructHttp11ConnectionAsync(async, stream, transportContext, request, remoteEndPoint, cancellationToken).ConfigureAwait(false);
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
            catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
            {
                stream.Dispose();
                throw;
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

        private async ValueTask<HttpConnection> ConstructHttp11ConnectionAsync(bool async, Stream stream, TransportContext? transportContext, HttpRequestMessage request, IPEndPoint? remoteEndPoint, CancellationToken cancellationToken)
        {
            Stream newStream = await ApplyPlaintextFilterAsync(async, stream, HttpVersion.Version11, request, cancellationToken).ConfigureAwait(false);
            return new HttpConnection(this, newStream, transportContext, remoteEndPoint);
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

        private async ValueTask<Stream> EstablishProxyTunnelAsync(bool async, CancellationToken cancellationToken)
        {
            // Send a CONNECT request to the proxy server to establish a tunnel.
            HttpRequestMessage tunnelRequest = new HttpRequestMessage(HttpMethod.Connect, _proxyUri);
            tunnelRequest.Headers.Host = $"{_originAuthority.IdnHost}:{_originAuthority.Port}";    // This specifies destination host/port to connect to

            if (_connectTunnelUserAgent is not null)
            {
                tunnelRequest.Headers.TryAddWithoutValidation(KnownHeaders.UserAgent.Descriptor, _connectTunnelUserAgent);
            }

            HttpResponseMessage tunnelResponse = await _poolManager.SendProxyConnectAsync(tunnelRequest, _proxyUri!, async, cancellationToken).ConfigureAwait(false);

            if (tunnelResponse.StatusCode != HttpStatusCode.OK)
            {
                tunnelResponse.Dispose();
                throw new HttpRequestException(HttpRequestError.ProxyTunnelError, SR.Format(SR.net_http_proxy_tunnel_returned_failure_status_code, _proxyUri, (int)tunnelResponse.StatusCode));
            }

            try
            {
                return tunnelResponse.Content.ReadAsStream(cancellationToken);
            }
            catch
            {
                tunnelResponse.Dispose();
                throw;
            }
        }

        private async ValueTask<Stream> EstablishSocksTunnel(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(_proxyUri != null);

            Stream stream = await ConnectToTcpHostAsync(_proxyUri.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(false);

            try
            {
                await SocksHelper.EstablishSocksTunnelAsync(stream, _originAuthority.IdnHost, _originAuthority.Port, _proxyUri, ProxyCredentials, async, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                Debug.Assert(!(e is HttpRequestException));
                throw new HttpRequestException(HttpRequestError.ProxyTunnelError, SR.net_http_proxy_tunnel_error, e);
            }

            return stream;
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

        /// <summary>
        /// Called when an HttpConnection from this pool is no longer usable.
        /// Note, this is always called from HttpConnection.Dispose, which is a bit different than how HTTP2 works.
        /// </summary>
        public void InvalidateHttp11Connection(HttpConnection connection, bool disposing = true)
        {
            lock (SyncObj)
            {
                Debug.Assert(_associatedHttp11ConnectionCount > 0);
                Debug.Assert(!disposing || !_availableHttp11Connections.Contains(connection));

                _associatedHttp11ConnectionCount--;

                CheckForHttp11ConnectionInjection();
            }
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

        private bool CheckExpirationOnReturn(HttpConnectionBase connection)
        {
            TimeSpan lifetime = _poolManager.Settings._pooledConnectionLifetime;
            if (lifetime != Timeout.InfiniteTimeSpan)
            {
                return lifetime == TimeSpan.Zero || connection.GetLifetimeTicks(Environment.TickCount64) > lifetime.TotalMilliseconds;
            }

            return false;
        }

        public void RecycleHttp11Connection(HttpConnection connection) => ReturnHttp11Connection(connection, false);

        private void ReturnHttp11Connection(HttpConnection connection, bool isNewConnection, HttpConnectionWaiter<HttpConnection>? initialRequestWaiter = null)
        {
            if (NetEventSource.Log.IsEnabled()) connection.Trace($"{nameof(isNewConnection)}={isNewConnection}");

            Debug.Assert(isNewConnection || initialRequestWaiter is null, "Shouldn't have a request unless the connection is new");

            if (!isNewConnection && CheckExpirationOnReturn(connection))
            {
                if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP/1.1 connection return to pool. Connection lifetime expired.");
                connection.Dispose();
                return;
            }

            // Loop in case we get a request that has already been canceled or handled by a different connection.
            while (true)
            {
                HttpConnectionWaiter<HttpConnection>? waiter = null;
                bool added = false;
                lock (SyncObj)
                {
                    Debug.Assert(!_availableHttp11Connections.Contains(connection), $"Connection already in available list");
                    Debug.Assert(_associatedHttp11ConnectionCount > _availableHttp11Connections.Count,
                        $"Expected _associatedHttp11ConnectionCount={_associatedHttp11ConnectionCount} > _availableHttp11Connections.Count={_availableHttp11Connections.Count}");
                    Debug.Assert(_associatedHttp11ConnectionCount <= _maxHttp11Connections,
                        $"Expected _associatedHttp11ConnectionCount={_associatedHttp11ConnectionCount} <= _maxHttp11Connections={_maxHttp11Connections}");

                    if (isNewConnection)
                    {
                        Debug.Assert(_pendingHttp11ConnectionCount > 0);
                        _pendingHttp11ConnectionCount--;
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
                        _http11RequestQueue.TryDequeueSpecificWaiter(waiter);
                    }
                    else if (_http11RequestQueue.TryDequeueWaiter(this, out waiter))
                    {
                        Debug.Assert(_availableHttp11Connections.Count == 0, $"With {_availableHttp11Connections.Count} available HTTP/1.1 connections, we shouldn't have a waiter.");
                    }
                    else if (!_disposed)
                    {
                        // Add connection to the pool.
                        added = true;
                        connection.MarkConnectionAsIdle();
                        _availableHttp11Connections.Add(connection);
                    }

                    // If the pool has been disposed of, we will dispose the connection below outside the lock.
                    // We do this after processing the queue above so that any queued requests will be handled by existing connections if possible.
                }

                if (waiter is not null)
                {
                    Debug.Assert(!added);
                    if (waiter.TrySetResult(connection))
                    {
                        if (NetEventSource.Log.IsEnabled()) connection.Trace("Dequeued waiting HTTP/1.1 request.");
                        return;
                    }
                    else
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            Trace(waiter.Task.IsCanceled
                                ? "Discarding canceled HTTP/1.1 request from queue."
                                : "Discarding signaled HTTP/1.1 request waiter from queue.");
                        }
                        // Loop and process the queue again
                    }
                }
                else if (added)
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Put HTTP/1.1 connection in pool.");
                    return;
                }
                else
                {
                    Debug.Assert(_disposed);
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP/1.1 connection returned to pool. Pool was disposed.");
                    connection.Dispose();
                    return;
                }
            }
        }

        private void ReturnHttp2Connection(Http2Connection connection, bool isNewConnection, HttpConnectionWaiter<Http2Connection?>? initialRequestWaiter = null)
        {
            if (NetEventSource.Log.IsEnabled()) connection.Trace($"{nameof(isNewConnection)}={isNewConnection}");

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
                        if (waiter.TrySetResult(connection))
                        {
                            if (NetEventSource.Log.IsEnabled()) connection.Trace("Dequeued waiting HTTP2 request.");
                            break;
                        }
                        else
                        {
                            if (NetEventSource.Log.IsEnabled())
                            {
                                Trace(waiter.Task.IsCanceled
                                    ? "Discarding canceled HTTP/2 request from queue."
                                    : "Discarding signaled HTTP/2 request waiter from queue.");
                            }
                            // Loop and process the queue again
                        }
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
            List<HttpConnectionBase>? toDispose = null;

            lock (SyncObj)
            {
                if (!_disposed)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace("Disposing pool.");

                    _disposed = true;

                    toDispose = new List<HttpConnectionBase>(_availableHttp11Connections.Count + (_availableHttp2Connections?.Count ?? 0));
                    toDispose.AddRange(_availableHttp11Connections);
                    if (_availableHttp2Connections is not null)
                    {
                        toDispose.AddRange(_availableHttp2Connections);
                    }

                    // Note: Http11 connections will decrement the _associatedHttp11ConnectionCount when disposed.
                    // Http2 connections will not, hence the difference in handing _associatedHttp2ConnectionCount.

                    Debug.Assert(_associatedHttp11ConnectionCount >= _availableHttp11Connections.Count,
                        $"Expected {nameof(_associatedHttp11ConnectionCount)}={_associatedHttp11ConnectionCount} >= {nameof(_availableHttp11Connections)}.Count={_availableHttp11Connections.Count}");
                    _availableHttp11Connections.Clear();

                    Debug.Assert(_associatedHttp2ConnectionCount >= (_availableHttp2Connections?.Count ?? 0));
                    _associatedHttp2ConnectionCount -= (_availableHttp2Connections?.Count ?? 0);
                    _availableHttp2Connections?.Clear();

                    if (_http3Connection is not null)
                    {
                        toDispose.Add(_http3Connection);
                        _http3Connection = null;
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

                Debug.Assert(_availableHttp11Connections.Count == 0, $"Expected {nameof(_availableHttp11Connections)}.{nameof(_availableHttp11Connections.Count)} == 0");
                Debug.Assert((_availableHttp2Connections?.Count ?? 0) == 0, $"Expected {nameof(_availableHttp2Connections)}.{nameof(_availableHttp2Connections.Count)} == 0");
            }

            // Dispose outside the lock to avoid lock re-entrancy issues.
            toDispose?.ForEach(c => c.Dispose());
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
            long nowTicks = Environment.TickCount64;

            List<HttpConnectionBase>? toDispose = null;

            lock (SyncObj)
            {
                // If there are now no connections associated with this pool, we can dispose of it. We
                // avoid aggressively cleaning up pools that have recently been used but currently aren't;
                // if a pool was used since the last time we cleaned up, give it another chance. New pools
                // start out saying they've recently been used, to give them a bit of breathing room and time
                // for the initial collection to be added to it.
                if (!_usedSinceLastCleanup && _associatedHttp11ConnectionCount == 0 && _associatedHttp2ConnectionCount == 0)
                {
                    _disposed = true;
                    return true; // Pool is disposed of.  It should be removed.
                }

                // Reset the cleanup flag.  Any pools that are empty and not used since the last cleanup
                // will be purged next time around.
                _usedSinceLastCleanup = false;

                ScavengeConnectionList(_availableHttp11Connections, ref toDispose, nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout);
                if (_availableHttp2Connections is not null)
                {
                    int removed = ScavengeConnectionList(_availableHttp2Connections, ref toDispose, nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout);
                    _associatedHttp2ConnectionCount -= removed;

                    // Note: Http11 connections will decrement the _associatedHttp11ConnectionCount when disposed.
                    // Http2 connections will not, hence the difference in handing _associatedHttp2ConnectionCount.
                }
            }

            // Dispose the stale connections outside the pool lock, to avoid holding the lock too long.
            // Dispose them asynchronously to not to block the caller on closing the SslStream or NetworkStream.
            if (toDispose is not null)
            {
                Task.Factory.StartNew(static s => ((List<HttpConnectionBase>)s!).ForEach(c => c.Dispose()), toDispose,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            // Pool is active.  Should not be removed.
            return false;

            static int ScavengeConnectionList<T>(List<T> list, ref List<HttpConnectionBase>? toDispose, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout)
                where T : HttpConnectionBase
            {
                int freeIndex = 0;
                while (freeIndex < list.Count && IsUsableConnection(list[freeIndex], nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
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
                        while (current < list.Count && !IsUsableConnection(list[current], nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout))
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

            static bool IsUsableConnection(HttpConnectionBase connection, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout)
            {
                // Validate that the connection hasn't been idle in the pool for longer than is allowed.
                if (pooledConnectionIdleTimeout != Timeout.InfiniteTimeSpan)
                {
                    long idleTicks = connection.GetIdleTicks(nowTicks);
                    if (idleTicks > pooledConnectionIdleTimeout.TotalMilliseconds)
                    {
                        if (NetEventSource.Log.IsEnabled()) connection.Trace($"Scavenging connection. Idle {TimeSpan.FromMilliseconds(idleTicks)} > {pooledConnectionIdleTimeout}.");
                        return false;
                    }
                }

                // Validate that the connection lifetime has not been exceeded.
                if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
                {
                    long lifetimeTicks = connection.GetLifetimeTicks(nowTicks);
                    if (lifetimeTicks > pooledConnectionLifetime.TotalMilliseconds)
                    {
                        if (NetEventSource.Log.IsEnabled()) connection.Trace($"Scavenging connection. Lifetime {TimeSpan.FromMilliseconds(lifetimeTicks)} > {pooledConnectionLifetime}.");
                        return false;
                    }
                }

                if (!connection.CheckUsabilityOnScavenge())
                {
                    if (NetEventSource.Log.IsEnabled()) connection.Trace($"Scavenging connection. Keep-Alive timeout exceeded, unexpected data or EOF received.");
                    return false;
                }

                return true;
            }
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
            Http2Connection[]? localHttp2Connections;
            lock (SyncObj)
            {
                localHttp2Connections = _availableHttp2Connections?.ToArray();
            }

            if (localHttp2Connections is not null)
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
                    $"https://{_originAuthority}" + (_sslOptionsHttp11.TargetHost != _originAuthority.IdnHost ? $", SSL TargetHost={_sslOptionsHttp11.TargetHost}" : null)) :
                (_sslOptionsHttp11 == null ?
                    $"Proxy {_proxyUri}" :
                    $"https://{_originAuthority}/ tunnelled via Proxy {_proxyUri}" + (_sslOptionsHttp11.TargetHost != _originAuthority.IdnHost ? $", SSL TargetHost={_sslOptionsHttp11.TargetHost}" : null)));

        private void Trace(string? message, [CallerMemberName] string? memberName = null) =>
            NetEventSource.Log.HandlerMessage(
                GetHashCode(),               // pool ID
                0,                           // connection ID
                0,                           // request ID
                memberName,                  // method name
                message);                    // message

        private struct RequestQueue<T>
            where T : HttpConnectionBase?
        {
            public struct QueueItem
            {
                public HttpRequestMessage Request;
                public HttpConnectionWaiter<T> Waiter;
            }

            // This implementation mimics that of Queue<T>, but without version checks and with an extra head pointer
            // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Queue.cs
            private QueueItem[] _array;
            private int _head; // The index from which to dequeue if the queue isn't empty.
            private int _tail; // The index at which to enqueue if the queue isn't full.
            private int _size; // Number of elements.
            private int _attemptedConnectionsOffset; // The offset from head where we should next peek for a request without a connection attempt

            public RequestQueue()
            {
                _array = Array.Empty<QueueItem>();
                _head = 0;
                _tail = 0;
                _size = 0;
                _attemptedConnectionsOffset = 0;
            }

            private void Enqueue(QueueItem queueItem)
            {
                if (_size == _array.Length)
                {
                    Grow();
                }

                _array[_tail] = queueItem;
                MoveNext(ref _tail);

                _size++;
            }

            private QueueItem Dequeue()
            {
                Debug.Assert(_size > 0);

                int head = _head;
                QueueItem[] array = _array;

                QueueItem queueItem = array[head];
                array[head] = default;

                MoveNext(ref _head);

                if (_attemptedConnectionsOffset > 0)
                {
                    _attemptedConnectionsOffset--;
                }

                _size--;
                return queueItem;
            }

            private bool TryPeek(out QueueItem queueItem)
            {
                if (_size == 0)
                {
                    queueItem = default!;
                    return false;
                }

                queueItem = _array[_head];
                return true;
            }

            private void MoveNext(ref int index)
            {
                int tmp = index + 1;
                if (tmp == _array.Length)
                {
                    tmp = 0;
                }
                index = tmp;
            }

            private void Grow()
            {
                var newArray = new QueueItem[Math.Max(4, _array.Length * 2)];

                if (_size != 0)
                {
                    if (_head < _tail)
                    {
                        Array.Copy(_array, _head, newArray, 0, _size);
                    }
                    else
                    {
                        Array.Copy(_array, _head, newArray, 0, _array.Length - _head);
                        Array.Copy(_array, 0, newArray, _array.Length - _head, _tail);
                    }
                }

                _array = newArray;
                _head = 0;
                _tail = _size;
            }


            public HttpConnectionWaiter<T> EnqueueRequest(HttpRequestMessage request)
            {
                var waiter = new HttpConnectionWaiter<T>();
                Enqueue(new QueueItem { Request = request, Waiter = waiter });
                return waiter;
            }

            public void PruneCompletedRequestsFromHeadOfQueue(HttpConnectionPool pool)
            {
                while (TryPeek(out QueueItem queueItem) && queueItem.Waiter.Task.IsCompleted)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        pool.Trace(queueItem.Waiter.Task.IsCanceled
                            ? "Discarding canceled request from queue."
                            : "Discarding signaled request waiter from queue.");
                    }

                    Dequeue();
                }
            }

            public bool TryDequeueWaiter(HttpConnectionPool pool, [MaybeNullWhen(false)] out HttpConnectionWaiter<T> waiter)
            {
                PruneCompletedRequestsFromHeadOfQueue(pool);

                if (Count != 0)
                {
                    waiter = Dequeue().Waiter;
                    return true;
                }

                waiter = null;
                return false;
            }

            public void TryDequeueSpecificWaiter(HttpConnectionWaiter<T> waiter)
            {
                if (TryPeek(out QueueItem queueItem) && queueItem.Waiter == waiter)
                {
                    Dequeue();
                }
            }

            public QueueItem PeekNextRequestForConnectionAttempt()
            {
                Debug.Assert(_attemptedConnectionsOffset >= 0);
                Debug.Assert(_attemptedConnectionsOffset < _size, $"{_attemptedConnectionsOffset} < {_size}");

                int index = _head + _attemptedConnectionsOffset;
                _attemptedConnectionsOffset++;

                if (index >= _array.Length)
                {
                    index -= _array.Length;
                }

                return _array[index];
            }

            public int Count => _size;

            public int RequestsWithoutAConnectionAttempt => _size - _attemptedConnectionsOffset;
        }

        private sealed class HttpConnectionWaiter<T> : TaskCompletionSourceWithCancellation<T>
            where T : HttpConnectionBase?
        {
            // When a connection attempt is pending, reference the connection's CTS, so we can tear it down if the initiating request is cancelled
            // or completes on a different connection.
            public CancellationTokenSource? ConnectionCancellationTokenSource;

            // Distinguish connection cancellation that happens because the initiating request is cancelled or completed on a different connection.
            public bool CancelledByOriginatingRequestCompletion { get; set; }

            public ValueTask<T> WaitForConnectionAsync(HttpRequestMessage request, HttpConnectionPool pool, bool async, CancellationToken requestCancellationToken)
            {
                return HttpTelemetry.Log.IsEnabled() || pool.Settings._metrics!.RequestsQueueDuration.Enabled
                    ? WaitForConnectionWithTelemetryAsync(request, pool, async, requestCancellationToken)
                    : WaitWithCancellationAsync(async, requestCancellationToken);
            }

            private async ValueTask<T> WaitForConnectionWithTelemetryAsync(HttpRequestMessage request, HttpConnectionPool pool, bool async, CancellationToken requestCancellationToken)
            {
                Debug.Assert(typeof(T) == typeof(HttpConnection) || typeof(T) == typeof(Http2Connection));

                long startingTimestamp = Stopwatch.GetTimestamp();
                try
                {
                    return await WaitWithCancellationAsync(async, requestCancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    TimeSpan duration = Stopwatch.GetElapsedTime(startingTimestamp);
                    int versionMajor = typeof(T) == typeof(HttpConnection) ? 1 : 2;

                    pool.Settings._metrics!.RequestLeftQueue(request, pool, duration, versionMajor);

                    if (HttpTelemetry.Log.IsEnabled())
                    {
                        HttpTelemetry.Log.RequestLeftQueue(versionMajor, duration);
                    }
                }
            }
        }
    }
}
