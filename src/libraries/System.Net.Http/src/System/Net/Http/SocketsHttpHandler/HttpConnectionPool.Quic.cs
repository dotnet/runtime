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
using System.Net.Quic.Implementations;
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
    internal sealed partial class HttpConnectionPool : IDisposable
    {
        /// <summary>Initially set to null, this can be set to enable HTTP/3 based on Alt-Svc.</summary>
        private volatile HttpAuthority? _http3Authority;

        /// <summary>If true, the <see cref="_http3Authority"/> will persist across a network change. If false, it will be reset to <see cref="_originAuthority"/>.</summary>
        private bool _persistAuthority;

        /// <summary>A timer to expire <see cref="_http3Authority"/> and return the pool to <see cref="_originAuthority"/>. Initialized on first use.</summary>
        private Timer? _authorityExpireTimer;

        /// <summary>
        /// When an Alt-Svc authority fails due to 421 Misdirected Request, it is placed in the blocklist to be ignored
        /// for <see cref="AltSvcBlocklistTimeoutInMilliseconds"/> milliseconds. Initialized on first use.
        /// </summary>
        private volatile HashSet<HttpAuthority>? _altSvcBlocklist;
        private CancellationTokenSource? _altSvcBlocklistTimerCancellation;
        private volatile bool _altSvcEnabled;

        /// <summary>
        /// If <see cref="_altSvcBlocklist"/> exceeds this size, Alt-Svc will be disabled entirely for <see cref="AltSvcBlocklistTimeoutInMilliseconds"/> milliseconds.
        /// This is to prevent a failing server from bloating the dictionary beyond a reasonable value.
        /// </summary>
        private const int MaxAltSvcIgnoreListSize = 8;

        /// <summary>The time, in milliseconds, that an authority should remain in <see cref="_altSvcBlocklist"/>.</summary>
        private const int AltSvcBlocklistTimeoutInMilliseconds = 10 * 60 * 1000;

        private bool IsHttp3Enabled;
        private Http3Connection? _http3Connection;
        private SemaphoreSlim? _http3ConnectionCreateLock;
        internal byte[]? _http3EncodedAuthorityHostHeader;
        private SslClientAuthenticationOptions? _sslOptionsHttp3;

        private static readonly List<SslApplicationProtocol> s_http3ApplicationProtocols = new List<SslApplicationProtocol>() { Http3Connection.Http3ApplicationProtocol31, Http3Connection.Http3ApplicationProtocol30, Http3Connection.Http3ApplicationProtocol29 };

        private ValueTask<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>?
            GetHttp3ConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpAuthority? authority = _http3Authority;
            // H3 is explicitly requested, assume prenegotiated H3.
            if (request.Version.Major >= 3 && request.VersionPolicy != HttpVersionPolicy.RequestVersionOrLower)
            {
                authority = authority ?? _originAuthority;
            }
            if (authority != null)
            {
#pragma warning disable CA2012
                if (IsAltSvcBlocked(authority))
                {
                    return ValueTask.FromException<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>(
                        new HttpRequestException(SR.Format(SR.net_http_requested_version_cannot_establish, request.Version, request.VersionPolicy, 3)));
                }

                return GetHttp3ConnectionAsync(request, authority, cancellationToken);
#pragma warning restore CA2012
            }

            return null;
        }

        private async ValueTask<(HttpConnectionBase? connection, bool isNewConnection, HttpResponseMessage? failureResponse)>
            GetHttp3ConnectionAsync(HttpRequestMessage request, HttpAuthority authority, CancellationToken cancellationToken)
        {
            Debug.Assert(_kind == HttpConnectionKind.Https);
            Debug.Assert(IsHttp3Enabled == true);

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
                    quicConnection = await ConnectQuicAsync(Settings._quicImplementationProvider ?? QuicImplementationProviders.Default, new DnsEndPoint(authority.IdnHost, authority.Port), _sslOptionsHttp3, cancellationToken).ConfigureAwait(false);
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

        private static async ValueTask<QuicConnection> ConnectQuicAsync(QuicImplementationProvider quicImplementationProvider, DnsEndPoint endPoint, SslClientAuthenticationOptions? clientAuthenticationOptions, CancellationToken cancellationToken)
        {
            QuicConnection con = new QuicConnection(quicImplementationProvider, endPoint, clientAuthenticationOptions);
            try
            {
                await con.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return con;
            }
            catch (Exception ex)
            {
                con.Dispose();
                throw ConnectHelper.CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
            }
        }

        partial void InitializeHttp3EncodedAuthorityHostHeader(string hostHeader)
        {
            _http3EncodedAuthorityHostHeader = QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(H3StaticTable.Authority, hostHeader);
        }

        partial void InitializeHttp3SslOptions(string sslHostName)
        {
            _sslOptionsHttp3 = ConstructSslOptions(_poolManager, sslHostName);
            _sslOptionsHttp3.ApplicationProtocols = s_http3ApplicationProtocols;
        }

        partial void InitializeHttpsConnectionKind()
        {
            IsHttp3Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version30 && (_poolManager.Settings._quicImplementationProvider ?? QuicImplementationProviders.Default).IsSupported;
            _altSvcEnabled = IsHttp3Enabled;
        }

        partial void DisposeHttp3Objects()
        {
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

        private bool ProcessAltSvc(HttpResponseMessage response, HttpConnectionBase? connection)
        {
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
                return true;
            }

            return false;
        }

        /// <summary>
        /// Expires the current Alt-Svc authority, resetting the connection back to origin.
        /// </summary>
        partial void ExpireAltSvcAuthority(bool expireTimer)
        {
            // If we ever support prenegotiated HTTP/3, this should be set to origin, not nulled out.
            _http3Authority = null;
            if (expireTimer)
            {
                Debug.Assert(_authorityExpireTimer != null);
                _authorityExpireTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
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
                }
            }
        }

        partial void HandleAltSvcQuic(HttpAuthority? nextAuthority, TimeSpan nextAuthorityMaxAge, bool nextAuthorityPersist)
        {
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
                                    @this.ExpireAltSvcAuthority(expireTimer: false);
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
    }
}
