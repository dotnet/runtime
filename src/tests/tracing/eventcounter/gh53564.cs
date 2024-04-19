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

namespace gh53564Tests
{
    public class RuntimeCounterListener : EventListener
    {
        public RuntimeCounterListener(){}

        private DateTime? setToZeroTimestamp = null;
        private DateTime? mostRecentTimestamp = null;
        private ManualResetEvent setToZero = new ManualResetEvent(initialState: false);
        public ManualResetEvent ReadyToVerify { get; } = new ManualResetEvent(initialState: false);

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("System.Runtime"))
            {
                Dictionary<string, string> refreshInterval = new Dictionary<string, string>();

                Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] OnEventSourceCreated :: Setting interval to 1");
                // first set interval to 1 seconds
                refreshInterval["EventCounterIntervalSec"] = "1";
                EnableEvents(source, EventLevel.Informational, (EventKeywords)(-1), refreshInterval);

                // wait a moment to get some events
                Thread.Sleep(TimeSpan.FromSeconds(3));

                // then set interval to 0
                Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] OnEventSourceCreated :: Setting interval to 0");
                refreshInterval["EventCounterIntervalSec"] = "0";
                EnableEvents(source, EventLevel.Informational, (EventKeywords)(-1), refreshInterval);
                setToZeroTimestamp = DateTime.UtcNow + TimeSpan.FromSeconds(1); // Stash timestamp 1 second after setting to 0
                setToZero.Set();

                // then attempt to set interval back to 1
                Thread.Sleep(TimeSpan.FromSeconds(3));
                Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] OnEventSourceCreated :: Setting interval to 1");
                refreshInterval["EventCounterIntervalSec"] = "1";
                EnableEvents(source, EventLevel.Informational, (EventKeywords)(-1), refreshInterval);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!ReadyToVerify.WaitOne(0))
            {
                mostRecentTimestamp = eventData.TimeStamp;
                if (setToZero.WaitOne(0) && mostRecentTimestamp > setToZeroTimestamp)
                {
                    Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] OnEventWritten :: Setting ReadyToVerify");
                    ReadyToVerify.Set();
                }
            }
        }

        public bool Verify()
        {
            if (!ReadyToVerify.WaitOne(0))
                return false;

            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] Verify :: Verifying");
            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}]   setToZeroTimestamp = {setToZeroTimestamp?.ToString("hh:mm:ss.fff") ?? "NULL"}");
            Console.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}]   mostRecentTimestamp = {mostRecentTimestamp?.ToString("hh:mm:ss.fff") ?? "NULL"}");

            return (setToZeroTimestamp is null || mostRecentTimestamp is null) ? false : setToZeroTimestamp < mostRecentTimestamp;
        }
    }

    public partial class TestRuntimeEventCounter
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // Create an EventListener.
            using (RuntimeCounterListener myListener = new RuntimeCounterListener())
            {
                if (myListener.ReadyToVerify.WaitOne(TimeSpan.FromSeconds(30)))
                {
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
