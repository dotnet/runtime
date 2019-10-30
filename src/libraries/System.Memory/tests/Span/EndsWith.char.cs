// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthEndsWith_Char()
        {
            var a = new char[3];

            var span = new Span<char>(a);
            var slice = new ReadOnlySpan<char>(a, 2, 0);
            bool b = span.EndsWith<char>(slice);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanEndsWith_Char()
        {
            char[] a = { '4', '5', '6' };
            var span = new Span<char>(a);
            bool b = span.EndsWith(span);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchEndsWith_Char()
        {
            char[] a = { '4', '5', '6' };
            var span = new Span<char>(a, 0, 2);
            var slice = new ReadOnlySpan<char>(a, 0, 3);
            bool b = span.EndsWith(slice);
            Assert.False(b);
        }

        [Fact]
        public static void EndsWithMatch_Char()
        {
            char[] a = { '4', '5', '6' };
            var span = new Span<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(a, 1, 2);
            bool b = span.EndsWith(slice);
            Assert.True(b);
        }

        [Fact]
        public static void EndsWithMatchDifferentSpans_Char()
        {
            char[] a = { '4', '5', '6' };
            char[] b = { '4', '5', '6' };
            var span = new Span<char>(a, 0, 3);
            var slice = new ReadOnlySpan<char>(b, 0, 3);
            bool c = span.EndsWith(slice);
            Assert.True(c);
        }

        [Fact]
        public static void EndsWithNoMatch_Char()
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    var first = new char[length];
                    var second = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = (char)(i + 1);
                    }

                    second[mismatchIndex] = (char)(second[mismatchIndex] + 1);

                    var firstSpan = new Span<char>(first);
                    var secondSpan = new ReadOnlySpan<char>(second);
                    bool b = firstSpan.EndsWith(secondSpan);
                    Assert.False(b);
                }
            }
        }

        [Fact]
        public static void MakeSureNoEndsWithChecksGoOutOfRange_Char()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                first[0] = '9';
                first[length + 1] = '9';
                var second = new char[length + 2];
                second[0] = 'a';
                second[length + 1] = 'a';
                var span1 = new Span<char>(first, 1, length);
                var span2 = new ReadOnlySpan<char>(second, 1, length);
                bool b = span1.EndsWith(span2);
                Assert.True(b);
            }
        }
    }
}
