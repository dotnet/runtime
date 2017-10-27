// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthStartsWith_Byte()
        {
            byte[] a = new byte[3];

            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
            ReadOnlySpan<byte> slice = new ReadOnlySpan<byte>(a, 2, 0);
            bool b = span.StartsWith<byte>(slice);
            Assert.True(b);
        }

        [Fact]
        public static void SameSpanStartsWith_Byte()
        {
            byte[] a = { 4, 5, 6 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a);
            bool b = span.StartsWith<byte>(span);
            Assert.True(b);
        }

        [Fact]
        public static void LengthMismatchStartsWith_Byte()
        {
            byte[] a = { 4, 5, 6 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 0, 2);
            ReadOnlySpan<byte> slice = new ReadOnlySpan<byte>(a, 0, 3);
            bool b = span.StartsWith<byte>(slice);
            Assert.False(b);
        }

        [Fact]
        public static void StartsWithMatch_Byte()
        {
            byte[] a = { 4, 5, 6 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 0, 3);
            ReadOnlySpan<byte> slice = new ReadOnlySpan<byte>(a, 0, 2);
            bool b = span.StartsWith<byte>(slice);
            Assert.True(b);
        }

        [Fact]
        public static void StartsWithMatchDifferentSpans_Byte()
        {
            byte[] a = { 4, 5, 6 };
            byte[] b = { 4, 5, 6 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(a, 0, 3);
            ReadOnlySpan<byte> slice = new ReadOnlySpan<byte>(b, 0, 3);
            bool c = span.StartsWith<byte>(slice);
            Assert.True(c);
        }

        [Fact]
        public static void StartsWithNoMatch_Byte()
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
                    bool b = firstSpan.StartsWith<byte>(secondSpan);
                    Assert.False(b);
                }
            }
        }

        [Fact]
        public static void MakeSureNoStartsWithChecksGoOutOfRange_Byte()
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
                bool b = span1.StartsWith<byte>(span2);
                Assert.True(b);
            }
        }
    }
}
