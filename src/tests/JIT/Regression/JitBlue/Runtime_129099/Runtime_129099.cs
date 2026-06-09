// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NI_PRIMITIVE_RotateLeft/Right's const-fold path stored the unsigned
// fold result into a TYP_INT/TYP_UINT GenTreeIntCon via gtNewIconNode.
// For uint operands with the high bit set (e.g. RotateRight(0xFFFFFFFFu, k))
// the zero-extended ssize_t value (0xFFFFFFFF = 4294967295) does not fit
// in int32_t, tripping a downstream FitsIn<int32_t> assert during
// 'Morph - Global' when the constant was bashed/updated.
//
// The volatile Sink is required so the fold result is materialized as a
// store (rather than dropped or inlined into the return path) -- that
// store is what hits the wide-value assert.

namespace Runtime_129099;

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_129099
{
    private static volatile uint SinkU32;
    private static volatile int SinkI32;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint FoldRotateRightUInt()
    {
        uint v = BitOperations.RotateRight(0xFFFFFFFFu, 1);
        SinkU32 = v;
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint FoldRotateLeftUInt()
    {
        uint v = BitOperations.RotateLeft(0xFFFFFFFFu, 1);
        SinkU32 = v;
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint FoldRotateRightHighBit()
    {
        uint v = BitOperations.RotateRight(0x80000000u, 3);
        SinkU32 = v;
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int FoldIntRotateRightMinusOne()
    {
        int v = int.RotateRight(-1, 3);
        SinkI32 = v;
        return v;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (FoldRotateRightUInt()      != 0xFFFFFFFFu) return 101;
        if (FoldRotateLeftUInt()       != 0xFFFFFFFFu) return 102;
        if (FoldRotateRightHighBit()   != 0x10000000u) return 103;
        if (FoldIntRotateRightMinusOne() != -1)        return 104;
        return 100;
    }
}
