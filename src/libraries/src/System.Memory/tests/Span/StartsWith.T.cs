// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthStartsWith()
        {
            int[] a = new int[3];

            Span<int> first = new Span<int>(a, 1, 0);
            ReadOnlySpan<int> second = new ReadOnlySpan<int>(a, 2, 0);
            bool b = first.StartsWith(second);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanStartsWith()
        {
            int[] a = { 4, 5, 6 };
            Span<int> span = new Span<int>(a);
            bool b = span.StartsWith(span);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchStartsWith()
        {
            int[] a = { 4, 5, 6 };
            Span<int> first = new Span<int>(a, 0, 2);
            ReadOnlySpan<int> second = new ReadOnlySpan<int>(a, 0, 3);
            bool b = first.StartsWith(second);
            Assert.False(b);
        }

        [Fact]
        public static void StartsWithMatch()
        {
            int[] a = { 4, 5, 6 };
            Span<int> span = new Span<int>(a, 0, 3);
            ReadOnlySpan<int> slice = new ReadOnlySpan<int>(a, 0, 2);
            bool b = span.StartsWith(slice);
            Assert.True(b);
        }

        [Fact]
        public static void StartsWithMatchDifferentSpans()
        {
            int[] a = { 4, 5, 6 };
            int[] b = { 4, 5, 6 };
            Span<int> span = new Span<int>(a, 0, 3);
            ReadOnlySpan<int> slice = new ReadOnlySpan<int>(b, 0, 3);
            bool c = span.StartsWith(slice);
            Assert.True(c);
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

                Span<TInt> firstSpan = new Span<TInt>(first);
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

                    Span<TInt> firstSpan = new Span<TInt>(first);
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

                Span<TInt> firstSpan = new Span<TInt>(first, GuardLength, length);
                ReadOnlySpan<TInt> secondSpan = new ReadOnlySpan<TInt>(second, GuardLength, length);
                bool b = firstSpan.StartsWith(secondSpan);
                Assert.True(b);
            }
        }
    }
}
