// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135818
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(499999500000L, Test(true, 1_000_000, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Test(bool flag, int a, int b)
    {
        long sum = 0;
        int i = 0;
        while (i < (flag ? a : b))
        {
            sum += i;
            i++;
        }
        return sum;
    }
}
