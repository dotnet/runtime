// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_133585;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133585
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] array, int index, int dividend, int divisor, int count)
    {
        int result = 0;

        for (int i = 0; i < count; i++)
        {
            result += array[index];
            result += dividend / divisor;
        }

        return result;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(new int[1], 5, 1, 0, 5));
    }
}
