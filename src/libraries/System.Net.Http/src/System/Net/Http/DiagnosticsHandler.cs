// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// DiagnosticHandler notifies DiagnosticSource subscribers about outgoing Http requests
    /// </summary>
    internal sealed class DiagnosticsHandler : DelegatingHandler
    {
        /// <summary>
        /// DiagnosticHandler constructor
        /// </summary>
        /// <param name="innerHandler">Inner handler: Windows or Unix implementation of HttpMessageHandler. 
        /// Note that DiagnosticHandler is the latest in the pipeline </param>
        public DiagnosticsHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        internal static bool IsEnabled()
        {
            //check if someone listens to HttpHandlerDiagnosticListener
            return s_diagnosticListener.IsEnabled();
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            //HttpClientHandler is responsible to call DiagnosticsHandler.IsEnabled() before forwarding request here.
            //This code will not be reached if no one listens to 'HttpHandlerDiagnosticListener', unless consumer unsubscribes
            //from DiagnosticListener right after the check. So some requests happening right after subscription starts
            //might not be instrumented. Similarly, when consumer unsubscribes, extra requests might be instrumented

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            Activity activity = null;
            Guid loggingRequestId = Guid.Empty;

            // If System.Net.Http.HttpRequestOut is on see if we should log the start (or just log the activity)
            if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityName, request))
            {
                activity = new Activity(DiagnosticsHandlerLoggingStrings.ActivityName);
                //Only send start event to users who subscribed for it, but start activity anyway
                if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityStartName))
                {
                    s_diagnosticListener.StartActivity(activity, new { Request = request });
                }
                else
                {
                    activity.Start();
                }
            }
            //try to write System.Net.Http.Request event (deprecated)
            if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.RequestWriteNameDeprecated))
            {
                long timestamp = Stopwatch.GetTimestamp();
                loggingRequestId = Guid.NewGuid();
                s_diagnosticListener.Write(DiagnosticsHandlerLoggingStrings.RequestWriteNameDeprecated,
                    new
                    {
                        Request = request,
                        LoggingRequestId = loggingRequestId,
                        Timestamp = timestamp
                    }
                );
            }

            // If we are on at all, we propagate any activity information
            // unless tracing system or user injected Request-Id for backward compatibility reasons.
            Activity currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                if (currentActivity.IdFormat == ActivityIdFormat.W3C)
                {
                    if (!request.Headers.Contains(DiagnosticsHandlerLoggingStrings.TraceParentHeaderName))
                    {
                        request.Headers.Add(DiagnosticsHandlerLoggingStrings.TraceParentHeaderName, currentActivity.Id);
                        if (currentActivity.TraceStateString != null)
                        {
                            request.Headers.Add(DiagnosticsHandlerLoggingStrings.TraceStateHeaderName, currentActivity.TraceStateString);
                        }
                    }
                }
                else
                {
                    if (!request.Headers.Contains(DiagnosticsHandlerLoggingStrings.RequestIdHeaderName))
                    {
                        request.Headers.Add(DiagnosticsHandlerLoggingStrings.RequestIdHeaderName, currentActivity.Id);
                    }
                }

                //we expect baggage to be empty or contain a few items
                using (IEnumerator<KeyValuePair<string, string>> e = currentActivity.Baggage.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        var baggage = new List<string>();
                        do
                        {
                            KeyValuePair<string, string> item = e.Current;
                            baggage.Add(new NameValueHeaderValue(WebUtility.UrlEncode(item.Key), WebUtility.UrlEncode(item.Value)).ToString());
                        }
                        while (e.MoveNext());
                        request.Headers.Add(DiagnosticsHandlerLoggingStrings.CorrelationContextHeaderName, baggage);
                    }
                }
            }

            Task<HttpResponseMessage> responseTask = null;
            try
            {
                responseTask = base.SendAsync(request, cancellationToken);

                return await responseTask.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                //we'll report task status in HttpRequestOut.Stop
                throw;
            }
            catch (Exception ex)
            {
                if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ExceptionEventName))
                {
                    //If request was initially instrumented, Activity.Current has all necessary context for logging
                    //Request is passed to provide some context if instrumentation was disabled and to avoid
                    //extensive Activity.Tags usage to tunnel request properties
                    s_diagnosticListener.Write(DiagnosticsHandlerLoggingStrings.ExceptionEventName, new { Exception = ex, Request = request });
                }
                throw;
            }
            finally
            {
                //always stop activity if it was started
                if (activity != null)
                {
                    s_diagnosticListener.StopActivity(activity, new
                    {
                        Response = responseTask?.Status == TaskStatus.RanToCompletion ? responseTask.Result : null,
                        //If request is failed or cancelled, there is no response, therefore no information about request;
                        //pass the request in the payload, so consumers can have it in Stop for failed/canceled requests
                        //and not retain all requests in Start 
                        Request = request,
                        RequestTaskStatus = responseTask?.Status ?? TaskStatus.Faulted
                    });
                }
                // Try to write System.Net.Http.Response event (deprecated)
                if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ResponseWriteNameDeprecated))
                {
                    long timestamp = Stopwatch.GetTimestamp();
                    s_diagnosticListener.Write(DiagnosticsHandlerLoggingStrings.ResponseWriteNameDeprecated,
                        new
                        {
                            Response = responseTask?.Status == TaskStatus.RanToCompletion ? responseTask.Result : null,
                            LoggingRequestId = loggingRequestId,
                            TimeStamp = timestamp,
                            RequestTaskStatus = responseTask?.Status ?? TaskStatus.Faulted
                        }
                    );
                }
            }
        }

        #region private

        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(DiagnosticsHandlerLoggingStrings.DiagnosticListenerName);

        #endregion
    }
}
