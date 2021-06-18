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

    public ManualResetEvent TPWaitEvent = new ManualResetEvent(false);
        
        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                EnableEvents(source, EventLevel.Informational, (EventKeywords)0x10000);
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
        }
    }

    class EventListenerThreadPool
    {
        static int Main(string[] args)
        {
            using (RuntimeEventListener listener = new RuntimeEventListener())
            {
                int someNumber = 0;
                Task[] tasks = new Task[100];
                for (int i = 0; i < tasks.Length; i++) 
                {
                    tasks[i] = Task.Run(() => { someNumber += 1; });
                }

                listener.TPWaitEvent.WaitOne(TimeSpan.FromMinutes(3));

                if (listener.TPWorkerThreadStartCount > 0 ||
                    listener.TPWorkerThreadStopCount > 0 ||
                    listener.TPWorkerThreadWaitCount > 0)
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
