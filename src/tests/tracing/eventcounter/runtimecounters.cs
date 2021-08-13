// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RuntimeEventCounterTests
{
    public class RuntimeCounterListener : EventListener
    {
        private Dictionary<string, bool> observedEvents = new Dictionary<string, bool>() {
            { "cpu-usage" , false },
            { "working-set", false },
            { "gc-heap-size", false },
            { "gen-0-gc-count", false },
            { "gen-1-gc-count", false },
            { "gen-2-gc-count", false },
            { "threadpool-thread-count", false },
            { "monitor-lock-contention-count", false },
            { "threadpool-queue-length", false },
            { "threadpool-completed-items-count", false },
            { "alloc-rate", false },
            { "active-timer-count", false },
            { "gc-fragmentation", false },
            { "gc-committed", false },
            { "exception-count", false },
            { "time-in-gc", false },
            { "gen-0-size", false },
            { "gen-1-size", false },
            { "gen-2-size", false },
            { "loh-size", false },
            { "poh-size", false },
            { "assembly-count", false },
            { "il-bytes-jitted", false },
            { "methods-jitted-count", false },
            { "time-in-jit", false },
            // These are AppContext switches, not counters
            { "appContextSwitch", false },
            { "appContextData", false }
        };

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("System.Runtime"))
            {
                Dictionary<string, string> refreshInterval = new Dictionary<string, string>();
                refreshInterval.Add("EventCounterIntervalSec", "1");
                EnableEvents(source, EventLevel.Informational, (EventKeywords)(-1), refreshInterval);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // Check AppContext switches
            if (eventData is { EventName: "LogAppContextSwitch",
                               Payload: { Count: 2 } } &&
                eventData.Payload[0] is string switchName &&
                observedEvents.ContainsKey(switchName))
            {
                observedEvents[switchName] = true;
                return;
            }

            // Check counters
            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                IDictionary<string, object> eventPayload = eventData.Payload[i] as IDictionary<string, object>;
                if (eventPayload != null)
                {
                    foreach (KeyValuePair<string, object> payload in eventPayload)
                    {
                        if (payload.Key.Equals("Name"))
                            observedEvents[payload.Value.ToString()] = true;
                    }
                }
            }
        }

        public bool Verify()
        {
            foreach (string counterName in observedEvents.Keys)
            {
                if (!observedEvents[counterName])
                {
                    return false;
                }
                else
                {
                    Console.WriteLine($"Saw {counterName}");
                }
            }
            return true;
        }
    }

    public partial class TestRuntimeEventCounter
    {
        public static int Main(string[] args)
        {
            AppContext.SetSwitch("appContextSwitch", true);
            AppDomain.CurrentDomain.SetData("appContextData", true);

            // Create an EventListener.
            using (RuntimeCounterListener myListener = new RuntimeCounterListener())
            {
                Thread.Sleep(3000);
                if (myListener.Verify())
                {
                    Console.WriteLine("Test passed");
                    return 100;
                }
                else
                {
                    Console.WriteLine($"Test Failed - did not see one or more of the expected runtime counters.");
                    return 1;
                }
            }
        }
    }
}
