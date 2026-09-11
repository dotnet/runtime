// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_130216
{
    private static int Sink;

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(new int[6], true));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test(int[] arr, bool c)
    {
        int prev = arr[5];
        int j;
        if (c)
        {
            Sink = 1;
            j = -5;
        }
        else
        {
            j = 3;
        }
        return prev + arr[j];
    }
}
