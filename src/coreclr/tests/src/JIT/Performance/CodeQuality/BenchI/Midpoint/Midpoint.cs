// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class Midpoint
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 70000;
#endif

    static T[][] AllocArray<T>(int n1, int n2) {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2];
        }
        return a;
    }

    static int Inner(ref int x, ref int y, ref int z) {
        int mid;

        if (x < y) {
            if (y < z) {
                mid = y;
            }
            else {
                if (x < z) {
                    mid = z;
                }
                else {
                    mid = x;
                }
            }
        }
        else {
            if (x < z) {
                mid = x;
            }
            else {
                if (y < z) {
                    mid = z;
                }
                else {
                    mid = y;
                }
            }
        }

        return (mid);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[][] a = AllocArray<int>(2001, 4);
        int[] mid = new int[2001];
        int j = 99999;

        for (int i = 1; i <= 2000; i++) {
            a[i][1] = j & 32767;
            a[i][2] = (j + 11111) & 32767;
            a[i][3] = (j + 22222) & 32767;
            j = j + 33333;
        }

        for (int k = 1; k <= Iterations; k++) {
            for (int l = 1; l <= 2000; l++) {
                mid[l] = Inner(ref a[l][1], ref a[l][2], ref a[l][3]);
            }
        }

        return (mid[2000] == 17018);
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
