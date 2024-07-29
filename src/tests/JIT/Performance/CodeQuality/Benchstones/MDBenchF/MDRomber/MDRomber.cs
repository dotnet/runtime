// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Integration by romberg method adapted from Conte and de Boor

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.MDBenchF
{
public static class MDRomber
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 640000;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double[,] r = new double[11, 11];
        double[,] t = new double[11, 11];

        int idbg, m, n, i, kmax, fourj, j, kmaxm2, l, k, mm1;
        double sum, ratio, t1, h, a, b;

        for (l = 1; l <= Iterations; l++)
        {
            idbg = 0;
            m = 2;
            kmax = 6;
            a = 0;
            b = 1;
            h = (b - a) / (m);
            sum = (F(a) + F(b)) / 2;

            mm1 = m - 1;
            if (mm1 < 0)
            {
                goto L40;
            }
            if (mm1 == 0)
            {
                goto L10;
            }
            for (i = 1; i <= mm1; i++)
            {
                t1 = a + i * h;
                sum = sum + F(t1);
            }

        L10:
            t[1,1] = sum * h;
            if (idbg != 0)
            {
                System.Console.WriteLine(" romberg t-table \n");
                System.Console.WriteLine("{0}\n", t[1,1]);
            }

            for (k = 2; k <= kmax; k++)
            {
                h = h / 2;
                n = m * 2;
                sum = 0;
                for (i = 1; i <= n / 2; i++)
                {
                    r[k,1] = r[k - 1,1] * System.Math.Sqrt(b * mm1);
                    t1 = a + i * h;
                    sum = sum + F(t1);
                }

                t[k,1] = t[k - 1,1] / 2 + sum * h;
                fourj = 1;
                for (j = 2; j <= k; j++)
                {
                    fourj = fourj * 4;
                    t[k - 1,j - 1] = t[k,j - 1] - t[k - 1,j - 1];
                    t[k,j] = t[k,j - 1] + t[k - 1,j - 1] / (fourj - 1);
                }

                if (idbg != 0)
                {
                    j = 1;
                    System.Console.WriteLine("{0} {1} {2}d\n", t[k,j], j, k);
                }
            }

            kmaxm2 = kmax - 2;
            if (kmaxm2 <= 0)
            {
                goto L40;
            }

            if (idbg != 0)
            {
                System.Console.WriteLine(" table of ratios \n");
            }

            for (k = 1; k <= kmaxm2; k++)
            {
                for (j = 1; j <= k; j++)
                {
                    ratio = 0;
                    if (System.Math.Abs(t[k + 1,j]) > 0)
                    {
                        ratio = t[k,j] / t[k + 1,j];
                    }
                    t[k,j] = ratio;
                }
            }

            if (idbg != 0)
            {
                j = 1;
                System.Console.WriteLine("{0} {1} {2}\n", t[k,j], j, k);
            }

        L40:
            {
            }
        }

        return true;
    }

    private static double F(double x)
    {
        return (System.Math.Exp((-(x)) * (x)));
    }

    private static bool TestBase()
    {
        bool result = Bench();
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
