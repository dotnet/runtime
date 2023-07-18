// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

// Example from the OSR doc

public class OSR_Example
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double F(int from, int to)
    {
        double result = 0;
        for (int i = from; i < to; i++)
        {
            result += (double)i;
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test(int? arg)
    {
        int final = arg ?? 1_000_000;
        long frequency = Stopwatch.Frequency;
        long nanosecPerTick = (1000L*1000L*1000L) / frequency;
        // Console.WriteLine($"computing sum over {final} ints");
        // Get some of the initial jit cost out of the way
        Stopwatch s = new Stopwatch();
        s.Start();
        s.Stop();

        s = new Stopwatch();
        s.Start();
        double result = F(0, final);
        s.Stop();
        double elapsedTime = 1000.0 * (double) s.ElapsedTicks / (double) frequency;
        Console.WriteLine($"{final} iterations took {elapsedTime:F2}ms");
        return result == 499999500000 ? 100 : -1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Test(null);
    }
}
