// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for two related GC holes in stack-target struct block ops.
// When a struct that contains GC pointers lives on the stack and is larger than
// the SIMD unroll threshold, the JIT used to drop the write barriers (correct for
// a stack destination) but then fall back to a managed helper call:
//   * copies were lowered to CORINFO_HELP_MEMCPY (SpanHelpers.Memmove);
//   * zeroing was lowered to CORINFO_HELP_MEMZERO (SpanHelpers.ClearWithoutReferences).
// Both helpers are normal managed calls and therefore GC-safe points, so a GC
// triggered mid-operation could observe torn GC pointers in the partially-written,
// GC-reported stack destination and corrupt the heap. The fix keeps such copies on
// the GC-aware CpObj path and such zeroing on the atomic pointer-sized loop.
//
// The structs below are intentionally larger than every unroll threshold so the
// block ops are lowered to the helper calls. Needs GCStress to reliably reproduce;
// CI pipelines that exercise GCStress will catch a regression.

namespace Runtime_130286;

using System.Runtime.CompilerServices;
using Xunit;

public struct BigGcStruct
{
    // 40 object references => 320 bytes on 64-bit, larger than the largest
    // (AVX-512) Memcpy unroll threshold of 256 bytes.
    public object O00, O01, O02, O03, O04, O05, O06, O07, O08, O09;
    public object O10, O11, O12, O13, O14, O15, O16, O17, O18, O19;
    public object O20, O21, O22, O23, O24, O25, O26, O27, O28, O29;
    public object O30, O31, O32, O33, O34, O35, O36, O37, O38, O39;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public BigGcStruct(object o)
    {
        O00 = o; O01 = o; O02 = o; O03 = o; O04 = o; O05 = o; O06 = o; O07 = o; O08 = o; O09 = o;
        O10 = o; O11 = o; O12 = o; O13 = o; O14 = o; O15 = o; O16 = o; O17 = o; O18 = o; O19 = o;
        O20 = o; O21 = o; O22 = o; O23 = o; O24 = o; O25 = o; O26 = o; O27 = o; O28 = o; O29 = o;
        O30 = o; O31 = o; O32 = o; O33 = o; O34 = o; O35 = o; O36 = o; O37 = o; O38 = o; O39 = o;
    }
}

public struct HugeGcStruct
{
    // 80 object references => 640 bytes on 64-bit, larger than the largest
    // (AVX-512) Memset unroll threshold of 512 bytes.
    public object O00, O01, O02, O03, O04, O05, O06, O07, O08, O09;
    public object O10, O11, O12, O13, O14, O15, O16, O17, O18, O19;
    public object O20, O21, O22, O23, O24, O25, O26, O27, O28, O29;
    public object O30, O31, O32, O33, O34, O35, O36, O37, O38, O39;
    public object O40, O41, O42, O43, O44, O45, O46, O47, O48, O49;
    public object O50, O51, O52, O53, O54, O55, O56, O57, O58, O59;
    public object O60, O61, O62, O63, O64, O65, O66, O67, O68, O69;
    public object O70, O71, O72, O73, O74, O75, O76, O77, O78, O79;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public HugeGcStruct(object o)
    {
        O00 = o; O09 = o; O79 = o;
    }
}

public class Runtime_130286
{
    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 200_000; i++)
        {
            // Allocate garbage to increase GC pressure/frequency.
            object o = new byte[8];
            if (!CopyAndCheck(o) || !ZeroAndCheck(o))
            {
                return -1;
            }
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CopyAndCheck(object o)
    {
        // The ctor builds the struct into a stack temp ...
        BigGcStruct src = new BigGcStruct(o);
        // ... which is then copied stack-to-stack. This is the STORE_BLK that used
        // to be lowered to CORINFO_HELP_MEMCPY for this GC-pointer-containing struct.
        BigGcStruct dst = src;
        // Passing 'dst' by reference forces it to be address-exposed and reported to the GC.
        return Check(in dst);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Check(in BigGcStruct s)
    {
        // Touch every slot so a torn GC pointer would be observed.
        return s.O00 != null && s.O09 != null && s.O19 != null && s.O29 != null && s.O39 != null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ZeroAndCheck(object o)
    {
        HugeGcStruct s = new HugeGcStruct(o);
        bool populated = CheckHuge(in s);
        // Re-zeroing a live, GC-reported stack struct. This is the STORE_BLK that used
        // to be lowered to CORINFO_HELP_MEMZERO for this GC-pointer-containing struct.
        s = default;
        return populated && !CheckHuge(in s);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckHuge(in HugeGcStruct s)
    {
        return s.O00 != null || s.O09 != null || s.O79 != null;
    }
}
