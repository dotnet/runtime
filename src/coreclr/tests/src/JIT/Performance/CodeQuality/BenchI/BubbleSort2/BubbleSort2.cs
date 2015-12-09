// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class BubbleSort2
{

#if DEBUG
    public const int Iterations = 1;
    public const int Bound = 5 * Iterations;
#else
    public const int Iterations = 15;
    public const int Bound = 500 * Iterations;
#endif

    static void Inner(int[] x) {
        int limit1 = Bound - 1;
        for (int i = 1; i <= limit1; i++) {
            for (int j = i; j <= Bound; j++) {
                if (x[i] > x[j]) {
                    int temp = x[j];
                    x[j] = x[i];
                    x[i] = temp;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[] x = new int[Bound + 1];
        int i, j;
        int limit;
        j = 99999;
        limit = Bound - 2;
        i = 1;
        do {
            x[i] = j & 32767;
            x[i + 1] = (j + 11111) & 32767;
            x[i + 2] = (j + 22222) & 32767;
            j = j + 33333;
            i = i + 3;
        } while (i <= limit);
        x[Bound - 1] = j;
        x[Bound] = j;

        Inner(x);

        for (i = 0; i < Bound - 1; i++) {
            if (x[i] > x[i + 1]) {
                return false;
            }
        }

        return true;
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
