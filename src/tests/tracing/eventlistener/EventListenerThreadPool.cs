// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Tracing.Tests.Common;

namespace Tracing.Tests
{
    internal sealed class RuntimeEventListener : EventListener
    {
        public int TPWorkerThreadStartCount = 0;
        public int TPWorkerThreadStopCount = 0;
        public int TPWorkerThreadWaitCount = 0;
        
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
                TPWorkerThreadStartCount += 1;
            }
            else if (eventData.EventName.Equals("ThreadPoolWorkerThreadStop"))
            {
                TPWorkerThreadStopCount += 1;
            }
            else if (eventData.EventName.Equals("ThreadPoolWorkerThreadWait"))
            {
                TPWorkerThreadWaitCount += 1;
            }
        }
    }

    class EventPipeSmoke
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
                Task.WaitAll(tasks);
                Thread.Sleep(3000);

                if (listener.TPWorkerThreadStartCount > 10 ||
                    listener.TPWorkerThreadStopCount > 10 ||
                    listener.TPWorkerThreadWaitCount > 10)
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
