// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceEqual_Char()
        {
            char[] a = new char[3];

            Span<char> first = new Span<char>(a, 1, 0);
            Span<char> second = new Span<char>(a, 2, 0);

            Assert.True(first.SequenceEqual(second));
            Assert.True(first.SequenceEqual(second, null));
            Assert.True(first.SequenceEqual(second, EqualityComparer<char>.Default));
        }

        [Fact]
        public static void SameSpanSequenceEqual_Char()
        {
            char[] a = { '4', '5', '6' };
            Span<char> span = new Span<char>(a);

            Assert.True(span.SequenceEqual(span));
            Assert.True(span.SequenceEqual(span, null));
            Assert.True(span.SequenceEqual(span, EqualityComparer<char>.Default));
        }

        [Fact]
        public static void LengthMismatchSequenceEqual_Char()
        {
            char[] a = { '4', '5', '6' };
            Span<char> first = new Span<char>(a, 0, 3);
            Span<char> second = new Span<char>(a, 0, 2);

            Assert.False(first.SequenceEqual(second));
            Assert.False(first.SequenceEqual(second, null));
            Assert.False(first.SequenceEqual(second, EqualityComparer<char>.Default));
        }

        [Fact]
        public static void SequenceEqualNoMatch_Char()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    char[] first = new char[length];
                    char[] second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    Span<char> firstSpan = new Span<char>(first);
                    ReadOnlySpan<char> secondSpan = new ReadOnlySpan<char>(second);

                    Assert.False(firstSpan.SequenceEqual(secondSpan));
                    Assert.False(firstSpan.SequenceEqual(secondSpan, null));
                    Assert.False(firstSpan.SequenceEqual(secondSpan, EqualityComparer<char>.Default));
                }
            }
        }

        [Fact]
        public static void MakeSureNoSequenceEqualChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                char[] first = new char[length + 2];
                first[0] = '9';
                first[length + 1] = '9';
                char[] second = new char[length + 2];
                second[0] = 'a';
                second[length + 1] = 'a';
                Span<char> span1 = new Span<char>(first, 1, length);
                ReadOnlySpan<char> span2 = new ReadOnlySpan<char>(second, 1, length);

                Assert.True(span1.SequenceEqual(span2));
                Assert.True(span1.SequenceEqual(span2, null));
                Assert.True(span1.SequenceEqual(span2, EqualityComparer<char>.Default));
            }
        }
    }
}
