// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace Allocate
{
    [EventSource(Name = "Allocations-Run")]
    public class AllocationsRunEventSource : EventSource
    {
        public static readonly AllocationsRunEventSource Log = new AllocationsRunEventSource();

        [Event(600, Level = EventLevel.Informational)]
        public void StartRun(int iterationsCount, int allocationCount, string listOfTypes)
        {
            WriteEvent(eventId: 600, iterationsCount, allocationCount, listOfTypes);
        }

        [Event(601, Level = EventLevel.Informational)]
        public void StopRun()
        {
            WriteEvent(eventId: 601);
        }

        [Event(602, Level = EventLevel.Informational)]
        public void StartIteration(int iteration)
        {
            WriteEvent(eventId: 602, iteration);
        }

        [Event(603, Level = EventLevel.Informational)]
        public void StopIteration(int iteration)
        {
            WriteEvent(eventId: 603, iteration);
        }
    }
}
