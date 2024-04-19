// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Quic;
using System.Net.Security;
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
        [SupportedOSPlatformGuard("Windows")]
        internal static bool IsHttp3Supported() => (OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid()) || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

        private bool _http3Enabled;
        private Http3Connection? _http3Connection;
        private SemaphoreSlim? _http3ConnectionCreateLock;
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

                if (quicConnection.NegotiatedApplicationProtocol != SslApplicationProtocol.Http3)
                {
                    BlocklistAuthority(authority);
                    throw new HttpRequestException(HttpRequestError.ConnectionError, "QUIC connected but no HTTP/3 indicated via ALPN.", null, RequestRetryType.RetryOnConnectionFailure);
                }

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
