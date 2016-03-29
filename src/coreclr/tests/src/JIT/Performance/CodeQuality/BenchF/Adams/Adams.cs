// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// The Adams-Moulton Predictor Corrector Method adapted from Conte and de Boor
// original source: adams_d.c

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
#if XUNIT_PERF
using Xunit;
using Microsoft.Xunit.Performance;
#endif

#if XUNIT_PERF
[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]
#endif

public static class Adams
{
#if DEBUG
    public static int Iterations = 1;
#else
    public static int Iterations = 200000;
#endif

    static double g_xn, g_yn, g_dn, g_en;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Bench()
    {
        double[] f = new double[5];
        double xn, yn, dn, en, yxn, h, fnp, ynp, y0, x0, nz;
        int i, k, n, nstep;

#if DEBUG
        Console.WriteLine(" ADAMS-MOULTON METHOD ");
#endif // DEBUG 

        n = 4;
        h = 1.0 / 32.0;
        nstep = 32;
        y0 = 0.0;
        x0 = 0.0;
        xn = 0.0;
        yn = 0.0;
        dn = 0.0;
        en = 0.0;
        nz = 0;

        f[1] = x0 + y0;
#if DEBUG
        Console.WriteLine("{0},  {1},  {2},  {3},  {4}", nz, x0, y0, dn, en);
#endif // DEBUG
        xn = x0;
        for (i = 2; i <= 4; i++)
        {
            k = i - 1;
            xn = xn + h;
            yn = Soln(xn);
            f[i] = xn + yn;
#if DEBUG
            Console.WriteLine("{0},  {1},  {2},  {3},  {4}", k, xn, yn, dn, en);
#endif // DEBUG 
        }

        for (k = 4; k <= nstep; k++)
        {
            ynp = yn + (h / 24) * (55 * f[n] - 59 * f[n - 1] + 37 * f[n - 2] - 9 * f[n - 3]);
            xn = xn + h;
            fnp = xn + ynp;
            yn = yn + (h / 24) * (9 * fnp + 19 * f[n] - 5 * f[n - 1] + f[n - 2]);
            dn = (yn - ynp) / 14;
            f[n - 3] = f[n - 2];
            f[n - 2] = f[n - 1];
            f[n - 1] = f[n];
            f[n] = xn + yn;
            yxn = Soln(xn);
            en = yn - yxn;
#if DEBUG
            Console.WriteLine("{0},  {1},  {2},  {3},  {4}", k, xn, yn, dn, en);
#endif // DEBUG
        }

        // Escape calculated values:
        g_xn += xn;
        g_yn += yn;
        g_dn += dn;
        g_en += en;
    }

    private static double Soln(double x)
    {
        return (System.Math.Exp(x) - 1.0 - (x));
    }

#if XUNIT_PERF
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
#endif

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static int Main(string[] argv)
    {
        if (argv.Length > 0)
        {
            Iterations = Int32.Parse(argv[0]);
        }

        Stopwatch sw = Stopwatch.StartNew();

        for (int l = 1; l <= Iterations; l++)
        {
            Bench();
        }
        sw.Stop();

        Console.WriteLine("Test iterations: {0}; Total time: {1} sec", Iterations, sw.Elapsed.TotalSeconds);

        Console.WriteLine(" BASE.....P1 1/4 (ADAMS-MOULTON), XN = .09999999E +01");
        Console.WriteLine(" VERIFY...P1 1/4 (ADAMS-MOULTON), XN = {0}\n", g_xn / Iterations);
        Console.WriteLine(" BASE.....P1 2/4 (ADAMS-MOULTON), YN = .71828180E+00");
        Console.WriteLine(" VERIFY...P1 2/4 (ADAMS-MOULTON), YN = {0}\n", g_yn / Iterations);
        Console.WriteLine(" BASE.....P1 3/4 (ADAMS-MOULTON), DN = .21287372E-08");
        Console.WriteLine(" VERIFY...P1 3/4 (ADAMS-MOULTON), DN = {0}\n", g_dn / Iterations);
        Console.WriteLine(" BASE.....P1 4/4 (ADAMS-MOULTON), EN = .74505806E-08");
        Console.WriteLine(" VERIFY...P1 4/4 (ADAMS-MOULTON), EN = {0}", g_en / Iterations);

        Console.WriteLine("Passed");
        return (100);
    }
}
