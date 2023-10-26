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

        public bool SawNanFragmentation = false;

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
            string fragmentationReported = "";
            bool isGCFragmentationCounter = false;

            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                IDictionary<string, object> eventPayload = eventData.Payload[i] as IDictionary<string, object>;
                if (eventPayload != null)
                {
                    foreach (KeyValuePair<string, object> payload in eventPayload)
                    {
                        if (payload.Key.Equals("Name") && payload.Value.ToString().Equals("gc-fragmentation"))
                            isGCFragmentationCounter = true;
                        if (payload.Key.Equals("Mean"))
                        {
                            fragmentationReported = payload.Value.ToString();
                        }
                    }
                    if (isGCFragmentationCounter && fragmentationReported.Equals("NaN"))
                    {
                        SawNanFragmentation = true;
                    }
                }
            }
        }
    }

    public partial class TestEventCounter
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // Create an EventListener.
            using (SimpleEventListener myListener = new SimpleEventListener())
            {
                Thread.Sleep(3000); 
                if (!myListener.SawNanFragmentation)
                {
                    Console.WriteLine("Test passed");
                    return 100;
                }
                else
                {
                    Console.WriteLine($"Test Failed - GC fragmentation counter reported a NaN");
                    return 1;
                }
            }
        }
    }
}
