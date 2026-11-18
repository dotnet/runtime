// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137676
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] a, uint n)
    {
        int sum = 0;
        uint i = 0;
        do
        {
            sum += a[i];
            i++;
        } while (i < n);
        return sum;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(new int[4], 0x80000000u));
    }
}
