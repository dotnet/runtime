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

public static class BenchMk2
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 4000000;
#endif

    static int s_i, s_n;
    static double s_p, s_a, s_x, s_f, s_e;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
       s_p = Math.Acos(-1.0);
       s_a = 0.0;
       s_n = Iterations;
       s_f = s_p / s_n;
       for (s_i = 1; s_i <= s_n; ++s_i) {
          s_f = s_p / s_n;
          s_x = s_f * s_i;
          s_e = Math.Abs(Math.Log(Math.Exp(s_x)) / s_x) - Math.Sqrt((Math.Sin(s_x) * Math.Sin(s_x)) + Math.Cos(s_x) * Math.Cos(s_x));
          s_a = s_a + Math.Abs(s_e);
       }

       return true;
    }

    [Benchmark]
    public static void Test() {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                Bench();
            }
        }
    }

    static bool TestBase() {
        bool result = Bench();
        return result;
    }

    public static int Main() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
