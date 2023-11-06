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
            private EventCounter _myCounter;

            public SimpleEventSource(string _displayName, string _displayUnits)
            {
                _myCounter = new EventCounter("test-counter", this) { DisplayName = _displayName, DisplayUnits = _displayUnits };
            }

            public void WriteOne()
            {
                _myCounter.WriteMetric(1);
            }
        }

        internal sealed class SimpleEventListener : EventListener
        {
            private readonly string _targetSourceName;
            private readonly EventLevel _level;
            private Dictionary<string, string> args;
            
            public HashSet<int> means;
            public string displayName;
            public string displayUnits;
            public int callbackCount;
            public ManualResetEvent sawEvent;
            
            public SimpleEventListener(string targetSourceName, EventLevel level)
            {
                // Store the arguments
                _targetSourceName = targetSourceName;
                _level = level;
                displayName = "";
                displayUnits = "";
                callbackCount = 0;
                means = new HashSet<int>(); 
                args = new Dictionary<string, string>();
                args.Add("EventCounterIntervalSec", "1");
                sawEvent = new ManualResetEvent(false);
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
                        foreach (KeyValuePair<string, object> payload in eventPayload)
                        {
                            if (payload.Key.Equals("Mean"))
                            {
                                int mean = Int32.Parse(payload.Value.ToString());
                                Console.WriteLine("Adding " + mean + " to known list of means");
                                means.Add(mean);
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
                    }
                    sawEvent.Set();
                    callbackCount++;
                }
            }

            public bool validateMean()
            {
                // we expect to see 1 because we wrote only 1s
                // we *might* also see 0 because there is a period of time we didn't write stuff and got callback
                if (!means.Contains(1)) 
                {
                    Console.WriteLine("Mean doesn't have a 1");
                    return false;
                }
                return true;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {

            // Create an EventListener.
            using (SimpleEventListener myListener = new SimpleEventListener("SimpleEventSource", EventLevel.Verbose))
            {
                string displayName = "Mock Counter";
                string displayUnits = "Count";

                SimpleEventSource eventSource = new SimpleEventSource(displayName, displayUnits);
                int iter = 100;

                // increment 100 times
                for (int i = 0; i < iter; i++)
                {
                    eventSource.WriteOne();
                }

                myListener.sawEvent.WaitOne(-1); // Block until we see at least one event

                if (!myListener.validateMean())
                {
                    Console.WriteLine("Test Failed - Incorrect mean calculation");
                    return 1;                    
                }

                if (displayName != myListener.displayName)
                {
                    Console.WriteLine("Test Failed");
                    Console.WriteLine($"Expected to see {displayName} as DisplayName property in payload - saw {myListener.displayName}");
                    return 1;
                }

                if (displayUnits != myListener.displayUnits)
                {
                    Console.WriteLine("Test Failed");
                    Console.WriteLine($"Expected to see {displayUnits} as DisplayUnits property in payload - saw {myListener.displayUnits}");
                    return 1;
                }

                if (myListener.callbackCount == 0)
                {
                    Console.WriteLine("Test Failed: Expected to see 1 or more EventListener callback but got none");
                }

                Console.WriteLine("Test passed");
                return 100;
            }
        }
    }
}
