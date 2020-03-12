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
            Assert.Equal(CborReaderState.Finished, reader.Peek());
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
            Assert.Equal(CborReaderState.Finished, reader.Peek());
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
            Assert.Equal(CborReaderState.Finished, reader.Peek());
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
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData("01020304", "4401020304")]
        [InlineData("ffffffffffffffffffffffffffff", "4effffffffffffffffffffffffffff")]
        public static void TryReadByteString_BufferTooSmall_ShouldReturnFalse(string actualValue, string hexEncoding)
        {
            byte[] buffer = new byte[actualValue.Length / 2 - 1];
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            bool result = reader.TryReadByteString(buffer, out int bytesWritten);
            Assert.False(result);
            Assert.Equal(0, bytesWritten);
            Assert.All(buffer, (b => Assert.Equal(0, b)));
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
            char[] buffer = new char[actualValue.Length - 1];
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            bool result = reader.TryReadTextString(buffer, out int charsWritten);
            Assert.False(result);
            Assert.Equal(0, charsWritten);
            Assert.All(buffer, (b => Assert.Equal(0, '\0')));
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<InvalidOperationException>(() => reader.ReadByteString());
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            
            Assert.Throws<InvalidOperationException>(() => reader.ReadTextString());
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
            byte[] data = hexEncoding.HexToByteArray();
            byte[] buffer = new byte[32];
            var reader = new CborReader(data);

            Assert.Throws<InvalidOperationException>(() => reader.TryReadByteString(buffer, out int _));
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
            byte[] data = hexEncoding.HexToByteArray();
            char[] buffer = new char[32];
            var reader = new CborReader(data);

            Assert.Throws<InvalidOperationException>(() => reader.TryReadTextString(buffer, out int _));
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<FormatException>(() => reader.ReadByteString());
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<FormatException>(() => reader.ReadTextString());
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
            byte[] data = hexEncoding.HexToByteArray();
            byte[] buffer = new byte[32];
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.TryReadByteString(buffer, out int _));
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<OverflowException>(() => reader.ReadByteString());
        }

        [Theory]
        // the input strings are not valid CBOR, however want the reader to throw as soon as the length has been read
        [InlineData("7b0000000100000000ff")]
        [InlineData("7bffffffffffffffff")]
        public static void ReadTextString_StringLengthTooLarge_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<OverflowException>(() => reader.ReadTextString());
        }

        [Theory]
        [InlineData("61ff")]
        [InlineData("62f090")]
        public static void ReadTextString_InvalidUnicode_ShouldThrowDecoderFallbackException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<System.Text.DecoderFallbackException>(() => reader.ReadTextString());
        }

        [Theory]
        [InlineData("61ff")]
        [InlineData("62f090")]
        public static void TryReadTextString_InvalidUnicode_ShouldThrowDecoderFallbackException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            char[] buffer = new char[32];
            var reader = new CborReader(data);

            Assert.Throws<System.Text.DecoderFallbackException>(() => reader.TryReadTextString(buffer, out int _));
        }

        [Fact]
        public static void ReadTextString_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadTextString());
        }

        [Fact]
        public static void ReadByteString_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadByteString());
        }
    }
}
