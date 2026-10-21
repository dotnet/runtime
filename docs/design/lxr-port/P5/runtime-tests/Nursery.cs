// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.Json;

internal static partial class Program
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint RegisterFrozen(nint memory, nuint capacity, nint array, nint target);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void UnregisterFrozen(nint segment);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint NativeCopy(nint destination, nint source, nuint bytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint VirtualAlloc(nint address, nuint size, uint allocation, uint protection);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualProtect(nint address, nuint size, uint protection, out uint previous);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualFree(nint address, nuint size, uint operation);

    [LibraryImport("libc", EntryPoint = "mmap", SetLastError = true)]
    private static partial nint Mmap(nint address, nuint size, int protection, int flags, int fd, nint offset);

    [LibraryImport("libc", EntryPoint = "mprotect", SetLastError = true)]
    private static partial int Mprotect(nint address, nuint size, int protection);

    [LibraryImport("libc", EntryPoint = "munmap", SetLastError = true)]
    private static partial int Munmap(nint address, nuint size);

    private static int RunNursery(string modulePath, string mode)
    {
        if (mode is not ("on" or "off"))
        {
            throw new ArgumentException("Specify the independently requested nursery mode on or off.", nameof(mode));
        }
        bool moving = mode == "on";
        Check(Environment.GetEnvironmentVariable("DOTNET_LXRNoMutatorLineRecycling") == "1",
            "Decisive movement inputs require fresh nursery sources, not the oracle's stationary recycled-line births.");
        Check(!GCSettings.IsServerGC, "The nursery test unexpectedly loaded Server GC.");
        nint module = NativeLibrary.Load(modulePath);
        GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
        HandleAddress address = Marshal.GetDelegateForFunctionPointer<HandleAddress>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetHandleAddress"));
        Collect collect = Marshal.GetDelegateForFunctionPointer<Collect>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_Collect"));
        Check(metric(0) == 1 && metric(1) > 0 && metric(78) == (moving ? 1ul : 0ul),
            "The requested nursery toggle did not select an initialized, owning collector.");
        VerifyMovingGraph(moving, metric, address, collect);
        VerifyNurseryPinsAndInteriors(moving, metric, address, collect);
        VerifyReadOnlyFrozen(moving, module, metric, address, collect);
        Console.WriteLine($"PASS: {s_checks} managed nursery checks mode={mode} " +
            $"copied_objects={metric(79)} copied_bytes={metric(80)} pinned_promotion_bytes={metric(81)} " +
            $"copy_failures={metric(82)} forwarding_waits={metric(83)} retired_source_bytes={metric(84)} " +
            $"survival_ppb={metric(39)} module_sha256={Hash(modulePath)}");
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GCHandle[] MakeAgedMatureCohort(HandleAddress address, Collect collect)
    {
        GCHandle[] handles = new GCHandle[512];
        for (int index = 0; index < handles.Length; index++)
        {
            byte[] value = new byte[4096];
            value[2000] = (byte)index;
            value[4095] = (byte)(index >> 8);
            handles[index] = GCHandle.Alloc(value);
        }
        Check(collect(1) == 0, "The mature cohort did not survive its first real RC pause.");
        HashSet<nint> blocks = new();
        for (int index = 0; index < handles.Length; index++)
        {
            nint block = address(GCHandle.ToIntPtr(handles[index])) & ~32767;
            if (!blocks.Add(block))
            {
                handles[index].Free();
                handles[index] = default;
            }
        }
        Check(collect(1) == 0, "The mature cohort's dead neighbors were not reclaimed.");
        return handles;
    }

    private static int RunMature(string modulePath)
    {
        Check(!GCSettings.IsServerGC, "The mature test unexpectedly loaded Server GC.");
        nint module = NativeLibrary.Load(modulePath);
        GetIdentity identity = Marshal.GetDelegateForFunctionPointer<GetIdentity>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetIdentity"));
        Check(Marshal.PtrToStringUTF8(identity())!.Contains("P6 nursery+mature", StringComparison.Ordinal),
            "The loaded collector does not advertise the mature metric surface.");
        GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
        HandleAddress address = Marshal.GetDelegateForFunctionPointer<HandleAddress>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetHandleAddress"));
        Collect collect = Marshal.GetDelegateForFunctionPointer<Collect>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_Collect"));
        Check(metric(0) == 1 && metric(1) > 0 && metric(90) == 1 && metric(87) == 1 && metric(88) == 1,
            "Mature movement must use the owning collector and default line recycling.");
        GCHandle[] handles = MakeAgedMatureCohort(address, collect);
        nint[] before = new nint[handles.Length];
        for (int index = 0; index < handles.Length; index++)
        {
            if (handles[index].IsAllocated)
            {
                before[index] = address(GCHandle.ToIntPtr(handles[index]));
            }
        }
        int pinnedIndex = Array.FindIndex(handles, static handle => handle.IsAllocated);
        int interiorIndex = Array.FindIndex(handles, pinnedIndex + 1, static handle => handle.IsAllocated);
        byte[] pinnedValue = (byte[])handles[pinnedIndex].Target!;
        byte[] interiorValue = (byte[])handles[interiorIndex].Target!;
        GCHandle pin = GCHandle.Alloc(pinnedValue, GCHandleType.Pinned);
        nint pinnedData = pin.AddrOfPinnedObject();
        int hash = RuntimeHelpers.GetHashCode(interiorValue);
        ref byte interior = ref interiorValue[2000];
        byte expected = interior;
        ulong copied = metric(96);
        ulong retired = metric(101);
        ulong released = metric(102);
        int moved = 0;
        Monitor.Enter(interiorValue);
        try
        {
            Check(collect(0) == 0, "Mature relocation and the full lifetime closure failed.");
            for (int index = 0; index < handles.Length; index++)
            {
                if (!handles[index].IsAllocated)
                {
                    continue;
                }
                nint after = address(GCHandle.ToIntPtr(handles[index]));
                moved += after != before[index] ? 1 : 0;
                Check(handles[index].Target is byte[] { Length: 4096 } value &&
                    value[2000] == (byte)index && value[4095] == (byte)(index >> 8),
                    "A genuine aged survivor lost logical identity or copied payload.");
            }
            Check(moved > 0 && metric(96) > copied && metric(101) - retired == metric(96) - copied &&
                metric(102) > released, "Mature copying did not recover whole source blocks.");
            Check(GC.GetGCMemoryInfo().Compacted, "A genuinely moving mature pause was reported as noncompacting.");
            Check(address(GCHandle.ToIntPtr(handles[pinnedIndex])) == before[pinnedIndex] &&
                pin.AddrOfPinnedObject() == pinnedData,
                "An ordinary alias discovered before a late pinned handle moved the pinned payload.");
            Check(interior == expected && RuntimeHelpers.GetHashCode(interiorValue) == hash &&
                Monitor.IsEntered(interiorValue), "Mature interiors, object hash or inflated monitor identity changed.");
            interior = 217;
            Check(((byte[])handles[interiorIndex].Target!)[2000] == 217,
                "The live managed interior does not address the relocated destination.");
        }
        finally
        {
            Monitor.Exit(interiorValue);
            pin.Free();
            foreach (GCHandle handle in handles)
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }
        VerifyMatureNativePins(address, collect);
        Console.WriteLine($"PASS: {s_checks} managed mature checks moved={moved} " +
            $"mature_copied_bytes={metric(96) - copied} mature_retired_bytes={metric(101) - retired} " +
            $"recovered_blocks={metric(102) - released} pinned_blocks={metric(93)} " +
            $"module_sha256={Hash(modulePath)}");
        return 0;
    }

    private static void VerifyMatureNativePins(HandleAddress address, Collect collect)
    {
        nint library = NativeLibrary.Load(OperatingSystem.IsWindows() ? "ucrtbase.dll" : "libc.so.6");
        NativeCopy copy = Marshal.GetDelegateForFunctionPointer<NativeCopy>(NativeLibrary.GetExport(library, "memcpy"));
        nint scratch = Marshal.AllocHGlobal(4096);
        GCHandle[] pins = new GCHandle[16];
        GCHandle[] aliases = new GCHandle[16];
        nint[] pointers = new nint[16];
        nint[] objects = new nint[16];
        using ManualResetEventSlim ready = new();
        int stop = 0;
        long copies = 0;
        Thread? worker = null;
        try
        {
            for (int index = 0; index < pins.Length; index++)
            {
                byte[] value = new byte[4096];
                value[2000] = (byte)(index + 1);
                aliases[index] = GCHandle.Alloc(value);
                pins[index] = GCHandle.Alloc(value, GCHandleType.Pinned);
                pointers[index] = pins[index].AddrOfPinnedObject();
                objects[index] = address(GCHandle.ToIntPtr(aliases[index]));
            }
            worker = new Thread(() =>
            {
                while (Volatile.Read(ref stop) == 0)
                {
                    for (int index = 0; index < pointers.Length; index++)
                    {
                        copy(scratch, pointers[index], 4096);
                        if (Marshal.ReadByte(scratch, 2000) != index + 1)
                        {
                            throw new InvalidOperationException("A live native buffer pointer changed during mature collection.");
                        }
                        Interlocked.Increment(ref copies);
                    }
                    ready.Set();
                }
            });
            worker.Start();
            Check(ready.Wait(TimeSpan.FromSeconds(30)), "The native pinned-buffer consumer did not run.");
            long before = Interlocked.Read(ref copies);
            for (int round = 0; round < 4; round++)
            {
                Check(collect(0) == 0, "Mature collection failed while native buffer addresses were exposed.");
                for (int index = 0; index < pins.Length; index++)
                {
                    Check(address(GCHandle.ToIntPtr(aliases[index])) == objects[index] &&
                        pins[index].AddrOfPinnedObject() == pointers[index] &&
                        Marshal.ReadByte(pointers[index], 2000) == index + 1,
                        "A real native consumer's pinned buffer or ordinary alias moved.");
                }
            }
            Volatile.Write(ref stop, 1);
            Check(worker.Join(30000) && Interlocked.Read(ref copies) > before,
                "Native memcpy consumption did not remain active across the collection lifetime.");
            Console.WriteLine($"native_pin_copies={Interlocked.Read(ref copies)} buffers=16 full_calls=4");
        }
        finally
        {
            Volatile.Write(ref stop, 1);
            if (worker is not null && worker.IsAlive && !worker.Join(30000))
            {
                throw new InvalidOperationException("The owned native-buffer worker did not stop.");
            }
            foreach (GCHandle handle in pins)
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            foreach (GCHandle handle in aliases)
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
            Marshal.FreeHGlobal(scratch);
            NativeLibrary.Free(library);
        }
    }

    private sealed class MatureStudyNode
    {
        internal readonly byte[] Payload = new byte[4096];
        internal readonly int Value;
        internal MatureStudyNode? Next;

        internal MatureStudyNode(int value)
        {
            Value = value;
            Payload[2000] = (byte)value;
            Payload[4095] = (byte)(value >> 8);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MatureStudyNode[] PrepareMatureStudy(string scenario, Collect? collect)
    {
        GCHandle[] handles = new GCHandle[512];
        for (int index = 0; index < handles.Length; index++)
        {
            handles[index] = GCHandle.Alloc(new MatureStudyNode(index));
        }
        if (collect is not null)
        {
            Check(collect(1) == 0, "The study's initial RC aging failed.");
        }
        else
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
        int stride = scenario == "dense" ? 1 : 8;
        MatureStudyNode[] live = new MatureStudyNode[handles.Length / stride];
        for (int index = 0; index < handles.Length; index++)
        {
            if (index % stride == 0)
            {
                live[index / stride] = (MatureStudyNode)handles[index].Target!;
            }
            handles[index].Free();
        }
        for (int index = 0; index < live.Length; index++)
        {
            live[index].Next = live[(index + 1) % live.Length];
        }
        return live;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MatureStudyChecksum(MatureStudyNode[] nodes)
    {
        long checksum = 0;
        for (int index = 0; index < nodes.Length; index++)
        {
            MatureStudyNode node = nodes[index];
            Check(ReferenceEquals(node.Next, nodes[(index + 1) % nodes.Length]) &&
                node.Payload[2000] == (byte)node.Value && node.Payload[4095] == (byte)(node.Value >> 8),
                "The aged cyclic graph changed aliases, identity or retained payload.");
            checksum += node.Value;
        }
        return checksum;
    }

    private static int RunMatureStudy(string[] args)
    {
        if (args.Length != 6 || args[1] is not ("fragmented" or "dense" or "pinned" or "high-survival") ||
            args[2] is not ("LXR" or "WKS" or "Server") || args[4] is not ("normal" or "injected") ||
            args[5] is not ("off" or "diagnostic"))
        {
            throw new ArgumentException("Use --mature-study scenario LXR|WKS|Server module normal|injected off|diagnostic.", nameof(args));
        }
        bool lxr = args[2] == "LXR";
        Check(GCSettings.IsServerGC == (args[2] == "Server"), "The mature study loaded a different collector category.");
        GetMetric? metric = null;
        HandleAddress? address = null;
        Collect? collect = null;
        bool matureSurface = false;
        bool costs = false;
        string identityText = args[2];
        if (lxr)
        {
            nint module = NativeLibrary.Load(args[3]);
            GetIdentity identity = Marshal.GetDelegateForFunctionPointer<GetIdentity>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetIdentity"));
            identityText = Marshal.PtrToStringUTF8(identity())!;
            matureSurface = identityText.Contains("P6 nursery+mature", StringComparison.Ordinal);
            costs = identityText.Contains("costs-v1", StringComparison.Ordinal);
            metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
            address = Marshal.GetDelegateForFunctionPointer<HandleAddress>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetHandleAddress"));
            collect = Marshal.GetDelegateForFunctionPointer<Collect>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_Collect"));
            Check(identityText.Contains("P6 nursery", StringComparison.Ordinal) && metric(0) == 1 && metric(1) > 0,
                "The study did not load the expected initialized, owning P6 collector.");
            Check(metric(78) == (Environment.GetEnvironmentVariable("DOTNET_LXREvacuateNursery") == "1" ? 1ul : 0ul) &&
                metric(87) == 1 && metric(88) == 1,
                "The actual nursery mode or default recycling differs from the declared study settings.");
            if (matureSurface)
            {
                Check(metric(90) == (Environment.GetEnvironmentVariable("DOTNET_LXREvacuateMature") == "1" ? 1ul : 0ul),
                    "The actual mature mode differs from the declared study arm.");
            }
            if (args[5] == "diagnostic")
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                EnableStorage enable = Marshal.GetDelegateForFunctionPointer<EnableStorage>(
                    NativeLibrary.GetExport(module, "GC_LxrPrototype_EnableStorageDiagnostics"));
                Check(enable() == 0 && (!costs || metric(104) == 1),
                    "Cost diagnostics did not activate at a drained, inactive boundary.");
            }
        }
        else
        {
            Check(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCPath")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPlus_GCPath")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCName")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPlus_GCName")),
                "A built-in study arm inherited an external collector.");
        }
        MatureStudyNode[] live = PrepareMatureStudy(args[1], collect);
        GCHandle[] pins = new GCHandle[args[1] == "pinned" ? 8 : 0];
        for (int index = 0; index < pins.Length; index++)
        {
            pins[index] = GCHandle.Alloc(live[index * 8].Payload, GCHandleType.Pinned);
        }
        if (collect is not null)
        {
            Check(collect(1) == 0, "The aged study graph's dead neighbors did not complete RC reclamation.");
        }
        else
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
        HashSet<nint> oldBlocks = new();
        if (address is not null)
        {
            foreach (MatureStudyNode node in live)
            {
                GCHandle handle = GCHandle.Alloc(node.Payload);
                oldBlocks.Add(address(GCHandle.ToIntPtr(handle)) & ~32767);
                handle.Free();
            }
        }
        ulong? Read(uint id) => metric is null ? null : metric(id);
        Dictionary<uint, ulong?> before = new();
        for (uint id = 5; id <= 116; id++)
        {
            if (id is 5 or 7 or 10 or 11 or 12 or 23 or 24 or 79 or 80 or 81 or 82 or 84 ||
                (matureSurface && id >= 91 && id <= 103) || (costs && args[5] == "diagnostic" && id >= 105 && id <= 113) ||
                (costs && args[5] == "diagnostic" && id == 115))
            {
                before[id] = Read(id);
            }
        }
        ulong? Delta(uint id) => before.TryGetValue(id, out ulong? value) && value.HasValue
            ? Read(id) - value : null;
        double[] calls = new double[8];
        double[] rounds = new double[8];
        MatureStudyNode[][] burst = new MatureStudyNode[4][];
        using Process process = Process.GetCurrentProcess();
        TimeSpan cpuBefore = process.TotalProcessorTime;
        TimeSpan pauseBefore = GC.GetTotalPauseDuration();
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        int collectionsBefore = GC.CollectionCount(2);
        long checksum = 0;
        long start = Stopwatch.GetTimestamp();
        for (int round = 0; round < rounds.Length; round++)
        {
            long roundStart = Stopwatch.GetTimestamp();
            if (args[1] == "high-survival")
            {
                MatureStudyNode[] values = new MatureStudyNode[128];
                for (int index = 0; index < values.Length; index++)
                {
                    values[index] = new MatureStudyNode(index);
                }
                burst[round & 3] = values;
            }
            long callStart = Stopwatch.GetTimestamp();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            calls[round] = Stopwatch.GetElapsedTime(callStart).TotalMilliseconds;
            checksum += MatureStudyChecksum(live);
            if (args[4] == "injected")
            {
                Thread.Sleep(25);
            }
            rounds[round] = Stopwatch.GetElapsedTime(roundStart).TotalMilliseconds;
        }
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        double cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
        double paused = (GC.GetTotalPauseDuration() - pauseBefore).TotalMilliseconds;
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        int collections = GC.CollectionCount(2) - collectionsBefore;
        Check(checksum == (args[1] == "dense" ? 1046528 : 129024), "The fixed aged-graph checksum changed.");
        Dictionary<string, object?> record = new()
        {
            ["scenario"] = args[1], ["collector"] = args[2], ["control"] = args[4], ["instrumentation"] = args[5],
            ["identity"] = identityText, ["server"] = GCSettings.IsServerGC, ["module_sha256"] = Hash(args[3]),
            ["nursery_enabled"] = Read(78), ["mature_enabled"] = matureSurface ? Read(90) : null,
            ["retained_nodes"] = live.Length, ["retained_payload_bytes"] = live.Length * 4096, ["checksum"] = checksum,
            ["primary_full_calls"] = 8, ["elapsed_ms"] = elapsed, ["cpu_ms"] = cpu, ["pause_ms"] = paused,
            ["allocated_bytes"] = allocated, ["collections"] = collections,
            ["initial_marks"] = Delta(10), ["final_marks"] = Delta(11), ["full_marks"] = Delta(12),
            ["increment_entries"] = Delta(23), ["decrement_entries"] = Delta(24), ["all_released_blocks"] = Delta(7),
            ["nursery_copied_objects"] = Delta(79), ["nursery_copied_bytes"] = Delta(80),
            ["nursery_pinned_bytes"] = Delta(81), ["nursery_copy_failures"] = Delta(82), ["nursery_retired_bytes"] = Delta(84),
            ["eligible_blocks"] = Delta(91), ["selected_blocks"] = Delta(92), ["pinned_blocks"] = Delta(93),
            ["estimated_mature_bytes"] = Delta(94), ["mature_copied_objects"] = Delta(95), ["mature_copied_bytes"] = Delta(96),
            ["mature_copy_failures"] = Delta(97), ["remembered_entries"] = Delta(98), ["stale_tag_rejects"] = Delta(99),
            ["replayed_entries"] = Delta(100), ["mature_retired_bytes"] = Delta(101),
            ["mature_released_blocks"] = Delta(102), ["invalid_slot_rejects"] = Delta(103),
            ["nursery_allocate_ticks"] = Delta(105), ["nursery_initialize_ticks"] = Delta(106),
            ["nursery_payload_ticks"] = Delta(107), ["mature_allocate_ticks"] = Delta(108),
            ["mature_initialize_ticks"] = Delta(109), ["mature_payload_ticks"] = Delta(110),
            ["remset_closure_ticks"] = Delta(111), ["trace_closure_ticks"] = Delta(112), ["trace_scan_ticks"] = Delta(113),
            ["clock_frequency"] = costs ? Read(114) : null, ["root_capture_ticks"] = Delta(115),
            ["round_ms"] = rounds, ["full_call_ms"] = calls,
            ["managed_reserve"] = Read(35), ["managed_admit"] = Read(36),
            ["managed_commit"] = lxr ? Read(26) : GC.GetGCMemoryInfo().TotalCommittedBytes,
            ["managed_live"] = lxr ? Read(3) : GC.GetTotalMemory(false), ["native_planes"] = Read(38),
            ["fragmentation_bytes"] = GC.GetGCMemoryInfo().FragmentedBytes,
        };
        byte[][] recovery = new byte[256][];
        int reused = 0;
        long recoveryStart = Stopwatch.GetTimestamp();
        for (int index = 0; index < recovery.Length; index++)
        {
            recovery[index] = new byte[4096];
            recovery[index][2000] = (byte)index;
            if (address is not null)
            {
                GCHandle handle = GCHandle.Alloc(recovery[index]);
                reused += oldBlocks.Contains(address(GCHandle.ToIntPtr(handle)) & ~32767) ? 1 : 0;
                handle.Free();
            }
        }
        record["recovery_elapsed_ms"] = Stopwatch.GetElapsedTime(recoveryStart).TotalMilliseconds;
        record["recovery_allocations"] = recovery.Length;
        record["old_payload_blocks_reused_by_actual_allocations"] = lxr ? reused : null;
        process.Refresh();
        record["rss_bytes"] = process.WorkingSet64;
        record["private_bytes"] = process.PrivateMemorySize64;
        Console.WriteLine("mature_study=" + JsonSerializer.Serialize(record));
        foreach (GCHandle pin in pins)
        {
            pin.Free();
        }
        GC.KeepAlive(live);
        GC.KeepAlive(burst);
        GC.KeepAlive(recovery);
        return 0;
    }

    private static int RunDefaultNursery(string modulePath)
    {
        nint module = NativeLibrary.Load(modulePath);
        GetMetric metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
        HandleAddress address = Marshal.GetDelegateForFunctionPointer<HandleAddress>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_GetHandleAddress"));
        Collect collect = Marshal.GetDelegateForFunctionPointer<Collect>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_Collect"));
        Check(metric(0) == 1 && metric(1) > 0 && metric(78) == 1 && metric(87) == 1 && metric(88) == 1,
            "Default copying must retain normal mutator and copy line recycling.");
        GCHandle[] handles = new GCHandle[32];
        nint[] before = new nint[32];
        try
        {
            for (int index = 0; index < handles.Length; index++)
            {
                byte[] value = new byte[24000];
                value[19999] = (byte)index;
                handles[index] = GCHandle.Alloc(value);
                before[index] = address(GCHandle.ToIntPtr(handles[index]));
            }
            ulong copied = metric(80);
            Check(collect(1) == 0, "Default-policy nursery promotion failed.");
            int moved = 0;
            for (int index = 0; index < handles.Length; index++)
            {
                moved += address(GCHandle.ToIntPtr(handles[index])) != before[index] ? 1 : 0;
                Check(handles[index].Target is byte[] { Length: 24000 } value && value[19999] == (byte)index,
                    "Default-policy movement corrupted a naturally allocated cohort.");
            }
            Check(moved > 0 && metric(80) > copied,
                "Moving mode only works with a diagnostic allocation switch, not default fresh-source exhaustion.");
            Console.WriteLine($"PASS: {s_checks} default-policy nursery checks moved={moved} " +
                $"copied_bytes={metric(80) - copied} mutator_recycling={metric(87)} copy_recycling={metric(88)} " +
                $"module_sha256={Hash(modulePath)}");
        }
        finally
        {
            foreach (GCHandle handle in handles)
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }
        return 0;
    }

    private static int RunNurseryStudy(string[] args)
    {
        if (args.Length != 5 || args[1] is not ("low-ref" or "high-ref" or "low-byte" or "high-byte" or "pinned") ||
            args[2] is not ("LXR" or "WKS" or "Server") || args[4] is not ("normal" or "injected"))
        {
            throw new ArgumentException("Use --nursery-study scenario LXR|WKS|Server module normal|injected.", nameof(args));
        }
        string scenario = args[1];
        bool lxr = args[2] == "LXR";
        Check(GCSettings.IsServerGC == (args[2] == "Server"), "The requested collector category was not loaded.");
        GetMetric? metric = null;
        bool nurseryModule = false;
        if (lxr)
        {
            nint module = NativeLibrary.Load(args[3]);
            metric = Marshal.GetDelegateForFunctionPointer<GetMetric>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetMetric"));
            GetIdentity identity = Marshal.GetDelegateForFunctionPointer<GetIdentity>(
                NativeLibrary.GetExport(module, "GC_LxrPrototype_GetIdentity"));
            nurseryModule = Marshal.PtrToStringUTF8(identity())!.Contains("P6 nursery", StringComparison.Ordinal);
            Check(metric(0) == 1 && metric(1) > 0, "The study did not select an owning LXR module.");
            if (nurseryModule)
            {
                Check(metric(78) == (Environment.GetEnvironmentVariable("DOTNET_LXREvacuateNursery") == "1" ? 1ul : 0ul),
                    "The actual nursery mode differs from the explicitly requested study arm.");
            }
        }
        else
        {
            Check(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_GCPath")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPlus_GCPath")),
                "A built-in arm unexpectedly selected an external collector.");
        }
        bool bytes = scenario is "low-byte" or "high-byte" or "pinned";
        int width = bytes ? 256 : 2048;
        object?[] roots = new object[4 * width];
        GCHandle[] pins = new GCHandle[4 * width / 128];
        double[] rounds = new double[32];
        double[] calls = new double[8];
        NurseryStudyBody(scenario, roots, pins, 4, false, rounds, calls);
        ForceFull();
        ulong copiedBefore = nurseryModule ? metric!(80) : 0;
        ulong pinnedBefore = nurseryModule ? metric!(81) : 0;
        ulong failuresBefore = nurseryModule ? metric!(82) : 0;
        ulong waitsBefore = nurseryModule ? metric!(83) : 0;
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        long pauseBefore = GC.GetTotalPauseDuration().Ticks;
        int collectionsBefore = GC.CollectionCount(2);
        ulong fullBefore = lxr ? metric!(11) + metric(12) : 0;
        using Process process = Process.GetCurrentProcess();
        TimeSpan cpuBefore = process.TotalProcessorTime;
        long start = Stopwatch.GetTimestamp();
        long checksum = NurseryStudyBody(scenario, roots, pins, 32, args[4] == "injected", rounds, calls);
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        double cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
        double paused = TimeSpan.FromTicks(GC.GetTotalPauseDuration().Ticks - pauseBefore).TotalMilliseconds;
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        process.Refresh();
        Check(checksum == 32L * (width - 1),
            "The fixed eight-call survival-study checksum changed.");
        ulong copied = nurseryModule ? metric!(80) - copiedBefore : 0;
        string roundTimes = string.Join(",", Array.ConvertAll(rounds, static value => value.ToString("F3", CultureInfo.InvariantCulture)));
        string callTimes = string.Join(",", Array.ConvertAll(calls, static value => value.ToString("F3", CultureInfo.InvariantCulture)));
        Console.WriteLine(
            $"nursery_study={scenario} collector={args[2]} control={args[4]} server={GCSettings.IsServerGC} " +
            $"elapsed_ms={elapsed.ToString("F3", CultureInfo.InvariantCulture)} " +
            $"cpu_ms={cpu.ToString("F3", CultureInfo.InvariantCulture)} pause_ms={paused.ToString("F3", CultureInfo.InvariantCulture)} " +
            $"operations={32 * width} checksum={checksum} primary_full_calls=8 " +
            $"collections={GC.CollectionCount(2) - collectionsBefore} full_completions={(lxr ? metric!(11) + metric(12) - fullBefore : 0)} " +
            $"allocated_bytes={allocated} copied_bytes={copied} " +
            $"pinned_promotion_bytes={(nurseryModule ? metric!(81) - pinnedBefore : 0)} " +
            $"copy_failures={(nurseryModule ? metric!(82) - failuresBefore : 0)} forwarding_waits={(nurseryModule ? metric!(83) - waitsBefore : 0)} " +
            $"survival_ppb={(lxr ? metric!(39) : 0)} managed_reserve={(lxr ? metric!(35) : 0)} managed_admit={(lxr ? metric!(36) : 0)} " +
            $"managed_commit={(lxr ? metric!(26) : (ulong)GC.GetGCMemoryInfo().TotalCommittedBytes)} managed_live={(lxr ? metric!(3) : (ulong)GC.GetTotalMemory(false))} " +
            $"native_planes={(lxr ? metric!(38) : 0)} rss_bytes={process.WorkingSet64} private_bytes={process.PrivateMemorySize64} " +
            $"round_ms={roundTimes} full_call_ms={callTimes} module_sha256={Hash(args[3])}");
        foreach (GCHandle pin in pins)
        {
            if (pin.IsAllocated)
            {
                pin.Free();
            }
        }
        GC.KeepAlive(roots);
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long NurseryStudyBody(
        string scenario, object?[] roots, GCHandle[] pins, int rounds, bool injected,
        double[] roundTimes, double[] callTimes)
    {
        bool bytes = scenario is "low-byte" or "high-byte" or "pinned";
        bool high = scenario is "high-ref" or "high-byte" or "pinned";
        int width = roots.Length / 4;
        long checksum = 0;
        for (int round = 0; round < rounds; round++)
        {
            long start = Stopwatch.GetTimestamp();
            int baseIndex = (round & 3) * width;
            for (int index = 0; index < width; index++)
            {
                object value;
                if (bytes)
                {
                    byte[] array = new byte[4096];
                    array[0] = (byte)index;
                    array[4095] = (byte)(index >> 8);
                    value = array;
                }
                else
                {
                    Node node = new(index);
                    if ((index & 15) == 0)
                    {
                        node.Next = node;
                    }
                    value = node;
                }
                roots[baseIndex + index] = high || (index & 63) == 0 || index == width - 1 ? value : null;
                if (scenario == "pinned" && (index & 127) == 0)
                {
                    int pinIndex = (baseIndex + index) / 128;
                    if (pins[pinIndex].IsAllocated)
                    {
                        pins[pinIndex].Free();
                    }
                    pins[pinIndex] = GCHandle.Alloc(value, GCHandleType.Pinned);
                }
            }
            if ((round & 3) == 3)
            {
                long gcStart = Stopwatch.GetTimestamp();
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                callTimes[round / 4] = Stopwatch.GetElapsedTime(gcStart).TotalMilliseconds;
                for (int block = 0; block < 4; block++)
                {
                    object retained = roots[block * width + width - 1]!;
                    checksum += bytes ? ((byte[])retained)[0] : ((Node)retained).Value;
                }
                if (injected)
                {
                    Thread.Sleep(25);
                }
            }
            roundTimes[round] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        return checksum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GCHandle RegisterFrozenTarget(RegisterFrozen register, nint memory, out nint segment)
    {
        byte[] target = new byte[24000];
        target[17003] = 179;
        object[] template = [target];
        GCHandle targetHandle = GCHandle.Alloc(target);
        GCHandle templateHandle = GCHandle.Alloc(template);
        try
        {
            segment = register(memory, 4096, GCHandle.ToIntPtr(templateHandle), GCHandle.ToIntPtr(targetHandle));
            Check(segment != 0, "The real selected heap did not register its frozen reference array fixture.");
            return GCHandle.Alloc(target, GCHandleType.Weak);
        }
        finally
        {
            templateHandle.Free();
            targetHandle.Free();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckFrozenTarget(GCHandle observer)
    {
        Check(observer.Target is byte[] { Length: 24000 } target && target[17003] == 179,
            "A frozen-only target lost identity, liveness or payload.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GCHandle MakeEarlyFrozenAlias(GCHandle observer) => GCHandle.Alloc(observer.Target);

    private static void VerifyReadOnlyFrozen(
        bool moving, nint module, GetMetric metric, HandleAddress address, Collect collect)
    {
        RegisterFrozen register = Marshal.GetDelegateForFunctionPointer<RegisterFrozen>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_RegisterFrozenArray"));
        UnregisterFrozen unregister = Marshal.GetDelegateForFunctionPointer<UnregisterFrozen>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_UnregisterFrozen"));
        ArmLifetime arm = Marshal.GetDelegateForFunctionPointer<ArmLifetime>(
            NativeLibrary.GetExport(module, "GC_LxrPrototype_ArmLifetimeProbe"));
        bool windows = OperatingSystem.IsWindows();
        nint memory = windows ? VirtualAlloc(0, 4096, 0x3000, 4) : Mmap(0, 4096, 3, 0x22, -1, 0);
        if (memory == 0 || memory == -1)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
        nint segment = 0;
        GCHandle observer = default;
        GCHandle ordinaryHandle = default;
        GCHandle earlyAlias = default;
        try
        {
            observer = RegisterFrozenTarget(register, memory, out segment);
            bool protectedReadOnly = windows
                ? VirtualProtect(memory, 4096, 2, out _)
                : Mprotect(memory, 4096, 1) == 0;
            if (!protectedReadOnly)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            Check(arm(GCHandle.ToIntPtr(observer)) == 0,
                "The non-rooting frozen-target lifetime probe was not armed.");
            nint original = address(GCHandle.ToIntPtr(observer));
            earlyAlias = MakeEarlyFrozenAlias(observer);
            byte[] ordinary = new byte[24000];
            ordinary[19007] = 193;
            ordinaryHandle = GCHandle.Alloc(ordinary);
            nint ordinaryBefore = address(GCHandle.ToIntPtr(ordinaryHandle));
            Check(collect(1) == 0, "RC failed with an ordinary alias discovered before its late read-only frozen pin.");
            Check(address(GCHandle.ToIntPtr(observer)) == original && Marshal.ReadIntPtr(memory, 24) == original,
                "The collector moved a target whose read-only frozen field cannot be rewritten.");
            Check((address(GCHandle.ToIntPtr(ordinaryHandle)) != ordinaryBefore) == moving &&
                ordinary[19007] == 193,
                "Frozen pin discovery also blanket-pinned a separately rooted ordinary object.");
            earlyAlias.Free();
            earlyAlias = default;
            Check(collect(0) == 0, "Full completion failed with an actually read-only frozen-only outgoing root.");
            CheckFrozenTarget(observer);
            Check(metric(47) == 0, "The registered frozen-only target was prematurely reclaimed.");
            unregister(segment);
            segment = 0;
            Check(collect(0) == 0 && collect(0) == 0 && metric(47) >= 1 && observer.Target is null,
                "Unregistration did not release the frozen reader/root lifetime after truthful full completion.");
            GC.KeepAlive(ordinary);
        }
        finally
        {
            if (segment != 0)
            {
                unregister(segment);
            }
            if (ordinaryHandle.IsAllocated)
            {
                ordinaryHandle.Free();
            }
            if (earlyAlias.IsAllocated)
            {
                earlyAlias.Free();
            }
            if (observer.IsAllocated)
            {
                observer.Free();
            }
            bool released = windows ? VirtualFree(memory, 0, 0x8000) : Munmap(memory, 4096) == 0;
            if (!released)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }
    }

    private static void VerifyCopiedFeedback(GetMetric metric, double previous)
    {
        ulong allocated = metric(86);
        double current = Math.Min(allocated == 0 ? 0.0 : (double)metric(85) / allocated, 1.0);
        bool weighted = Environment.GetEnvironmentVariable("DOTNET_LXRSurvivalPredictorWeighted") == "1";
        bool harmonic = Environment.GetEnvironmentVariable("DOTNET_LXRSurvivalPredictorHarmonicMean") == "1";
        double expected = allocated == 0 ? previous : weighted
            ? current > previous ? (3.0 * current + previous) / 4.0 : (current + 3.0 * previous) / 4.0
            : harmonic
                ? current >= previous
                    ? 2.0 * current * previous / (current + previous)
                    : (current * current + previous * previous) / (current + previous)
                : (3.0 * current + previous) / 4.0;
        expected = double.IsNaN(expected) ? 1.0 : Math.Min(expected, 1.0);
        Check(Math.Abs(metric(39) / 1e9 - expected) < 2e-9,
            "Actual copied promotions did not feed the oracle predictor at pause release.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VerifyMovingGraph(bool moving, GetMetric metric, HandleAddress address, Collect collect)
    {
        Node first = new(731);
        Node second = new(913);
        first.Next = second;
        second.Next = first;
        object[] aliases = [first, first, second];
        GCHandle firstHandle = GCHandle.Alloc(first);
        GCHandle secondHandle = GCHandle.Alloc(second);
        GCHandle aliasesHandle = GCHandle.Alloc(aliases);
        GCHandle weak = GCHandle.Alloc(first, GCHandleType.Weak);
        try
        {
            int hash = RuntimeHelpers.GetHashCode(first);
            nint before = address(GCHandle.ToIntPtr(firstHandle));
            nint secondBefore = address(GCHandle.ToIntPtr(secondHandle));
            double survival = metric(39) / 1e9;
            ulong copied = metric(80);
            lock (first)
            {
                lock (second)
                {
                    Check(collect(1) == 0, "Moving RC promotion failed.");
                    Check((address(GCHandle.ToIntPtr(firstHandle)) != before) == moving &&
                        (address(GCHandle.ToIntPtr(secondHandle)) != secondBefore) == moving,
                        "Ordinary hash/monitor-backed graph roots did not genuinely obey the nursery toggle.");
                    Check(ReferenceEquals(first, firstHandle.Target) && ReferenceEquals(first, weak.Target) &&
                        ReferenceEquals(first, aliases[0]) && ReferenceEquals(aliases[0], aliases[1]) &&
                        ReferenceEquals(second, aliases[2]) && ReferenceEquals(first.Next, second) &&
                        ReferenceEquals(second.Next, first),
                        "Stack, strong/weak handles, duplicate fields and the copied cycle did not converge.");
                    Check(Monitor.IsEntered(first) && Monitor.IsEntered(second) &&
                        RuntimeHelpers.GetHashCode(first) == hash && first.Value + second.Value == 1644,
                        "Moving changed object identity, hash, thin/inflated monitor state or graph payload.");
                }
            }
            Check((metric(80) > copied) == moving && metric(80) == metric(84) &&
                metric(85) == metric(80) - copied,
                "Copied objects and safely retired physical source bytes disagree.");
            VerifyCopiedFeedback(metric, survival);
            nint mature = address(GCHandle.ToIntPtr(firstHandle));
            nint matureSecond = address(GCHandle.ToIntPtr(secondHandle));
            nint matureAliases = address(GCHandle.ToIntPtr(aliasesHandle));
            survival = metric(39) / 1e9;
            copied = metric(80);
            int collected = collect(1);
            nint after = address(GCHandle.ToIntPtr(firstHandle));
            ulong copiedAfter = metric(80);
            Check(collected == 0 && after == mature &&
                address(GCHandle.ToIntPtr(secondHandle)) == matureSecond &&
                address(GCHandle.ToIntPtr(aliasesHandle)) == matureAliases,
                "The next RC epoch moved the already promoted graph or its alias array.");
            Check(metric(85) == copiedAfter - copied && metric(80) == metric(84) &&
                (metric(86) != 0 || metric(85) == 0),
                "Other runtime births must be actual copies with retired sources, not fabricated survival.");
            VerifyCopiedFeedback(metric, survival);
            GC.KeepAlive(aliases);
            GC.KeepAlive(first);
            GC.KeepAlive(second);
        }
        finally
        {
            weak.Free();
            aliasesHandle.Free();
            secondHandle.Free();
            firstHandle.Free();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void VerifyNurseryPinsAndInteriors(
        bool moving, GetMetric metric, HandleAddress address, Collect collect)
    {
        byte[] pinnedBuffer = new byte[1000];
        byte[] fixedBuffer = new byte[4000];
        byte[] poh = GC.AllocateArray<byte>(4000, pinned: true);
        byte[] interiorBuffer = new byte[24000];
        pinnedBuffer[777] = 71;
        fixedBuffer[3001] = 83;
        poh[3002] = 97;
        interiorBuffer[10000] = 157;
        GCHandle pin = GCHandle.Alloc(pinnedBuffer, GCHandleType.Pinned);
        GCHandle fixedHandle = GCHandle.Alloc(fixedBuffer);
        GCHandle pohHandle = GCHandle.Alloc(poh);
        GCHandle interiorHandle = GCHandle.Alloc(interiorBuffer);
        try
        {
            nint pinnedBefore = pin.AddrOfPinnedObject();
            nint fixedBefore = address(GCHandle.ToIntPtr(fixedHandle));
            nint pohBefore = address(GCHandle.ToIntPtr(pohHandle));
            nint interiorBefore = address(GCHandle.ToIntPtr(interiorHandle));
            ref byte interior = ref interiorBuffer[10000];
            double survival = metric(39) / 1e9;
            fixed (byte* pointer = fixedBuffer)
            {
                Check(collect(1) == 0, "Pinned/interior nursery RC closure failed.");
                Check(pin.AddrOfPinnedObject() == pinnedBefore &&
                    address(GCHandle.ToIntPtr(fixedHandle)) == fixedBefore &&
                    address(GCHandle.ToIntPtr(pohHandle)) == pohBefore,
                    "GCHandle.Pinned, fixed or POH exposed addresses moved.");
                Check(pointer[3001] == 83 && pinnedBuffer[777] == 71 && poh[3002] == 97 &&
                    interior == 157 && interiorBuffer[10000] == 157,
                    "Pinned payload or a relocatable interior beyond its first page was corrupted.");
                Check((address(GCHandle.ToIntPtr(interiorHandle)) != interiorBefore) == moving,
                    "An ordinary interior root was blanket-pinned instead of legally relocated.");
            }
            VerifyCopiedFeedback(metric, survival);
            Check(!moving || metric(81) >= 9000,
                "Pinned in-place promotion was not separately accounted from copied survival.");
            GC.KeepAlive(interiorBuffer);
        }
        finally
        {
            interiorHandle.Free();
            pohHandle.Free();
            fixedHandle.Free();
            pin.Free();
        }
    }
}
