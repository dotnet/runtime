// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Metrics
{
    /// <summary>
    /// Represents a unique combination of tags for tracking active requests.
    /// </summary>
    internal readonly struct ActiveRequestsTagKey : IEquatable<ActiveRequestsTagKey>
    {
        public readonly string? Scheme;
        public readonly string? Host;
        public readonly int Port;
        public readonly string Method;
        private readonly int _hashCode;

        public ActiveRequestsTagKey(string? scheme, string? host, int port, string method)
        {
            Scheme = scheme;
            Host = host;
            Port = port;
            Method = method;
            _hashCode = HashCode.Combine(scheme, host, port, method);
        }

        public bool Equals(ActiveRequestsTagKey other) =>
            Scheme == other.Scheme &&
            Host == other.Host &&
            Port == other.Port &&
            Method == other.Method;

        public override bool Equals(object? obj) => obj is ActiveRequestsTagKey other && Equals(other);

        public override int GetHashCode() => _hashCode;

        public TagList ToTagList()
        {
            TagList tags = default;
            if (Scheme is not null)
            {
                tags.Add("url.scheme", Scheme);
                tags.Add("server.address", Host);
                tags.Add("server.port", DiagnosticsHelper.GetBoxedInt32(Port));
            }
            tags.Add("http.request.method", Method);
            return tags;
        }

        public override string ToString() =>
            $"{Method}{(Scheme is not null ? $" {Scheme}://{Host}:{Port}" : "")}";
    }

    /// <summary>
    /// Thread-safe tracker for active request counts by tag combination.
    /// Every handler that uses a meter shares that meter's tracker and its http.client.active_requests instrument.
    /// </summary>
    /// <remarks>
    /// A meter does not cache observable instruments and keeps each one until the meter is disposed.
    /// One instrument per handler would therefore grow without bound when handlers are recreated.
    /// </remarks>
    internal sealed class ActiveRequestsTracker
    {
        private static readonly ConditionalWeakTable<Meter, ActiveRequestsTracker> s_trackers = new();

        private readonly ConcurrentDictionary<ActiveRequestsTagKey, long> _counts = new();
        private ObservableUpDownCounter<long>? _instrument;

        private ActiveRequestsTracker()
        {
        }

        public bool Enabled => _instrument?.Enabled ?? false;

        public static ActiveRequestsTracker GetOrCreate(Meter meter)
        {
            if (!s_trackers.TryGetValue(meter, out ActiveRequestsTracker? tracker))
            {
                // Only the caller whose tracker wins the add creates the instrument, so it is created once per meter without a lock.
                // Creating the instrument runs listener callbacks, and a callback that creates a handler on this meter finds the tracker.
                // A caller that gets the tracker before its instrument exists sees Enabled == false, as if no listener had subscribed yet.
                ActiveRequestsTracker created = new();
                tracker = s_trackers.GetOrAdd(meter, created);
                if (tracker == created)
                {
                    created._instrument = meter.CreateObservableUpDownCounter<long>(
                        "http.client.active_requests",
                        observeValues: created.GetMeasurements,
                        unit: "{request}",
                        description: "Number of outbound HTTP requests that are currently active on the client.");
                }
            }

            return tracker;
        }

        /// <summary>
        /// Increments the count for the specified tag combination.
        /// </summary>
        public void Increment(in ActiveRequestsTagKey key)
        {
            _counts.AddOrUpdate(key, 1, static (_, currentValue) => currentValue + 1);
        }

        /// <summary>
        /// Decrements the count for the specified tag combination.
        /// Removes the entry if the count reaches zero.
        /// </summary>
        public void Decrement(in ActiveRequestsTagKey key)
        {
            // We need to atomically decrement and remove if zero.
            // Use a spin loop with TryGetValue/TryUpdate/TryRemove to handle this safely.
            while (true)
            {
                if (!_counts.TryGetValue(key, out long currentValue))
                {
                    // Key doesn't exist, nothing to decrement.
                    // This shouldn't happen in normal operation but we handle it gracefully.
                    Debug.Fail($"Decrement for non-existing request {key}");
                    return;
                }

                if (currentValue <= 1)
                {
                    // Try to remove the entry since it will become zero.
                    // Use the overload that checks the current value to ensure atomicity.
                    if (_counts.TryRemove(new KeyValuePair<ActiveRequestsTagKey, long>(key, currentValue)))
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
            foreach (KeyValuePair<ActiveRequestsTagKey, long> entry in _counts)
            {
                yield return new Measurement<long>(entry.Value, entry.Key.ToTagList());
            }
        }
    }

    internal sealed class MetricsHandler : HttpMessageHandlerStage
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly ActiveRequestsTracker _activeRequestsTracker;
        private readonly Histogram<double> _requestsDuration;
        private readonly IWebProxy? _proxy;

        public MetricsHandler(HttpMessageHandler innerHandler, IMeterFactory? meterFactory, IWebProxy? proxy, out Meter meter)
        {
            Debug.Assert(GlobalHttpSettings.MetricsHandler.IsGloballyEnabled);

            _innerHandler = innerHandler;
            _proxy = proxy;

            meter = meterFactory?.Create("System.Net.Http") ?? SharedMeter.Instance;

            _activeRequestsTracker = ActiveRequestsTracker.GetOrCreate(meter);
            _requestsDuration = meter.CreateHistogram<double>(
                "http.client.request.duration",
                unit: "s",
                description: "Duration of HTTP client requests.",
                advice: DiagnosticsHelper.ShortHistogramAdvice);
        }

        internal override ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (_activeRequestsTracker.Enabled || _requestsDuration.Enabled)
            {
                return SendAsyncWithMetrics(request, async, cancellationToken);
            }
            else
            {
                return async ?
                    new ValueTask<HttpResponseMessage>(_innerHandler.SendAsync(request, cancellationToken)) :
                    new ValueTask<HttpResponseMessage>(_innerHandler.Send(request, cancellationToken));
            }
        }

        private async ValueTask<HttpResponseMessage> SendAsyncWithMetrics(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            Debug.Assert(GlobalHttpSettings.MetricsHandler.IsGloballyEnabled);

            (long startTimestamp, bool recordCurrentRequests, ActiveRequestsTagKey requestTagKey) = RequestStart(request);
            HttpResponseMessage? response = null;
            Exception? exception = null;
            try
            {
                response = async ?
                    await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false) :
                    _innerHandler.Send(request, cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                RequestStop(request, response, exception, startTimestamp, recordCurrentRequests, requestTagKey);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerHandler.Dispose();
            }

            base.Dispose(disposing);
        }

        private (long StartTimestamp, bool RecordCurrentRequests, ActiveRequestsTagKey RequestTagKey) RequestStart(HttpRequestMessage request)
        {
            bool recordCurrentRequests = _activeRequestsTracker.Enabled;
            long startTimestamp = Stopwatch.GetTimestamp();

            ActiveRequestsTagKey requestTagKey = CreateActiveRequestsTagKey(request);
            if (recordCurrentRequests)
            {
                _activeRequestsTracker.Increment(requestTagKey);
            }

            return (startTimestamp, recordCurrentRequests, requestTagKey);
        }

        private void RequestStop(HttpRequestMessage request, HttpResponseMessage? response, Exception? exception, long startTimestamp, bool recordCurrentRequests, ActiveRequestsTagKey requestTagKey)
        {
            if (recordCurrentRequests)
            {
                _activeRequestsTracker.Decrement(requestTagKey);
            }

            if (!_requestsDuration.Enabled)
            {
                return;
            }

            TagList tags = requestTagKey.ToTagList();
            if (response is not null)
            {
                tags.Add("http.response.status_code", DiagnosticsHelper.GetBoxedInt32((int)response.StatusCode));
                tags.Add("network.protocol.version", DiagnosticsHelper.GetProtocolVersionString(response.Version));
            }

            if (DiagnosticsHelper.TryGetErrorType(response, exception, out string? errorType))
            {
                tags.Add("error.type", errorType);
            }

            TimeSpan durationTime = Stopwatch.GetElapsedTime(startTimestamp, Stopwatch.GetTimestamp());

            List<Action<HttpMetricsEnrichmentContext>>? callbacks = HttpMetricsEnrichmentContext.GetEnrichmentCallbacksForRequest(request);
            if (callbacks is null)
            {
                _requestsDuration.Record(durationTime.TotalSeconds, tags);
            }
            else
            {
                HttpMetricsEnrichmentContext.RecordDurationWithEnrichment(callbacks, request, response, exception, durationTime, tags, _requestsDuration);
            }
        }

        private ActiveRequestsTagKey CreateActiveRequestsTagKey(HttpRequestMessage request)
        {
            string? scheme = null;
            string? host = null;
            int port = 0;

            if (request.RequestUri is Uri requestUri && requestUri.IsAbsoluteUri)
            {
                scheme = requestUri.Scheme;
                host = DiagnosticsHelper.GetServerAddress(request, _proxy);
                port = requestUri.Port;
            }

            string method = (string)DiagnosticsHelper.GetMethodTag(request.Method, out _).Value!;

            return new ActiveRequestsTagKey(scheme, host, port, method);
        }

        private sealed class SharedMeter : Meter
        {
            public static Meter Instance { get; } = new SharedMeter();
            private SharedMeter()
                : base("System.Net.Http")
            {
            }

            protected override void Dispose(bool disposing)
            {
                // NOP to prevent disposing the global instance from MeterListener callbacks.
            }
        }
    }
}
