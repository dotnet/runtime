// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private IncrementingPollingCounter? _startedRequestsPerSecondCounter;
        private IncrementingPollingCounter? _abortedRequestsPerSecondCounter;
        private PollingCounter? _startedRequestsCounter;
        private PollingCounter? _currentRequestsCounter;
        private PollingCounter? _abortedRequestsCounter;

        private long _startedRequests;
        private long _stoppedRequests;
        private long _abortedRequests;

        // NOTE
        // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
        //   enable creating 'activities'.
        //   For more information, take a look at the following blog post:
        //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
        // - A stop event's event id must be next one after its start event.

        [Event(1, Level = EventLevel.Informational)]
        public void RequestStart(string scheme, string host, int port, string pathAndQuery, int versionMajor, int versionMinor)
        {
            Interlocked.Increment(ref _startedRequests);
            WriteEvent(eventId: 1, scheme, host, port, pathAndQuery, versionMajor, versionMinor);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void RequestStop()
        {
            Interlocked.Increment(ref _stoppedRequests);
            WriteEvent(eventId: 2);
        }

        [Event(3, Level = EventLevel.Error)]
        public void RequestAborted()
        {
            Interlocked.Increment(ref _abortedRequests);
            WriteEvent(eventId: 3);
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
                _startedRequestsPerSecondCounter ??= new IncrementingPollingCounter("requests-started-rate", this, () => Interlocked.Read(ref _startedRequests))
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
                _abortedRequestsPerSecondCounter ??= new IncrementingPollingCounter("requests-aborted-rate", this, () => Interlocked.Read(ref _abortedRequests))
                {
                    DisplayName = "Requests Aborted Rate",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                // The current number of active HTTP requests that have started but not yet completed or aborted.
                // Use (-_stoppedRequests + _startedRequests) to avoid returning a negative value if _stoppedRequests is
                // incremented after reading _startedRequests due to race conditions with completing the HTTP request.
                _currentRequestsCounter ??= new PollingCounter("current-requests", this, () => -Interlocked.Read(ref _stoppedRequests) + Interlocked.Read(ref _startedRequests))
                {
                    DisplayName = "Current Requests"
                };
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, string? arg2, int arg3, string? arg4, int arg5, int arg6)
        {
            if (IsEnabled())
            {
                if (arg1 == null) arg1 = "";
                if (arg2 == null) arg2 = "";
                if (arg4 == null) arg4 = "";

                fixed (char* arg1Ptr = arg1)
                fixed (char* arg2Ptr = arg2)
                fixed (char* arg4Ptr = arg4)
                {
                    const int NumEventDatas = 6;
                    var descrs = stackalloc EventData[NumEventDatas];

                    descrs[0] = new EventData
                    {
                        DataPointer = (IntPtr)(arg1Ptr),
                        Size = (arg1.Length + 1) * sizeof(char)
                    };
                    descrs[1] = new EventData
                    {
                        DataPointer = (IntPtr)(arg2Ptr),
                        Size = (arg2.Length + 1) * sizeof(char)
                    };
                    descrs[2] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg3),
                        Size = sizeof(int)
                    };
                    descrs[3] = new EventData
                    {
                        DataPointer = (IntPtr)(arg4Ptr),
                        Size = (arg4.Length + 1) * sizeof(char)
                    };
                    descrs[4] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg5),
                        Size = sizeof(int)
                    };
                    descrs[5] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg6),
                        Size = sizeof(int)
                    };

                    WriteEventCore(eventId, NumEventDatas, descrs);
                }
            }
        }
    }
}
