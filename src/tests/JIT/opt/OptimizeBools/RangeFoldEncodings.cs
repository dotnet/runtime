// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Cover the comparison forms enabled by modular range folding, including wraparound boundaries.
public class RangeFoldEncodings
{
    private struct FieldValue
    {
        public int Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ExcludedPair(int x)
    {
        // The excluded values are small, their wrapped complement is not.
        // ARM64-NOT: movn
        // ARM64-NOT: movz
        // X64-NOT: -3
        return x != 5 && x != 6;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SignedSecond(int length)
    {
        // The signed test of the interval starts it at the minimum value, where the constants are small.
        // ARM64-NOT: movz
        // ARM64-NOT: movk
        // X64-NOT: 0x7FFFFFF9
        return (uint)length < 257 || unchecked(length - 257) < 6 ? 42 : length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualityIntersection(int x) => x > 5 && x == 6;

    // This has two holes and must not be widened to exclude 6 as well.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SeparatedExclusions(int x) => x != 5 && x != 7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NegativeBounds(int x) => x >= -9 && x <= -2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool OverlappingRanges(int x) => x > 10 && x > 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SingleValue(int x) => x >= 10 && x <= 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EmptyRange(int x) => x >= 20 && x < 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FullRange(int x) => x < 20 || x >= 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AddedOffset(int x) => unchecked(x + 5) >= 10 && unchecked(x + 5) <= 20;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SubtractedOffset(int x) => unchecked(x - 5) >= -10 && unchecked(x - 5) <= 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool MixedSignedness(int x) => (uint)x < 100 && x >= 10;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FieldLocal(FieldValue x)
    {
        // ARM64-NOT: ccmp
        // X64-NOT: 100
        return x.Value >= 10 && x.Value <= 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SymbolicLowerBound(int x, ReadOnlySpan<int> span) => x > -1 && x < span.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SymbolicImpliedBound(int x, ReadOnlySpan<int> span) => x < 0 && x < span.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LongExcludedPair(long x) => x != long.MinValue && x != long.MinValue + 1;

    private static void Check(Func<int, bool> test, params (int Value, bool Expected)[] cases)
    {
        foreach (var item in cases)
        {
            Assert.Equal(item.Expected, test(item.Value));
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Check(ExcludedPair, (int.MinValue, true), (-1, true), (4, true), (5, false), (6, false),
            (7, true), (int.MaxValue, true));
        Check(EqualityIntersection, (int.MinValue, false), (5, false), (6, true), (7, false), (int.MaxValue, false));
        Check(SeparatedExclusions, (int.MinValue, true), (5, false), (6, true), (7, false), (int.MaxValue, true));
        Check(NegativeBounds, (int.MinValue, false), (-10, false), (-9, true), (-2, true),
            (-1, false), (0, false), (int.MaxValue, false));
        Check(OverlappingRanges, (int.MinValue, false), (10, false), (11, false), (100, false),
            (101, true), (int.MaxValue, true));
        Check(SingleValue, (int.MinValue, false), (9, false), (10, true), (11, false), (int.MaxValue, false));
        Check(EmptyRange, (int.MinValue, false), (9, false), (10, false), (19, false),
            (20, false), (int.MaxValue, false));
        Check(FullRange, (int.MinValue, true), (9, true), (10, true), (19, true), (20, true), (int.MaxValue, true));
        Check(AddedOffset, (int.MinValue, false), (4, false), (5, true), (15, true),
            (16, false), (int.MaxValue - 4, false), (int.MaxValue, false));
        Check(SubtractedOffset, (int.MinValue, false), (-6, false), (-5, true), (5, true),
            (6, false), (int.MaxValue, false));
        Check(MixedSignedness, (int.MinValue, false), (-1, false), (9, false), (10, true),
            (99, true), (100, false), (int.MaxValue, false));
        Check(x => FieldLocal(new FieldValue { Value = x }), (int.MinValue, false), (9, false),
            (10, true), (100, true), (101, false), (int.MaxValue, false));

        foreach (var item in new (int Value, int Expected)[]
        {
            (int.MinValue, int.MinValue), (int.MinValue + 256, int.MinValue + 256),
            (int.MinValue + 257, 42), (-1, 42), (0, 42), (256, 42), (257, 42),
            (262, 42), (263, 263), (int.MaxValue, int.MaxValue)
        })
        {
            Assert.Equal(item.Expected, SignedSecond(item.Value));
        }

        Span<int> span = stackalloc int[8];
        foreach (var item in new (int Value, bool InBounds, bool Negative)[]
        {
            (int.MinValue, false, true), (-1, false, true), (0, true, false),
            (7, true, false), (8, false, false), (int.MaxValue, false, false)
        })
        {
            Assert.Equal(item.InBounds, SymbolicLowerBound(item.Value, span));
            Assert.False(SymbolicLowerBound(item.Value, ReadOnlySpan<int>.Empty));
            Assert.Equal(item.Negative, SymbolicImpliedBound(item.Value, span));
            Assert.Equal(item.Negative, SymbolicImpliedBound(item.Value, ReadOnlySpan<int>.Empty));
        }

        Assert.False(LongExcludedPair(long.MinValue));
        Assert.False(LongExcludedPair(long.MinValue + 1));
        Assert.True(LongExcludedPair(long.MinValue + 2));
        Assert.True(LongExcludedPair(-1));
        Assert.True(LongExcludedPair(0));
        Assert.True(LongExcludedPair(long.MaxValue));
    }
}
