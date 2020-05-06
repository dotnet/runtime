// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Linq;
using System.Text;
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
        [InlineData("", "60")]
        [InlineData("a", "6161")]
        [InlineData("IETF", "6449455446")]
        [InlineData("\"\\", "62225c")]
        [InlineData("\u00fc", "62c3bc")]
        [InlineData("\u6c34", "63e6b0b4")]
        [InlineData("\x3bb", "62cebb")]
        [InlineData("\ud800\udd51", "64f0908591")]
        public static void ReadTextString_SingleValue_HappyPath(string expectedValue, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            string actualResult = reader.ReadTextString();
            Assert.Equal(expectedValue, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData("", "60")]
        [InlineData("a", "6161")]
        [InlineData("IETF", "6449455446")]
        [InlineData("\"\\", "62225c")]
        [InlineData("\u00fc", "62c3bc")]
        [InlineData("\u6c34", "63e6b0b4")]
        [InlineData("\x3bb", "62cebb")]
        [InlineData("\ud800\udd51", "64f0908591")]
        public static void TryReadTextString_SingleValue_HappyPath(string expectedValue, string hexEncoding)
        {
            char[] buffer = new char[32];
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            bool result = reader.TryReadTextString(buffer, out int charsWritten);
            Assert.True(result);
            Assert.Equal(expectedValue.Length, charsWritten);
            Assert.Equal(expectedValue.ToCharArray(), buffer[..charsWritten]);
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
            Assert.Equal(CborReaderState.StartByteString, reader.PeekState());
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
            Assert.Equal(CborReaderState.StartByteString, reader.PeekState());

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
        [InlineData(new string[] { }, "7fff")]
        [InlineData(new string[] { "" }, "7f60ff")]
        [InlineData(new string[] { "ab", "" }, "7f62616260ff")]
        [InlineData(new string[] { "ab", "bc", "" }, "7f62616262626360ff")]
        public static void ReadTextString_IndefiniteLength_SingleValue_HappyPath(string[] expectedValues, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Helpers.VerifyValue(reader, expectedValues);
        }

        [Theory]
        [InlineData("", "7fff")]
        [InlineData("", "7f60ff")]
        [InlineData("ab", "7f62616260ff")]
        [InlineData("abbc", "7f62616262626360ff")]
        public static void ReadTextString_IndefiniteLengthConcatenated_SingleValue_HappyPath(string expectedValue, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Equal(CborReaderState.StartTextString, reader.PeekState());
            string actualValue = reader.ReadTextString();
            Assert.Equal(expectedValue, actualValue);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Fact]
        public static void ReadTextString_IndefiniteLengthConcatenated_NestedValues_HappyPath()
        {
            string hexEncoding = "827f62616260ff7f62616260ff";
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            reader.ReadStartArray();
            Assert.Equal("ab", reader.ReadTextString());
            Assert.Equal("ab", reader.ReadTextString());
            reader.ReadEndArray();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData("", "7fff")]
        [InlineData("", "7f60ff")]
        [InlineData("ab", "7f62616260ff")]
        [InlineData("abbc", "7f62616262626360ff")]
        public static void TryReadTextString_IndefiniteLengthConcatenated_SingleValue__HappyPath(string expectedValue, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Equal(CborReaderState.StartTextString, reader.PeekState());

            Span<char> buffer = new char[32];
            bool result = reader.TryReadTextString(buffer, out int charsWritten);

            Assert.True(result);
            Assert.Equal(expectedValue.Length, charsWritten);
            Assert.Equal(expectedValue, new string(buffer.Slice(0, charsWritten)));
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
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
        [InlineData("a", "6161")]
        [InlineData("IETF", "6449455446")]
        [InlineData("\"\\", "62225c")]
        [InlineData("\u00fc", "62c3bc")]
        [InlineData("\u6c34", "63e6b0b4")]
        [InlineData("\x3bb", "62cebb")]
        [InlineData("\ud800\udd51", "64f0908591")]
        public static void TryReadTextString_BufferTooSmall_ShouldReturnFalse(string actualValue, string hexEncoding)
        {
            char[] buffer = new char[actualValue.Length];
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            bool result = reader.TryReadTextString(buffer.AsSpan(1), out int charsWritten);
            Assert.False(result);
            Assert.Equal(0, charsWritten);
            Assert.All(buffer, (b => Assert.Equal(0, '\0')));

            // ensure that reader is still able to complete the read operation if a large enough buffer is supplied subsequently
            result = reader.TryReadTextString(buffer, out charsWritten);
            Assert.True(result);
            Assert.Equal(actualValue.Length, charsWritten);
            Assert.Equal(actualValue, new string(buffer.AsSpan(0, charsWritten)));
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
        [InlineData("ab", "7f62616260ff")]
        [InlineData("abbc", "7f62616262626360ff")]
        public static void TryReadTextString_IndefiniteLengthConcatenated_BufferTooSmall_ShouldReturnFalse(string expectedValue, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            char[] buffer = new char[expectedValue.Length];
            bool result = reader.TryReadTextString(buffer.AsSpan(1), out int charsWritten);

            Assert.False(result);
            Assert.Equal(0, charsWritten);
            Assert.All(buffer, (b => Assert.Equal(0, '\0')));

            // ensure that reader is still able to perform the read operation if a large enough buffer is supplied subsequently
            result = reader.TryReadTextString(buffer, out charsWritten);
            Assert.True(result);
            Assert.Equal(expectedValue.Length, charsWritten);
            Assert.Equal(expectedValue, new string(buffer.AsSpan(0, charsWritten)));
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, "5800")]
        [InlineData(CborConformanceLevel.Lax, "590000")]
        [InlineData(CborConformanceLevel.Lax, "5a00000000")]
        [InlineData(CborConformanceLevel.Lax, "5b0000000000000000")]
        [InlineData(CborConformanceLevel.Strict, "5800")]
        [InlineData(CborConformanceLevel.Strict, "590000")]
        [InlineData(CborConformanceLevel.Strict, "5a00000000")]
        [InlineData(CborConformanceLevel.Strict, "5b0000000000000000")]
        public static void ReadByteString_NonCanonicalLengths_SupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            byte[] value = reader.ReadByteString();
            Assert.Equal(Array.Empty<byte>(), value);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "5800")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "590000")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "5a00000000")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "5b0000000000000000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "5800")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "590000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "5a00000000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "5b0000000000000000")]
        public static void ReadByteString_NonCanonicalLengths_UnSupportedConformanceLevel_ShouldThrowFormatException(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Assert.Throws<FormatException>(() => reader.ReadByteString());
            Assert.Equal(0, reader.BytesRead);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, "7800")]
        [InlineData(CborConformanceLevel.Lax, "790000")]
        [InlineData(CborConformanceLevel.Lax, "7a00000000")]
        [InlineData(CborConformanceLevel.Lax, "7b0000000000000000")]
        [InlineData(CborConformanceLevel.Strict, "7800")]
        [InlineData(CborConformanceLevel.Strict, "790000")]
        [InlineData(CborConformanceLevel.Strict, "7a00000000")]
        [InlineData(CborConformanceLevel.Strict, "7b0000000000000000")]
        public static void ReadTextString_NonCanonicalLengths_SupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            string value = reader.ReadTextString();
            Assert.Equal("", value);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "7800")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "790000")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "7a00000000")]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "7b0000000000000000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "7800")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "790000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "7a00000000")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "7b0000000000000000")]
        public static void ReadTextString_NonCanonicalLengths_UnSupportedConformanceLevel_ShouldThrowFormatException(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Assert.Throws<FormatException>(() => reader.ReadTextString());
            Assert.Equal(0, reader.BytesRead);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, "5f40ff")]
        [InlineData(CborConformanceLevel.Strict, "5f40ff")]
        public static void ReadByteString_IndefiniteLength_SupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            reader.ReadStartByteStringIndefiniteLength();
            reader.ReadByteString();
            reader.ReadEndByteStringIndefiniteLength();
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, "5f40ff")]
        [InlineData(CborConformanceLevel.Strict, "5f40ff")]
        public static void ReadByteString_IndefiniteLength_AsSingleItem_SupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            reader.ReadByteString();
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "5f40ff")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "5f40ff")]
        public static void ReadByteString_IndefiniteLength_UnSupportedConformanceLevel_ShouldThrowFormatExceptoin(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Assert.Throws<FormatException>(() => reader.ReadStartByteStringIndefiniteLength());
            Assert.Equal(0, reader.BytesRead);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "5f40ff")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "5f40ff")]
        public static void ReadByteString_IndefiniteLength_AsSingleItem_UnSupportedConformanceLevel_ShouldThrowFormatException(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Assert.Throws<FormatException>(() => reader.ReadByteString());
            Assert.Equal(0, reader.BytesRead);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, "7f60ff")]
        [InlineData(CborConformanceLevel.Strict, "7f60ff")]
        public static void ReadTextString_IndefiniteLength_SupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            reader.ReadStartTextStringIndefiniteLength();
            reader.ReadTextString();
            reader.ReadEndTextStringIndefiniteLength();
        }

        [Theory]
        [InlineData(CborConformanceLevel.Lax, "7f60ff")]
        [InlineData(CborConformanceLevel.Strict, "7f60ff")]
        public static void ReadTextString_IndefiniteLength_AsSingleItem_SupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            reader.ReadTextString();
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "7f60ff")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "7f60ff")]
        public static void ReadTextString_IndefiniteLength_UnSupportedConformanceLevel_ShouldThrowFormatExceptoin(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Assert.Throws<FormatException>(() => reader.ReadStartTextStringIndefiniteLength());
            Assert.Equal(0, reader.BytesRead);
        }

        [Theory]
        [InlineData(CborConformanceLevel.Rfc7049Canonical, "7f60ff")]
        [InlineData(CborConformanceLevel.Ctap2Canonical, "7f60ff")]
        public static void ReadTextString_IndefiniteLength_AsSingleItem_UnSupportedConformanceLevel_ShouldThrowFormatException(CborConformanceLevel level, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, level);
            Assert.Throws<FormatException>(() => reader.ReadTextString());
            Assert.Equal(0, reader.BytesRead);
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
        [InlineData("40")] // empty byte string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadTextString_InvalidType_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            
            Assert.Throws<InvalidOperationException>(() => reader.ReadTextString());
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
        [InlineData("00")] // 0
        [InlineData("20")] // -1
        [InlineData("40")] // empty byte string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void TryReadTextString_InvalidType_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            char[] buffer = new char[32];
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.TryReadTextString(buffer, out int _));
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
        public static void ReadByteString_InvalidData_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<FormatException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        // Invalid initial bytes with byte string major type
        [InlineData("7c")]
        [InlineData("7d")]
        [InlineData("7e")]
        // valid initial bytes missing required length data
        [InlineData("78")]
        [InlineData("7912")]
        [InlineData("7a000000")]
        [InlineData("7b00000000000000")]
        // valid string length data missing required bytes
        [InlineData("61")]
        [InlineData("62ff")]
        [InlineData("7803ffff")]
        [InlineData("790100ff")]
        [InlineData("7a00010000ff")]
        public static void ReadTextString_InvalidData_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<FormatException>(() => reader.ReadTextString());
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
        public static void TryReadByteString_InvalidData_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            byte[] buffer = new byte[32];
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.TryReadByteString(buffer, out int _));
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        // Invalid initial bytes with byte string major type
        [InlineData("7c")]
        [InlineData("7d")]
        [InlineData("7e")]
        // valid initial bytes missing required length data
        [InlineData("78")]
        [InlineData("7912")]
        [InlineData("7a000000")]
        [InlineData("7b00000000000000")]
        // valid string length data missing required bytes
        [InlineData("61")]
        [InlineData("62ff")]
        [InlineData("7803ffff")]
        [InlineData("790100ff")]
        [InlineData("7a00010000ff")]
        public static void TryReadTextString_InvalidData_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            char[] buffer = new char[32];
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.TryReadTextString(buffer, out int _));
        }

        [Theory]
        // the input strings are not valid CBOR, however want the reader to throw as soon as the length has been read
        [InlineData("5b0000000100000000ff")]
        [InlineData("5bffffffffffffffff")]
        public static void ReadByteString_StringLengthTooLarge_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<OverflowException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        // the input strings are not valid CBOR, however want the reader to throw as soon as the length has been read
        [InlineData("7b0000000100000000ff")]
        [InlineData("7bffffffffffffffff")]
        public static void ReadTextString_StringLengthTooLarge_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<OverflowException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("61ff")]
        [InlineData("62f090")]
        public static void ReadTextString_InvalidUnicode_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            FormatException exn = Assert.Throws<FormatException>(() => reader.ReadTextString());
            Assert.NotNull(exn.InnerException);
            Assert.IsType<System.Text.DecoderFallbackException>(exn.InnerException);
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("61ff")]
        [InlineData("62f090")]
        public static void TryReadTextString_InvalidUnicode_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            char[] buffer = new char[32];
            var reader = new CborReader(encoding);

            FormatException exn = Assert.Throws<FormatException>(() => reader.TryReadTextString(buffer, out int _));
            Assert.NotNull(exn.InnerException);
            Assert.IsType<System.Text.DecoderFallbackException>(exn.InnerException);
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_IndefiniteLength_ContainingInvalidMajorTypes_ShouldThrowFormatException()
        {
            string hexEncoding = "5f4001ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartByteStringIndefiniteLength();
            reader.ReadByteString();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Equal(CborReaderState.FormatError, reader.PeekState());
            // throws FormatException even if it's the right major type we're trying to read
            Assert.Throws<FormatException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLength_ContainingInvalidMajorTypes_ShouldThrowFormatException()
        {
            string hexEncoding = "7f6001ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartTextStringIndefiniteLength();
            reader.ReadTextString();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Equal(CborReaderState.FormatError, reader.PeekState());
            // throws FormatException even if it's the right major type we're trying to read
            Assert.Throws<FormatException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_IndefiniteLength_ContainingNestedIndefiniteLengthStrings_ShouldThrowFormatException()
        {
            string hexEncoding = "5f5fffff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartByteStringIndefiniteLength();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadStartByteStringIndefiniteLength());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_IndefiniteLengthConcatenated_ContainingNestedIndefiniteLengthStrings_ShouldThrowFormatException()
        {
            string hexEncoding = "5f5fffff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLength_ContainingNestedIndefiniteLengthStrings_ShouldThrowFormatException()
        {
            string hexEncoding = "7f7fffff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartTextStringIndefiniteLength();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadStartTextStringIndefiniteLength());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLengthConcatenated_ContainingNestedIndefiniteLengthStrings_ShouldThrowFormatException()
        {
            string hexEncoding = "7f7fffff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadByteString_IndefiniteLengthConcatenated_ContainingInvalidMajorTypes_ShouldThrowFormatException()
        {
            string hexEncoding = "5f4001ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<FormatException>(() => reader.ReadByteString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLengthConcatenated_ContainingInvalidMajorTypes_ShouldThrowFormatException()
        {
            string hexEncoding = "7f6001ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<FormatException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLengthConcatenated_InvalidUtf8Chunks_ShouldThrowFormatException()
        {
            // while the concatenated string is valid utf8, the individual chunks are not,
            // which is in violation of the CBOR format.

            string hexEncoding = "7f62f090628591ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<FormatException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }
    }
}
