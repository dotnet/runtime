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
using Xunit;

namespace BasicEventSourceTests
{
    public partial class TestEventCounter
    {
        [EventSource(Name = "SimpleEventSource")]
        private sealed class SimpleEventSource : EventSource
        {
            private PollingCounter _failureCounter;
            private PollingCounter _successCounter;

            public SimpleEventSource(Func<double> getMockedCount, Func<double> getSuccessCount)
            {
                _failureCounter = new PollingCounter("failureCount", this, getSuccessCount);
                _successCounter = new PollingCounter("successCount", this, getMockedCount);
            }
        }

        internal sealed class SimpleEventListener : EventListener
        {
            private readonly string _targetSourceName;
            private readonly EventLevel _level;
            private Dictionary<string, string> args;
            
            public int FailureEventCount { get; private set; } = 0;
            public int SuccessEventCount { get; private set; } = 0;
            public bool Failed = false;

            public SimpleEventListener(string targetSourceName, EventLevel level)
            {
                // Store the arguments
                _targetSourceName = targetSourceName;
                _level = level;
                args = new Dictionary<string, string>();
                args.Add("EventCounterIntervalSec", "1");
            }
            
            protected override void OnEventSourceCreated(EventSource source)
            {
                if (source.Name.Equals(_targetSourceName))
                {
                    EnableEvents(source, _level, (EventKeywords)(-1), args);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.EventName.Equals("EventCounters"))
                {
                    for (int i = 0; i < eventData.Payload.Count; i++)
                    {

                        // Decode the payload
                        IDictionary<string, object> eventPayload = eventData.Payload[i] as IDictionary<string, object>;

                        string name = "";
                        string min = "";
                        string max = "";
                        string mean = "";
                        string stdev = "";

                        foreach (KeyValuePair<string, object> payload in eventPayload)
                        {
                            if (payload.Key.Equals("Name"))
                            {
                                name = payload.Value.ToString();
                                if (name.Equals("failureCount"))
                                    FailureEventCount++;
                                else if (name.Equals("successCount"))
                                    SuccessEventCount++;
                            }

                            else if (payload.Key.Equals("Min"))
                            {
                                min = payload.Value.ToString();
                            }
                            else if (payload.Key.Equals("Max"))
                            {
                                max = payload.Value.ToString();
                            }
                            else if (payload.Key.Equals("Mean"))
                            {
                                mean = payload.Value.ToString();
                            }
                            else if (payload.Key.Equals("StandardDeviation"))
                            {
                                stdev = payload.Value.ToString();
                            }
                        }

                        // Check if the mean is what we expect it to be
                        if (name.Equals("failureCount"))
                        {
                            if (Int32.Parse(mean) != successCountCalled)  
                            {
                                Console.WriteLine($"Mean is not what we expected: {mean} vs {successCountCalled}");
                                Failed = true;
                            }
                        }
                        else if (name.Equals("successCount"))
                        {
                            if (Int32.Parse(mean) != mockedCountCalled)
                            {
                                Console.WriteLine($"Mean is not what we expected: {mean} vs {mockedCountCalled}");
                            }
                        }

                        // In PollingCounter, min/max/mean should have the same value since we aggregate value only once per counter
                        if (!min.Equals(mean) || !min.Equals(max))
                        {
                            Console.WriteLine("mean/min/max are not equal");
                            Failed = true;
                        }

                        // In PollingCounter, stdev should always be 0 since we aggregate value only once per counter. 
                        if (!stdev.Equals("0"))
                        {
                            Console.WriteLine("standard deviation is not 0");
                            Failed = true;
                        }
                    }
                }
            }
        }


        public static int mockedCountCalled = 0;
        public static int successCountCalled = 0;

        public static double getMockedCount()
        {
            mockedCountCalled++;
            return mockedCountCalled;
        }

        public static double getSuccessCount()
        {
            successCountCalled++;
            return successCountCalled;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            // Create an EventListener.
            using (SimpleEventListener myListener = new SimpleEventListener("SimpleEventSource", EventLevel.Verbose))
            {
                SimpleEventSource eventSource = new SimpleEventSource(getMockedCount, getSuccessCount);

                // Want to sleep for 5000 ms to get some counters piling up.
                Thread.Sleep(5000);

                if (myListener.FailureEventCount > 0 && myListener.SuccessEventCount > 0 && !myListener.Failed && (mockedCountCalled > 0 && successCountCalled > 0))
                {
                    Console.WriteLine("Test Passed");
                    return 100;    
                }
                else
                {
                    Console.WriteLine("Test Failed");
                    return 1;
                }
            }
        }
    }
}
