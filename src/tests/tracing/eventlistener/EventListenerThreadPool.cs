// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tracing.Tests
{
    internal sealed class RuntimeEventListener : EventListener
    {
        public volatile int TPWorkerThreadWaitCount = 0;
        public volatile int TPIOPack = 0;
        public volatile int TPIOEnqueue = 0;
        public volatile int TPIODequeue = 0;

        public int TPIOPackGoal = 0;
        public int TPIOEnqueueGoal = 1;
        public int TPIODequeueGoal = 1;

        public ManualResetEvent TPWaitWorkerThreadEvent = new ManualResetEvent(false);
        public ManualResetEvent TPWaitIOPackEvent = new ManualResetEvent(false);
        public ManualResetEvent TPWaitIOEnqueueEvent = new ManualResetEvent(false);
        public ManualResetEvent TPWaitIODequeueEvent = new ManualResetEvent(false);
        
        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x10000);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName.Equals("ThreadPoolWorkerThreadWait"))
            {
                Interlocked.Increment(ref TPWorkerThreadWaitCount);
                TPWaitWorkerThreadEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolIOPack"))
            {
                Interlocked.Increment(ref TPIOPack);
                if (TPIOPack == TPIOPackGoal)
                    TPWaitIOPackEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolIOEnqueue"))
            {
                Interlocked.Increment(ref TPIOEnqueue);
                if (TPIOEnqueue == TPIOEnqueueGoal)
                    TPWaitIOEnqueueEvent.Set();
            }
            else if (eventData.EventName.Equals("ThreadPoolIODequeue"))
            {
                Interlocked.Increment(ref TPIODequeue);
                if (TPIODequeue == TPIODequeueGoal)
                    TPWaitIODequeueEvent.Set();
            }
        }
    }

    public class EventListenerThreadPool
    {
        [Fact]
        public static int TestEntryPoint()
        {
            using (RuntimeEventListener listener = new RuntimeEventListener())
            {
                // This should fire at least one ThreadPoolWorkerThreadWait
                int someNumber = 0;
                Task[] tasks = new Task[100];
                for (int i = 0; i < tasks.Length; i++) 
                {
                    tasks[i] = Task.Run(() => { someNumber += 1; });
                }

                if (TestLibrary.Utilities.IsWindows)
                {
                    // This part is Windows-specific, it should fire an IOPack, IOEnqueue and IODequeue event
                    listener.TPIOPackGoal += 1;
                    listener.TPIOEnqueueGoal += 1;
                    listener.TPIODequeueGoal += 1;
                
                    Overlapped overlapped = new Overlapped();
                    unsafe
                    {
                        NativeOverlapped* nativeOverlapped = overlapped.Pack(null);
                        ThreadPool.UnsafeQueueNativeOverlapped(nativeOverlapped);
                    }
                }

                // RegisterWaitForSingleObject should fire an IOEnqueue and IODequeue event
                ManualResetEvent manualResetEvent = new ManualResetEvent(false);
                WaitOrTimerCallback work = (x, timedOut) => { int y = (int)x; };
                ThreadPool.RegisterWaitForSingleObject(manualResetEvent, work, 1, 100, true);
                manualResetEvent.Set();

                ManualResetEvent[] waitEvents = new ManualResetEvent[] {listener.TPWaitIOPackEvent,
                                                                        listener.TPWaitIOEnqueueEvent,
                                                                        listener.TPWaitIODequeueEvent};

                WaitHandle.WaitAll(waitEvents, TimeSpan.FromMinutes(1));

                if (!TestLibrary.Utilities.IsNativeAot)
                {
                    listener.TPWaitWorkerThreadEvent.WaitOne(TimeSpan.FromMinutes(1));
                    if (listener.TPWorkerThreadWaitCount == 0)
                    {
                        Console.WriteLine("Test Failed: Did not see the expected event.");
                        Console.WriteLine($"ThreadPoolWorkerThreadWaitCount: {listener.TPWorkerThreadWaitCount}");
                        return -1;
                    }
                }

                if (!(listener.TPIOPack >= listener.TPIOPackGoal &&
                    listener.TPIOEnqueue >= listener.TPIOEnqueueGoal &&
                    listener.TPIODequeue >= listener.TPIODequeueGoal))
                {
                    Console.WriteLine("Test Failed: Did not see all of the expected events.");
                    Console.WriteLine($"ThreadPoolIOPack: {listener.TPIOPack}");
                    Console.WriteLine($"ThreadPoolIOEnqueue: {listener.TPIOEnqueue}");
                    Console.WriteLine($"ThreadPoolIODequeue: {listener.TPIODequeue}");
                    return -1;
                }

                Console.WriteLine("Test Passed.");
                return 100;
            }
        }
    }
}
