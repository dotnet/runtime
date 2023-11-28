// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Newton's method adapted from Conte and De Boor

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class NewtR
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 80000000;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Escape(object _) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        int idbg, iflag;
        double x0, fx0;

        iflag = 0;
        idbg = 0;
        fx0 = 0.0;
        x0 = 1.0;

        for (int i = 1; i <= Iterations; i++)
        {
            Inner(ref x0, 0.0000001, 0.0000001, 10, out iflag);
            if (iflag > 1)
            {
                goto L888;
            }

            fx0 = FF(x0);
            if (idbg != 0)
            {
                System.Console.WriteLine(" THE ROOT IS {0:e} F(ROOT) := {1:E}\n", x0, fx0);
            }

        L888:
            {
            }
        }

        // Escape iflag, x0, and fx0 so that they appear live
        Escape(iflag);
        Escape(x0);
        Escape(fx0);

        return true;
    }

    private static double FF(double x)
    {
        return (-1.0 - ((x) * (1.0 - ((x) * (x)))));
    }

    private static double FFDer(double x)
    {
        return (3.0 * (x) * (x) - 1.0);
    }

    private static void Inner(ref double x0, double xtol, double ftol, int ntol, out int iflag)
    {
        double fx0, deriv, deltax;

        iflag = 0;
        for (int n = 1; n <= ntol; n++)
        {
            fx0 = FF(x0);
            if (System.Math.Abs(fx0) < ftol)
            {
                goto L999;
            }
            deriv = FFDer(x0);

            if (deriv == 0.0)
            {
                goto L999;
            }
            deltax = fx0 / deriv;
            x0 = x0 - deltax;
            if (System.Math.Abs(deltax) < xtol)
            {
                goto L999;
            }
        }
    L999:
        iflag = 2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = Bench();
        return (result ? 100 : -1);
    }
}
}
