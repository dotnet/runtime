// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134454
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(-3, Test(0));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int n)
    {
        int i = 0, m = -4;
        while (i < 100 && m < 20)
        {
            if (i < -2)
                return i;
            int k = m + 2;
            i += k;
            n ^= k;
            m++;
        }
        return n;
    }
}
