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

            public SimpleEventSource(Func<float> getFailureCount, Type IncrementingPollingCounterType)
            {
                _failureCounter = Activator.CreateInstance(IncrementingPollingCounterType, "failureCount", this, getFailureCount);    
            }
        }

        internal sealed class SimpleEventListener : EventListener
        {
            private readonly string _targetSourceName;
            private readonly EventLevel _level;
            private Dictionary<string, string> args;
            
            public int FailureEventCount { get; private set; } = 0;
            public bool Failed = false;
            public bool MetadataSet = false;

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
                        string increment = "";

                        foreach (KeyValuePair<string, object> payload in eventPayload)
                        {
                            if (payload.Key.Equals("Name"))
                            {
                                name = payload.Value.ToString();
                            }
                            else if (payload.Key.Equals("Increment"))
                            {
                                increment = payload.Value.ToString();
                            }
                        }

                        // Check if the mean is what we expect it to be
                        if (!increment.Equals("1")) // Increment should always be 1
                        {
                            Console.WriteLine($"Incorrect increment: {increment}.");
                            Failed = true;
                        }
                    }
                }
            }
        }


        public static int failureCountCalled = 0;

        public static float getFailureCount()
        {
            failureCountCalled++;
            return failureCountCalled;
        }
        public static int Main(string[] args)
        {
            // Create an EventListener.
            using (SimpleEventListener myListener = new SimpleEventListener("SimpleEventSource", EventLevel.Verbose))
            {
                 // Reflect over System.Private.CoreLib and get the IncrementingPollingCounter type.
                Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
                if(SPC == null)
                {
                    Console.WriteLine("Failed to get System.Private.CoreLib assembly.");
                    return 1;
                }
                Type IncrementingPollingCounterType = SPC.GetType("System.Diagnostics.Tracing.IncrementingPollingCounter");
                if(IncrementingPollingCounterType == null)
                {
                    Console.WriteLine("Failed to get System.Diagnostics.Tracing.IncrementingPollingCounterType type.");
                    return 1;
                }

                SimpleEventSource eventSource = new SimpleEventSource(getFailureCount, IncrementingPollingCounterType);

                // Want to sleep for 5000 ms to get some counters piling up.
                Thread.Sleep(5000);

                if (!myListener.Failed && failureCountCalled > 0)
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