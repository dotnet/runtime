// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace EP_Target
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Threading;

    public static class Program
    {
        public static int Main(string[] args)
        {
            // Waiting 10 seconds to client to get the PID
            Thread.Sleep(10 * 1000);
            TargetStartLogging();   
            // returning success since the actual test is in ..\EventPipeClient         
            return 100;
        }

        private static void TargetStartLogging()
        {
            DemoEventSource.Log.AppStarted("Data from NativeAOT app!", 42);
        }
    }

    [EventSource(Name = "Demo")]
    class DemoEventSource : EventSource
    {
        public static DemoEventSource Log { get; } = new DemoEventSource();

        [Event(1, Keywords = Keywords.Startup)]
        public void AppStarted(string message, int favoriteNumber) => WriteEvent(1, message, favoriteNumber);

        public class Keywords
        {
            public const EventKeywords Startup = (EventKeywords)0x0001;
        }
    }
}
