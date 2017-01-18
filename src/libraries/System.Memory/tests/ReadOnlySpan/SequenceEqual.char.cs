// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceEqual_Char()
        {
            char[] a = new char[3];

            ReadOnlySpan<char> first = new ReadOnlySpan<char>(a, 1, 0);
            ReadOnlySpan<char> second = new ReadOnlySpan<char>(a, 2, 0);
            bool b = first.SequenceEqual(second);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanSequenceEqual_Char()
        {
            char[] a = { '4', '5', '6' };
            ReadOnlySpan<char> span = new ReadOnlySpan<char>(a);
            bool b = span.SequenceEqual(span);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchSequenceEqual_Char()
        {
            char[] a = { '4', '5', '6' };
            ReadOnlySpan<char> first = new ReadOnlySpan<char>(a, 0, 3);
            ReadOnlySpan<char> second = new ReadOnlySpan<char>(a, 0, 2);
            bool b = first.SequenceEqual(second);
            Assert.False(b);
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

                    ReadOnlySpan<char> firstSpan = new ReadOnlySpan<char>(first);
                    ReadOnlySpan<char> secondSpan = new ReadOnlySpan<char>(second);
                    bool b = firstSpan.SequenceEqual(secondSpan);
                    Assert.False(b);
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
                ReadOnlySpan<char> span1 = new ReadOnlySpan<char>(first, 1, length);
                ReadOnlySpan<char> span2 = new ReadOnlySpan<char>(second, 1, length);
                bool b = span1.SequenceEqual(span2);
                Assert.True(b);
            }
        }
    }
}
