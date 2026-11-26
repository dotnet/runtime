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
}
