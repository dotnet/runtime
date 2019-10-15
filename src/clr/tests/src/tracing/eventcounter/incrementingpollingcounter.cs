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

namespace BasicEventSourceTests
{
    public partial class TestEventCounter
    {
        [EventSource(Name = "SimpleEventSource")]
        private sealed class SimpleEventSource : EventSource
        {
            private IncrementingPollingCounter _mockedCounter;

            public SimpleEventSource(Func<double> getMockedCount)
            {
                _mockedCounter = new IncrementingPollingCounter("failureCount", this, getMockedCount) { DisplayName = "Failure Count", DisplayUnits = "Count", DisplayRateTimeScale = new TimeSpan(0, 0, 1) };
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
            public string displayName = "";
            public string displayRateTimeScale = "";
            public string displayUnits = "";

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
                            else if (payload.Key.Equals("DisplayRateTimeScale"))
                            {
                                displayRateTimeScale = payload.Value.ToString();
                            }
                            else if (payload.Key.Equals("DisplayName"))
                            {
                                displayName = payload.Value.ToString();
                            }
                            else if (payload.Key.Equals("DisplayUnits"))
                            {
                                displayUnits = payload.Value.ToString();
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


        public static int mockedCountCalled = 0;

        public static double getMockedCount()
        {
            mockedCountCalled++;
            return mockedCountCalled;
        }
        public static int Main(string[] args)
        {
            // Create an EventListener.
            using (SimpleEventListener myListener = new SimpleEventListener("SimpleEventSource", EventLevel.Verbose))
            {
                SimpleEventSource eventSource = new SimpleEventSource(getMockedCount);

                // Want to sleep for 5000 ms to get some counters piling up.
                Thread.Sleep(5000);

                if (myListener.Failed || mockedCountCalled <= 0)
                {
                    Console.WriteLine($"Test Failed - mockedCountCalled = {mockedCountCalled}, myListener.Failed = {myListener.Failed}");
                    return 1;    
                }
                
                if (myListener.displayRateTimeScale != "00:00:01")
                {
                    Console.WriteLine($"Test Failed - Incorrect DisplayRateTimeScale in payload: {myListener.displayRateTimeScale}");
                    return 1;
                }
                
                if (myListener.displayName != "Failure Count")
                {
                    Console.WriteLine($"Test Failed - Incorrect DisplayName in payload: {myListener.displayName}");
                    return 1;
                }

                if (myListener.displayUnits != "Count")
                {
                    Console.WriteLine($"Test failed - Incorrect DisplayUnits in payload: {myListener.displayUnits}");
                    return 1;
                }
                
                Console.WriteLine("Test passed");
                return 100;
            }
        }
    }
}