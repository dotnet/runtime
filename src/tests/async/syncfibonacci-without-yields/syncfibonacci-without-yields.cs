// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;

public class SyncFibonacci
{
    const int iterations = 3;
    const bool doYields = false;

    public static int Main()
    {
        long allocated = GC.GetTotalAllocatedBytes(precise: true);

        Entry();

        allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
        System.Console.WriteLine("allocated: " + allocated);

        return 100;
    }

    public static void Entry()
    {
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            int result = Fib(25);
            sw.Stop();

            Console.WriteLine($"{sw.ElapsedMilliseconds} ms result={result}");
        }
    }

    static int Fib(int i)
    {
        if (i <= 1)
        {
            return 1;
        }

        int i1 = Fib(i - 1);
        int i2 = Fib(i - 2);

        return i1 + i2;
    }
}
