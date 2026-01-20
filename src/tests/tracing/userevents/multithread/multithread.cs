// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Tracing.UserEvents.Tests.Common;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.UserEvents.Tests.MultiThread
{
    [EventSource(Name = "DemoMultiThread")]
    public sealed class MultiThreadEventSource : EventSource
    {
        public static readonly MultiThreadEventSource Log = new MultiThreadEventSource();

        private MultiThreadEventSource() {}

        [Event(1, Level = EventLevel.Informational)]
        public void WorkerEvent(int workerId)
        {
            WriteEvent(1, workerId);
        }
    }

    public static class MultiThread
    {
        private const int WorkerCount = 4;

        public static void MultiThreadTracee()
        {
            Task[] tasks = new Task[WorkerCount];

            for (int i = 0; i < WorkerCount; i++)
            {
                int workerId = i;
                tasks[i] = Task.Run(() =>
                {
                    MultiThreadEventSource.Log.WorkerEvent(workerId);
                });
            }

            Task.WaitAll(tasks);
        }

        private static readonly Func<EventPipeEventSource, bool> s_traceValidator = source =>
        {
            HashSet<int> seenWorkers = new HashSet<int>();

            source.Dynamic.All += (TraceEvent e) =>
            {
                if (!string.Equals(e.ProviderName, "DemoMultiThread", StringComparison.Ordinal))
                {
                    return;
                }

                if (e.EventName is not "WorkerEvent")
                {
                    return;
                }

                int workerId = -1;
                try
                {
                    workerId = (int)(e.PayloadByName("workerId") ?? -1);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Exception while reading workerId payload: {ex}");
                }

                if (workerId >= 0)
                {
                    seenWorkers.Add(workerId);
                }
            };

            source.Process();

            for (int i = 0; i < WorkerCount; i++)
            {
                if (!seenWorkers.Contains(i))
                {
                    Console.Error.WriteLine($"Did not observe event for worker {i}.");
                    return false;
                }
            }

            return true;
        };

        public static int Main(string[] args)
        {
            return UserEventsTestRunner.Run(
                args,
                "multithread",
                typeof(MultiThread).Assembly.Location,
                MultiThreadTracee,
                s_traceValidator);
        }
    }
}
