// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Metrics;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class HttpConnectionBase : IDisposable, IHttpTrace
    {
        protected readonly HttpConnectionPool _pool;

        private static long s_connectionCounter = -1;

        // May be null if none of the counters were enabled when the connection was established.
        private ConnectionMetrics? _connectionMetrics;

        // Indicates whether we've counted this connection as established, so that we can
        // avoid decrementing the counter once it's closed in case telemetry was enabled in between.
        private bool _httpTelemetryMarkedConnectionAsOpened;

        private readonly long _creationTickCount = Environment.TickCount64;
        private long? _idleSinceTickCount;

        /// <summary>Cached string for the last Date header received on this connection.</summary>
        private string? _lastDateHeaderValue;
        /// <summary>Cached string for the last Server header received on this connection.</summary>
        private string? _lastServerHeaderValue;

        public long Id { get; } = Interlocked.Increment(ref s_connectionCounter);

        public HttpConnectionBase(HttpConnectionPool pool)
        {
            Debug.Assert(this is HttpConnection or Http2Connection or Http3Connection);
            Debug.Assert(pool != null);
            _pool = pool;
        }
        public HttpConnectionBase(HttpConnectionPool pool, IPEndPoint? remoteEndPoint)
            : this(pool)
        {
            MarkConnectionAsEstablished(remoteEndPoint);
        }

        protected void MarkConnectionAsEstablished(IPEndPoint? remoteEndPoint)
        {
            Debug.Assert(_pool.Settings._metrics is not null);

            SocketsHttpHandlerMetrics metrics = _pool.Settings._metrics;
            if (metrics.OpenConnections.Enabled || metrics.ConnectionDuration.Enabled)
            {
                // While requests may report HTTP/1.0 as the protocol, we treat all HTTP/1.X connections as HTTP/1.1.
                string protocol =
                    this is HttpConnection ? "1.1" :
                    this is Http2Connection ? "2" :
                    "3";

                _connectionMetrics = new ConnectionMetrics(
                    metrics,
                    protocol,
                    _pool.IsSecure ? "https" : "http",
                    _pool.OriginAuthority.HostValue,
                    _pool.OriginAuthority.Port,
                    remoteEndPoint?.Address?.ToString());

                _connectionMetrics.ConnectionEstablished();
            }

            _idleSinceTickCount = _creationTickCount;

            if (HttpTelemetry.Log.IsEnabled())
            {
                _httpTelemetryMarkedConnectionAsOpened = true;

                string scheme = _pool.IsSecure ? "https" : "http";
                string host = _pool.OriginAuthority.HostValue;
                int port = _pool.OriginAuthority.Port;

                if (this is HttpConnection) HttpTelemetry.Log.Http11ConnectionEstablished(Id, scheme, host, port, remoteEndPoint);
                else if (this is Http2Connection) HttpTelemetry.Log.Http20ConnectionEstablished(Id, scheme, host, port, remoteEndPoint);
                else HttpTelemetry.Log.Http30ConnectionEstablished(Id, scheme, host, port, remoteEndPoint);
            }
        }

        public void MarkConnectionAsClosed()
        {
            _connectionMetrics?.ConnectionClosed(durationMs: Environment.TickCount64 - _creationTickCount);

            if (HttpTelemetry.Log.IsEnabled())
            {
                // Only decrement the connection count if we counted this connection
                if (_httpTelemetryMarkedConnectionAsOpened)
                {
                    if (this is HttpConnection) HttpTelemetry.Log.Http11ConnectionClosed(Id);
                    else if (this is Http2Connection) HttpTelemetry.Log.Http20ConnectionClosed(Id);
                    else HttpTelemetry.Log.Http30ConnectionClosed(Id);
                }
            }
        }

        public void MarkConnectionAsIdle()
        {
            _idleSinceTickCount = Environment.TickCount64;
            _connectionMetrics?.IdleStateChanged(idle: true);
        }

        public void MarkConnectionAsNotIdle()
        {
            _idleSinceTickCount = null;
            _connectionMetrics?.IdleStateChanged(idle: false);
        }

        /// <summary>Uses <see cref="HeaderDescriptor.GetHeaderValue"/>, but first special-cases several known headers for which we can use caching.</summary>
        public string GetResponseHeaderValueWithCaching(HeaderDescriptor descriptor, ReadOnlySpan<byte> value, Encoding? valueEncoding)
        {
            return
                descriptor.Equals(KnownHeaders.Date) ? GetOrAddCachedValue(ref _lastDateHeaderValue, descriptor, value, valueEncoding) :
                descriptor.Equals(KnownHeaders.Server) ? GetOrAddCachedValue(ref _lastServerHeaderValue, descriptor, value, valueEncoding) :
                descriptor.GetHeaderValue(value, valueEncoding);

            static string GetOrAddCachedValue([NotNull] ref string? cache, HeaderDescriptor descriptor, ReadOnlySpan<byte> value, Encoding? encoding)
            {
                string? lastValue = cache;
                if (lastValue is null || !Ascii.Equals(value, lastValue))
                {
                    cache = lastValue = descriptor.GetHeaderValue(value, encoding);
                }
                Debug.Assert(cache is not null);
                return lastValue;
            }
        }

        public abstract void Trace(string message, [CallerMemberName] string? memberName = null);

        protected void TraceConnection(Stream stream)
        {
            if (stream is SslStream sslStream)
            {
                Trace(
                    $"{this}. Id:{Id}, " +
                    $"SslProtocol:{sslStream.SslProtocol}, NegotiatedApplicationProtocol:{sslStream.NegotiatedApplicationProtocol}, " +
                    $"NegotiatedCipherSuite:{sslStream.NegotiatedCipherSuite}, CipherAlgorithm:{sslStream.CipherAlgorithm}, CipherStrength:{sslStream.CipherStrength}, " +
                    $"HashAlgorithm:{sslStream.HashAlgorithm}, HashStrength:{sslStream.HashStrength}, " +
                    $"KeyExchangeAlgorithm:{sslStream.KeyExchangeAlgorithm}, KeyExchangeStrength:{sslStream.KeyExchangeStrength}, " +
                    $"LocalCertificate:{sslStream.LocalCertificate}, RemoteCertificate:{sslStream.RemoteCertificate}");
            }
            else
            {
                Trace($"{this}. Id:{Id}");
            }
        }

        public long GetLifetimeTicks(long nowTicks) => nowTicks - _creationTickCount;

        public long GetIdleTicks(long nowTicks) => _idleSinceTickCount is long idleSinceTickCount ? nowTicks - idleSinceTickCount : 0;

        /// <summary>Check whether a connection is still usable, or should be scavenged.</summary>
        /// <returns>True if connection can be used.</returns>
        public virtual bool CheckUsabilityOnScavenge() => true;

        internal static bool IsDigit(byte c) => (uint)(c - '0') <= '9' - '0';

        internal static int ParseStatusCode(ReadOnlySpan<byte> value)
        {
            byte status1, status2, status3;
            if (value.Length != 3 ||
                !IsDigit(status1 = value[0]) ||
                !IsDigit(status2 = value[1]) ||
                !IsDigit(status3 = value[2]))
            {
                throw new HttpRequestException(HttpRequestError.InvalidResponse, SR.Format(SR.net_http_invalid_response_status_code, Encoding.ASCII.GetString(value)));
            }

            return 100 * (status1 - '0') + 10 * (status2 - '0') + (status3 - '0');
        }

        /// <summary>Awaits a task, logging any resulting exceptions (which are otherwise ignored).</summary>
        internal void LogExceptions(Task task)
        {
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                {
                    LogFaulted(this, task);
                }
            }
            else
            {
                task.ContinueWith(static (t, state) => LogFaulted((HttpConnectionBase)state!, t), this,
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }

            static void LogFaulted(HttpConnectionBase connection, Task task)
            {
                Debug.Assert(task.IsFaulted);
                Exception? e = task.Exception!.InnerException; // Access Exception even if not tracing, to avoid TaskScheduler.UnobservedTaskException firing
                if (NetEventSource.Log.IsEnabled()) connection.Trace($"Exception from asynchronous processing: {e}");
            }
        }

        public abstract void Dispose();

        /// <summary>
        /// Called by <see cref="HttpConnectionPool.CleanCacheAndDisposeIfUnused"/> while holding the lock.
        /// </summary>
        public bool IsUsable(long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout)
        {
            // Validate that the connection hasn't been idle in the pool for longer than is allowed.
            if (pooledConnectionIdleTimeout != Timeout.InfiniteTimeSpan)
            {
                long idleTicks = GetIdleTicks(nowTicks);
                if (idleTicks > pooledConnectionIdleTimeout.TotalMilliseconds)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"Scavenging connection. Idle {TimeSpan.FromMilliseconds(idleTicks)} > {pooledConnectionIdleTimeout}.");
                    return false;
                }
            }

            // Validate that the connection lifetime has not been exceeded.
            if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
            {
                long lifetimeTicks = GetLifetimeTicks(nowTicks);
                if (lifetimeTicks > pooledConnectionLifetime.TotalMilliseconds)
                {
                    if (NetEventSource.Log.IsEnabled()) Trace($"Scavenging connection. Lifetime {TimeSpan.FromMilliseconds(lifetimeTicks)} > {pooledConnectionLifetime}.");
                    return false;
                }
            }

            if (!CheckUsabilityOnScavenge())
            {
                if (NetEventSource.Log.IsEnabled()) Trace($"Scavenging connection. Keep-Alive timeout exceeded, unexpected data or EOF received.");
                return false;
            }

            return true;
        }
    }
}
