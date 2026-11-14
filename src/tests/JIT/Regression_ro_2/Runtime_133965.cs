// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133965
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Neg() => -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long NegL() => -1;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(int x)
    {
        int unused = int.Log2(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void TestLong(long x)
    {
        long unused = long.Log2(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void TestNative(nint x)
    {
        nint unused = nint.Log2(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestUsed(int x)
    {
        return int.Log2(x);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Test(8);
        TestLong(8);
        TestNative(8);
        Assert.Equal(3, TestUsed(8));
        Assert.Throws<ArgumentOutOfRangeException>(() => Test(Neg()));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestLong(NegL()));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestNative(Neg()));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestUsed(Neg()));
    }
}
