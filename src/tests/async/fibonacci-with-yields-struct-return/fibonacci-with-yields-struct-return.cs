// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define WITH_OBJECT

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

public class Async2FibonacceWithYields
{
    const int iterations = 3;

    [Fact]
    public static void Test()
    {
        long allocated = GC.GetTotalAllocatedBytes(precise: true);

        AsyncEntry().GetAwaiter().GetResult();

        allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
        System.Console.WriteLine("allocated: " + allocated);
    }

    public struct MyInt
    {
        public int i;
#if WITH_OBJECT
        public object dummy;
#else
        IntPtr dummy;
#endif
        public MyInt(int i) => this.i = i;
    }

    public static async Task AsyncEntry()
    {
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            MyInt result = await Fib(new MyInt(25));
            sw.Stop();

            Console.WriteLine($"{sw.ElapsedMilliseconds} ms result={result.i}");
        }
    }

    static async Task<MyInt> Fib(MyInt n)
    {
        int i = n.i;
        if (i <= 1)
        {
            await Task.Yield();
            return new MyInt(1);
        }

        int i1 = (await Fib(new MyInt(i - 1))).i;
        int i2 = (await Fib(new MyInt(i - 2))).i;

        return new MyInt(i1 + i2);
    }
}
