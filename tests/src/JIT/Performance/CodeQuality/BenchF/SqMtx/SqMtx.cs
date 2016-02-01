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

public static class SqMtx
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 4000;
#endif

    private const int MatrixSize = 40;

    private static T[][] AllocArray<T>(int n1, int n2)
    {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i)
        {
            a[i] = new T[n2];
        }
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double[][] a = AllocArray<double>(41, 41);
        double[][] c = AllocArray<double>(41, 41);

        int i, j;

        for (i = 1; i <= MatrixSize; i++)
        {
            for (j = 1; j <= MatrixSize; j++)
            {
                a[i][j] = i + j;
            }
        }

        for (i = 1; i <= Iterations; i++)
        {
            Inner(a, c, MatrixSize);
        }

        if (c[1][1] == 23820.0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static void Inner(double[][] a, double[][] c, int n)
    {
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                c[i][j] = 0.0;
                for (int k = 1; k <= n; k++)
                {
                    c[i][j] = c[i][j] + a[i][k] * a[k][j];
                }
            }
        }
    }

    [Benchmark]
    public static void Test()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                Bench();
            }
        }
    }

    private static bool TestBase()
    {
        bool result = Bench();
        return result;
    }

    public static int Main()
    {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
