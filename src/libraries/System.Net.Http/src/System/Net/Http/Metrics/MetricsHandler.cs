// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Metrics
{
    internal sealed class MetricsHandler : HttpMessageHandlerStage
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly UpDownCounter<long> _currentRequests;
        private readonly Counter<long> _failedRequests;
        private readonly Histogram<double> _requestsDuration;

        public MetricsHandler(HttpMessageHandler innerHandler, IMeterFactory? meterFactory)
        {
            _innerHandler = innerHandler;

            Meter meter = meterFactory?.Create("System.Net.Http") ?? SharedMeter.Instance;

            // Meter has a cache for the instruments it owns
            _currentRequests = meter.CreateUpDownCounter<long>(
                "http-client-current-requests",
                description: "Number of outbound HTTP requests that are currently active on the client.");
            _failedRequests = meter.CreateCounter<long>(
                "http-client-failed-requests",
                description: "Number of outbound HTTP requests that have failed.");
            _requestsDuration = meter.CreateHistogram<double>(
                "http-client-request-duration",
                unit: "s",
                description: "The duration of outbound HTTP requests.");
        }

        internal override ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (_currentRequests.Enabled || _failedRequests.Enabled || _requestsDuration.Enabled)
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
            bool recordCurrentRequests = _currentRequests.Enabled;
            long startTimestamp = Stopwatch.GetTimestamp();

            if (recordCurrentRequests)
            {
                TagList tags = InitializeCommonTags(request);
                _currentRequests.Add(1, tags);
            }

            return (startTimestamp, recordCurrentRequests);
        }

        private void RequestStop(HttpRequestMessage request, HttpResponseMessage? response, Exception? exception, long startTimestamp, bool recordCurrentRequsts)
        {
            TagList tags = InitializeCommonTags(request);

            if (recordCurrentRequsts)
            {
                _currentRequests.Add(-1, tags);
            }

            bool recordRequestDuration = _requestsDuration.Enabled;
            bool recordFailedRequests = _failedRequests.Enabled && response is null;

            HttpMetricsEnrichmentContext? enrichmentContext = null;
            if (recordRequestDuration || recordFailedRequests)
            {
                if (response is not null)
                {
                    tags.Add("status-code", GetBoxedStatusCode((int)response.StatusCode));
                    tags.Add("protocol", GetProtocolName(response.Version));
                }
                enrichmentContext = HttpMetricsEnrichmentContext.GetEnrichmentContextForRequest(request);
            }

            if (enrichmentContext is null)
            {
                if (recordRequestDuration)
                {
                    TimeSpan duration = Stopwatch.GetElapsedTime(startTimestamp, Stopwatch.GetTimestamp());
                    _requestsDuration.Record(duration.TotalSeconds, tags);
                }

                if (recordFailedRequests)
                {
                    _failedRequests.Add(1, tags);
                }
            }
            else
            {
                enrichmentContext.RecordWithEnrichment(request, response, exception, startTimestamp, tags, recordRequestDuration, recordFailedRequests, _requestsDuration, _failedRequests);
            }
        }

        private static string GetProtocolName(Version httpVersion) => (httpVersion.Major, httpVersion.Minor) switch
        {
            (1, 0) => "HTTP/1.0",
            (1, 1) => "HTTP/1.1",
            (2, 0) => "HTTP/2",
            (3, 0) => "HTTP/3",
            _ => $"HTTP/{httpVersion.Major}.{httpVersion.Minor}"
        };

        private static TagList InitializeCommonTags(HttpRequestMessage request)
        {
            TagList tags = default;

            if (request.RequestUri is Uri requestUri && requestUri.IsAbsoluteUri)
            {
                tags.Add("scheme", requestUri.Scheme);
                tags.Add("host", requestUri.Host);
                // Add port tag when not the default value for the current scheme
                if (!requestUri.IsDefaultPort)
                {
                    tags.Add("port", requestUri.Port);
                }
            }
            tags.Add("method", request.Method.Method);

            return tags;
        }

        private static object[]? s_boxedStatusCodes;

        private static object GetBoxedStatusCode(int statusCode)
        {
            object[] boxes = LazyInitializer.EnsureInitialized(ref s_boxedStatusCodes, static () => new object[512]);

            return (uint)statusCode < (uint)boxes.Length
                ? boxes[statusCode] ??= statusCode
                : statusCode;
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
