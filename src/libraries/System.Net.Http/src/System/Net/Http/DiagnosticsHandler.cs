// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            return IsGloballyEnabled() && (Activity.Current != null || Settings.s_diagnosticListener.IsEnabled());
        }

        internal static bool IsGloballyEnabled()
        {
            return Settings.s_activityPropagationEnabled;
        }

        // SendAsyncCore returns already completed ValueTask for when async: false is passed.
        // Internally, it calls the synchronous Send method of the base class.
        protected internal override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ValueTask<HttpResponseMessage> sendTask = SendAsyncCore(request, async: false, cancellationToken);
            Debug.Assert(sendTask.IsCompleted);
            return sendTask.GetAwaiter().GetResult();
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            SendAsyncCore(request, async: true, cancellationToken).AsTask();

        private async ValueTask<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, bool async,
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

            Activity? activity = null;
            DiagnosticListener diagnosticListener = Settings.s_diagnosticListener;

            // if there is no listener, but propagation is enabled (with previous IsEnabled() check)
            // do not write any events just start/stop Activity and propagate Ids
            if (!diagnosticListener.IsEnabled())
            {
                activity = new Activity(DiagnosticsHandlerLoggingStrings.ActivityName);
                activity.Start();
                InjectHeaders(activity, request);

                try
                {
                    return async ?
                        await base.SendAsync(request, cancellationToken).ConfigureAwait(false) :
                        base.Send(request, cancellationToken);
                }
                finally
                {
                    activity.Stop();
                }
            }

            Guid loggingRequestId = Guid.Empty;

            // There is a listener. Check if listener wants to be notified about HttpClient Activities
            if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityName, request))
            {
                activity = new Activity(DiagnosticsHandlerLoggingStrings.ActivityName);

                // Only send start event to users who subscribed for it, but start activity anyway
                if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityStartName))
                {
                    diagnosticListener.StartActivity(activity, new ActivityStartData(request));
                }
                else
                {
                    activity.Start();
                }
            }
            // try to write System.Net.Http.Request event (deprecated)
            if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.RequestWriteNameDeprecated))
            {
                long timestamp = Stopwatch.GetTimestamp();
                loggingRequestId = Guid.NewGuid();
                diagnosticListener.Write(DiagnosticsHandlerLoggingStrings.RequestWriteNameDeprecated,
                    new RequestData(request, loggingRequestId, timestamp));
            }

            // If we are on at all, we propagate current activity information
            Activity? currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                InjectHeaders(currentActivity, request);
            }

            HttpResponseMessage? response = null;
            TaskStatus taskStatus = TaskStatus.RanToCompletion;
            try
            {
                response = async ?
                    await base.SendAsync(request, cancellationToken).ConfigureAwait(false) :
                    base.Send(request, cancellationToken);
                return response;
            }
            catch (OperationCanceledException)
            {
                taskStatus = TaskStatus.Canceled;

                // we'll report task status in HttpRequestOut.Stop
                throw;
            }
            catch (Exception ex)
            {
                taskStatus = TaskStatus.Faulted;

                if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ExceptionEventName))
                {
                    // If request was initially instrumented, Activity.Current has all necessary context for logging
                    // Request is passed to provide some context if instrumentation was disabled and to avoid
                    // extensive Activity.Tags usage to tunnel request properties
                    diagnosticListener.Write(DiagnosticsHandlerLoggingStrings.ExceptionEventName, new ExceptionData(ex, request));
                }
                throw;
            }
            finally
            {
                // always stop activity if it was started
                if (activity != null)
                {
                    diagnosticListener.StopActivity(activity, new ActivityStopData(
                        response,
                        // If request is failed or cancelled, there is no response, therefore no information about request;
                        // pass the request in the payload, so consumers can have it in Stop for failed/canceled requests
                        // and not retain all requests in Start
                        request,
                        taskStatus));
                }
                // Try to write System.Net.Http.Response event (deprecated)
                if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ResponseWriteNameDeprecated))
                {
                    long timestamp = Stopwatch.GetTimestamp();
                    diagnosticListener.Write(DiagnosticsHandlerLoggingStrings.ResponseWriteNameDeprecated,
                        new ResponseData(
                            response,
                            loggingRequestId,
                            timestamp,
                            taskStatus));
                }
            }
        }

        #region private

        private sealed class ActivityStartData
        {
            internal ActivityStartData(HttpRequestMessage request)
            {
                Request = request;
            }

            public HttpRequestMessage Request { get; }

            public override string ToString() => $"{{ {nameof(Request)} = {Request} }}";
        }

        private sealed class ActivityStopData
        {
            internal ActivityStopData(HttpResponseMessage? response, HttpRequestMessage request, TaskStatus requestTaskStatus)
            {
                Response = response;
                Request = request;
                RequestTaskStatus = requestTaskStatus;
            }

            public HttpResponseMessage? Response { get; }
            public HttpRequestMessage Request { get; }
            public TaskStatus RequestTaskStatus { get; }

            public override string ToString() => $"{{ {nameof(Response)} = {Response}, {nameof(Request)} = {Request}, {nameof(RequestTaskStatus)} = {RequestTaskStatus} }}";
        }

        private sealed class ExceptionData
        {
            internal ExceptionData(Exception exception, HttpRequestMessage request)
            {
                Exception = exception;
                Request = request;
            }

            public Exception Exception { get; }
            public HttpRequestMessage Request { get; }

            public override string ToString() => $"{{ {nameof(Exception)} = {Exception}, {nameof(Request)} = {Request} }}";
        }

        private sealed class RequestData
        {
            internal RequestData(HttpRequestMessage request, Guid loggingRequestId, long timestamp)
            {
                Request = request;
                LoggingRequestId = loggingRequestId;
                Timestamp = timestamp;
            }

            public HttpRequestMessage Request { get; }
            public Guid LoggingRequestId { get; }
            public long Timestamp { get; }

            public override string ToString() => $"{{ {nameof(Request)} = {Request}, {nameof(LoggingRequestId)} = {LoggingRequestId}, {nameof(Timestamp)} = {Timestamp} }}";
        }

        private sealed class ResponseData
        {
            internal ResponseData(HttpResponseMessage? response, Guid loggingRequestId, long timestamp, TaskStatus requestTaskStatus)
            {
                Response = response;
                LoggingRequestId = loggingRequestId;
                Timestamp = timestamp;
                RequestTaskStatus = requestTaskStatus;
            }

            public HttpResponseMessage? Response { get; }
            public Guid LoggingRequestId { get; }
            public long Timestamp { get; }
            public TaskStatus RequestTaskStatus { get; }

            public override string ToString() => $"{{ {nameof(Response)} = {Response}, {nameof(LoggingRequestId)} = {LoggingRequestId}, {nameof(Timestamp)} = {Timestamp}, {nameof(RequestTaskStatus)} = {RequestTaskStatus} }}";
        }

        private static class Settings
        {
            private const string EnableActivityPropagationEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION";
            private const string EnableActivityPropagationAppCtxSettingName = "System.Net.Http.EnableActivityPropagation";

            public static readonly bool s_activityPropagationEnabled = GetEnableActivityPropagationValue();

            private static bool GetEnableActivityPropagationValue()
            {
                // First check for the AppContext switch, giving it priority over the environment variable.
                if (AppContext.TryGetSwitch(EnableActivityPropagationAppCtxSettingName, out bool enableActivityPropagation))
                {
                    return enableActivityPropagation;
                }

                // AppContext switch wasn't used. Check the environment variable to determine which handler should be used.
                string? envVar = Environment.GetEnvironmentVariable(EnableActivityPropagationEnvironmentVariableSettingName);
                if (envVar != null && (envVar.Equals("false", StringComparison.OrdinalIgnoreCase) || envVar.Equals("0")))
                {
                    // Suppress Activity propagation.
                    return false;
                }

                // Defaults to enabling Activity propagation.
                return true;
            }

            public static readonly DiagnosticListener s_diagnosticListener =
                new DiagnosticListener(DiagnosticsHandlerLoggingStrings.DiagnosticListenerName);
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
            using (IEnumerator<KeyValuePair<string, string?>> e = currentActivity.Baggage.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    var baggage = new List<string>();
                    do
                    {
                        KeyValuePair<string, string?> item = e.Current;
                        baggage.Add(new NameValueHeaderValue(WebUtility.UrlEncode(item.Key), WebUtility.UrlEncode(item.Value)).ToString());
                    }
                    while (e.MoveNext());
                    request.Headers.TryAddWithoutValidation(DiagnosticsHandlerLoggingStrings.CorrelationContextHeaderName, baggage);
                }
            }
        }

        #endregion
    }
}
