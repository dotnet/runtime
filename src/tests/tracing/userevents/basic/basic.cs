// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using Tracing.UserEvents.Tests.Common;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.UserEvents.Tests.Basic
{
    public class Basic
    {
        private static byte[] s_array;

        public static int BasicTracee()
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            long targetTicks = Stopwatch.Frequency; // 1s

            while (Stopwatch.GetTimestamp() - startTimestamp < targetTicks)
            {
                s_array = new byte[1024 * 100];
                Thread.Sleep(100);
            }

            return 100;
        }

        private static Func<EventPipeEventSource, bool> s_traceValidator = (source) =>
        {
            bool allocationSampledEventFound = false;

            source.Dynamic.All += (TraceEvent e) =>
            {
                if (e.ProviderName == "Microsoft-Windows-DotNETRuntime")
                {
                    // TraceEvent's ClrTraceEventParser does not know about the AllocationSampled Event, so it shows up as "Unknown(303)"
                    if (e.EventName.StartsWith("Unknown") && e.ID == (TraceEventID)303)
                    {
                        allocationSampledEventFound = true;
                    }
                }
            };

            source.Process();
            return allocationSampledEventFound;
        };

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("tracee", System.StringComparison.OrdinalIgnoreCase))
            {
                return BasicTracee();
            }

            return UserEventsTestRunner.Run("basic", typeof(Basic).Assembly.Location, s_traceValidator);
        }
    }
}

