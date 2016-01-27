// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class Ackermann
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    static int Acker(int m, int n) {
        if (m == 0) {
            return n + 1;
        }
        else if (n == 0) {
            return Acker(m - 1, 1);
        }
        else {
            return Acker(m - 1, Acker(m, n - 1));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int a00 = Acker(0, 0);
        int a11 = Acker(1, 1);
        int a22 = Acker(2, 2);
        int a33 = Acker(3, 3);
        return (a00 == 1) && (a11 == 3) && (a22 == 7) & (a33 == 61);
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
