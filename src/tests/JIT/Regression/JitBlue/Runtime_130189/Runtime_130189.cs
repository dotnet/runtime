// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

// Regression test for https://github.com/dotnet/runtime/issues/130189
//
// Redundant branch opts' dominator-based jump threading used to bypass a block
// that contained a globally-used PHI def without updating SSA. That left the
// loop reading a stale (soon to be dead) PHI value, which subsequent induction
// variable widening then miscompiled, producing a wrong result.

public sealed class Runtime_130189
{
    ushort[] d = [0, 0, 1, 65535, 1, 0, 1, 65535, 65535];
    int[] a = [0, 4];
    int i;

    bool Next(out int p)
    {
        bool r;
        if (r = i < a.Length)
            p = a[i++];
        else
            p = -1;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    internal long Sum()
    {
        i = 0;
        long s = 0;
        int x, p;
        while (Next(out p))
        {
            while ((x = d[p]) != 65535)
            {
                s += x;
                p++;
            }
        }
        return s;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(3, new Runtime_130189().Sum());
    }
}
