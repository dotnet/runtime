// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133821
{
    private static int s_sink;

    [Fact]
    public static void TestEntryPoint()
    {
        int[] array = new int[8];
        Assert.Equal(0, Test(array, 4, 2, true));
        Assert.Equal(0, Test(array, 4, 2, false));
        Assert.Equal(-1, Test(array, 4, 8, false));
        Assert.Throws<IndexOutOfRangeException>(() => Test(array, -5, -3, false));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] array, int length, int index, bool allocate)
    {
        if (allocate)
        {
            int[] temporary = new int[length];
            s_sink = temporary[0];
        }

        int selectedIndex = 0;
        if (index >= length)
        {
            selectedIndex = index;
        }

        if (selectedIndex < array.Length)
        {
            return array[selectedIndex];
        }

        return -1;
    }
}
