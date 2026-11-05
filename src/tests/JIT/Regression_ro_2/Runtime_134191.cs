// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134191
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<OverflowException>(() => Test(int.MaxValue, 1, 0, 3, 5, 7, 9));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int p, int q, int y, int z, int w, int u, int v)
    {
        return (checked(p + q) / y) + ((((z * w) + (u * v)) & 0x7FFFFF) / y);
    }
}
