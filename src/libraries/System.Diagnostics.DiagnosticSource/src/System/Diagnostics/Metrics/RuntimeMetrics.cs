// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if !NETFRAMEWORK && !NETSTANDARD
using System.Threading;
#endif

namespace System.Diagnostics.Metrics
{
    internal static class RuntimeMetrics
    {
        private const string MeterName = "System.Runtime";

        private static readonly Meter s_meter = new(MeterName);

        // These MUST align to the possible attribute values defined in the semantic conventions (TODO: link to the spec)
        private static readonly string[] s_genNames = ["gen0", "gen1", "gen2", "loh", "poh"];
#if !NETFRAMEWORK && !NETSTANDARD
        private static readonly int s_maxGenerations = Math.Min(GC.GetGCMemoryInfo().GenerationInfo.Length, s_genNames.Length);
#endif

        static RuntimeMetrics()
        {
            AppDomain.CurrentDomain.FirstChanceException += (source, e) =>
            {
                s_exceptionCount.Add(1, new KeyValuePair<string, object?>("error.type", e.Exception.GetType().Name));
            };
        }

        // GC Metrics

        private static readonly ObservableCounter<long> s_gcCollectionsCounter = s_meter.CreateObservableCounter(
            "dotnet.gc.collections.count",
            GetGarbageCollectionCounts,
            unit: "{collection}",
            description: "Number of garbage collections that have occurred since the process has started.");

        private static readonly ObservableUpDownCounter<long> s_gcObjectsSize = s_meter.CreateObservableUpDownCounter(
            "dotnet.gc.objects.size",
            () => GC.GetTotalMemory(forceFullCollection: false),
            unit: "By",
            description: "The number of bytes currently allocated on the managed GC heap. Fragmentation and other GC committed memory pools are excluded.");

#if !NETFRAMEWORK && !NETSTANDARD
        private static readonly ObservableCounter<long> s_gcMemoryTotalAllocated = s_meter.CreateObservableCounter(
            "dotnet.gc.memory.total_allocated",
            () => GC.GetTotalAllocatedBytes(),
            unit: "By",
            description: "The approximate number of bytes allocated on the managed GC heap since the process has started. The returned value does not include any native allocations.");

        private static readonly ObservableUpDownCounter<long> s_gcMemoryCommited = s_meter.CreateObservableUpDownCounter(
            "dotnet.gc.memory.commited",
            () =>
            {
                GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

                return gcInfo.Index == 0
                    ? Array.Empty<Measurement<long>>()
                    : [new(GC.GetGCMemoryInfo().TotalCommittedBytes)];
            },
            unit: "By",
            description: "The amount of committed virtual memory for the managed GC heap, as observed during the latest garbage collection.");

        private static readonly ObservableUpDownCounter<long> s_gcHeapSize = s_meter.CreateObservableUpDownCounter(
            "dotnet.gc.heap.size",
            GetHeapSizes,
            unit: "By",
            description: "The managed GC heap size (including fragmentation), as observed during the latest garbage collection.");

        private static readonly ObservableUpDownCounter<long> s_gcHeapFragmentation = s_meter.CreateObservableUpDownCounter(
            "dotnet.gc.heap.fragmentation",
            GetHeapFragmentation,
            unit: "By",
            description: "The heap fragmentation, as observed during the latest garbage collection.");

        private static readonly ObservableCounter<double> s_gcPauseTime = s_meter.CreateObservableCounter(
            "dotnet.gc.pause.time",
            () => GC.GetTotalPauseDuration().TotalSeconds,
            unit: "s",
            description: "The total amount of time paused in GC since the process has started.");

        // JIT Metrics

        private static readonly ObservableCounter<long> s_jitCompiledSize = s_meter.CreateObservableCounter(
            "dotnet.jit.compiled_il.size",
            () => Runtime.JitInfo.GetCompiledILBytes(),
            unit: "By",
            description: "Count of bytes of intermediate language that have been compiled since the process has started.");

        private static readonly ObservableCounter<long> s_jitCompiledMethodCount = s_meter.CreateObservableCounter(
            "dotnet.jit.compiled_method.count",
            () => Runtime.JitInfo.GetCompiledMethodCount(),
            unit: "{method}",
            description: "The number of times the JIT compiler (re)compiled methods since the process has started.");

        private static readonly ObservableCounter<double> s_jitCompilationTime = s_meter.CreateObservableCounter(
            "dotnet.jit.compilation.time",
            () => Runtime.JitInfo.GetCompilationTime().TotalSeconds,
            unit: "s",
            description: "The number of times the JIT compiler (re)compiled methods since the process has started.");

        // Monitor Metrics

        private static readonly ObservableCounter<long> s_monitorLockContention = s_meter.CreateObservableCounter(
            "dotnet.monitor.lock_contention.count",
            () => Monitor.LockContentionCount,
            unit: "s",
            description: "The number of times there was contention when trying to acquire a monitor lock since the process has started.");

        // Thread Pool Metrics

        //private static readonly ObservableCounter<long> s_threadPoolThreadCount = s_meter.CreateObservableCounter(
        //    "dotnet.thread_pool.thread_count",
        //    () => (long)ThreadPool.ThreadCount,
        //    unit: "{thread}",
        //    description: "The number of thread pool threads that currently exist.");

        // TODO

        // Timer Metrics

        private static readonly ObservableUpDownCounter<long> s_timerCount = s_meter.CreateObservableUpDownCounter(
            "dotnet.timer.count",
            () => Timer.ActiveCount,
            unit: "{timer}",
            description: "The number of timer instances that are currently active. An active timer is registered to tick at some point in the future and has not yet been canceled.");
#endif

        private static readonly ObservableUpDownCounter<long> s_assemblyCount = s_meter.CreateObservableUpDownCounter(
            "dotnet.assemblies.count",
            () => (long)AppDomain.CurrentDomain.GetAssemblies().Length,
            unit: "{assembly}",
            description: "The number of .NET assemblies that are currently loaded.");

        private static readonly Counter<long> s_exceptionCount = s_meter.CreateCounter<long>(
            "dotnet.exceptions.count",
            unit: "{exception}",
            description: "The number of exceptions that have been thrown in managed code.");

        public static bool IsEnabled()
        {
            return s_gcCollectionsCounter.Enabled
                || s_gcObjectsSize.Enabled
#if !NETFRAMEWORK && !NETSTANDARD
                || s_gcMemoryTotalAllocated.Enabled
                || s_gcMemoryCommited.Enabled
                || s_gcHeapSize.Enabled
                || s_gcHeapFragmentation.Enabled
                || s_gcPauseTime.Enabled
                || s_jitCompiledSize.Enabled
                || s_jitCompiledMethodCount.Enabled
                || s_jitCompilationTime.Enabled
                || s_monitorLockContention.Enabled
                || s_timerCount.Enabled
#endif
                || s_assemblyCount.Enabled
                || s_exceptionCount.Enabled;
        }

        private static IEnumerable<Measurement<long>> GetGarbageCollectionCounts()
        {
            long collectionsFromHigherGeneration = 0;

            for (int gen = 2; gen >= 0; --gen)
            {
                long collectionsFromThisGeneration = GC.CollectionCount(gen);
                yield return new(collectionsFromThisGeneration - collectionsFromHigherGeneration, new KeyValuePair<string, object?>("generation", s_genNames[gen]));
                collectionsFromHigherGeneration = collectionsFromThisGeneration;
            }
        }

#if !NETFRAMEWORK && !NETSTANDARD
        private static IEnumerable<Measurement<long>> GetHeapSizes()
        {
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

            if (gcInfo.Index == 0)
                yield break;

            for (int i = 0; i < s_maxGenerations; ++i)
            {
                yield return new(gcInfo.GenerationInfo[i].SizeAfterBytes, new KeyValuePair<string, object?>("generation", s_genNames[i]));
            }
        }

        private static IEnumerable<Measurement<long>> GetHeapFragmentation()
        {
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

            if (gcInfo.Index == 0)
                yield break;

            for (int i = 0; i < s_maxGenerations; ++i)
            {
                yield return new(gcInfo.GenerationInfo[i].FragmentationAfterBytes, new KeyValuePair<string, object?>("generation", s_genNames[i]));
            }
        }
#endif
    }
}
