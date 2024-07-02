// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging
{
    internal static class LogHelper
    {
        private static readonly LogDefineOptions s_skipEnabledCheckLogDefineOptions = new LogDefineOptions() { SkipEnabledCheck = true };
        private static readonly bool s_disableUriRedaction = GetDisableUriRedactionSettingValue();

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

        private static bool GetDisableUriRedactionSettingValue()
        {
            if (AppContext.TryGetSwitch("System.Net.Http.DisableUriRedaction", out bool value))
            {
                return value;
            }

            string? envVar = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_DISABLEURIREDACTION");

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

            if (s_disableUriRedaction)
            {
                return uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString();
            }

            if (!uri.IsAbsoluteUri)
            {
                // We cannot guarantee the redaction of UserInfo for relative Uris without implementing some subset of Uri parsing in this package.
                // To avoid this, we redact the whole Uri. Seeing a relative Uri in LoggingHttpMessageHandler or LoggingScopeHttpMessageHandler
                // requires a custom handler chain with custom expansion logic implemented by the user's HttpMessageHandler.
                // In such advanced scenarios we recommend users to log the Uri in their handler.
                return "*";
            }

            string pathAndQuery = uri.PathAndQuery;
            int queryIndex = pathAndQuery.IndexOf('?');

            bool redactQuery = queryIndex >= 0 && // Query is present.
                queryIndex < pathAndQuery.Length - 1; // Query is not empty.

            return (redactQuery, uri.IsDefaultPort) switch
            {
                (true, true) => $"{uri.Scheme}://{uri.Host}{GetPath(pathAndQuery, queryIndex)}*",
                (true, false) => $"{uri.Scheme}://{uri.Host}:{uri.Port}{GetPath(pathAndQuery, queryIndex)}*",
                (false, true) => $"{uri.Scheme}://{uri.Host}{pathAndQuery}",
                (false, false) => $"{uri.Scheme}://{uri.Host}:{uri.Port}{pathAndQuery}"
            };

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static ReadOnlySpan<char> GetPath(string pathAndQuery, int queryIndex) => pathAndQuery.AsSpan(0, queryIndex + 1);
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static string GetPath(string pathAndQuery, int queryIndex) => pathAndQuery.Substring(0, queryIndex + 1);
#endif
        }
    }
}
