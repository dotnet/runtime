// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;


namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// RuntimeEventSource is an EventSource that represents events emitted by the managed runtime.
    /// </summary>
    [EventSource(Guid="49592C0F-5A05-516D-AA4B-A64E02026C89", Name = "System.Runtime")]
    internal sealed class RuntimeEventSource : EventSource
    {
        private static RuntimeEventSource s_RuntimeEventSource;
        private PollingCounter _gcHeapSizeCounter;
        private IncrementingPollingCounter _gen0GCCounter;
        private IncrementingPollingCounter _gen1GCCounter;
        private IncrementingPollingCounter _gen2GCCounter;
        private IncrementingPollingCounter _exceptionCounter;
        private PollingCounter _cpuTimeCounter;
        private PollingCounter _workingSetCounter;

        private const int EnabledPollingIntervalMilliseconds = 1000; // 1 second

        public static void Initialize()
        {
            s_RuntimeEventSource = new RuntimeEventSource();
        }
        
        private RuntimeEventSource(): base(new Guid(0x49592C0F, 0x5A05, 0x516D, 0xAA, 0x4B, 0xA6, 0x4E, 0x02, 0x02, 0x6C, 0x89), "System.Runtime", EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        protected override void OnEventCommand(System.Diagnostics.Tracing.EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // NOTE: These counters will NOT be disposed on disable command because we may be introducing 
                // a race condition by doing that. We still want to create these lazily so that we aren't adding
                // overhead by at all times even when counters aren't enabled.

                // On disable, PollingCounters will stop polling for values so it should be fine to leave them around.
                _cpuTimeCounter = _cpuTimeCounter ?? new PollingCounter("CPU Usage", this, () => RuntimeEventSourceHelper.GetCpuUsage());
                _workingSetCounter = _workingSetCounter ?? new PollingCounter("Working Set", this, () => Environment.WorkingSet);
                _gcHeapSizeCounter = _gcHeapSizeCounter ?? new PollingCounter("GC Heap Size", this, () => GC.GetTotalMemory(false));
                _gen0GCCounter = _gen0GCCounter ?? new IncrementingPollingCounter("Gen 0 GC Count", this, () => GC.CollectionCount(0));
                _gen1GCCounter = _gen1GCCounter ?? new IncrementingPollingCounter("Gen 1 GC Count", this, () => GC.CollectionCount(1));
                _gen2GCCounter = _gen2GCCounter ?? new IncrementingPollingCounter("Gen 2 GC Count", this, () => GC.CollectionCount(2));
                _exceptionCounter = _exceptionCounter ?? new IncrementingPollingCounter("Exception Count", this, () => Exception.GetExceptionCount());
            }
        }
    }
}
