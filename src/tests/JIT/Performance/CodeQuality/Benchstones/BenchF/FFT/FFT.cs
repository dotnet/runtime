// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FFT benchmark adapted from a Fortran routine from the book
// Digital Signal Analysis, Samuel Stearns, Hayden Book Co.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class FFT
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 300000;
#endif

    private static readonly int s_points = 16;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Escape(object _) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double[] fr = new double[17];
        double[] fi = new double[17];

        int i;
        double t;

        for (int iter = 1; iter <= Iterations; iter++)
        {
            for (i = 1; i <= s_points; ++i)
            {
                t = ((double)0.375) * ((double)(i - 1));
                fr[i] = System.Math.Exp(-t) * System.Math.Sin(t);
                fi[i] = 0.0;
            }
            FastFourierT(fr, fi, s_points);
        }

        // Escape the results to live-out.
        Escape(fr);
        Escape(fi);

        return true;
    }

    private static void FastFourierT(double[] fr, double[] fi, int n)
    {
        int i, j, l, m;
        int istep, mr, nn;
        double a, el, tr, ti, wr, wi;

        mr = 0;
        nn = n - 1;
        m = 1;

        do
        {
            l = n;
            for (l = l / 2; ((mr + l) > nn); l = l / 2)
            {
            }
            // l <= n/2
            // mr <= (mr % l) + l ==> mr <= (l - 1) + l = 2l - 1
            // ==> mr <= n - 1
            mr = (mr % l) + l;

            if (mr > m)
            {
                // Accessing upto m + 1 ==> nn + 1 ==> n - 1 + 1 ==> n
                tr = fr[m + 1];
                // Accessing upto mr + 1 ==> n - 1 + 1 ==> n
                fr[m + 1] = fr[mr + 1];
                fr[mr + 1] = tr;
                ti = fi[m + 1];
                fi[m + 1] = fi[mr + 1];
                fi[mr + 1] = ti;
            }
            ++m;
        } while (m <= nn);

        for (l = 1; l < n; l = istep)
        {
            istep = 2 * l;

            el = ((double)l);
            m = 1;
            do
            {
                a = ((double)3.1415926535) * (((double)(1 - m)) / el);
                wr = System.Math.Cos(a);
                wi = System.Math.Sin(a);
                i = m;
                do
                {
                    // l can have a maximum value of 2^x where 2^x < n and 2^(x+1) = n, since n is even
                    // ==> istep <= 2^(x+1) ==> i can only take the value of m and m <= l
                    // Therefore, j <= l + l
                    // or j <= 2^x + 2^x = 2^(x+1) = n
                    // i.e. j <= n
                    j = i + l;

                    // Accessing upto j <= n, i <= n
                    tr = wr * fr[j] - wi * fi[j];
                    ti = wr * fi[j] + wi * fr[j];
                    fr[j] = fr[i] - tr;
                    fi[j] = fi[i] - ti;
                    fr[i] = fr[i] + tr;
                    fi[i] = fi[i] + ti;
                    i += istep;
                } while (i <= n);
                ++m;
            } while (m <= l);
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
