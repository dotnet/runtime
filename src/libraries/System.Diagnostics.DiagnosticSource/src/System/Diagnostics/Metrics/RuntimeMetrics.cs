// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics.Metrics
{
    internal static class RuntimeMetrics
    {
        [ThreadStatic] private static bool t_handlingFirstChanceException;

        private const string MeterName = "System.Runtime";

        private static readonly Meter s_meter = new(MeterName);

        // These MUST align to the possible attribute values defined in the semantic conventions (TODO: link to the spec)
        private static readonly string[] s_genNames = ["gen0", "gen1", "gen2", "loh", "poh"];

        private static readonly int s_maxGenerations = Math.Min(GC.GetGCMemoryInfo().GenerationInfo.Length, s_genNames.Length);

        static RuntimeMetrics()
        {
            AppDomain.CurrentDomain.FirstChanceException += (source, e) =>
            {
                // Avoid recursion if the listener itself throws an exception while recording the measurement
                // in its `OnMeasurementRecorded` callback.
                if (t_handlingFirstChanceException) return;
                t_handlingFirstChanceException = true;
                s_exceptions.Add(1, new KeyValuePair<string, object?>("error.type", e.Exception.GetType().Name));
                t_handlingFirstChanceException = false;
            };
        }

        private static readonly ObservableCounter<long> s_gcCollections = s_meter.CreateObservableCounter(
            "dotnet.gc.collections",
            GetGarbageCollectionCounts,
            unit: "{collection}",
            description: "The number of garbage collections that have occurred since the process has started.");

        private static readonly ObservableUpDownCounter<long> s_processWorkingSet = s_meter.CreateObservableUpDownCounter(
            "dotnet.process.memory.working_set",
            () => Environment.WorkingSet,
            unit: "By",
            description: "The number of bytes of physical memory mapped to the process context.");

        private static readonly ObservableCounter<long> s_gcHeapTotalAllocated = s_meter.CreateObservableCounter(
            "dotnet.gc.heap.total_allocated",
            () => GC.GetTotalAllocatedBytes(),
            unit: "By",
            description: "The approximate number of bytes allocated on the managed GC heap since the process has started. The returned value does not include any native allocations.");

        private static readonly ObservableUpDownCounter<long> s_gcLastCollectionMemoryCommitted = s_meter.CreateObservableUpDownCounter(
            "dotnet.gc.last_collection.memory.committed_size",
            () =>
            {
                GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

                return gcInfo.Index == 0
                    ? Array.Empty<Measurement<long>>()
                    : [new(gcInfo.TotalCommittedBytes)];
            },
            unit: "By",
            description: "The amount of committed virtual memory in use by the .NET GC, as observed during the latest garbage collection.");

        private static readonly ObservableUpDownCounter<long> s_gcLastCollectionHeapSize = s_meter.CreateObservableUpDownCounter(
            "dotnet.gc.last_collection.heap.size",
            GetHeapSizes,
            unit: "By",
            description: "The managed GC heap size (including fragmentation), as observed during the latest garbage collection.");

        private static readonly ObservableUpDownCounter<long> s_gcLastCollectionFragmentationSize = s_meter.CreateObservableUpDownCounter(
            "dotnet.gc.last_collection.heap.fragmentation.size",
            GetHeapFragmentation,
            unit: "By",
            description: "The heap fragmentation, as observed during the latest garbage collection.");

        private static readonly ObservableCounter<double> s_gcPauseTime = s_meter.CreateObservableCounter(
            "dotnet.gc.pause.time",
            () => GC.GetTotalPauseDuration().TotalSeconds,
            unit: "s",
            description: "The total amount of time paused in GC since the process has started.");

        private static readonly ObservableCounter<long> s_jitCompiledSize = s_meter.CreateObservableCounter(
            "dotnet.jit.compiled_il.size",
            () => Runtime.JitInfo.GetCompiledILBytes(),
            unit: "By",
            description: "Count of bytes of intermediate language that have been compiled since the process has started.");

        private static readonly ObservableCounter<long> s_jitCompiledMethodCount = s_meter.CreateObservableCounter(
            "dotnet.jit.compiled_methods",
            () => Runtime.JitInfo.GetCompiledMethodCount(),
            unit: "{method}",
            description: "The number of times the JIT compiler (re)compiled methods since the process has started.");

        private static readonly ObservableCounter<double> s_jitCompilationTime = s_meter.CreateObservableCounter(
            "dotnet.jit.compilation.time",
            () => Runtime.JitInfo.GetCompilationTime().TotalSeconds,
            unit: "s",
            description: "The number of times the JIT compiler (re)compiled methods since the process has started.");

        private static readonly ObservableCounter<long> s_monitorLockContention = s_meter.CreateObservableCounter(
            "dotnet.monitor.lock_contentions",
            () => Monitor.LockContentionCount,
            unit: "{contention}",
            description: "The number of times there was contention when trying to acquire a monitor lock since the process has started.");

        private static readonly ObservableCounter<long> s_threadPoolThreadCount = s_meter.CreateObservableCounter(
            "dotnet.thread_pool.thread.count",
            () => (long)ThreadPool.ThreadCount,
            unit: "{thread}",
            description: "The number of thread pool threads that currently exist.");

        private static readonly ObservableCounter<long> s_threadPoolCompletedWorkItems = s_meter.CreateObservableCounter(
            "dotnet.thread_pool.work_item.count",
            () => ThreadPool.CompletedWorkItemCount,
            unit: "{work_item}",
            description: "The number of work items that the thread pool has completed since the process has started.");

        private static readonly ObservableCounter<long> s_threadPoolQueueLength = s_meter.CreateObservableCounter(
            "dotnet.thread_pool.queue.length",
            () => ThreadPool.PendingWorkItemCount,
            unit: "{work_item}",
            description: "The number of work items that are currently queued to be processed by the thread pool.");

        private static readonly ObservableUpDownCounter<long> s_timerCount = s_meter.CreateObservableUpDownCounter(
            "dotnet.timer.count",
            () => Timer.ActiveCount,
            unit: "{timer}",
            description: "The number of timer instances that are currently active. An active timer is registered to tick at some point in the future and has not yet been canceled.");

        private static readonly ObservableUpDownCounter<long> s_assembliesCount = s_meter.CreateObservableUpDownCounter(
            "dotnet.assembly.count",
            () => (long)AppDomain.CurrentDomain.GetAssemblies().Length,
            unit: "{assembly}",
            description: "The number of .NET assemblies that are currently loaded.");

        private static readonly Counter<long> s_exceptions = s_meter.CreateCounter<long>(
            "dotnet.exceptions",
            unit: "{exception}",
            description: "The number of exceptions that have been thrown in managed code.");

        private static readonly ObservableUpDownCounter<long> s_processCpuCount = s_meter.CreateObservableUpDownCounter(
            "dotnet.process.cpu.count",
            () => (long)Environment.ProcessorCount,
            unit: "{cpu}",
            description: "The number of processors available to the process.");

        private static readonly ObservableCounter<double>? s_processCpuTime =
                                    OperatingSystem.IsBrowser() || OperatingSystem.IsTvOS() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()) ?
                                    null :
                                    s_meter.CreateObservableCounter(
                                        "dotnet.process.cpu.time",
                                        GetCpuTime,
                                        unit: "s",
                                        description: "CPU time used by the process.");

        public static bool IsEnabled()
        {
            return s_gcCollections.Enabled
                || s_processWorkingSet.Enabled
                || s_gcHeapTotalAllocated.Enabled
                || s_gcLastCollectionMemoryCommitted.Enabled
                || s_gcLastCollectionHeapSize.Enabled
                || s_gcLastCollectionFragmentationSize.Enabled
                || s_gcPauseTime.Enabled
                || s_jitCompiledSize.Enabled
                || s_jitCompiledMethodCount.Enabled
                || s_jitCompilationTime.Enabled
                || s_monitorLockContention.Enabled
                || s_timerCount.Enabled
                || s_threadPoolThreadCount.Enabled
                || s_threadPoolCompletedWorkItems.Enabled
                || s_threadPoolQueueLength.Enabled
                || s_assembliesCount.Enabled
                || s_exceptions.Enabled
                || s_processCpuCount.Enabled
                || s_processCpuTime?.Enabled is true;
        }

        private static IEnumerable<Measurement<long>> GetGarbageCollectionCounts()
        {
            long collectionsFromHigherGeneration = 0;

            for (int gen = GC.MaxGeneration; gen >= 0; --gen)
            {
                long collectionsFromThisGeneration = GC.CollectionCount(gen);
                yield return new(collectionsFromThisGeneration - collectionsFromHigherGeneration, new KeyValuePair<string, object?>("gc.heap.generation", s_genNames[gen]));
                collectionsFromHigherGeneration = collectionsFromThisGeneration;
            }
        }

        [SupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("browser")]
        private static IEnumerable<Measurement<double>> GetCpuTime()
        {
            Debug.Assert(s_processCpuTime is not null);
            Debug.Assert(!OperatingSystem.IsBrowser() && !OperatingSystem.IsTvOS() && !(OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()));

            Environment.ProcessCpuUsage processCpuUsage = Environment.CpuUsage;

            yield return new(processCpuUsage.UserTime.TotalSeconds, [new KeyValuePair<string, object?>("cpu.mode", "user")]);
            yield return new(processCpuUsage.PrivilegedTime.TotalSeconds, [new KeyValuePair<string, object?>("cpu.mode", "system")]);
        }

        private static IEnumerable<Measurement<long>> GetHeapSizes()
        {
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

            if (gcInfo.Index == 0)
                yield break;

            for (int i = 0; i < s_maxGenerations; ++i)
            {
                yield return new(gcInfo.GenerationInfo[i].SizeAfterBytes, new KeyValuePair<string, object?>("gc.heap.generation", s_genNames[i]));
            }
        }

        private static IEnumerable<Measurement<long>> GetHeapFragmentation()
        {
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

            if (gcInfo.Index == 0)
                yield break;

            for (int i = 0; i < s_maxGenerations; ++i)
            {
                yield return new(gcInfo.GenerationInfo[i].FragmentationAfterBytes, new KeyValuePair<string, object?>("gc.heap.generation", s_genNames[i]));
            }
        }
    }
}
