// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class MetricsHandler : HttpMessageHandlerStage, IHttpMetricsLogger
{
    private readonly HttpMessageHandler _innerHandler;
    private readonly UpDownCounter<long> _currentRequests;
    private readonly Counter<long> _failedRequests;
    private readonly Histogram<double> _requestsDuration;

    [ThreadStatic]
    private static HttpMetricsEnrichmentContext? s_cachedEnrichmentContext;

    public MetricsHandler(HttpMessageHandler innerHandler, Meter meter)
    {
        _innerHandler = innerHandler;

        // Meter has a cache for
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

    public static Meter DefaultMeter { get; } = new SharedMeter();

    internal override ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
    {
        if (_currentRequests.Enabled || _failedRequests.Enabled || _requestsDuration.Enabled)
        {
            ArgumentNullException.ThrowIfNull(request);
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
            if (_failedRequests.Enabled)
            {
                // No exception has been thrown to the point of reading headers, but errors can still occur while buffering the response content in HttpClient.
                // We need to report http-client-failed-requests if it happens.
                response.HttpMetricsLogger = this;
            }
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

        if (recordRequestDuration || recordFailedRequests)
        {
            ApplyExtendedTags(ref tags, request, response, exception);
        }

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

    private static void ApplyExtendedTags(ref TagList tags, HttpRequestMessage request, HttpResponseMessage? response, Exception? exception)
    {
        if (response is not null)
        {
            tags.Add("status-code", StatusCodeCache.GetBoxedStatusCode(response.StatusCode));
            tags.Add("protocol", GetProtocolName(response.Version));
        }

        s_cachedEnrichmentContext ??= new HttpMetricsEnrichmentContext();

        // Protect against re-entrancy
        HttpMetricsEnrichmentContext enrichmentContext = s_cachedEnrichmentContext.InProgress ?
            new HttpMetricsEnrichmentContext() :
            s_cachedEnrichmentContext;

        enrichmentContext.ApplyEnrichment(request, response, exception, ref tags);
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

    void IHttpMetricsLogger.LogRequestFailed(HttpResponseMessage response, Exception exception)
    {
        Debug.Assert(response.RequestMessage is not null);
        TagList tags = InitializeCommonTags(response.RequestMessage);
        ApplyExtendedTags(ref tags, response.RequestMessage, response, exception);
        _failedRequests.Add(1, tags);
    }

    private static class StatusCodeCache
    {
        private static readonly object OK = (int)HttpStatusCode.OK;
        private static readonly object Created = (int)HttpStatusCode.Created;
        private static readonly object Accepted = (int)HttpStatusCode.Accepted;
        private static readonly object NoContent = (int)HttpStatusCode.NoContent;
        private static readonly object Moved = (int)HttpStatusCode.Moved;
        private static readonly object Redirect = (int)HttpStatusCode.Redirect;
        private static readonly object NotModified = (int)HttpStatusCode.NotModified;
        private static readonly object InternalServerError = (int)HttpStatusCode.InternalServerError;

        public static object GetBoxedStatusCode(HttpStatusCode statusCode) => statusCode switch
        {
            HttpStatusCode.OK => OK,
            HttpStatusCode.Created => Created,
            HttpStatusCode.Accepted => Accepted,
            HttpStatusCode.NoContent => NoContent,
            HttpStatusCode.Moved => Moved,
            HttpStatusCode.Redirect => Redirect,
            HttpStatusCode.NotModified => NotModified,
            HttpStatusCode.InternalServerError => InternalServerError,
            _ => (int)statusCode
        };
    }

    private sealed class SharedMeter : Meter
    {
        public SharedMeter()
            : base("System.Net.Http")
        {
        }

        protected override void Dispose(bool disposing)
        {
            // NOP to prevent disposing the global instance from arbitrary user code.
        }
    }
}
