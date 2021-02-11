// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceEqual_Long()
        {
            long[] a = new long[3];

            Span<long> first = new Span<long>(a, 1, 0);
            Span<long> second = new Span<long>(a, 2, 0);
            bool b = first.SequenceEqual<long>(second);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanSequenceEqual_Long()
        {
            long[] a = { 488238291, 52498989823, 619890289890 };
            Span<long> span = new Span<long>(a);
            bool b = span.SequenceEqual<long>(span);
            Assert.True(b);
        }

        [Fact]
        public static void SequenceEqualArrayImplicit_Long()
        {
            long[] a = { 488238291, 52498989823, 619890289890 };
            Span<long> first = new Span<long>(a, 0, 3);
            bool b = first.SequenceEqual<long>(a);
            Assert.True(b);
        }

        [Fact]
        public static void SequenceEqualArraySegmentImplicit_Long()
        {
            long[] src = { 1989089123, 234523454235, 3123213231 };
            long[] dst = { 5, 1989089123, 234523454235, 3123213231, 10 };
            ArraySegment<long> segment = new ArraySegment<long>(dst, 1, 3);

            Span<long> first = new Span<long>(src, 0, 3);
            bool b = first.SequenceEqual<long>(segment);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchSequenceEqual_Long()
        {
            long[] a = { 488238291, 52498989823, 619890289890 };
            Span<long> first = new Span<long>(a, 0, 3);
            Span<long> second = new Span<long>(a, 0, 2);
            bool b = first.SequenceEqual<long>(second);
            Assert.False(b);
        }

        [Fact]
        public static void SequenceEqualNoMatch_Long()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    long[] first = new long[length];
                    long[] second = new long[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (byte)(i + 1);
                    }

                    second[mismatchIndex] = (byte)(second[mismatchIndex] + 1);

                    Span<long> firstSpan = new Span<long>(first);
                    ReadOnlySpan<long> secondSpan = new ReadOnlySpan<long>(second);
                    bool b = firstSpan.SequenceEqual<long>(secondSpan);
                    Assert.False(b);
                }
            }
        }

        [Fact]
        public static void MakeSureNoSequenceEqualChecksGoOutOfRange_Long()
        {
            for (int length = 0; length < 100; length++)
            {
                long[] first = new long[length + 2];
                first[0] = 99;
                first[length + 1] = 99;
                long[] second = new long[length + 2];
                second[0] = 100;
                second[length + 1] = 100;
                Span<long> span1 = new Span<long>(first, 1, length);
                ReadOnlySpan<long> span2 = new ReadOnlySpan<long>(second, 1, length);
                bool b = span1.SequenceEqual<long>(span2);
                Assert.True(b);
            }
        }
    }
}
