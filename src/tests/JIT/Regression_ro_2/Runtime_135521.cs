// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135521
{
    private sealed class IntItem
    {
        public int A;
        public int B;
    }

    private sealed class LongItem
    {
        public long A;
        public long B;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static IntItem[] FillInt(int n, int a, int start)
    {
        IntItem[] items = new IntItem[n];
        for (int k = start; k < start + n; k++)
        {
            items[k - start] = new IntItem { A = a, B = k % 2 };
        }

        return items;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static LongItem[] FillLong(int n, long a, long start)
    {
        LongItem[] items = new LongItem[n];
        for (long k = start; k < start + n; k++)
        {
            items[k - start] = new LongItem { A = a, B = k % 2 };
        }

        return items;
    }

    [Theory]
    [InlineData(-11)]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue - 11)]
    public static void TestEntryPoint(int start)
    {
        for (int a = 0; a < 100; a++)
        {
            IntItem[] ints = FillInt(11, a, start);
            LongItem[] longs = FillLong(11, a, start);
            for (int k = 0; k < ints.Length; k++)
            {
                int dividend = k + start;
                int expected = dividend < 0 ? -(dividend & 1) : dividend & 1;
                Assert.Equal(a, ints[k].A);
                Assert.Equal(expected, ints[k].B);
                Assert.Equal((long)a, longs[k].A);
                Assert.Equal((long)expected, longs[k].B);
            }
        }
    }
}
