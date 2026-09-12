// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133583
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(2, NotEqual());
        Assert.Equal(2, LessThan());
        Assert.Equal(3, LessThanOrEqual());
        Assert.Equal(2, GreaterThan());
        Assert.Equal(3, GreaterThanOrEqual());
        Assert.Equal(1, SubtractIntMinValue());
        Assert.Throws<OverflowException>(() => { _ = NotEqualOverflow(); });
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NotEqual()
    {
        int count = 0;
        int i = 0;
        while (i != 2)
        {
            count++;
            i = checked(i - (-1));
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int LessThan()
    {
        int count = 0;
        int i = -2;
        while (i < 0)
        {
            count++;
            i = checked(i - (-1));
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int LessThanOrEqual()
    {
        int count = 0;
        int i = -2;
        while (i <= 0)
        {
            count++;
            i = checked(i - (-1));
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int GreaterThan()
    {
        int count = 0;
        int i = 2;
        while (i > 0)
        {
            count++;
            i = checked(i - 1);
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int GreaterThanOrEqual()
    {
        int count = 0;
        int i = 2;
        while (i >= 0)
        {
            count++;
            i = checked(i - 1);
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SubtractIntMinValue()
    {
        int count = 0;
        int i = int.MinValue;
        while (i != 0)
        {
            count++;
            i = checked(i - int.MinValue);
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NotEqualOverflow()
    {
        int count = 0;
        int i = int.MaxValue - 1;
        while (i != int.MaxValue - 2)
        {
            count++;
            i = checked(i - (-1));
        }

        return count;
    }
}
