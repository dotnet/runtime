// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A

        [Theory]
        [InlineData("", "40")]
        [InlineData("01020304", "4401020304")]
        [InlineData("ffffffffffffffffffffffffffff", "4effffffffffffffffffffffffffff")]
        public static void ReadByteString_SingleValue_HappyPath(string hexExpectedValue, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            byte[] expectedValue = hexExpectedValue.HexToByteArray();
            var reader = new CborReader(encoding);
            byte[] output = reader.ReadByteString();
            Assert.Equal(expectedValue, output);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData("", "40")]
        [InlineData("01020304", "4401020304")]
        [InlineData("ffffffffffffffffffffffffffff", "4effffffffffffffffffffffffffff")]
        public static void TryReadByteString_SingleValue_HappyPath(string hexExpectedValue, string hexEncoding)
        {
            byte[] buffer = new byte[32];
            byte[] encoding = hexEncoding.HexToByteArray();
            byte[] expectedValue = hexExpectedValue.HexToByteArray();
            var reader = new CborReader(encoding);
            bool result = reader.TryReadByteString(buffer, out int bytesWritten);
            Assert.True(result);
            Assert.Equal(expectedValue.Length, bytesWritten);
            Assert.Equal(expectedValue, buffer[..bytesWritten]);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(new string[] { }, "5fff")]
        [InlineData(new string[] { "" }, "5f40ff")]
        [InlineData(new string[] { "ab", "" }, "5f41ab40ff")]
        [InlineData(new string[] { "ab", "bc", "" }, "5f41ab41bc40ff")]
        public static void ReadByteString_IndefiniteLength_SingleValue_HappyPath(string[] expectedHexValues, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            byte[][] expectedValues = expectedHexValues.Select(x => x.HexToByteArray()).ToArray();
            var reader = new CborReader(data);
            Helpers.VerifyValue(reader, expectedValues);
        }

        [Theory]
        [InlineData("", "5fff")]
        [InlineData("", "5f40ff")]
        [InlineData("ab", "5f41ab40ff")]
        [InlineData("abbc", "5f41ab41bc40ff")]
        public static void ReadByteString_IndefiniteLengthConcatenated_SingleValue_HappyPath(string expectedHexValue, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Equal(CborReaderState.StartIndefiniteLengthByteString, reader.PeekState());
            byte[] actualValue = reader.ReadByteString();
            Assert.Equal(expectedHexValue.ToUpper(), actualValue.ByteArrayToHex());
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData("", "5fff")]
        [InlineData("", "5f40ff")]
        [InlineData("ab", "5f41ab40ff")]
        [InlineData("abbc", "5f41ab41bc40ff")]
        public static void TryReadByteString_IndefiniteLengthConcatenated_SingleValue_HappyPath(string expectedHexValue, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Equal(CborReaderState.StartIndefiniteLengthByteString, reader.PeekState());

            Span<byte> buffer = new byte[32];
            bool result = reader.TryReadByteString(buffer, out int bytesWritten);

            Assert.True(result);
            Assert.Equal(expectedHexValue.Length / 2, bytesWritten);
            Assert.Equal(expectedHexValue.ToUpper(), buffer.Slice(0, bytesWritten).ByteArrayToHex());
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void ReadByteString_IndefiniteLengthConcatenated_NestedValues_HappyPath()
        {
            string hexEncoding = "825f41ab40ff5f41ab40ff";
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            reader.ReadStartArray();
            Assert.Equal("AB", reader.ReadByteString().ByteArrayToHex());
            Assert.Equal("AB", reader.ReadByteString().ByteArrayToHex());
            reader.ReadEndArray();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData("", "40")]
        [InlineData("01020304", "4401020304")]
        [InlineData("ffffffffffffffffffffffffffff", "4effffffffffffffffffffffffffff")]
        public static void ReadByteStringDefiniteLength_SingleValue_HappyPath(string expectedHexValue, string hexEncoding)
        {
            byte[] expectedValue = expectedHexValue.HexToByteArray();
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ReadOnlyMemory<byte> result = reader.ReadDefiniteLengthByteString();
            AssertHelper.HexEqual(expectedValue, result);
            Assert.Equal(0, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("5fff")]
        [InlineData("5f40ff")]
        public static void ReadByteStringDefiniteLength_IndefiniteLengthInput_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadDefiniteLengthByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
            reader.ReadByteString(); // regular byte string reader should still succeed
        }

        [Theory]
        [InlineData("01020304", "4401020304")]
        [InlineData("ffffffffffffffffffffffffffff", "4effffffffffffffffffffffffffff")]
        public static void TryReadByteString_BufferTooSmall_ShouldReturnFalse(string actualValue, string hexEncoding)
        {
            byte[] buffer = new byte[actualValue.Length / 2];
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            bool result = reader.TryReadByteString(buffer.AsSpan(1), out int bytesWritten);
            Assert.False(result);
            Assert.Equal(0, bytesWritten);
            Assert.All(buffer, (b => Assert.Equal(0, b)));

            // ensure that reader is still able to complete the read operation if a large enough buffer is supplied subsequently
            result = reader.TryReadByteString(buffer, out bytesWritten);
            Assert.True(result);
            Assert.Equal(buffer.Length, bytesWritten);
            Assert.Equal(actualValue.ToUpper(), buffer.ByteArrayToHex());
        }

        [Theory]
        [InlineData("ab", "5f41ab40ff")]
        [InlineData("abbc", "5f41ab41bc40ff")]
        public static void TryReadByteString_IndefiniteLengthConcatenated_BufferTooSmall_ShouldReturnFalse(string expectedHexValue, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            byte[] buffer = new byte[expectedHexValue.Length / 2];
            bool result = reader.TryReadByteString(buffer.AsSpan(1), out int bytesWritten);

            Assert.False(result);
            Assert.Equal(0, bytesWritten);
            Assert.All(buffer, (b => Assert.Equal(0, b)));

            // ensure that reader is still able to complete the read operation if a large enough buffer is supplied subsequently
            result = reader.TryReadByteString(buffer, out bytesWritten);
            Assert.True(result);
            Assert.Equal(buffer.Length, bytesWritten);
            Assert.Equal(expectedHexValue.ToUpper(), buffer.ByteArrayToHex());
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax, "5800")]
        [InlineData(CborConformanceMode.Lax, "590000")]
        [InlineData(CborConformanceMode.Lax, "5a00000000")]
        [InlineData(CborConformanceMode.Lax, "5b0000000000000000")]
        [InlineData(CborConformanceMode.Strict, "5800")]
        [InlineData(CborConformanceMode.Strict, "590000")]
        [InlineData(CborConformanceMode.Strict, "5a00000000")]
        [InlineData(CborConformanceMode.Strict, "5b0000000000000000")]
        public static void ReadByteString_NonCanonicalLengths_SupportedConformanceMode_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            byte[] value = reader.ReadByteString();
            Assert.Equal(Array.Empty<byte>(), value);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical, "5800")]
        [InlineData(CborConformanceMode.Canonical, "590000")]
        [InlineData(CborConformanceMode.Canonical, "5a00000000")]
        [InlineData(CborConformanceMode.Canonical, "5b0000000000000000")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "5800")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "590000")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "5a00000000")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "5b0000000000000000")]
        public static void ReadByteString_NonCanonicalLengths_UnSupportedConformanceMode_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            Assert.Throws<CborContentException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax, "5f40ff")]
        [InlineData(CborConformanceMode.Strict, "5f40ff")]
        public static void ReadByteString_IndefiniteLength_SupportedConformanceMode_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            reader.ReadStartIndefiniteLengthByteString();
            reader.ReadByteString();
            reader.ReadEndIndefiniteLengthByteString();
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax, "5f40ff")]
        [InlineData(CborConformanceMode.Strict, "5f40ff")]
        public static void ReadByteString_IndefiniteLength_AsSingleItem_SupportedConformanceMode_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            reader.ReadByteString();
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical, "5f40ff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "5f40ff")]
        public static void ReadByteString_IndefiniteLength_UnSupportedConformanceMode_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            Assert.Throws<CborContentException>(() => reader.ReadStartIndefiniteLengthByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical, "5f40ff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "5f40ff")]
        public static void ReadByteString_IndefiniteLength_AsSingleItem_UnSupportedConformanceMode_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            Assert.Throws<CborContentException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("00")] // 0
        [InlineData("20")] // -1
        [InlineData("60")] // empty text string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadByteString_InvalidType_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("00")] // 0
        [InlineData("20")] // -1
        [InlineData("60")] // empty text string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void TryReadByteString_InvalidType_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            byte[] buffer = new byte[32];
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.TryReadByteString(buffer, out int _));
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        // Invalid initial bytes with byte string major type
        [InlineData("5c")]
        [InlineData("5d")]
        [InlineData("5e")]
        // valid initial bytes missing required length data
        [InlineData("58")]
        [InlineData("5912")]
        [InlineData("5a000000")]
        [InlineData("5b00000000000000")]
        // valid string length data missing required bytes
        [InlineData("41")]
        [InlineData("42ff")]
        [InlineData("5803ffff")]
        [InlineData("590100ff")]
        [InlineData("5a00010000ff")]
        public static void ReadByteString_InvalidData_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        // Invalid initial bytes with byte string major type
        [InlineData("5c")]
        [InlineData("5d")]
        [InlineData("5e")]
        // valid initial bytes missing required length data
        [InlineData("58")]
        [InlineData("5912")]
        [InlineData("5a000000")]
        [InlineData("5b00000000000000")]
        // valid string length data missing required bytes
        [InlineData("41")]
        [InlineData("42ff")]
        [InlineData("5803ffff")]
        [InlineData("590100ff")]
        [InlineData("5a00010000ff")]
        public static void TryReadByteString_InvalidData_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            byte[] buffer = new byte[32];
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.TryReadByteString(buffer, out int _));
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("5b0000000100000000ff")]
        [InlineData("5bffffffffffffffff")]
        public static void ReadByteString_StringLengthTooLarge_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_EmptyBuffer_ShouldThrowCborContentException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_IndefiniteLength_ContainingInvalidMajorTypes_ShouldThrowCborContentException()
        {
            string hexEncoding = "5f4001ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartIndefiniteLengthByteString();
            reader.ReadByteString();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<CborContentException>(() => reader.PeekState());
            Assert.Throws<CborContentException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_IndefiniteLength_ContainingNestedIndefiniteLengthStrings_ShouldThrowCborContentException()
        {
            string hexEncoding = "5f5fffff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartIndefiniteLengthByteString();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<CborContentException>(() => reader.ReadStartIndefiniteLengthByteString());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_IndefiniteLengthConcatenated_ContainingNestedIndefiniteLengthStrings_ShouldThrowCborContentException()
        {
            string hexEncoding = "5f5fffff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_IndefiniteLengthConcatenated_ContainingInvalidMajorTypes_ShouldThrowCborContentException()
        {
            string hexEncoding = "5f4001ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }
    }
}
