// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134938
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(1, Test(10, 0, 5));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int a, int b, int n)
    {
        int state = 0;
        int sum = 0;
        try
        {
            int i = 0;
            do
            {
                state = i + 1;
                sum += a / b;
                i++;
            } while (i < n);
        }
        catch (DivideByZeroException)
        {
            return state;
        }
        return sum;
    }
}
