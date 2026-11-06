// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_133585;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133585
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int[] array, int index, int dividend, int divisor, int count)
    {
        int result = 0;

        for (int i = 0; i < count; i++)
        {
            result += array[index];
            result += dividend / divisor;
        }

        return result;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Test(new int[1], 5, 1, 0, 5));
    }

    [Theory]
    [InlineData(1, 0, false)]
    [InlineData(1, 0, true)]
    [InlineData(int.MinValue, -1, false)]
    [InlineData(int.MinValue, -1, true)]
    public static void UnprofitableThrowingCandidate(int dividend, int divisor, bool sameStatement)
    {
        Assert.Throws<NullReferenceException>(() => sameStatement
            ? TestArrayLengthInStatement(null, dividend, divisor, 2, 10)
            : TestArrayLength(null, dividend, divisor, 2, 10));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(int value) { }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestArrayLength(int[] array, int dividend, int divisor, int adjustment, int count)
    {
        int result = 0;
        for (int i = 0; i < count; i++)
        {
            result += array.Length;
            result += dividend / divisor + adjustment;
            Consume(result + i);
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestArrayLengthInStatement(int[] array, int dividend, int divisor, int adjustment, int count)
    {
        int result = 0;
        for (int i = 0; i < count; i++)
        {
            result += array.Length + (dividend / divisor + adjustment + i);
            Consume(result + i);
        }
        return result;
    }
}
