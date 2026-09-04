// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_133251;

public class Runtime_133251
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(1, Case(GetX()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long GetX() => 9223372036854775806L;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Case(long x)
    {
        int result = 0;
        long y = x - 16;

        if (x == 9223372036854775806L)
        {
            result |= 1;

            if (y < -2)
            {
                result |= 2;
            }
        }

        return result;
    }
}
