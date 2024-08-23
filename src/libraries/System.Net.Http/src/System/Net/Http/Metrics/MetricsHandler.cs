// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Metrics
{
    internal sealed class MetricsHandler : HttpMessageHandlerStage
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly UpDownCounter<long> _activeRequests;
        private readonly Histogram<double> _requestsDuration;

        public MetricsHandler(HttpMessageHandler innerHandler, IMeterFactory? meterFactory, out Meter meter)
        {
            _innerHandler = innerHandler;

            meter = meterFactory?.Create("System.Net.Http") ?? SharedMeter.Instance;

            // Meter has a cache for the instruments it owns
            _activeRequests = meter.CreateUpDownCounter<long>(
                "http.client.active_requests",
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
                TagList tags = InitializeCommonTags(request);
                _activeRequests.Add(1, tags);
            }

            return (startTimestamp, recordCurrentRequests);
        }

        private void RequestStop(HttpRequestMessage request, HttpResponseMessage? response, Exception? exception, long startTimestamp, bool recordCurrentRequests)
        {
            TagList tags = InitializeCommonTags(request);

            if (recordCurrentRequests)
            {
                _activeRequests.Add(-1, tags);
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

            HttpMetricsEnrichmentContext? enrichmentContext = HttpMetricsEnrichmentContext.GetEnrichmentContextForRequest(request);
            if (enrichmentContext is null)
            {
                _requestsDuration.Record(durationTime.TotalSeconds, tags);
            }
            else
            {
                enrichmentContext.RecordDurationWithEnrichment(request, response, exception, durationTime, tags, _requestsDuration);
            }
        }

        private static TagList InitializeCommonTags(HttpRequestMessage request)
        {
            TagList tags = default;

            if (request.RequestUri is Uri requestUri && requestUri.IsAbsoluteUri)
            {
                tags.Add("url.scheme", requestUri.Scheme);
                tags.Add("server.address", requestUri.Host);
                tags.Add("server.port", DiagnosticsHelper.GetBoxedInt32(requestUri.Port));
            }
            tags.Add(DiagnosticsHelper.GetMethodTag(request.Method, out _));

            return tags;
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
