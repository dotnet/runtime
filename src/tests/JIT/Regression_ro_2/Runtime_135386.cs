// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_135386
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(5, Test(5));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use(ref int x)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int v)
    {
        int x = 0;
        Use(ref x);
        Volatile.Write(ref x, v);
        Volatile.Write(ref x, v);
        return Volatile.Read(ref x);
    }
}
