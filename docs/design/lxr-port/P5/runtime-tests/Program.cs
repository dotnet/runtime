// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

internal static partial class Program
{
    private static bool s_syncBlockDiagnostic;
    private static bool s_storageDiagnostic;
    private static bool s_nativeAllocatorDiagnostic;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong GetMetric(uint metric);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int IsOwned(nint handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint GetIdentity();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int Collect(uint kind);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int HoldTrace(int hold);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint HandleAddress(nint handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint AddressExtent(nint address);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ArmScan(nint handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ArmLifetime(nint handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EnableStorage();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int StorageRange(uint component, out nuint start, out nuint bytes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int WaitForTrace(int timeout);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint NativeSize();

    [LibraryImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryWorkingSetEx(nint process, nint information, uint bytes);

    [LibraryImport("libc", EntryPoint = "mincore", SetLastError = true)]
    private static unsafe partial int Mincore(nint address, nuint length, byte* residency);

    [LibraryImport("libc", EntryPoint = "open_memstream", SetLastError = true)]
    private static unsafe partial nint OpenMemoryStream(nint* buffer, nuint* size);

    [LibraryImport("libc", EntryPoint = "malloc_info", SetLastError = true)]
    private static partial int MallocInfo(int options, nint stream);

    [LibraryImport("libc", EntryPoint = "fclose", SetLastError = true)]
    private static partial int CloseNativeStream(nint stream);

    private sealed class Node
    {
        internal Node? Next { get; set; }
        internal int Value { get; }

        internal Node(int value) => Value = value;
    }

    private sealed class Finalizable
    {
        internal static int s_runs;
        internal static Finalizable? s_resurrected;
        internal int Value { get; } = 73;
        private readonly bool _resurrect;

        internal Finalizable(bool resurrect) => _resurrect = resurrect;

        ~Finalizable()
        {
            Interlocked.Increment(ref s_runs);
            if (_resurrect)
            {
                Volatile.Write(ref s_resurrected, this);
            }
        }
    }

    private sealed class OrderedFinalizer
    {
        ~OrderedFinalizer() => s_finalizer_order[Interlocked.Increment(ref s_finalizer_position) - 1] = 1;
    }

    private sealed class OrderedCriticalFinalizer : CriticalFinalizerObject
    {
        ~OrderedCriticalFinalizer() => s_finalizer_order[Interlocked.Increment(ref s_finalizer_position) - 1] = 2;
    }

    private sealed unsafe class ProbeComWrappers : ComWrappers
    {
        protected override ComInterfaceEntry* ComputeVtables(object value, CreateComInterfaceFlags flags, out int count)
        {
            count = 0;
            return null;
        }

        protected override object CreateObject(nint value, CreateObjectFlags flags) =>
            throw new PlatformNotSupportedException("This probe creates only managed COM wrappers.");

        protected override void ReleaseObjects(IEnumerable values) =>
            throw new PlatformNotSupportedException("This probe does not import external COM objects.");
    }

    private static readonly ProbeComWrappers s_wrappers = new();
    private static readonly int[] s_finalizer_order = new int[68];
    private static int s_finalizer_position;
    private static int s_checks;

    private static void Check(bool condition, string message)
    {
        s_checks++;
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string Hash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Node>[] CreateCycles()
    {
        Node left = new(17);
        Node right = new(29);
        Node self = new(43);
        left.Next = right;
        right.Next = left;
        self.Next = self;
        WeakReference<Node>[] weak = [new(left), new(right), new(self)];
        GC.Collect();
        GC.KeepAlive(left);
        GC.KeepAlive(self);
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Node> CreateSaturation()
    {
        Node value = new(61);
        Node[] repeated = new Node[512];
        Array.Fill(repeated, value);
        WeakReference<Node> weak = new(value);
        GC.Collect();
        GC.KeepAlive(repeated);
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference<Finalizable>, WeakReference<Finalizable>) CreateFinalizable(bool resurrect)
    {
        Finalizable value = new(resurrect);
        return (new(value, trackResurrection: false), new(value, trackResurrection: true));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAcyclicGarbage(int count)
    {
        for (int index = 0; index < count; index++)
        {
            Node parent = new(index);
            parent.Next = new Node(index ^ 0x55);
            GC.KeepAlive(parent);
        }
    }

    private static void ForceFull()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void VerifyCycles(GetMetric metric)
    {
        ulong cyclesBefore = metric(19);
        WeakReference<Node>[] cycles = CreateCycles();
        WeakReference<Node> saturation = CreateSaturation();
        ForceFull();
        foreach (WeakReference<Node> weak in cycles)
        {
            Check(!weak.TryGetTarget(out _), "An unreachable cycle survived completed full traces.");
        }
        Check(!saturation.TryGetTarget(out _), "An unreachable saturated object survived a full trace.");
        Check(metric(19) >= cyclesBefore + 4, "Cycle/sticky reclamation did not reach the real managed objects.");

        Node alive = new(101);
        alive.Next = new Node(103) { Next = alive };
        for (int epoch = 0; epoch < 8; epoch++)
        {
            CreateAcyclicGarbage(2048);
            ForceFull();
            Check(alive.Value == 101 && alive.Next.Value == 103 &&
                ReferenceEquals(alive.Next.Next, alive), "A reachable managed cycle was corrupted.");
        }
        ulong plateau = metric(3);
        for (int epoch = 0; epoch < 8; epoch++)
        {
            CreateAcyclicGarbage(2048);
            ForceFull();
        }
        Check(metric(3) <= plateau + 262144, "Live owned heap did not plateau under repeated acyclic churn.");
        GC.KeepAlive(alive);
    }

    private static void VerifyPinnedAndInterior()
    {
        byte[] buffer = new byte[100000];
        for (int index = 0; index < buffer.Length; index++)
        {
            buffer[index] = (byte)(index * 17 + 3);
        }
        GCHandle pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            nint before = pin.AddrOfPinnedObject();
            ref byte interior = ref buffer[8193];
            ForceFull();
            Check(pin.AddrOfPinnedObject() == before && interior == (byte)((8193 * 17 + 3) & 255),
                "Pinned/interior roots did not preserve a real nonmoving LOS allocation.");
            for (int index = 0; index < buffer.Length; index++)
            {
                Check(buffer[index] == (byte)(index * 17 + 3), "Pinned LOS checksum mismatch.");
            }
        }
        finally
        {
            pin.Free();
        }
        GC.KeepAlive(buffer);
    }

    private static void VerifyFinalization()
    {
        int before = Volatile.Read(ref Finalizable.s_runs);
        (WeakReference<Finalizable> shortWeak, WeakReference<Finalizable> longWeak) = CreateFinalizable(true);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Check(Volatile.Read(ref Finalizable.s_runs) == before + 1, "The finalizer did not execute exactly once.");
        Check(!shortWeak.TryGetTarget(out _), "A short weak reference tracked resurrection.");
        CheckResurrected(longWeak);
        Volatile.Write(ref Finalizable.s_resurrected, null);
        ForceFull();
        Check(!longWeak.TryGetTarget(out _), "The long weak reference was not cleared after resurrection ended.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckResurrected(WeakReference<Finalizable> weak)
    {
        Check(weak.TryGetTarget(out Finalizable? value) && value.Value == 73 &&
            ReferenceEquals(value, Volatile.Read(ref Finalizable.s_resurrected)),
            "Pending/resurrected object or long weak ordering was lost.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateFinalizerBatch()
    {
        for (int index = 0; index < 64; index++)
        {
            GC.KeepAlive(new OrderedFinalizer());
        }
        OrderedFinalizer suppressed = new();
        GC.SuppressFinalize(suppressed);
        OrderedFinalizer registered = new();
        GC.SuppressFinalize(registered);
        GC.ReRegisterForFinalize(registered);
        GC.KeepAlive(new OrderedCriticalFinalizer());
        GC.KeepAlive(suppressed);
        GC.KeepAlive(registered);
    }

    private static void VerifyFinalizerBatch()
    {
        CreateFinalizerBatch();
        ForceFull();
        Check(Volatile.Read(ref s_finalizer_position) == 66, "Suppression/re-registration lost or duplicated finalizers.");
        Check(s_finalizer_order[65] == 2, "Critical finalization ran before the normal queue drained.");
        for (int index = 0; index < 65; index++)
        {
            Check(s_finalizer_order[index] == 1, "The normal finalizer batch was not completely drained.");
        }
    }

    private static void VerifyDependentHandles()
    {
        ConditionalWeakTable<Node, Node> table = new();
        Node key = new(211);
        Node middle = new(223);
        table.Add(middle, new Node(227));
        table.Add(key, middle);
        ForceFull();
        Check(table.TryGetValue(key, out Node? value) && value.Value == 223,
            "A reachable dependent secondary was lost.");
        Check(table.TryGetValue(middle, out Node? last) && last.Value == 227,
            "Dependent fixed-point closure lost a chained secondary.");
        WeakReference<Node>[] dead = CreateDependentCycle(table);
        ForceFull();
        Check(!dead[0].TryGetTarget(out _) && !dead[1].TryGetTarget(out _),
            "An unreachable dependent key/value back-edge survived weak closure.");
        GC.KeepAlive(key);
        GC.KeepAlive(middle);
        GC.KeepAlive(table);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Node>[] CreateDependentCycle(ConditionalWeakTable<Node, Node> table)
    {
        Node key = new(229);
        Node value = new(233) { Next = key };
        table.Add(key, value);
        return [new(key), new(value)];
    }

    private static void VerifyThreadChurn(GetMetric metric)
    {
        object?[] destinations = new object?[2048];
        Array.Fill(destinations, new object());
        ForceFull();
        ulong flushes = metric(22);
        for (int index = 0; index < destinations.Length; index++)
        {
            int slot = index;
            Thread writer = new(() => destinations[slot] = null);
            writer.Start();
            writer.Join();
        }
        ForceFull();
        Check(metric(22) >= flushes + (ulong)destinations.Length,
            "Actual managed thread exits did not flush more-than-capacity sequential identities.");
        Check(Array.TrueForAll(destinations, static value => value is null), "Thread-churn stores were lost.");
        Check(metric(27) > 0, "No thread exercised a real barrier-side preallocated bootstrap before allocation.");
        GC.KeepAlive(destinations);
    }

    // P28 (F10). With counts of 2 bits a count of 3 is sticky, and a root counts at every pause before the root of the pause
    // before it is taken away. The graph is therefore held by the handle alone (no local variable of the caller is a second root
    // while the collection runs), so that the parent never reaches 3 and its death is an RC death.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GCHandle AllocateDeletionHandle()
    {
        return GCHandle.Alloc(new Node(307) { Next = new Node(311) { Next = new Node(313) } });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GCHandle CreateDeletionGraph()
    {
        GCHandle handle = AllocateDeletionHandle();
        ForceFull();
        return handle;
    }

    private static void VerifyHeldDeletion(GetMetric metric, Collect collect, HoldTrace hold)
    {
        GCHandle graph = CreateDeletionGraph();
        ulong marks = metric(16);
        Check(hold(1) == 0 && collect(2) == 0, "InitialMark could not arm a deterministic managed deletion race.");
        graph.Free();
        Check(collect(1) == 0, "The RC pause could not interrupt the managed snapshot.");
        long deadline = Environment.TickCount64 + 30000;
        while (metric(16) < marks + 3 && Environment.TickCount64 < deadline)
        {
            Thread.Sleep(1);
        }
        Check(metric(16) >= marks + 3, "Unmarked managed parent/children were not marked on deletion.");
        Check(collect(3) == 0, "FinalMark did not finish a held managed trace.");
        Check(metric(28) == 0, "Trace ownership did not retire after FinalMark.");
    }

    private static void VerifySatbOverflow(GetMetric metric, Collect collect, HoldTrace hold)
    {
        const int Count = 4097;
        object?[] slots = new object?[Count];
        for (int index = 0; index < slots.Length; index++)
        {
            slots[index] = new Node(index);
        }
        object failedComparand = new();
        ForceFull();
        ulong oldEntries = metric(15);
        Check(hold(1) == 0 && collect(2) == 0, "Managed SATB overflow trace did not start.");
        Thread writer = new(() =>
        {
            for (int index = 0; index < 1025; index++)
            {
                Interlocked.CompareExchange(ref slots[index], null, failedComparand);
            }
            Array.Clear(slots);
        });
        writer.Start();
        writer.Join();
        Check(collect(1) == 0 && collect(3) == 0, "Shared RC/SATB stream did not survive overflow and thread exit.");
        Check(metric(15) >= oldEntries + Count, "Real managed old-target SATB consumption missed a full/partial chunk.");
        Check(Array.TrueForAll(slots, static value => value is null), "Managed bulk clearing was corrupted.");
        GC.KeepAlive(slots);
    }

    private static void VerifyConcurrentMutation(
        GetMetric metric, Collect collect, HoldTrace hold, ArmScan arm, WaitForTrace wait,
        Action? duringTrace = null)
    {
        object?[] slots = new object?[100000];
        object before = new Node(401);
        object after = new Node(409);
        Array.Fill(slots, before);
        ForceFull();
        ulong scanned = metric(14);
        ulong concurrentSlices = metric(29);
        ulong interrupted = metric(31);
        ManualResetEventSlim begin = new(false);
        ManualResetEventSlim mutated = new(false);
        ManualResetEventSlim finish = new(false);
        Thread writer = new(() =>
        {
            begin.Wait();
            duringTrace?.Invoke();
            for (int index = 0; index < slots.Length; index++)
            {
                Volatile.Write(ref slots[index], after);
            }
            mutated.Set();
            finish.Wait();
        });
        writer.Start();
        GCHandle target = GCHandle.Alloc(slots);
        Check(arm(GCHandle.ToIntPtr(target)) == 0 &&
            hold(1) == 0 && collect(2) == 0 && hold(0) == 0, "Concurrent scan checkpoint could not be armed.");
        long deadline = Environment.TickCount64 + 30000;
        while (metric(30) == 0 && Environment.TickCount64 < deadline)
        {
            Thread.Sleep(1);
        }
        Check(metric(30) == 1 && metric(29) > concurrentSlices, "No actual concurrent scan reached its split checkpoint.");
        Check(wait(0) == unchecked((int)0x800705B4), "An unfinished trace wait did not honor its zero timeout.");
        begin.Set();
        Check(mutated.Wait(30000), "The concurrent writer did not finish its stores.");
        Check(metric(30) == 1, "Mutation did not overlap the live, partially scanned packet.");
        Check(collect(1) == 0, "A real RC pause could not interrupt concurrent tracing/mutation.");
        Check(metric(31) == interrupted + 1 && metric(30) == 0,
            "The pause did not interrupt and release the executing split packet.");
        finish.Set();
        writer.Join();
        GC.Collect();
        Check(wait(0) == 0, "Full collection did not complete the concurrent wait contract.");
        Check(Array.TrueForAll(slots, value => ReferenceEquals(value, after)),
            "Concurrent scanning or an interrupting pause corrupted managed array stores.");
        Check(metric(14) >= scanned + (ulong)slots.Length,
            "Real concurrent/split packet scans did not visit the array reference layout.");
        begin.Dispose();
        mutated.Dispose();
        finish.Dispose();
        target.Free();
        GC.KeepAlive(slots);
        GC.KeepAlive(before);
        GC.KeepAlive(after);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (object, FieldInfo) CreateCollectibleObject()
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("LxrCollectibleProbe"), AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule("Probe");
        TypeBuilder builder = module.DefineType("Probe", TypeAttributes.Public);
        builder.DefineField("Value", typeof(object), FieldAttributes.Public);
        Type type = builder.CreateType() ?? throw new InvalidOperationException("Collectible type creation failed.");
        FieldInfo field = type.GetField("Value") ?? throw new InvalidOperationException("Collectible field is missing.");
        object value = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Collectible birth failed.");
        field.SetValue(value, new Node(419));
        return (value, field);
    }

    private static void VerifyCollectibleLifetime()
    {
        (object value, FieldInfo field) = CreateCollectibleObject();
        ForceFull();
        Check(field.GetValue(value) is Node { Value: 419 },
            "Collectible MethodTable/LoaderAllocator or a real reflection-store edge was lost.");
        GC.KeepAlive(value);
        GC.KeepAlive(field);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (nint, WeakReference<Node>) CreateExternalWeakRoot()
    {
        Node value = new(431);
        return (s_wrappers.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None), new(value));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckExternalRoot(nint value)
    {
        Check(ComWrappers.TryGetObject(value, out object? target) && target is Node { Value: 431 },
            "An externally rooted ref-counted handle lost its managed object.");
    }

    private static void VerifyRefCountedRoot(Collect collect)
    {
        (nint external, WeakReference<Node> weak) = CreateExternalWeakRoot();
        try
        {
            for (int epoch = 0; epoch < 6; epoch++)
            {
                Check(collect(1) == 0, "Ref-counted-root RC epoch failed.");
                CheckExternalRoot(external);
            }
        }
        finally
        {
            Marshal.Release(external);
        }
        ForceFull();
        Check(!weak.TryGetTarget(out _), "A released ref-counted callback root retained its original managed identity.");
        GC.KeepAlive(s_wrappers);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nint CreateSyncBlockGarbage(HandleAddress address, ArmLifetime arm)
    {
        Node value = new(443);
        RuntimeHelpers.GetHashCode(value);
        lock (value)
        {
            Monitor.Wait(value, 1);
        }
        GCHandle temporary = GCHandle.Alloc(value);
        ForceFull();
        nint result = address(GCHandle.ToIntPtr(temporary));
        Check(arm(GCHandle.ToIntPtr(temporary)) == 0, "The non-rooting SyncBlock lifetime probe could not be armed.");
        temporary.Free();
        GC.KeepAlive(value);
        return result;
    }

    private static void VerifySyncBlockLifetime(
        Collect collect, HandleAddress address, AddressExtent extent, GetMetric metric, ArmLifetime arm)
    {
        nint old = CreateSyncBlockGarbage(address, arm);
        for (int epoch = 0; epoch < 4; epoch++)
        {
            Check(collect(1) == 0, "SyncBlock-retention RC epoch failed.");
            CreateAcyclicGarbage(2048);
            Check(metric(47) == 0 && extent(old) != 0,
                "A SyncBlock-only referent was reclaimed before suspended weak cleanup.");
        }
        ForceFull();
        nuint remainingExtent = extent(old);
        if (s_syncBlockDiagnostic)
        {
            Console.WriteLine($"syncblock_diagnostic address={old:X} extent={remainingExtent} births={metric(46)} reclaims={metric(47)}");
        }
        Check(metric(47) >= 1, "An unreachable SyncBlock referent did not retire during weak cleanup.");
        Node replacement = new(449);
        int hash = RuntimeHelpers.GetHashCode(replacement);
        ForceFull();
        lock (replacement)
        {
            Check(replacement.Value == 449 && RuntimeHelpers.GetHashCode(replacement) == hash,
                "Stale SyncBlock cleanup reset a replacement object's header or payload.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nint CreateWatchedLargeGarbage(HandleAddress address, ArmLifetime arm)
    {
        byte[] value = new byte[65536];
        RuntimeHelpers.GetHashCode(value);
        lock (value)
        {
            Monitor.Wait(value, 1);
        }
        GCHandle temporary = GCHandle.Alloc(value);
        nint result = address(GCHandle.ToIntPtr(temporary));
        Check(arm(GCHandle.ToIntPtr(temporary)) == 0, "The large-object lifetime probe could not be armed.");
        temporary.Free();
        GC.KeepAlive(value);
        return result;
    }

    private static int RunLifetimeReuse(string path, bool historicalControl)
    {
        path = Path.GetFullPath(path);
        nint module = NativeLibrary.Load(path);
        GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
        HandleAddress address = Marshal.GetDelegateForFunctionPointer<HandleAddress>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetHandleAddress"));
        AddressExtent extent = Marshal.GetDelegateForFunctionPointer<AddressExtent>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetAddressExtent"));
        ArmLifetime arm = Marshal.GetDelegateForFunctionPointer<ArmLifetime>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_ArmLifetimeProbe"));
        Collect collect = Marshal.GetDelegateForFunctionPointer<Collect>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_Collect"));
        Check(metric(0) == 1, "The lifetime control did not select the owning LXR collector.");
        nint watched = CreateWatchedLargeGarbage(address, arm);
        Check(collect(0) == 0 && collect(0) == 0, "The lifetime control's two real full collections failed.");
        Check(metric(47) == 1, "The watched allocation must retire exactly once before address reuse.");
        byte[][] retained = new byte[96][];
        bool reused = false;
        for (int index = 0; index < retained.Length; index++)
        {
            retained[index] = new byte[65536];
            GCHandle handle = GCHandle.Alloc(retained[index]);
            nint current = address(GCHandle.ToIntPtr(handle));
            handle.Free();
            if (current == watched)
            {
                reused = true;
                break;
            }
        }
        Check(reused, "The bounded counterfactual did not reuse the exact watched physical address.");
        Check(collect(0) == 0, "The separately retained replacement's full collection failed.");
        nuint activeExtent = extent(watched);
        ulong births = metric(46);
        ulong reclaims = metric(47);
        Console.WriteLine($"lifetime_reuse module={path} sha256={Hash(path)} address={watched:X} extent={activeExtent} births={births} reclaims={reclaims} historical_control={historicalControl}");
        Check(activeExtent != 0 && births == 1 && reclaims == 1,
            "A new same-type allocation must own the address without resurrecting the retired referent.");
        Check(historicalControl ? activeExtent == 0 : reclaims == 1,
            "The historical extent-only retirement assertion confuses a retained replacement with the original referent.");
        GC.KeepAlive(retained);
        Console.WriteLine($"PASS: {s_checks} exact-address lifecycle checks");
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (nint[], nuint[]) CreateLargeRanges(HandleAddress address, AddressExtent extent)
    {
        byte[][] values = new byte[96][];
        nint[] starts = new nint[values.Length];
        nuint[] sizes = new nuint[values.Length];
        GCHandle probe = GCHandle.Alloc(null);
        try
        {
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = new byte[100000];
                values[index][99999] = (byte)index;
                probe.Target = values[index];
                starts[index] = address(GCHandle.ToIntPtr(probe));
                sizes[index] = extent(starts[index]);
                Check(sizes[index] >= 100000, "A managed LOS birth has no complete owned extent.");
            }
            ForceFull();
            for (int index = 0; index < values.Length; index++)
            {
                Check(values[index][99999] == (byte)index, "A retained managed LOS cohort was corrupted.");
            }
        }
        finally
        {
            probe.Free();
        }
        GC.KeepAlive(values);
        return (starts, sizes);
    }

    private static void VerifyManagedRangeReuse(HandleAddress address, AddressExtent extent, GetMetric metric)
    {
        ulong releasedBefore = metric(6);
        (nint[] old, nuint[] sizes) = CreateLargeRanges(address, extent);
        ForceFull();
        foreach (nint start in old)
        {
            Check(extent(start) == 0, "An unreachable managed LOS extent was not reclaimed.");
        }
        (nint[] current, nuint[] currentSizes) = CreateLargeRanges(address, extent);
        bool reused = false;
        for (int index = 0; index < current.Length; index++)
        {
            nuint start = (nuint)current[index];
            for (int prior = 0; prior < old.Length; prior++)
            {
                nuint oldStart = (nuint)old[prior];
                reused |= start < oldStart + sizes[prior] && oldStart < start + currentSizes[index];
            }
        }
        Check(reused && metric(6) >= releasedBefore + 9600000,
            "Two managed LOS cohorts larger than the arena did not reclaim and reuse actual prior ranges.");
    }

    private static void VerifyMemoryInfo(bool expectConcurrent = false)
    {
        GCMemoryInfo blocking = GC.GetGCMemoryInfo(GCKind.FullBlocking);
        Check(blocking.Index > 0 && blocking.Generation == 2 && !blocking.Concurrent,
            "Full-blocking diagnostics do not describe the last completed full collection.");
        Check(blocking.HeapSizeBytes > 0 && blocking.TotalCommittedBytes > 0 &&
            blocking.GenerationInfo.Length == 5 && blocking.PauseDurations.Length == 2,
            "Heap/generation/pause diagnostics have an invalid shape.");
        Check(GC.GetGCMemoryInfo(GCKind.Ephemeral).Index == 0,
            "The nongenerational collector invented an ephemeral collection.");
        if (expectConcurrent)
        {
            GCMemoryInfo background = GC.GetGCMemoryInfo(GCKind.Background);
            Check(background.Index > 0 && background.Concurrent &&
                background.PauseDurations[0] > TimeSpan.Zero && background.PauseDurations[1] > TimeSpan.Zero,
                "Background diagnostics lost InitialMark/FinalMark pauses or confused a later blocking collection.");
        }
    }

    private static void VerifyOom(GetMetric metric, Collect collect, HoldTrace hold)
    {
        Node survivor = new(457);
        ulong full = metric(11) + metric(12);
        Check(hold(1) == 0 && collect(2) == 0, "Emergency trace completion control did not start.");
        bool exhausted = false;
        try
        {
            GC.KeepAlive(new byte[20 * 1024 * 1024]);
        }
        catch (OutOfMemoryException)
        {
            exhausted = true;
        }
        Check(exhausted && metric(11) + metric(12) > full && metric(28) == 0,
            "Owned LOS exhaustion did not finish the trace and report managed OOM.");
        Check(survivor.Value == 457 && new byte[1024].Length == 1024, "OOM corrupted roots or prevented recovery.");
        Check(!GC.TryStartNoGCRegion(1024), "The fixed prototype falsely guaranteed a no-GC reservation.");
        ForceFull();
        VerifyMemoryInfo();
        Check(GC.GetGCMemoryInfo(GCKind.Background).Index == 0,
            "Emergency blocking completion invented a completed background collection.");
        GC.KeepAlive(survivor);
    }

    private static int RunStudy(string[] args)
    {
        if (args.Length != 4 || args[1] is not ("LXR" or "WKS" or "Server") ||
            args[3] is not ("normal" or "injected"))
        {
            throw new ArgumentException("Use --study LXR|WKS|Server module-path normal|injected.", nameof(args));
        }
        bool lxr = args[1] == "LXR";
        Check(GCSettings.IsServerGC == (args[1] == "Server"), "The requested collector category was not loaded.");
        string modulePath = Path.GetFullPath(args[2]);
        GetMetric? metric = null;
        if (lxr)
        {
            nint module = NativeLibrary.Load(modulePath);
            metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
            Check(metric(0) == 1 && metric(1) > 0, "The study did not load the owned LXR host.");
        }
        else
        {
            Check(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCPath")),
                "A built-in study unexpectedly selected an external collector.");
        }
        Node[] roots = new Node[4097];
        for (int index = 0; index < roots.Length; index++)
        {
            roots[index] = new Node(index);
        }
        StudyBody(roots, 2, false);
        ForceFull();
        ulong copiedBefore = metric is null ? 0 : metric(23) + metric(24);
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        long pausesBefore = GC.GetTotalPauseDuration().Ticks;
        int collectionsBefore = GC.CollectionCount(2);
        long start = Stopwatch.GetTimestamp();
        long checksum = StudyBody(roots, 32, args[3] == "injected");
        double milliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        double pauseMilliseconds = TimeSpan.FromTicks(GC.GetTotalPauseDuration().Ticks - pausesBefore).TotalMilliseconds;
        ulong copied = metric is null ? 0 : metric(23) + metric(24) - copiedBefore;
        Check(checksum == 49152 && roots[4096].Value == 31 * 8192 + 4096, "Study inputs/checksum changed.");
        Console.WriteLine($"study={args[1]} control={args[3]} server={GCSettings.IsServerGC} " +
            $"module_sha256={Hash(modulePath)} elapsed_ms={milliseconds.ToString("F3", CultureInfo.InvariantCulture)} " +
            $"pause_ms={pauseMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} allocated_bytes={allocated} " +
            $"collections={GC.CollectionCount(2) - collectionsBefore} copied_rc_entries={copied} checksum={checksum}");
        GC.KeepAlive(roots);
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long StudyBody(Node[] roots, int rounds, bool injected)
    {
        long checksum = 0;
        for (int round = 0; round < rounds; round++)
        {
            CreateAcyclicGarbage(1024);
            for (int index = 0; index < roots.Length; index++)
            {
                Node value = new(round * 8192 + index);
                value.Next = (index & 7) == 0 ? value : roots[(index + 1) % roots.Length];
                roots[index] = value;
            }
            if ((round & 3) == 3)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            }
            if (injected)
            {
                Thread.Sleep(20);
            }
            checksum += roots[1536].Value - round * 8192;
        }
        return checksum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Node AllocateSmall(int value) => new(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (object Roots, long Checksum, long Operations, bool Exhausted) VerticalBody(
        string scenario, Node[]? preparedMutation)
    {
        if (scenario is "growth" or "growth-hard")
        {
            byte[]?[] roots = new byte[1240][];
            int allocated = 0;
            bool exhausted = false;
            try
            {
                for (; allocated < roots.Length; allocated++)
                {
                    int bytes = allocated < 1200 ? 16384 : 1024 * 1024;
                    byte[] value = new byte[bytes];
                    value[0] = (byte)(allocated % 251);
                    value[^1] = (byte)(allocated % 251);
                    roots[allocated] = value;
                }
            }
            catch (OutOfMemoryException)
            {
                exhausted = true;
            }
            long checksum = 0;
            for (int index = 0; index < allocated; index++)
            {
                byte[] value = roots[index]!;
                Check(value[0] == index % 251 && value[^1] == index % 251,
                    "Retained in-place survivors changed during growth or hard-limit collection.");
                checksum += value[0] + value[^1];
            }
            return (roots, checksum, allocated, exhausted);
        }
        if (scenario is "automatic")
        {
            byte[][] roots = new byte[128][];
            long checksum = 0;
            for (int index = 0; index < 16384; index++)
            {
                byte[] value = new byte[8192];
                value[0] = (byte)(index % 251);
                roots[index % roots.Length] = value;
                checksum += value[0];
            }
            Check(checksum == 2041721, "Automatic-collection inputs changed.");
            return (roots, checksum, 16384, false);
        }
        if (scenario is "forced")
        {
            Node[] roots = new Node[4097];
            for (int index = 0; index < roots.Length; index++) { roots[index] = AllocateSmall(index); }
            long checksum = StudyBody(roots, 32, false);
            Check(checksum == 49152, "Forced full-cycle inputs changed.");
            return (roots, checksum, 32, false);
        }
        if (scenario is "mutation")
        {
            Node[] roots = preparedMutation ?? throw new ArgumentException("Mutation setup is required.", nameof(preparedMutation));
            long checksum = 0;
            for (int round = 0; round < 128; round++)
            {
                for (int index = 0; index < roots.Length; index++)
                {
                    Node value = roots[(index + round) & 4095];
                    roots[index].Next = value;
                    checksum += value.Value;
                }
            }
            Check(checksum == 1073479680, "Mutation-focused inputs changed.");
            return (roots, checksum, 128 * 4096, false);
        }
        if (scenario is "allocation")
        {
            Node[] roots = new Node[1024];
            long checksum = 0;
            for (int index = 0; index < 524288; index++)
            {
                Node value = AllocateSmall(index & 4095);
                roots[index & 1023] = value;
                checksum += value.Value;
            }
            Check(checksum == 1073479680, "Allocation-focused inputs changed.");
            return (roots, checksum, 524288, false);
        }
        throw new ArgumentException("Unknown vertical scenario.", nameof(scenario));
    }

    private static unsafe long OwnedResidentBytes(nuint start, nuint bytes)
    {
        if (OperatingSystem.IsWindows())
        {
            const int Entries = 4096;
            const int EntryBytes = 16;
            nint information = Marshal.AllocHGlobal(Entries * EntryBytes);
            try
            {
                long resident = 0;
                nuint page = (nuint)Environment.SystemPageSize;
                for (nuint offset = 0; offset < bytes;)
                {
                    int count = (int)Math.Min((nuint)Entries, (bytes - offset + page - 1) / page);
                    for (int index = 0; index < count; index++)
                    {
                        Marshal.WriteIntPtr(information, index * EntryBytes, (nint)(start + offset + (nuint)index * page));
                        Marshal.WriteIntPtr(information, index * EntryBytes + 8, 0);
                    }
                    if (!QueryWorkingSetEx(-1, information, (uint)(count * EntryBytes)))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
                    }
                    for (int index = 0; index < count; index++)
                    {
                        if ((Marshal.ReadIntPtr(information, index * EntryBytes + 8) & 1) != 0) { resident += (long)page; }
                    }
                    offset += (nuint)count * page;
                }
                return resident;
            }
            finally
            {
                Marshal.FreeHGlobal(information);
            }
        }
        const int Pages = 1024;
        byte* residency = stackalloc byte[Pages];
        nuint pageBytes = (nuint)Environment.SystemPageSize;
        long total = 0;
        for (nuint offset = 0; offset < bytes;)
        {
            int count = (int)Math.Min((nuint)Pages, (bytes - offset + pageBytes - 1) / pageBytes);
            if (Mincore((nint)(start + offset), (nuint)count * pageBytes, residency) != 0)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
            }
            for (int index = 0; index < count; index++)
            {
                if ((residency[index] & 1) != 0) { total += (long)pageBytes; }
            }
            offset += (nuint)count * pageBytes;
        }
        return total;
    }

    private static int RunSingleGrowth(string path)
    {
        nint module = NativeLibrary.Load(path);
        GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
        GC.Collect();
        byte[] value = new byte[1536 * 1024];
        value[0] = 17;
        value[^1] = 29;
        GC.Collect();
        Check(value[0] == 17 && value[^1] == 29 && metric(37) >= 1,
            "An object spanning initial LOS plus a coalescible tail incorrectly reported OOM.");
        Console.WriteLine($"PASS: {s_checks} P8 single-object LOS growth checks");
        GC.KeepAlive(value);
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object CreateRetentionInput(bool pointerRich)
    {
        if (!pointerRich)
        {
            (object roots, _, _, bool exhausted) = VerticalBody("growth", null);
            Check(!exhausted, "The fixed byte-heavy retained input exhausted its heap.");
            return roots;
        }
        Node[] nodes = new Node[262144];
        for (int index = 0; index < nodes.Length; index++)
        {
            nodes[index] = new Node(index);
            if (index != 0)
            {
                nodes[index].Next = nodes[index - 1];
            }
        }
        return nodes;
    }

    private static int RunRetention(string path, bool pointerRich)
    {
        path = Path.GetFullPath(path);
        nint module = NativeLibrary.Load(path);
        GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
        EnableStorage enable = Marshal.GetDelegateForFunctionPointer<EnableStorage>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_EnableStorageDiagnostics"));
        NativeSize ownedStart = Marshal.GetDelegateForFunctionPointer<NativeSize>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetOwnedStart"));
        NativeSize ownedBytes = Marshal.GetDelegateForFunctionPointer<NativeSize>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetOwnedBytes"));
        Check(metric(0) == 1, "The retention input did not select owned LXR.");
        ForceFull();
        Check(enable() == 0, "The retention accounting window did not start quiescently.");
        ulong reclaimedBefore = metric(4);
        ExerciseRetention(pointerRich, metric, ownedStart, ownedBytes);
        ForceFull();
        PrintRetention("quiescent", pointerRich, metric, ownedStart, ownedBytes);
        ulong payload = pointerRich ? 262144UL * 40 : 1200UL * 16384 + 40UL * 1024 * 1024;
        Check(metric(4) - reclaimedBefore >= payload, "The retained input did not actually reclaim its payload.");
        Check(metric(49) == 0 && metric(53) == 0 && metric(56) == 0 && metric(57) == 0,
            "Quiescence retained drained Work, TraceScan or trace-plane storage.");
        Console.WriteLine($"PASS: {s_checks} retention checks module={path} sha256={Hash(path)}");
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExerciseRetention(
        bool pointerRich, GetMetric metric, NativeSize ownedStart, NativeSize ownedBytes)
    {
        object roots = CreateRetentionInput(pointerRich);
        ForceFull();
        if (roots is Node[] nodes)
        {
            for (int index = 0; index < nodes.Length; index++)
            {
                Check(nodes[index].Value == index, "Reference-rich retained payload changed.");
                nodes[index].Next = nodes[(index + 257) & (nodes.Length - 1)];
            }
            GC.Collect();
            for (int index = 0; index < nodes.Length; index++)
            {
                Check(ReferenceEquals(nodes[index].Next, nodes[(index + 257) & (nodes.Length - 1)]),
                    "A legitimate mutation burst lost a retained reference.");
            }
        }
        ForceFull();
        PrintRetention("live", pointerRich, metric, ownedStart, ownedBytes);
        GC.KeepAlive(roots);
    }

    private static void PrintRetention(
        string phase, bool pointerRich, GetMetric metric, NativeSize ownedStart, NativeSize ownedBytes)
    {
        using Process process = Process.GetCurrentProcess();
        process.Refresh();
        long owned = OwnedResidentBytes(ownedStart(), ownedBytes());
        Console.WriteLine($"retention={phase} pointer_rich={pointerRich} working_set_bytes={process.WorkingSet64} private_bytes={process.PrivateMemorySize64} owned_resident_bytes={owned} managed_committed_bytes={metric(26)} admitted_bytes={metric(36)} native_plane_committed_bytes={metric(38)} live_bytes={metric(3)} work_owned_bytes={metric(49)} work_high_water_bytes={metric(50)} copied_reference_bytes={metric(51)} scan_owned_bytes={metric(53)} scan_high_water_bytes={metric(54)}");
        if (s_nativeAllocatorDiagnostic)
        {
            PrintNativeAllocator(phase);
        }
    }

    private static unsafe void PrintNativeAllocator(string phase)
    {
        nint* data = stackalloc nint[2];
        data[0] = data[1] = 0;
        nint stream = OpenMemoryStream(data, (nuint*)(data + 1));
        if (stream == 0)
        {
            throw new IOException($"open_memstream failed for allocator diagnostics: errno {Marshal.GetLastPInvokeError()}.");
        }
        try
        {
            int result = MallocInfo(0, stream);
            int infoError = Marshal.GetLastPInvokeError();
            int closed = CloseNativeStream(stream);
            stream = 0;
            if (result != 0 || closed != 0)
            {
                throw new IOException($"Allocator diagnostics failed: malloc_info={result}, errno={infoError}, fclose={closed}.");
            }
            nuint size = (nuint)data[1];
            if (size > int.MaxValue)
            {
                throw new InvalidOperationException("Allocator diagnostic output exceeded managed string capacity.");
            }
            string xml = Marshal.PtrToStringUTF8(data[0], (int)size) ??
                throw new InvalidOperationException("Allocator diagnostics returned no buffer.");
            Console.WriteLine($"native_allocator_phase={phase}");
            Console.WriteLine(xml);
        }
        finally
        {
            if (stream != 0 && CloseNativeStream(stream) != 0)
            {
                Console.Error.WriteLine("Allocator diagnostic stream cleanup failed while propagating an earlier error.");
            }
            NativeMemory.Free((void*)data[0]);
        }
    }

    private static int RunVertical(string[] args)
    {
        if (args.Length != 5 || args[2] is not ("LXR" or "WKS" or "Server") ||
            args[4] is not ("normal" or "injected"))
        {
            throw new ArgumentException("Use --vertical scenario LXR|WKS|Server module-path normal|injected.", nameof(args));
        }
        bool lxr = args[2] == "LXR";
        Check(GCSettings.IsServerGC == (args[2] == "Server"), "Vertical collector category mismatch.");
        string modulePath = Path.GetFullPath(args[3]);
        GetMetric? metric = null;
        NativeSize? ownedStart = null;
        NativeSize? ownedBytes = null;
        bool p8 = false;
        EnableStorage? enableStorage = null;
        StorageRange? storageRange = null;
        if (lxr)
        {
            nint module = NativeLibrary.Load(modulePath);
            metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
            GetIdentity identity = Marshal.GetDelegateForFunctionPointer<GetIdentity>(NativeLibrary.GetExport(module, "GC_LxrPrototype_GetIdentity"));
            string collectorIdentity = Marshal.PtrToStringUTF8(identity())!;
            p8 = collectorIdentity.Contains("P8", StringComparison.Ordinal) ||
                collectorIdentity.Contains("P6 nursery", StringComparison.Ordinal);
            ownedStart = Marshal.GetDelegateForFunctionPointer<NativeSize>(NativeLibrary.GetExport(module, "GC_LxrPrototype_GetOwnedStart"));
            ownedBytes = Marshal.GetDelegateForFunctionPointer<NativeSize>(NativeLibrary.GetExport(module, "GC_LxrPrototype_GetOwnedBytes"));
            Check(metric(0) == 1 && metric(1) > 0, "Vertical did not load owned LXR execution.");
            if (s_storageDiagnostic)
            {
                enableStorage = Marshal.GetDelegateForFunctionPointer<EnableStorage>(
                    NativeLibrary.GetExport(module, "GC_LxrPrototype_EnableStorageDiagnostics"));
                storageRange = Marshal.GetDelegateForFunctionPointer<StorageRange>(
                    NativeLibrary.GetExport(module, "GC_LxrPrototype_GetStorageRange"));
            }
        }
        else
        {
            Check(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCPath")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPlus_GCPath")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCName")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPlus_GCName")),
                "Built-in characterization inherited an external collector selection.");
        }
        AllocateSmall(17);
        Node[]? preparedMutation = null;
        if (args[1] == "mutation")
        {
            preparedMutation = new Node[4096];
            for (int index = 0; index < preparedMutation.Length; index++) { preparedMutation[index] = AllocateSmall(index); }
        }
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        using Process? scopedCpu = s_storageDiagnostic ? Process.GetCurrentProcess() : null;
        if (enableStorage is not null)
        {
            Check(enableStorage() == 0, "Storage accounting must begin with quiescent, inactive tracing.");
        }
        long cpuBefore = scopedCpu?.TotalProcessorTime.Ticks ?? 0;
        ulong initialBefore = s_storageDiagnostic && lxr ? metric!(10) : 0;
        ulong finalBefore = s_storageDiagnostic && lxr ? metric!(11) : 0;
        ulong fullBefore = s_storageDiagnostic && lxr ? metric!(12) : 0;
        ulong fastBefore = p8 ? metric!(32) : 0;
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        int collectionsBefore = GC.CollectionCount(2);
        long pausesBefore = GC.GetTotalPauseDuration().Ticks;
        long start = Stopwatch.GetTimestamp();
        (object roots, long checksum, long operations, bool exhausted) = VerticalBody(args[1], preparedMutation);
        if (args[4] == "injected") { Thread.Sleep(200); }
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        double cpu = TimeSpan.FromTicks((scopedCpu?.TotalProcessorTime.Ticks ?? 0) - cpuBefore).TotalMilliseconds;
        ulong initial = s_storageDiagnostic && lxr ? metric!(10) - initialBefore : 0;
        ulong final = s_storageDiagnostic && lxr ? metric!(11) - finalBefore : 0;
        ulong full = s_storageDiagnostic && lxr ? metric!(12) - fullBefore : 0;
        ulong backlog = s_storageDiagnostic && lxr ? metric!(28) : 0;
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        int collections = GC.CollectionCount(2) - collectionsBefore;
        double pause = TimeSpan.FromTicks(GC.GetTotalPauseDuration().Ticks - pausesBefore).TotalMilliseconds;
        ulong fast = p8 ? metric!(32) - fastBefore : 0;
        start = Stopwatch.GetTimestamp();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        double postFull = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        GCMemoryInfo memory = GC.GetGCMemoryInfo();
        long ownedResident = lxr ? OwnedResidentBytes(ownedStart!(), ownedBytes!()) : -1;
        using Process process = Process.GetCurrentProcess();
        process.Refresh();
        long workingSet = process.WorkingSet64;
        Check(ownedResident <= workingSet, "Owned residency exceeds the observed process working set.");
        if (p8 && args[1] == "allocation")
        {
            bool enabled = Environment.GetEnvironmentVariable("DOTNET_LXRFastAllocation") is not "0";
            Check(enabled ? fast > 500000 : fast == 0,
                "Fast-helper evidence does not match the explicit allocation control.");
        }
        if (p8 && args[1] == "growth")
        {
            Check(!exhausted && metric!(37) >= 4 && metric(26) > 24 * 1024 * 1024 &&
                metric(3) > 24 * 1024 * 1024, "Both grown spaces must support a live set larger than the old total capacity.");
        }
        if (p8 && args[1] == "growth-hard")
        {
            Check(exhausted && metric!(36) <= metric(44) && metric(28) == 0,
                "Copied-volume prediction must not bypass hard-limit OOM or blocking recovery.");
            Check(new byte[1024].Length == 1024, "Growth hard-limit exhaustion prevented recovery.");
        }
        Console.WriteLine($"vertical={args[1]} collector={args[2]} control={args[4]} " +
            $"module_sha256={Hash(modulePath)} operations={operations} checksum={checksum} exhausted={exhausted} " +
            $"elapsed_ms={elapsed.ToString("F3", CultureInfo.InvariantCulture)} " +
            $"post_full_ms={postFull.ToString("F3", CultureInfo.InvariantCulture)} " +
            $"pause_ms={pause.ToString("F3", CultureInfo.InvariantCulture)} allocated_bytes={allocated} collections={collections} " +
            $"working_set_bytes={workingSet} private_bytes={process.PrivateMemorySize64} " +
            $"owned_resident_bytes={ownedResident} non_owned_resident_bytes={(lxr ? workingSet - ownedResident : -1)} " +
            $"heap_bytes={memory.HeapSizeBytes} fragmentation_bytes={memory.FragmentedBytes} " +
            $"managed_committed_bytes={(lxr ? metric!(26) : (ulong)memory.TotalCommittedBytes)} " +
            $"reserved_bytes={(lxr ? ownedBytes!() : 0)} admitted_bytes={(p8 ? metric!(36) : 0)} " +
            $"native_plane_committed_bytes={(p8 ? metric!(38) : 0)} live_bytes={(lxr ? metric!(3) : 0)} " +
            $"reclaimed_bytes={(lxr ? metric!(4) : 0)} fast_hits={fast} refills={(p8 ? metric!(33) : 0)} " +
            $"growth_count={(p8 ? metric!(37) : 0)} hard_limit_bytes={(p8 ? metric!(44) : 0)} checks={s_checks}");
        if (s_storageDiagnostic && metric is not null && storageRange is not null)
        {
            Console.WriteLine($"storage_phase={args[1]} wall_ms={elapsed.ToString("F3", CultureInfo.InvariantCulture)} cpu_ms={cpu.ToString("F3", CultureInfo.InvariantCulture)} stw_ms={pause.ToString("F3", CultureInfo.InvariantCulture)} initial={initial} final={final} full={full} trace_phase_at_window_end={backlog}");
            for (uint index = 48; index <= 75; index++)
            {
                Console.WriteLine($"storage_metric={index} value={metric(index)}");
            }
            if (metric(76) > 32)
            {
                Console.WriteLine($"storage_metric=77 value={metric(77)}");
            }
            long auxResident = 0;
            for (uint component = 0; component < metric(76); component++)
            {
                Check(storageRange(component, out nuint address, out nuint bytes) == 0,
                    "The storage component range could not be inspected.");
                long resident = bytes == 0 ? 0 : OwnedResidentBytes(address, bytes);
                auxResident += resident;
                Console.WriteLine($"storage_component={component} start={address:X} reserved_coverage_bytes={bytes} resident_bytes={resident}");
            }
            Console.WriteLine($"storage_resident_bytes={auxResident}");
        }
        GC.KeepAlive(roots);
        return exhausted && args[1] != "growth-hard" ? 2 : 0;
    }

    private static int RunBirth(string path)
    {
        nint module = NativeLibrary.Load(path);
        GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
        Collect collect = Marshal.GetDelegateForFunctionPointer<Collect>(NativeLibrary.GetExport(module, "GC_LxrPrototype_Collect"));
        HoldTrace hold = Marshal.GetDelegateForFunctionPointer<HoldTrace>(NativeLibrary.GetExport(module, "GC_LxrPrototype_HoldTrace"));
        ArmScan arm = Marshal.GetDelegateForFunctionPointer<ArmScan>(NativeLibrary.GetExport(module, "GC_LxrPrototype_ArmScanCheckpoint"));
        WaitForTrace wait = Marshal.GetDelegateForFunctionPointer<WaitForTrace>(NativeLibrary.GetExport(module, "GC_LxrPrototype_WaitForTrace"));
        ulong hits = metric(32);
        Node[] roots = new Node[4096];
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < roots.Length; index++) { roots[index] = AllocateSmall(index); }
        Check(GC.GetAllocatedBytesForCurrentThread() - before == roots.Length * 32,
            "TLAB tail accounting changed exact per-thread object allocation.");
        Check(metric(32) - hits > 4000, "Allocation helpers did not actually consume the exported span.");
        foreach (int length in new[] {0, 1, 15, 255, 4093, 8191, 32743, 32744, 65535})
        {
            object?[] references = GC.AllocateUninitializedArray<object?>(length);
            Check(Array.TrueForAll(references, value => value is null), "Reference payload was published before safe zeroing.");
            byte[] bytes = new byte[length];
            string text = new('x', length);
            GC.Collect();
            Check(bytes.Length == length && text.Length == length && (length == 0 || text[^1] == 'x'),
                "Array/string shape or complete extent was lost across refill and collection.");
            GC.KeepAlive(references);
        }
        Node[] concurrent = new Node[256];
        VerifyConcurrentMutation(metric, collect, hold, arm, wait, () =>
        {
            for (int index = 0; index < concurrent.Length; index++) { concurrent[index] = AllocateSmall(index + 257); }
        });
        Check(Array.TrueForAll(concurrent, value => value.Value >= 257),
            "Actual concurrent scanning lost a fast-born allocation.");
        GC.Collect();
        Check(roots[4095].Value == 4095, "Collection or detach invalidated a fast-born root.");
        Console.WriteLine($"PASS: {s_checks} P8 managed birth/fast-helper checks fast_hits={metric(32) - hits} refills={metric(33)}");
        GC.KeepAlive(roots);
        GC.KeepAlive(concurrent);
        return 0;
    }

    private sealed class ClosureFinalizer
    {
        internal static int s_runs;
        internal ClosureFinalizer? Child;

        ~ClosureFinalizer()
        {
            if (Child is not null && Child.GetType() != typeof(ClosureFinalizer))
            {
                Environment.FailFast("Finalization lost its transitive object graph.");
            }
            Interlocked.Increment(ref s_runs);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<ClosureFinalizer>[] CreateFinalizerClosure()
    {
        ClosureFinalizer parent = new();
        parent.Child = new ClosureFinalizer();
        return [new(parent, trackResurrection: true), new(parent.Child, trackResurrection: true)];
    }

    private static void VerifySingleCollectionFinalizerClosure()
    {
        int before = Volatile.Read(ref ClosureFinalizer.s_runs);
        WeakReference<ClosureFinalizer>[] weak = CreateFinalizerClosure();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        Check(Volatile.Read(ref ClosureFinalizer.s_runs) == before + 2,
            "One completed collection did not discover both initially unreachable finalizable objects.");
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        foreach (WeakReference<ClosureFinalizer> reference in weak)
        {
            Check(!reference.TryGetTarget(out _), "A completed finalizer closure retained an original identity.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (DependentHandle[], GCHandle, WeakReference<Node>) CreateDependentClosure()
    {
        Node[] nodes = new Node[33];
        for (int index = 0; index < nodes.Length; index++)
        {
            nodes[index] = new Node(7000 + index);
        }
        DependentHandle[] handles = new DependentHandle[32];
        for (int index = handles.Length - 1; index >= 0; index--)
        {
            handles[index] = new DependentHandle(nodes[index], nodes[index + 1]);
        }
        Node tail = new(7999) { Next = nodes[0] };
        nodes[^1].Next = tail;
        return (handles, GCHandle.Alloc(nodes[0]), new(tail));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckDependentClosure(DependentHandle[] handles, WeakReference<Node> tail)
    {
        for (int index = 0; index < handles.Length; index++)
        {
            (object? target, object? dependent) = handles[index].TargetAndDependent;
            Check(target is Node primary && primary.Value == 7000 + index &&
                dependent is Node secondary && secondary.Value == 7001 + index,
                "One collection did not complete the reverse dependent chain with the original identities.");
        }
        Check(tail.TryGetTarget(out Node? value) && value.Value == 7999 && value.Next is Node { Value: 7000 },
            "Dependent promotion did not complete ordinary transitive closure in the same collection.");
    }

    private static void VerifySingleCollectionDependentClosure(Collect collect)
    {
        (DependentHandle[] handles, GCHandle root, WeakReference<Node> tail) = CreateDependentClosure();
        try
        {
            Check(collect(1) == 0, "The dependent RC closure failed.");
            CheckDependentClosure(handles, tail);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            CheckDependentClosure(handles, tail);
            WeakReference<Node> replacement = ReplaceDependent(handles[0]);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Check(IsReplacementDependent(handles[0]),
                "Dependent mutation lost the new secondary.");
            for (int index = 1; index < handles.Length; index++)
            {
                Check(handles[index].TargetAndDependent == (null, null),
                    "A detached dependent chain survived one completed marking fixed point.");
            }
            Check(!tail.TryGetTarget(out _), "Dependent mutation retained the old transitive tail.");
            handles[0].Dependent = null;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Check(!replacement.TryGetTarget(out _), "A null dependent retained its previous identity.");
            handles[0].Target = null;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Check(IsOriginalDependentPrimary(root) && handles[0].TargetAndDependent == (null, null),
                "Clearing a dependent primary failed while its original object remained strongly rooted.");
            root.Free();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            foreach (DependentHandle handle in handles)
            {
                Check(handle.TargetAndDependent == (null, null),
                    "An unmarked dependent primary or its secondary survived one completed full collection.");
            }
            Check(!tail.TryGetTarget(out _), "All secondaries were treated as strong tracing roots.");
        }
        finally
        {
            if (root.IsAllocated)
            {
                root.Free();
            }
            foreach (DependentHandle handle in handles)
            {
                handle.Dispose();
            }
        }
        using DependentHandle emptySecondary = CreateEmptyDependent();
        Check(collect(1) == 0, "A null-secondary dependent handle broke RC closure.");
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        Check(emptySecondary.TargetAndDependent == (null, null),
            "A null secondary kept an unmarked primary alive.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static DependentHandle CreateEmptyDependent() => new(new Node(8001), null);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static int RunDependentScopeControl(string[] args)
    {
        if (args.Length != 4 || args[1] is not ("before" or "after") ||
            args[2] is not ("LXR" or "WKS" or "Server"))
        {
            throw new ArgumentException("Use --dependent-scope before|after LXR|WKS|Server module.", nameof(args));
        }
        Check(GCSettings.IsServerGC == (args[2] == "Server"), "Dependent scope collector category mismatch.");
        if (args[2] == "LXR")
        {
            nint module = NativeLibrary.Load(args[3]);
            GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
            Check(metric(0) == 1, "Dependent scope control did not select owning LXR.");
        }
        else
        {
            Check(Environment.GetEnvironmentVariable("DOTNET_GCPath") is null,
                "Dependent scope built-in control inherited an external collector.");
        }
        using DependentHandle handle = args[1] == "before"
            ? new(new Node(8001), null) : CreateEmptyDependent();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        Check(handle.TargetAndDependent == (null, null),
            "The direct constructor's Debug temporary remains an actual strong root.");
        Console.WriteLine($"PASS: {s_checks} dependent scope checks module={Path.GetFullPath(args[3])} sha256={Hash(args[3])}");
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Node> ReplaceDependent(DependentHandle handle)
    {
        Node value = new(8101);
        handle.Dependent = value;
        return new(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsReplacementDependent(DependentHandle handle) => handle.Dependent is Node { Value: 8101 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsOriginalDependentPrimary(GCHandle handle) => handle.Target is Node { Value: 7000 };

    private sealed class ReregisteredFinalizer
    {
        internal static int s_runs;
        internal static ReregisteredFinalizer? s_resurrected;
        private bool _registeredAgain;

        ~ReregisteredFinalizer()
        {
            Interlocked.Increment(ref s_runs);
            if (!_registeredAgain)
            {
                _registeredAgain = true;
                GC.ReRegisterForFinalize(this);
                Volatile.Write(ref s_resurrected, this);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<ReregisteredFinalizer> CreateReregisteredFinalizer() =>
        new(new ReregisteredFinalizer(), trackResurrection: true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool HasReregisteredResurrection() =>
        Volatile.Read(ref ReregisteredFinalizer.s_resurrected) is not null;

    private static void VerifyReregistration()
    {
        int before = Volatile.Read(ref ReregisteredFinalizer.s_runs);
        WeakReference<ReregisteredFinalizer> weak = CreateReregisteredFinalizer();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        Check(Volatile.Read(ref ReregisteredFinalizer.s_runs) == before + 1 &&
            HasReregisteredResurrection(),
            "Finalizer-thread re-registration or resurrection did not complete.");
        Volatile.Write(ref ReregisteredFinalizer.s_resurrected, null);
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        Check(Volatile.Read(ref ReregisteredFinalizer.s_runs) == before + 2,
            "The re-registered original identity was not delivered exactly twice.");
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        Check(!weak.TryGetTarget(out _), "The twice-finalized identity retained a stale registration lease.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (GCHandle, GCHandle, nint) CreateWeakOnly(
        HandleAddress address, ArmLifetime arm)
    {
        byte[] value = new byte[65536];
        value[2000] = 73;
        GCHandle shortWeak = GCHandle.Alloc(value, GCHandleType.Weak);
        GCHandle longWeak = GCHandle.Alloc(value, GCHandleType.WeakTrackResurrection);
        Check(arm(GCHandle.ToIntPtr(shortWeak)) == 0, "Could not arm the non-rooting weak identity probe.");
        return (shortWeak, longWeak, address(GCHandle.ToIntPtr(shortWeak)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GCHandle CreateWeakReplacement()
    {
        byte[] value = new byte[65536];
        value[2000] = 173;
        return GCHandle.Alloc(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GCHandle CreateUnregisteredWeak() => GCHandle.Alloc(new Node(8201), GCHandleType.Weak);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (DependentHandle, nint) CreateNullSecondary(HandleAddress address, ArmLifetime arm)
    {
        byte[] value = new byte[65536];
        value[2000] = 73;
        DependentHandle dependent = new(value, null);
        GCHandle temporary = GCHandle.Alloc(value);
        try
        {
            nint result = address(GCHandle.ToIntPtr(temporary));
            Check(arm(GCHandle.ToIntPtr(temporary)) == 0, "Could not watch the null-secondary primary identity.");
            return (dependent, result);
        }
        finally
        {
            temporary.Free();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsNullSecondaryOriginal(DependentHandle handle) =>
        handle.TargetAndDependent is (byte[] value, null) && value[2000] == 73;

    private static void VerifyWatchedReplacement(
        nint old, HandleAddress address, Collect collect, GetMetric metric)
    {
        GCHandle[] replacements = new GCHandle[128];
        try
        {
            int reused = -1;
            for (int index = 0; index < replacements.Length && reused < 0; index++)
            {
                replacements[index] = CreateWeakReplacement();
                if (address(GCHandle.ToIntPtr(replacements[index])) == old)
                {
                    reused = index;
                }
            }
            Check(reused >= 0 && metric(46) >= 1, "The bounded lifetime test did not physically replace the watched address.");
            Console.WriteLine($"replacement_births={metric(46)} original_reclaims={metric(47)}");
            Check(collect(1) == 0 && collect(1) == 0, "Post-reuse old-root retirement failed.");
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Check(replacements[reused].Target is byte[] payload && payload[2000] == 173 && metric(47) == 1,
                "An old lifetime/RC lease affected a different identity at the same address.");
        }
        finally
        {
            foreach (GCHandle replacement in replacements)
            {
                if (replacement.IsAllocated) { replacement.Free(); }
            }
        }
    }

    private static void VerifyNullSecondary(nint module, Collect collect, GetMetric metric)
    {
        HandleAddress address = Marshal.GetDelegateForFunctionPointer<HandleAddress>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetHandleAddress"));
        ArmLifetime arm = Marshal.GetDelegateForFunctionPointer<ArmLifetime>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_ArmLifetimeProbe"));
        (DependentHandle dependent, nint old) = CreateNullSecondary(address, arm);
        try
        {
            for (int epoch = 0; epoch < 4; epoch++)
            {
                Check(collect(1) == 0, "Null-secondary RC epoch failed.");
                ulong reclaimed = metric(47);
                Console.WriteLine($"null_secondary epoch={epoch} births={metric(46)} reclaims={reclaimed}");
                Check(reclaimed == 0,
                    "RC reclaimed a null-secondary primary while its uncleared dependent handle could still return it.");
                Check(IsNullSecondaryOriginal(dependent), "A null-secondary handle returned a different primary identity.");
            }
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Check(dependent.TargetAndDependent == (null, null) && metric(47) == 1,
                "Full weak cleanup did not clear the nontracing null-secondary primary and reclaim its original identity.");
            VerifyWatchedReplacement(old, address, collect, metric);
            Check(dependent.TargetAndDependent == (null, null), "A cleared dependent handle exposed its address replacement.");
        }
        finally
        {
            dependent.Dispose();
        }
    }

    private static void VerifyWeakIdentity(nint module, Collect collect, GetMetric metric)
    {
        HandleAddress address = Marshal.GetDelegateForFunctionPointer<HandleAddress>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetHandleAddress"));
        ArmLifetime arm = Marshal.GetDelegateForFunctionPointer<ArmLifetime>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_ArmLifetimeProbe"));
        (GCHandle shortWeak, GCHandle longWeak, nint old) = CreateWeakOnly(address, arm);
        try
        {
            for (int epoch = 0; epoch < 4; epoch++)
            {
                Check(collect(1) == 0 && metric(47) == 0,
                    "A weak-only referent lost its RC lease before suspended weak cleanup.");
            }
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Check(shortWeak.Target is null && longWeak.Target is null && metric(47) == 1,
                "One full collection did not clear weak-only handles and reclaim their original identity.");
            VerifyWatchedReplacement(old, address, collect, metric);
            Check(shortWeak.Target is null && longWeak.Target is null,
                "Cleared weak handles exposed a different identity at the same address.");
            shortWeak.Free();
            longWeak.Free();
            GCHandle weak = CreateUnregisteredWeak();
            weak.Free();
            weak = CreateUnregisteredWeak();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Check(weak.Target is null, "Handle unregister/reuse retained an unrelated original weak identity.");
            weak.Free();
        }
        finally
        {
            if (shortWeak.IsAllocated) { shortWeak.Free(); }
            if (longWeak.IsAllocated) { longWeak.Free(); }
        }
    }

    private static void VerifyLifecycleChurn(Collect collect)
    {
        int before = Volatile.Read(ref ClosureFinalizer.s_runs);
        for (int round = 0; round < 16; round++)
        {
            DependentHandle[] handles = CreateCostFinalizers();
            try
            {
                if ((round & 1) == 0)
                {
                    Check(collect(2) == 0 && collect(1) == 0 && collect(3) == 0,
                        "Concurrent/RC/FinalMark lifetime churn failed.");
                }
                else
                {
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                }
                CheckCostDependents(handles);
                GC.WaitForPendingFinalizers();
                Check(Volatile.Read(ref ClosureFinalizer.s_runs) == before + (round + 1) * 128,
                    "Repeated lifetime churn lost or duplicated same-cycle finalizer delivery.");
            }
            finally
            {
                foreach (DependentHandle handle in handles)
                {
                    handle.Dispose();
                }
            }
        }
    }

    private sealed class CollectingFinalizer
    {
        internal static int s_runs;
        internal static int s_thread;
        internal static int s_error;

        ~CollectingFinalizer()
        {
            Node value = new(8401) { Next = new Node(8403) };
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
            if (value.Value != 8401 || value.Next is not Node { Value: 8403 })
            {
                Volatile.Write(ref s_error, 1);
            }
            Volatile.Write(ref s_thread, Environment.CurrentManagedThreadId);
            Interlocked.Increment(ref s_runs);
            GC.KeepAlive(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateCollectingFinalizer() => GC.KeepAlive(new CollectingFinalizer());

    private static void VerifyFinalizerReentrancy()
    {
        int before = Volatile.Read(ref CollectingFinalizer.s_runs);
        int caller = Environment.CurrentManagedThreadId;
        CreateCollectingFinalizer();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        Check(Volatile.Read(ref CollectingFinalizer.s_runs) == before + 1 &&
            Volatile.Read(ref CollectingFinalizer.s_thread) != caller &&
            Volatile.Read(ref CollectingFinalizer.s_error) == 0,
            "Real finalizer-thread nested collection/wait failed or lost its transitive roots.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitializeCollectibleStatics(FieldInfo root, FieldInfo counter)
    {
        root.SetValue(null, new Node(8301));
        counter.SetValue(null, 8303);
    }

    private static void VerifyCollectibleStaticInteriors()
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("LxrWeakInteriorStatics"), AssemblyBuilderAccess.RunAndCollect);
        TypeBuilder builder = assembly.DefineDynamicModule("Probe").DefineType("StaticProbe", TypeAttributes.Public);
        builder.DefineField("Root", typeof(object), FieldAttributes.Public | FieldAttributes.Static);
        builder.DefineField("Counter", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
        Type type = builder.CreateType() ?? throw new InvalidOperationException("Collectible static type creation failed.");
        FieldInfo root = type.GetField("Root") ?? throw new InvalidOperationException("Collectible root field is missing.");
        FieldInfo counter = type.GetField("Counter") ?? throw new InvalidOperationException("Collectible integer field is missing.");
        InitializeCollectibleStatics(root, counter);
        for (int round = 0; round < 8; round++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Check(root.GetValue(null) is Node { Value: 8301 } && counter.GetValue(null) is int value &&
                value == 8303 + round, "Collectible GC/non-GC weak-interior static owners lost their payload or relocation.");
            counter.SetValue(null, 8304 + round);
        }
        GC.KeepAlive(type);
        GC.KeepAlive(assembly);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateCollectibleCycles(int round)
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("LxrCollectibleCycles" + round), AssemblyBuilderAccess.RunAndCollect);
        TypeBuilder builder = assembly.DefineDynamicModule("Cycles").DefineType("Cell", TypeAttributes.Public);
        builder.DefineField("Next", typeof(object), FieldAttributes.Public);
        builder.DefineField("Payload", typeof(long), FieldAttributes.Public);
        Type type = builder.CreateType() ?? throw new InvalidOperationException("Collectible cell type creation failed.");
        FieldInfo next = type.GetField("Next") ?? throw new InvalidOperationException("Collectible field is missing.");
        const int Count = 96;
        object[] cells = new object[Count];
        Array all = Array.CreateInstance(type, Count + round % 7);
        for (int index = 0; index < Count; index++)
        {
            cells[index] = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Collectible birth failed.");
            all.SetValue(cells[index], index);
        }
        for (int index = 0; index < Count; index++)
        {
            next.SetValue(cells[index], index == Count - 1 ? all : cells[index + 1]);
        }
        return new WeakReference(assembly);
    }

    // Dead cycles of objects of a collectible type: the lazy cycle sweep of a tracing pause reads their sizes from the
    // objects, while the runtime frees the type data from the finalizer thread once it learns that the loader is dead.
    // The collector must not let that happen before the sweep has finished. Many rounds make the race likely.
    private static void VerifyCollectibleCycleUnload()
    {
        // A large live set makes the lazy sweep long, so the finalizer thread has time to free type data if it may.
        Node[] ballast = new Node[150000];
        for (int index = 0; index < ballast.Length; index++)
        {
            ballast[index] = new Node(index);
        }
        int unloaded = 0;
        for (int round = 0; round < 40; round++)
        {
            WeakReference weak = CreateCollectibleCycles(round);
            for (int attempt = 0; attempt < 30 && weak.IsAlive; attempt++)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();
                CreateAcyclicGarbage(512);
            }
            if (!weak.IsAlive)
            {
                unloaded++;
            }
        }
        Console.WriteLine($"collectible_cycles unloaded={unloaded} of 40");
        Check(unloaded >= 30, "Dead cycles of collectible-type objects did not unload their loader allocators.");
        GC.KeepAlive(ballast);
    }

    private static int RunLifecycle(string scenario, string path)
    {
        Collect? collect = null;
        GetMetric? metric = null;
        nint module = 0;
        bool lxr = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCPath"));
        if (lxr)
        {
            module = NativeLibrary.Load(path);
            metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
            collect = Marshal.GetDelegateForFunctionPointer<Collect>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_Collect"));
            Check(metric(0) == 1 && metric(1) > 0,
                "Lifecycle qualification requires the initialized owning LXR collector.");
            Check(metric(78) == (Environment.GetEnvironmentVariable("DOTNET_LXREvacuateNursery") == "1" ? 1ul : 0ul) &&
                metric(90) == (Environment.GetEnvironmentVariable("DOTNET_LXREvacuateMature") == "1" ? 1ul : 0ul),
                "Lifecycle qualification loaded different movement modes.");
        }
        else
        {
            Check(scenario == "finalizer-closure" && GCSettings.IsServerGC ==
                (Environment.GetEnvironmentVariable("DOTNET_gcServer") == "1"),
                "The built-in lifecycle control loaded the wrong category or scenario.");
        }
        Console.WriteLine($"lifecycle={scenario} module={Path.GetFullPath(path)} sha256={Hash(path)}");
        switch (scenario)
        {
            case "finalizer-closure":
                VerifySingleCollectionFinalizerClosure();
                break;
            case "dependent":
                if (collect is null)
                {
                    throw new InvalidOperationException("Dependent qualification requires LXR collection controls.");
                }
                VerifySingleCollectionDependentClosure(collect);
                break;
            case "weak-finalizer":
                VerifyFinalization();
                VerifyFinalizerBatch();
                break;
            case "reregister":
                VerifyReregistration();
                break;
            case "weak-identity":
                if (collect is null || metric is null)
                {
                    throw new InvalidOperationException("Weak identity qualification requires LXR collection controls.");
                }
                VerifyWeakIdentity(module, collect, metric);
                break;
            case "dependent-null":
                if (collect is null || metric is null)
                {
                    throw new InvalidOperationException("Null-secondary qualification requires LXR collection controls.");
                }
                VerifyNullSecondary(module, collect, metric);
                break;
            case "churn":
                if (collect is null)
                {
                    throw new InvalidOperationException("Lifetime churn requires LXR collection controls.");
                }
                VerifyLifecycleChurn(collect);
                break;
            case "weak-interior-statics":
                VerifyCollectibleStaticInteriors();
                break;
            case "collectible-cycles":
                VerifyCollectibleCycleUnload();
                break;
            case "finalizer-reentrant":
                VerifyFinalizerReentrancy();
                break;
            case "refcounted":
                if (collect is null)
                {
                    throw new InvalidOperationException("Ref-counted qualification requires LXR collection controls.");
                }
                VerifyRefCountedRoot(collect);
                break;
            case "finalizer-churn":
                VerifyFinalizerChurn(metric);
                break;
            case "finalizer-churn-concurrent":
                VerifyConcurrentFinalizerChurn();
                break;
            case "weak-churn":
                VerifyWeakChurn();
                break;
            case "resurrection":
                VerifyResurrection();
                break;
            case "prompt-collect":
                VerifyPromptExplicitCollect();
                break;
            case "pooled-reregister":
                VerifyPooledReregistration(metric);
                break;
            case "pin-handles-churn":
                VerifyPinnedHandleChurn();
                break;
            case "fixed-pressure":
                VerifyFixedUnderPressure();
                break;
            case "poh-churn":
                VerifyPinnedObjectHeapChurn();
                break;
            case "pin-finalizers-weak":
                VerifyPinnedFinalizersAndWeak();
                break;
            case "r2r-stores":
                if (collect is null || metric is null)
                {
                    throw new InvalidOperationException("The framework store scenario requires LXR collection controls.");
                }
                VerifyFrameworkOldValueStores(metric, collect);
                break;
            case "young-interior":
                if (collect is null)
                {
                    throw new InvalidOperationException("The young interior scenario requires LXR collection controls.");
                }
                VerifyYoungInteriorRoots(collect);
                break;
            case "alloc-storm":
                if (collect is null)
                {
                    throw new InvalidOperationException("The allocation storm requires LXR collection controls.");
                }
                VerifyAllocationStorm(collect);
                break;
            case "alloc-sizes":
                if (collect is null)
                {
                    throw new InvalidOperationException("The allocation size scenario requires LXR collection controls.");
                }
                VerifyAllocationSizes(collect);
                break;
            case "pin-concurrent-trace":
                if (collect is null)
                {
                    throw new InvalidOperationException("The pin trace scenario requires LXR collection controls.");
                }
                VerifyPinsDuringConcurrentTrace(collect);
                break;
            default:
                throw new ArgumentException("Unknown lifecycle scenario.", nameof(scenario));
        }
        Console.WriteLine($"PASS: {s_checks} lifecycle checks");
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GCHandle[] CreateCostRoots()
    {
        GCHandle[] handles = new GCHandle[4096];
        for (int index = 0; index < handles.Length; index++)
        {
            handles[index] = GCHandle.Alloc(new Node(index) { Next = new Node(index ^ 0x55) });
        }
        return handles;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CheckCostRoots(GCHandle[] handles)
    {
        long sum = 0;
        for (int index = 0; index < handles.Length; index++)
        {
            Check(handles[index].Target is Node node && node.Value == index &&
                node.Next is Node child && child.Value == (index ^ 0x55),
                "The fixed root-heavy graph lost an original identity or edge.");
            sum += index;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static DependentHandle[] CreateCostFinalizers()
    {
        DependentHandle[] handles = new DependentHandle[64];
        for (int index = 0; index < handles.Length; index++)
        {
            ClosureFinalizer parent = new() { Child = new ClosureFinalizer() };
            handles[index] = new DependentHandle(parent, new Node(index) { Next = new Node(index ^ 0x55) });
        }
        return handles;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckCostDependents(DependentHandle[] handles)
    {
        for (int index = 0; index < handles.Length; index++)
        {
            (object? target, object? dependent) = handles[index].TargetAndDependent;
            Check(target is ClosureFinalizer && dependent is Node node &&
                node.Value == index && node.Next is Node child && child.Value == (index ^ 0x55),
                "Post-finalization dependent promotion did not complete within the same collection.");
        }
    }

    private static int RunLifecycleCost(string[] args)
    {
        if (args.Length != 6 || args[1] is not ("empty" or "roots" or "lifetimes") ||
            args[2] is not ("LXR" or "WKS" or "Server") ||
            args[4] is not ("off" or "diagnostic") || args[5] is not ("normal" or "injected"))
        {
            throw new ArgumentException("Use --p7-cost empty|roots|lifetimes LXR|WKS|Server module off|diagnostic normal|injected.", nameof(args));
        }
        GetMetric? metric = null;
        string identityText = args[2];
        bool diagnostic = args[4] == "diagnostic";
        bool scanCosts = false;
        Check(GCSettings.IsServerGC == (args[2] == "Server"), "The cost arm loaded the wrong collector category.");
        if (args[2] == "LXR")
        {
            nint module = NativeLibrary.Load(args[3]);
            GetIdentity identity = Marshal.GetDelegateForFunctionPointer<GetIdentity>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetIdentity"));
            identityText = Marshal.PtrToStringUTF8(identity())!;
            scanCosts = identityText.Contains("lifecycle-costs-v1", StringComparison.Ordinal);
            metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
            Check(metric(0) == 1 && metric(1) > 0 && metric(78) ==
                (Environment.GetEnvironmentVariable("DOTNET_LXREvacuateNursery") == "1" ? 1ul : 0ul) &&
                metric(90) == (Environment.GetEnvironmentVariable("DOTNET_LXREvacuateMature") == "1" ? 1ul : 0ul),
                "The cost arm did not select the declared owning collector and modes.");
            if (diagnostic)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                EnableStorage enable = Marshal.GetDelegateForFunctionPointer<EnableStorage>(
                    NativeLibrary.GetExport(module, "GC_LxrPrototype_EnableStorageDiagnostics"));
                Check(enable() == 0, "Cost instrumentation did not activate at a drained inactive boundary.");
            }
        }
        else
        {
            Check(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCPath")),
                "A built-in cost arm inherited GCPath.");
        }
        GCHandle[] roots = args[1] == "roots" ? CreateCostRoots() : [];
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        using Process process = Process.GetCurrentProcess();
        ulong[]? before = scanCosts && diagnostic ? new ulong[88] : null;
        if (before is not null && metric is not null)
        {
            for (uint index = 0; index < before.Length; index++)
            {
                before[index] = metric(118 + index);
            }
        }
        ulong[] ordinary = [5, 8, 12, 13, 14, 23, 24, 79, 80, 95, 96];
        ulong?[] counts = new ulong?[ordinary.Length];
        for (int index = 0; index < ordinary.Length; index++)
        {
            counts[index] = metric?.Invoke((uint)ordinary[index]);
        }
        int finalizersBefore = Volatile.Read(ref ClosureFinalizer.s_runs);
        int threadsBefore = process.Threads.Count;
        int collectionsBefore = GC.CollectionCount(2);
        TimeSpan pauseBefore = GC.GetTotalPauseDuration();
        TimeSpan cpuBefore = process.TotalProcessorTime;
        double[] calls = new double[8];
        double[] delivery = new double[8];
        long checksum = 0;
        long start = Stopwatch.GetTimestamp();
        for (int round = 0; round < calls.Length; round++)
        {
            DependentHandle[] handles = args[1] == "lifetimes" ? CreateCostFinalizers() : [];
            long callStart = Stopwatch.GetTimestamp();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            calls[round] = Stopwatch.GetElapsedTime(callStart).TotalMilliseconds;
            if (handles.Length != 0)
            {
                CheckCostDependents(handles);
            }
            long deliveryStart = Stopwatch.GetTimestamp();
            GC.WaitForPendingFinalizers();
            delivery[round] = Stopwatch.GetElapsedTime(deliveryStart).TotalMilliseconds;
            int finalized = Volatile.Read(ref ClosureFinalizer.s_runs) - finalizersBefore;
            Check(finalized == (args[1] == "lifetimes" ? (round + 1) * 128 : 0),
                "A full request omitted required same-cycle finalizer discovery or delivery.");
            checksum += args[1] switch
            {
                "roots" => CheckCostRoots(roots),
                "lifetimes" => finalized,
                _ => 1,
            };
            foreach (DependentHandle handle in handles)
            {
                handle.Dispose();
            }
            if (args[5] == "injected")
            {
                Thread.Sleep(25);
            }
        }
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        double cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
        double pause = (GC.GetTotalPauseDuration() - pauseBefore).TotalMilliseconds;
        int collections = GC.CollectionCount(2) - collectionsBefore;
        Check(checksum == (args[1] switch { "roots" => 67092480, "lifetimes" => 4608, _ => 8 }),
            "Fixed lifecycle cost inputs/checksum changed.");
        ulong?[]? scanDelta = before is null ? null : new ulong?[before.Length];
        if (scanDelta is not null && before is not null && metric is not null)
        {
            for (uint index = 0; index < before.Length; index++)
            {
                bool measured = index < 44 ? index <= 40 || index == 42 : index >= 48 && index <= 75;
                scanDelta[index] = measured ? metric(118 + index) - before[index] : null;
            }
        }
        ulong?[] countDelta = new ulong?[counts.Length];
        for (int index = 0; index < counts.Length; index++)
        {
            countDelta[index] = metric?.Invoke((uint)ordinary[index]) - counts[index];
        }
        process.Refresh();
        int threadsAfter = process.Threads.Count;
        long rss = process.WorkingSet64;
        long privateBytes = process.PrivateMemorySize64;
        TimeSpan idleCpuStart = process.TotalProcessorTime;
        Thread.Sleep(100);
        double idleCpu = (process.TotalProcessorTime - idleCpuStart).TotalMilliseconds;
        Dictionary<string, object?> record = new()
        {
            ["scenario"] = args[1], ["collector"] = args[2], ["instrumentation"] = args[4],
            ["control"] = args[5], ["identity"] = identityText, ["module_sha256"] = Hash(args[3]),
            ["server"] = GCSettings.IsServerGC, ["nursery"] = metric?.Invoke(78), ["mature"] = metric?.Invoke(90),
            ["elapsed_ms"] = elapsed, ["cpu_ms"] = cpu, ["pause_ms"] = pause,
            ["primary_full_calls"] = 8, ["collections"] = collections, ["checksum"] = checksum,
            ["finalizers"] = Volatile.Read(ref ClosureFinalizer.s_runs) - finalizersBefore,
            ["full_call_ms"] = calls, ["delivery_wait_ms"] = delivery,
            ["threads_before"] = threadsBefore, ["threads_after"] = threadsAfter, ["idle_cpu_100ms"] = idleCpu,
            ["rss_bytes"] = rss, ["private_bytes"] = privateBytes,
            ["native_planes"] = metric?.Invoke(38), ["managed_live"] = metric?.Invoke(3),
            ["ordinary_metric_ids"] = ordinary, ["ordinary_metric_deltas"] = countDelta,
            ["scan_wall_counts_then_cpu_ns"] = scanDelta, ["clock_frequency"] = metric?.Invoke(114),
        };
        Console.WriteLine("lifecycle_cost=" + JsonSerializer.Serialize(record));
        foreach (GCHandle root in roots)
        {
            root.Free();
        }
        return 0;
    }

    private static Dictionary<string, object?> LogSnapshot(GetMetric? metric, bool logMetrics, bool diagnostics)
    {
        using Process process = Process.GetCurrentProcess();
        process.Refresh();
        Dictionary<string, object?> snapshot = new()
        {
            ["rss_bytes"] = process.WorkingSet64,
            ["private_bytes"] = process.PrivateMemorySize64,
            ["managed_committed_bytes"] = metric?.Invoke(26),
            ["managed_admitted_bytes"] = metric?.Invoke(36),
            ["managed_live_bytes"] = metric?.Invoke(3),
            ["native_plane_committed_bytes"] = metric?.Invoke(38),
            ["work_owned_bytes"] = diagnostics ? metric?.Invoke(49) : null,
            ["work_high_water_bytes"] = diagnostics ? metric?.Invoke(50) : null,
            ["scan_owned_bytes"] = diagnostics ? metric?.Invoke(53) : null,
            ["scan_high_water_bytes"] = diagnostics ? metric?.Invoke(54) : null,
            ["ingress_occupied_entries"] = null,
            ["exclusive_gc_cpu_ms"] = null,
        };
        if (metric is not null)
        {
            List<Dictionary<string, object?>> streams = new();
            uint[] payloadMetrics = [59, 61, 65, 67];
            for (uint stream = 0; stream < 4; stream++)
            {
                ulong?[] counters = new ulong?[6];
                if (logMetrics)
                {
                    for (uint counter = 0; counter < counters.Length; counter++)
                    {
                        ulong value = metric(208 + stream * 6 + counter);
                        counters[counter] = value == ulong.MaxValue ? null : value;
                    }
                }
                streams.Add(new()
                {
                    ["bank"] = stream / 2, ["increment"] = stream % 2 == 0,
                    ["payload_committed_bytes"] = metric(payloadMetrics[stream]),
                    ["publication_committed_bytes"] = metric(payloadMetrics[stream] + 1),
                    ["capacity_entries"] = counters[0], ["reserved_entries"] = counters[1],
                    ["high_water_entries"] = counters[2], ["flushes"] = counters[3],
                    ["copied_bytes"] = counters[4], ["reset_header_bytes"] = counters[5],
                });
            }
            snapshot["streams"] = streams;
        }
        if (diagnostics && metric is not null)
        {
            nint module = NativeLibrary.Load(Environment.GetEnvironmentVariable("DOTNET_GCPath")!);
            StorageRange range = Marshal.GetDelegateForFunctionPointer<StorageRange>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetStorageRange"));
            List<Dictionary<string, object?>> components = new();
            for (uint component = 0; component < metric(76); component++)
            {
                Check(range(component, out nuint address, out nuint bytes) == 0,
                    "Log study could not inspect an actual storage descriptor.");
                components.Add(new()
                {
                    ["component"] = component, ["start"] = (ulong)address,
                    ["reserved_coverage_bytes"] = (ulong)bytes,
                    ["resident_bytes"] = bytes == 0 ? 0 : OwnedResidentBytes(address, bytes),
                });
            }
            snapshot["components"] = components;
        }
        return snapshot;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dictionary<string, object?> RunLogBody(
        string input, int count, bool injected, GetMetric? metric, bool logMetrics, bool diagnostics)
    {
        object roots = input == "references" ? CreateLogReferences(count) : CreateLogBytes(count);
        for (int warmup = 0; warmup < 2; warmup++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
        if (diagnostics && metric is not null)
        {
            nint module = NativeLibrary.Load(Environment.GetEnvironmentVariable("DOTNET_GCPath")!);
            EnableStorage enable = Marshal.GetDelegateForFunctionPointer<EnableStorage>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_EnableStorageDiagnostics"));
            Check(enable() == 0, "Log diagnostics must start after all previous readers drain.");
        }
        Dictionary<string, object?> initial = LogSnapshot(metric, logMetrics, diagnostics);
        using Process process = Process.GetCurrentProcess();
        double[] calls = new double[8];
        int collectionsBefore = GC.CollectionCount(2);
        long pauseBefore = GC.GetTotalPauseDuration().Ticks;
        TimeSpan cpuBefore = process.TotalProcessorTime;
        long checksum = 0;
        long expectedChecksum = 0;
        long started = Stopwatch.GetTimestamp();
        for (int cycle = 0; cycle < calls.Length; cycle++)
        {
            if (roots is Node[] nodes)
            {
                for (int index = 0; index < nodes.Length; index++)
                {
                    nodes[index].Next = nodes[(index + cycle + 1) & (count - 1)];
                }
            }
            else if (roots is byte[][] blocks)
            {
                for (int index = 0; index < blocks.Length; index++)
                {
                    blocks[index][0] = blocks[index][^1] = (byte)((index + cycle) % 251);
                }
            }
            long call = Stopwatch.GetTimestamp();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            calls[cycle] = Stopwatch.GetElapsedTime(call).TotalMilliseconds;
            if (roots is Node[] retained)
            {
                for (int index = 0; index < retained.Length; index++)
                {
                    int expected = (index + cycle + 1) & (count - 1);
                    Check(retained[index].Next is not null && retained[index].Next!.Value == expected,
                        "A completed log-study cycle lost a reference-rich edge.");
                    checksum += retained[index].Next!.Value;
                    expectedChecksum += expected;
                }
            }
            else if (roots is byte[][] retainedBytes)
            {
                for (int index = 0; index < retainedBytes.Length; index++)
                {
                    int expected = (index + cycle) % 251;
                    Check(retainedBytes[index][0] == expected && retainedBytes[index][^1] == expected,
                        "A completed log-study cycle changed pointer-free payload.");
                    checksum += retainedBytes[index][0] + retainedBytes[index][^1];
                    expectedChecksum += 2 * expected;
                }
            }
        }
        if (injected)
        {
            Thread.Sleep(200);
        }
        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        double cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
        double pause = TimeSpan.FromTicks(GC.GetTotalPauseDuration().Ticks - pauseBefore).TotalMilliseconds;
        int collections = GC.CollectionCount(2) - collectionsBefore;
        Check(checksum == expectedChecksum && collections == 8,
            "Log-study inputs, checksum or actual completed full-collection count changed.");
        Dictionary<string, object?> record = new()
        {
            ["input"] = input, ["count"] = count, ["checksum"] = checksum,
            ["primary_full_calls"] = calls.Length, ["collections"] = collections,
            ["full_call_ms"] = calls, ["elapsed_ms"] = elapsed, ["cpu_ms"] = cpu,
            ["pause_ms"] = pause, ["injected"] = injected, ["diagnostics"] = diagnostics,
            ["before"] = initial, ["live"] = LogSnapshot(metric, logMetrics, diagnostics),
            ["warmup_full_calls"] = 2,
        };
        GC.KeepAlive(roots);
        return record;
    }

    private static Node[] CreateLogReferences(int count)
    {
        Node[] nodes = new Node[count];
        for (int index = 0; index < nodes.Length; index++)
        {
            nodes[index] = new Node(index);
            if (index != 0)
            {
                nodes[index].Next = nodes[index - 1];
            }
        }
        return nodes;
    }

    private static byte[][] CreateLogBytes(int count)
    {
        byte[][] blocks = new byte[count / 256][];
        for (int index = 0; index < blocks.Length; index++)
        {
            blocks[index] = new byte[48 * 256];
        }
        return blocks;
    }

    private static int RunLogStudy(string[] args)
    {
        if (args.Length != 7 || args[1] is not ("bytes" or "references") ||
            args[2] is not ("LXR" or "WKS" or "Server") ||
            args[5] is not ("normal" or "injected") || args[6] is not ("off" or "on"))
        {
            throw new ArgumentException(
                "Use --log-study bytes|references LXR|WKS|Server module count normal|injected off|on.", nameof(args));
        }
        int count = int.Parse(args[4], CultureInfo.InvariantCulture);
        Check(count is 65536 or 131072 or 262144, "Log-study size is not a declared power-of-two input.");
        Check(GCSettings.IsServerGC == (args[2] == "Server"), "Log-study collector category mismatch.");
        GetMetric? metric = null;
        bool logMetrics = false;
        bool diagnostics = args[6] == "on";
        string path = Path.GetFullPath(args[3]);
        if (args[2] == "LXR")
        {
            nint module = NativeLibrary.Load(path);
            metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
            GetIdentity identity = Marshal.GetDelegateForFunctionPointer<GetIdentity>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetIdentity"));
            string text = Marshal.PtrToStringUTF8(identity())!;
            logMetrics = text.Contains("log-publication-v1", StringComparison.Ordinal) ||
                text.Contains("log-metrics-v1", StringComparison.Ordinal);
            Check(metric(0) == 1, "Log-study did not load the owning collector.");
        }
        else
        {
            Check(Environment.GetEnvironmentVariable("DOTNET_GCPath") is null,
                "Built-in log-study characterization inherited an external collector.");
        }
        Dictionary<string, object?> record = RunLogBody(args[1], count, args[5] == "injected",
            metric, logMetrics, diagnostics);
        long cleanup = Stopwatch.GetTimestamp();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        record["cleanup_full_calls"] = 1;
        record["cleanup_ms"] = Stopwatch.GetElapsedTime(cleanup).TotalMilliseconds;
        record["quiescent"] = LogSnapshot(metric, logMetrics, diagnostics);
        record["collector"] = args[2];
        record["nursery"] = metric?.Invoke(78);
        record["mature"] = metric?.Invoke(90);
        record["module_sha256"] = Hash(path);
        record["checks"] = s_checks;
        List<Dictionary<string, string>> modules = new();
        using (Process process = Process.GetCurrentProcess())
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (Path.GetFileName(module.FileName) is "coreclr.dll" or "clrjit.dll" or "clrlxrgc.dll" or
                    "libcoreclr.so" or "libclrjit.so" or "libclrlxrgc.so" or "corerun" or "corerun.exe")
                {
                    modules.Add(new() { ["path"] = module.FileName, ["sha256"] = Hash(module.FileName) });
                }
            }
        }
        record["loaded_native_modules"] = modules;
        string corelib = Path.Combine(Path.GetDirectoryName(path)!, "System.Private.CoreLib.dll");
        record["framework_corelib_input"] = new Dictionary<string, string>
        {
            ["path"] = corelib,
            ["sha256"] = Hash(corelib),
        };
        Console.WriteLine("log_study=" + JsonSerializer.Serialize(record));
        return 0;
    }

    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--dependent-scope")
        {
            return RunDependentScopeControl(args);
        }
        if (args.Length > 0 && args[0] == "--log-study")
        {
            return RunLogStudy(args);
        }
        if (args.Length == 2 && args[0] == "--retention-native-allocator")
        {
            if (!OperatingSystem.IsLinux())
            {
                throw new PlatformNotSupportedException("Native allocator XML diagnostics require the qualified glibc Linux host.");
            }
            s_nativeAllocatorDiagnostic = true;
            return RunRetention(args[1], pointerRich: true);
        }
        if (args.Length > 0 && args[0] == "--p7-cost")
        {
            return RunLifecycleCost(args);
        }
        if (args.Length == 3 && args[0] == "--p7-lifecycle")
        {
            return RunLifecycle(args[1], args[2]);
        }
        if (args.Length > 0 && args[0] == "--mature-study")
        {
            return RunMatureStudy(args);
        }
        if (args.Length == 2 && args[0] == "--mature")
        {
            return RunMature(args[1]);
        }
        if (args.Length == 2 && args[0] == "--nursery-default")
        {
            return RunDefaultNursery(args[1]);
        }
        if (args.Length > 0 && args[0] == "--nursery-study")
        {
            return RunNurseryStudy(args);
        }
        if (args.Length == 3 && args[0] == "--nursery")
        {
            return RunNursery(args[1], args[2]);
        }
        if (args.Length == 2 && args[0] is "--retention-bytes" or "--retention-references")
        {
            return RunRetention(args[1], args[0] == "--retention-references");
        }
        if (args.Length > 0 && args[0] == "--storage")
        {
            s_storageDiagnostic = true;
            args[0] = "--vertical";
        }
        if (args.Length == 2 && args[0] is "--lifetime-reuse" or "--lifetime-reuse-historical-control")
        {
            return RunLifetimeReuse(args[1], args[0] == "--lifetime-reuse-historical-control");
        }
        if (args.Length == 2 && args[0] == "--syncblock-diagnostic")
        {
            s_syncBlockDiagnostic = true;
            args = [args[1]];
        }
        if (args.Length > 0 && args[0] == "--vertical") { return RunVertical(args); }
        if (args.Length == 2 && args[0] == "--p8-birth") { return RunBirth(args[1]); }
        if (args.Length == 2 && args[0] == "--p8-single-growth") { return RunSingleGrowth(args[1]); }
        if (args.Length > 0 && args[0] == "--study")
        {
            return RunStudy(args);
        }
        bool oom = args.Length == 2 && args[0] == "--oom";
        bool unsupportedLoh = args.Length == 2 && args[0] == "--unsupported-loh";
        if (oom || unsupportedLoh)
        {
            args = [args[1]];
        }
        if (args.Length != 1)
        {
            throw new ArgumentException("Supply the exact selected LXR collector module path.", nameof(args));
        }
        string path = Path.GetFullPath(args[0]);
        nint module = NativeLibrary.Load(path);
        GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
        IsOwned owned = Marshal.GetDelegateForFunctionPointer<IsOwned>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_IsOwnedHandle"));
        GetIdentity identity = Marshal.GetDelegateForFunctionPointer<GetIdentity>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetIdentity"));
        Collect collect = Marshal.GetDelegateForFunctionPointer<Collect>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_Collect"));
        HoldTrace hold = Marshal.GetDelegateForFunctionPointer<HoldTrace>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_HoldTrace"));
        HandleAddress address = Marshal.GetDelegateForFunctionPointer<HandleAddress>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetHandleAddress"));
        AddressExtent extent = Marshal.GetDelegateForFunctionPointer<AddressExtent>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetAddressExtent"));
        ArmScan arm = Marshal.GetDelegateForFunctionPointer<ArmScan>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_ArmScanCheckpoint"));
        WaitForTrace wait = Marshal.GetDelegateForFunctionPointer<WaitForTrace>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_WaitForTrace"));
        Check(metric(0) == 1, "The collector was not initialized through GCPath.");
        Check(metric(1) > 0 && metric(2) > 0, "The selected collector did not own startup allocations.");
        string collectorIdentity = Marshal.PtrToStringUTF8(identity())!;
        // LXR is a custom collector (GC_HEAP_CUSTOM); the runtime reports a custom collector as Server GC, as for Satori.
        if (collectorIdentity.Contains("P6 nursery+mature", StringComparison.Ordinal))
        {
            string? nurseryMode = Environment.GetEnvironmentVariable("DOTNET_LXREvacuateNursery");
            string? matureMode = Environment.GetEnvironmentVariable("DOTNET_LXREvacuateMature");
            if (nurseryMode is "0" or "1")
            {
                Check(metric(78) == (nurseryMode == "1" ? 1ul : 0ul), "The actual nursery mode differs from the requested lifetime arm.");
            }
            if (matureMode is "0" or "1")
            {
                Check(metric(90) == (matureMode == "1" ? 1ul : 0ul), "The actual mature mode differs from the requested lifetime arm.");
            }
        }
        Console.WriteLine($"collector={collectorIdentity} module={path} sha256={Hash(path)}");
        if (unsupportedLoh)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            throw new InvalidOperationException("Unsupported compaction did not fail closed.");
        }
        if (oom)
        {
            VerifyOom(metric, collect, hold);
            Console.WriteLine($"PASS: {s_checks} selected-host OOM/recovery checks");
            return 0;
        }
        object[] roots = [new object(), new byte[100000], new string('x', 257)];
        GCHandle handle = GCHandle.Alloc(roots);
        try
        {
            Check(owned(GCHandle.ToIntPtr(handle)) == 1, "A real managed array is not in the owned LXR arenas.");
            for (int iteration = 0; iteration < 3; iteration++)
            {
                GC.Collect();
                Check(((byte[])roots[1]).Length == 100000 &&
                    ((string)roots[2]).Length == 257 && ((string)roots[2])[256] == 'x',
                    "The selected collector corrupted a reachable LOS/string graph.");
            }
            Check(metric(5) >= 3 && metric(12) >= 3, "GC.Collect did not execute LXR full traces.");
            Check(metric(8) > 0, "Real EE/handle roots were not copied into the owned engine.");
            Console.WriteLine("phase=cycles");
            VerifyCycles(metric);
            Console.WriteLine("phase=pinned-interior");
            VerifyPinnedAndInterior();
            Console.WriteLine("phase=finalization");
            VerifyFinalization();
            VerifyFinalizerBatch();
            Console.WriteLine("phase=dependent");
            VerifyDependentHandles();
            Console.WriteLine("phase=thread-churn");
            VerifyThreadChurn(metric);
            Console.WriteLine("phase=held-deletion");
            VerifyHeldDeletion(metric, collect, hold);
            Console.WriteLine("phase=satb-overflow");
            VerifySatbOverflow(metric, collect, hold);
            Console.WriteLine("phase=concurrent-mutation");
            VerifyConcurrentMutation(metric, collect, hold, arm, wait);
            Console.WriteLine("phase=collectible");
            VerifyCollectibleLifetime();
            Console.WriteLine("phase=refcounted-root");
            VerifyRefCountedRoot(collect);
            Console.WriteLine("phase=syncblock");
            ArmLifetime armLifetime = Marshal.GetDelegateForFunctionPointer<ArmLifetime>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_ArmLifetimeProbe"));
            VerifySyncBlockLifetime(collect, address, extent, metric, armLifetime);
            Console.WriteLine("phase=managed-range-reuse");
            VerifyManagedRangeReuse(address, extent, metric);
            VerifyMemoryInfo(expectConcurrent: true);
            Console.WriteLine($"owned_allocations={metric(1)} allocated_bytes={metric(2)} live_bytes={metric(3)} " +
                $"reclaimed_bytes={metric(4)} collections={metric(5)} full_traces={metric(12)} " +
                $"root_increments={metric(8)} checks={s_checks}");
        }
        finally
        {
            handle.Free();
        }
        GC.KeepAlive(roots);
        Console.WriteLine($"PASS: {s_checks} selected-host startup checks");
        return 0;
    }
}
