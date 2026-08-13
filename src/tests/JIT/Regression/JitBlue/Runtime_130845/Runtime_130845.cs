// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Exercises the AND(X, NEG(X)) -> ExtractLowestSetBit (BLSI) lowering where the
// result feeds a flags consumer (a compare-to-zero / branch). Validates that the
// transform preserves both the produced value and the branch behavior.

using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_130845
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static long IsolateLong(long x) => x & (-x);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int IsolateInt(int x) => x & (-x);

    // The AND result feeds a branch, so its flags are consumed downstream.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int BranchOnIsolateLong(long x)
    {
        if ((x & (-x)) == 0)
        {
            return 42;
        }
        return 7;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int BranchOnIsolateInt(int x)
    {
        if ((x & (-x)) != 0)
        {
            return 7;
        }
        return 42;
    }

    // The isolated value is also consumed by value, guarding against the AND
    // being incorrectly replaced when its result is used beyond the flags.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static long IsolateAndAddLong(long x)
    {
        long y = x & (-x);
        if (y == 0)
        {
            return -1;
        }
        return y + 1;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        long[] longInputs = { 0, 1, 2, 6, 0x100, unchecked((long)0x8000_0000_0000_0000), -1, 0x5A5A_5A5A_0000_0000 };
        foreach (long x in longInputs)
        {
            long expected = x & (-x);
            Assert.Equal(expected, IsolateLong(x));
            Assert.Equal((expected == 0) ? 42 : 7, BranchOnIsolateLong(x));
            Assert.Equal((expected == 0) ? -1 : expected + 1, IsolateAndAddLong(x));
        }

        int[] intInputs = { 0, 1, 2, 6, 0x100, unchecked((int)0x8000_0000), -1, 0x5A5A_0000 };
        foreach (int x in intInputs)
        {
            int expected = x & (-x);
            Assert.Equal(expected, IsolateInt(x));
            Assert.Equal((expected == 0) ? 42 : 7, BranchOnIsolateInt(x));
        }
    }
}
