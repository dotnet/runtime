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
    internal sealed class DiagnosticsHandler : HttpMessageHandlerStage
    {
        private static readonly DiagnosticListener s_diagnosticListener = new DiagnosticListener(DiagnosticsHandlerLoggingStrings.DiagnosticListenerName);
        private static readonly ActivitySource s_activitySource = new ActivitySource(DiagnosticsHandlerLoggingStrings.Namespace);

        private readonly HttpMessageHandler _innerHandler;
        private readonly DistributedContextPropagator _propagator;
        private readonly HeaderDescriptor[]? _propagatorFields;

        public DiagnosticsHandler(HttpMessageHandler innerHandler, DistributedContextPropagator propagator, bool autoRedirect = false)
        {
            Debug.Assert(IsGloballyEnabled());
            Debug.Assert(innerHandler is not null && propagator is not null);

            _innerHandler = innerHandler;
            _propagator = propagator;

            // Prepare HeaderDescriptors for fields we need to clear when following redirects
            if (autoRedirect && _propagator.Fields is IReadOnlyCollection<string> fields && fields.Count > 0)
            {
                var fieldDescriptors = new List<HeaderDescriptor>(fields.Count);
                foreach (string field in fields)
                {
                    if (field is not null && HeaderDescriptor.TryGet(field, out HeaderDescriptor descriptor))
                    {
                        fieldDescriptors.Add(descriptor);
                    }
                }
                _propagatorFields = fieldDescriptors.ToArray();
            }
        }

        private static bool IsEnabled()
        {
            // check if there is a parent Activity or if someone listens to "System.Net.Http" ActivitySource or "HttpHandlerDiagnosticListener" DiagnosticListener.
            return Activity.Current != null ||
                   s_activitySource.HasListeners() ||
                   s_diagnosticListener.IsEnabled();
        }

        private static Activity? CreateActivity(HttpRequestMessage requestMessage)
        {
            Activity? activity = null;
            if (s_activitySource.HasListeners())
            {
                activity = s_activitySource.CreateActivity(DiagnosticsHandlerLoggingStrings.ActivityName, ActivityKind.Client);
            }

            if (activity is null)
            {
                if (Activity.Current is not null || s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityName, requestMessage))
                {
                    activity = new Activity(DiagnosticsHandlerLoggingStrings.ActivityName);
                }
            }

            return activity;
        }

        internal static bool IsGloballyEnabled() => GlobalHttpSettings.DiagnosticsHandler.EnableActivityPropagation;

        internal override ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            if (IsEnabled())
            {
                ArgumentNullException.ThrowIfNull(request);
                return SendAsyncCore(request, async, cancellationToken);
            }
            else
            {
                return async ?
                    new ValueTask<HttpResponseMessage>(_innerHandler.SendAsync(request, cancellationToken)) :
                    new ValueTask<HttpResponseMessage>(_innerHandler.Send(request, cancellationToken));
            }
        }

        private async ValueTask<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
        {
            // HttpClientHandler is responsible to call static DiagnosticsHandler.IsEnabled() before forwarding request here.
            // It will check if propagation is on (because parent Activity exists or there is a listener) or off (forcibly disabled)
            // This code won't be called unless consumer unsubscribes from DiagnosticListener right after the check.
            // So some requests happening right after subscription starts might not be instrumented. Similarly,
            // when consumer unsubscribes, extra requests might be instrumented

            // Since we are reusing the request message instance on redirects, clear any existing headers
            // Do so before writing DiagnosticListener events as instrumentations use those to inject headers
            if (request.WasRedirected() && _propagatorFields is HeaderDescriptor[] fields)
            {
                foreach (HeaderDescriptor field in fields)
                {
                    request.Headers.Remove(field);
                }
            }

            DiagnosticListener diagnosticListener = s_diagnosticListener;

            Guid loggingRequestId = Guid.Empty;
            Activity? activity = CreateActivity(request);

            // Start activity anyway if it was created.
            if (activity is not null)
            {
                activity.Start();

                // Only send start event to users who subscribed for it.
                if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityStartName))
                {
                    Write(diagnosticListener, DiagnosticsHandlerLoggingStrings.ActivityStartName, new ActivityStartData(request));
                }
            }

            // Try to write System.Net.Http.Request event (deprecated)
            if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.RequestWriteNameDeprecated))
            {
                long timestamp = Stopwatch.GetTimestamp();
                loggingRequestId = Guid.NewGuid();
                Write(diagnosticListener, DiagnosticsHandlerLoggingStrings.RequestWriteNameDeprecated,
                    new RequestData(
                        request,
                        loggingRequestId,
                        timestamp));
            }

            if (activity is not null)
            {
                InjectHeaders(activity, request);
            }

            HttpResponseMessage? response = null;
            TaskStatus taskStatus = TaskStatus.RanToCompletion;
            try
            {
                response = async ?
                    await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(false) :
                    _innerHandler.Send(request, cancellationToken);
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
                    Write(diagnosticListener, DiagnosticsHandlerLoggingStrings.ExceptionEventName, new ExceptionData(ex, request));
                }
                throw;
            }
            finally
            {
                // Always stop activity if it was started.
                if (activity is not null)
                {
                    activity.SetEndTime(DateTime.UtcNow);

                    // Only send stop event to users who subscribed for it.
                    if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityStopName))
                    {
                        Write(diagnosticListener, DiagnosticsHandlerLoggingStrings.ActivityStopName, new ActivityStopData(response, request, taskStatus));
                    }

                    activity.Stop();
                }

                // Try to write System.Net.Http.Response event (deprecated)
                if (diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ResponseWriteNameDeprecated))
                {
                    long timestamp = Stopwatch.GetTimestamp();
                    Write(diagnosticListener, DiagnosticsHandlerLoggingStrings.ResponseWriteNameDeprecated,
                        new ResponseData(
                            response,
                            loggingRequestId,
                            timestamp,
                            taskStatus));
                }
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

        #region private

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

        private void InjectHeaders(Activity currentActivity, HttpRequestMessage request)
        {
            _propagator.Inject(currentActivity, request, static (carrier, key, value) =>
            {
                if (carrier is HttpRequestMessage request &&
                    key is not null &&
                    HeaderDescriptor.TryGet(key, out HeaderDescriptor descriptor) &&
                    !request.Headers.TryGetHeaderValue(descriptor, out _))
                {
                    request.Headers.TryAddWithoutValidation(descriptor, value);
                }
            });
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
            Justification = "The values being passed into Write have the commonly used properties being preserved with DynamicDependency.")]
        private static void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            DiagnosticSource diagnosticSource,
            string name,
            T value)
        {
            diagnosticSource.Write(name, value);
        }
        #endregion
    }
}
