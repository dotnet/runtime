// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The secant algorithm adapted from Conte and DeBoor

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class Secant
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 3000000;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Escape(object _) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        int idbg, iflag;
        double x0, x1, fx1;

        iflag = 0;
        idbg = 0;
        x1 = 0;
        fx1 = 0.0;

        for (int i = 1; i <= Iterations; i++)
        {
            x0 = 1.0;
            x1 = 2.0;
            Inner(ref x0, ref x1, 0.0000001, 0.0000001, 30, out iflag);
            if (iflag > 1)
            {
                goto L888;
            }

            fx1 = FF(x1);
            if (idbg != 0)
            {
                System.Console.WriteLine(" the root is {0:E}, F(ROOT):= {1:E}\n", x1, fx1);
            }
        L888:
            {
            }
        }

        // Escape iflag, x1, and fx1 so that they appear live
        Escape(iflag);
        Escape(x1);
        Escape(fx1);

        return true;
    }

    private static double FF(double x)
    {
        return (-1.0 - (x * (1.0 - (x * x))));
    }

    private static void Inner(ref double x0, ref double x1, double xtol, double ftol, int ntol, out int iflag)
    {
        double deltax, deltaf, f0, f1;

        iflag = 0;
        f0 = FF(x0);
        deltax = x1 - x0;

        for (int n = 1; n <= ntol; n++)
        {
            f1 = FF(x1);

            if (System.Math.Abs(f1) <= ftol)
            {
                goto L30;
            }

            deltaf = f0 - f1;
            if (deltaf == 0.0)
            {
                goto L999;
            }

            deltax = f1 / deltaf * deltax;
            x0 = x1;
            x1 = x1 + deltax;
            if (System.Math.Abs(deltax) <= xtol)
            {
                goto L88;
            }

            f0 = f1;
        }

    L999:
        iflag = 2;
        goto L88;
    L30:
        iflag = 1;
    L88:
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
