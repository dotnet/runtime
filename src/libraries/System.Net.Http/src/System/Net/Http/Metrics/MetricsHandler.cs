// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
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
        public readonly bool HasUriTags;
        public readonly string Method;
        private readonly int _hashCode;

        public ActiveRequestsTagKey(string? scheme, string? host, int port, bool hasUriTags, string method)
        {
            Scheme = scheme;
            Host = host;
            Port = port;
            HasUriTags = hasUriTags;
            Method = method;
            _hashCode = HashCode.Combine(scheme, host, port, hasUriTags, method);
        }

        public bool Equals(ActiveRequestsTagKey other) =>
            Scheme == other.Scheme &&
            Host == other.Host &&
            Port == other.Port &&
            HasUriTags == other.HasUriTags &&
            Method == other.Method;

        public override bool Equals(object? obj) => obj is ActiveRequestsTagKey other && Equals(other);

        public override int GetHashCode() => _hashCode;

        public TagList ToTagList()
        {
            TagList tags = default;
            if (HasUriTags)
            {
                tags.Add("url.scheme", Scheme);
                tags.Add("server.address", Host);
                tags.Add("server.port", DiagnosticsHelper.GetBoxedInt32(Port));
            }
            tags.Add("http.request.method", Method);
            return tags;
        }
    }

    /// <summary>
    /// Thread-safe tracker for active request counts by tag combination.
    /// </summary>
    internal sealed class ActiveRequestsTracker
    {
        private readonly ConcurrentDictionary<ActiveRequestsTagKey, long> _counts = new();

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
        private readonly ActiveRequestsTracker _activeRequestsTracker = new();
        private readonly ObservableUpDownCounter<long> _activeRequests;
        private readonly Histogram<double> _requestsDuration;
        private readonly IWebProxy? _proxy;

        public MetricsHandler(HttpMessageHandler innerHandler, IMeterFactory? meterFactory, IWebProxy? proxy, out Meter meter)
        {
            Debug.Assert(GlobalHttpSettings.MetricsHandler.IsGloballyEnabled);

            _innerHandler = innerHandler;
            _proxy = proxy;

            meter = meterFactory?.Create("System.Net.Http") ?? SharedMeter.Instance;

            // Meter has a cache for the instruments it owns
            _activeRequests = meter.CreateObservableUpDownCounter<long>(
                "http.client.active_requests",
                observeValues: _activeRequestsTracker.GetMeasurements,
                unit: "{request}",
                description: "Number of outbound HTTP requests that are currently active on the client.");
            _requestsDuration = meter.CreateHistogram<double>(
                "http.client.request.duration",
                unit: "s",
                description: "Duration of HTTP client requests.",
                advice: DiagnosticsHelper.ShortHistogramAdvice);
        }

        internal override ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (_activeRequests.Enabled || _requestsDuration.Enabled)
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

            (long startTimestamp, bool recordCurrentRequests) = RequestStart(request);
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
                RequestStop(request, response, exception, startTimestamp, recordCurrentRequests);
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

        private (long StartTimestamp, bool RecordCurrentRequests) RequestStart(HttpRequestMessage request)
        {
            bool recordCurrentRequests = _activeRequests.Enabled;
            long startTimestamp = Stopwatch.GetTimestamp();

            if (recordCurrentRequests)
            {
                _activeRequestsTracker.Increment(CreateActiveRequestsTagKey(request));
            }

            return (startTimestamp, recordCurrentRequests);
        }

        private void RequestStop(HttpRequestMessage request, HttpResponseMessage? response, Exception? exception, long startTimestamp, bool recordCurrentRequests)
        {
            TagList tags = InitializeCommonTags(request);

            if (recordCurrentRequests)
            {
                _activeRequestsTracker.Decrement(CreateActiveRequestsTagKey(request));
            }

            if (!_requestsDuration.Enabled)
            {
                return;
            }

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

        private TagList InitializeCommonTags(HttpRequestMessage request)
        {
            TagList tags = default;

            if (request.RequestUri is Uri requestUri && requestUri.IsAbsoluteUri)
            {
                tags.Add("url.scheme", requestUri.Scheme);
                tags.Add("server.address", DiagnosticsHelper.GetServerAddress(request, _proxy));
                tags.Add("server.port", DiagnosticsHelper.GetBoxedInt32(requestUri.Port));
            }
            tags.Add(DiagnosticsHelper.GetMethodTag(request.Method, out _));

            return tags;
        }

        private ActiveRequestsTagKey CreateActiveRequestsTagKey(HttpRequestMessage request)
        {
            string? scheme = null;
            string? host = null;
            int port = 0;
            bool hasUriTags = false;

            if (request.RequestUri is Uri requestUri && requestUri.IsAbsoluteUri)
            {
                scheme = requestUri.Scheme;
                host = DiagnosticsHelper.GetServerAddress(request, _proxy);
                port = requestUri.Port;
                hasUriTags = true;
            }

            string method = (string)DiagnosticsHelper.GetMethodTag(request.Method, out _).Value!;

            return new ActiveRequestsTagKey(scheme, host, port, hasUriTags, method);
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
