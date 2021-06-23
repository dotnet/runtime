// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private const string Namespace                      = "System.Net.Http";
        private const string RequestWriteNameDeprecated     = Namespace + ".Request";
        private const string ResponseWriteNameDeprecated    = Namespace + ".Response";
        private const string ExceptionEventName             = Namespace + ".Exception";
        private const string ActivityName                   = Namespace + ".HttpRequestOut";
        private const string ActivityStartName              = ActivityName + ".Start";
        private const string ActivityStopName               = ActivityName + ".Stop";

        private static readonly DiagnosticListener s_diagnosticListener = new("HttpHandlerDiagnosticListener");
        private static readonly ActivitySource s_activitySource = new(Namespace);

        public static bool IsGloballyEnabled { get; } = GetEnableActivityPropagationValue();

        private static bool GetEnableActivityPropagationValue()
        {
            // First check for the AppContext switch, giving it priority over the environment variable.
            if (AppContext.TryGetSwitch(Namespace + ".EnableActivityPropagation", out bool enableActivityPropagation))
            {
                return enableActivityPropagation;
            }

            // AppContext switch wasn't used. Check the environment variable to determine which handler should be used.
            string? envVar = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION");
            if (envVar != null && (envVar.Equals("false", StringComparison.OrdinalIgnoreCase) || envVar.Equals("0")))
            {
                // Suppress Activity propagation.
                return false;
            }

            // Defaults to enabling Activity propagation.
            return true;
        }

        public DiagnosticsHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            Debug.Assert(IsGloballyEnabled);
        }

        private static bool ShouldLogDiagnostics(HttpRequestMessage request, out Activity? activity)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request), SR.net_http_handler_norequest);
            }

            activity = null;

            if (s_activitySource.HasListeners())
            {
                activity = s_activitySource.CreateActivity(ActivityName, ActivityKind.Client);
            }

            if (activity is null)
            {
                bool diagnosticListenerEnabled = s_diagnosticListener.IsEnabled();

                if (Activity.Current is not null || (diagnosticListenerEnabled && s_diagnosticListener.IsEnabled(ActivityName, request)))
                {
                    // If a diagnostics listener is enabled for the Activity, always create one
                    activity = new Activity(ActivityName);
                }
                else
                {
                    // There is no Activity, but we may still want to use the instrumented SendAsyncCore if diagnostic listeners are interested in other events
                    return diagnosticListenerEnabled;
                }
            }

            activity.Start();

            if (s_diagnosticListener.IsEnabled(ActivityStartName))
            {
                Write(ActivityStartName, new ActivityStartData(request));
            }

            InjectHeaders(activity, request);

            return true;
        }

        protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ShouldLogDiagnostics(request, out Activity? activity))
            {
                ValueTask<HttpResponseMessage> sendTask = SendAsyncCore(request, activity, async: false, cancellationToken);
                return sendTask.IsCompleted ?
                    sendTask.Result :
                    sendTask.AsTask().GetAwaiter().GetResult();
            }
            else
            {
                return base.Send(request, cancellationToken);
            }
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ShouldLogDiagnostics(request, out Activity? activity))
            {
                return SendAsyncCore(request, activity, async: true, cancellationToken).AsTask();
            }
            else
            {
                return base.SendAsync(request, cancellationToken);
            }
        }

        private async ValueTask<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, Activity? activity, bool async, CancellationToken cancellationToken)
        {
            Guid loggingRequestId = default;

            if (s_diagnosticListener.IsEnabled(RequestWriteNameDeprecated))
            {
                loggingRequestId = Guid.NewGuid();
                Write(RequestWriteNameDeprecated, new RequestData(request, loggingRequestId, Stopwatch.GetTimestamp()));
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
                throw;
            }
            catch (Exception ex)
            {
                if (s_diagnosticListener.IsEnabled(ExceptionEventName))
                {
                    Write(ExceptionEventName, new ExceptionData(ex, request));
                }

                taskStatus = TaskStatus.Faulted;
                throw;
            }
            finally
            {
                if (activity is not null)
                {
                    activity.SetEndTime(DateTime.UtcNow);

                    if (s_diagnosticListener.IsEnabled(ActivityStopName))
                    {
                        Write(ActivityStopName, new ActivityStopData(response, request, taskStatus));
                    }

                    activity.Stop();
                }

                if (s_diagnosticListener.IsEnabled(ResponseWriteNameDeprecated))
                {
                    Write(ResponseWriteNameDeprecated, new ResponseData(response, loggingRequestId, Stopwatch.GetTimestamp(), taskStatus));
                }
            }
        }

        private sealed class ActivityStartData
        {
            // matches the properties selected in https://github.com/dotnet/diagnostics/blob/ffd0254da3bcc47847b1183fa5453c0877020abd/src/Microsoft.Diagnostics.Monitoring.EventPipe/Configuration/HttpRequestSourceConfiguration.cs#L36-L40
            [DynamicDependency(nameof(HttpRequestMessage.RequestUri), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(HttpRequestMessage.Method), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(HttpRequestMessage.RequestUri), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(Uri.Host), typeof(Uri))]
            [DynamicDependency(nameof(Uri.Port), typeof(Uri))]
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
            // preserve the same properties as ActivityStartData above + common Exception properties
            [DynamicDependency(nameof(HttpRequestMessage.RequestUri), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(HttpRequestMessage.Method), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(HttpRequestMessage.RequestUri), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(Uri.Host), typeof(Uri))]
            [DynamicDependency(nameof(Uri.Port), typeof(Uri))]
            [DynamicDependency(nameof(System.Exception.Message), typeof(Exception))]
            [DynamicDependency(nameof(System.Exception.StackTrace), typeof(Exception))]
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
            // preserve the same properties as ActivityStartData above
            [DynamicDependency(nameof(HttpRequestMessage.RequestUri), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(HttpRequestMessage.Method), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(HttpRequestMessage.RequestUri), typeof(HttpRequestMessage))]
            [DynamicDependency(nameof(Uri.Host), typeof(Uri))]
            [DynamicDependency(nameof(Uri.Port), typeof(Uri))]
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
            [DynamicDependency(nameof(HttpResponseMessage.StatusCode), typeof(HttpResponseMessage))]
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

        private static void InjectHeaders(Activity currentActivity, HttpRequestMessage request)
        {
            const string TraceParentHeaderName = "traceparent";
            const string TraceStateHeaderName = "tracestate";
            const string RequestIdHeaderName = "Request-Id";
            const string CorrelationContextHeaderName = "Correlation-Context";

            if (currentActivity.IdFormat == ActivityIdFormat.W3C)
            {
                if (!request.Headers.Contains(TraceParentHeaderName))
                {
                    request.Headers.TryAddWithoutValidation(TraceParentHeaderName, currentActivity.Id);
                    if (currentActivity.TraceStateString != null)
                    {
                        request.Headers.TryAddWithoutValidation(TraceStateHeaderName, currentActivity.TraceStateString);
                    }
                }
            }
            else
            {
                if (!request.Headers.Contains(RequestIdHeaderName))
                {
                    request.Headers.TryAddWithoutValidation(RequestIdHeaderName, currentActivity.Id);
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
                    request.Headers.TryAddWithoutValidation(CorrelationContextHeaderName, baggage);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
            Justification = "The values being passed into Write have the commonly used properties being preserved with DynamicDependency.")]
        private static void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string name, T value)
        {
            s_diagnosticListener.Write(name, value);
        }
    }
}
