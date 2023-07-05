// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Net.Http
{
    [EventSource(Name = "System.Net.Http")]
    internal sealed partial class HttpTelemetry : EventSource
    {
        public static readonly HttpTelemetry Log = new HttpTelemetry();

        public static class Keywords
        {
            public const EventKeywords RequestFailedDetailed = (EventKeywords)1;
        }

        private long _startedRequests;
        private long _stoppedRequests;
        private long _failedRequests;

        private long _openedHttp11Connections;
        private long _openedHttp20Connections;
        private long _openedHttp30Connections;

        // NOTE
        // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
        //   enable creating 'activities'.
        //   For more information, take a look at the following blog post:
        //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
        // - A stop event's event id must be next one after its start event.

        [Event(1, Level = EventLevel.Informational)]
        private void RequestStart(string scheme, string host, int port, string pathAndQuery, byte versionMajor, byte versionMinor, HttpVersionPolicy versionPolicy)
        {
            Interlocked.Increment(ref _startedRequests);
            WriteEvent(eventId: 1, scheme, host, port, pathAndQuery, versionMajor, versionMinor, versionPolicy);
        }

        [NonEvent]
        public void RequestStart(HttpRequestMessage request)
        {
            Debug.Assert(request.RequestUri != null && request.RequestUri.IsAbsoluteUri);

            RequestStart(
                request.RequestUri.Scheme,
                request.RequestUri.IdnHost,
                request.RequestUri.Port,
                request.RequestUri.PathAndQuery,
                (byte)request.Version.Major,
                (byte)request.Version.Minor,
                request.VersionPolicy);
        }

        [NonEvent]
        public void RequestStop(HttpResponseMessage? response)
        {
            RequestStop(response is null ? -1 : (int)response.StatusCode);
        }

        [Event(2, Level = EventLevel.Informational, Version = 1)]
        private void RequestStop(int statusCode)
        {
            Interlocked.Increment(ref _stoppedRequests);
            WriteEvent(eventId: 2, statusCode);
        }

        [NonEvent]
        public void RequestFailed(Exception exception)
        {
            Interlocked.Increment(ref _failedRequests);

            if (IsEnabled(EventLevel.Error, EventKeywords.None))
            {
                RequestFailed(exceptionMessage: exception.Message);

                if (IsEnabled(EventLevel.Error, Keywords.RequestFailedDetailed))
                {
                    RequestFailedDetailed(exception: exception.ToString());
                }
            }
        }

        [Event(3, Level = EventLevel.Error, Version = 1)]
        private void RequestFailed(string exceptionMessage)
        {
            WriteEvent(eventId: 3, exceptionMessage);
        }

        [Event(4, Level = EventLevel.Informational)]
        private void ConnectionEstablished(byte versionMajor, byte versionMinor)
        {
            WriteEvent(eventId: 4, versionMajor, versionMinor);
        }

        [Event(5, Level = EventLevel.Informational)]
        private void ConnectionClosed(byte versionMajor, byte versionMinor)
        {
            WriteEvent(eventId: 5, versionMajor, versionMinor);
        }

        [Event(6, Level = EventLevel.Informational)]
        private void RequestLeftQueue(double timeOnQueueMilliseconds, byte versionMajor, byte versionMinor)
        {
            WriteEvent(eventId: 6, timeOnQueueMilliseconds, versionMajor, versionMinor);
        }

        [Event(7, Level = EventLevel.Informational)]
        public void RequestHeadersStart()
        {
            WriteEvent(eventId: 7);
        }

        [Event(8, Level = EventLevel.Informational)]
        public void RequestHeadersStop()
        {
            WriteEvent(eventId: 8);
        }

        [Event(9, Level = EventLevel.Informational)]
        public void RequestContentStart()
        {
            WriteEvent(eventId: 9);
        }

        [Event(10, Level = EventLevel.Informational)]
        public void RequestContentStop(long contentLength)
        {
            WriteEvent(eventId: 10, contentLength);
        }

        [Event(11, Level = EventLevel.Informational)]
        public void ResponseHeadersStart()
        {
            WriteEvent(eventId: 11);
        }

        [Event(12, Level = EventLevel.Informational, Version = 1)]
        public void ResponseHeadersStop(int statusCode)
        {
            WriteEvent(eventId: 12, statusCode);
        }

        [Event(13, Level = EventLevel.Informational)]
        public void ResponseContentStart()
        {
            WriteEvent(eventId: 13);
        }

        [Event(14, Level = EventLevel.Informational)]
        public void ResponseContentStop()
        {
            WriteEvent(eventId: 14);
        }

        [Event(15, Level = EventLevel.Error, Keywords = Keywords.RequestFailedDetailed)]
        private void RequestFailedDetailed(string exception)
        {
            WriteEvent(eventId: 15, exception);
        }

        [NonEvent]
        public void Http11ConnectionEstablished()
        {
            Interlocked.Increment(ref _openedHttp11Connections);
            ConnectionEstablished(versionMajor: 1, versionMinor: 1);
        }

        [NonEvent]
        public void Http11ConnectionClosed()
        {
            long count = Interlocked.Decrement(ref _openedHttp11Connections);
            Debug.Assert(count >= 0);
            ConnectionClosed(versionMajor: 1, versionMinor: 1);
        }

        [NonEvent]
        public void Http20ConnectionEstablished()
        {
            Interlocked.Increment(ref _openedHttp20Connections);
            ConnectionEstablished(versionMajor: 2, versionMinor: 0);
        }

        [NonEvent]
        public void Http20ConnectionClosed()
        {
            long count = Interlocked.Decrement(ref _openedHttp20Connections);
            Debug.Assert(count >= 0);
            ConnectionClosed(versionMajor: 2, versionMinor: 0);
        }

        [NonEvent]
        public void Http30ConnectionEstablished()
        {
            Interlocked.Increment(ref _openedHttp30Connections);
            ConnectionEstablished(versionMajor: 3, versionMinor: 0);
        }

        [NonEvent]
        public void Http30ConnectionClosed()
        {
            long count = Interlocked.Decrement(ref _openedHttp30Connections);
            Debug.Assert(count >= 0);
            ConnectionClosed(versionMajor: 3, versionMinor: 0);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Parameters to this method are primitive and are trimmer safe")]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, string? arg2, int arg3, string? arg4, byte arg5, byte arg6, HttpVersionPolicy arg7)
        {
            arg1 ??= "";
            arg2 ??= "";
            arg4 ??= "";

            fixed (char* arg1Ptr = arg1)
            fixed (char* arg2Ptr = arg2)
            fixed (char* arg4Ptr = arg4)
            {
                const int NumEventDatas = 7;
                EventData* descrs = stackalloc EventData[NumEventDatas];

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
                    Size = sizeof(byte)
                };
                descrs[5] = new EventData
                {
                    DataPointer = (IntPtr)(&arg6),
                    Size = sizeof(byte)
                };
                descrs[6] = new EventData
                {
                    DataPointer = (IntPtr)(&arg7),
                    Size = sizeof(HttpVersionPolicy)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Parameters to this method are primitive and are trimmer safe")]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, double arg1, byte arg2, byte arg3)
        {
            const int NumEventDatas = 3;
            EventData* descrs = stackalloc EventData[NumEventDatas];

            descrs[0] = new EventData
            {
                DataPointer = (IntPtr)(&arg1),
                Size = sizeof(double)
            };
            descrs[1] = new EventData
            {
                DataPointer = (IntPtr)(&arg2),
                Size = sizeof(byte)
            };
            descrs[2] = new EventData
            {
                DataPointer = (IntPtr)(&arg3),
                Size = sizeof(byte)
            };

            WriteEventCore(eventId, NumEventDatas, descrs);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Parameters to this method are primitive and are trimmer safe")]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, byte arg1, byte arg2)
        {
            const int NumEventDatas = 2;
            EventData* descrs = stackalloc EventData[NumEventDatas];

            descrs[0] = new EventData
            {
                DataPointer = (IntPtr)(&arg1),
                Size = sizeof(byte)
            };
            descrs[1] = new EventData
            {
                DataPointer = (IntPtr)(&arg2),
                Size = sizeof(byte)
            };

            WriteEventCore(eventId, NumEventDatas, descrs);
        }
    }
}
