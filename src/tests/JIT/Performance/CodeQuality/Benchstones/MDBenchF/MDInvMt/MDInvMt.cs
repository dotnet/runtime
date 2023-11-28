// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Solution of linear algebraic equations and matrix inversion.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.MDBenchF
{
public static class MDInvMt
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 80;
#endif

    private const int MatSize = Iterations;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double[,] t = new double[MatSize + 1, (MatSize + 1) * 2];

        double det, detinv, ber, p;
        int n, i, j;

        n = MatSize;
        for (i = 1; i <= n; i++)
        {
            for (j = 1; j <= n; j++)
            {
                if (i == j)
                {
                    t[i,j] = 2.0001;
                    t[i,n + 1 + j] = 1.0;
                }
                else
                {
                    t[i,j] = 1.0001;
                    t[i,n + 1 + j] = 0.0;
                }
            }
            t[i,n + 1] = System.Math.Sqrt((float)i);
        }

        Inner(t, out det, ref n);

        for (i = 1; i <= n; i++)
        {
            for (j = 1; j <= n; j++)
            {
                p = t[i,j];
                t[i,j] = t[i,n + 1 + j];
                t[i,n + 1 + j] = p;
            }
        }

        Inner(t, out detinv, ref n);

        ber = 0.0;
        for (i = 1; i <= n; i++)
        {
            ber = ber + System.Math.Abs(System.Math.Sqrt((double)i) - t[i,n + 1]);
        }

        return true;
    }

    private static void Inner(double[,] t, out double det, ref int n)
    {
        double tik, tkk;

        det = 1.0;
        for (int k = 1; k <= n; k++)
        {
            tkk = t[k,k];
            det = det * tkk;

            for (int j = 1; j <= (2 * n + 1); j++)
            {
                t[k,j] = t[k,j] / tkk;
            }

            for (int i = 1; i <= n; i++)
            {
                if (i != k)
                {
                    tik = t[i,k];
                    for (int j = 1; j <= (2 * n + 1); j++)
                    {
                        t[i,j] = t[i,j] - t[k,j] * tik;
                    }
                }
            }
        }
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
