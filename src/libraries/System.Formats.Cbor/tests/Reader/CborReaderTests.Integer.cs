// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborReaderTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

        [Theory]
        [InlineData(0, "00")]
        [InlineData(1, "01")]
        [InlineData(10, "0a")]
        [InlineData(23, "17")]
        [InlineData(24, "1818")]
        [InlineData(25, "1819")]
        [InlineData(100, "1864")]
        [InlineData(1000, "1903e8")]
        [InlineData(1000000, "1a000f4240")]
        [InlineData(1000000000000, "1b000000e8d4a51000")]
        [InlineData(-1, "20")]
        [InlineData(-10, "29")]
        [InlineData(-24, "37")]
        [InlineData(-100, "3863")]
        [InlineData(-1000, "3903e7")]
        [InlineData(byte.MaxValue, "18ff")]
        [InlineData(byte.MaxValue + 1, "190100")]
        [InlineData(-1 - byte.MaxValue, "38ff")]
        [InlineData(-2 - byte.MaxValue, "390100")]
        [InlineData(ushort.MaxValue, "19ffff")]
        [InlineData(ushort.MaxValue + 1, "1a00010000")]
        [InlineData(-1 - ushort.MaxValue, "39ffff")]
        [InlineData(-2 - ushort.MaxValue, "3a00010000")]
        [InlineData(uint.MaxValue, "1affffffff")]
        [InlineData((long)uint.MaxValue + 1, "1b0000000100000000")]
        [InlineData(-1 - uint.MaxValue, "3affffffff")]
        [InlineData(-2 - uint.MaxValue, "3b0000000100000000")]
        [InlineData(long.MinValue, "3b7fffffffffffffff")]
        [InlineData(long.MaxValue, "1b7fffffffffffffff")]
        public static void ReadInt64_SingleValue_HappyPath(long expectedResult, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            long actualResult = reader.ReadInt64();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(0, "00")]
        [InlineData(1, "01")]
        [InlineData(10, "0a")]
        [InlineData(23, "17")]
        [InlineData(24, "1818")]
        [InlineData(25, "1819")]
        [InlineData(100, "1864")]
        [InlineData(1000, "1903e8")]
        [InlineData(1000000, "1a000f4240")]
        [InlineData(-1, "20")]
        [InlineData(-10, "29")]
        [InlineData(-100, "3863")]
        [InlineData(-1000, "3903e7")]
        [InlineData(byte.MaxValue, "18ff")]
        [InlineData(byte.MaxValue + 1, "190100")]
        [InlineData(-1 - byte.MaxValue, "38ff")]
        [InlineData(-2 - byte.MaxValue, "390100")]
        [InlineData(ushort.MaxValue, "19ffff")]
        [InlineData(ushort.MaxValue + 1, "1a00010000")]
        [InlineData(-1 - ushort.MaxValue, "39ffff")]
        [InlineData(-2 - ushort.MaxValue, "3a00010000")]
        [InlineData(int.MaxValue, "1a7fffffff")]
        [InlineData(int.MinValue, "3a7fffffff")]
        public static void ReadInt32_SingleValue_HappyPath(int expectedResult, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            long actualResult = reader.ReadInt32();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(0, "00")]
        [InlineData(1, "01")]
        [InlineData(10, "0a")]
        [InlineData(23, "17")]
        [InlineData(24, "1818")]
        [InlineData(25, "1819")]
        [InlineData(100, "1864")]
        [InlineData(1000, "1903e8")]
        [InlineData(1000000, "1a000f4240")]
        [InlineData(1000000000000, "1b000000e8d4a51000")]
        [InlineData(byte.MaxValue, "18ff")]
        [InlineData(byte.MaxValue + 1, "190100")]
        [InlineData(ushort.MaxValue, "19ffff")]
        [InlineData(ushort.MaxValue + 1, "1a00010000")]
        [InlineData(uint.MaxValue, "1affffffff")]
        [InlineData((ulong)uint.MaxValue + 1, "1b0000000100000000")]
        [InlineData(long.MaxValue, "1b7fffffffffffffff")]
        [InlineData(ulong.MaxValue, "1bffffffffffffffff")]
        public static void ReadUInt64_SingleValue_HappyPath(ulong expectedResult, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            ulong actualResult = reader.ReadUInt64();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(0, "00")]
        [InlineData(1, "01")]
        [InlineData(10, "0a")]
        [InlineData(23, "17")]
        [InlineData(24, "1818")]
        [InlineData(25, "1819")]
        [InlineData(100, "1864")]
        [InlineData(1000, "1903e8")]
        [InlineData(1000000, "1a000f4240")]
        [InlineData(byte.MaxValue, "18ff")]
        [InlineData(byte.MaxValue + 1, "190100")]
        [InlineData(ushort.MaxValue, "19ffff")]
        [InlineData(ushort.MaxValue + 1, "1a00010000")]
        [InlineData(int.MaxValue, "1a7fffffff")]
        [InlineData(uint.MaxValue, "1affffffff")]
        public static void ReadUInt32_SingleValue_HappyPath(uint expectedResult, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            uint actualResult = reader.ReadUInt32();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(0, "20")]
        [InlineData(9, "29")]
        [InlineData(23, "37")]
        [InlineData(99, "3863")]
        [InlineData(999, "3903e7")]
        [InlineData(byte.MaxValue, "38ff")]
        [InlineData(ushort.MaxValue, "39ffff")]
        [InlineData(uint.MaxValue, "3affffffff")]
        [InlineData(ulong.MaxValue, "3bffffffffffffffff")]
        public static void ReadCborNegativeIntegerRepresentation_SingleValue_HappyPath(ulong expectedResult, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            ulong actualResult = reader.ReadCborNegativeIntegerRepresentation();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax, "1817", 23)]
        [InlineData(CborConformanceMode.Lax, "1900ff", byte.MaxValue)]
        [InlineData(CborConformanceMode.Lax, "1a0000ffff", ushort.MaxValue)]
        [InlineData(CborConformanceMode.Lax, "1b00000000ffffffff", uint.MaxValue)]
        [InlineData(CborConformanceMode.Lax, "1b0000000000000001", 1)]
        [InlineData(CborConformanceMode.Strict, "1817", 23)]
        [InlineData(CborConformanceMode.Strict, "1900ff", byte.MaxValue)]
        [InlineData(CborConformanceMode.Strict, "1a0000ffff", ushort.MaxValue)]
        [InlineData(CborConformanceMode.Strict, "1b00000000ffffffff", uint.MaxValue)]
        [InlineData(CborConformanceMode.Strict, "1b0000000000000001", 1)]
        public static void ReadUInt64_NonCanonicalEncodings_SupportedConformanceMode_ShouldSucceed(CborConformanceMode mode, string hexEncoding, ulong expectedValue)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data, mode);
            ulong result = reader.ReadUInt64();
            Assert.Equal(expectedValue, result);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical, "1817")]
        [InlineData(CborConformanceMode.Canonical, "1900ff")]
        [InlineData(CborConformanceMode.Canonical, "1a0000ffff")]
        [InlineData(CborConformanceMode.Canonical, "1b00000000ffffffff")]
        [InlineData(CborConformanceMode.Canonical, "1b0000000000000001")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "1817")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "1900ff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "1a0000ffff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "1b00000000ffffffff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "1b0000000000000001")]
        public static void ReadUInt64_NonCanonicalEncodings_UnSupportedConformanceMode_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data, mode);
            Assert.Throws<CborContentException>(() => reader.ReadUInt64());
            Assert.Equal(data.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(CborConformanceMode.Lax, "3817", -24)]
        [InlineData(CborConformanceMode.Lax, "3900ff", -1 - byte.MaxValue)]
        [InlineData(CborConformanceMode.Lax, "3a0000ffff", -1 - ushort.MaxValue)]
        [InlineData(CborConformanceMode.Lax, "3b00000000ffffffff", -1 - uint.MaxValue)]
        [InlineData(CborConformanceMode.Lax, "3b0000000000000000", -1)]
        [InlineData(CborConformanceMode.Strict, "3817", -24)]
        [InlineData(CborConformanceMode.Strict, "3900ff", -1 - byte.MaxValue)]
        [InlineData(CborConformanceMode.Strict, "3a0000ffff", -1 - ushort.MaxValue)]
        [InlineData(CborConformanceMode.Strict, "3b00000000ffffffff", -1 - uint.MaxValue)]
        [InlineData(CborConformanceMode.Strict, "3b0000000000000000", -1)]
        public static void ReadInt64_NonCanonicalEncodings_SupportedConformanceMode_ShouldSucceed(CborConformanceMode mode, string hexEncoding, long expectedValue)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data, mode);
            long result = reader.ReadInt64();
            Assert.Equal(expectedValue, result);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData(CborConformanceMode.Canonical, "3817")]
        [InlineData(CborConformanceMode.Canonical, "3900ff")]
        [InlineData(CborConformanceMode.Canonical, "3a0000ffff")]
        [InlineData(CborConformanceMode.Canonical, "3b00000000ffffffff")]
        [InlineData(CborConformanceMode.Canonical, "3b0000000000000001")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "3817")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "3900ff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "3a0000ffff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "3b00000000ffffffff")]
        [InlineData(CborConformanceMode.Ctap2Canonical, "3b0000000000000001")]
        public static void ReadInt64_NonCanonicalEncodings_UnSupportedConformanceMode_ShouldThrowCborContentException(CborConformanceMode mode, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data, mode);
            Assert.Throws<CborContentException>(() => reader.ReadInt64());
            Assert.Equal(data.Length, reader.BytesRemaining);
        }


        [Theory]
        [InlineData("1b8000000000000000")] // long.MaxValue + 1
        [InlineData("3b8000000000000000")] // long.MinValue - 1
        [InlineData("1bffffffffffffffff")] // ulong.MaxValue
        public static void ReadInt64_OutOfRangeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<OverflowException>(() => reader.ReadInt64());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("1a80000000")]         // int.MaxValue + 1
        [InlineData("3a80000000")]         // int.MinValue - 1
        [InlineData("1b8000000000000000")] // long.MaxValue + 1
        [InlineData("3a8000000000000000")] // long.MinValue - 1
        [InlineData("1bffffffffffffffff")] // ulong.MaxValue
        public static void ReadInt32_OutOfRangeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<OverflowException>(() => reader.ReadInt32());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("20")]
        [InlineData("1b0000000100000000")] // uint.MaxValue + 1
        public static void ReadUInt32_OutOfRangeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<OverflowException>(() => reader.ReadUInt32());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("20")] // -1
        [InlineData("3863")] // -100
        [InlineData("3b7fffffffffffffff")] // long.MinValue
        public static void ReadUInt64_OutOfRangeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<OverflowException>(() => reader.ReadUInt64());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadInt64_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadInt32_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadInt32());

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadUInt32_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadUInt32());

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("40")] // empty byte string
        [InlineData("60")] // empty text string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadUInt64_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadUInt64());

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("00")] // 0
        [InlineData("17")] // 23
        [InlineData("40")] // empty byte string
        [InlineData("60")] // empty text string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadCborNegativeIntegerRepresentation_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadCborNegativeIntegerRepresentation());

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        // Invalid initial bytes with numeric major type
        [InlineData("1c")]
        [InlineData("1d")]
        [InlineData("1e")]
        [InlineData("3c")]
        [InlineData("3d")]
        [InlineData("3e")]
        // valid initial bytes missing required data
        [InlineData("18")]
        [InlineData("1912")]
        [InlineData("1a000000")]
        [InlineData("1b00000000000000")]
        [InlineData("38")]
        [InlineData("3912")]
        [InlineData("3a000000")]
        [InlineData("3b00000000000000")]
        public static void ReadInt64_InvalidData_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<CborContentException>(() => reader.ReadInt64());

            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        // Invalid initial bytes with numeric major type
        [InlineData("3c")]
        [InlineData("3d")]
        [InlineData("3e")]
        // valid initial bytes missing required data
        [InlineData("38")]
        [InlineData("3912")]
        [InlineData("3a000000")]
        [InlineData("3b00000000000000")]
        public static void ReadCborNegativeIntegerRepresentation_InvalidData_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.ReadCborNegativeIntegerRepresentation());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("1f")]
        [InlineData("3f")]
        public static void ReadInt64_IndefiniteLengthIntegers_ShouldThrowCborContentException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.ReadInt64());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadUInt64_EmptyBuffer_ShouldThrowCborContentException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.ReadUInt64());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadCborNegativeIntegerRepresentation_EmptyBuffer_ShouldThrowCborContentException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<CborContentException>(() => reader.ReadCborNegativeIntegerRepresentation());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }
    }
}
