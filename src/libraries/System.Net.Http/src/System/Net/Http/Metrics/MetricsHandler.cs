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
                description: "Duration of HTTP client requests.");
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
                tags.Add("http.response.status_code", GetBoxedStatusCode((int)response.StatusCode));
                tags.Add("network.protocol.version", GetProtocolVersionString(response.Version));
            }

            if (TryGetErrorType(response, exception, out string? errorType))
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

        private static bool TryGetErrorType(HttpResponseMessage? response, Exception? exception, out string? errorType)
        {
            if (response is not null)
            {
                int statusCode = (int)response.StatusCode;

                // In case the status code indicates a client or a server error, return the string representation of the status code.
                // See the paragraph Status and the definition of 'error.type' in
                // https://github.com/open-telemetry/semantic-conventions/blob/2bad9afad58fbd6b33cc683d1ad1f006e35e4a5d/docs/http/http-spans.md
                if (statusCode >= 400 && statusCode <= 599)
                {
                    errorType = GetErrorStatusCodeString(statusCode);
                    return true;
                }
            }

            if (exception is null)
            {
                errorType = null;
                return false;
            }

            Debug.Assert(Enum.GetValues<HttpRequestError>().Length == 12, "We need to extend the mapping in case new values are added to HttpRequestError.");
            errorType = (exception as HttpRequestException)?.HttpRequestError switch
            {
                HttpRequestError.NameResolutionError => "name_resolution_error",
                HttpRequestError.ConnectionError => "connection_error",
                HttpRequestError.SecureConnectionError => "secure_connection_error",
                HttpRequestError.HttpProtocolError => "http_protocol_error",
                HttpRequestError.ExtendedConnectNotSupported => "extended_connect_not_supported",
                HttpRequestError.VersionNegotiationError => "version_negotiation_error",
                HttpRequestError.UserAuthenticationError => "user_authentication_error",
                HttpRequestError.ProxyTunnelError => "proxy_tunnel_error",
                HttpRequestError.InvalidResponse => "invalid_response",
                HttpRequestError.ResponseEnded => "response_ended",
                HttpRequestError.ConfigurationLimitExceeded => "configuration_limit_exceeded",

                // Fall back to the exception type name in case of HttpRequestError.Unknown or when exception is not an HttpRequestException.
                _ => exception.GetType().FullName!
            };
            return true;
        }

        private static string GetProtocolVersionString(Version httpVersion) => (httpVersion.Major, httpVersion.Minor) switch
        {
            (1, 0) => "1.0",
            (1, 1) => "1.1",
            (2, 0) => "2",
            (3, 0) => "3",
            _ => httpVersion.ToString()
        };

        private static TagList InitializeCommonTags(HttpRequestMessage request)
        {
            TagList tags = default;

            if (request.RequestUri is Uri requestUri && requestUri.IsAbsoluteUri)
            {
                tags.Add("url.scheme", requestUri.Scheme);
                tags.Add("server.address", requestUri.Host);
                // Add port tag when not the default value for the current scheme
                if (!requestUri.IsDefaultPort)
                {
                    tags.Add("server.port", requestUri.Port);
                }
            }
            tags.Add(GetMethodTag(request.Method));

            return tags;
        }

        internal static KeyValuePair<string, object?> GetMethodTag(HttpMethod method)
        {
            // Return canonical names for known methods and "_OTHER" for unknown ones.
            HttpMethod? known = HttpMethod.GetKnownMethod(method.Method);
            return new KeyValuePair<string, object?>("http.request.method", known?.Method ?? "_OTHER");
        }

        private static object[]? s_boxedStatusCodes;
        private static string[]? s_statusCodeStrings;

#pragma warning disable CA1859 // we explictly box here
        private static object GetBoxedStatusCode(int statusCode)
        {
            object[] boxes = LazyInitializer.EnsureInitialized(ref s_boxedStatusCodes, static () => new object[512]);

            return (uint)statusCode < (uint)boxes.Length
                ? boxes[statusCode] ??= statusCode
                : statusCode;
        }
#pragma warning restore

        private static string GetErrorStatusCodeString(int statusCode)
        {
            Debug.Assert(statusCode >= 400 && statusCode <= 599);

            string[] strings = LazyInitializer.EnsureInitialized(ref s_statusCodeStrings, static () => new string[200]);
            int index = statusCode - 400;
            return (uint)index < (uint)strings.Length
                ? strings[index] ??= statusCode.ToString()
                : statusCode.ToString();
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
