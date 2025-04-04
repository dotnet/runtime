// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;

namespace Tracing.Tests.ThreadStress
{
    internal sealed class ThreadStressEventListener : EventListener
    {
        public bool GCEventObserved { get; private set; } = false;
        public int ExceptionEventCount { get; private set; } = 0;

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                EnableEvents(source, EventLevel.Informational, (EventKeywords)0x8001);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName == "ExceptionThrown_V1")
            {
                ExceptionEventCount++;
            }
            else if (!GCEventObserved && eventData.EventName == "GCTriggered")
            {
                GCEventObserved = true;
            }
        }
    }

    public class ThreadStressValidation
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Console.WriteLine("Starting ThreadStressValidation test...");
            using (var listener = new ThreadStressEventListener())
            {
                // Start stressing NativeRuntimeEventSource EventPipe Session's
                // Buffer Manager's ThreadSessionStateList
                Console.WriteLine("Spawning short-lived threads to populate the EP Session's Buffer Manager's ThreadSessionStateList...");
                for (int i = 0; i < 1000; i++)
                {
                    var thread = new Thread(() => { int[] array = new int[1000]; });
                    thread.Start();
                    thread.Join();
                }
                if (!listener.GCEventObserved)
                {
                    Console.WriteLine("GC event not observed. Expected threads to stress EP Session's Buffer Manager's ThreadSessionStateList. ThreadStressValidation test failed!");
                    return -1;
                }

                Console.WriteLine("Begin throwing exceptions to trigger GetNextEvent...");
                for (int i = 0; i < 500000; i++)
                {
                    try
                    {
                        throw new Exception();
                    }
                    catch {}
                }

                Console.WriteLine($"\tEventListener received {listener.ExceptionEventCount} exception event(s)\n");
                bool pass = listener.ExceptionEventCount >= 495000 && listener.ExceptionEventCount <= 500000;
                if (pass)
                {
                    Console.WriteLine("Dropped less than 1% of events. ThreadStressValidation test passed!");
                    return 100;
                }
                else
                {
                    Console.WriteLine("Dropped more than 1% of events. ThreadStressValidation test failed!");
                    return -1;
                }
            }
        }
    }
}
