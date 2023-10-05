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
using Xunit;

namespace EventCounterRegressionTests
{

    public class SimpleEventListener : EventListener
    {        
        private readonly EventLevel _level = EventLevel.Verbose;

        public double MaxIncrement { get; private set; } = 0;

        public SimpleEventListener()
        {
        }


        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("System.Runtime"))
            {
                Dictionary<string, string> refreshInterval = new Dictionary<string, string>();
                refreshInterval.Add("EventCounterIntervalSec", "1");
                EnableEvents(source, _level, (EventKeywords)(-1), refreshInterval);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            double increment = 0;
            bool isExceptionCounter = false;

            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                IDictionary<string, object> eventPayload = eventData.Payload[i] as IDictionary<string, object>;
                if (eventPayload != null)
                {
                    foreach (KeyValuePair<string, object> payload in eventPayload)
                    {
                        if (payload.Key.Equals("Name") && payload.Value.ToString().Equals("exception-count"))
                            isExceptionCounter = true;
                        if (payload.Key.Equals("Increment"))
                        {
                            increment = double.Parse(payload.Value.ToString());
                        }
                    }
                    if (isExceptionCounter)
                    {
                        if (MaxIncrement < increment)
                        {
                            MaxIncrement = increment;
                        }
                    }
                }
            }
        }
    }

    public partial class TestEventCounter
    {

        public static void ThrowExceptionTask()
        {
            // This will throw an exception every 1000 ms
            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    Debug.WriteLine("Exception thrown at " + DateTime.UtcNow.ToString("mm.ss.ffffff"));
                    throw new Exception("an exception");
                }
                catch
                {}
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Task exceptionTask = Task.Run(ThrowExceptionTask);
            Thread.Sleep(5000);

            // Create an EventListener.
            using (SimpleEventListener myListener = new SimpleEventListener())
            {
                Thread.Sleep(5000);

                // The number below is supposed to be 2 at maximum, but in debug builds, the calls to 
                // EventSource.Write() takes a lot longer than we thought, and the reflection in 
                // workingset counter also adds a huge amount of time, which makes the test fail in 
                // debug CIs. 
                // This gives us 2 + 1 (EventSource delay) + 1 (Reflection delay) = 4 maximum possible increments 
                // for the very first callback we get in EventListener. Setting the check to 4 to compensate for these.
                if (myListener.MaxIncrement > 4)
                {
                    Console.WriteLine($"Test Failed - Saw more than 3 exceptions / sec {myListener.MaxIncrement}");
                    return 1;
                }
                else
                {
                    Console.WriteLine("Test passed");
                    return 100;
                }
            }
        }
    }
}
