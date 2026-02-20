// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using Tracing.UserEvents.Tests.Common;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.UserEvents.Tests.ManagedEvent
{
    public class ManagedEvent
    {
        public static void ManagedEventTracee()
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            long targetTicks = Stopwatch.Frequency; // 1s

            while (Stopwatch.GetTimestamp() - startTimestamp < targetTicks)
            {
                ManagedUserEventSource.Log.SampleEvent("SampleWork");
                Thread.Sleep(100);
            }
        }

        private static readonly Func<EventPipeEventSource, bool> s_traceValidator = source =>
        {
            bool sampleEventFound = false;

            source.Dynamic.All += (TraceEvent e) =>
            {
                if (!string.Equals(e.ProviderName, "ManagedUserEvent", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (e.EventName is null)
                {
                    return;
                }

                sampleEventFound = true;
            };

            source.Process();

            if (!sampleEventFound)
            {
                Console.Error.WriteLine("The trace did not contain the expected managed event.");
            }

            return sampleEventFound;
        };

        public static int Main(string[] args)
        {
            return UserEventsTestRunner.Run(
                args,
                "managedevent",
                ManagedEventTracee,
                s_traceValidator);
        }
    }

    [EventSource(Name = "ManagedUserEvent")]
    internal sealed class ManagedUserEventSource : EventSource
    {
        public static readonly ManagedUserEventSource Log = new ManagedUserEventSource();

        [Event(1)]
        public void SampleEvent(string requestName) => WriteEvent(1, requestName);
    }
}