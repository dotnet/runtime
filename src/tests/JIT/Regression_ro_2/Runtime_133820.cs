// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133820
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(int.MinValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int x)
    {
        int[] arr = new int[8];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = i + 1;
        }

        if (x < int.MaxValue)
        {
            // For int.MinValue, the addition underflows and wraps to arr.Length.
            int y = unchecked(x + (-2147483640));
            if (y >= 0)
            {
                return arr[y];
            }
        }

        return -1;
    }
}
