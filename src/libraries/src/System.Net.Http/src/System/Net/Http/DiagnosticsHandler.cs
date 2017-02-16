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

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            //HttpClientHandler is responsible to call DiagnosticsHandler.IsEnabled() before forwarding request here.
            //This code will not be reached if no one listens to 'HttpHandlerDiagnosticListener', unless consumer unsubscribes
            //from DiagnosticListener right after the check. So some requests happening right after subscription starts
            //might not be instrumented. Similarly, when consumer unsubscribes, extra requests might be instumented

            Task<HttpResponseMessage> responseTask;

            //We need to know if activity events are enabled and send Request/Response events otherwise
            if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityName))
            {
                if (Activity.Current != null)
                {
                    responseTask = SendInstrumentedAsync(request, cancellationToken);
                }
                else
                {
                    //null Activity.Current means that incoming request was not instrumented
                    //and there is nothing we can do for outgoing request. 
                    //Activity events are enabled so we don't send Request/Response events
                    responseTask = base.SendAsync(request, cancellationToken);
                }
            }
            else 
            {
                Guid loggingRequestId = Guid.NewGuid();
                LogHttpRequest(request, loggingRequestId);
                responseTask = base.SendAsync(request, cancellationToken);
                LogHttpResponse(responseTask, loggingRequestId);
            }
            return responseTask;
        }

        #region private

        private async Task<HttpResponseMessage> SendInstrumentedAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Activity activity = null;
            //check if user wants THIS request to be instrumented
            if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityName, request))
            {
                activity = new Activity(DiagnosticsHandlerLoggingStrings.ActivityName);

                s_diagnosticListener.StartActivity(activity, new { Request = request });

                //inject headers unless user does not want it to happen
                //we may pass request to IsEnabled, but not clear how useful it is
                if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityPropagateName))
                {
                    request.Headers.Add(HttpKnownHeaderNames.RequestId, activity.Id);
                    
                    //we expect baggage to be empty or contain a few items
                    bool baggageIsEmpty = true;
                    foreach (var _ in activity.Baggage)
                    {
                        baggageIsEmpty = false;
                        break;
                    }

                    if (!baggageIsEmpty)
                    {
                        List<string> baggage = new List<string>();
                        foreach (KeyValuePair<string, string> baggageItem in activity.Baggage)
                        {
                            baggage.Add(new NameValueHeaderValue(baggageItem.Key, baggageItem.Value).ToString());
                        }
                        request.Headers.Add(HttpKnownHeaderNames.CorrelationContext, baggage);
                    }
                }
            }

            Task<HttpResponseMessage> responseTask = base.SendAsync(request, cancellationToken);
            try
            {
                await responseTask.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                //we'll report task status in Activity.Stop
                throw;
            }
            catch (Exception ex)
            {
                //If request was initialy instrumented, Activity.Current has all necessary context for logging
                //If user decided to NOT instrument this request AND it threw an exception then:
                //Activity.Current represents 'parent' Activity (presumably incoming request)
                //So we let user log it as exception happened in this 'parent' activity
                if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ExceptionEventName))
                {
                    s_diagnosticListener.Write(DiagnosticsHandlerLoggingStrings.ExceptionEventName, new {Exception = ex});
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
                        Response = responseTask.Status == TaskStatus.RanToCompletion ? responseTask.Result : null,
                        RequestTaskStatus = responseTask.Status
                    });
                }
            }
            return responseTask.Result;
        }

        private static void LogHttpRequest(HttpRequestMessage request, Guid loggingRequestId)
        {
            if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.RequestWriteName))
            {
                long timestamp = Stopwatch.GetTimestamp();

                s_diagnosticListener.Write(
                    DiagnosticsHandlerLoggingStrings.RequestWriteName,
                    new
                    {
                        Request = request,
                        LoggingRequestId = loggingRequestId,
                        Timestamp = timestamp
                    }
                );
            }
        }

        private static void LogHttpResponse(Task<HttpResponseMessage> responseTask, Guid loggingRequestId)
        {
            responseTask.ContinueWith(
                (t, s) =>
                {
                    long timestamp = Stopwatch.GetTimestamp();

                    if (t.IsFaulted && s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ExceptionEventName))
                    {
                        s_diagnosticListener.Write(
                            DiagnosticsHandlerLoggingStrings.ExceptionEventName,
                            new
                            {
                                LoggingRequestId = (Guid) s,
                                Timestamp = timestamp,
                                Exception = t.Exception,
                            }
                        );
                    }

                    if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ResponseWriteName))
                    {
                        s_diagnosticListener.Write(
                            DiagnosticsHandlerLoggingStrings.ResponseWriteName,
                            new
                            {
                                Response = t.Status == TaskStatus.RanToCompletion ? t.Result : null,
                                LoggingRequestId = (Guid) s,
                                TimeStamp = timestamp,
                                RequestTaskStatus = t.Status
                            }
                        );
                    }
                }, loggingRequestId, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }

        private static readonly DiagnosticListener s_diagnosticListener =
            new DiagnosticListener(DiagnosticsHandlerLoggingStrings.DiagnosticListenerName);

        #endregion
    }
}
