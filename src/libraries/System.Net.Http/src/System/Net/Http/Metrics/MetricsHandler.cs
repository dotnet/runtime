// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
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
                enrichmentContext.RecordWithEnrichment(request, response, exception, tags, recordRequestDuration, recordFailedRequests, startTimestamp, _requestsDuration, _failedRequests);
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

        // Status Codes listed at http://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml
        private static readonly FrozenDictionary<int, object> s_boxedStatusCodes = FrozenDictionary.ToFrozenDictionary(new[]
        {
            KeyValuePair.Create<int, object>(100, 100),
            KeyValuePair.Create<int, object>(101, 101),
            KeyValuePair.Create<int, object>(102, 102),

            KeyValuePair.Create<int, object>(200, 200),
            KeyValuePair.Create<int, object>(201, 201),
            KeyValuePair.Create<int, object>(202, 202),
            KeyValuePair.Create<int, object>(203, 203),
            KeyValuePair.Create<int, object>(204, 204),
            KeyValuePair.Create<int, object>(205, 205),
            KeyValuePair.Create<int, object>(206, 206),
            KeyValuePair.Create<int, object>(207, 207),
            KeyValuePair.Create<int, object>(208, 208),
            KeyValuePair.Create<int, object>(226, 226),

            KeyValuePair.Create<int, object>(300, 300),
            KeyValuePair.Create<int, object>(301, 301),
            KeyValuePair.Create<int, object>(302, 302),
            KeyValuePair.Create<int, object>(303, 303),
            KeyValuePair.Create<int, object>(304, 304),
            KeyValuePair.Create<int, object>(305, 305),
            KeyValuePair.Create<int, object>(306, 306),
            KeyValuePair.Create<int, object>(307, 307),
            KeyValuePair.Create<int, object>(308, 308),

            KeyValuePair.Create<int, object>(400, 400),
            KeyValuePair.Create<int, object>(401, 401),
            KeyValuePair.Create<int, object>(402, 402),
            KeyValuePair.Create<int, object>(403, 403),
            KeyValuePair.Create<int, object>(404, 404),
            KeyValuePair.Create<int, object>(405, 405),
            KeyValuePair.Create<int, object>(406, 406),
            KeyValuePair.Create<int, object>(407, 407),
            KeyValuePair.Create<int, object>(408, 408),
            KeyValuePair.Create<int, object>(409, 409),
            KeyValuePair.Create<int, object>(410, 410),
            KeyValuePair.Create<int, object>(411, 411),
            KeyValuePair.Create<int, object>(412, 412),
            KeyValuePair.Create<int, object>(413, 413),
            KeyValuePair.Create<int, object>(414, 414),
            KeyValuePair.Create<int, object>(415, 415),
            KeyValuePair.Create<int, object>(416, 416),
            KeyValuePair.Create<int, object>(417, 417),
            KeyValuePair.Create<int, object>(418, 418),
            KeyValuePair.Create<int, object>(419, 419),
            KeyValuePair.Create<int, object>(421, 421),
            KeyValuePair.Create<int, object>(422, 422),
            KeyValuePair.Create<int, object>(423, 423),
            KeyValuePair.Create<int, object>(424, 424),
            KeyValuePair.Create<int, object>(426, 426),
            KeyValuePair.Create<int, object>(428, 428),
            KeyValuePair.Create<int, object>(429, 429),
            KeyValuePair.Create<int, object>(431, 431),
            KeyValuePair.Create<int, object>(451, 451),
            KeyValuePair.Create<int, object>(499, 499),

            KeyValuePair.Create<int, object>(500, 500),
            KeyValuePair.Create<int, object>(501, 501),
            KeyValuePair.Create<int, object>(502, 502),
            KeyValuePair.Create<int, object>(503, 503),
            KeyValuePair.Create<int, object>(504, 504),
            KeyValuePair.Create<int, object>(505, 505),
            KeyValuePair.Create<int, object>(506, 506),
            KeyValuePair.Create<int, object>(507, 507),
            KeyValuePair.Create<int, object>(508, 508),
            KeyValuePair.Create<int, object>(510, 510),
            KeyValuePair.Create<int, object>(511, 511)
        });

        private static object GetBoxedStatusCode(int statusCode)
        {
            if (s_boxedStatusCodes.TryGetValue(statusCode, out object? result))
            {
                return result;
            }

            return statusCode;
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
                // NOP to prevent disposing the global instance from arbitrary user code.
            }
        }
    }
}
