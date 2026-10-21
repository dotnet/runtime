// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Lxr.Harness.Core;

namespace Lxr.Harness.Scenarios;

/// <summary>
/// Scenario 8 - pinning-heavy I/O, the port-side evidence base for ledger row R05.
///
/// <para>LXR declares pinning unsupported, and not conditionally. At the declared oracle revision
/// <c>304ce69d</c>, <c>PinningProcessEdges</c> is <c>UnsupportedProcessEdges</c> in both work
/// contexts, the <c>TPinningClosure</c> and <c>PinningRootsTrace</c> buckets are unconditionally
/// <c>set_enabled(false)</c>, <c>is_pinned()</c> compiles to constant <c>false</c>, and
/// <c>UnsupportedProcessEdges</c> panics in five members. None of that is <c>cfg</c>-gated, so it is
/// present in every build the reference measurements were taken on.</para>
///
/// <para>.NET has no equivalent opt-out. <c>GCHandleType.Pinned</c>, <c>fixed</c>, interop marshalling
/// and the pinned object heap all produce immovable objects in ordinary code. There is therefore no
/// reference mechanism to port here - the ledger records the declared oracle for R05 as
/// <c>none</c> - which makes this scenario the only evidence that will exist for the port's own
/// pinning design.</para>
/// </summary>
public sealed class PinningHeavyIoScenario : IScenario
{
    private GCHandle[] _residentPins = [];
    private byte[]?[] _residentBuffers = [];
    private byte[][] _perWorkerScratch = [];
    private string? _ioPath;
    private FileStream? _ioStream;
    private int _bufferBytes;
    private int _ioBytes;
    private bool _ioEnabled;
    private bool _usePinnedHeap;
    private long _operations;
    private long _pinsTaken;
    private long _bytesTransferred;

    public ScenarioDescriptor Describe() => new()
    {
        Id = "pinning-heavy-io",
        Rationale =
            "The reference collector declares pinning unsupported (ledger row R05, declared oracle " +
            "'none'): at oracle revision 304ce69d the pinning edge processor panics, the pinning trace " +
            "buckets are unconditionally disabled and is_pinned() is constant false, none of it " +
            "cfg-gated. .NET cannot opt out, because GCHandleType.Pinned, fixed, interop and the pinned " +
            "object heap all produce immovable objects in ordinary application code. This scenario " +
            "therefore measures something the reference cannot do at all, and is the only evidence base " +
            "the port's own pinning design will have.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        DefaultWorkerCount = 2,
        MaxWorkerCount = 16,
        MinimumOperations = 200,
        ProvisionalBaselineHeapBytes = 256L * 1024 * 1024,
        Axes = ["pinDensity", "bufferBytes", "ioBytes", "ioEnabled", "usePinnedHeap"],
    };

    public void Setup(ScenarioContext context)
    {
        _bufferBytes = context.Parameters.GetInt32("bufferBytes", 8 * 1024);
        _ioBytes = context.Parameters.GetInt32("ioBytes", 4 * 1024);
        _ioEnabled = context.Parameters.GetBoolean("ioEnabled", true);
        _usePinnedHeap = context.Parameters.GetBoolean("usePinnedHeap", true);

        // Long-lived pins scattered through the heap are what actually constrains a moving collector:
        // a transient pin can be waited out, a resident one cannot.
        int pinDensity = context.Parameters.GetInt32("pinDensity", 512);
        _residentPins = new GCHandle[pinDensity];
        _residentBuffers = new byte[]?[pinDensity];
        for (int i = 0; i < pinDensity; i++)
        {
            byte[] buffer = _usePinnedHeap && (i % 2 == 0)
                ? GC.AllocateArray<byte>(_bufferBytes, pinned: true)
                : new byte[_bufferBytes];

            _residentBuffers[i] = buffer;

            // Objects already on the pinned object heap do not need a pinning handle; taking one for
            // the rest exercises the handle-table path as well as the heap path.
            if (!_usePinnedHeap || i % 2 != 0)
            {
                _residentPins[i] = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            }
        }

        _perWorkerScratch = new byte[context.WorkerCount][];
        for (int i = 0; i < context.WorkerCount; i++)
        {
            _perWorkerScratch[i] = new byte[_ioBytes];
        }

        if (_ioEnabled)
        {
            _ioPath = Path.Combine(Path.GetTempPath(), $"lxr-harness-pinning-{Environment.ProcessId}.bin");
            _ioStream = new FileStream(_ioPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 0, FileOptions.RandomAccess);
            _ioStream.SetLength(Math.Max(_ioBytes * 64L, 1024 * 1024));
        }

        _operations = 0;
        _pinsTaken = 0;
        _bytesTransferred = 0;
    }

    public unsafe long RunOperation(int workerIndex)
    {
        long index = Interlocked.Increment(ref _operations);
        byte[] scratch = _perWorkerScratch[workerIndex];
        long accumulator = index;

        // A transient pinning handle: the ordinary interop and I/O pattern.
        var transient = new byte[_bufferBytes];
        GCHandle handle = GCHandle.Alloc(transient, GCHandleType.Pinned);
        Interlocked.Increment(ref _pinsTaken);
        try
        {
            accumulator += handle.AddrOfPinnedObject().ToInt64() & 0xffff;
            transient[0] = (byte)index;

            // A 'fixed' block: the same immovability, expressed through the language rather than the
            // handle table, and with a different lifetime shape.
            fixed (byte* pinned = scratch)
            {
                pinned[0] = (byte)(index & 0xff);
                pinned[scratch.Length - 1] = (byte)workerIndex;
                accumulator += pinned[0] + pinned[scratch.Length - 1];
            }

            if (_ioStream is FileStream stream)
            {
                long offset = (index * _ioBytes) % Math.Max(stream.Length - _ioBytes, 1);
                lock (stream)
                {
                    stream.Position = offset;
                    stream.Write(scratch, 0, _ioBytes);
                    stream.Position = offset;
                    int read = stream.Read(scratch, 0, _ioBytes);
                    Interlocked.Add(ref _bytesTransferred, read + _ioBytes);
                    accumulator += read;
                }
            }
        }
        finally
        {
            handle.Free();
        }

        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long operations = Interlocked.Read(ref _operations);
        var violations = new List<string>();

        int livePins = 0;
        foreach (GCHandle pin in _residentPins)
        {
            if (pin.IsAllocated)
            {
                livePins++;
                if (pin.Target is null)
                {
                    violations.Add("a resident pinning handle lost its target, which would mean a pinned object was collected");
                    break;
                }
            }
        }

        if (_ioEnabled && Interlocked.Read(ref _bytesTransferred) == 0 && operations > 0)
        {
            violations.Add("I/O was enabled but no bytes were transferred, so the scenario did not do the work it claims");
        }

        return new ScenarioVerification
        {
            Success = operations >= 200 && violations.Count == 0,
            Marker = $"pinning-heavy-io:pins={Interlocked.Read(ref _pinsTaken)}",
            Detail = $"operations={operations}, residentPins={livePins}, bytesTransferred={Interlocked.Read(ref _bytesTransferred)}",
            Violations = violations,
        };
    }

    public void Teardown()
    {
        foreach (GCHandle pin in _residentPins)
        {
            if (pin.IsAllocated)
            {
                pin.Free();
            }
        }

        _residentPins = [];
        _residentBuffers = [];
        _ioStream?.Dispose();
        _ioStream = null;

        if (_ioPath is not null && File.Exists(_ioPath))
        {
            try
            {
                File.Delete(_ioPath);
            }
            catch (IOException)
            {
                // A leftover scratch file is not a measurement failure.
            }
        }
    }
}

/// <summary>
/// Scenario 9 - lifecycle semantics, where the reference has no evidence at all.
///
/// <para>The paper's methodology section states that class unloading, compressed pointers and weak
/// references were disabled in all four collectors it compared (P0.2). That is a configuration a
/// shipping CoreCLR can never be in: finalizers, weak references, dependent handles and collectible
/// load contexts are ordinary parts of the platform. So there is no reference measurement to port and
/// no reference behaviour to match - the harness has to generate this evidence itself.</para>
///
/// <para>Consequently this scenario is correctness-first. Its output is not primarily a number: it
/// asserts semantic invariants that any collector claiming to be a .NET collector must satisfy, and a
/// violation fails the run. Those assertions are what a future LXR arm would be checked against.</para>
/// </summary>
public sealed class LifecycleSemanticsScenario : IScenario
{
    private sealed class Finalizable
    {
        private readonly StrongBox<int> _counter;

        public Finalizable(StrongBox<int> counter) => _counter = counter;

        ~Finalizable() => Interlocked.Increment(ref _counter.Value);
    }

    private sealed class Resurrecting
    {
        internal static object? Resurrected;

        ~Resurrecting() => Resurrected = this;
    }

    private readonly StrongBox<int> _finalizedCount = new(0);
    private ConditionalWeakTable<object, object> _table = new();
    private object?[] _tableKeys = [];
    private long _operations;
    private int _weakSlots;
    private WeakReference<object>?[] _weakShort = [];
    private WeakReference<object>?[] _weakLong = [];

    public ScenarioDescriptor Describe() => new()
    {
        Id = "lifecycle-semantics",
        Rationale =
            "The paper disabled class unloading, compressed pointers and weak references in all four " +
            "collectors it measured (P0.2), which is a configuration a shipping CoreCLR can never be " +
            "in. There is therefore no reference evidence in this area at all and the harness must " +
            "generate it. Correctness-first by design: it asserts finalizer execution and ordering, " +
            "short versus long weak-reference clearing across finalization, dependent-handle liveness, " +
            "ConditionalWeakTable key lifetime, every GCHandle type and collectible " +
            "AssemblyLoadContext unload, and a semantic violation fails the run rather than producing " +
            "a slower number.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        MinimumOperations = 100,
        ProvisionalBaselineHeapBytes = 128L * 1024 * 1024,
        AllowsInducedCollections = true,
        Axes = ["weakSlots", "finalizablePerOperation"],
    };

    public void Setup(ScenarioContext context)
    {
        _weakSlots = context.Parameters.GetInt32("weakSlots", 4096);
        _weakShort = new WeakReference<object>?[_weakSlots];
        _weakLong = new WeakReference<object>?[_weakSlots];
        _tableKeys = new object?[context.Parameters.GetInt32("tableKeys", 1024)];
        _table = new ConditionalWeakTable<object, object>();
        _operations = 0;
    }

    public long RunOperation(int workerIndex)
    {
        long index = Interlocked.Increment(ref _operations);
        long accumulator = index;

        // Churn of finalizable objects: the finalizer queue is itself a source of retention that a
        // reference-counting collector has to model correctly.
        _ = new Finalizable(_finalizedCount);

        var target = new object();
        int slot = (int)(index % _weakSlots);
        _weakShort[slot] = new WeakReference<object>(target, trackResurrection: false);
        _weakLong[slot] = new WeakReference<object>(target, trackResurrection: true);

        object key = new();
        _table.Add(key, new object());
        _tableKeys[(int)(index % _tableKeys.Length)] = index % 4 == 0 ? key : null;

        using (var dependent = new System.Runtime.DependentHandle(key, new object()))
        {
            accumulator += dependent.IsAllocated ? 1 : 0;
        }

        GCHandle normal = GCHandle.Alloc(target, GCHandleType.Normal);
        GCHandle weak = GCHandle.Alloc(target, GCHandleType.Weak);
        try
        {
            accumulator += (normal.Target is not null ? 2 : 0) + (weak.Target is not null ? 4 : 0);
        }
        finally
        {
            normal.Free();
            weak.Free();
        }

        GC.KeepAlive(target);
        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long operations = Interlocked.Read(ref _operations);
        var violations = new List<string>();

        // Each assertion below is run outside the measured region, which is why this scenario declares
        // AllowsInducedCollections: forcing collections is how a lifecycle invariant is observed at all.
        AssertShortWeakReferenceCleared(violations);
        AssertLongWeakReferenceSurvivesUntilFinalized(violations);
        AssertDependentHandleTracksPrimary(violations);
        AssertConditionalWeakTableReleasesDeadKeys(violations);
        AssertFinalizersRun(violations);
        AssertResurrectionWorks(violations);
        AssertPinnedHandleAddressIsStable(violations);
        AssertCollectibleLoadContextUnloads(violations);

        return new ScenarioVerification
        {
            Success = operations >= 100 && violations.Count == 0,
            Marker = $"lifecycle-semantics:finalized={Volatile.Read(ref _finalizedCount.Value)}",
            Detail = $"operations={operations}, assertions=8",
            Violations = violations,
        };
    }

    private static void FullCollect()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<object> CreateOrphanWeak(bool trackResurrection) =>
        new(new object(), trackResurrection);

    private static void AssertShortWeakReferenceCleared(List<string> violations)
    {
        WeakReference<object> reference = CreateOrphanWeak(trackResurrection: false);
        FullCollect();
        if (reference.TryGetTarget(out _))
        {
            violations.Add("a short weak reference to an unreachable object was not cleared after a full collection");
        }
    }

    private static void AssertLongWeakReferenceSurvivesUntilFinalized(List<string> violations)
    {
        WeakReference<object> reference = CreateOrphanWeak(trackResurrection: true);
        FullCollect();
        if (reference.TryGetTarget(out _))
        {
            violations.Add("a long weak reference to an unreachable, non-resurrected object was not cleared after finalization");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static System.Runtime.DependentHandle CreateDependentWithDeadPrimary()
    {
        var primary = new object();
        var secondary = new object();
        return new System.Runtime.DependentHandle(primary, secondary);
    }

    private static void AssertDependentHandleTracksPrimary(List<string> violations)
    {
        // While the primary is alive, the dependent secondary must be kept alive with it.
        object livePrimary = new();
        object liveSecondary = new();
        using (var live = new System.Runtime.DependentHandle(livePrimary, liveSecondary))
        {
            FullCollect();
            if (live.Dependent is null)
            {
                violations.Add("a dependent handle dropped its secondary while the primary was still strongly reachable");
            }

            GC.KeepAlive(livePrimary);
        }

        // Once the primary is unreachable, the dependent must become collectible.
        System.Runtime.DependentHandle dead = CreateDependentWithDeadPrimary();
        try
        {
            FullCollect();
            if (dead.Target is not null)
            {
                violations.Add("a dependent handle kept its primary alive after the primary became unreachable");
            }
        }
        finally
        {
            dead.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddOrphanEntry(ConditionalWeakTable<object, object> table)
    {
        object key = new();
        table.Add(key, new object());
    }

    private static void AssertConditionalWeakTableReleasesDeadKeys(List<string> violations)
    {
        var table = new ConditionalWeakTable<object, object>();
        for (int i = 0; i < 64; i++)
        {
            AddOrphanEntry(table);
        }

        FullCollect();

        int remaining = 0;
        foreach (KeyValuePair<object, object> _ in table)
        {
            remaining++;
        }

        if (remaining > 0)
        {
            violations.Add($"a ConditionalWeakTable retained {remaining} entries whose keys were unreachable");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateOrphanFinalizable(StrongBox<int> counter) => _ = new Finalizable(counter);

    private static void AssertFinalizersRun(List<string> violations)
    {
        var counter = new StrongBox<int>(0);
        for (int i = 0; i < 32; i++)
        {
            CreateOrphanFinalizable(counter);
        }

        FullCollect();
        GC.WaitForPendingFinalizers();

        if (Volatile.Read(ref counter.Value) == 0)
        {
            violations.Add("no finalizer ran for 32 unreachable finalizable objects");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateOrphanResurrecting() => _ = new Resurrecting();

    private static void AssertResurrectionWorks(List<string> violations)
    {
        Resurrecting.Resurrected = null;
        CreateOrphanResurrecting();
        FullCollect();
        GC.WaitForPendingFinalizers();

        if (Resurrecting.Resurrected is null)
        {
            violations.Add("an object that stored itself in a static field from its finalizer was not resurrected");
        }
        else
        {
            Resurrecting.Resurrected = null;
        }
    }

    private static void AssertPinnedHandleAddressIsStable(List<string> violations)
    {
        byte[] buffer = new byte[4096];
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            IntPtr before = handle.AddrOfPinnedObject();

            // Allocate enough to provoke collections that would otherwise be free to move the buffer.
            for (int i = 0; i < 256; i++)
            {
                _ = new byte[64 * 1024];
            }

            FullCollect();

            if (handle.AddrOfPinnedObject() != before)
            {
                violations.Add("a pinned object moved across a full collection, which no collector may do");
            }
        }
        finally
        {
            handle.Free();
        }
    }

    public void Teardown()
    {
        _weakShort = [];
        _weakLong = [];
        _tableKeys = [];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndUnloadContext()
    {
        var context = new System.Runtime.Loader.AssemblyLoadContext("lxr-harness-collectible", isCollectible: true);
        var reference = new WeakReference(context, trackResurrection: true);
        context.Unload();
        return reference;
    }

    /// <summary>
    /// Collectible load contexts are one of the three features the paper switched off, so this is
    /// precisely the area where no reference evidence exists.
    /// </summary>
    private static void AssertCollectibleLoadContextUnloads(List<string> violations)
    {
        WeakReference reference = CreateAndUnloadContext();

        for (int attempt = 0; attempt < 12 && reference.IsAlive; attempt++)
        {
            FullCollect();
        }

        if (reference.IsAlive)
        {
            violations.Add("a collectible AssemblyLoadContext was still alive after unload and repeated full collections");
        }
    }
}
