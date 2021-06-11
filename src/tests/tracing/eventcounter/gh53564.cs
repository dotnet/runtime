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

namespace gh53564Tests
{
    public class RuntimeCounterListener : EventListener
    {
        public RuntimeCounterListener(){}

        private DateTime? setToZeroTimestamp = null;
        private DateTime? mostRecentTimestamp = null;
        public ManualResetEvent ReadyToVerify { get; } = new ManualResetEvent(initialState: false);

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("System.Runtime"))
            {
                Dictionary<string, string> refreshInterval = new Dictionary<string, string>();

                Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] Setting interval to 1");
                // first set interval to 1 seconds
                refreshInterval["EventCounterIntervalSec"] = "1";
                EnableEvents(source, EventLevel.Informational, (EventKeywords)(-1), refreshInterval);

                // wait a moment to get some events
                Thread.Sleep(TimeSpan.FromSeconds(3));

                // then set interval to 0
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] Setting interval to 0");
                refreshInterval["EventCounterIntervalSec"] = "0";
                EnableEvents(source, EventLevel.Informational, (EventKeywords)(-1), refreshInterval);
                setToZeroTimestamp = DateTime.Now + TimeSpan.FromSeconds(1); // Stash timestamp 1 second after setting to 0

                // then attempt to set interval back to 1
                Thread.Sleep(TimeSpan.FromSeconds(3));
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] Setting interval to 1");
                refreshInterval["EventCounterIntervalSec"] = "1";
                EnableEvents(source, EventLevel.Informational, (EventKeywords)(-1), refreshInterval);
                Thread.Sleep(TimeSpan.FromSeconds(3));
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] Setting ReadyToVerify");
                ReadyToVerify.Set();
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            mostRecentTimestamp = eventData.TimeStamp;
        }

        public bool Verify()
        {
            if (!ReadyToVerify.WaitOne(0))
                return false;

            return (setToZeroTimestamp is null || mostRecentTimestamp is null) ? false : setToZeroTimestamp < mostRecentTimestamp;
        }
    }

    public partial class TestRuntimeEventCounter
    {
        public static int Main(string[] args)
        {
            // Create an EventListener.
            using (RuntimeCounterListener myListener = new RuntimeCounterListener())
            {
                if (myListener.ReadyToVerify.WaitOne(TimeSpan.FromSeconds(15)))
                {
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] Ready to verify");
                    if (myListener.Verify())
                    {
                        Console.WriteLine("Test passed");
                        return 100;
                    }
                    else
                    {
                        Console.WriteLine($"Test Failed - did not see one or more of the expected runtime counters.");
                        return 1;
                    }
                }
                else
                {
                    Console.WriteLine("Test Failed - timed out waiting for reset");
                    return 1;
                }
            }
        }
    }
}
