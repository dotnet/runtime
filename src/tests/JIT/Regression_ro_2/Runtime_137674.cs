// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137674
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(ReadOnlySpan<int> span, ReadOnlySpan<int> repl)
    {
        int i = 0;
        int sum = 0;
        while (true)
        {
            if (span.Length - i < 0)
            {
                sum += 100;
            }

            if (i >= span.Length)
            {
                return sum;
            }

            sum += span[i];
            i++;
            span = repl;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(105, Test(new int[] { 5 }, new int[0]));
    }
}
