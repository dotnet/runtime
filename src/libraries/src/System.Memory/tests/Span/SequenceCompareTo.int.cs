// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceCompareTo_Int()
        {
            var a = new int[3];

            var first = new Span<int>(a, 1, 0);
            var second = new ReadOnlySpan<int>(a, 2, 0);
            int result = first.SequenceCompareTo<int>(second);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SameSpanSequenceCompareTo_Int()
        {
            int[] a = { 851227, 28052014, 429104168 };
            var span = new Span<int>(a);
            int result = span.SequenceCompareTo<int>(span);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SequenceCompareToArrayImplicit_Int()
        {
            int[] a = { 851227, 28052014, 429104168 };
            var first = new Span<int>(a, 0, 3);
            int result = first.SequenceCompareTo<int>(a);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SequenceCompareToArraySegmentImplicit_Int()
        {
            int[] src = { 851227, 28052014, 429104168 };
            int[] dst = { 5, 851227, 28052014, 429104168, 10 };
            var segment = new ArraySegment<int>(dst, 1, 3);

            var first = new Span<int>(src, 0, 3);
            int result = first.SequenceCompareTo<int>(segment);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void LengthMismatchSequenceCompareTo_Int()
        {
            int[] a = { 851227, 28052014, 429104168 };
            var first = new Span<int>(a, 0, 2);
            var second = new Span<int>(a, 0, 3);
            int result = first.SequenceCompareTo<int>(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo<int>(first);
            Assert.True(result > 0);

            // one sequence is empty
            first = new Span<int>(a, 1, 0);

            result = first.SequenceCompareTo<int>(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo<int>(first);
            Assert.True(result > 0);
        }

        [Fact]
        public static void SequenceCompareToWithSingleMismatch_Int()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new int[length];
                    var second = new int[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (int)(i + 1);
                    }

                    second[mismatchIndex] = (int)(second[mismatchIndex] + 1);

                    var firstSpan = new Span<int>(first);
                    var secondSpan = new ReadOnlySpan<int>(second);
                    int result = firstSpan.SequenceCompareTo<int>(secondSpan);
                    Assert.True(result < 0);

                    result = secondSpan.SequenceCompareTo<int>(firstSpan);
                    Assert.True(result > 0);
                }
            }
        }

        [Fact]
        public static void SequenceCompareToNoMatch_Int()
        {
            for (int length = 1; length < 32; length++)
            {
                var first = new int[length];
                var second = new int[length];

                for (int i = 0; i < length; i++)
                {
                    first[i] = (int)(i + 1);
                    second[i] = (int)(int.MaxValue - i);
                }

                var firstSpan = new Span<int>(first);
                var secondSpan = new ReadOnlySpan<int>(second);
                int result = firstSpan.SequenceCompareTo<int>(secondSpan);
                Assert.True(result < 0);

                result = secondSpan.SequenceCompareTo<int>(firstSpan);
                Assert.True(result > 0);
            }
        }

        [Fact]
        public static void MakeSureNoSequenceCompareToChecksGoOutOfRange_Int()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new int[length + 2];
                first[0] = 99;
                first[length + 1] = 99;

                var second = new int[length + 2];
                second[0] = 100;
                second[length + 1] = 100;

                var span1 = new Span<int>(first, 1, length);
                var span2 = new ReadOnlySpan<int>(second, 1, length);
                int result = span1.SequenceCompareTo<int>(span2);
                Assert.Equal(0, result);
            }
        }
    }
}
