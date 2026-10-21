// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

internal static class Program
{
    private const uint StateMask = 0x00000003;
    private const uint Sentinel = 0xa5a50000;
    private static int s_checks;
    private static int s_finalizationCount;
    private static int s_clearControlApplied;
    private static int s_reuseControlApplied;

    private static int Main()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P16_NATIVE_HOOK_LIBRARY")
            ?? throw new InvalidOperationException("P16_NATIVE_HOOK_LIBRARY is required.");

        using NativeHooks hooks = NativeHooks.Load(libraryPath);
        ObjectHeaderBitsParameters parameters = hooks.GetParameters();
        Check(parameters.RequestStatus == 1, "request accepted");
        Check(parameters.Request == 1, "request enabled");
        Check(parameters.Version == 1, "descriptor version");
        Check(parameters.Size == (uint)Marshal.SizeOf<ObjectHeaderBitsParameters>(), "descriptor size");
        Check(parameters.RequestedBitCount == 2, "requested bit count");
        Check(parameters.RequestedStateCount == 3, "requested state count");
        Check(parameters.RequiredMemoryOrder == 1, "required SeqCst order");
        Check(parameters.ObjectByteOffset == -8, "object byte offset");
        Check(parameters.StorageWordSize == sizeof(uint), "storage word size");
        Check(parameters.BitMask == StateMask, "state mask");
        Check(parameters.GrantedMemoryOrder == 1, "granted SeqCst order");
        Check(parameters.ClearState == 0, "clear state");
        Check(parameters.InvalidState == 1, "invalid state");
        Check(parameters.TransitionState == 2, "transition state");
        Check(parameters.PublishedState == 3, "published state");

        object[] objects =
        [
            new object(),
            new object[16],
            new string('x', 64),
            42,
            new byte[100_000],
            GC.AllocateArray<byte>(256, pinned: true),
            new Finalizable(),
        ];

        foreach (object value in objects)
        {
            ExerciseObject(hooks, value);
        }

        ExerciseContention(hooks);
        ExerciseWaitPulse(hooks);
        ExerciseClaimRace(hooks);
        ExercisePublication(hooks);
        ExerciseClone(hooks);
        ExerciseFinalizationBits(hooks);
        ExercisePinnedCompaction(hooks);
        ExerciseCollectible(hooks);
        ExerciseRecycledAllocationClearing(hooks);

        Console.WriteLine($"{s_checks} object-header runtime checks passed");
        return 100;
    }

    private static void ExerciseObject(NativeHooks hooks, object value)
    {
        using Handle handle = new(value);
        hooks.SetRaw(handle, Sentinel);

        for (uint state = 0; state < 4; state++)
        {
            uint syncBlockBeforeStateChange = hooks.GetSyncBlockValue(handle);
            hooks.Set(handle, state);
            Check(hooks.Load(handle) == state, $"state {state} installed");
            Check((hooks.LoadRaw(handle) & ~StateMask) == Sentinel, $"state {state} preserves sentinel");
            Check(
                hooks.GetSyncBlockValue(handle) == syncBlockBeforeStateChange,
                $"state {state} leaves sync word");

            int firstHash = RuntimeHelpers.GetHashCode(value);
            int secondHash = RuntimeHelpers.GetHashCode(value);
            Check(firstHash == secondHash, $"state {state} hash stable");
            Check(hooks.Load(handle) == state, $"state {state} survives hash");

            lock (value)
            {
                lock (value)
                {
                    Check(Monitor.IsEntered(value), $"state {state} recursive lock owned");
                }
            }
            Check(hooks.Load(handle) == state, $"state {state} survives recursive lock");

            nuint before = hooks.GetObjectAddress(handle);
            ForceCompactingCollection();
            nuint after = hooks.GetObjectAddress(handle);
            if ((state == 2) &&
                (Environment.GetEnvironmentVariable("P16_CLEAR_AFTER_COMPACTION_CONTROL") == "1") &&
                (Interlocked.Exchange(ref s_clearControlApplied, 1) == 0))
            {
                hooks.SetRaw(handle, 0);
            }
            Check(before != 0 && after != 0, $"state {state} object address available");
            Check(hooks.Load(handle) == state, $"state {state} survives compacting GC");
        }

        hooks.SetRaw(handle, 0);
    }

    private static void ExerciseContention(NativeHooks hooks)
    {
        object gate = new();
        using Handle handle = new(gate);
        hooks.Set(handle, 2);
        int counter = 0;
        Parallel.For(
            0,
            8,
            _ =>
            {
                for (int iteration = 0; iteration < 2_000; iteration++)
                {
                    lock (gate)
                    {
                        counter++;
                    }
                }
            });

        Check(counter == 16_000, "contended lock count");
        Check(hooks.Load(handle) == 2, "transition survives contention and inflation");
        hooks.SetRaw(handle, 0);
    }

    private static void ExerciseWaitPulse(NativeHooks hooks)
    {
        object gate = new();
        using Handle handle = new(gate);
        hooks.Set(handle, 3);
        bool waiting = false;
        bool completed = false;

        Thread waiter = new(
            () =>
            {
                lock (gate)
                {
                    waiting = true;
                    Monitor.PulseAll(gate);
                    Monitor.Wait(gate);
                    completed = true;
                }
            });
        waiter.Start();

        lock (gate)
        {
            while (!waiting)
            {
                Monitor.Wait(gate);
            }
            Monitor.PulseAll(gate);
        }

        waiter.Join();
        Check(completed, "waiter completed");
        Check(hooks.Load(handle) == 3, "published survives wait/pulse");
        hooks.SetRaw(handle, 0);
    }

    private static void ExerciseClaimRace(NativeHooks hooks)
    {
        object value = new();
        using Handle handle = new(value);
        hooks.SetRaw(handle, Sentinel);
        int winners = 0;
        using Barrier barrier = new(33);
        Task[] tasks = new Task[32];

        for (int index = 0; index < tasks.Length; index++)
        {
            tasks[index] = Task.Run(
                () =>
                {
                    barrier.SignalAndWait();
                    if (hooks.CompareExchange(handle, 2, 0) == 0)
                    {
                        Interlocked.Increment(ref winners);
                    }
                });
        }

        barrier.SignalAndWait();
        Task.WaitAll(tasks);
        Check(winners == 1, "exactly one managed claim winner");
        Check(hooks.Load(handle) == 2, "claim race leaves transition");
        Check((hooks.LoadRaw(handle) & ~StateMask) == Sentinel, "claim race preserves sentinel");
        hooks.SetRaw(handle, 0);
    }

    private static void ExercisePublication(NativeHooks hooks)
    {
        int payload = 0;
        hooks.SetSynthetic(2);

        Task waiter = Task.Run(
            () =>
            {
                Check(hooks.WaitSynthetic(2) == 3, "wait observes published state");
                Check(Volatile.Read(ref payload) == 42, "publication exposes payload");
            });

        Volatile.Write(ref payload, 42);
        Check(hooks.SetSynthetic(3) == 2, "transition publishes");
        waiter.Wait();
        Check(hooks.SetSynthetic(0) == 3, "publication state observed");
    }

    private enum ReuseHeap
    {
        Small,
        Large,
        Pinned,
    }

    private static void ExerciseRecycledAllocationClearing(NativeHooks hooks)
    {
        foreach (ReuseHeap heap in Enum.GetValues<ReuseHeap>())
        {
            bool observed = ObserveRecycledAllocation(hooks, heap);
            int bound = GetReuseAttemptsPerCycle(heap) * 8;
            Console.WriteLine(
                $"P16_REUSE={heap};OBSERVED={(observed ? 1 : 0)};BOUND={bound}");
            if (heap != ReuseHeap.Small)
            {
                Check(observed, $"{heap} allocation reuses marked address");
            }
        }
    }

    private static bool ObserveRecycledAllocation(NativeHooks hooks, ReuseHeap heap)
    {
        nuint markedAddress = AllocateMarkedAndRelease(hooks, heap);
        int attemptsPerCycle = GetReuseAttemptsPerCycle(heap);

        for (int cycle = 0; cycle < 8; cycle++)
        {
            if (heap == ReuseHeap.Large)
            {
                GCSettings.LargeObjectHeapCompactionMode =
                    GCLargeObjectHeapCompactionMode.CompactOnce;
            }
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();

            for (int attempt = 0; attempt < attemptsPerCycle; attempt++)
            {
                if (AllocateAndCheckForReuse(hooks, heap, markedAddress))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int GetReuseAttemptsPerCycle(ReuseHeap heap) =>
        heap switch
        {
            ReuseHeap.Small =>
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCStress"))
                    ? 20_000
                    : 128,
            ReuseHeap.Large => 128,
            ReuseHeap.Pinned => 1_024,
            _ => throw new UnreachableException(),
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint AllocateMarkedAndRelease(NativeHooks hooks, ReuseHeap heap)
    {
        byte[] value = AllocateReuseObject(heap);
        using Handle handle = new(value);
        hooks.Set(handle, 3);
        Check(hooks.Load(handle) == 3, $"{heap} dying allocation is marked");
        return hooks.GetObjectAddress(handle);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AllocateAndCheckForReuse(
        NativeHooks hooks,
        ReuseHeap heap,
        nuint markedAddress)
    {
        byte[] value = AllocateReuseObject(heap);
        using Handle handle = new(value);
        if (hooks.GetObjectAddress(handle) != markedAddress)
        {
            return false;
        }

        if ((heap == ReuseHeap.Large) &&
            (Environment.GetEnvironmentVariable("P16_STALE_REUSE_CONTROL") == "1") &&
            (Interlocked.Exchange(ref s_reuseControlApplied, 1) == 0))
        {
            hooks.Set(handle, 3);
        }

        Check(hooks.Load(handle) == 0, $"recycled {heap} header starts clear");
        return true;
    }

    private static byte[] AllocateReuseObject(ReuseHeap heap) =>
        heap switch
        {
            ReuseHeap.Small => new byte[512],
            ReuseHeap.Large => new byte[100_000],
            ReuseHeap.Pinned => GC.AllocateArray<byte>(4_096, pinned: true),
            _ => throw new UnreachableException(),
        };

    private static void ExerciseClone(NativeHooks hooks)
    {
        Cloneable source = new();
        using Handle sourceHandle = new(source);
        hooks.Set(sourceHandle, 3);
        Cloneable clone = source.Clone();
        using Handle cloneHandle = new(clone);

        Check(hooks.Load(sourceHandle) == 3, "clone preserves source state");
        Check(hooks.Load(cloneHandle) == 0, "clone starts clear");
        hooks.SetRaw(sourceHandle, 0);
    }

    private static void ExerciseFinalizationBits(NativeHooks hooks)
    {
        Finalizable value = new();
        using Handle handle = new(value);
        hooks.Set(handle, 3);
        GC.SuppressFinalize(value);
        Check(hooks.Load(handle) == 3, "state survives SuppressFinalize");
        GC.ReRegisterForFinalize(value);
        Check(hooks.Load(handle) == 3, "state survives ReRegisterForFinalize");
        GC.SuppressFinalize(value);
        hooks.SetRaw(handle, 0);
    }

    private static void ExercisePinnedCompaction(NativeHooks hooks)
    {
        object pinned = new byte[4_096];
        using Handle normalHandle = new(pinned);
        GCHandle pinnedHandle = GCHandle.Alloc(pinned, GCHandleType.Pinned);
        try
        {
            hooks.Set(normalHandle, 2);

            for (int iteration = 0; iteration < 4; iteration++)
            {
                ForceCompactingCollection();
                Check(hooks.Load(normalHandle) == 2, "pinned plug preserves transition");
            }

            hooks.SetRaw(normalHandle, 0);
        }
        finally
        {
            pinnedHandle.Free();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExerciseCollectible(NativeHooks hooks)
    {
        AssemblyLoadContext context = new("P16Collectible", isCollectible: true);
        object value = new WeakReference(context, trackResurrection: false);
        using Handle handle = new(value);
        hooks.Set(handle, 3);
        context.Unload();
        ForceCompactingCollection();
        Check(hooks.Load(handle) == 3, "state survives collectible unload collection");
        hooks.SetRaw(handle, 0);
    }

    private static void ForceCompactingCollection()
    {
        _ = new byte[4 * 1024 * 1024];
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
        Interlocked.Increment(ref s_checks);
    }

    private sealed class Cloneable
    {
        public Cloneable Clone() => (Cloneable)MemberwiseClone();
    }

    private sealed class Finalizable
    {
        ~Finalizable()
        {
            Interlocked.Increment(ref s_finalizationCount);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ObjectHeaderBitsParameters
    {
        public uint RequestStatus;
        public uint Request;
        public uint Version;
        public uint Size;
        public uint RequestedBitCount;
        public uint RequestedStateCount;
        public uint RequestedProtocol;
        public uint RequiredAtomicOperations;
        public uint RequiredMemoryOrder;
        public int ObjectByteOffset;
        public uint StorageWordSize;
        public uint BitMask;
        public uint BitShift;
        public uint GrantedProtocol;
        public uint GrantedAtomicOperations;
        public uint GrantedMemoryOrder;
        public uint ClearState;
        public uint InvalidState;
        public uint TransitionState;
        public uint PublishedState;
    }

    private sealed class Handle : IDisposable
    {
        private GCHandle _handle;

        public Handle(object value)
        {
            _handle = GCHandle.Alloc(value);
        }

        public nint Value => GCHandle.ToIntPtr(_handle);

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }

    private sealed class NativeHooks : IDisposable
    {
        private readonly nint _library;
        private readonly GetParametersDelegate _getParameters;
        private readonly LoadDelegate _load;
        private readonly CompareExchangeDelegate _compareExchange;
        private readonly SetDelegate _set;
        private readonly SyntheticStateDelegate _setSynthetic;
        private readonly SyntheticStateDelegate _waitSynthetic;
        private readonly LoadDelegate _loadRaw;
        private readonly SetDelegate _setRaw;
        private readonly LoadDelegate _getSyncBlockValue;
        private readonly GetObjectAddressDelegate _getObjectAddress;

        private NativeHooks(nint library)
        {
            _library = library;
            _getParameters = GetDelegate<GetParametersDelegate>("GC_ObjectHeaderBitsTest_GetParameters");
            _load = GetDelegate<LoadDelegate>("GC_ObjectHeaderBitsTest_Load");
            _compareExchange =
                GetDelegate<CompareExchangeDelegate>("GC_ObjectHeaderBitsTest_CompareExchange");
            _set = GetDelegate<SetDelegate>("GC_ObjectHeaderBitsTest_Set");
            _setSynthetic =
                GetDelegate<SyntheticStateDelegate>("GC_ObjectHeaderBitsTest_SetSynthetic");
            _waitSynthetic =
                GetDelegate<SyntheticStateDelegate>("GC_ObjectHeaderBitsTest_WaitSynthetic");
            _loadRaw = GetDelegate<LoadDelegate>("GC_ObjectHeaderBitsTest_LoadRaw");
            _setRaw = GetDelegate<SetDelegate>("GC_ObjectHeaderBitsTest_SetRaw");
            _getSyncBlockValue =
                GetDelegate<LoadDelegate>("GC_ObjectHeaderBitsTest_GetSyncBlockValue");
            _getObjectAddress =
                GetDelegate<GetObjectAddressDelegate>("GC_ObjectHeaderBitsTest_GetObjectAddress");
        }

        public static NativeHooks Load(string path) => new(NativeLibrary.Load(path));

        public ObjectHeaderBitsParameters GetParameters()
        {
            Check(_getParameters(out ObjectHeaderBitsParameters parameters) == 1, "parameters exported");
            return parameters;
        }

        public uint Load(Handle handle) => _load(handle.Value);

        public uint CompareExchange(Handle handle, uint value, uint comparand) =>
            _compareExchange(handle.Value, value, comparand);

        public uint Set(Handle handle, uint value) => _set(handle.Value, value);

        public uint SetSynthetic(uint value) => _setSynthetic(value);

        public uint WaitSynthetic(uint value) => _waitSynthetic(value);

        public uint LoadRaw(Handle handle) => _loadRaw(handle.Value);

        public uint SetRaw(Handle handle, uint value) => _setRaw(handle.Value, value);

        public uint GetSyncBlockValue(Handle handle) => _getSyncBlockValue(handle.Value);

        public nuint GetObjectAddress(Handle handle) => _getObjectAddress(handle.Value);

        private T GetDelegate<T>(string name)
            where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library, name));

        public void Dispose() => NativeLibrary.Free(_library);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetParametersDelegate(out ObjectHeaderBitsParameters parameters);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint LoadDelegate(nint handle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint CompareExchangeDelegate(nint handle, uint value, uint comparand);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint SetDelegate(nint handle, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint SyntheticStateDelegate(uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate nuint GetObjectAddressDelegate(nint handle);
    }
}
