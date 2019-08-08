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
            // check if there is a parent Activity (and propagation is not suppressed)
            // or if someone listens to HttpHandlerDiagnosticListener
            return s_enableActivityPropagation && (Activity.Current != null || s_diagnosticListener.IsEnabled());
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // HttpClientHandler is responsible to call static DiagnosticsHandler.IsEnabled() before forwarding request here.
            // It will check if propagation is on (because parent Activity exists or there is a listener) or off (forcibly disabled)
            // This code won't be called unless consumer unsubscribes from DiagnosticListener right after the check.
            // So some requests happening right after subscription starts might not be instrumented. Similarly,
            // when consumer unsubscribes, extra requests might be instrumented

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            Activity activity = null;

            // if there is no listener, but propagation is enabled (with previous IsEnabled() check)
            // do not write any events just start/stop Activity and propagate Ids
            if (!s_diagnosticListener.IsEnabled())
            {
                activity = new Activity(DiagnosticsHandlerLoggingStrings.ActivityName);
                activity.Start();
                InjectHeaders(activity, request);

                try
                {
                    return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    activity.Stop();
                }
            }

            Guid loggingRequestId = Guid.Empty;

            // There is a listener. Check if listener wants to be notified about HttpClient Activities
            if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityName, request))
            {
                activity = new Activity(DiagnosticsHandlerLoggingStrings.ActivityName);

                // Only send start event to users who subscribed for it, but start activity anyway
                if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityStartName))
                {
                    s_diagnosticListener.StartActivity(activity, new { Request = request });
                }
                else
                {
                    activity.Start();
                }
            }
            // try to write System.Net.Http.Request event (deprecated)
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

            // If we are on at all, we propagate current activity information
            Activity currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                InjectHeaders(currentActivity, request);
            }

            Task<HttpResponseMessage> responseTask = null;
            try
            {
                responseTask = base.SendAsync(request, cancellationToken);

                return await responseTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // we'll report task status in HttpRequestOut.Stop
                throw;
            }
            catch (Exception ex)
            {
                if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ExceptionEventName))
                {
                    // If request was initially instrumented, Activity.Current has all necessary context for logging
                    // Request is passed to provide some context if instrumentation was disabled and to avoid
                    // extensive Activity.Tags usage to tunnel request properties
                    s_diagnosticListener.Write(DiagnosticsHandlerLoggingStrings.ExceptionEventName, new { Exception = ex, Request = request });
                }
                throw;
            }
            finally
            {
                // always stop activity if it was started
                if (activity != null)
                {
                    s_diagnosticListener.StopActivity(activity, new
                    {
                        Response = responseTask?.Status == TaskStatus.RanToCompletion ? responseTask.Result : null,
                        // If request is failed or cancelled, there is no response, therefore no information about request;
                        // pass the request in the payload, so consumers can have it in Stop for failed/canceled requests
                        // and not retain all requests in Start
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

        private const string EnableActivityPropagationEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION";
        private const string EnableActivityPropagationAppCtxSettingName = "System.Net.Http.EnableActivityPropagation";

        private static readonly bool s_enableActivityPropagation = GetEnableActivityPropagationValue();

        private static bool GetEnableActivityPropagationValue()
        {
            // First check for the AppContext switch, giving it priority over the environment variable.
            if (AppContext.TryGetSwitch(EnableActivityPropagationAppCtxSettingName, out bool enableActivityPropagation))
            {
                return enableActivityPropagation;
            }

            // AppContext switch wasn't used. Check the environment variable to determine which handler should be used.
            string envVar = Environment.GetEnvironmentVariable(EnableActivityPropagationEnvironmentVariableSettingName);
            if (envVar != null && (envVar.Equals("false", StringComparison.OrdinalIgnoreCase) || envVar.Equals("0")))
            {
                // Suppress Activity propagation.
                return false;
            }

            // Defaults to enabling Activity propagation.
            return true;
        }

        private static void InjectHeaders(Activity currentActivity, HttpRequestMessage request)
        {
            if (currentActivity.IdFormat == ActivityIdFormat.W3C)
            {
                if (!request.Headers.Contains(DiagnosticsHandlerLoggingStrings.TraceParentHeaderName))
                {
                    request.Headers.TryAddWithoutValidation(DiagnosticsHandlerLoggingStrings.TraceParentHeaderName, currentActivity.Id);
                    if (currentActivity.TraceStateString != null)
                    {
                        request.Headers.TryAddWithoutValidation(DiagnosticsHandlerLoggingStrings.TraceStateHeaderName, currentActivity.TraceStateString);
                    }
                }
            }
            else
            {
                if (!request.Headers.Contains(DiagnosticsHandlerLoggingStrings.RequestIdHeaderName))
                {
                    request.Headers.TryAddWithoutValidation(DiagnosticsHandlerLoggingStrings.RequestIdHeaderName, currentActivity.Id);
                }
            }

            // we expect baggage to be empty or contain a few items
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
                    request.Headers.TryAddWithoutValidation(DiagnosticsHandlerLoggingStrings.CorrelationContextHeaderName, baggage);
                }
            }
        }

        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(DiagnosticsHandlerLoggingStrings.DiagnosticListenerName);

        #endregion
    }
}
