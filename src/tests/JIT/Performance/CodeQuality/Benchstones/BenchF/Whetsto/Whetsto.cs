// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// C# translation of Whetstone Double Precision Benchmark

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchF
{
public static class Whetsto
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 50000;
#endif

    private static int s_j, s_k, s_l;
    private static double s_t, s_t2;

    public static volatile int Volatile_out;

    private static void Escape(int n, int j, int k, double x1, double x2, double x3, double x4)
    {
        Volatile_out = n;
        Volatile_out = j;
        Volatile_out = k;
        Volatile_out = (int)x1;
        Volatile_out = (int)x2;
        Volatile_out = (int)x3;
        Volatile_out = (int)x4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double[] e1 = new double[4];
        double x1, x2, x3, x4, x, y, z, t1;
        int i, n1, n2, n3, n4, n6, n7, n8, n9, n10, n11;

        s_t = 0.499975;
        t1 = 0.50025;
        s_t2 = 2.0;
        n1 = 0 * Iterations;
        n2 = 12 * Iterations;
        n3 = 14 * Iterations;
        n4 = 345 * Iterations;
        n6 = 210 * Iterations;
        n7 = 32 * Iterations;
        n8 = 899 * Iterations;
        n9 = 616 * Iterations;
        n10 = 0 * Iterations;
        n11 = 93 * Iterations;
        x1 = 1.0;
        x2 = x3 = x4 = -1.0;

        for (i = 1; i <= n1; i += 1)
        {
            x1 = (x1 + x2 + x3 - x4) * s_t;
            x2 = (x1 + x2 - x3 - x4) * s_t;
            x3 = (x1 - x2 + x3 + x4) * s_t;
            x4 = (-x1 + x2 + x3 + x4) * s_t;
        }
        Escape(n1, n1, n1, x1, x2, x3, x4);

        /* MODULE 2:  array elements */
        e1[0] = 1.0;
        e1[1] = e1[2] = e1[3] = -1.0;
        for (i = 1; i <= n2; i += 1)
        {
            e1[0] = (e1[0] + e1[1] + e1[2] - e1[3]) * s_t;
            e1[1] = (e1[0] + e1[1] - e1[2] + e1[3]) * s_t;
            e1[2] = (e1[0] - e1[1] + e1[2] + e1[3]) * s_t;
            e1[3] = (-e1[0] + e1[1] + e1[2] + e1[3]) * s_t;
        }
        Escape(n2, n3, n2, e1[0], e1[1], e1[2], e1[3]);

        /* MODULE 3:  array as parameter */
        for (i = 1; i <= n3; i += 1)
        {
            PA(e1);
        }
        Escape(n3, n2, n2, e1[0], e1[1], e1[2], e1[3]);

        /* MODULE 4:  conditional jumps */
        s_j = 1;
        for (i = 1; i <= n4; i += 1)
        {
            if (s_j == 1)
            {
                s_j = 2;
            }
            else
            {
                s_j = 3;
            }
            if (s_j > 2)
            {
                s_j = 0;
            }
            else
            {
                s_j = 1;
            }
            if (s_j < 1)
            {
                s_j = 1;
            }
            else
            {
                s_j = 0;
            }
        }
        Escape(n4, s_j, s_j, x1, x2, x3, x4);

        /* MODULE 5:  omitted */
        /* MODULE 6:  integer Math */
        s_j = 1;
        s_k = 2;
        s_l = 3;
        for (i = 1; i <= n6; i += 1)
        {
            s_j = s_j * (s_k - s_j) * (s_l - s_k);
            s_k = s_l * s_k - (s_l - s_j) * s_k;
            s_l = (s_l - s_k) * (s_k + s_j);
            e1[s_l - 2] = s_j + s_k + s_l;
            e1[s_k - 2] = s_j * s_k * s_l;
        }
        Escape(n6, s_j, s_k, e1[0], e1[1], e1[2], e1[3]);

        /* MODULE 7:  trig. functions */
        x = y = 0.5;
        for (i = 1; i <= n7; i += 1)
        {
            x = s_t * System.Math.Atan(s_t2 * System.Math.Sin(x) * System.Math.Cos(x) / (System.Math.Cos(x + y) + System.Math.Cos(x - y) - 1.0));
            y = s_t * System.Math.Atan(s_t2 * System.Math.Sin(y) * System.Math.Cos(y) / (System.Math.Cos(x + y) + System.Math.Cos(x - y) - 1.0));
        }
        Escape(n7, s_j, s_k, x, x, y, y);

        /* MODULE 8:  procedure calls */
        x = y = z = 1.0;
        for (i = 1; i <= n8; i += 1)
        {
            P3(x, y, out z);
        }
        Escape(n8, s_j, s_k, x, y, z, z);

        /* MODULE9:  array references */
        s_j = 1;
        s_k = 2;
        s_l = 3;
        e1[0] = 1.0;
        e1[1] = 2.0;
        e1[2] = 3.0;
        for (i = 1; i <= n9; i += 1)
        {
            P0(e1);
        }
        Escape(n9, s_j, s_k, e1[0], e1[1], e1[2], e1[3]);

        /* MODULE10:  integer System.Math */
        s_j = 2;
        s_k = 3;
        for (i = 1; i <= n10; i += 1)
        {
            s_j = s_j + s_k;
            s_k = s_j + s_k;
            s_j = s_k - s_j;
            s_k = s_k - s_j - s_j;
        }
        Escape(n10, s_j, s_k, x1, x2, x3, x4);

        /* MODULE11:  standard functions */
        x = 0.75;
        for (i = 1; i <= n11; i += 1)
        {
            x = System.Math.Sqrt(System.Math.Exp(System.Math.Log(x) / t1));
        }
        Escape(n11, s_j, s_k, x, x, x, x);

        return true;
    }

    private static void PA(double[] e)
    {
        int j;
        j = 0;
    lab:
        e[0] = (e[0] + e[1] + e[2] - e[3]) * s_t;
        e[1] = (e[0] + e[1] - e[2] + e[3]) * s_t;
        e[2] = (e[0] - e[1] + e[2] + e[3]) * s_t;
        e[3] = (-e[0] + e[1] + e[2] + e[3]) / s_t2;
        j += 1;
        if (j < 6)
        {
            goto lab;
        }
    }

    private static void P3(double x, double y, out double z)
    {
        x = s_t * (x + y);
        y = s_t * (x + y);
        z = (x + y) / s_t2;
    }

    private static void P0(double[] e1)
    {
        e1[s_j] = e1[s_k];
        e1[s_k] = e1[s_l];
        e1[s_l] = e1[s_j];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = Bench();
        return (result ? 100 : -1);
    }
}
}
