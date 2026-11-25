// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_135813;

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Jump threading in redundant branch opts bypasses the loop header, so x * a in the
// two arms now uses different values of x. CSE must not treat them as equal.
public static class Runtime_135813
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int Test(int v0, int v1, int v2, int a, int c)
    {
        int flag = 0;
        int x = v0;
        int k = a;
        while (true)
        {
            if (flag == 0)
            {
                if (x * a == c)
                    return -1;
            }
            else
            {
                int z = x * a + k;
                if (z > c)
                    return z;
            }
            flag = 1;
            x = v1;
            k = v2;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(1035, Test(3, 5, 1000, 7, 100));
    }
}
