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
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
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

            if (host != null)
            {
                _originAuthority = new HttpAuthority(host, port);
            }

            _http2Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version20;

            if (IsHttp3Supported())
            {
                _http3Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version30 && (_poolManager.Settings._quicImplementationProvider ?? QuicImplementationProviders.Default).IsSupported;
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

                if (IsHttp3Supported())
                {
                    if (_http3Enabled)
                    {
                        _sslOptionsHttp3 = ConstructSslOptions(poolManager, sslHostName);
                        _sslOptionsHttp3.ApplicationProtocols = s_http3ApplicationProtocols;
                    }
                }
            }

            // Set up for PreAuthenticate.  Access to this cache is guarded by a lock on the cache itself.
            if (_poolManager.Settings._preAuthenticate)
            {
                PreAuthCredentials = new CredentialCache();
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
        public bool IsSecure => _kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel || _kind == HttpConnectionKind.SslSocksTunnel;
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
                    sb.Append(IsSecure ? "https://" : "http://")
                      .Append(_originAuthority.IdnHost);

                    if (_originAuthority.Port != (IsSecure ? DefaultHttpsPort : DefaultHttpPort))
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

        private static HttpRequestException GetVersionException(HttpRequestMessage request, int desiredVersion)
        {
            Debug.Assert(desiredVersion == 2 || desiredVersion == 3);

            return new HttpRequestException(SR.Format(SR.net_http_requested_version_cannot_establish, request.Version, request.VersionPolicy, desiredVersion));
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

        private async Task AddHttp11ConnectionAsync(HttpRequestMessage request)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("Creating new HTTP/1.1 connection for pool.");

            HttpConnection connection;
            using (CancellationTokenSource cts = GetConnectTimeoutCancellationTokenSource())
            {
                try
                {
                    connection = await CreateHttp11ConnectionAsync(request, false, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token)
                {
                    HandleHttp11ConnectionFailure(CreateConnectTimeoutException(oce));
                    return;
                }
                catch (Exception e)
                {
                    HandleHttp11ConnectionFailure(e);
                    return;
                }
            }

            // Add the established connection to the pool.
            ReturnHttp11Connection(connection, isNewConnection: true);
        }

        private void CheckForHttp11ConnectionInjection()
        {
            Debug.Assert(HasSyncObjLock);

            if (!_http11RequestQueue.TryPeekNextRequest(out HttpRequestMessage? request))
            {
                return;
            }

            // Determine if we can and should add a new connection to the pool.
            if (_availableHttp11Connections.Count == 0 &&                           // No available connections
                _http11RequestQueue.Count > _pendingHttp11ConnectionCount &&        // More requests queued than pending connections
                _associatedHttp11ConnectionCount < _maxHttp11Connections)           // Under the connection limit
            {
                _associatedHttp11ConnectionCount++;
                _pendingHttp11ConnectionCount++;

                Task.Run(() => AddHttp11ConnectionAsync(request));
            }
        }

        private async ValueTask<HttpConnection> GetHttp11ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            // Look for a usable idle connection.
            TaskCompletionSourceWithCancellation<HttpConnection> waiter;
            while (true)
            {
                HttpConnection? connection = null;
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

                        // Break out of the loop and continue processing below.
                        break;
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
                return connection;
            }

            // There were no available idle connections. This request has been added to the request queue.
            if (NetEventSource.Log.IsEnabled()) Trace($"No available HTTP/1.1 connections; request queued.");

            ValueStopwatch stopwatch = ValueStopwatch.StartNew();
            try
            {
                return await waiter.WaitWithCancellationAsync(async, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (HttpTelemetry.Log.IsEnabled())
                {
                    HttpTelemetry.Log.Http11RequestLeftQueue(stopwatch.GetElapsedTime().TotalMilliseconds);
                }
            }
        }

        private async Task HandleHttp11Downgrade(HttpRequestMessage request, Socket? socket, Stream stream, TransportContext? transportContext, CancellationToken cancellationToken)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("Server does not support HTTP2; disabling HTTP2 use and proceeding with HTTP/1.1 connection");

            bool canUse = true;
            lock (SyncObj)
            {
                Debug.Assert(_pendingHttp2Connection);
                Debug.Assert(_associatedHttp2ConnectionCount > 0);

                // Server does not support HTTP2. Disable further HTTP2 attempts.
                _http2Enabled = false;
                _associatedHttp2ConnectionCount--;
                _pendingHttp2Connection = false;

                // Signal to any queued HTTP2 requests that they must downgrade.
                while (_http2RequestQueue.TryDequeueNextRequest(null))
                    ;

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
                http11Connection = await ConstructHttp11ConnectionAsync(false, socket, stream, transportContext, request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
            {
                HandleHttp11ConnectionFailure(CreateConnectTimeoutException(oce));
                return;
            }
            catch (Exception e)
            {
                HandleHttp11ConnectionFailure(e);
                return;
            }

            ReturnHttp11Connection(http11Connection, isNewConnection: true);
        }

        private async Task AddHttp2ConnectionAsync(HttpRequestMessage request)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("Creating new HTTP/2 connection for pool.");

            Http2Connection connection;
            using (CancellationTokenSource cts = GetConnectTimeoutCancellationTokenSource())
            {
                try
                {
                    (Socket? socket, Stream stream, TransportContext? transportContext) = await ConnectAsync(request, false, cts.Token).ConfigureAwait(false);

                    if (IsSecure)
                    {
                        SslStream sslStream = (SslStream)stream;

                        if (sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
                        {
                            // The server accepted our request for HTTP2.

                            if (sslStream.SslProtocol < SslProtocols.Tls12)
                            {
                                stream.Dispose();
                                throw new HttpRequestException(SR.Format(SR.net_ssl_http2_requires_tls12, sslStream.SslProtocol));
                            }

                            connection = await ConstructHttp2ConnectionAsync(stream, request, cts.Token).ConfigureAwait(false);
                        }
                        else
                        {
                            // We established an SSL connection, but the server denied our request for HTTP2.
                            await HandleHttp11Downgrade(request, socket, stream, transportContext, cts.Token).ConfigureAwait(false);
                            return;
                        }
                    }
                    else
                    {
                        connection = await ConstructHttp2ConnectionAsync(stream, request, cts.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token)
                {
                    HandleHttp2ConnectionFailure(CreateConnectTimeoutException(oce));
                    return;
                }
                catch (Exception e)
                {
                    HandleHttp2ConnectionFailure(e);
                    return;
                }
            }

            // Register for shutdown notification.
            // Do this before we return the connection to the pool, because that may result in it being disposed.
            ValueTask shutdownTask = connection.WaitForShutdownAsync();

            // Add the new connection to the pool.
            ReturnHttp2Connection(connection, isNewConnection: true);

            // Wait for connection shutdown.
            await shutdownTask.ConfigureAwait(false);

            InvalidateHttp2Connection(connection);
        }

        private void CheckForHttp2ConnectionInjection()
        {
            Debug.Assert(HasSyncObjLock);

            if (!_http2RequestQueue.TryPeekNextRequest(out HttpRequestMessage? request))
            {
                return;
            }

            // Determine if we can and should add a new connection to the pool.
            if ((_availableHttp2Connections?.Count ?? 0) == 0 &&                            // No available connections
                !_pendingHttp2Connection &&                                                 // Only allow one pending HTTP2 connection at a time
                (_associatedHttp2ConnectionCount == 0 || EnableMultipleHttp2Connections))   // We allow multiple connections, or don't have a connection currently
            {
                _associatedHttp2ConnectionCount++;
                _pendingHttp2Connection = true;

                Task.Run(() => AddHttp2ConnectionAsync(request));
            }
        }

        private async ValueTask<Http2Connection?> GetHttp2ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https || _kind == HttpConnectionKind.SslProxyTunnel || _kind == HttpConnectionKind.Http || _kind == HttpConnectionKind.SocksTunnel || _kind == HttpConnectionKind.SslSocksTunnel);

            // Look for a usable connection.
            TaskCompletionSourceWithCancellation<Http2Connection?> waiter;
            while (true)
            {
                Http2Connection connection;
                lock (SyncObj)
                {
                    _usedSinceLastCleanup = true;

                    if (!_http2Enabled)
                    {
                        return null;
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

                        // Break out of the loop and continue processing below.
                        break;
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
                return connection;
            }

            // There were no available connections. This request has been added to the request queue.
            if (NetEventSource.Log.IsEnabled()) Trace($"No available HTTP/2 connections; request queued.");

            ValueStopwatch stopwatch = ValueStopwatch.StartNew();
            try
            {
                return await waiter.WaitWithCancellationAsync(async, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (HttpTelemetry.Log.IsEnabled())
                {
                    HttpTelemetry.Log.Http20RequestLeftQueue(stopwatch.GetElapsedTime().TotalMilliseconds);
                }
            }
        }

        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private async ValueTask<Http3Connection> GetHttp3ConnectionAsync(HttpRequestMessage request, HttpAuthority authority, CancellationToken cancellationToken)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https);
            Debug.Assert(_http3Enabled == true);

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

                    return _http3Connection;
                }

                if (NetEventSource.Log.IsEnabled())
                {
                    Trace("Attempting new HTTP3 connection.");
                }

                QuicConnection quicConnection;
                try
                {
                    quicConnection = await ConnectHelper.ConnectQuicAsync(request, Settings._quicImplementationProvider ?? QuicImplementationProviders.Default, new DnsEndPoint(authority.IdnHost, authority.Port), _sslOptionsHttp3!, cancellationToken).ConfigureAwait(false);
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
        private async ValueTask<HttpResponseMessage?> TrySendUsingHttp3Async(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            if (_http3Enabled && (request.Version.Major >= 3 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)))
            {
                // Loop in case we get a 421 and need to send the request to a different authority.
                while (true)
                {
                    HttpAuthority? authority = _http3Authority;

                    // If H3 is explicitly requested, assume prenegotiated H3.
                    if (request.Version.Major >= 3 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
                    {
                        authority = authority ?? _originAuthority;
                    }

                    if (authority == null)
                    {
                        break;
                    }

                    if (IsAltSvcBlocked(authority))
                    {
                        throw GetVersionException(request, 3);
                    }

                    Http3Connection connection = await GetHttp3ConnectionAsync(request, authority, cancellationToken).ConfigureAwait(false);
                    HttpResponseMessage response = await connection.SendAsync(request, async, cancellationToken).ConfigureAwait(false);

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

            return null;
        }

        // Returns null if HTTP2 cannot be used.
        private async ValueTask<HttpResponseMessage?> TrySendUsingHttp2Async(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            // Send using HTTP/2 if we can.
            if (_http2Enabled && (request.Version.Major >= 2 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)) &&
               // If the connection is not secured and downgrade is possible, prefer HTTP/1.1.
               (request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower || IsSecure))
            {
                Http2Connection? connection = await GetHttp2ConnectionAsync(request, async, cancellationToken).ConfigureAwait(false);
                if (connection is null)
                {
                    Debug.Assert(!_http2Enabled);
                    return null;
                }

                return await connection.SendAsync(request, async, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private async ValueTask<HttpResponseMessage> SendUsingHttp11Async(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            HttpConnection connection = await GetHttp11ConnectionAsync(request, async, cancellationToken).ConfigureAwait(false);

            // In case we are doing Windows (i.e. connection-based) auth, we need to ensure that we hold on to this specific connection while auth is underway.
            connection.Acquire();
            try
            {
                return await SendWithNtConnectionAuthAsync(connection, request, async, doRequestAuth, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                connection.Release();
            }
        }

        private async ValueTask<HttpResponseMessage> DetermineVersionAndSendAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            HttpResponseMessage? response;

            if (IsHttp3Supported())
            {
                response = await TrySendUsingHttp3Async(request, async, doRequestAuth, cancellationToken).ConfigureAwait(false);
                if (response is not null)
                {
                    return response;
                }
            }

            // We cannot use HTTP/3. Do not continue if downgrade is not allowed.
            if (request.Version.Major >= 3 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
            {
                throw GetVersionException(request, 3);
            }

            response = await TrySendUsingHttp2Async(request, async, doRequestAuth, cancellationToken).ConfigureAwait(false);
            if (response is not null)
            {
                return response;
            }

            // We cannot use HTTP/2. Do not continue if downgrade is not allowed.
            if (request.Version.Major >= 2 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
            {
                throw GetVersionException(request, 2);
            }

            return await SendUsingHttp11Async(request, async, doRequestAuth, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<HttpResponseMessage> SendAndProcessAltSvcAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await DetermineVersionAndSendAsync(request, async, doRequestAuth, cancellationToken).ConfigureAwait(false);

            // Check for the Alt-Svc header, to upgrade to HTTP/3.
            if (_altSvcEnabled && response.Headers.TryGetValues(KnownHeaders.AltSvc.Descriptor, out IEnumerable<string>? altSvcHeaderValues))
            {
                HandleAltSvc(altSvcHeaderValues, response.Headers.Age);
            }

            return response;
        }

        public async ValueTask<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            while (true)
            {
                // Loop on connection failures (or other problems like version downgrade) and retry if possible.
                try
                {
                    return await SendAndProcessAltSvcAsync(request, async, doRequestAuth, cancellationToken).ConfigureAwait(false);
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
                        throw new HttpRequestException(SR.Format(SR.net_http_requested_version_server_refused, request.Version, request.VersionPolicy), e);
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
                        var authority = new HttpAuthority(value.Host ?? _originAuthority!.IdnHost, value.Port);

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

        private CancellationTokenSource GetConnectTimeoutCancellationTokenSource() => new CancellationTokenSource(Settings._connectTimeout);

        private async ValueTask<(Socket?, Stream, TransportContext?)> ConnectAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Stream? stream = null;
            Socket? socket = null;
            switch (_kind)
            {
                case HttpConnectionKind.Http:
                case HttpConnectionKind.Https:
                case HttpConnectionKind.ProxyConnect:
                    Debug.Assert(_originAuthority != null);
                    (socket, stream) = await ConnectToTcpHostAsync(_originAuthority.IdnHost, _originAuthority.Port, request, async, cancellationToken).ConfigureAwait(false);
                    break;

                case HttpConnectionKind.Proxy:
                    (socket, stream) = await ConnectToTcpHostAsync(_proxyUri!.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(false);
                    break;

                case HttpConnectionKind.ProxyTunnel:
                case HttpConnectionKind.SslProxyTunnel:
                    stream = await EstablishProxyTunnelAsync(async, request.HasHeaders ? request.Headers : null, cancellationToken).ConfigureAwait(false);
                    break;

                case HttpConnectionKind.SocksTunnel:
                case HttpConnectionKind.SslSocksTunnel:
                    (socket, stream) = await EstablishSocksTunnel(request, async, cancellationToken).ConfigureAwait(false);
                break;
            }

            Debug.Assert(stream != null);
            if (socket is null && stream is NetworkStream ns)
            {
                // We weren't handed a socket directly.  But if we're able to extract one, do so.
                // Most likely case here is a ConnectCallback was used and returned a NetworkStream.
                socket = ns.Socket;
            }

            TransportContext? transportContext = null;
            if (IsSecure)
            {
                SslStream sslStream = await ConnectHelper.EstablishSslConnectionAsync(GetSslOptionsForRequest(request), request, async, stream, cancellationToken).ConfigureAwait(false);
                transportContext = sslStream.TransportContext;
                stream = sslStream;
            }

            return (socket, stream, transportContext);
        }

        private async ValueTask<(Socket?, Stream)> ConnectToTcpHostAsync(string host, int port, HttpRequestMessage initialRequest, bool async, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var endPoint = new DnsEndPoint(host, port);
            Socket? socket = null;
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

                    stream = new NetworkStream(socket, ownsSocket: true);
                }

                return (socket, stream);
            }
            catch (Exception ex)
            {
                socket?.Dispose();
                throw ex is OperationCanceledException oce && oce.CancellationToken == cancellationToken ?
                    CancellationHelper.CreateOperationCanceledException(innerException: null, cancellationToken) :
                    ConnectHelper.CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
            }
        }

        internal async ValueTask<HttpConnection> CreateHttp11ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            (Socket? socket, Stream stream, TransportContext? transportContext) = await ConnectAsync(request, async, cancellationToken).ConfigureAwait(false);

            return await ConstructHttp11ConnectionAsync(async, socket, stream!, transportContext, request, cancellationToken).ConfigureAwait(false);
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

        private async ValueTask<HttpConnection> ConstructHttp11ConnectionAsync(bool async, Socket? socket, Stream stream, TransportContext? transportContext, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Stream newStream = await ApplyPlaintextFilterAsync(async, stream, HttpVersion.Version11, request, cancellationToken).ConfigureAwait(false);
            if (newStream != stream)
            {
                // If a plaintext filter created a new stream, we can't trust that the socket is still applicable.
                socket = null;
            }
            return new HttpConnection(this, socket, newStream, transportContext);
        }

        private async ValueTask<Http2Connection> ConstructHttp2ConnectionAsync(Stream stream, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            stream = await ApplyPlaintextFilterAsync(async: true, stream, HttpVersion.Version20, request, cancellationToken).ConfigureAwait(false);

            Http2Connection http2Connection = new Http2Connection(this, stream);
            try
            {
                await http2Connection.SetupAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Note, SetupAsync will dispose the connection if there is an exception.
                throw new HttpRequestException(SR.net_http_client_execution_error, e);
            }

            return http2Connection;
        }

        private async ValueTask<Stream> EstablishProxyTunnelAsync(bool async, HttpRequestHeaders? headers, CancellationToken cancellationToken)
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
                tunnelResponse.Dispose();
                throw new HttpRequestException(SR.Format(SR.net_http_proxy_tunnel_returned_failure_status_code, _proxyUri, (int)tunnelResponse.StatusCode));
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

        private async ValueTask<(Socket? socket, Stream stream)> EstablishSocksTunnel(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(_originAuthority != null);
            Debug.Assert(_proxyUri != null);

            (Socket? socket, Stream stream) = await ConnectToTcpHostAsync(_proxyUri.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(false);

            try
            {
                await SocksHelper.EstablishSocksTunnelAsync(stream, _originAuthority.IdnHost, _originAuthority.Port, _proxyUri, ProxyCredentials, async, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                Debug.Assert(!(e is HttpRequestException));
                throw new HttpRequestException(SR.net_http_request_aborted, e);
            }

            return (socket, stream);
        }

        private void HandleHttp11ConnectionFailure(Exception e)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("HTTP/1.1 connection failed");

            lock (SyncObj)
            {
                Debug.Assert(_associatedHttp11ConnectionCount > 0);
                Debug.Assert(_pendingHttp11ConnectionCount > 0);

                _associatedHttp11ConnectionCount--;
                _pendingHttp11ConnectionCount--;

                // Fail the next queued request (if any) with this error.
                _http11RequestQueue.TryFailNextRequest(e);

                CheckForHttp11ConnectionInjection();
            }
        }

        private void HandleHttp2ConnectionFailure(Exception e)
        {
            if (NetEventSource.Log.IsEnabled()) Trace("HTTP2 connection failed");

            lock (SyncObj)
            {
                Debug.Assert(_associatedHttp2ConnectionCount > 0);
                Debug.Assert(_pendingHttp2Connection);

                _associatedHttp2ConnectionCount--;
                _pendingHttp2Connection = false;

                // Fail the next queued request (if any) with this error.
                _http2RequestQueue.TryFailNextRequest(e);

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

        public void ReturnHttp11Connection(HttpConnection connection, bool isNewConnection = false)
        {
            if (NetEventSource.Log.IsEnabled()) connection.Trace($"{nameof(isNewConnection)}={isNewConnection}");

            if (!isNewConnection && CheckExpirationOnReturn(connection))
            {
                if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP/1.1 connection return to pool. Connection lifetime expired.");
                connection.Dispose();
                return;
            }

            lock (SyncObj)
            {
                Debug.Assert(!_availableHttp11Connections.Contains(connection));

                if (isNewConnection)
                {
                    Debug.Assert(_pendingHttp11ConnectionCount > 0);
                    _pendingHttp11ConnectionCount--;
                }

                if (_http11RequestQueue.TryDequeueNextRequest(connection))
                {
                    Debug.Assert(_availableHttp11Connections.Count == 0, $"With {_availableHttp11Connections.Count} available HTTP/1.1 connections, we shouldn't have a waiter.");

                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Dequeued waiting HTTP/1.1 request.");
                    return;
                }

                if (_disposed)
                {
                    // If the pool has been disposed of, dispose the connection being returned,
                    // as the pool is being deactivated. We do this after the above in order to
                    // use pooled connections to satisfy any requests that pended before the
                    // the pool was disposed of.
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing connection returned to pool. Pool was disposed.");
                }
                else
                {
                    // Add connection to the pool.
                    _availableHttp11Connections.Add(connection);
                    Debug.Assert(_availableHttp11Connections.Count <= _maxHttp11Connections, $"Expected {_availableHttp11Connections.Count} <= {_maxHttp11Connections}");
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Put connection in pool.");
                    return;
                }
            }

            // We determined that the connection is no longer usable.
            connection.Dispose();
        }

        public void ReturnHttp2Connection(Http2Connection connection, bool isNewConnection)
        {
            if (NetEventSource.Log.IsEnabled()) connection.Trace($"{nameof(isNewConnection)}={isNewConnection}");

            if (!isNewConnection && CheckExpirationOnReturn(connection))
            {
                lock (SyncObj)
                {
                    Debug.Assert(_availableHttp2Connections is null || !_availableHttp2Connections.Contains(connection));
                    Debug.Assert(_associatedHttp2ConnectionCount > (_availableHttp2Connections?.Count ?? 0));
                    _associatedHttp2ConnectionCount--;
                }

                if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP/2 connection return to pool. Connection lifetime expired.");
                connection.Dispose();
                return;
            }

            bool usable = true;
            bool poolDisposed = false;
            lock (SyncObj)
            {
                Debug.Assert(_availableHttp2Connections is null || !_availableHttp2Connections.Contains(connection));
                Debug.Assert(_associatedHttp2ConnectionCount > (_availableHttp2Connections?.Count ?? 0));

                if (isNewConnection)
                {
                    Debug.Assert(_pendingHttp2Connection);
                    _pendingHttp2Connection = false;
                }

                while (!_http2RequestQueue.IsEmpty)
                {
                    Debug.Assert((_availableHttp2Connections?.Count ?? 0) == 0, $"With {_availableHttp11Connections.Count} available HTTP2 connections, we shouldn't have a waiter.");

                    if (!connection.TryReserveStream())
                    {
                        usable = false;
                        if (isNewConnection)
                        {
                            // The new connection could not handle even one request, either because it shut down before we could use it for any requests,
                            // or because it immediately set the max concurrent streams limit to 0.
                            // We don't want to get stuck in a loop where we keep trying to create new connections for the same request.
                            // Fail the next request, if any.
                            HttpRequestException hre = new HttpRequestException(SR.net_http_http2_connection_not_established);
                            ExceptionDispatchInfo.SetCurrentStackTrace(hre);
                            _http2RequestQueue.TryFailNextRequest(hre);
                        }
                        break;
                    }

                    isNewConnection = false;

                    if (!_http2RequestQueue.TryDequeueNextRequest(connection))
                    {
                        connection.ReleaseStream();
                        break;
                    }

                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Dequeued waiting HTTP/2 request.");
                }

                // Since we only inject one connection at a time, we may want to inject another now.
                CheckForHttp2ConnectionInjection();

                if (_disposed)
                {
                    // If the pool has been disposed of, we want to dispose the connection being returned, as the pool is being deactivated.
                    // We do this after the above in order to satisfy any requests that were queued before the pool was disposed of.
                    Debug.Assert(_associatedHttp2ConnectionCount > (_availableHttp2Connections?.Count ?? 0));
                    _associatedHttp2ConnectionCount--;
                    poolDisposed = true;
                }
                else if (usable)
                {
                    if (_availableHttp2Connections is null)
                    {
                        _availableHttp2Connections = new List<Http2Connection>();
                    }

                    // Add connection to the pool.
                    _availableHttp2Connections.Add(connection);
                    if (NetEventSource.Log.IsEnabled()) connection.Trace("Put HTTP/2 connection in pool.");
                    return;
                }
            }

            if (poolDisposed)
            {
                if (NetEventSource.Log.IsEnabled()) connection.Trace("Disposing HTTP/2 connection returned to pool. Pool was disposed.");
                connection.Dispose();
                return;
            }

            Debug.Assert(!usable);

            // We need to wait until the connection is usable again.
            DisableHttp2Connection(connection);
        }

        /// <summary>
        /// Disable usage of the specified connection because it cannot handle any more streams at the moment.
        /// We will register to be notified when it can handle more streams (or becomes permanently unusable).
        /// </summary>
        private void DisableHttp2Connection(Http2Connection connection)
        {
            if (NetEventSource.Log.IsEnabled()) connection.Trace("");

            Task.Run(async () =>
            {
                bool usable = await connection.WaitForAvailableStreamsAsync().ConfigureAwait(false);

                if (NetEventSource.Log.IsEnabled()) connection.Trace($"WaitForAvailableStreamsAsync completed, {nameof(usable)}={usable}");

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
            });
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

                long nowTicks = Environment.TickCount64;

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
            toDispose?.ForEach(c => c.Dispose());

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
                    toDispose ??= new List<HttpConnectionBase> { list[freeIndex] };

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
                    if (NetEventSource.Log.IsEnabled()) connection.Trace($"Scavenging connection. Unexpected data or EOF received.");
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

        private struct RequestQueue<T>
        {
            private struct QueueItem
            {
                public HttpRequestMessage Request;
                public TaskCompletionSourceWithCancellation<T> Waiter;
            }

            private Queue<QueueItem>? _queue;

            public TaskCompletionSourceWithCancellation<T> EnqueueRequest(HttpRequestMessage request)
            {
                if (_queue is null)
                {
                    _queue = new Queue<QueueItem>();
                }

                TaskCompletionSourceWithCancellation<T> waiter = new TaskCompletionSourceWithCancellation<T>();
                _queue.Enqueue(new QueueItem { Request = request, Waiter = waiter });
                return waiter;
            }

            public bool TryFailNextRequest(Exception e)
            {
                Debug.Assert(e is HttpRequestException or OperationCanceledException, "Unexpected exception type for connection failure");

                if (_queue is not null)
                {
                    // Fail the next queued request (if any) with this error.
                    while (_queue.TryDequeue(out QueueItem item))
                    {
                        // Try to complete the waiter task. If it's been cancelled already, this will fail.
                        if (item.Waiter.TrySetException(e))
                        {
                            return true;
                        }

                        // Couldn't transfer to that waiter because it was cancelled. Try again.
                        Debug.Assert(item.Waiter.Task.IsCanceled);
                    }
                }

                return false;
            }

            public bool TryDequeueNextRequest(T connection)
            {
                if (_queue is not null)
                {
                    while (_queue.TryDequeue(out QueueItem item))
                    {
                        // Try to complete the task. If it's been cancelled already, this will return false.
                        if (item.Waiter.TrySetResult(connection))
                        {
                            return true;
                        }

                        // Couldn't transfer to that waiter because it was cancelled. Try again.
                        Debug.Assert(item.Waiter.Task.IsCanceled);
                    }
                }

                return false;
            }

            public bool TryPeekNextRequest([NotNullWhen(true)] out HttpRequestMessage? request)
            {
                if (_queue is not null)
                {
                    if (_queue.TryPeek(out QueueItem item))
                    {
                        request = item.Request;
                        return true;
                    }
                }

                request = null;
                return false;
            }

            public bool IsEmpty => Count == 0;

            public int Count => (_queue?.Count ?? 0);
        }
    }
}
