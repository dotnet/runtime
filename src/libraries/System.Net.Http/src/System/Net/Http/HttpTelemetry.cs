// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Http
{
    [EventSource(Name = "System.Net.Http")]
    internal sealed class HttpTelemetry : EventSource
    {
        public static readonly HttpTelemetry Log = new HttpTelemetry();

        private IncrementingPollingCounter? _requestsPerSecondCounter;
        private PollingCounter? _startedRequestsCounter;
        private PollingCounter? _currentRequestsCounter;
        private PollingCounter? _abortedRequestsCounter;

        private long _startedRequests;
        private long _currentRequests;
        private long _abortedRequests;

        public static new bool IsEnabled => Log.IsEnabled();

        // NOTE
        // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
        //   enable creating 'activities'.
        //   For more information, take a look at the following blog post:
        //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
        // - A stop event's event id must be next one after its start event.

        [Event(1, Level = EventLevel.Informational)]
        public void RequestStart(string host, int port)
        {
            Interlocked.Increment(ref _startedRequests);
            Interlocked.Increment(ref _currentRequests);
            WriteEvent(1, host, port);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void RequestStop(string host, int port)
        {
            Interlocked.Decrement(ref _currentRequests);
            WriteEvent(2, host, port);
        }

        [Event(3, Level = EventLevel.Error)]
        public void RequestAbort(string host, int port)
        {
            Interlocked.Increment(ref _abortedRequests);
            WriteEvent(3, host, port);
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).
                // They aren't disabled afterwards...

                // The cumulative number of HTTP requests started since the process started.
                _startedRequestsCounter ??= new PollingCounter("requests-started", this, () => Interlocked.Read(ref _startedRequests))
                {
                    DisplayName = "Requests Started",
                };

                // The number of HTTP requests started per second since the process started.
                _requestsPerSecondCounter ??= new IncrementingPollingCounter("requests-started-per-second", this, () => Interlocked.Read(ref _startedRequests))
                {
                    DisplayName = "Requests Started Rate",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                // The cumulative number of HTTP requests aborted since the process started.
                // Aborted means that an exception occurred during the handler's Send(Async) call as a result of a
                // connection related error, timeout, or explicitly cancelled.
                _abortedRequestsCounter ??= new PollingCounter("requests-aborted", this, () => Interlocked.Read(ref _abortedRequests))
                {
                    DisplayName = "Requests Aborted"
                };

                // The number of HTTP requests aborted per second since the process started.
                _requestsPerSecondCounter ??= new IncrementingPollingCounter("requests-aborted-per-second", this, () => Interlocked.Read(ref _abortedRequests))
                {
                    DisplayName = "Requests Aborted Rate",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                // The current number of active HTTP requests that have started but not yet completed or aborted.
                _currentRequestsCounter ??= new PollingCounter("current-requests", this, () => Interlocked.Read(ref _currentRequests))
                {
                    DisplayName = "Current Requests"
                };
            }
        }
    }
}
