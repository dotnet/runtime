// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceEqual_Byte()
        {
            byte[] a = new byte[3];

            ReadOnlySpan<byte> first = new ReadOnlySpan<byte>(a, 1, 0);
            ReadOnlySpan<byte> second = new ReadOnlySpan<byte>(a, 2, 0);

            Assert.True(first.SequenceEqual<byte>(second));
            Assert.True(first.SequenceEqual<byte>(second, null));
            Assert.True(first.SequenceEqual<byte>(second, EqualityComparer<byte>.Default));
        }

        [Fact]
        public static void SameSpanSequenceEqual_Byte()
        {
            byte[] a = { 4, 5, 6 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);

            Assert.True(span.SequenceEqual<byte>(span));
            Assert.True(span.SequenceEqual<byte>(span, null));
            Assert.True(span.SequenceEqual<byte>(span, EqualityComparer<byte>.Default));
        }

        [Fact]
        public static void SequenceEqualArrayImplicit_Byte()
        {
            byte[] a = { 4, 5, 6 };
            ReadOnlySpan<byte> first = new ReadOnlySpan<byte>(a, 0, 3);

            Assert.True(first.SequenceEqual<byte>(a));
            Assert.True(first.SequenceEqual<byte>(a, null));
            Assert.True(first.SequenceEqual<byte>(a, EqualityComparer<byte>.Default));
        }

        [Fact]
        public static void SequenceEqualArraySegmentImplicit_Byte()
        {
            byte[] src = { 1, 2, 3 };
            byte[] dst = { 5, 1, 2, 3, 10 };
            var segment = new ArraySegment<byte>(dst, 1, 3);

            ReadOnlySpan<byte> first = new ReadOnlySpan<byte>(src, 0, 3);

            Assert.True(first.SequenceEqual<byte>(segment));
            Assert.True(first.SequenceEqual<byte>(segment, null));
            Assert.True(first.SequenceEqual<byte>(segment, EqualityComparer<byte>.Default));
        }

        [Fact]
        public static void LengthMismatchSequenceEqual_Byte()
        {
            byte[] a = { 4, 5, 6 };
            ReadOnlySpan<byte> first = new ReadOnlySpan<byte>(a, 0, 3);
            ReadOnlySpan<byte> second = new ReadOnlySpan<byte>(a, 0, 2);

            Assert.False(first.SequenceEqual<byte>(second));
            Assert.False(first.SequenceEqual<byte>(second, null));
            Assert.False(first.SequenceEqual<byte>(second, EqualityComparer<byte>.Default));
        }

        [Fact]
        public static void SequenceEqualNoMatch_Byte()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    byte[] first = new byte[length];
                    byte[] second = new byte[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (byte)(i + 1);
                    }

                    second[mismatchIndex] = (byte)(second[mismatchIndex] + 1);

                    ReadOnlySpan<byte> firstSpan = new ReadOnlySpan<byte>(first);
                    ReadOnlySpan<byte> secondSpan = new ReadOnlySpan<byte>(second);

                    Assert.False(firstSpan.SequenceEqual<byte>(secondSpan));
                    Assert.False(firstSpan.SequenceEqual<byte>(secondSpan, null));
                    Assert.False(firstSpan.SequenceEqual<byte>(secondSpan, EqualityComparer<byte>.Default));
                }
            }
        }

        [Fact]
        public static void MakeSureNoSequenceEqualChecksGoOutOfRange_Byte()
        {
            for (int length = 0; length < 100; length++)
            {
                byte[] first = new byte[length + 2];
                first[0] = 99;
                first[length + 1] = 99;
                byte[] second = new byte[length + 2];
                second[0] = 100;
                second[length + 1] = 100;
                ReadOnlySpan<byte> span1 = new ReadOnlySpan<byte>(first, 1, length);
                ReadOnlySpan<byte> span2 = new ReadOnlySpan<byte>(second, 1, length);

                Assert.True(span1.SequenceEqual<byte>(span2));
                Assert.True(span1.SequenceEqual<byte>(span2, null));
                Assert.True(span1.SequenceEqual<byte>(span2, EqualityComparer<byte>.Default));
            }
        }
    }
}
