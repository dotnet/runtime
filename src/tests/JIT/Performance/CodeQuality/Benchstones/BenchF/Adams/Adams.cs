// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The Adams-Moulton Predictor Corrector Method adapted from Conte and de Boor
// original source: adams_d.c

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Xunit;

namespace Benchstone.BenchF
{
public static class Adams
{
#if DEBUG
    public static int Iterations = 1;
#else
    public static int Iterations = 200000;
#endif // DEBUG

    static double g_xn, g_yn, g_dn, g_en;
    const double g_xn_base = 0.09999999E+01;
    const double g_yn_base = 0.71828180E+00;
    const double g_dn_base = 0.21287372E-08;
    const double g_en_base = 0.74505806E-08;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Bench()
    {
        double[] f = new double[5];
        double xn, yn, dn, en, yxn, h, fnp, ynp, y0, x0;
        int i, k, n, nstep;

#if VERBOSE
        Console.WriteLine(" ADAMS-MOULTON METHOD ");
#endif // VERBOSE

        n = 4;
        h = 1.0 / 32.0;
        nstep = 32;
        y0 = 0.0;
        x0 = 0.0;
        xn = 0.0;
        yn = 0.0;
        dn = 0.0;
        en = 0.0;

        f[1] = x0 + y0;
#if VERBOSE
        Console.WriteLine("{0},  {1},  {2},  {3}", x0, y0, dn, en);
#endif // VERBOSE
        xn = x0;
        for (i = 2; i <= 4; i++)
        {
            k = i - 1;
            xn = xn + h;
            yn = Soln(xn);
            f[i] = xn + yn;
#if VERBOSE
            Console.WriteLine("{0},  {1},  {2},  {3},  {4}", k, xn, yn, dn, en);
#endif // VERBOSE
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
#if VERBOSE
            Console.WriteLine("{0},  {1},  {2},  {3},  {4}", k, xn, yn, dn, en);
#endif // VERBOSE
        }

        // Escape calculated values:
        g_xn = xn;
        g_yn = yn;
        g_dn = dn;
        g_en = en;
    }

    private static double Soln(double x)
    {
        return (System.Math.Exp(x) - 1.0 - (x));
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static void TestBench()
    {
        for (int l = 1; l <= Iterations; l++)
        {
            Bench();
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Test(null);
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static int Test(int? arg)
    {
        if (arg.HasValue)
        {
            Iterations = (int)arg;
        }

        Stopwatch sw = Stopwatch.StartNew();
        TestBench();
        sw.Stop();

        bool result = true;
        // Note: we can't check xn or yn better because of the precision
        // with which original results are given
        result &= System.Math.Abs(g_xn_base - g_xn) <= 1.5e-7;
        result &= System.Math.Abs(g_yn_base - g_yn) <= 1.5e-7;
        result &= System.Math.Abs(g_dn) <= 2.5e-9;
        // Actual error is much bigger than base error;
        // this is likely due to the fact that the original program was written in Fortran
        // and was running on a mainframe with a non-IEEE floating point arithmetic
        // (it's still beyond the published precision of yn)
        result &= System.Math.Abs(g_en) <= 5.5e-8;
        Console.WriteLine(result ? "Passed" : "Failed");

        Console.WriteLine(" BASE.....P1 1/4 (ADAMS-MOULTON), XN = {0}", g_xn_base);
        Console.WriteLine(" VERIFY...P1 1/4 (ADAMS-MOULTON), XN = {0}\n", g_xn);
        Console.WriteLine(" BASE.....P1 2/4 (ADAMS-MOULTON), YN = {0}", g_yn_base);
        Console.WriteLine(" VERIFY...P1 2/4 (ADAMS-MOULTON), YN = {0}\n", g_yn);
        Console.WriteLine(" BASE.....P1 3/4 (ADAMS-MOULTON), DN = {0}", g_dn_base);
        Console.WriteLine(" VERIFY...P1 3/4 (ADAMS-MOULTON), DN = {0}\n", g_dn);
        Console.WriteLine(" BASE.....P1 4/4 (ADAMS-MOULTON), EN = {0}", g_en_base);
        Console.WriteLine(" VERIFY...P1 4/4 (ADAMS-MOULTON), EN = {0}\n", g_en);

        Console.WriteLine("Test iterations: {0}; Total time: {1} sec", Iterations, sw.Elapsed.TotalSeconds);
        return (result ? 100 : -1);
    }
}
}
