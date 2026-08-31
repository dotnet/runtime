// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System;
using Xunit;

public unsafe class TripCountOverflow
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<Exception>(() => InfiniteLoopExitedByException((long)int.MaxValue + 1, (long)int.MaxValue + 5));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long InfiniteLoopExitedByException(long n, long k)
    {
        // Since the caller passes int.MaxValue + 1 this loop is infinite, so we shouldn't be able to analyze
        // its trip count (without doing some form of cloning).
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += 3;

            if (k-- == 0)
            {
                sum += Foo();
            }
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo()
    {
        throw new Exception();
    }
}
