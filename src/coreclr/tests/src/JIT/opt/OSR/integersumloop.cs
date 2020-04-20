// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

class IntegerSumLoop
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F(int from, int to)
    {
        int result = 0;
        for (int i = from; i < to; i++)
        {
            result += i;
        }
        return result;
    }

    public static int Main(string[] args)
    {
        int final = 1_000_000;
        long frequency = Stopwatch.Frequency;
        long nanosecPerTick = (1000L*1000L*1000L) / frequency;
        F(0, 10);
        Stopwatch s = new Stopwatch();
        s.Start();
        int result = F(0, final);
        s.Stop();
        double elapsedTime = 1000.0 * (double) s.ElapsedTicks / (double) frequency;
        Console.WriteLine($"{final} iterations took {elapsedTime:F2}ms");
        return result == 1783293664 ? 100 : -1;
    }  
}
