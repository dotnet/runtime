// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DownCounted
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static bool ArrayProblem(int[] a, int n)
    {
        bool result = false;
        for (int i = n; i > 0; i--)
        {
            a[i] = 44;
            result |= (i == n);
        }
        return result;
    }

    [Fact]
    public static int ArrayTest()
    {
        int[] a = new int[100];
        int result = -1;
        try
        {
            bool hasProblem = ArrayProblem(a, 100);
            Console.WriteLine($"failed, has problem={hasProblem}");
        }
        catch (IndexOutOfRangeException e)
        {
            Console.WriteLine("passed");
            result = 100;
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static bool SpanProblem(Span<int> a, int n)
    {
        bool result = false;
        for (int i = n; i > 0; i--)
        {
            a[i] = 44;
            result |= (i == n);
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int UnsignedArrayGT(int[] a, uint n, uint limit)
    {
        int sum = 0;
        for (uint i = n; i > limit; i -= 3)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int UnsignedArrayGE(int[] a, uint n, uint limit)
    {
        int sum = 0;
        for (uint i = n; i >= limit; i -= 3)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int UnsignedSpanGT(Span<int> a, uint n, uint limit)
    {
        int sum = 0;
        for (uint i = n; i > limit; i -= 3)
        {
            sum += a[(int)i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int UnsignedSpanGE(Span<int> a, uint n, uint limit)
    {
        int sum = 0;
        for (uint i = n; i >= limit; i -= 3)
        {
            sum += a[(int)i];
        }
        return sum;
    }

    [Fact]
    public static int SpanTest()
    {
        int[] a = new int[100];
        int result = -1;
        try
        {
            bool hasProblem = SpanProblem(a, 100);
            Console.WriteLine($"failed, has problem={hasProblem}");
        }
        catch (IndexOutOfRangeException e)
        {
            Console.WriteLine("passed");
            result = 100;
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int UnsignedArrayGTConstSafe(int[] a)
    {
        // init=7, limit=1, stride=3: remainder r=0, so a GT test never
        // visits "limit" itself -- always safe to clone.
        int sum = 0;
        for (uint i = 7; i > 1; i -= 3)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int UnsignedArrayGEConstSafe(int[] a)
    {
        // init=9, limit=3, stride=3: remainder r=0 and limit >= stride, so
        // the last visited value (limit + r == limit) can't underflow on
        // the next decrement.
        int sum = 0;
        for (uint i = 9; i >= 3; i -= 3)
        {
            sum += a[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int UnsignedArrayGEConstUnsafe(int[] a)
    {
        // init=7, limit=1, stride=3: remainder r=0 but limit < stride, so
        // the loop visits "limit" (1) via GE and then underflows on the
        // next decrement -- must not be proven safe.
        int sum = 0;
        for (uint i = 7; i >= 1; i -= 3)
        {
            sum += a[i];
        }
        return sum;
    }

    [Fact]
    public static void UnsignedUnderflowTest()
    {
        int[] a = { 0, 1, 2, 3, 4, 5, 6, 7 };

        Assert.Throws<IndexOutOfRangeException>(() => UnsignedArrayGT(a, 7, 0));
        Assert.Throws<IndexOutOfRangeException>(() => UnsignedArrayGE(a, 7, 1));
        Assert.Throws<IndexOutOfRangeException>(() => UnsignedSpanGT(a, 7, 0));
        Assert.Throws<IndexOutOfRangeException>(() => UnsignedSpanGE(a, 7, 1));

        Assert.Equal(11, UnsignedArrayGT(a, 7, 1));
        Assert.Equal(11, UnsignedArrayGE(a, 7, 2));
        Assert.Equal(11, UnsignedSpanGT(a, 7, 1));
        Assert.Equal(11, UnsignedSpanGE(a, 7, 2));
    }

    [Fact]
    public static void UnsignedConstBoundsTest()
    {
        int[] a = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        Assert.Equal(11, UnsignedArrayGTConstSafe(a));
        Assert.Equal(18, UnsignedArrayGEConstSafe(a));
        Assert.Throws<IndexOutOfRangeException>(() => UnsignedArrayGEConstUnsafe(a));
    }
}
