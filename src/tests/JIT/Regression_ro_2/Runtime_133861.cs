// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133861
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 15)]
    // Prevent inlining Sum without suppressing its implicit tail call with NoInlining.
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void TestEntryPoint(int n, int expected)
    {
        Runtime_133861 c = new Runtime_133861();
        Assert.Equal(expected, c.Sum(c, n, 0));
        Assert.Equal(expected, c.Sum(new Runtime_133861(), n, 0));

        bool threw = false;
        try
        {
            c.Sum(null, n, 0);
        }
        catch (NullReferenceException)
        {
            threw = true;
        }

        Assert.Equal(n != 0, threw);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private int Sum(Runtime_133861 o, int n, int acc)
    {
        if (n == 0)
        {
            return acc;
        }

        return o.Sum(o, n - 1, acc + n);
    }
}
