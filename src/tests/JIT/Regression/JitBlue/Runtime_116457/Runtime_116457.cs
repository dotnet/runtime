// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_116457
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Problem(new double[40]));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Problem(double[] arr)
    {
        int a = 50;
        double b = 1;
        for (int i = 0; i < arr.Length; i++)
        {
            var element = i < a ? arr[i * 2] : arr[i - a];
            b = element + 1;
        }
    }
}

