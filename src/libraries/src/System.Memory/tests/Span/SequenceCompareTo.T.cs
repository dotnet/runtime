// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceCompareTo()
        {
            int[] a = new int[3];

            Span<int> first = new Span<int>(a, 1, 0);
            Span<int> second = new Span<int>(a, 2, 0);
            int result = first.SequenceCompareTo(second);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SameSpanSequenceCompareTo()
        {
            int[] a = { 4, 5, 6 };
            Span<int> span = new Span<int>(a);
            int result = span.SequenceCompareTo(span);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void LengthMismatchSequenceCompareTo()
        {
            int[] a = { 4, 5, 6 };
            Span<int> first = new Span<int>(a, 0, 2);
            Span<int> second = new Span<int>(a, 0, 3);
            int result = first.SequenceCompareTo(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo(first);
            Assert.True(result > 0);

            // one sequence is empty
            first = new Span<int>(a, 1, 0);

            result = first.SequenceCompareTo(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo(first);
            Assert.True(result > 0);
        }

        [Fact]
        public static void OnSequenceCompareToOfEqualSpansMakeSureEveryElementIsCompared()
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
                int result = firstSpan.SequenceCompareTo(secondSpan);
                Assert.Equal(0, result);

                // Make sure each element of the array was compared once. (Strictly speaking, it would not be illegal for 
                // SequenceCompareTo to compare an element more than once but that would be a non-optimal implementation and 
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
        public static void SequenceCompareToSingleMismatch()
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
                    int result = firstSpan.SequenceCompareTo(secondSpan);
                    Assert.True(result < 0);
                    Assert.Equal(1, log.CountCompares(first[mismatchIndex].Value, second[mismatchIndex].Value));

                    result = secondSpan.SequenceCompareTo(firstSpan);       // adds to log.CountCompares
                    Assert.True(result > 0);
                    Assert.Equal(2, log.CountCompares(first[mismatchIndex].Value, second[mismatchIndex].Value));
                }
            }
        }

        [Fact]
        public static void SequenceCompareToNoMatch()
        {
            for (int length = 1; length < 32; length++)
            {
                TIntLog log = new TIntLog();

                TInt[] first = new TInt[length];
                TInt[] second = new TInt[length];
                
                for (int i = 0; i < length; i++)
                {
                    first[i] = new TInt(i + 1, log);
                    second[i] = new TInt(length + i + 1, log);
                }

                Span<TInt> firstSpan = new Span<TInt>(first);
                ReadOnlySpan<TInt> secondSpan = new ReadOnlySpan<TInt>(second);
                int result = firstSpan.SequenceCompareTo(secondSpan);
                Assert.True(result < 0);
                Assert.Equal(1, log.CountCompares(firstSpan[0].Value, secondSpan[0].Value));

                result = secondSpan.SequenceCompareTo(firstSpan);       // adds to log.CountCompares
                Assert.True(result > 0);
                Assert.Equal(2, log.CountCompares(firstSpan[0].Value, secondSpan[0].Value));
            }
        }

        [Fact]
        public static void MakeSureNoSequenceCompareToChecksGoOutOfRange()
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
                Span<TInt> secondSpan = new Span<TInt>(second, GuardLength, length);
                int result = firstSpan.SequenceCompareTo(secondSpan);
                Assert.Equal(0, result);
            }
        }
    }
}
