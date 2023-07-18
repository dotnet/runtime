// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.MDBenchI
{
public static class MDAddArray2
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 50;
#endif

    private const int Dim = 200;

    private static
    void BenchInner1(int[,] a, ref int nn)
    {
        int n;
        int l, m;
        n = nn;
        for (int i = 1; i <= n; i++)
        {
            for (int j = (i + 1); j <= n; j++)
            {
                for (int k = 1; k <= n; k++)
                {
                    l = a[i,k];
                    m = a[j,k];
                    unchecked
                    {
                        a[j,k] = l + m;
                    }
                }
            }
        }
    }

    private static
    void BenchInner2(int[,] a, ref int nn)
    {
        int n;
        int l, m;
        n = nn;
        for (int i = 1; i <= n; i++)
        {
            for (int j = (i + 1); j <= n; j++)
            {
                for (int k = 1; k <= n; k++)
                {
                    l = a[k,i];
                    m = a[k,j];
                    unchecked
                    {
                        a[k,j] = l + m;
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench(int[,] a)
    {
        int n = Dim;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                a[i,j] = i + j;
            }
        }

        BenchInner1(a, ref n);
        n = Dim;
        BenchInner2(a, ref n);

        return true;
    }

    private static bool TestBase()
    {
        int[,] array = new int[Dim + 1, Dim + 1];
        bool result = true;
        for (int i = 1; i <= Iterations; i++)
        {
            result &= Bench(array);
        }
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
