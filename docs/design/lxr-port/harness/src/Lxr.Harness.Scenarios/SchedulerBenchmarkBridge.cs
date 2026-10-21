// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Lxr.Harness.Scenarios;

internal sealed class SchedulerBenchmarkBridge : IDisposable
{
    private readonly nint _library;
    private readonly nint _handle;
    private readonly RunCallback _run;
    private readonly GetMetricsCallback _getMetrics;
    private readonly DestroyCallback _destroy;
    private readonly int _mode;
    private bool _disposed;

    public SchedulerBenchmarkBridge(
        string libraryPath,
        int workerCount,
        int producerCount,
        int referenceCapacity,
        int mode)
    {
        _mode = mode;
        _library = NativeLibrary.Load(libraryPath);
        try
        {
            CreateCallback create = GetExport<CreateCallback>("P31_Benchmark_Create");
            _run = GetExport<RunCallback>("P31_Benchmark_Run");
            _getMetrics = GetExport<GetMetricsCallback>("P31_Benchmark_GetMetrics");
            _destroy = GetExport<DestroyCallback>("P31_Benchmark_Destroy");
            _handle = create(
                (nuint)workerCount,
                (nuint)producerCount,
                (nuint)referenceCapacity,
                (uint)mode);
            if (_handle == 0)
            {
                throw new InvalidOperationException("The native P3.1 scheduler benchmark rejected its configuration.");
            }
        }
        catch
        {
            NativeLibrary.Free(_library);
            throw;
        }
    }

    public ulong Run(int producerIndex, ulong sequence)
    {
        ulong result = _run(_handle, (nuint)producerIndex, sequence, out int status);
        if (status != 0)
        {
            throw new InvalidOperationException($"The native P3.1 scheduler benchmark failed with status {status}.");
        }
        return result;
    }

    public string Verify(long operations, List<string> violations)
    {
        if (_getMetrics(_handle, out NativeMetrics metrics) != 0)
        {
            violations.Add("the native scheduler benchmark did not return its metrics");
            return "schedulerMetrics=unavailable";
        }

        bool scheduled = _mode != 0;
        ulong expectedScheduledOperations = _mode == 4 ?
            checked((ulong)operations * 2) :
            (ulong)operations;
        if (scheduled)
        {
            if (metrics.Submitted != expectedScheduledOperations ||
                metrics.Completed != expectedScheduledOperations ||
                metrics.SchedulerSubmitted != expectedScheduledOperations ||
                metrics.SchedulerExecuted != expectedScheduledOperations)
            {
                violations.Add(
                    $"native scheduler counts differ: operations={operations}, " +
                    $"submitted={metrics.Submitted}, completed={metrics.Completed}, " +
                    $"schedulerSubmitted={metrics.SchedulerSubmitted}, " +
                    $"schedulerExecuted={metrics.SchedulerExecuted}");
            }
            if (metrics.OwnershipViolations != 0)
            {
                violations.Add($"native scheduler reported {metrics.OwnershipViolations} ownership violations");
            }
        }
        else if (metrics.Submitted != 0 ||
                 metrics.Completed != 0 ||
                 metrics.SchedulerSubmitted != 0 ||
                 metrics.SchedulerExecuted != 0)
        {
            violations.Add("the direct scheduler control unexpectedly queued work");
        }

        double aaRatio = metrics.AaCountA == 0 || metrics.AaCountB == 0 ?
            double.NaN :
            (metrics.AaTicksB / (double)metrics.AaCountB) /
                (metrics.AaTicksA / (double)metrics.AaCountA);
        return
            $"schedulerMode={_mode}, schedulerSubmitted={metrics.SchedulerSubmitted}, " +
            $"schedulerExecuted={metrics.SchedulerExecuted}, steals={metrics.Stolen}, " +
            $"spills={metrics.Spilled}, parks={metrics.Parks}, wakes={metrics.Wakes}, " +
            $"aaRatio={aaRatio.ToString("R", CultureInfo.InvariantCulture)}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _destroy(_handle);
        NativeLibrary.Free(_library);
    }

    private T GetExport<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library, name));

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMetrics
    {
        public ulong Submitted;
        public ulong Completed;
        public ulong SchedulerSubmitted;
        public ulong SchedulerExecuted;
        public ulong Stolen;
        public ulong Spilled;
        public ulong OwnershipViolations;
        public ulong Parks;
        public ulong Wakes;
        public ulong AaTicksA;
        public ulong AaTicksB;
        public ulong AaCountA;
        public ulong AaCountB;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CreateCallback(
        nuint workerCount,
        nuint producerCount,
        nuint referenceCapacity,
        uint mode);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong RunCallback(
        nint handle,
        nuint producerIndex,
        ulong sequence,
        out int status);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetMetricsCallback(nint handle, out NativeMetrics metrics);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DestroyCallback(nint handle);
}
