// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Tracing.Tests
{
    internal sealed class RuntimeEventListener : EventListener
    {
        public volatile int TPWorkerThreadStartCount = 0;
        public volatile int TPWorkerThreadStopCount = 0;
        public volatile int TPWorkerThreadWaitCount = 0;
        public volatile int TPIOPack = 0;

    public ManualResetEvent TPWaitEvent = new ManualResetEvent(false);
        
        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x10000);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName.Equals("ThreadPoolWorkerThreadStart"))
            {
                Interlocked.Increment(ref TPWorkerThreadStartCount);
                TPWaitEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolWorkerThreadStop"))
            {
                Interlocked.Increment(ref TPWorkerThreadStopCount);
                TPWaitEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolWorkerThreadWait"))
            {
                Interlocked.Increment(ref TPWorkerThreadWaitCount);
                TPWaitEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolIOPack"))
            {
                Interlocked.Increment(ref TPIOPack);
                TPWaitEvent.Set();
            }
        }
    }

    class EventListenerThreadPool
    {
        static int Main()
        {
            using (RuntimeEventListener listener = new RuntimeEventListener())
            {
                Overlapped overlapped = new Overlapped();
                IOCompletionCallback completionCallback = null;

                unsafe
                {
                    overlapped.Pack(completionCallback);
                }

                listener.TPWaitEvent.WaitOne(TimeSpan.FromMinutes(3));

                if (listener.TPIOPack > 0)
                {
                    Console.WriteLine("Test Passed.");
                    return 100;
                }
                else
                {
                    Console.WriteLine("Test Failed: Did not see any of the expected events.");
                    Console.WriteLine($"ThreadPoolWorkerThreadStartCount: {listener.TPWorkerThreadStartCount}");
                    Console.WriteLine($"ThreadPoolWorkerThreadStopCount: {listener.TPWorkerThreadStopCount}");
                    Console.WriteLine($"ThreadPoolWorkerThreadWaitCount: {listener.TPWorkerThreadWaitCount}");
                    return -1;
                }
            }
        }
    }
}
