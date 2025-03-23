﻿// Licensed to the .NET Foundation under one or more agreements.
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

        private static readonly Counter<long> s_exceptions;

        public static void EnsureInitialized()
        {
            // Dummy method to ensure that the static constructor run and created the meters
        }

        static RuntimeMetrics()
        {
            s_meter.CreateObservableCounter(
                "dotnet.gc.collections",
                GetGarbageCollectionCounts,
                unit: "{collection}",
                description: "The number of garbage collections that have occurred since the process has started.");

            s_meter.CreateObservableUpDownCounter(
                "dotnet.process.memory.working_set",
                () => Environment.WorkingSet,
                unit: "By",
                description: "The number of bytes of physical memory mapped to the process context.");

            s_meter.CreateObservableCounter(
                "dotnet.gc.heap.total_allocated",
                () => GC.GetTotalAllocatedBytes(),
                unit: "By",
                description: "The approximate number of bytes allocated on the managed GC heap since the process has started. The returned value does not include any native allocations.");

            s_meter.CreateObservableUpDownCounter(
                "dotnet.gc.last_collection.memory.committed_size",
                () => GC.GetGCMemoryInfo().TotalCommittedBytes,
                unit: "By",
                description: "The amount of committed virtual memory in use by the .NET GC, as observed during the latest garbage collection.");

            s_meter.CreateObservableUpDownCounter(
                "dotnet.gc.last_collection.heap.size",
                GetHeapSizes,
                unit: "By",
                description: "The managed GC heap size (including fragmentation), as observed during the latest garbage collection.");

            s_meter.CreateObservableUpDownCounter(
                "dotnet.gc.last_collection.heap.fragmentation.size",
                GetHeapFragmentation,
                unit: "By",
                description: "The heap fragmentation, as observed during the latest garbage collection.");

            s_meter.CreateObservableCounter(
                "dotnet.gc.pause.time",
                () => GC.GetTotalPauseDuration().TotalSeconds,
                unit: "s",
                description: "The total amount of time paused in GC since the process has started.");

            s_meter.CreateObservableCounter(
                "dotnet.jit.compiled_il.size",
                () => Runtime.JitInfo.GetCompiledILBytes(),
                unit: "By",
                description: "Count of bytes of intermediate language that have been compiled since the process has started.");

            s_meter.CreateObservableCounter(
                "dotnet.jit.compiled_methods",
                () => Runtime.JitInfo.GetCompiledMethodCount(),
                unit: "{method}",
                description: "The number of times the JIT compiler (re)compiled methods since the process has started.");

            s_meter.CreateObservableCounter(
                "dotnet.jit.compilation.time",
                () => Runtime.JitInfo.GetCompilationTime().TotalSeconds,
                unit: "s",
                description: "The number of times the JIT compiler (re)compiled methods since the process has started.");

            s_meter.CreateObservableCounter(
                "dotnet.monitor.lock_contentions",
                () => Monitor.LockContentionCount,
                unit: "{contention}",
                description: "The number of times there was contention when trying to acquire a monitor lock since the process has started.");

            s_meter.CreateObservableCounter(
                "dotnet.thread_pool.thread.count",
                () => (long)ThreadPool.ThreadCount,
                unit: "{thread}",
                description: "The number of thread pool threads that currently exist.");

            s_meter.CreateObservableCounter(
                "dotnet.thread_pool.work_item.count",
                () => ThreadPool.CompletedWorkItemCount,
                unit: "{work_item}",
                description: "The number of work items that the thread pool has completed since the process has started.");

            s_meter.CreateObservableCounter(
                "dotnet.thread_pool.queue.length",
                () => ThreadPool.PendingWorkItemCount,
                unit: "{work_item}",
                description: "The number of work items that are currently queued to be processed by the thread pool.");

            s_meter.CreateObservableUpDownCounter(
                "dotnet.timer.count",
                () => Timer.ActiveCount,
                unit: "{timer}",
                description: "The number of timer instances that are currently active. An active timer is registered to tick at some point in the future and has not yet been canceled.");

            s_meter.CreateObservableUpDownCounter(
                "dotnet.assembly.count",
                () => (long)AppDomain.CurrentDomain.GetAssemblies().Length,
                unit: "{assembly}",
                description: "The number of .NET assemblies that are currently loaded.");

            s_exceptions = s_meter.CreateCounter<long>(
                "dotnet.exceptions",
                unit: "{exception}",
                description: "The number of exceptions that have been thrown in managed code.");

            AppDomain.CurrentDomain.FirstChanceException += (source, e) =>
            {
                // Avoid recursion if the listener itself throws an exception while recording the measurement
                // in its `OnMeasurementRecorded` callback.
                if (t_handlingFirstChanceException) return;
                t_handlingFirstChanceException = true;
                s_exceptions.Add(1, new KeyValuePair<string, object?>("error.type", e.Exception.GetType().Name));
                t_handlingFirstChanceException = false;
            };

            s_meter.CreateObservableUpDownCounter(
                "dotnet.process.cpu.count",
                () => (long)Environment.ProcessorCount,
                unit: "{cpu}",
                description: "The number of processors available to the process.");

            if (!OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi() && !OperatingSystem.IsTvOS() && !(OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()))
            {
                s_meter.CreateObservableCounter(
                    "dotnet.process.cpu.time",
                    GetCpuTime,
                    unit: "s",
                    description: "CPU time used by the process.");
            }
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

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("browser")]
        [SupportedOSPlatform("maccatalyst")]
        private static IEnumerable<Measurement<double>> GetCpuTime()
        {
            Debug.Assert(!OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi() &&!OperatingSystem.IsTvOS() && !(OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()));

            Environment.ProcessCpuUsage processCpuUsage = Environment.CpuUsage;

            yield return new(processCpuUsage.UserTime.TotalSeconds, [new KeyValuePair<string, object?>("cpu.mode", "user")]);
            yield return new(processCpuUsage.PrivilegedTime.TotalSeconds, [new KeyValuePair<string, object?>("cpu.mode", "system")]);
        }

        private static IEnumerable<Measurement<long>> GetHeapSizes()
        {
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

            for (int i = 0; i < s_maxGenerations; ++i)
            {
                yield return new(gcInfo.GenerationInfo[i].SizeAfterBytes, new KeyValuePair<string, object?>("gc.heap.generation", s_genNames[i]));
            }
        }

        private static IEnumerable<Measurement<long>> GetHeapFragmentation()
        {
            GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();

            for (int i = 0; i < s_maxGenerations; ++i)
            {
                yield return new(gcInfo.GenerationInfo[i].FragmentationAfterBytes, new KeyValuePair<string, object?>("gc.heap.generation", s_genNames[i]));
            }
        }
    }
}
