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
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace TwoCountersTests
{
    public class RuntimeCounterListener : EventListener
    {
        public RuntimeCounterListener(Func<bool> callback)
        {
            _callback = callback;

            observedRuntimeCounters = new Dictionary<string, bool>() {
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
                { "time-in-jit", false }
            };
        }

        private Dictionary<string, bool> observedRuntimeCounters;
        private Func<bool> _callback;

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

            if (eventData.EventName.Equals("EventCounters"))
            {
                _callback();
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

    public partial class TestTwoCounterSessions
    {
        public static int Main(string[] args)
        {
            int myPid = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(myPid);
            List<EventPipeProvider> counterProviders = new List<EventPipeProvider>
            {
                new EventPipeProvider(
                    /* name */      "System.Runtime",
                    /* level */     EventLevel.Informational,
                    /* keywords */  (-1 & (~1 /* RuntimeEventSource.Keywords.AppContext */)),
                    /* arguments */ new Dictionary<string, string>() { { "EventCounterIntervalSec", "1" } })
            };

            using (EventPipeSession outerSession = client.StartEventPipeSession(counterProviders, /* requestRunDown */ false))
            {
                EventPipeEventSource outerSource = new EventPipeEventSource(outerSession.EventStream);
                bool after = false;
                ManualResetEvent outerEventEvent = new ManualResetEvent(false);

                outerSource.Dynamic.All += (TraceEvent traceEvent) =>
                {
                    if (traceEvent.EventName.Equals("EventCounters"))
                    {
                        if (Volatile.Read(ref after))
                        {
                            outerEventEvent.Set();
                        }
                    }
                };

                Thread outerProcessingThread = new Thread(new ThreadStart(() =>
                {
                    outerSource.Process();
                }));
                outerProcessingThread.Start();

                ManualResetEvent innerEventEvent = new ManualResetEvent(false);
                using (RuntimeCounterListener innerListener = new RuntimeCounterListener(() => innerEventEvent.Set()))
                {
                    if (!innerEventEvent.WaitOne(TimeSpan.FromMinutes(3)))
                    {
                        Console.WriteLine("Event listener received no events.");
                        return 1;
                    }

                    if (!innerListener.Verify())
                    {
                        return 1;
                    }
                }

                Thread.Sleep(3000);

                //using (EventPipeSession innerSession = client.StartEventPipeSession(counterProviders, /* requestRunDown */ false))
                //{
                //    EventPipeEventSource innerSource = new EventPipeEventSource(innerSession.EventStream);

                //    ManualResetEvent innerEventEvent = new ManualResetEvent(false);
                //    innerSource.Dynamic.All += (TraceEvent traceEvent) =>
                //    {
                //        if (traceEvent.EventName.Equals("EventCounters"))
                //        {
                //            innerEventEvent.Set();
                //        }
                //    };

                //    Thread innerProcessingThread = new Thread(new ThreadStart(() =>
                //    {
                //        innerSource.Process();
                //    }));
                //    innerProcessingThread.Start();

                //    if (!innerEventEvent.WaitOne(TimeSpan.FromMinutes(3)))
                //    {
                //        Console.WriteLine("Inner session received no events!");
                //        return 1;
                //    }

                //    innerSession.Stop();
                //    innerProcessingThread.Join();
                //}

                Volatile.Write(ref after, true);

                if (!outerEventEvent.WaitOne(TimeSpan.FromMinutes(3)))
                {
                    Console.WriteLine("Session did not receive any events after inner session stopped.");
                    return 2;
                }

                outerSession.Stop();
                outerProcessingThread.Join();

                return 100;

                //// Create an EventListener.
                //using (RuntimeCounterListener myListener = new RuntimeCounterListener())
                //{
                //    using (SecondListener mySecondListener = new SecondListener())
                //    {
                //        Thread.Sleep(1000);
                //        if (!mySecondListener.Verify())
                //        {
                //            return 2;
                //        }
                //    }

                //    Thread.Sleep(1000);

                //    myListener.SetAfterSecondListener();
                //    Thread.Sleep(3000);

                //    if (myListener.Verify())
                //    {
                //        Console.WriteLine("Test passed");
                //        return 100;
                //    }
                //    else
                //    {
                //        Console.WriteLine($"Test Failed - did not see one or more of the expected runtime counters.");
                //        return 1;
                //    }
                //}
            }
        }
    }
}
