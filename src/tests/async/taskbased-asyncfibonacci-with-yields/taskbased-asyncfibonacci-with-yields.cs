// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;

public class TaskBasedAsyncFibonacciWithYields
{
    const int iterations = 3;
    const bool doYields = true;

    public static int Main()
    {
        long allocated = GC.GetTotalAllocatedBytes(precise: true);

        AsyncEntry().GetAwaiter().GetResult();

        allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
        System.Console.WriteLine("allocated: " + allocated);

        return 100;
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    public static async Task AsyncEntry()
    {
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            int result = await Fib(25);
            sw.Stop();

            Console.WriteLine($"{sw.ElapsedMilliseconds} ms result={result}");
        }
    }

    [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
    static async Task<int> Fib(int i)
    {
        if (i <= 1)
        {
            if (doYields)
            {
                await Task.Yield();
            }

            return 1;
        }

        int i1 = await Fib(i - 1);
        int i2 = await Fib(i - 2);

        return i1 + i2;
    }
}
