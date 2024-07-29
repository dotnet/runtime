// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Integration by Simpson's rule adapted from Conte and de Boor

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class Simpsn
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 90000;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double a, b, x, s, c, h, hov2, half, t1;
        int idbg, n, nm1;

        s = 0;
        idbg = 0;
        if (idbg != 0)
        {
            System.Console.WriteLine("simpsons rule\n");
        }

        for (int j = 1; j <= Iterations; j++)
        {
            a = 0;
            b = 1;
            c = 4;
            n = 100;
            h = (b - a) / n;
            hov2 = h / System.Math.Sqrt(c);
            s = 0;
            t1 = a + hov2;
            half = F(t1);
            nm1 = n - 1;
            for (int i = 1; i <= nm1; i++)
            {
                x = a + i * h;
                s = s + F(x);
                t1 = x + hov2;
                half = half + F(t1);
                s = (h / 6) * (F(a) + 4 * half + 2 * s + F(b));
                if (idbg != 0)
                {
                    System.Console.WriteLine(" integral from a = {0} to b = {1} for n = {2} is {3}\n", a, b, n, s);
                }
            }
        }

        return true;
    }

    private static double F(double x)
    {
        return (System.Math.Exp((-(x)) * 2));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = Bench();
        return (result ? 100 : -1);
    }
}
}
