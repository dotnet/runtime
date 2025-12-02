// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RuntimeEventCounterTests
{
    public class RuntimeCounterListener : EventListener
    {
        public RuntimeCounterListener()
        {
            observedRuntimeCounters = new ConcurrentDictionary<string, bool>();
            foreach (string counter in new string[] {
                "cpu-usage",
                "working-set",
                "gc-heap-size",
                "gen-0-gc-count",
                "gen-1-gc-count",
                "gen-2-gc-count",
                "threadpool-thread-count",
                "monitor-lock-contention-count",
                "threadpool-queue-length",
                "threadpool-completed-items-count",
                "alloc-rate",
                "active-timer-count",
                "gc-fragmentation",
                "gc-committed",
                "exception-count",
                "time-in-gc",
                "gen-0-size",
                "gen-1-size",
                "gen-2-size",
                "loh-size",
                "poh-size",
                "assembly-count",
                "il-bytes-jitted",
                "methods-jitted-count",
                "time-in-jit" })
            {
                observedRuntimeCounters[counter] = false;
            }
        }
        private ConcurrentDictionary<string, bool> observedRuntimeCounters;

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("System.Runtime"))
            {
                Dictionary<string, string> refreshInterval = new Dictionary<string, string>();
                refreshInterval.Add("EventCounterIntervalSec", "1");
                EnableEvents(source, EventLevel.Informational,
                    (EventKeywords)(-1 & (~1 /* RuntimeEventSource.Keywords.AppContext */)),
                    refreshInterval);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                IDictionary<string, object> eventPayload = eventData.Payload[i] as IDictionary<string, object>;
                if (eventPayload != null)
                {
                    foreach (KeyValuePair<string, object> payload in eventPayload)
                    {
                        if (payload.Key.Equals("Name"))
                            observedRuntimeCounters[payload.Value.ToString()] = true;
                    }
                }
            }
        }

        public bool Verify()
        {
            foreach (string counterName in observedRuntimeCounters.Keys)
            {
                if (!observedRuntimeCounters[counterName])
                {
                    Console.WriteLine($"Did not see {counterName}");
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
        [Fact]
        public static int TestEntryPoint()
        {
            // Create an EventListener.
            using (RuntimeCounterListener myListener = new RuntimeCounterListener())
            {
                // Wait max 60 seconds
                for (int i = 0; i < 60; i++)
                {
                    Thread.Sleep(1000);
                    if (myListener.Verify())
                    {
                        Console.WriteLine("Test passed");
                        return 100;
                    }
                }

                Console.WriteLine($"Test Failed - did not see one or more of the expected runtime counters.");
                return 1;
            }
        }
    }
}
