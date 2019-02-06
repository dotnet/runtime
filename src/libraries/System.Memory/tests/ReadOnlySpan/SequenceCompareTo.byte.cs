// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceCompareTo_Byte()
        {
            var a = new byte[3];

            var first = new ReadOnlySpan<byte>(a, 1, 0);
            var second = new ReadOnlySpan<byte>(a, 2, 0);
            int result = first.SequenceCompareTo<byte>(second);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SameSpanSequenceCompareTo_Byte()
        {
            byte[] a = { 4, 5, 6 };
            var span = new ReadOnlySpan<byte>(a);
            int result = span.SequenceCompareTo<byte>(span);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SequenceCompareToArrayImplicit_Byte()
        {
            byte[] a = { 4, 5, 6 };
            var first = new ReadOnlySpan<byte>(a, 0, 3);
            int result = first.SequenceCompareTo<byte>(a);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void SequenceCompareToArraySegmentImplicit_Byte()
        {
            byte[] src = { 1, 2, 3 };
            byte[] dst = { 5, 1, 2, 3, 10 };
            var segment = new ArraySegment<byte>(dst, 1, 3);

            var first = new ReadOnlySpan<byte>(src, 0, 3);
            int result = first.SequenceCompareTo<byte>(segment);
            Assert.Equal(0, result);
        }

        [Fact]
        public static void LengthMismatchSequenceCompareTo_Byte()
        {
            byte[] a = { 4, 5, 6 };
            var first = new ReadOnlySpan<byte>(a, 0, 2);
            var second = new ReadOnlySpan<byte>(a, 0, 3);
            int result = first.SequenceCompareTo<byte>(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo<byte>(first);
            Assert.True(result > 0);

            // one sequence is empty
            first = new ReadOnlySpan<byte>(a, 1, 0);

            result = first.SequenceCompareTo<byte>(second);
            Assert.True(result < 0);

            result = second.SequenceCompareTo<byte>(first);
            Assert.True(result > 0);
        }

        [Fact]
        public static void SequenceCompareToEqual_Byte()
        {
            for (int length = 1; length < 128; length++)
            {
                var first = new byte[length];
                var second = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    first[i] = second[i] = (byte)(i + 1);
                }

                var firstSpan = new ReadOnlySpan<byte>(first);
                var secondSpan = new ReadOnlySpan<byte>(second);

                Assert.Equal(0, firstSpan.SequenceCompareTo<byte>(secondSpan));
                Assert.Equal(0, secondSpan.SequenceCompareTo<byte>(firstSpan));
            }
        }

        [Fact]
        public static void SequenceCompareToWithSingleMismatch_Byte()
        {
            for (int length = 1; length < 128; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new byte[length];
                    var second = new byte[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (byte)(i + 1);
                    }

                    second[mismatchIndex] = (byte)(second[mismatchIndex] + 1);

                    var firstSpan = new ReadOnlySpan<byte>(first);
                    var secondSpan = new ReadOnlySpan<byte>(second);
                    int result = firstSpan.SequenceCompareTo<byte>(secondSpan);
                    Assert.True(result < 0);

                    result = secondSpan.SequenceCompareTo<byte>(firstSpan);
                    Assert.True(result > 0);
                }
            }
        }

        [Fact]
        public static void SequenceCompareToNoMatch_Byte()
        {
            for (int length = 1; length < 128; length++)
            {
                var first = new byte[length];
                var second = new byte[length];

                for (int i = 0; i < length; i++)
                {
                    first[i] = (byte)(i + 1);
                    second[i] = (byte)(byte.MaxValue - i);
                }

                var firstSpan = new ReadOnlySpan<byte>(first);
                var secondSpan = new ReadOnlySpan<byte>(second);
                int result = firstSpan.SequenceCompareTo<byte>(secondSpan);
                Assert.True(result < 0);

                result = secondSpan.SequenceCompareTo<byte>(firstSpan);
                Assert.True(result > 0);
            }
        }

        [Fact]
        public static void MakeSureNoSequenceCompareToChecksGoOutOfRange_Byte()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new byte[length + 2];
                first[0] = 99;
                first[length + 1] = 99;

                var second = new byte[length + 2];
                second[0] = 100;
                second[length + 1] = 100;

                var span1 = new ReadOnlySpan<byte>(first, 1, length);
                var span2 = new ReadOnlySpan<byte>(second, 1, length);
                int result = span1.SequenceCompareTo<byte>(span2);
                Assert.Equal(0, result);
            }
        }
    }
}
