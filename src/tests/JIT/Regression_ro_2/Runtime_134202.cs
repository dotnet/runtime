// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The "x >= 0" guard used to be dropped: a[x]'s bounds check was removed using that very
// assertion, so the non-faulting load was speculated above its own guard.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134202
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool Test(int x, int[] a)
    {
        if (x >= a.Length)
        {
            return false;
        }
        if (x >= 0 && x < (a[x] & int.MaxValue))
        {
            return true;
        }
        return false;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int[] a = new int[4];
        a[1] = 42;
        Assert.True(Test(1, a));
        Assert.False(Test(0, a));
        Assert.False(Test(int.MinValue, a));
    }
}
