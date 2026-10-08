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


}
    
