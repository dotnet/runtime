// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ModularRanges
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Positive(int x)
    {
        // ARM64: cmp
        // ARM64-NOT: cmp
        if ((uint)x >= 10 && (uint)(x - 10) >= 20) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Negative(int x)
    {
        if (x >= -10 && unchecked(x + 10) >= -10) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Empty(int x)
    {
        // ARM64-NOT: cmp
        if ((uint)x >= unchecked((uint)-10) && (uint)unchecked(x + 10) >= unchecked((uint)-10)) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Offsets(int x)
    {
        // ARM64: cmp
        // ARM64-NOT: cmp
        if ((uint)unchecked(x + 5) <= 10 && (uint)unchecked(x - 3) <= 20) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Multiple(int x)
    {
        if (x >= -10 && (uint)x >= 30) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Singleton(int x)
    {
        if (x >= 10 && x <= 10) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Checked(int x)
    {
        if (x >= -10 && checked(x + 10) >= -10) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Wide(long x)
    {
        if ((ulong)x >= 10 && (ulong)unchecked(x - 10) >= 20) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long WideNegative(long x)
    {
        if (x >= -10 && unchecked(x + 10) >= -10) return x;
        return ~x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Symbolic(int x, ReadOnlySpan<int> span) => x >= 0 && x < span.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SymbolicInclusive(int x, ReadOnlySpan<int> span) => x >= 0 && span.Length >= x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SymbolicOffset(int x, ReadOnlySpan<int> span)
    {
        // ARM64: cmp
        // ARM64-NOT: cmp
        return unchecked(x + int.MinValue) < 0 && x < span.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnknownBound(int x, int bound) => x >= 0 && x <= bound;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ThrowingBound(int x, int[] array) => x >= 0 && x < array.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OrderedBound(int x, int[] array) => x >= 0 && x < array[x];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SymbolicFirstEffect(int x, ReadOnlySpan<int> span) => (x = Observe(x)) >= 0 && x < span.Length;

    private static int s_effects;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Observe(int x)
    {
        s_effects++;
        if (x == int.MinValue) throw new InvalidOperationException();
        return unchecked(x + 1);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FirstEffect(int x)
    {
        if ((uint)(x = Observe(x)) >= unchecked((uint)-10) &&
            (uint)unchecked(x + 10) >= unchecked((uint)-10)) return x;
        return ~x;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SecondEffect(int x)
    {
        if (x >= -10 && Observe(x) >= -10) return x;
        return ~x;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static int Reference(int x, int kind)
    {
        bool result = kind switch
        {
            0 => (uint)x >= 10 && (uint)unchecked(x - 10) >= 20,
            1 => x >= -10 && unchecked(x + 10) >= -10,
            2 => (uint)x >= unchecked((uint)-10) && (uint)unchecked(x + 10) >= unchecked((uint)-10),
            3 => (uint)unchecked(x + 5) <= 10 && (uint)unchecked(x - 3) <= 20,
            4 => x >= -10 && (uint)x >= 30,
            _ => x >= 10 && x <= 10,
        };
        return result ? x : ~x;
    }

    private static void Check(int x)
    {
        Assert.Equal(Reference(x, 0), Positive(x));
        Assert.Equal(Reference(x, 1), Negative(x));
        Assert.Equal(Reference(x, 2), Empty(x));
        Assert.Equal(Reference(x, 3), Offsets(x));
        Assert.Equal(Reference(x, 4), Multiple(x));
        Assert.Equal(Reference(x, 5), Singleton(x));
    }

    [Fact]
    public static void TestEntryPoint()
    {
        foreach (int x in new[] { int.MinValue, int.MinValue + 10, -31, -20, -11, -10, -1, 0, 1, 3, 5, 10, 29, 30, 31, int.MaxValue - 10, int.MaxValue }) Check(x);
        Random random = new Random(122679);
        for (int i = 0; i < 10000; i++) Check((int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1));
        foreach (long x in new[] { long.MinValue, long.MinValue + 10, -11L, -10, -1, 0, 1, 29, 30, long.MaxValue - 10, long.MaxValue })
        {
            bool positive = (ulong)x >= 30;
            bool negative = x >= -10 && x <= long.MaxValue - 10;
            Assert.Equal(positive ? x : ~x, Wide(x));
            Assert.Equal(negative ? x : ~x, WideNegative(x));
        }
        foreach (int length in new[] { 0, 1, 10, 31 })
        {
            ReadOnlySpan<int> span = new int[length];
            foreach (int x in new[] { int.MinValue, -1, 0, 1, length - 1, length, int.MaxValue })
            {
                bool expected = x >= 0 && x < length;
                Assert.Equal(expected, Symbolic(x, span));
                Assert.Equal(expected, SymbolicOffset(x, span));
                Assert.Equal(x >= 0 && x <= length, SymbolicInclusive(x, span));
            }
        }
        foreach (int bound in new[] { int.MinValue, -1, 0, int.MaxValue })
            foreach (int x in new[] { int.MinValue, -1, 0, 1, int.MaxValue })
                Assert.Equal(x >= 0 && x <= bound, UnknownBound(x, bound));
        Assert.False(ThrowingBound(-1, null));
        Assert.Throws<NullReferenceException>(() => ThrowingBound(0, null));
        Assert.False(OrderedBound(-1, Array.Empty<int>()));
        Assert.Throws<IndexOutOfRangeException>(() => OrderedBound(0, Array.Empty<int>()));
        s_effects = 0;
        Assert.True(SymbolicFirstEffect(-1, new int[1]));
        Assert.False(SymbolicFirstEffect(-2, new int[1]));
        Assert.Equal(2, s_effects);
        Assert.Throws<OverflowException>(() => Checked(int.MaxValue));
        Assert.Equal(0, Checked(0));
        s_effects = 0;
        Assert.Equal(~(-1), FirstEffect(-2));
        Assert.Equal(1, s_effects);
        Assert.Throws<InvalidOperationException>(() => FirstEffect(int.MinValue));
        Assert.Equal(2, s_effects);
        s_effects = 0;
        Assert.Equal(~(-20), SecondEffect(-20));
        Assert.Equal(0, s_effects);
        Assert.Equal(0, SecondEffect(0));
        Assert.Equal(1, s_effects);
    }
}
