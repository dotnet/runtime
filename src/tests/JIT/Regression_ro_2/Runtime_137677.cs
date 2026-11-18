// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137677
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] a, int limit)
    {
        int sum = 0;
        int i = 0;
        int retries = 0;
        while (true)
        {
            sum += a[i];
            try
            {
                i++;
                if (i >= checked(limit + (int.MinValue + 5)))
                {
                    break;
                }
            }
            catch (OverflowException)
            {
                if (++retries > 1000)
                {
                    return -1;
                }
            }
        }
        return sum;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(new int[8], int.MinValue + 3));
    }
}
