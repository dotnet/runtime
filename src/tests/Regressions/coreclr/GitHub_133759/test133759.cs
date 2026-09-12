// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(int seed, long[] dst)
    {
        int c = seed;
        for (int i = 0; i < 10; i++)
        {
            dst[i] = (uint)c * 8L;
            c++;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        long[] actual = new long[10];
        Test(-3, actual);

        Assert.Equal(
            [
                34359738344,
                34359738352,
                34359738360,
                0,
                8,
                16,
                24,
                32,
                40,
                48,
            ],
            actual);

        return 100;
    }
}
