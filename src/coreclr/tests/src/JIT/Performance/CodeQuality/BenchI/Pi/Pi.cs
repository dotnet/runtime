// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class Pi
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100;
#endif

    static int[] ComputePi(int[] a) {

        int d = 4;
        int r = 10000;
        int n = 251;
        int m = (int)(3.322 * n * d);
        int[] digits = new int[n];
        int i, k, q;

        for (i = 0; i <= m; i++) {
            a[i] = 2;
        }

        a[m] = 4;

        for (i = 1; i <= n; i++) {
            q = 0;
            for (k = m; k > 0L; k--) {
                a[k] = a[k] * r + q;
                q = a[k] / (2 * k + 1);
                a[k] -= (2 * k + 1) * q;
                q *= k;
            }
            a[0] = a[0] * r + q;
            q = a[0] / r;
            a[0] -= q * r;
            digits[i-1] = q;
        }

        return digits;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench(int[] a) {
        int[] digits = ComputePi(a);
        return (digits[0] == 3 && digits[1] == 1415 && digits[2] == 9265 && digits[250] == 1989);
    }

    [Benchmark]
    public static void Test() {
        int[] a = new int[3340];
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                for (int i = 0; i < Iterations; i++) {
                    Bench(a);
                }
            }
        }
    }

    static bool TestBase() {
        bool result = true;
        int[] a = new int[3340];
        for (int i = 0; i < Iterations; i++) {
            result &= Bench(a);
        }
        return result;
    }
    
    public static int Main() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
