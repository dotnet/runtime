// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134004
{
    private static bool s_throw = true;

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(0, Test(5));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Zero() => 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Thrower()
    {
        if (s_throw)
        {
            throw new Exception("boom");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(int v)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SetRef(ref int x, int v)
    {
        x = v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int p)
    {
        int x = 0;
        try
        {
            Thrower();
        }
        catch (Exception)
        {
            SetRef(ref x, p);
            Consume(x);
        }

        x = Zero();
        Consume(x);
        return x;
    }
}
