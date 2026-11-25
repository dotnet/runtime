// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137674
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(ReadOnlySpan<int> span, ReadOnlySpan<int> repl)
    {
        int i = 0;
        int sum = 0;
        while (true)
        {
            if (span.Length - i < 0)
            {
                sum += 100;
            }

            if (i >= span.Length)
            {
                return sum;
            }

            sum += span[i];
            i++;
            span = repl;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestArray(int[] array, int[] repl)
    {
        int i = 0;
        int sum = 0;
        while (true)
        {
            if (array.Length - i < 0)
            {
                sum += 100;
            }

            if (i >= array.Length)
            {
                return sum;
            }

            sum += array[i];
            i++;
            array = repl;
        }
    }

    private static int[] s_array;
    private static int[] s_repl;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestStatic()
    {
        int i = 0;
        int sum = 0;
        while (true)
        {
            int[] array = s_array;
            if (array.Length - i < 0)
            {
                sum += 100;
            }

            if (i >= array.Length)
            {
                return sum;
            }

            sum += array[i];
            i++;
            s_array = s_repl;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(105, Test(new int[] { 5 }, new int[0]));
        Assert.Equal(105, TestArray(new int[] { 5 }, new int[0]));
        Assert.Equal(5, TestArray(new int[] { 5 }, new int[1]));
        Assert.Equal(0, TestArray(new int[0], new int[1]));

        s_array = new int[] { 5 };
        s_repl = new int[0];
        Assert.Equal(105, TestStatic());
    }
}
