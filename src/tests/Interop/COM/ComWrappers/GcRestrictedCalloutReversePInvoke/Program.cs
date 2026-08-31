// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ComWrappersTests.Common;
using Xunit;
using TestLibrary;

public class Program
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    [SkipOnCoreClr("This test is not compatible with GC stress.", RuntimeTestModes.AnyGCStress)]
    public static void TestEntryPoint()
    {
        ComWrappers.RegisterForTrackerSupport(TrackerComWrappers.Instance);

        IntPtr trackerObject = MockReferenceTrackerRuntime.CreateTrackerObject();
        object wrapper = TrackerComWrappers.Instance.GetOrCreateObjectForComInstance(trackerObject, CreateObjectFlags.TrackerObject);
        Marshal.Release(trackerObject);

        // dotnet/runtime#110683
        // Before the runtime fix, assertion-enabled NativeAOT runtimes can fail with
        // ASSERT(ThreadStore::IsTrapThreadsRequested()) when this managed GC restricted
        // callout is reverse-invoked on a background GC thread.
        ForceBackgroundGen2Collection(timeout: TimeSpan.FromSeconds(30));
        GC.KeepAlive(wrapper);
    }

    private static void ForceBackgroundGen2Collection(TimeSpan timeout)
    {
        long initialBackgroundGcIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;
        using AllocationPressure pressure = new();
        pressure.Start();

        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: false);

            if (SpinWait.SpinUntil(() => GC.GetGCMemoryInfo(GCKind.Background).Index > initialBackgroundGcIndex, TimeSpan.FromMilliseconds(500)))
            {
                return;
            }
        }

        long finalBackgroundGcIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;
        throw new Exception($"Timed out after {timeout} waiting for a background GC. Initial index: {initialBackgroundGcIndex}, final index: {finalBackgroundGcIndex}.");
    }
}

sealed class AllocationPressure : IDisposable
{
    private readonly ManualResetEventSlim _stopSignal = new(initialState: false);
    private readonly Thread _allocatorThread;

    public AllocationPressure()
    {
        _allocatorThread = new Thread(AllocateUntilStopped)
        {
            IsBackground = true,
            Name = "GcRestrictedCalloutReversePInvoke_AllocationPressure"
        };
    }

    public void Start() => _allocatorThread.Start();

    public void Stop()
    {
        _stopSignal.Set();
        if (!_allocatorThread.Join(millisecondsTimeout: 5000))
        {
            throw new Exception("Allocation pressure thread did not stop in time.");
        }
    }

    private void AllocateUntilStopped()
    {
        byte[][] ring = new byte[64][];
        int index = 0;

        while (!_stopSignal.IsSet)
        {
            ring[index] = new byte[256 * 1024];
            index = (index + 1) % ring.Length;
        }
    }

    public void Dispose()
    {
        Stop();
        _stopSignal.Dispose();
    }
}

sealed class TrackerComWrappers : ComWrappers
{
    public static TrackerComWrappers Instance { get; } = new();

    protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        count = 0;
        return null;
    }

    protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags) => new object();

    protected override void ReleaseObjects(IEnumerable objects)
    {
    }
}
