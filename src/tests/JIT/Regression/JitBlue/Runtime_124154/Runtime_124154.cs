// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

// Codegen for NI_AVX512_NotMask selected `knotb` for any mask with 8 or fewer
// lanes. For a 2- or 4-lane mask `knotb` inverts all 8 bits of the mask
// register, leaving the bits above the lane count set, and nothing cleared
// them. A following MoveMask handed those stale bits to PopCount, which then
// over-counted by (8 - laneCount).

public class Runtime_124154
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Vector128<uint> is 4 lanes, so only bits 0-3 may ever be set.
        Vector128<uint> zero32 = Vector128<uint>.Zero;
        Assert.Equal(0xFu, NotAnd128UInt32(zero32, zero32, zero32, zero32));
        Assert.Equal(0x0u, NotAnd128UInt32(zero32, Vector128.Create(1u), zero32, Vector128.Create(1u)));
        Assert.Equal(0x5u, NotAnd128UInt32(zero32, Vector128.Create(0u, 1u, 0u, 1u), zero32, Vector128.Create(0u, 1u, 0u, 1u)));

        // Vector128<ulong> is 2 lanes, the worst case at 6 stale bits.
        Vector128<ulong> zero64 = Vector128<ulong>.Zero;
        Assert.Equal(0x3u, NotAnd128UInt64(zero64, zero64, zero64, zero64));
        Assert.Equal(0x0u, NotAnd128UInt64(zero64, Vector128.Create(1ul), zero64, Vector128.Create(1ul)));
        Assert.Equal(0x1u, NotAnd128UInt64(zero64, Vector128.Create(0ul, 1ul), zero64, Vector128.Create(0ul, 1ul)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint NotAnd128UInt32(Vector128<uint> a, Vector128<uint> b, Vector128<uint> c, Vector128<uint> d) =>
        (~(Vector128.LessThan(a, b) & Vector128.LessThan(c, d))).ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint NotAnd128UInt64(Vector128<ulong> a, Vector128<ulong> b, Vector128<ulong> c, Vector128<ulong> d) =>
        (~(Vector128.LessThan(a, b) & Vector128.LessThan(c, d))).ExtractMostSignificantBits();
}
