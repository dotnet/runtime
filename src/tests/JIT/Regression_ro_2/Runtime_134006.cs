// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134006
{
    [Theory]
    [SkipOnMono("CoreCLR JIT regression test")]
    [InlineData(0)]
    [InlineData(1)]
    public static void TestEntryPoint(int value)
    {
        int finallyCount = 0;
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => LoopInFinally(value, ref finallyCount));
        Assert.Equal("boom", exception.Message);
        Assert.Equal(1, finallyCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void LoopInFinally(int value, ref int finallyCount)
    {
        int local = value;
        try
        {
            throw new InvalidOperationException("boom");
        }
        finally
        {
        Start:
            if (local == 1)
            {
                goto Done;
            }

            local = 1;
            goto Start;

        Done:
            finallyCount++;
        }
    }
}
