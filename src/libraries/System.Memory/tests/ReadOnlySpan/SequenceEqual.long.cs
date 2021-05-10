// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceEqual_Long()
        {
            long[] a = new long[3];

            ReadOnlySpan<long> first = new ReadOnlySpan<long>(a, 1, 0);
            ReadOnlySpan<long> second = new ReadOnlySpan<long>(a, 2, 0);

            Assert.True(first.SequenceEqual<long>(second));
            Assert.True(first.SequenceEqual<long>(second, null));
            Assert.True(first.SequenceEqual<long>(second, EqualityComparer<long>.Default));
        }

        [Fact]
        public static void SameSpanSequenceEqual_Long()
        {
            long[] a = { 488238291, 52498989823, 619890289890 };
            ReadOnlySpan<long> span = new ReadOnlySpan<long>(a);

            Assert.True(span.SequenceEqual<long>(span));
            Assert.True(span.SequenceEqual<long>(span, null));
            Assert.True(span.SequenceEqual<long>(span, EqualityComparer<long>.Default));
        }

        [Fact]
        public static void SequenceEqualArrayImplicit_Long()
        {
            long[] a = { 488238291, 52498989823, 619890289890 };
            ReadOnlySpan<long> first = new ReadOnlySpan<long>(a, 0, 3);

            Assert.True(first.SequenceEqual<long>(a));
            Assert.True(first.SequenceEqual<long>(a, null));
            Assert.True(first.SequenceEqual<long>(a, EqualityComparer<long>.Default));
        }

        [Fact]
        public static void SequenceEqualArraySegmentImplicit_Long()
        {
            long[] src = { 1989089123, 234523454235, 3123213231 };
            long[] dst = { 5, 1989089123, 234523454235, 3123213231, 10 };
            ArraySegment<long> segment = new ArraySegment<long>(dst, 1, 3);

            ReadOnlySpan<long> first = new ReadOnlySpan<long>(src, 0, 3);

            Assert.True(first.SequenceEqual<long>(segment));
            Assert.True(first.SequenceEqual<long>(segment, null));
            Assert.True(first.SequenceEqual<long>(segment, EqualityComparer<long>.Default));
        }

        [Fact]
        public static void LengthMismatchSequenceEqual_Long()
        {
            long[] a = { 488238291, 52498989823, 619890289890 };
            ReadOnlySpan<long> first = new ReadOnlySpan<long>(a, 0, 3);
            ReadOnlySpan<long> second = new ReadOnlySpan<long>(a, 0, 2);

            Assert.False(first.SequenceEqual<long>(second));
            Assert.False(first.SequenceEqual<long>(second, null));
            Assert.False(first.SequenceEqual<long>(second, EqualityComparer<long>.Default));
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

                    ReadOnlySpan<long> firstSpan = new ReadOnlySpan<long>(first);
                    ReadOnlySpan<long> secondSpan = new ReadOnlySpan<long>(second);

                    Assert.False(firstSpan.SequenceEqual<long>(secondSpan));
                    Assert.False(firstSpan.SequenceEqual<long>(secondSpan, null));
                    Assert.False(firstSpan.SequenceEqual<long>(secondSpan, EqualityComparer<long>.Default));
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
                ReadOnlySpan<long> span1 = new ReadOnlySpan<long>(first, 1, length);
                ReadOnlySpan<long> span2 = new ReadOnlySpan<long>(second, 1, length);

                Assert.True(span1.SequenceEqual<long>(span2));
                Assert.True(span1.SequenceEqual<long>(span2, null));
                Assert.True(span1.SequenceEqual<long>(span2, EqualityComparer<long>.Default));
            }
        }
    }
}
