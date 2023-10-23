// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;

public class Async2FibonacceWithYields
{
    const uint Threshold = 1_000;
    static bool done;

    [Fact]
    public static void Test()
    {
        AsyncEntry().Wait();
    }

    public static async Task AsyncEntry()
    {
        for (int i = 0; i < 10 && !done; i++)
        {
            var sw = new Stopwatch();
            sw.Start();
            uint result = await A(100_000_000);
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms result={result}");
        }
    }

    static async2 uint A(uint n)
    {
        uint result = n;
        for (uint i = 0; i < n && !done; i++)
            result = await B(result);
        return result;
    }

    static async2 uint B(uint n)
    {
        uint result = n;

        result = result * 1_999_999_981;
        if (result < Threshold)
        {
            await Task.Yield();
        }

        result = result * 1_999_999_981;
        if (result < Threshold)
        {
            await Task.Yield();
        }

        result = result * 1_999_999_981;
        if (result < Threshold)
        {
            await Task.Yield();
        }

        return result;
    }
}
