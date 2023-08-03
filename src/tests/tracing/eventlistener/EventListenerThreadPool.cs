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
        public volatile int TPIOEnqueue = 0;
        public volatile int TPIODequeue = 0;

        public ManualResetEvent TPWaitWorkerThreadEvent = new ManualResetEvent(false);
        public ManualResetEvent TPWaitIOEvent = new ManualResetEvent(false);
        
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
                TPWaitWorkerThreadEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolWorkerThreadStop"))
            {
                Interlocked.Increment(ref TPWorkerThreadStopCount);
                TPWaitWorkerThreadEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolWorkerThreadWait"))
            {
                Interlocked.Increment(ref TPWorkerThreadWaitCount);
                TPWaitWorkerThreadEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolIOPack"))
            {
                Interlocked.Increment(ref TPIOPack);
                TPWaitIOEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolIOEnqueue"))
            {
                Interlocked.Increment(ref TPIOEnqueue);
                TPWaitIOEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolIODequeue"))
            {
                Interlocked.Increment(ref TPIODequeue);
                TPWaitIOEvent.Set();
            }
        }
    }

    class EventListenerThreadPool
    {
        static int Main()
        {
            using (RuntimeEventListener listener = new RuntimeEventListener())
            {
                int someNumber = 0;
                Task[] tasks = new Task[100];
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(() => { someNumber += 1; });
                }

                listener.TPWaitWorkerThreadEvent.WaitOne(TimeSpan.FromMinutes(3));

                bool workerThreadEventsOk = listener.TPWorkerThreadStartCount > 0 ||
                                            listener.TPWorkerThreadStopCount > 0 ||
                                            listener.TPWorkerThreadWaitCount > 0;

                if (!TestLibrary.Utilities.IsNativeAot && !workerThreadEventsOk)
                {
                    Console.WriteLine("Test Failed: Did not see any of the expected events.");
                    Console.WriteLine($"ThreadPoolWorkerThreadStartCount: {listener.TPWorkerThreadStartCount}");
                    Console.WriteLine($"ThreadPoolWorkerThreadStopCount: {listener.TPWorkerThreadStopCount}");
                    Console.WriteLine($"ThreadPoolWorkerThreadWaitCount: {listener.TPWorkerThreadWaitCount}");
                    return -1;
                }

                Overlapped overlapped = new Overlapped();

                unsafe
                {
                    NativeOverlapped* nativeOverlapped = overlapped.Pack(null);
                    ThreadPool.UnsafeQueueNativeOverlapped(nativeOverlapped);
                }

                for (int i = 0; i < 3; ++i)
                {
                    if (listener.TPIOPack == 0 || listener.TPIOEnqueue == 0 || listener.TPIODequeue == 0)
                    {
                        listener.TPWaitIOEvent.WaitOne(TimeSpan.FromMinutes(1));
                    }
                }

                if (!(listener.TPIOPack > 0 && listener.TPIOEnqueue > 0 && listener.TPIODequeue > 0))
                {
                    Console.WriteLine("Test Failed: Did not see all of the expected events.");
                    Console.WriteLine($"ThreadPoolIOPack: {listener.TPIOPack}");
                    Console.WriteLine($"ThreadPoolIOEnqueue: {listener.TPIOEnqueue}");
                    Console.WriteLine($"ThreadPoolIODequeue: {listener.TPIODequeue}");
                    return -1;
                }
                else
                {
                    Console.WriteLine("Test Passed.");
                    return 100;
                }
            }
        }
    }
}
