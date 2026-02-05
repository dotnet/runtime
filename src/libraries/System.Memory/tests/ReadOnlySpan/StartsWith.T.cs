// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthStartsWith()
        {
            int[] a = new int[3];

            Assert.True(new ReadOnlySpan<int>(a, 1, 0).StartsWith(new ReadOnlySpan<int>(a, 2, 0)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.True(new ReadOnlySpan<int>(a, 1, 0).StartsWith(new ReadOnlySpan<int>(a, 2, 0), comparer)));
            Assert.True(new ReadOnlySpan<int>(a, 1, 0).StartsWith(new ReadOnlySpan<int>(a, 2, 0), GetFalseEqualityComparer<int>()));
        }

        [Fact]
        public static void SameSpanStartsWith()
        {
            int[] a = { 4, 5, 6 };
            Assert.True(new ReadOnlySpan<int>(a).StartsWith(a));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.True(new ReadOnlySpan<int>(a).StartsWith(a, comparer)));
            Assert.False(new ReadOnlySpan<int>(a).StartsWith(a, GetFalseEqualityComparer<int>()));
        }

        [Fact]
        public static void LengthMismatchStartsWith()
        {
            int[] a = { 4, 5, 6 };

            Assert.False(new ReadOnlySpan<int>(a, 0, 2).StartsWith(new ReadOnlySpan<int>(a, 0, 3)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.False(new ReadOnlySpan<int>(a, 0, 2).StartsWith(new ReadOnlySpan<int>(a, 0, 3), comparer)));
        }

        [Fact]
        public static void StartsWithMatch()
        {
            int[] a = { 4, 5, 6 };

            Assert.True(new ReadOnlySpan<int>(a, 0, 3).StartsWith(new ReadOnlySpan<int>(a, 0, 2)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.True(new ReadOnlySpan<int>(a, 0, 3).StartsWith(new ReadOnlySpan<int>(a, 0, 2), comparer)));
            Assert.False(new ReadOnlySpan<int>(a, 0, 3).StartsWith(new ReadOnlySpan<int>(a, 0, 2), GetFalseEqualityComparer<int>()));
        }

        [Fact]
        public static void StartsWithMatchDifferentSpans()
        {
            int[] a = { 4, 5, 6 };
            int[] b = { 4, 5, 6 };

            Assert.True(new ReadOnlySpan<int>(a, 0, 3).StartsWith(new ReadOnlySpan<int>(b, 0, 3)));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.True(new ReadOnlySpan<int>(a, 0, 3).StartsWith(new ReadOnlySpan<int>(b, 0, 3), comparer)));
            Assert.False(new ReadOnlySpan<int>(a, 0, 3).StartsWith(new ReadOnlySpan<int>(b, 0, 3), GetFalseEqualityComparer<int>()));
        }

        [Fact]
        public static void OnStartsWithOfEqualSpansMakeSureEveryElementIsCompared()
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
                bool b = firstSpan.StartsWith(secondSpan);
                Assert.True(b);

                // Make sure each element of the array was compared once. (Strictly speaking, it would not be illegal for
                // StartsWith to compare an element more than once but that would be a non-optimal implementation and
                // a red flag. So we'll stick with the stricter test.)
                Assert.Equal(first.Length, log.Count);
                foreach (TInt elem in first)
                {
                    int numCompares = log.CountCompares(elem.Value, elem.Value);
                    Assert.True(numCompares == 1, $"Expected {numCompares} == 1 for element {elem.Value}.");
                }
            }
        }

        [Fact]
        public static void StartsWithNoMatch()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    TIntLog log = new TIntLog();

                    TInt[] first = new TInt[length];
                    TInt[] second = new TInt[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = new TInt(10 * (i + 1), log);
                    }

                    second[mismatchIndex] = new TInt(second[mismatchIndex].Value + 1, log);

                    ReadOnlySpan<TInt> firstSpan = new ReadOnlySpan<TInt>(first);
                    ReadOnlySpan<TInt> secondSpan = new ReadOnlySpan<TInt>(second);
                    bool b = firstSpan.StartsWith(secondSpan);
                    Assert.False(b);

                    Assert.Equal(1, log.CountCompares(first[mismatchIndex].Value, second[mismatchIndex].Value));
                }
            }
        }

        [Fact]
        public static void MakeSureNoStartsWithChecksGoOutOfRange()
        {
            const int GuardValue = 77777;
            const int GuardLength = 50;

            Action<int, int> checkForOutOfRangeAccess =
                delegate (int x, int y)
                {
                    if (x == GuardValue || y == GuardValue)
                        throw new Exception("Detected out of range access in IndexOf()");
                };

            for (int length = 0; length < 100; length++)
            {
                TInt[] first = new TInt[GuardLength + length + GuardLength];
                TInt[] second = new TInt[GuardLength + length + GuardLength];
                for (int i = 0; i < first.Length; i++)
                {
                    first[i] = second[i] = new TInt(GuardValue, checkForOutOfRangeAccess);
                }

                for (int i = 0; i < length; i++)
                {
                    first[GuardLength + i] = second[GuardLength + i] = new TInt(10 * (i + 1), checkForOutOfRangeAccess);
                }

                Assert.True(new ReadOnlySpan<TInt>(first, GuardLength, length).StartsWith(new ReadOnlySpan<TInt>(second, GuardLength, length)));
                Assert.All(GetDefaultEqualityComparers<TInt>(), comparer => Assert.True(new ReadOnlySpan<TInt>(first, GuardLength, length).StartsWith(new ReadOnlySpan<TInt>(second, GuardLength, length), comparer)));
            }
        }
    }
}
