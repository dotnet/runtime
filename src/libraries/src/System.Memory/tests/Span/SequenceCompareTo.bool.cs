// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceCompareTo_Bool()
        {
            var a = new bool[3];

            var first = new Span<bool>(a, 1, 0);
            var second = new ReadOnlySpan<bool>(a, 2, 0);
            int result = first.SequenceCompareTo<bool>(second);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SameSpanSequenceCompareTo_Bool()
        {
            bool[] a = { true, true, false };
            var span = new Span<bool>(a);
            int result = span.SequenceCompareTo<bool>(span);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SequenceCompareToArrayImplicit_Bool()
        {
            bool[] a = { true, true, false };
            var first = new Span<bool>(a, 0, 3);
            int result = first.SequenceCompareTo<bool>(a);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SequenceCompareToArraySegmentImplicit_Bool()
        {
            bool[] src = { true, true, true };
            bool[] dst = { false, true, true, true, false };
            var segment = new ArraySegment<bool>(dst, 1, 3);

            var first = new Span<bool>(src, 0, 3);
            int result = first.SequenceCompareTo<bool>(segment);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void LengthMismatchSequenceCompareTo_Bool()
        {
            bool[] a = { true, true, false };
            var first = new Span<bool>(a, 0, 2);
            var second = new Span<bool>(a, 0, 3);
            int result = first.SequenceCompareTo<bool>(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo<bool>(first);
            Assert.True(result > 0);

            // one sequence is empty
            first = new Span<bool>(a, 1, 0);

            result = first.SequenceCompareTo<bool>(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo<bool>(first);
            Assert.True(result > 0);
        }

        [Fact]
        public static void SequenceCompareToWithSingleMismatch_Bool()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new bool[length];
                    var second = new bool[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = true;
                    }

                    second[mismatchIndex] = !second[mismatchIndex];

                    var firstSpan = new Span<bool>(first);
                    var secondSpan = new ReadOnlySpan<bool>(second);
                    int result = firstSpan.SequenceCompareTo<bool>(secondSpan);
                    Assert.True(result > 0);

                    result = secondSpan.SequenceCompareTo<bool>(firstSpan);
                    Assert.True(result < 0);
                }
            }
        }

        [Fact]
        public static void SequenceCompareToNoMatch_Bool()
        {
            for (int length = 1; length < 32; length++)
            {
                var first = new bool[length];
                var second = new bool[length];

                for (int i = 0; i < length; i++)
                {
                    first[i] = (i % 2 != 0);
                    second[i] = (i % 2 == 0);
                }

                var firstSpan = new Span<bool>(first);
                var secondSpan = new ReadOnlySpan<bool>(second);
                int result = firstSpan.SequenceCompareTo<bool>(secondSpan);
                Assert.True(result < 0);

                result = secondSpan.SequenceCompareTo<bool>(firstSpan);
                Assert.True(result > 0);
            }
        }

        [Fact]
        public static void MakeSureNoSequenceCompareToChecksGoOutOfRange_Bool()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new bool[length + 2];
                first[0] = true;
                for (int k = 1; k <= length; k++)
                    first[k] = false;
                first[length + 1] = true;

                var second = new bool[length + 2];
                second[0] = false;
                for (int k = 1; k <= length; k++)
                    second[k] = false;
                second[length + 1] = false;

                var span1 = new Span<bool>(first, 1, length);
                var span2 = new ReadOnlySpan<bool>(second, 1, length);
                int result = span1.SequenceCompareTo<bool>(span2);
                Assert.Equal(0, result);
            }
        }
    }
}
