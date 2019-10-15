// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthEndsWith_Byte()
        {
            byte[] a = new byte[3];

            Span<byte> span = new Span<byte>(a);
            ReadOnlySpan<byte> slice = new ReadOnlySpan<byte>(a, 2, 0);
            bool b = span.EndsWith<byte>(slice);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanEndsWith_Byte()
        {
            byte[] a = { 4, 5, 6 };
            Span<byte> span = new Span<byte>(a);
            bool b = span.EndsWith(span);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchEndsWith_Byte()
        {
            byte[] a = { 4, 5, 6 };
            Span<byte> span = new Span<byte>(a, 0, 2);
            ReadOnlySpan<byte> slice = new ReadOnlySpan<byte>(a, 0, 3);
            bool b = span.EndsWith(slice);
            Assert.False(b);
        }

        [Fact]
        public static void EndsWithMatch_Byte()
        {
            byte[] a = { 4, 5, 6 };
            Span<byte> span = new Span<byte>(a, 0, 3);
            ReadOnlySpan<byte> slice = new ReadOnlySpan<byte>(a, 1, 2);
            bool b = span.EndsWith(slice);
            Assert.True(b);
        }

        [Fact]
        public static void EndsWithMatchDifferentSpans_Byte()
        {
            byte[] a = { 4, 5, 6 };
            byte[] b = { 4, 5, 6 };
            Span<byte> span = new Span<byte>(a, 0, 3);
            ReadOnlySpan<byte> slice = new ReadOnlySpan<byte>(b, 0, 3);
            bool c = span.EndsWith(slice);
            Assert.True(c);
        }

        [Fact]
        public static void EndsWithNoMatch_Byte()
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

                    Span<byte> firstSpan = new Span<byte>(first);
                    ReadOnlySpan<byte> secondSpan = new ReadOnlySpan<byte>(second);
                    bool b = firstSpan.EndsWith(secondSpan);
                    Assert.False(b);
                }
            }
        }

        [Fact]
        public static void MakeSureNoEndsWithChecksGoOutOfRange_Byte()
        {
            for (int length = 0; length < 100; length++)
            {
                byte[] first = new byte[length + 2];
                first[0] = 99;
                first[length + 1] = 99;
                byte[] second = new byte[length + 2];
                second[0] = 100;
                second[length + 1] = 100;
                Span<byte> span1 = new Span<byte>(first, 1, length);
                ReadOnlySpan<byte> span2 = new ReadOnlySpan<byte>(second, 1, length);
                bool b = span1.EndsWith(span2);
                Assert.True(b);
            }
        }
    }
}
