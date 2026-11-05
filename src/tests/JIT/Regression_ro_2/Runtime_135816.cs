// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135816
{
    private static int s_sideCalls;

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Test1(-1));
        Assert.Equal(1, s_sideCalls);

        Assert.Throws<IndexOutOfRangeException>(() => Test2(new int[2], 5, -1));
        Assert.Throws<IndexOutOfRangeException>(() => Test3(new long[2], 5, -1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Side()
    {
        s_sideCalls++;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test1(int x) => Side() + int.Log2(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test2(int[] a, int i, int x) => a[i] + int.Log2(x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Test3(long[] a, int i, long x) => a[i] + long.Log2(x);
}
