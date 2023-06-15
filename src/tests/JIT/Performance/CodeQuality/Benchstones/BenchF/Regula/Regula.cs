// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The modified regula-falsi routine adapted from Conte and De Boor

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class Regula
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 4000000;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Escape(object _) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double error, fxi;
        double a, b, xi;
        int idbg, iflag;

        iflag = 0;
        idbg = 0;
        xi = 0;
        error = 0.0;
        fxi = 0.0;

        for (int i = 1; i <= Iterations; i++)
        {
            a = 1.0;
            b = 2.0;
            Inner(ref a, ref b, 0.0000001, 0.0000000001, 30, out iflag);
            if (iflag > 2)
            {
                goto L999;
            }

            xi = (a + b) / 2.0;
            error = System.Math.Abs(b - a) / 2.0;
            fxi = FG(xi);

            if (idbg != 0)
            {
                System.Console.WriteLine(" the root is  {0:E}", xi);
                System.Console.WriteLine(" plus/minus {0}\n", error);
                System.Console.WriteLine(" fg(root):= {0:E}\n", fxi);
            }

        L999:
            {
            }
        }

        // Escape iflag, xi, error, and fxi so that they appear live
        Escape(iflag);
        Escape(xi);
        Escape(error);
        Escape(fxi);

        return true;
    }

    private static double FG(double x)
    {
        return (-1.0 - (x * (1.0 - (x * x))));
    }

    private static void Inner(ref double a, ref double b, double xtol, double ftol, int ntol, out int iflag)
    {
        double signfa, prevfw, fa, fb, fw, w;

        iflag = 0;
        fa = FG(a);
        if (fa < 0.0)
        {
            signfa = -1.0;
        }
        else
        {
            signfa = 1.0;
        }

        fb = FG(b);
        if (signfa * fb <= 0.0)
        {
            goto L5;
        }

        iflag = 3;
        goto L99;

    L5:
        w = a;
        fw = fa;
        for (int i = 1; i <= ntol; i++)
        {
            if (System.Math.Abs(b - a) / 2.0 <= xtol)
            {
                goto L99;
            }
            if (System.Math.Abs(fw) > ftol)
            {
                goto L9;
            }

            a = w;
            b = w;
            iflag = 1;
            goto L99;

        L9:
            w = (fa * b - fb * a) / (fa - fb);
            if (fw < 0.0)
            {
                prevfw = -1.0;
            }
            else
            {
                prevfw = 1.0;
            }
            fw = FG(w);

            if (signfa * fw < 0.0)
            {
                goto L10;
            }
            a = w;
            fa = fw;
            if (fw * prevfw > 0.0)
            {
                fb = fb / 2.0;
            }
            goto L20;

        L10:
            b = w;
            fb = fw;
            if (fw * prevfw > 0.0)
            {
                fa = fa / 2.0;
            }

        L20:
            {
            }
        }

        iflag = 2;
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
