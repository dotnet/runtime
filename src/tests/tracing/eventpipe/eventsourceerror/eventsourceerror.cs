// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;

namespace Tracing.Tests.EventSourceError
{
    // This class tries to use IEnumerable<int> events with manifest based
    // EventSource, which will cause an error. The test is validating we see
    // that error over EventPipe.
    class IllegalTypesEventSource : EventSource
    {
        public IllegalTypesEventSource()
        {
        }

        [Event(1, Level = EventLevel.LogAlways)]
        public void SimpleArrayEvent(int[] simpleArray)
        {
           WriteEvent(1, simpleArray);
        }

        [Event(2, Level = EventLevel.LogAlways)]
        public void BasicEvent(int i)
        {
            WriteEvent(2, i);
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            Console.WriteLine($"command={command.Command}");
        }
    }

    public class EventSourceError
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // This test validates that if an EventSource generates an error
            // during construction it gets emitted over EventPipe

            var providers = new List<EventPipeProvider>
            {
                new EventPipeProvider("IllegalTypesEventSource", EventLevel.Verbose)
            };

            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _DoesRundownContainMethodEvents);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "IllegalTypesEventSource", 1 }
        };

        private static Action _eventGeneratingAction = () =>
        {
            // Constructing the EventSource should generate the error message
            IllegalTypesEventSource eventSource = new IllegalTypesEventSource();

            // This will be a no-op since the EventSource failed to construct
            eventSource.SimpleArrayEvent(new int[] { 12 });
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesRundownContainMethodEvents = (source) =>
        {
            int eventCount = 0;
            bool sawEvent = false;
            source.Dynamic.All += (TraceEvent traceEvent) =>
            {
                if (traceEvent.ProviderName == "SentinelEventSource"
                    || traceEvent.ProviderName == "Microsoft-Windows-DotNETRuntime"
                    || traceEvent.ProviderName == "Microsoft-Windows-DotNETRuntimeRundown"
                    || traceEvent.ProviderName == "Microsoft-DotNETCore-EventPipe")
                {
                    return;
                }

                ++eventCount;

                if (traceEvent.ProviderName == "IllegalTypesEventSource"
                    && traceEvent.EventName == "EventSourceMessage"
                    && traceEvent.FormattedMessage.StartsWith("ERROR: Exception in Command Processing for EventSource IllegalTypesEventSource", StringComparison.OrdinalIgnoreCase))
                {
                    sawEvent = true;
                }
                else
                {
                    Console.WriteLine($"Saw unexpected event {traceEvent}");
                }
            };

            return () => ((eventCount == 1) && sawEvent) ? 100 : -1;
        };
    }
}
