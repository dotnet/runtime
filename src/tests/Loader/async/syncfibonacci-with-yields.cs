// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
﻿using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Xunit;

public class SyncFibonacciWithYields
{
    const uint Threshold = 1_000;

    [Fact]
    public static void Test()
    {
        for (int i = 0; i < 10; i++)
        {
            var sw = new Stopwatch();
            sw.Start();
            uint result = A(100_000_000);
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms result={result}");
        }
    }

    static uint A(uint n)
    {
        uint result = n;
        for (uint i = 0; i < n; i++)
            result = B(result);
        return result;
    }

    static uint B(uint n)
    {
        uint result = n;

        result = result * 1_999_999_981;
        if (result < Threshold)
            MyYield();

        result = result * 1_999_999_981;
        if (result < Threshold)
            MyYield();

        result = result * 1_999_999_981;
        if (result < Threshold)
            MyYield();

        return result;
    }

    // Workaround for inlining of Thread.Yield inflating the caller's frame.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void MyYield() => Thread.Yield();

}

