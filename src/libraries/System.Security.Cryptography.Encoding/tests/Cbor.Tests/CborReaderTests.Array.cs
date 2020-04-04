// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborReaderTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        [Theory]
        [InlineData(new object[] { }, "80")]
        [InlineData(new object[] { 42 }, "81182a")]
        [InlineData(new object[] { 1, 2, 3 }, "83010203")]
        [InlineData(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 }, "98190102030405060708090a0b0c0d0e0f101112131415161718181819")]
        [InlineData(new object[] { 1, -1, "", new byte[] { 7 } }, "840120604107")]
        [InlineData(new object[] { "lorem", "ipsum", "dolor" }, "83656c6f72656d65697073756d65646f6c6f72")]
        [InlineData(new object?[] { false, null, float.NaN, double.PositiveInfinity }, "84f4f6faffc00000fb7ff0000000000000")]
        public static void ReadArray_SimpleValues_HappyPath(object[] expectedValues, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyArray(reader, expectedValues);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(new object[] { new object[] { } }, "8180")]
        [InlineData(new object[] { 1, new object[] { 2, 3 }, new object[] { 4, 5 } }, "8301820203820405")]
        [InlineData(new object[] { "", new object[] { new object[] { }, new object[] { 1, new byte[] { 10 } } } }, "826082808201410a")]
        public static void ReadArray_NestedValues_HappyPath(object[] expectedValues, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyArray(reader, expectedValues);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(new object[] { }, "9fff")]
        [InlineData(new object[] { 42 }, "9f182aff")]
        [InlineData(new object[] { 1, 2, 3 }, "9f010203ff")]
        [InlineData(new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 }, "9f0102030405060708090a0b0c0d0e0f101112131415161718181819ff")]
        [InlineData(new object[] { 1, -1, "", new byte[] { 7 } }, "9f0120604107ff")]
        [InlineData(new object[] { "lorem", "ipsum", "dolor" }, "9f656c6f72656d65697073756d65646f6c6f72ff")]
        [InlineData(new object?[] { false, null, float.NaN, double.PositiveInfinity }, "9ff4f6faffc00000fb7ff0000000000000ff")]
        public static void ReadArray_IndefiniteLength_HappyPath(object[] expectedValues, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Helpers.VerifyArray(reader, expectedValues, expectDefiniteLengthCollections: false);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData("80", 0)]
        [InlineData("8101", 1)]
        [InlineData("83010203", 3)]
        public static void ReadArray_DefiniteLengthExceeded_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartArray();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < expectedLength; i++)
            {
                reader.ReadInt64();
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
        }

        [Theory]
        [InlineData("818101", 1)]
        [InlineData("83810181028103", 3)]
        public static void ReadArray_DefiniteLengthExceeded_WithNestedData_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartArray();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < expectedLength; i++)
            {
                ulong? nestedLength = reader.ReadStartArray();
                Assert.Equal(1, (int)nestedLength!.Value);
                reader.ReadInt64();
                reader.ReadEndArray();
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());
        }

        [Theory]
        [InlineData("9f")]
        [InlineData("9f01")]
        [InlineData("9f0102")]
        public static void ReadArray_IndefiniteLength_MissingBreakByte_ShouldReportEndOfData(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartArray();
            while (reader.Peek() == CborReaderState.UnsignedInteger)
            {
                reader.ReadInt64();
            }

            Assert.Equal(CborReaderState.EndOfData, reader.Peek());
        }

        [Theory]
        [InlineData("9f01ff", 1)]
        [InlineData("9f0102ff", 2)]
        [InlineData("9f010203ff", 3)]
        public static void ReadArray_IndefiniteLength_PrematureEndArrayCall_ShouldThrowInvalidOperationException(string hexEncoding, int length)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartArray();
            for (int i = 1; i < length; i++)
            {
                reader.ReadInt64();
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadEndArray());
        }

        [Theory]
        [InlineData("8101", 1)]
        [InlineData("83010203", 3)]
        public static void EndReadArray_DefiniteLengthNotMet_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartArray();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 1; i < expectedLength; i++)
            {
                reader.ReadInt64();
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadEndArray());
        }

        [Theory]
        [InlineData("818101", 1)]
        [InlineData("83810181028103", 3)]
        public static void EndReadArray_DefiniteLengthNotMet_WithNestedData_ShouldThrowInvalidOperationException(string hexEncoding, int expectedLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartArray();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 1; i < expectedLength; i++)
            {
                ulong? nestedLength = reader.ReadStartArray();
                Assert.Equal(1, (int)nestedLength!.Value);
                reader.ReadInt64();
                reader.ReadEndArray();
            }

            Assert.Throws<InvalidOperationException>(() => reader.ReadEndArray());
        }

        [Fact]
        public static void EndReadArray_ImbalancedCall_ShouldThrowInvalidOperationException()
        {
            byte[] encoding = "80".HexToByteArray(); // []
            var reader = new CborReader(encoding);
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndArray());
        }

        [Theory]
        [InlineData("821907e4", 2, 1)]
        [InlineData("861907e41907e4", 6, 2)]
        public static void ReadArray_IncorrectDefiniteLength_ShouldThrowFormatException(string hexEncoding, int expectedLength, int actualLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartArray();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < actualLength; i++)
            {
                reader.ReadInt64();
            }

            Assert.Throws<FormatException>(() => reader.ReadInt64());
        }

        [Theory]
        [InlineData("828101", 2, 1)]
        [InlineData("868101811907e4", 6, 2)]
        public static void ReadArray_IncorrectDefiniteLength_NestedValues_ShouldThrowFormatException(string hexEncoding, int expectedLength, int actualLength)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ulong? length = reader.ReadStartArray();
            Assert.Equal(expectedLength, (int)length!.Value);

            for (int i = 0; i < actualLength; i++)
            {
                ulong? innerLength = reader.ReadStartArray();
                Assert.Equal(1, (int)innerLength!.Value);
                reader.ReadInt64();
                reader.ReadEndArray();
            }

            Assert.Throws<FormatException>(() => reader.ReadInt64());
        }

        [Fact]
        public static void ReadStartArray_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadStartArray());
        }

        [Theory]
        [InlineData("00")] // 0
        [InlineData("20")] // -1
        [InlineData("40")] // empty byte string
        [InlineData("60")] // empty text string
        [InlineData("f6")] // null
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadStartArray_InvalidType_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<InvalidOperationException>(() => reader.ReadStartArray());
        }

        [Theory]
        // Invalid initial bytes with array major type
        [InlineData("9c")]
        [InlineData("9d")]
        [InlineData("9e")]
        // valid initial bytes missing required definite length data
        [InlineData("98")]
        [InlineData("9912")]
        [InlineData("9a000000")]
        [InlineData("9b00000000000000")]
        public static void ReadStartArray_InvalidData_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadStartArray());
        }

        [Theory]
        [InlineData("81")]
        [InlineData("830102")]
        [InlineData("9b7fffffffffffffff")] // long.MaxValue
        public static void ReadStartArray_BufferTooSmall_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadStartArray());
        }
    }
}
