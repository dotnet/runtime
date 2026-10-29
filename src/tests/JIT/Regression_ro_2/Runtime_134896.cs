// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134896
{
    private static int Zero() => 0;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int a)
    {
        switch (Zero())
        {
            default: return a + 2;
            case 0: case 1: return a;
            case 2: return a + 1;
            case 3: return a * 3;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(5, Test(5));
    }
}
