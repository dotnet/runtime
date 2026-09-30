// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134936
{
    private class Finalizable
    {
        public static int Count;

        ~Finalizable()
        {
            Count++;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        try
        {
            Test(1, 0, 10);
        }
        catch (DivideByZeroException)
        {
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.Equal(1, Finalizable.Count);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int a, int b, int n)
    {
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            new Finalizable();
            sum += a / b;
        }

        return sum;
    }
}
