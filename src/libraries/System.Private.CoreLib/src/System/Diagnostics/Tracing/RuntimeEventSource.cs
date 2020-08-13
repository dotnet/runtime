// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// RuntimeEventSource is an EventSource that represents events emitted by the managed runtime.
    /// </summary>
    [EventSource(Guid = "49592C0F-5A05-516D-AA4B-A64E02026C89", Name = "System.Runtime")]
    internal sealed class RuntimeEventSource : EventSource
    {
        internal const string EventSourceName = "System.Runtime";

        private static RuntimeEventSource? s_RuntimeEventSource;
        private PollingCounter? _gcHeapSizeCounter;
        private IncrementingPollingCounter? _gen0GCCounter;
        private IncrementingPollingCounter? _gen1GCCounter;
        private IncrementingPollingCounter? _gen2GCCounter;
        private PollingCounter? _cpuTimeCounter;
        private PollingCounter? _workingSetCounter;
        private PollingCounter? _threadPoolThreadCounter;
        private IncrementingPollingCounter? _monitorContentionCounter;
        private PollingCounter? _threadPoolQueueCounter;
        private IncrementingPollingCounter? _completedItemsCounter;
        private IncrementingPollingCounter? _allocRateCounter;
        private PollingCounter? _timerCounter;
        private PollingCounter? _fragmentationCounter;

#if !MONO
        private IncrementingPollingCounter? _exceptionCounter;
        private PollingCounter? _gcTimeCounter;
        private PollingCounter? _gen0SizeCounter;
        private PollingCounter? _gen1SizeCounter;
        private PollingCounter? _gen2SizeCounter;
        private PollingCounter? _lohSizeCounter;
        private PollingCounter? _pohSizeCounter;
        private PollingCounter? _assemblyCounter;
        private PollingCounter? _ilBytesJittedCounter;
        private PollingCounter? _methodsJittedCounter;
#endif

        public static void Initialize()
        {
            s_RuntimeEventSource = new RuntimeEventSource();
        }

        private RuntimeEventSource() : base(new Guid(0x49592C0F, 0x5A05, 0x516D, 0xAA, 0x4B, 0xA6, 0x4E, 0x02, 0x02, 0x6C, 0x89), EventSourceName, EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // NOTE: These counters will NOT be disposed on disable command because we may be introducing
                // a race condition by doing that. We still want to create these lazily so that we aren't adding
                // overhead by at all times even when counters aren't enabled.

                // On disable, PollingCounters will stop polling for values so it should be fine to leave them around.
                _cpuTimeCounter ??= new PollingCounter("cpu-usage", this, () => RuntimeEventSourceHelper.GetCpuUsage()) { DisplayName = "CPU Usage", DisplayUnits = "%" };
                _workingSetCounter ??= new PollingCounter("working-set", this, () => (double)(Environment.WorkingSet / 1000000)) { DisplayName = "Working Set", DisplayUnits = "MB" };
                _gcHeapSizeCounter ??= new PollingCounter("gc-heap-size", this, () => (double)(GC.GetTotalMemory(false) / 1000000)) { DisplayName = "GC Heap Size", DisplayUnits = "MB" };
                _gen0GCCounter ??= new IncrementingPollingCounter("gen-0-gc-count", this, () => GC.CollectionCount(0)) { DisplayName = "Gen 0 GC Count", DisplayRateTimeScale = new TimeSpan(0, 1, 0) };
                _gen1GCCounter ??= new IncrementingPollingCounter("gen-1-gc-count", this, () => GC.CollectionCount(1)) { DisplayName = "Gen 1 GC Count", DisplayRateTimeScale = new TimeSpan(0, 1, 0) };
                _gen2GCCounter ??= new IncrementingPollingCounter("gen-2-gc-count", this, () => GC.CollectionCount(2)) { DisplayName = "Gen 2 GC Count", DisplayRateTimeScale = new TimeSpan(0, 1, 0) };
                _threadPoolThreadCounter ??= new PollingCounter("threadpool-thread-count", this, () => ThreadPool.ThreadCount) { DisplayName = "ThreadPool Thread Count" };
                _monitorContentionCounter ??= new IncrementingPollingCounter("monitor-lock-contention-count", this, () => Monitor.LockContentionCount) { DisplayName = "Monitor Lock Contention Count", DisplayRateTimeScale = new TimeSpan(0, 0, 1) };
                _threadPoolQueueCounter ??= new PollingCounter("threadpool-queue-length", this, () => ThreadPool.PendingWorkItemCount) { DisplayName = "ThreadPool Queue Length" };
                _completedItemsCounter ??= new IncrementingPollingCounter("threadpool-completed-items-count", this, () => ThreadPool.CompletedWorkItemCount) { DisplayName = "ThreadPool Completed Work Item Count", DisplayRateTimeScale = new TimeSpan(0, 0, 1) };
                _allocRateCounter ??= new IncrementingPollingCounter("alloc-rate", this, () => GC.GetTotalAllocatedBytes()) { DisplayName = "Allocation Rate", DisplayUnits = "B", DisplayRateTimeScale = new TimeSpan(0, 0, 1) };
                _timerCounter ??= new PollingCounter("active-timer-count", this, () => Timer.ActiveCount) { DisplayName = "Number of Active Timers" };
                _fragmentationCounter ??= new PollingCounter("gc-fragmentation", this, () => {
                    var gcInfo = GC.GetGCMemoryInfo();
                    return gcInfo.FragmentedBytes * 100d / gcInfo.HeapSizeBytes;
                 }) { DisplayName = "GC Fragmentation", DisplayUnits = "%" };
#if !MONO
                _exceptionCounter ??= new IncrementingPollingCounter("exception-count", this, () => Exception.GetExceptionCount()) { DisplayName = "Exception Count", DisplayRateTimeScale = new TimeSpan(0, 0, 1) };
                _gcTimeCounter ??= new PollingCounter("time-in-gc", this, () => GC.GetLastGCPercentTimeInGC()) { DisplayName = "% Time in GC since last GC", DisplayUnits = "%" };
                _gen0SizeCounter ??= new PollingCounter("gen-0-size", this, () => GC.GetGenerationSize(0)) { DisplayName = "Gen 0 Size", DisplayUnits = "B" };
                _gen1SizeCounter ??= new PollingCounter("gen-1-size", this, () => GC.GetGenerationSize(1)) { DisplayName = "Gen 1 Size", DisplayUnits = "B" };
                _gen2SizeCounter ??= new PollingCounter("gen-2-size", this, () => GC.GetGenerationSize(2)) { DisplayName = "Gen 2 Size", DisplayUnits = "B" };
                _lohSizeCounter ??= new PollingCounter("loh-size", this, () => GC.GetGenerationSize(3)) { DisplayName = "LOH Size", DisplayUnits = "B" };
                _pohSizeCounter ??= new PollingCounter("poh-size", this, () => GC.GetGenerationSize(4)) { DisplayName = "POH (Pinned Object Heap) Size", DisplayUnits = "B" };
                _assemblyCounter ??= new PollingCounter("assembly-count", this, () => System.Reflection.Assembly.GetAssemblyCount()) { DisplayName = "Number of Assemblies Loaded" };
                _ilBytesJittedCounter ??= new PollingCounter("il-bytes-jitted", this, () => System.Runtime.CompilerServices.RuntimeHelpers.GetILBytesJitted()) { DisplayName = "IL Bytes Jitted", DisplayUnits = "B" };
                _methodsJittedCounter ??= new PollingCounter("methods-jitted-count", this, () => System.Runtime.CompilerServices.RuntimeHelpers.GetMethodsJittedCount()) { DisplayName = "Number of Methods Jitted" };
#endif
            }

        }
    }
}
