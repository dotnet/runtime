// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133963
{
    private static int s_index;

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    public static void TestEntryPoint(bool readOnly, int testCase)
    {
        int[] data = { 10, 20 };
        Func<int> test = () => readOnly ? TestReadOnlySpan(data, testCase) : TestSpan(data, testCase);

        s_index = 0;
        if (testCase == 2)
        {
            Assert.Throws<DivideByZeroException>(() => test());
        }
        else
        {
            Assert.Equal(20, test());
        }
        Assert.Equal(testCase == 1 ? 2 : 1, s_index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref Span<int> GetSpan(ref Span<int> span)
    {
        s_index++;
        return ref span;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref ReadOnlySpan<int> GetReadOnlySpan(ref ReadOnlySpan<int> span)
    {
        s_index++;
        return ref span;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetIndex() => s_index++;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestSpan(int[] data, int testCase)
    {
        Span<int> span = data;
        return testCase switch
        {
            0 => GetSpan(ref span)[s_index],
            1 => GetSpan(ref span)[GetIndex()],
            2 => GetSpan(ref span)[1 / (s_index - 1)],
            _ => s_index + GetSpan(ref span)[s_index],
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int TestReadOnlySpan(int[] data, int testCase)
    {
        ReadOnlySpan<int> span = data;
        return testCase switch
        {
            0 => GetReadOnlySpan(ref span)[s_index],
            1 => GetReadOnlySpan(ref span)[GetIndex()],
            2 => GetReadOnlySpan(ref span)[1 / (s_index - 1)],
            _ => s_index + GetReadOnlySpan(ref span)[s_index],
        };
    }
}
