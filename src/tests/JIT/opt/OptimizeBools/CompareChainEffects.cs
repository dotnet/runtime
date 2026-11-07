// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class CompareChainEffects
{
    private static int s_trace;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CseLength(int[] array, int start, int count)
    {
        // ARM64: ccmp
        if ((uint)start > (uint)array.Length || (uint)count > (uint)(array.Length - start))
            return -1;
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Update(int value)
    {
        s_trace = s_trace * 10 + 1;
        if (value < 0)
            throw new InvalidOperationException();
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FirstEffect(int value)
    {
        // ARM64: ccmp
        if (s_trace < Update(value) || value > 5)
            return 1;
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SecondEffect(int value) => value == 0 || Update(value) > 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckedSecond(int value) => value == int.MaxValue || checked(value + 1) > 0;

    [Fact]
    public static void TestEntryPoint()
    {
        int[] array = new int[8];
        int[] values = { int.MinValue, -1, 0, 1, 7, 8, 9, int.MaxValue };
        foreach (int start in values)
            foreach (int count in values)
                Assert.Equal(start >= 0 && count >= 0 && (long)start + count <= array.Length ? count : -1,
                    CseLength(array, start, count));
        Assert.Throws<NullReferenceException>(() => CseLength(null, 0, 0));

        foreach (int initial in new[] { 0, 1, 2, 7 })
            foreach (int value in new[] { 0, 1, 3, 6 })
            {
                s_trace = initial;
                Assert.Equal(initial < value || value > 5 ? 1 : 2, FirstEffect(value));
                Assert.Equal(initial * 10 + 1, s_trace);
            }
        s_trace = 0;
        Assert.Throws<InvalidOperationException>(() => FirstEffect(-1));
        Assert.Equal(1, s_trace);
        s_trace = 0;
        Assert.True(SecondEffect(0));
        Assert.Equal(0, s_trace);
        Assert.Throws<InvalidOperationException>(() => SecondEffect(-1));
        Assert.Equal(1, s_trace);
        Assert.True(CheckedSecond(int.MaxValue));
        Assert.False(CheckedSecond(int.MinValue));
    }
}
