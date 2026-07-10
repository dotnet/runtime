// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Correctness coverage for the xarch `bzhi` (ZeroHighBits) lowering.
//
// The transform is opportunistic on AVX2, so this test intentionally avoids disasm checks (which
// would be flaky on hardware without AVX2) and instead validates that the optimized expression
// produces the same results as an independent oracle. The oracle is parameterized on runtime
// values so it does not itself fold into `bzhi`.
public static class Bzhi
{
    // Oracle for `x & ((1 << y) - 1)` restricted to the well-defined in-range domain [0, width).
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ZeroHighRef(int x, int y) => x & (int)(((1u << y) - 1));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ZeroHighRef(long x, int y) => x & (long)(((1ul << y) - 1));

    // bzhi candidates (variable index).
    [MethodImpl(MethodImplOptions.NoInlining)] static int Bzhi_I(int x, int y) => x & ((1 << y) - 1);
    [MethodImpl(MethodImplOptions.NoInlining)] static long Bzhi_L(long x, int y) => x & ((1L << y) - 1);

    [Fact]
    public static void Test()
    {
        var rng = new Random(12345);

        for (int i = 0; i < 5000; i++)
        {
            uint xu = (uint)rng.Next() ^ ((uint)rng.Next() << 1);
            int xi = (int)xu;
            ulong xul = ((ulong)xu << 32) | (uint)rng.Next();
            long xl = (long)xul;

            // Restrict bzhi validation to the well-defined [0, width) index range.
            int yi = rng.Next(0, 32);
            int yl = rng.Next(0, 64);
            Assert.Equal(ZeroHighRef(xi, yi), Bzhi_I(xi, yi));
            Assert.Equal(ZeroHighRef(xl, yl), Bzhi_L(xl, yl));
        }
    }
}
