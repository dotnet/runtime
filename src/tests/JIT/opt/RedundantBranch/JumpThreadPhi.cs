// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class JumpThreadPhi
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Phi_00(int value)
    {
        int idx = BitOperations.TrailingZeroCount(value);
        idx = (idx != 32) ? idx : -1;

        if (idx != -1)
        {
            return idx + 100;
        }

        return -1;
    }

    [Fact]
    public static void TestPhi00()
    {
        Assert.Equal(-1, Phi_00(0));
        Assert.Equal(100, Phi_00(1));
        Assert.Equal(103, Phi_00(8));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Phi_01(bool q, bool r, int u, int v)
    {
        int z = 0, w = 0, m = 0, n = 0;
        if (r)
        {
            m = v;
            n = 1;
            goto Target;
        }

        if (q)
        {
            z = u;
            w = 1;
        }

        if (z != w)
        {
            return -1;
        }

    Target:
        if (m == n)
        {
            return 200;
        }

        return 100 + n;
    }

    [Fact]
    public static void TestPhi01()
    {
        Assert.Equal(200, Phi_01(false, false, 0, 0));
    }
}
