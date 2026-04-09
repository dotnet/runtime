// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthEndsWith()
        {
            int[] a = new int[3];

            Assert.True(new ReadOnlySpan<int>(a, 1, 0).EndsWith(new ReadOnlySpan<int>(a, 2, 0)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.True(new ReadOnlySpan<int>(a, 1, 0).EndsWith(new ReadOnlySpan<int>(a, 2, 0), comparer)));
        }

        [Fact]
        public static void SameSpanEndsWith()
        {
            int[] a = { 4, 5, 6 };
            Assert.True(new ReadOnlySpan<int>(a).EndsWith(new ReadOnlySpan<int>(a)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.True(new ReadOnlySpan<int>(a).EndsWith(new ReadOnlySpan<int>(a), comparer)));
            Assert.False(new ReadOnlySpan<int>(a).EndsWith(new ReadOnlySpan<int>(a), GetFalseEqualityComparer<int>()));
        }

        [Fact]
        public static void LengthMismatchEndsWith()
        {
            int[] a = { 4, 5, 6 };
            Assert.False(new ReadOnlySpan<int>(a, 0, 2).EndsWith(new ReadOnlySpan<int>(a, 0, 3)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.False(new ReadOnlySpan<int>(a, 0, 2).EndsWith(new ReadOnlySpan<int>(a, 0, 3), comparer)));
        }

        [Fact]
        public static void EndsWithMatch()
        {
            int[] a = { 4, 5, 6 };
            Assert.True(new ReadOnlySpan<int>(a, 0, 3).EndsWith(new ReadOnlySpan<int>(a, 1, 2)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.True(new ReadOnlySpan<int>(a, 0, 3).EndsWith(new ReadOnlySpan<int>(a, 1, 2), comparer)));
            Assert.False(new ReadOnlySpan<int>(a, 0, 3).EndsWith(new ReadOnlySpan<int>(a, 1, 2), GetFalseEqualityComparer<int>()));
        }

        [Fact]
        public static void EndsWithMatchDifferentSpans()
        {
            int[] a = { 4, 5, 6 };
            int[] b = { 4, 5, 6 };
            Assert.True(new ReadOnlySpan<int>(a, 0, 3).EndsWith(new ReadOnlySpan<int>(b, 0, 3)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.True(new ReadOnlySpan<int>(a, 0, 3).EndsWith(new ReadOnlySpan<int>(b, 0, 3), comparer)));
            Assert.False(new ReadOnlySpan<int>(a, 0, 3).EndsWith(new ReadOnlySpan<int>(b, 0, 3), GetFalseEqualityComparer<int>()));
        }

        [Fact]
        public static void OnEndsWithOfEqualSpansMakeSureEveryElementIsCompared()
        {
            for (int length = 0; length < 100; length++)
            {
                TIntLog log = new TIntLog();

                TInt[] first = new TInt[length];
                TInt[] second = new TInt[length];
                for (int i = 0; i < length; i++)
                {
                    first[i] = second[i] = new TInt(10 * (i + 1), log);
                }

                ReadOnlySpan<TInt> firstSpan = new ReadOnlySpan<TInt>(first);
                ReadOnlySpan<TInt> secondSpan = new ReadOnlySpan<TInt>(second);
                Assert.True(firstSpan.EndsWith(secondSpan));

                // Make sure each element of the array was compared once. (Strictly speaking, it would not be illegal for
                // EndsWith to compare an element more than once but that would be a non-optimal implementation and
                // a red flag. So we'll stick with the stricter test.)
                Assert.Equal(first.Length, log.Count);
                foreach (TInt elem in first)
                {
                    int numCompares = log.CountCompares(elem.Value, elem.Value);
                    Assert.True(numCompares == 1, $"Expected {numCompares} == 1 for element {elem.Value}.");
                }
            }
        }
    }
}
