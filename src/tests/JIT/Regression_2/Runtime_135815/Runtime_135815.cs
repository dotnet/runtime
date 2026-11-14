// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_135815
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(3, GetElementOptimizeBools(5, 1));
        Assert.Equal(21, GetElementOptimizeBools(5, 7));
        Assert.Equal(2, GetElementOptimizeBools(5, int.MinValue));

        Assert.Equal(2, WithElementOptimizeBools(5, 0));
        Assert.Equal(3, WithElementOptimizeBools(5, 1));
        Assert.Equal(21, WithElementOptimizeBools(5, 7));
        Assert.Equal(2, WithElementOptimizeBools(5, int.MinValue));

        Vector128<int> v = Vector128.Create(1, 2, 3, 4);

        Assert.Equal(3, GetElementIfConversion(v, 2));
        Assert.Equal(0, GetElementIfConversion(v, int.MaxValue));

        Assert.Equal(9, WithElementIfConversion(v, 0));
        Assert.Equal(1, WithElementIfConversion(v, 2));
        Assert.Equal(0, WithElementIfConversion(v, int.MaxValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetElementOptimizeBools(int a, int x)
    {
        Vector128<int> v = Vector128.Create(a, a + 1, a + 2, a + 3);
        int s = v.GetElement(1) + v.GetElement(2) + v.GetElement(3);
        if (x >= 4)
        {
            return s;
        }

        if (x < 0 || v.GetElement(x) < 0)
        {
            return 2;
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int WithElementOptimizeBools(int a, int x)
    {
        Vector128<int> v = Vector128.Create(a, a + 1, a + 2, a + 3);
        int s = v.GetElement(1) + v.GetElement(2) + v.GetElement(3);
        if (x >= 4)
        {
            return s;
        }

        if (x < 0 || v.WithElement(x, -1).ToScalar() < 0)
        {
            return 2;
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetElementIfConversion(Vector128<int> v, int x)
    {
        if (x < 0)
        {
            return -1;
        }

        int r = 0;
        if (x < 4)
        {
            r = v.GetElement(x);
        }

        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int WithElementIfConversion(Vector128<int> v, int x)
    {
        if (x < 0)
        {
            return -1;
        }

        int r = 0;
        if (x < 4)
        {
            r = v.WithElement(x, 9).ToScalar();
        }

        return r;
    }
}
