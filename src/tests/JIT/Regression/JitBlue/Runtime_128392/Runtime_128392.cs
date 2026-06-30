// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_128392;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_128392
{
    private static long s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Process(int x)
    {
        if (x < 0)
        {
            throw new Exception("boom");
        }

        s_sink += x;
    }

    // The loop counter 'i' is updated inside the try and read in the catch handler.
    // Induction-variable optimization must not drop the in-loop update of 'i', since
    // it is live into the EH handler; otherwise the handler observes a stale value.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int CountProcessedBeforeThrow(int[] values)
    {
        int i = 0;
        try
        {
            for (; i < values.Length; i++)
            {
                Process(values[i]);
            }
        }
        catch (Exception)
        {
            return i;
        }

        return -1;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int[] values = { 0, 1, 2, -1, 4 };
        Assert.Equal(3, CountProcessedBeforeThrow(values));
    }
}
