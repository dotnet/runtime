// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

namespace BasicEventSourceTests
{
    public partial class TestEventCounter
    {
        [EventSource(Name = "SimpleEventSource")]
        private sealed class SimpleEventSource : EventSource
        {
            private object _failureCounter;
            private object _successCounter;

            public SimpleEventSource(Func<float> getFailureCount, Func<float> getSuccessCount, Type PollingCounterType)
            {
                _failureCounter = Activator.CreateInstance(PollingCounterType, "failureCount", this, getSuccessCount);
                _successCounter = Activator.CreateInstance(PollingCounterType, "successCount", this, getFailureCount);
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
                            if (Int32.Parse(mean) != failureCountCalled)
                            {
                                Console.WriteLine($"Mean is not what we expected: {mean} vs {failureCountCalled}");
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


        public static int failureCountCalled = 0;
        public static int successCountCalled = 0;

        public static float getFailureCount()
        {
            failureCountCalled++;
            return failureCountCalled;
        }

        public static float getSuccessCount()
        {
            successCountCalled++;
            return successCountCalled;
        }

        public static int Main(string[] args)
        {
            // Create an EventListener.
            using (SimpleEventListener myListener = new SimpleEventListener("SimpleEventSource", EventLevel.Verbose))
            {
                 // Reflect over System.Private.CoreLib and get the PollingCounter type.
                Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
                if(SPC == null)
                {
                    Console.WriteLine("Failed to get System.Private.CoreLib assembly.");
                    return 1;
                }
                Type PollingCounterType = SPC.GetType("System.Diagnostics.Tracing.PollingCounter");
                if(PollingCounterType == null)
                {
                    Console.WriteLine("Failed to get System.Diagnostics.Tracing.PollingCounter type.");
                    return 1;
                }

                SimpleEventSource eventSource = new SimpleEventSource(getFailureCount, getSuccessCount, PollingCounterType);

                // Want to sleep for 5000 ms to get some counters piling up.
                Thread.Sleep(5000);

                if (myListener.FailureEventCount > 0 && myListener.SuccessEventCount > 0 && !myListener.Failed && (failureCountCalled > 0 && successCountCalled > 0))
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