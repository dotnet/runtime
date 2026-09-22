// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134452
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(1, IntToFloatRounding(16777219));
        Assert.Equal(1, DoubleToUIntSaturation(-5));
        Assert.Throws<IndexOutOfRangeException>(() => IndexThroughFloat(new byte[16777220], 16777219));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int IntToFloatRounding(int x)
    {
        if (x >= 0 && x <= 16777219)
        {
            // (float)16777219 rounds to 16777220
            int y = (int)(float)x;
            if (y > 16777219)
                return 1;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int DoubleToUIntSaturation(int x)
    {
        if (x < 0)
        {
            // Saturates to 0
            uint u = (uint)(double)x;
            if (u == 0)
                return 1;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int IndexThroughFloat(byte[] arr, int x)
    {
        if (arr.Length == 16777220 && x >= 0 && x <= 16777219)
            return arr[(int)(float)x];
        return -1;
    }
}
