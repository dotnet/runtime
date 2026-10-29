// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133271
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(new int[4], int.MinValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] array, int index)
    {
        if ((uint)(index - int.MaxValue) < (uint)array.Length && index < array.Length)
        {
            ref int unused = ref array[index];
            return 1;
        }
        return 0;
    }
}
