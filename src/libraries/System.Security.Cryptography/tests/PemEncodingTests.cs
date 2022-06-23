// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class PemEncodingTests
    {
        [Fact]
        public static void GetEncodedSize_Empty()
        {
            int size = PemEncoding.GetEncodedSize(labelLength: 0, dataLength: 0);
            Assert.Equal(31, size);
        }

        [Theory]
        [InlineData(1, 0, 33)]
        [InlineData(1, 1, 38)]
        [InlineData(16, 2048, 2838)]
        public static void GetEncodedSize_Simple(int labelLength, int dataLength, int expectedSize)
        {
            int size = PemEncoding.GetEncodedSize(labelLength, dataLength);
            Assert.Equal(expectedSize, size);
        }

        [Theory]
        [InlineData(1_073_741_808, 0, int.MaxValue)]
        [InlineData(1_073_741_805, 1, int.MaxValue - 1)]
        [InlineData(0, 1_585_834_053, int.MaxValue - 2)]
        [InlineData(1, 1_585_834_053, int.MaxValue)]
        public static void GetEncodedSize_Boundaries(int labelLength, int dataLength, int expectedSize)
        {
            int size = PemEncoding.GetEncodedSize(labelLength, dataLength);
            Assert.Equal(expectedSize, size);
        }

        [Fact]
        public static void GetEncodedSize_LabelLength_Overflow()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("labelLength",
                () => PemEncoding.GetEncodedSize(labelLength: 1_073_741_809, dataLength: 0));
        }

        [Fact]
        public static void GetEncodedSize_DataLength_Overflow()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dataLength",
                () => PemEncoding.GetEncodedSize(labelLength: 0, dataLength: 1_585_834_054));
        }

        [Fact]
        public static void GetEncodedSize_Combined_Overflow()
        {
            Assert.Throws<ArgumentException>(
                () => PemEncoding.GetEncodedSize(labelLength: 2, dataLength: 1_585_834_052));
        }

        [Fact]
        public static void GetEncodedSize_DataLength_Negative()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dataLength",
                () => PemEncoding.GetEncodedSize(labelLength: 0, dataLength: -1));
        }

        [Fact]
        public static void GetEncodedSize_LabelLength_Negative()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("labelLength",
                () => PemEncoding.GetEncodedSize(labelLength: -1, dataLength: 0));
        }

        [Fact]
        public static void Write_Simple()
        {
            string label = "HELLO";
            byte[] content = new byte[] { 0x66, 0x6F, 0x6F };
            AssertWrites("-----BEGIN HELLO-----\nZm9v\n-----END HELLO-----", label, content);
        }

        [Fact]
        public static void Write_Empty()
        {
            AssertWrites("-----BEGIN -----\n-----END -----", default, default);
        }

        [Fact]
        public static void Write_ExactLineNoPadding()
        {
            ReadOnlySpan<byte> data = new byte[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7
            };
            string label = "FANCY DATA";
            string expected =
                "-----BEGIN FANCY DATA-----\n" +
                "AAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYH\n" +
                "-----END FANCY DATA-----";
            AssertWrites(expected, label, data);
        }

        [Fact]
        public static void TryWrite_BufferTooSmall()
        {
            char[] buffer = new char[30];
            Assert.False(PemEncoding.TryWrite(default, default, buffer, out _));
        }

        [Fact]
        public static void TryWrite_DoesNotWriteOutsideBounds()
        {
            Span<char> buffer = new char[1000];
            buffer.Fill('!');
            ReadOnlySpan<byte> data = new byte[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7
            };

            Span<char> write = buffer[10..];
            string label = "FANCY DATA";
            Assert.True(PemEncoding.TryWrite(label, data, write, out int charsWritten));
            string pem = new string(buffer[..(charsWritten + 20)]);
            string expected =
                "!!!!!!!!!!-----BEGIN FANCY DATA-----\n" +
                "AAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYH\n" +
                "-----END FANCY DATA-----!!!!!!!!!!";
            Assert.Equal(expected, pem);
        }

        [Fact]
        public static void Write_WrapPadding()
        {
            ReadOnlySpan<byte> data = new byte[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9
            };
            string label = "UNFANCY DATA";
            string expected =
                "-----BEGIN UNFANCY DATA-----\n" +
                "AAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYH\n" +
                "CAk=\n" +
                "-----END UNFANCY DATA-----";
            AssertWrites(expected, label, data);
        }

        [Fact]
        public static void Write_EcKey()
        {
            ReadOnlySpan<byte> data = new byte[] {
                0x30, 0x74, 0x02, 0x01, 0x01, 0x04, 0x20, 0x20,
                0x59, 0xef, 0xff, 0x13, 0xd4, 0x92, 0xf6, 0x6a,
                0x6b, 0xcd, 0x07, 0xf4, 0x12, 0x86, 0x08, 0x6d,
                0x81, 0x93, 0xed, 0x9c, 0xf0, 0xf8, 0x5b, 0xeb,
                0x00, 0x70, 0x7c, 0x40, 0xfa, 0x12, 0x6c, 0xa0,
                0x07, 0x06, 0x05, 0x2b, 0x81, 0x04, 0x00, 0x0a,
                0xa1, 0x44, 0x03, 0x42, 0x00, 0x04, 0xdf, 0x23,
                0x42, 0xe5, 0xab, 0x3c, 0x25, 0x53, 0x79, 0x32,
                0x31, 0x7d, 0xe6, 0x87, 0xcd, 0x4a, 0x04, 0x41,
                0x55, 0x78, 0xdf, 0xd0, 0x22, 0xad, 0x60, 0x44,
                0x96, 0x7c, 0xf9, 0xe6, 0xbd, 0x3d, 0xe7, 0xf9,
                0xc3, 0x0c, 0x25, 0x40, 0x7d, 0x95, 0x42, 0x5f,
                0x76, 0x41, 0x4d, 0x81, 0xa4, 0x81, 0xec, 0x99,
                0x41, 0xfa, 0x4a, 0xd9, 0x55, 0x55, 0x7c, 0x4f,
                0xb1, 0xd9, 0x41, 0x75, 0x43, 0x44
            };
            string label = "EC PRIVATE KEY";
            string expected =
                "-----BEGIN EC PRIVATE KEY-----\n" +
                "MHQCAQEEICBZ7/8T1JL2amvNB/QShghtgZPtnPD4W+sAcHxA+hJsoAcGBSuBBAAK\n" +
                "oUQDQgAE3yNC5as8JVN5MjF95ofNSgRBVXjf0CKtYESWfPnmvT3n+cMMJUB9lUJf\n" +
                "dkFNgaSB7JlB+krZVVV8T7HZQXVDRA==\n" +
                "-----END EC PRIVATE KEY-----";
            AssertWrites(expected, label, data);
        }

        [Fact]
        public static void TryWrite_Throws_InvalidLabel()
        {
            char[] buffer = new char[50];
            AssertExtensions.Throws<ArgumentException>("label", () =>
                PemEncoding.TryWrite("\n", default, buffer, out _));
        }

        [Fact]
        public static void Write_Throws_InvalidLabel()
        {
            AssertExtensions.Throws<ArgumentException>("label", () =>
                PemEncoding.Write("\n", default));
        }

        [Fact]
        public static void WriteString_Throws_InvalidLabel()
        {
            AssertExtensions.Throws<ArgumentException>("label", () =>
                PemEncoding.WriteString("\n", default));
        }

        private static void AssertWrites(string expected, ReadOnlySpan<char> label, ReadOnlySpan<byte> data)
        {
            // Array-returning
            char[] resultArray = PemEncoding.Write(label, data);
            Assert.Equal(expected, new string(resultArray));

            // String-returning
            string resultString = PemEncoding.WriteString(label, data);
            Assert.Equal(expected, resultString);

            // Buffer-writing
            resultArray.AsSpan().Clear();
            Assert.True(PemEncoding.TryWrite(label, data, resultArray, out int written), "PemEncoding.TryWrite");
            Assert.Equal(expected.Length, written);
            Assert.Equal(expected, new string(resultArray));
        }
    }
}
