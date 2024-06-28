// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging
{
    internal static class LogHelper
    {
        private static readonly LogDefineOptions s_skipEnabledCheckLogDefineOptions = new LogDefineOptions() { SkipEnabledCheck = true };
        private static readonly bool s_disableUriQueryRedaction = GetDisableUriQueryRedactionSettingValue();

        private static class EventIds
        {
            public static readonly EventId RequestStart = new EventId(100, "RequestStart");
            public static readonly EventId RequestEnd = new EventId(101, "RequestEnd");

            public static readonly EventId RequestHeader = new EventId(102, "RequestHeader");
            public static readonly EventId ResponseHeader = new EventId(103, "ResponseHeader");

            public static readonly EventId PipelineStart = new EventId(100, "RequestPipelineStart");
            public static readonly EventId PipelineEnd = new EventId(101, "RequestPipelineEnd");

            public static readonly EventId RequestPipelineRequestHeader = new EventId(102, "RequestPipelineRequestHeader");
            public static readonly EventId RequestPipelineResponseHeader = new EventId(103, "RequestPipelineResponseHeader");
        }

        private static readonly Action<ILogger, HttpMethod, string?, Exception?> _requestStart = LoggerMessage.Define<HttpMethod, string?>(
            LogLevel.Information,
            EventIds.RequestStart,
            "Sending HTTP request {HttpMethod} {Uri}",
            s_skipEnabledCheckLogDefineOptions);

        private static readonly Action<ILogger, double, int, Exception?> _requestEnd = LoggerMessage.Define<double, int>(
            LogLevel.Information,
            EventIds.RequestEnd,
            "Received HTTP response headers after {ElapsedMilliseconds}ms - {StatusCode}");

        private static readonly Func<ILogger, HttpMethod, string?, IDisposable?> _beginRequestPipelineScope = LoggerMessage.DefineScope<HttpMethod, string?>("HTTP {HttpMethod} {Uri}");

        private static readonly Action<ILogger, HttpMethod, string?, Exception?> _requestPipelineStart = LoggerMessage.Define<HttpMethod, string?>(
            LogLevel.Information,
            EventIds.PipelineStart,
            "Start processing HTTP request {HttpMethod} {Uri}");

        private static readonly Action<ILogger, double, int, Exception?> _requestPipelineEnd = LoggerMessage.Define<double, int>(
            LogLevel.Information,
            EventIds.PipelineEnd,
            "End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}");

        private static bool GetDisableUriQueryRedactionSettingValue()
        {
            if (AppContext.TryGetSwitch("Microsoft.Extensions.Http.DisableUriQueryRedaction", out bool value))
            {
                return value;
            }

            string? envVar = Environment.GetEnvironmentVariable("DOTNET_MICROSOFT_EXTENSIONS_HTTP_DISABLEURIQUERYREDACTION");

            if (bool.TryParse(envVar, out value))
            {
                return value;
            }
            else if (uint.TryParse(envVar, out uint intVal))
            {
                return intVal != 0;
            }

            return false;
        }

        public static void LogRequestStart(this ILogger logger, HttpRequestMessage request, Func<string, bool> shouldRedactHeaderValue)
        {
            // We check here to avoid allocating in the GetUriString call unnecessarily
            if (logger.IsEnabled(LogLevel.Information))
            {
                _requestStart(logger, request.Method, GetRedactedUriString(request.RequestUri), null);
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

        public static IDisposable? BeginRequestPipelineScope(this ILogger logger, HttpRequestMessage request)
        {
            return _beginRequestPipelineScope(logger, request.Method, GetRedactedUriString(request.RequestUri));
        }

        public static void LogRequestPipelineStart(this ILogger logger, HttpRequestMessage request, Func<string, bool> shouldRedactHeaderValue)
        {
            _requestPipelineStart(logger, request.Method, GetRedactedUriString(request.RequestUri), null);

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

        internal static string? GetRedactedUriString(Uri? uri)
        {
            if (uri is null)
            {
                return null;
            }

            string uriString = uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString();

            if (s_logQueryString)
            {
                return uriString;
            }

            int queryOffset = uriString.IndexOf('?');

            if (queryOffset < 0 || queryOffset == uriString.Length - 1)
            {
                // No query string or empty query string.
                return uriString;
            }

#if NET
            return $"{uriString.AsSpan(0, queryOffset + 1)}*";
#else
            return $"{uriString.Substring(0, queryOffset + 1)}*";
#endif
        }
    }
}
