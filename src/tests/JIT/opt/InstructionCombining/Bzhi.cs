// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Correctness coverage for the xarch `bzhi` (ZeroHighBits) lowering.
//
// The transform is opportunistic on AVX2, so this test intentionally avoids disasm checks (which
// would be flaky on hardware without AVX2) and instead validates that the optimized expression
// produces the same results as an independent oracle.
//
// C#'s `<<` masks the shift count modulo the operand width, so `x & ((1 << y) - 1)` is well-defined
// for *any* y (e.g. `1 << 32 == 1 << 0`). The lowering must reproduce that for out-of-range y too,
// so the index range below deliberately spans well past the operand width. The oracle routes the
// shift through a NoInlining `1 << y` helper: that emits a bare (masked) `shl` which is never itself
// recognized as `bzhi`, so it is an independent reference rather than a copy of the candidate.
public static class Bzhi
{
    [MethodImpl(MethodImplOptions.NoInlining)] static int ShlOne(int y) => 1 << y;
    [MethodImpl(MethodImplOptions.NoInlining)] static long ShlOneL(int y) => 1L << y;

    // Oracle for `x & ((1 << y) - 1)` using masked-shift (C#-defined) semantics.
    static int ZeroHighRef(int x, int y) => x & (ShlOne(y) - 1);
    static long ZeroHighRef(long x, int y) => x & (ShlOneL(y) - 1);

    // bzhi candidates (variable index).
    [MethodImpl(MethodImplOptions.NoInlining)] static int Bzhi_I(int x, int y) => x & ((1 << y) - 1);
    [MethodImpl(MethodImplOptions.NoInlining)] static long Bzhi_L(long x, int y) => x & ((1L << y) - 1);

    [Fact]
    public static void Test()
    {
        var rng = new Random(12345);

        // Boundary indices around and beyond both operand widths (32 and 64).
        int[] boundaries = { 0, 1, 15, 30, 31, 32, 33, 63, 64, 65, 95, 96, 127, 128, 200, 255 };

        for (int i = 0; i < 5000; i++)
        {
            uint xu = (uint)rng.Next() ^ ((uint)rng.Next() << 1);
            int xi = (int)xu;
            ulong xul = ((ulong)xu << 32) | (uint)rng.Next();
            long xl = (long)xul;

            foreach (int y in boundaries)
            {
                Assert.Equal(ZeroHighRef(xi, y), Bzhi_I(xi, y));
                Assert.Equal(ZeroHighRef(xl, y), Bzhi_L(xl, y));
            }

            // Random indices spanning past the operand width to exercise masked-shift semantics.
            int yi = rng.Next(0, 256);
            int yl = rng.Next(0, 256);
            Assert.Equal(ZeroHighRef(xi, yi), Bzhi_I(xi, yi));
            Assert.Equal(ZeroHighRef(xl, yl), Bzhi_L(xl, yl));
        }
    }
}
