// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

namespace System.Net.Http.Metrics
{
    /// <summary>
    /// Represents a unique combination of tags for tracking open connections.
    /// </summary>
    internal readonly struct OpenConnectionsTagKey : IEquatable<OpenConnectionsTagKey>
    {
        public readonly string ProtocolVersion;
        public readonly string Scheme;
        public readonly string Host;
        public readonly int Port;
        public readonly bool IsIdle;
        public readonly string? PeerAddress;
        private readonly int _hashCode;

        public OpenConnectionsTagKey(string protocolVersion, string scheme, string host, int port, bool isIdle, string? peerAddress)
        {
            ProtocolVersion = protocolVersion;
            Scheme = scheme;
            Host = host;
            Port = port;
            IsIdle = isIdle;
            PeerAddress = peerAddress;
            _hashCode = HashCode.Combine(protocolVersion, scheme, host, port, isIdle);
        }

        public bool Equals(OpenConnectionsTagKey other) =>
            ProtocolVersion == other.ProtocolVersion &&
            Scheme == other.Scheme &&
            Host == other.Host &&
            Port == other.Port &&
            IsIdle == other.IsIdle &&
            PeerAddress == other.PeerAddress;

        public override bool Equals(object? obj) => obj is OpenConnectionsTagKey other && Equals(other);

        public override int GetHashCode() => _hashCode;

        public TagList ToTagList()
        {
            TagList tags = default;
            tags.Add("network.protocol.version", ProtocolVersion);
            tags.Add("url.scheme", Scheme);
            tags.Add("server.address", Host);
            tags.Add("server.port", DiagnosticsHelper.GetBoxedInt32(Port));
            tags.Add("http.connection.state", IsIdle ? "idle" : "active");
            if (PeerAddress is not null)
            {
                tags.Add("network.peer.address", PeerAddress);
            }
            return tags;
        }
    }

    /// <summary>
    /// Thread-safe tracker for open connection counts by tag combination.
    /// </summary>
    internal sealed class OpenConnectionsTracker
    {
        private readonly ConcurrentDictionary<OpenConnectionsTagKey, long> _counts = new();

        /// <summary>
        /// Increments the count for the specified tag combination.
        /// </summary>
        public void Increment(in OpenConnectionsTagKey key)
        {
            _counts.AddOrUpdate(key, 1, static (_, currentValue) => currentValue + 1);
        }

        /// <summary>
        /// Decrements the count for the specified tag combination.
        /// Removes the entry if the count reaches zero.
        /// </summary>
        public void Decrement(in OpenConnectionsTagKey key)
        {
            // We need to atomically decrement and remove if zero.
            // Use a spin loop with TryGetValue/TryUpdate/TryRemove to handle this safely.
            while (true)
            {
                if (!_counts.TryGetValue(key, out long currentValue))
                {
                    // Key doesn't exist, nothing to decrement.
                    // This shouldn't happen in normal operation but we handle it gracefully.
                    return;
                }

                if (currentValue <= 1)
                {
                    // Try to remove the entry since it will become zero.
                    // Use the overload that checks the current value to ensure atomicity.
                    if (_counts.TryRemove(new KeyValuePair<OpenConnectionsTagKey, long>(key, currentValue)))
                    {
                        return;
                    }
                    // Another thread modified the value, retry.
                }
                else
                {
                    // Try to decrement the value.
                    if (_counts.TryUpdate(key, currentValue - 1, currentValue))
                    {
                        return;
                    }
                    // Another thread modified the value, retry.
                }
            }
        }

        /// <summary>
        /// Returns measurements for all tag combinations with non-zero counts.
        /// </summary>
        public IEnumerable<Measurement<long>> GetMeasurements()
        {
            foreach (KeyValuePair<OpenConnectionsTagKey, long> entry in _counts)
            {
                yield return new Measurement<long>(entry.Value, entry.Key.ToTagList());
            }
        }
    }

    internal sealed class SocketsHttpHandlerMetrics
    {
        public readonly OpenConnectionsTracker OpenConnectionsTracker = new();

        public readonly ObservableUpDownCounter<long> OpenConnections;

        public readonly Histogram<double> ConnectionDuration;

        public readonly Histogram<double> RequestsQueueDuration;

        public SocketsHttpHandlerMetrics(Meter meter)
        {
            OpenConnections = meter.CreateObservableUpDownCounter<long>(
                name: "http.client.open_connections",
                observeValues: OpenConnectionsTracker.GetMeasurements,
                unit: "{connection}",
                description: "Number of outbound HTTP connections that are currently active or idle on the client.");

            ConnectionDuration = meter.CreateHistogram<double>(
                name: "http.client.connection.duration",
                unit: "s",
                description: "The duration of successfully established outbound HTTP connections.",
                advice: new InstrumentAdvice<double>()
                {
                    // These values are not based on a standard and may change in the future.
                    HistogramBucketBoundaries = [0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 120, 300]
                });

            RequestsQueueDuration = meter.CreateHistogram<double>(
                name: "http.client.request.time_in_queue",
                unit: "s",
                description: "The amount of time requests spent on a queue waiting for an available connection.",
                advice: DiagnosticsHelper.ShortHistogramAdvice);
        }

        public void RequestLeftQueue(HttpRequestMessage request, HttpConnectionPool pool, TimeSpan duration, int versionMajor)
        {
            Debug.Assert(versionMajor is 1 or 2 or 3);

            if (RequestsQueueDuration.Enabled)
            {
                TagList tags = default;

                // While requests may report HTTP/1.0 as the protocol, we treat all HTTP/1.X connections as HTTP/1.1.
                tags.Add("network.protocol.version", versionMajor switch
                {
                    1 => "1.1",
                    2 => "2",
                    _ => "3"
                });

                tags.Add("url.scheme", pool.IsSecure ? "https" : "http");

                Debug.Assert(pool.TelemetryServerAddress is not null, "TelemetryServerAddress should not be null when System.Diagnostics.Metrics.Meter.IsSupported is true.");
                tags.Add("server.address", pool.TelemetryServerAddress);

                if (!pool.IsDefaultPort)
                {
                    tags.Add("server.port", pool.OriginAuthority.Port);
                }

                tags.Add(DiagnosticsHelper.GetMethodTag(request.Method, out _));

                RequestsQueueDuration.Record(duration.TotalSeconds, tags);
            }
        }
    }
}
