// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class InProd
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 70;
#endif

    const int RowSize = 10 * Iterations;

    static int s_seed;

    static T[][] AllocArray<T>(int n1, int n2) {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2];
        }
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        double[][] rma = AllocArray<double>(RowSize, RowSize);
        double[][] rmb = AllocArray<double>(RowSize, RowSize);
        double[][] rmr = AllocArray<double>(RowSize, RowSize);

        double sum;

        Inner(rma, rmb, rmr);

        for (int i = 1; i < RowSize; i++)
        {
            for (int j = 1; j < RowSize; j++)
            {
                sum = 0;
                for (int k = 1; k < RowSize; k++)
                {
                    sum = sum + rma[i][k] * rmb[k][j];
                }
                if (rmr[i][j] != sum)
                {
                    return false;
                }
           }
        }

        return true;
    }

    static void InitRand()
    {
        s_seed = 7774755;
    }

    static int Rand() {
        s_seed = (s_seed * 77 + 13218009) % 3687091;
        return s_seed;
    }

    static void InitMatrix(double[][] m) {
        for (int i = 1; i < RowSize; i++)
        {
            for (int j = 1; j < RowSize; j++)
            {
                m[i][j] = (Rand() % 120 - 60) / 3;
            }
        }
    }

    static void InnerProduct(out double result, double[][] a, double[][] b, int row, int col) {
        result = 0.0;
        for (int i = 1; i < RowSize; i++)
        {
            result = result + a[row][i] * b[i][col];
        }
    }

    static void Inner(double[][] rma, double[][] rmb, double[][] rmr) {
        InitRand();
        InitMatrix(rma);
        InitMatrix(rmb);
        for (int i = 1; i < RowSize; i++)
        {
            for (int j = 1; j < RowSize; j++)
            {
                InnerProduct(out rmr[i][j], rma, rmb, i, j);
            }
        }
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
