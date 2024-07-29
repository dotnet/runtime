// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.MDBenchF
{
public static class MDSqMtx
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 4000;
#endif

    private const int MatrixSize = 40;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        double[,] a = new double[41, 41];
        double[,] c = new double[41, 41];

        int i, j;

        for (i = 1; i <= MatrixSize; i++)
        {
            for (j = 1; j <= MatrixSize; j++)
            {
                a[i,j] = i + j;
            }
        }

        for (i = 1; i <= Iterations; i++)
        {
            Inner(a, c, MatrixSize);
        }

        if (c[1,1] == 23820.0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static void Inner(double[,] a, double[,] c, int n)
    {
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                c[i,j] = 0.0;
                for (int k = 1; k <= n; k++)
                {
                    c[i,j] = c[i,j] + a[i,k] * a[k,j];
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
