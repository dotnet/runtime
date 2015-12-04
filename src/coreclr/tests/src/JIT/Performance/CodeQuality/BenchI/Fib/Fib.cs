// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class Fib
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 3500;
#endif

    const int Number = 24;

    static int Fibonacci(int x) {
        if (x > 2) {
            return (Fibonacci(x - 1) + Fibonacci(x - 2));
        }
        else {
            return 1;
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int fib = Fibonacci(Number);
        return (fib == 46368);
    }

    [Benchmark]
    public static void Test() {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                for (int i = 0; i < Iterations; i++) {
                    Bench();
                }
            }
        }
    }

    static bool TestBase() {
        bool result = true;
        for (int i = 0; i < Iterations; i++) {
            result &= Bench();
        }
        return result;
    }
    
    public static int Main() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
