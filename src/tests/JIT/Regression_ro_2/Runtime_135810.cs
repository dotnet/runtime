// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_135810;

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Jump threading in redundant branch opts adds new predecessors to Target without
// phi args, so range check elimination must not conclude the index is in range.
public static class Runtime_135810
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int Test(bool cond0, bool cond1, int y)
    {
        int[] arr = new int[1];
        if (cond0)
        {
            y = 0;
            goto Target;
        }

        int v = 0;
        if (!cond1)
        {
            v = y;
        }

        if (v == 0)
        {
            goto Target;
        }

        return v;
    Target:
        return arr[y];
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(false, true, 100));
    }
}
