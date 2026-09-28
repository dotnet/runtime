// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging
{
    internal static class LogHelper
    {
        private static readonly LogDefineOptions s_skipEnabledCheckLogDefineOptions = new LogDefineOptions() { SkipEnabledCheck = true };

        private static class EventIds
        {
            public static readonly EventId RequestStart = new EventId(100, "RequestStart");
            public static readonly EventId RequestEnd = new EventId(101, "RequestEnd");

            public static readonly EventId RequestHeader = new EventId(102, "RequestHeader");
            public static readonly EventId ResponseHeader = new EventId(103, "ResponseHeader");

            public static readonly EventId RequestFailed = new EventId(104, "RequestFailed");

            public static readonly EventId PipelineStart = new EventId(100, "RequestPipelineStart");
            public static readonly EventId PipelineEnd = new EventId(101, "RequestPipelineEnd");

            public static readonly EventId RequestPipelineRequestHeader = new EventId(102, "RequestPipelineRequestHeader");
            public static readonly EventId RequestPipelineResponseHeader = new EventId(103, "RequestPipelineResponseHeader");

            public static readonly EventId PipelineFailed = new EventId(104, "RequestPipelineFailed");
        }

        public static readonly Func<string, bool> ShouldRedactHeaderValue = (header) => true;

        private static readonly Action<ILogger, HttpMethod, string?, Exception?> _requestStart = LoggerMessage.Define<HttpMethod, string?>(
            LogLevel.Information,
            EventIds.RequestStart,
            "Sending HTTP request {HttpMethod} {Uri}",
            s_skipEnabledCheckLogDefineOptions);

        private static readonly Action<ILogger, double, int, Exception?> _requestEnd = LoggerMessage.Define<double, int>(
            LogLevel.Information,
            EventIds.RequestEnd,
            "Received HTTP response headers after {ElapsedMilliseconds}ms - {StatusCode}");

        private static readonly Action<ILogger, double, Exception?> _requestFailed = LoggerMessage.Define<double>(
            LogLevel.Information,
            EventIds.RequestFailed,
            "HTTP request failed after {ElapsedMilliseconds}ms");

        private static readonly Func<ILogger, HttpMethod, string?, IDisposable?> _beginRequestPipelineScope = LoggerMessage.DefineScope<HttpMethod, string?>("HTTP {HttpMethod} {Uri}");

        private static readonly Action<ILogger, HttpMethod, string?, Exception?> _requestPipelineStart = LoggerMessage.Define<HttpMethod, string?>(
            LogLevel.Information,
            EventIds.PipelineStart,
            "Start processing HTTP request {HttpMethod} {Uri}");

        private static readonly Action<ILogger, double, int, Exception?> _requestPipelineEnd = LoggerMessage.Define<double, int>(
            LogLevel.Information,
            EventIds.PipelineEnd,
            "End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}");

        private static readonly Action<ILogger, double, Exception?> _requestPipelineFailed = LoggerMessage.Define<double>(
            LogLevel.Information,
            EventIds.PipelineFailed,
            "HTTP request failed after {ElapsedMilliseconds}ms");

        public static void LogRequestStart(this ILogger logger, HttpRequestMessage request, Func<string, bool> shouldRedactHeaderValue)
        {
            // We check here to avoid allocating in the GetRedactedUriString call unnecessarily
            if (logger.IsEnabled(LogLevel.Information))
            {
                _requestStart(logger, request.Method, UriRedactionHelper.GetRedactedUriString(request.RequestUri), null);
            }

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(
                    LogLevel.Trace,
                    EventIds.RequestHeader,
                    new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Request, request.Headers, request.Content?.Headers, shouldRedactHeaderValue),
                    null,
                    (state, ex) => state.ToString());
            }
        }

        public static void LogRequestEnd(this ILogger logger, HttpResponseMessage response, TimeSpan duration, Func<string, bool> shouldRedactHeaderValue)
        {
            _requestEnd(logger, duration.TotalMilliseconds, (int)response.StatusCode, null);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(
                    LogLevel.Trace,
                    EventIds.ResponseHeader,
                    new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Response, response.Headers, response.Content?.Headers, shouldRedactHeaderValue),
                    null,
                    (state, ex) => state.ToString());
            }
        }

        public static void LogRequestFailed(this ILogger logger, TimeSpan duration, HttpRequestException exception) =>
            _requestFailed(logger, duration.TotalMilliseconds, exception);

        public static IDisposable? BeginRequestPipelineScope(this ILogger logger, HttpRequestMessage request, out string? formattedUri)
        {
            formattedUri = UriRedactionHelper.GetRedactedUriString(request.RequestUri);
            return _beginRequestPipelineScope(logger, request.Method, formattedUri);
        }

        public static void LogRequestPipelineStart(this ILogger logger, HttpRequestMessage request, string? formattedUri, Func<string, bool> shouldRedactHeaderValue)
        {
            _requestPipelineStart(logger, request.Method, formattedUri, null);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(
                    LogLevel.Trace,
                    EventIds.RequestPipelineRequestHeader,
                    new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Request, request.Headers, request.Content?.Headers, shouldRedactHeaderValue),
                    null,
                    (state, ex) => state.ToString());
            }
        }

        public static void LogRequestPipelineEnd(this ILogger logger, HttpResponseMessage response, TimeSpan duration, Func<string, bool> shouldRedactHeaderValue)
        {
            _requestPipelineEnd(logger, duration.TotalMilliseconds, (int)response.StatusCode, null);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(
                    LogLevel.Trace,
                    EventIds.RequestPipelineResponseHeader,
                    new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Response, response.Headers, response.Content?.Headers, shouldRedactHeaderValue),
                    null,
                    (state, ex) => state.ToString());
            }
        }

        public static void LogRequestPipelineFailed(this ILogger logger, TimeSpan duration, HttpRequestException exception) =>
            _requestPipelineFailed(logger, duration.TotalMilliseconds, exception);
    }
}
