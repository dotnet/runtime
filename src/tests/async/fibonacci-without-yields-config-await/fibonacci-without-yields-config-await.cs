// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

#pragma warning disable SYSLIB5007 // 'System.Runtime.CompilerServices.AsyncHelpers' is for evaluation purposes only

public class Async2FibonacciWithYields
{
    const int iterations = 3;
    const bool doYields = false;

    [Fact]
    public static void Test()
    {
        long allocated = GC.GetTotalAllocatedBytes(precise: true);

        AsyncEntry().GetAwaiter().GetResult();

        allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
        System.Console.WriteLine("allocated: " + allocated);
    }

    public static async Task AsyncEntry()
    {
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            int result = AsyncHelpers.Await(Fib(30).ConfigureAwait(false));
            sw.Stop();

            Console.WriteLine($"{sw.ElapsedMilliseconds} ms result={result}");
        }
    }

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

        int i1 = AsyncHelpers.Await(Fib(i - 1).ConfigureAwait(true));
        int i2 = AsyncHelpers.Await(Fib(i - 2).ConfigureAwait(false));

        return i1 + i2;
    }
}
