// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133517
{
    [Fact]
    public static void TestEntryPoint()
    {
        foreach (int value in new[] { int.MinValue, int.MinValue + 1, -1, 0, 1, int.MaxValue })
        {
            Assert.Equal(value == int.MinValue ? -1 : 0, NegateDivision(value));
        }

        foreach (long value in new[] { long.MinValue, long.MinValue + 1, -1, 0, 1, long.MaxValue })
        {
            Assert.Equal(value == long.MinValue ? -1L : 0L, NegateDivision(value));
        }

        foreach (nint value in new[] { nint.MinValue, nint.MinValue + 1, -1, 0, 1, nint.MaxValue })
        {
            Assert.Equal(value == nint.MinValue ? (nint)(-1) : 0, NegateDivision(value));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NegateDivision(int value) => -(value / int.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static long NegateDivision(long value) => -(value / long.MinValue);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint NegateDivision(nint value) => -(value / nint.MinValue);
}
