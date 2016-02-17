// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Defines default values for http handler properties which is meant to be re-used across WinHttp and UnixHttp Handlers
    /// </summary>
    internal static class HttpHandlerDiagnosticListenerExtensions
    {
        public static Guid LogHttpRequest(this DiagnosticListener @this, HttpRequestMessage request)
        {
            return @this.IsEnabled(HttpHandlerLoggingStrings.RequestWriteName) ?
                LogHttpRequestCore(@this, request) :
                Guid.Empty;
        }

        public static void LogHttpResponse(this DiagnosticListener @this, HttpResponseMessage response, Guid loggingRequestId)
        {
            if (@this.IsEnabled(HttpHandlerLoggingStrings.ResponseWriteName))
            {
                LogHttpResponseCore(@this, response, loggingRequestId);
            }
        }

        public static void LogHttpResponse(this DiagnosticListener @this, Task<HttpResponseMessage> responseTask, Guid loggingRequestId)
        {
            if (@this.IsEnabled(HttpHandlerLoggingStrings.ResponseWriteName))
            {
                ScheduleLogResponse(@this, responseTask, loggingRequestId);
            }
        }

        private static void ScheduleLogResponse(DiagnosticListener diagnosticListener, Task<HttpResponseMessage> responseTask, Guid loggingRequestId)
        {
            responseTask.ContinueWith(
                t =>
                {
                    if (!t.IsFaulted &&
                        !t.IsCanceled)
                    {
                        LogHttpResponseCore(diagnosticListener, t.Result, loggingRequestId);
                    }
                },
                TaskScheduler.Default);
        }

        private static Guid LogHttpRequestCore(DiagnosticListener diagnosticListener, HttpRequestMessage request)
        {
            Guid loggingRequestId = Guid.NewGuid();
            long timestamp = Stopwatch.GetTimestamp();

            diagnosticListener.Write(
                HttpHandlerLoggingStrings.RequestWriteName,
                new
                {
                    Request = request,
                    LoggingRequestId = loggingRequestId,
                    Timestamp = timestamp
                }
            );

            return loggingRequestId;
        }

        private static void LogHttpResponseCore(DiagnosticListener diagnosticListener, HttpResponseMessage response, Guid loggingRequestId)
        {
            // An empty loggingRequestId signifies that the request was not logged, so do
            // not attempt to log response.
            if (loggingRequestId != Guid.Empty)
            {
                long timestamp = Stopwatch.GetTimestamp();

                diagnosticListener.Write(
                    HttpHandlerLoggingStrings.ResponseWriteName,
                    new
                    {
                        Response = response,
                        LoggingRequestId = loggingRequestId,
                        TimeStamp = timestamp
                    }
                );
            }
        }
    }
}