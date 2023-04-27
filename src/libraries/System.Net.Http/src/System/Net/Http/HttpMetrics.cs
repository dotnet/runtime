// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace System.Net.Http
{
    internal sealed class HttpMetrics
    {
        private readonly Meter _meter;
        private readonly UpDownCounter<long> _currentRequests;
        private readonly Histogram<double> _requestsDuration;

        public HttpMetrics(Meter meter)
        {
            _meter = meter;

            _currentRequests = _meter.CreateUpDownCounter<long>(
                "current-requests",
                description: "The duration of outbound HTTP requests.");

            _requestsDuration = _meter.CreateHistogram<double>(
                "request-duration",
                unit: "s",
                description: "Number of outbound HTTP requests that are currently active on the client.");
        }

        public void RequestStart(HttpRequestMessage request)
        {
#pragma warning disable SA1129 // Do not use default value type constructor
            var tags = new TagList();
#pragma warning restore SA1129 // Do not use default value type constructor
            InitializeCommonTags(ref tags, request);
            _currentRequests.Add(1, tags);
        }

        public void RequestStop(HttpRequestMessage request, HttpResponseMessage? response, long startTimestamp, long currentTimestamp)
        {
#pragma warning disable SA1129 // Do not use default value type constructor
            var tags = new TagList();
#pragma warning restore SA1129 // Do not use default value type constructor
            InitializeCommonTags(ref tags, request);
            _currentRequests.Add(-1, tags);

            if (response != null)
            {
                tags.Add("status-code", (int)response.StatusCode); // Boxing?
                tags.Add("protocol", $"HTTP/{response.Version}"); // Hacky
            }
            if (request.HasTags)
            {
                foreach (var customTag in request.MetricsTags)
                {
                    tags.Add(customTag);
                }
            }
            var duration = Stopwatch.GetElapsedTime(startTimestamp, currentTimestamp);
            _requestsDuration.Record(duration.TotalSeconds, tags);
        }

        private static void InitializeCommonTags(ref TagList tags, HttpRequestMessage request)
        {
            if (request.RequestUri is { } requestUri && requestUri.IsAbsoluteUri)
            {
                if (requestUri.Scheme is not null)
                {
                    tags.Add("scheme", requestUri.Scheme);
                }
                if (requestUri.Host is not null)
                {
                    tags.Add("host", requestUri.Host);
                }
                // Add port tag when not the default value for the current scheme
                if (!requestUri.IsDefaultPort)
                {
                    tags.Add("port", requestUri.Port);
                }
            }
            tags.Add("method", request.Method.Method);
        }

        internal bool RequestCountersEnabled() => _currentRequests.Enabled || _requestsDuration.Enabled;
    }
}
