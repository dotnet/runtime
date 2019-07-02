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
    public partial class TestIncrementingEventCounter
    {

        [EventSource(Name = "SimpleEventSource")]
        private sealed class SimpleEventSource : EventSource
        {
            private IncrementingEventCounter _myCounter;

            public SimpleEventSource(string _displayName, string _displayUnits)
            {
                _myCounter = new IncrementingEventCounter("test-counter", this) { DisplayName = _displayName, DisplayUnits = _displayUnits, DisplayRateTimeScale = new TimeSpan(0, 0, 1) };
            }

            public void IncrementCounter()
            {
                _myCounter.Increment();
            }
        }

        internal sealed class SimpleEventListener : EventListener
        {
            private readonly string _targetSourceName;
            private readonly EventLevel _level;
            private Dictionary<string, string> args;
            
            public int incrementSum;
            public string displayName;
            public string displayUnits;
            public string displayRateTimeScale;
            
            public SimpleEventListener(string targetSourceName, EventLevel level)
            {
                // Store the arguments
                _targetSourceName = targetSourceName;
                _level = level;
                incrementSum = 0;
                displayName = "";
                displayUnits = "";
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
                        foreach (KeyValuePair<string, object> payload in eventPayload)
                        {
                            if (payload.Key.Equals("Increment"))
                            {
                                incrementSum += Int32.Parse(payload.Value.ToString());
                            }
                            else if (payload.Key.Equals("DisplayName"))
                            {
                                displayName = payload.Value.ToString();
                            }
                            else if (payload.Key.Equals("DisplayUnits"))
                            {
                                displayUnits = payload.Value.ToString();
                            }
                            else if (payload.Key.Equals("DisplayRateTimeScale"))
                            {
                                displayRateTimeScale = payload.Value.ToString();
                            }
                        }
                    }
                }
            }
        }

        public static int Main(string[] args)
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
                    eventSource.IncrementCounter();    
                }

                Thread.Sleep(3000);

                if (iter != myListener.incrementSum)
                {
                    Console.WriteLine("Test Failed");
                    Console.WriteLine($"Expected to see {iter} events - saw {myListener.incrementSum}");
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

                if (!myListener.displayRateTimeScale.Equals("00:00:01"))
                {
                    Console.WriteLine("Test failed");
                    Console.WriteLine($"Wrong DisplayRateTimeScale: {myListener.displayRateTimeScale}");
                }

                Console.WriteLine("Test passed");
                return 100;
            }
        }
    }
}