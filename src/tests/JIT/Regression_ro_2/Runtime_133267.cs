// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133267
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(4, Test(new object[4], [1, 1, 1, 1]));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(object[] key, int[] fields)
    {
        int c = key != null ? key.Length : 0;
        if (key is null || c <= 0 || fields.Length != c)
        {
            throw new ArgumentException();
        }

        int sum = 0;
        for (int i = 0; i < key.Length; i++)
        {
            sum += fields[i];
        }
        return sum;
    }
}
