// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
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
            Assert.Equal(expectedValue.ToCharArray(), buffer.Take(charsWritten));
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
            Assert.Equal(CborReaderState.StartIndefiniteLengthTextString, reader.PeekState());
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
            Assert.Equal(CborReaderState.StartIndefiniteLengthTextString, reader.PeekState());

            Span<char> buffer = new char[32];
            bool result = reader.TryReadTextString(buffer, out int charsWritten);

            Assert.True(result);
            Assert.Equal(expectedValue.Length, charsWritten);
            Assert.Equal(expectedValue, new string(buffer.Slice(0, charsWritten)
#if !NETCOREAPP
.ToArray()
#endif
                ));
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
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
            Assert.Equal(actualValue, new string(buffer.AsSpan(0, charsWritten)
#if !NETCOREAPP
.ToArray()
#endif
                ));
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
            Assert.Equal(expectedValue, new string(buffer.AsSpan(0, charsWritten)
#if !NETCOREAPP
.ToArray()
#endif
                ));
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
        public static void ReadDefiniteLengthTextStringBytes_SingleValue_HappyPath(string expectedValue, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            ReadOnlyMemory<byte> resultBytes = reader.ReadDefiniteLengthTextStringBytes();
            string result = Encoding.UTF8.GetString(resultBytes.Span
#if !NETCOREAPP
.ToArray()
#endif
                );
            Assert.Equal(expectedValue, result);
            Assert.Equal(0, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("7fff")]
        [InlineData("7f60ff")]
        public static void ReadDefiniteLengthTextStringBytes_IndefiniteLengthInput_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadDefiniteLengthTextStringBytes());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
            reader.ReadTextString(); // regular byte string reader should still succeed
        }

        [Fact]
        public static void ReadDefiniteLengthTextStringBytes_InvalidUtf8_LaxConformance_ShouldSucceed()
        {
            byte[] encoding = "62f090".HexToByteArray();
            var reader = new CborReader(encoding, CborConformanceMode.Lax);

            ReadOnlyMemory<byte> bytes = reader.ReadDefiniteLengthTextStringBytes();
            AssertHelper.HexEqual("f090".HexToByteArray(), bytes);
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict)]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void ReadDefiniteLengthTextStringBytes_InvalidUtf8_StrictConformance_ShouldThrowCborContentException(CborConformanceMode mode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            var reader = new CborReader(encoding, mode);

            Exception exn = Assert.Throws<CborContentException>(() => reader.ReadDefiniteLengthTextStringBytes());
            Assert.IsType<DecoderFallbackException>(exn.InnerException);
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }


        [Theory]
        [InlineData(CborConformanceMode.Lax, "7800")]
        [InlineData(CborConformanceMode.Lax, "790000")]
        [InlineData(CborConformanceMode.Lax, "7a00000000")]
        [InlineData(CborConformanceMode.Lax, "7b0000000000000000")]
        [InlineData(CborConformanceMode.Strict, "7800")]
        [InlineData(CborConformanceMode.Strict, "790000")]
        [InlineData(CborConformanceMode.Strict, "7a00000000")]
        [InlineData(CborConformanceMode.Strict, "7b0000000000000000")]
        public static void ReadTextString_NonCanonicalLengths_SupportedConformanceMode_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            string value = reader.ReadTextString();
            Assert.Equal("", value);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical, "7800")]
        [InlineData(CborConformanceMode.Canonical, "790000")]
        [InlineData(CborConformanceMode.Canonical, "7a00000000")]
        [InlineData(CborConformanceMode.Canonical, "7b0000000000000000")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "7800")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "790000")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "7a00000000")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "7b0000000000000000")]
        public static void ReadTextString_NonCanonicalLengths_UnSupportedConformanceMode_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            Assert.Throws<CborContentException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax, "7f60ff")]
        [InlineData(CborConformanceMode.Strict, "7f60ff")]
        public static void ReadTextString_IndefiniteLength_SupportedConformanceMode_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            reader.ReadStartIndefiniteLengthTextString();
            reader.ReadTextString();
            reader.ReadEndIndefiniteLengthTextString();
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax, "7f60ff")]
        [InlineData(CborConformanceMode.Strict, "7f60ff")]
        public static void ReadTextString_IndefiniteLength_AsSingleItem_SupportedConformanceMode_ShouldSucceed(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            reader.ReadTextString();
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical, "7f60ff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "7f60ff")]
        public static void ReadTextString_IndefiniteLength_UnSupportedConformanceMode_ShouldThrowFormatExceptoin(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            Assert.Throws<CborContentException>(() => reader.ReadStartIndefiniteLengthTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical, "7f60ff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "7f60ff")]
        public static void ReadTextString_IndefiniteLength_AsSingleItem_UnSupportedConformanceMode_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, mode);
            Assert.Throws<CborContentException>(() => reader.ReadTextString());
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
        public static void ReadTextString_InvalidData_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadTextString());
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
        public static void TryReadTextString_InvalidData_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            char[] buffer = new char[32];
            var reader = new CborReader(data);

            Assert.Throws<CborContentException>(() => reader.TryReadTextString(buffer, out int _));
        }

        [Theory]
        [InlineData("7b0000000100000000ff")]
        [InlineData("7bffffffffffffffff")]
        public static void ReadTextString_StringLengthTooLarge_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax)]
        public static void ReadTextString_InvalidUtf8_LaxConformance_ShouldSucceed(CborConformanceMode conformanceMode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            string expected = "\ufffd"; // unicode replacement character

            var reader = new CborReader(encoding, conformanceMode);
            string actual = reader.ReadTextString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict)]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void ReadTextString_InvalidUtf8_StrictConformance_ShouldThrowCborContentException(CborConformanceMode conformanceMode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            var reader = new CborReader(encoding, conformanceMode);
            CborContentException exn = Assert.Throws<CborContentException>(() => reader.ReadTextString());
            Assert.NotNull(exn.InnerException);
            Assert.IsType<System.Text.DecoderFallbackException>(exn.InnerException);
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax)]
        public static void TryReadTextString_InvalidUtf8_LaxConformance_ShouldSucceed(CborConformanceMode conformanceMode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            string expected = "\ufffd"; // unicode replacement character

            char[] buffer = new char[32];
            var reader = new CborReader(encoding, conformanceMode);

            bool result = reader.TryReadTextString(buffer, out int bytesRead);

            Assert.True(result);
            Assert.Equal(1, bytesRead);
            Assert.Equal(buffer[0], expected[0]);
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict)]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void TryReadTextString_InvalidUtf8_StrictConformance_ShouldThrowCborContentException(CborConformanceMode conformanceMode)
        {
            byte[] encoding = "62f090".HexToByteArray();
            char[] buffer = new char[32];
            var reader = new CborReader(encoding, conformanceMode);

            CborContentException exn = Assert.Throws<CborContentException>(() => reader.TryReadTextString(buffer, out int _));
            Assert.NotNull(exn.InnerException);
            Assert.IsType<System.Text.DecoderFallbackException>(exn.InnerException);
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_EmptyBuffer_ShouldThrowCborContentException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLength_ContainingInvalidMajorTypes_ShouldThrowCborContentException()
        {
            string hexEncoding = "7f6001ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            reader.ReadStartIndefiniteLengthTextString();
            reader.ReadTextString();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<CborContentException>(() => reader.PeekState());
            Assert.Throws<CborContentException>(() => reader.ReadInt64());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLength_ContainingNestedIndefiniteLengthStrings_ShouldThrowCborContentException()
        {
            string hexEncoding = "7f7fffff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartIndefiniteLengthTextString();

            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<CborContentException>(() => reader.ReadStartIndefiniteLengthTextString());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLengthConcatenated_ContainingNestedIndefiniteLengthStrings_ShouldThrowCborContentException()
        {
            string hexEncoding = "7f7fffff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTextString_IndefiniteLengthConcatenated_ContainingInvalidMajorTypes_ShouldThrowCborContentException()
        {
            string hexEncoding = "7f6001ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax)]
        public static void ReadTextString_IndefiniteLengthConcatenated_InvalidUtf8Chunks_LaxConformance_ShouldSucceed(CborConformanceMode conformanceMode)
        {
            // while the concatenated string is valid utf8, the individual chunks are not,
            // which is in violation of the CBOR format.

            string hexEncoding = "7f62f090628591ff";
            string expected = "\ufffd\ufffd\ufffd";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, conformanceMode);
            string actual = reader.ReadTextString();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(CborConformanceMode.Strict)]
        [InlineData(CborConformanceMode.Canonical)]
        [InlineData(CborConformanceMode.Ctap2Canonical)]
        public static void ReadTextString_IndefiniteLengthConcatenated_InvalidUtf8Chunks_StrictConformance_ShouldThrowCborContentException(CborConformanceMode conformanceMode)
        {
            // while the concatenated string is valid utf8, the individual chunks are not,
            // which is in violation of the CBOR format.

            string hexEncoding = "7f62f090628591ff";
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding, conformanceMode);
            Assert.Throws<CborContentException>(() => reader.ReadTextString());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }
    }
}
