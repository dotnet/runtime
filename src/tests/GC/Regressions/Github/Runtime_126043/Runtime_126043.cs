// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1825 // Avoid zero-length array allocations (needed for exact region packing)

// Repro for Server GC hang: infinite loop in gc_heap::allocate_in_condemned_generations
//
// Requires:
//   - Server GC (DOTNET_gcServer=1, DOTNET_GCHeapCount=1)
//   - 64-bit process
//
// Mechanism:
//   1. Allocate byte[8200] objects (8,224 bytes each) interleaved with byte[0] (24 bytes)
//      to fill each 4MB region exactly: 510 * 8,224 + 1 * 24 = 4,194,264 bytes
//   2. Compacting gen2 GCs pack objects contiguously (eliminate quantum gaps)
//   3. Pin all objects; gen2 compact -> pinned_surv ~= 4.19MB < 6MB -> demote to gen0
//   4. Free pins -> one big non-pinned plug = 4,194,264 bytes (full region)
//   5. Gen1 compact: plug + 24B SHORT_PLUGS front padding = 4,194,288 > 4,194,264 -> HANG

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

public class Runtime_126043
{
    // byte[8200] = 8,224 bytes on heap (24B header + 8200B data).
    // 510 per 4MB region = 4,194,240 bytes + 24B gap = 4,194,264 (usable).
    // To trigger the bug, we need plug > 4,194,240 (usable - front_pad).
    // Interleave one byte[0] (24 bytes) per 510 byte[8200] to fill the gap:
    //   510 * 8,224 + 1 * 24 = 4,194,264 = full region. plug = 4,194,264.
    //   plug + 24B front_pad = 4,194,288 > 4,194,264 -> HANG!
    //
    // 11,242 objects = 22 groups of (510+1), ensures LOH arrays (>85KB).
    private const int ArrayDataLength = 8200;
    private const int ObjectsPerGroup = 511; // 510 large + 1 small per region
    private const int GroupCount = 22;
    private const int ObjectCount = ObjectsPerGroup * GroupCount; // 11,242

    [Fact]
    public static void TestEntryPoint()
    {
        Console.WriteLine($"Server GC: {GCSettings.IsServerGC}  64-bit: {Environment.Is64BitProcess}");
        if (!GCSettings.IsServerGC || !Environment.Is64BitProcess)
        {
            throw new Exception("ERROR: Requires server GC and 64-bit process.");
        }

        Console.WriteLine($"Allocating {ObjectCount} x byte[{ArrayDataLength}] objects (~{ObjectCount * 8224 / 1024 / 1024}MB)...");
        RunHangScenario();

        Console.WriteLine("Completed without hang.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunHangScenario()
    {
        // Measure actual object size on heap
        long before = GC.GetAllocatedBytesForCurrentThread();
        byte[] probe = new byte[ArrayDataLength];
        long after = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"  Actual heap size of byte[{ArrayDataLength}]: {after - before} bytes");
        GC.KeepAlive(probe);

        // Phase 1: Clean slate.
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        // Phase 2: Allocate objects. byte[8200] = 8,224 bytes on heap, interleaved with byte[0].
        byte[][] live = AllocateObjects(ObjectCount);
        Console.WriteLine($"  Allocated, gen={GC.GetGeneration(live[0])}");

        // Phase 3: Promote to gen2 and compact (removes quantum gaps).
        for (int i = 0; i < 3; i++)
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Console.WriteLine($"  After compaction: gen={GC.GetGeneration(live[0])}");

        // Phase 4: Pin ALL objects.
        GCHandle[] pins = new GCHandle[ObjectCount];
        for (int i = 0; i < ObjectCount; i++)
            pins[i] = GCHandle.Alloc(live[i], GCHandleType.Pinned);
        Console.WriteLine($"  Pinned {ObjectCount} objects");

        // Phase 5: Burn through GC cycles so demotion kicks in.
        for (int i = 0; i < 60; i++)
            GC.Collect(0, GCCollectionMode.Forced, blocking: true);

        // Phase 6: Gen2 compact with demotion.
        Console.WriteLine("  Phase 6: gen2 compact (demotion)...");
        Console.Out.Flush();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Console.WriteLine($"  After demotion: gen={GC.GetGeneration(live[0])}");

        // Phase 7: Free ALL pins.
        for (int i = 0; i < pins.Length; i++)
            pins[i].Free();
        Console.WriteLine("  All pins freed.");

        // Phase 8: Gen1 compact. Non-pinned plug 4,194,264 + 24B front_pad > 4,194,264 -> HANG.
        Console.WriteLine("  Phase 8: gen1 compact (may HANG)...");
        Console.Out.Flush();
        GC.Collect(1, GCCollectionMode.Forced, blocking: true, compacting: true);

        Console.WriteLine("  Survived gen1 GC!");
        GC.KeepAlive(live);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[][] AllocateObjects(int count)
    {
        byte[][] arr = new byte[count][];
        for (int i = 0; i < count; i++)
        {
            // Every 511th object is byte[0] (24 bytes) to fill the 24-byte gap
            // at the end of each 4MB region. Other objects are byte[8200] (8,224 bytes).
            arr[i] = ((i + 1) % ObjectsPerGroup == 0) ? new byte[0] : new byte[ArrayDataLength];
        }

        return arr;
    }
}
