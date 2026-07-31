// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Each fixed region re-pins the span and derives 'p + Length' from the pointer that region
// established. Value numbering gave every "Cast away GC" temp holding those pointers the same
// number, so CSE computed the sum once and reused it in the later regions, by which point the
// GC had moved the array out from under it.

namespace Runtime_131226;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public static unsafe class Runtime_131226
{
    private static object s_garbage;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Relocate()
    {
        s_garbage = new byte[8192];
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.KeepAlive(s_garbage);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Spans(byte* start, byte* end, int length) => (end - start) == length;

    // Variant where an allocation (not a managed call) is the only operation between the two
    // fixed regions.  The GC safepoint epoch must advance at the allocation so CSE does not
    // merge the raw-address snapshot from the first region into the second.  No managed calls
    // are made inside the first fixed block to ensure the epoch has not already advanced before
    // the cast-away-GC store; the Relocate() that follows the allocation uses a managed call to
    // guarantee the object actually moves, so the compaction that exposes any stale-address CSE
    // is still reliable.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int PinAroundAllocation(ReadOnlySpan<byte> span)
    {
        int mismatches = 0;

        fixed (byte* p = &MemoryMarshal.GetReference(span))
        {
            // No managed calls inside: the cast-away-GC epoch is exactly the block-entry epoch.
            // 'p + span.Length' establishes a CSE candidate with this epoch.
            if (p + span.Length - p != span.Length) { mismatches++; }
        }

        // Allocation only between the two fixed regions.  This helper call is a GC safepoint
        // (IsNoGC is false for CORINFO_HELP_NEWSFAST) so fgCurGcEpochVN must advance here,
        // giving the second region's cast-away-GC temp a different epoch and a different VN.
        s_garbage = new byte[1];

        Relocate();

        fixed (byte* p = &MemoryMarshal.GetReference(span))
        {
            if (!Spans(p, p + span.Length, span.Length)) { mismatches++; }
        }

        return mismatches;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int PinSequentially(ReadOnlySpan<byte> span)
    {
        int mismatches = 0;

        fixed (byte* p = &MemoryMarshal.GetReference(span))
        {
            if (!Spans(p, p + span.Length, span.Length)) { mismatches++; }
        }

        Relocate();

        fixed (byte* p = &MemoryMarshal.GetReference(span))
        {
            if (!Spans(p, p + span.Length, span.Length)) { mismatches++; }
        }

        Relocate();

        fixed (byte* p = &MemoryMarshal.GetReference(span))
        {
            if (!Spans(p, p + span.Length, span.Length)) { mismatches++; }
        }

        return mismatches;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int PinInLoop(ReadOnlySpan<byte> span)
    {
        int mismatches = 0;

        for (int i = 0; i < 4; i++)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(span))
            {
                if (!Spans(p, p + span.Length, span.Length)) { mismatches++; }
            }

            Relocate();
        }

        return mismatches;
    }

    [Fact]
    public static void SequentialRegions()
    {
        int mismatches = 0;

        for (int i = 0; i < 8; i++)
        {
            mismatches += PinSequentially(new byte[38]);
        }

        Assert.Equal(0, mismatches);
    }

    [Fact]
    public static void RegionInLoop()
    {
        int mismatches = 0;

        for (int i = 0; i < 8; i++)
        {
            mismatches += PinInLoop(new byte[38]);
        }

        Assert.Equal(0, mismatches);
    }

    [Fact]
    public static void RegionAroundAllocation()
    {
        int mismatches = 0;

        for (int i = 0; i < 8; i++)
        {
            mismatches += PinAroundAllocation(new byte[38]);
        }

        Assert.Equal(0, mismatches);
    }
}
