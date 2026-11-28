// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_138446
{
    // Mimics the Debug IL shape of the original repro (bottom-tested loops with bool temps)
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] a)
    {
        int i = 0;
        goto outerCond;
    outerBody:
        int j = 0;
        goto innerCond;
    innerBody:
        a[i] = 1;
        j++;
        i++;
    innerCond:
        bool b1 = j < 50 ? i < 200 : false;
        if (b1) goto innerBody;
        bool b2 = i < 200;
        if (b2)
        {
            a[i] = 2;
            i++;
        }
    outerCond:
        bool b3 = i < 200;
        if (b3) goto outerBody;
        return Array.IndexOf(a, 2);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(50, Test(new int[200]));
    }
}
