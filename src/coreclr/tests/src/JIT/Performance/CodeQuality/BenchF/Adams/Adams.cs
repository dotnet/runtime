// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// The Adams-Moulton Predictor Corrector Method adapted from Conte and de Boor

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class Adams
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 200000;
#endif

    public static volatile object VolatileObject;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Escape(object obj)
    {
        VolatileObject = obj;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double[] f = new double[5];
        double xn, yn, h, fnp, ynp, y0, x0;
        int i, k, l, n, nstep;

        n = 4;
        h = 1.0 / 32.0;
        nstep = 32;
        y0 = 0.0;
        x0 = 0.0;
        xn = 0.0;
        yn = 0.0;

        for (l = 1; l <= Iterations; l++)
        {
            f[1] = x0 + y0;

            xn = x0;
            for (i = 2; i <= 4; i++)
            {
                k = i - 1;
                xn = xn + h;
                yn = Soln(xn);
                f[i] = xn + yn;
            }

            for (k = 4; k <= nstep; k++)
            {
                ynp = yn + (h / 24) * (55 * f[n] - 59 * f[n - 1] + 37 * f[n - 2] - 9 * f[n - 3]);
                xn = xn + h;
                fnp = xn + ynp;
                yn = yn + (h / 24) * (9 * fnp + 19 * f[n] - 5 * f[n - 1] + f[n - 2]);
                f[n - 3] = f[n - 2];
                f[n - 2] = f[n - 1];
                f[n - 1] = f[n];
                f[n] = xn + yn;
            }
        }

        // Escape f so that its elements will be live-out
        Escape(f);

        return true;
    }

    private static double Soln(double x)
    {
        return (System.Math.Exp(x) - 1.0 - (x));
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
