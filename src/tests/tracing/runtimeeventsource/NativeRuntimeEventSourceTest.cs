// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tracing.Tests.Common;
using System.Collections.Concurrent;

namespace Tracing.Tests
{
    public sealed class NativeRuntimeEventSourceTest
    {
        static int Main(string[] args)
        {
            SimpleEventListener.EnableKeywords = (EventKeywords)0;
            using (SimpleEventListener noEventsListener = new SimpleEventListener("NoEvents"))
            {
                // Create an EventListener.
                SimpleEventListener.EnableKeywords = (EventKeywords)0x4c14fccbd;
                using (SimpleEventListener listener = new SimpleEventListener("Simple"))
                {
                    // Trigger the allocator task.
                    Task.Run(new Action(Allocator));

                    // If on Windows, attempt some Overlapped IO (triggers ThreadPool events)
                    if (OperatingSystem.IsWindows())
                    {
                        DoOverlappedIO();
                    }

                    // Generate some GC events.
                    GC.Collect(2, GCCollectionMode.Forced);

                    Stopwatch sw = Stopwatch.StartNew();
                    
                    while (sw.Elapsed <= TimeSpan.FromMinutes(1))
                    {
                        Thread.Sleep(100);

                        if ((OperatingSystem.IsWindows() && listener.SeenProvidersAndEvents.Contains("Microsoft-Windows-DotNETRuntime/EVENTID(65)"))
                             || (!OperatingSystem.IsWindows() && listener.EventCount > 0))
                        {
                            break;
                        }
                    }

                    // Ensure that we've seen some events.
                    foreach (string s in listener.SeenProvidersAndEvents)
                    {
                        Console.WriteLine(s);
                    }

                    Assert.True("listener.EventCount > 0", listener.EventCount > 0);
                    
                    if (OperatingSystem.IsWindows())
                    {
                        Assert.True("Saw the ThreadPoolIOPack event", listener.SeenProvidersAndEvents.Contains("Microsoft-Windows-DotNETRuntime/EVENTID(65)"));
                    }
                }

                // Generate some more GC events.
                GC.Collect(2, GCCollectionMode.Forced);

                // Ensure that we've seen no events.
                Assert.True("noEventsListener.EventCount == 0", noEventsListener.EventCount == 0);
            }

            return 100;
        }

        private static void Allocator()
        {
            while (true)
            {
                for(int i=0; i<1000; i++)
                {
                    GC.KeepAlive(new object());
                }

                Thread.Sleep(10);
            }
        }

        private static unsafe void DoOverlappedIO()
        {
            Console.WriteLine("DOOVERLAPPEDIO");
            Overlapped overlapped = new();
            NativeOverlapped* pOverlap = overlapped.Pack(null, null);
            Overlapped.Free(pOverlap);
        }
    }

    internal sealed class SimpleEventListener : EventListener
    {
        public ConcurrentBag<string> SeenProvidersAndEvents { get; private set; } = new();
        private string m_name;

        // Keep track of the set of keywords to be enabled.
        public static EventKeywords EnableKeywords
        {
            get;
            set;
        }

        public SimpleEventListener(string name)
        {
            m_name = name;
        }

        public int EventCount { get; private set; } = 0;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                if (EnableKeywords != 0)
                {
                    // Enable events.
                    EnableEvents(eventSource, EventLevel.Verbose, EnableKeywords);
                }
                else
                {
                    // Enable the provider, but not any keywords, so we should get no events as long as no rundown occurs.
                    EnableEvents(eventSource, EventLevel.Critical, EnableKeywords);
                }
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Console.WriteLine($"[{m_name}] ThreadID = {eventData.OSThreadId} ID = {eventData.EventId} Name = {eventData.EventName}");
            Console.WriteLine($"TimeStamp: {eventData.TimeStamp.ToLocalTime()}");
            Console.WriteLine($"LocalTime: {DateTime.Now}");
            Console.WriteLine($"Difference: {DateTime.UtcNow - eventData.TimeStamp}");
            Assert.True("eventData.TimeStamp <= DateTime.UtcNow", eventData.TimeStamp <= DateTime.UtcNow);
            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                string payloadString = eventData.Payload[i] != null ? eventData.Payload[i].ToString() : string.Empty;
                Console.WriteLine($"\tName = \"{eventData.PayloadNames[i]}\" Value = \"{payloadString}\"");
            }
            Console.WriteLine("\n");

            SeenProvidersAndEvents.Add($"{eventData.EventSource.Name}");
            SeenProvidersAndEvents.Add($"{eventData.EventSource.Name}/EVENTID({eventData.EventId})");

            EventCount++;
        }
    }
}
