// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_122138
{
    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static void TestEntryPoint()
    {
        var test = new Runtime_122138();
        test.Method1(0, 0, 0, 999, 999);
    }

    private void Method1(int a, int b, int c, int value1, int? value2)
    {
        Method2(1, 2, 3, value1, value2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Method2(long a, int b, int c, int? value1, int? value2)
    {
        Assert.Equal(value1, value2);
    }
}