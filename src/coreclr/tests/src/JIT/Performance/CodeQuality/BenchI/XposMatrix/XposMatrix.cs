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

public static class XposMatrix
{
    public const int ArraySize = 100;

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 25000;
#endif

    static T[][] AllocArray<T>(int n1, int n2) {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2];
        }
        return a;
    }

    static void Inner(int[][] x, int n) {
        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= n; j++) {
                int t = x[i][j];
                x[i][j] = x[j][i];
                x[j][i] = t;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench(int[][] matrix) {

        int n = ArraySize;
        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= n; j++) {
                matrix[i][j] = 1;
            }
        }
        
        if (matrix[n][n] != 1) {
            return false;
        }
        
        Inner(matrix, n);

        if (matrix[n][n] != 1) {
            return false;
        }

        return true;
    }

    [Benchmark]
    public static void Test() {
        int[][] matrix = AllocArray<int>(ArraySize + 1, ArraySize + 1);
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                for (int i = 0; i < Iterations; i++) {
                    Bench(matrix);
                }
            }
        }
    }

    static bool TestBase() {
        int[][] matrix = AllocArray<int>(ArraySize + 1, ArraySize + 1);
        bool result = true;
        for (int i = 0; i < Iterations; i++) {
            result &= Bench(matrix);
        }
        return result;
    }

    public static int Main() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
