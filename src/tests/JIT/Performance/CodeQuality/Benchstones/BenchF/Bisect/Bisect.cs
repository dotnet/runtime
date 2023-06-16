// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The Bisect algorithm adapted from Conte and de Boor

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class Bisect
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 400000;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Escape(object _) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        int idbg, iflag;
        double a, b, error, p1, xi;

        iflag = 0;
        error = 0.0;
        xi = 0.0;
        idbg = 0;
        for (int i = 1; i <= Iterations; i++)
        {
            for (int j = 1; j <= 10; j++)
            {
                a = 1.0;
                b = 2.0;
                p1 = 0.000001;
                Inner(ref a, ref b, ref p1, out iflag);
                if (iflag > 1)
                {
                    goto L999;
                }

                xi = (a + b) / 2.0;
                if (a > b)
                {
                    error = (a - b) / 2.0;
                }
                else
                {
                    error = (b - a) / 2.0;
                }

                if (idbg != 0)
                {
                    System.Console.WriteLine(" the root is {0:E} plus/minus {1:E}\n", xi, error);
                }
            }
        }
    L999:
        {
        }

        // Escape iflag, error, xi so that they appear live
        Escape(iflag);
        Escape(error);
        Escape(xi);

        return true;
    }

    private static double FF(double x)
    {
        return ((-1.0 - (x * (1.0 - (x * x)))));
    }

    private static void Inner(ref double a, ref double b, ref double xtol, out int iflag)
    {
        double fa, error;
        double xm, fm;

        iflag = 0;
        fa = FF(a);
        /*      check for sign change  */
        if (((fa) * FF(b)) < 0.0)
        {
            goto L5;
        }

        iflag = 2;
        goto L99;

    L5:
        {
            error = System.Math.Abs(b - a);
        }
    L6:
        error = error / 2.0;
        /*      check for sufficiently small interval  */
        if (error < xtol)
        {
            goto L99;
        }
        xm = (a + b) / 2.0;
        /*      check for unreasonable error requirement */
        if (xm + error == xm)
        {
            goto L20;
        }

        fm = FF(xm);
        /*      change to new interval  */
        if (fa * fm < 0.0)
        {
            goto L9;
        }
        a = xm;
        fa = fm;
        goto L6;
    L9:
        b = xm;
        goto L6;
    L20:
        iflag = 1;
    L99:
        {
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = Bench();
        return (result ? 100 : -1);
    }
}
}
