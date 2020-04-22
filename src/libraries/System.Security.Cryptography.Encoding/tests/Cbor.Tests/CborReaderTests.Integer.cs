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
            Assert.Equal(CborReaderState.Finished, reader.Peek());
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
            Assert.Equal(CborReaderState.Finished, reader.Peek());
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
        public static void ReadCborNegativeIntegerEncoding_SingleValue_HappyPath(ulong expectedResult, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            ulong actualResult = reader.ReadCborNegativeIntegerEncoding();
            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(2, 2, "c202")]
        [InlineData(0, "2013-03-21T20:04:00Z", "c074323031332d30332d32315432303a30343a30305a")]
        [InlineData(1, 1363896240, "c11a514b67b0")]
        [InlineData(23, new byte[] { 1, 2, 3, 4 }, "d74401020304")]
        [InlineData(32, "http://www.example.com", "d82076687474703a2f2f7777772e6578616d706c652e636f6d")]
        [InlineData(int.MaxValue, 2, "da7fffffff02")]
        [InlineData(ulong.MaxValue, new object[] { 1, 2 }, "dbffffffffffffffff820102")]
        public static void ReadTag_SingleValue_HappyPath(ulong expectedTag, object expectedValue, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Equal(CborReaderState.Tag, reader.Peek());
            CborTag tag = reader.ReadTag();
            Assert.Equal(expectedTag, (ulong)tag);

            Helpers.VerifyValue(reader, expectedValue);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        [InlineData(new ulong[] { 1, 2, 3 }, 2, "c1c2c302")]
        [InlineData(new ulong[] { 0, 0, 0 }, "2013-03-21T20:04:00Z", "c0c0c074323031332d30332d32315432303a30343a30305a")]
        [InlineData(new ulong[] { int.MaxValue, ulong.MaxValue }, 1363896240, "da7fffffffdbffffffffffffffff1a514b67b0")]
        [InlineData(new ulong[] { 23, 24, 100 }, new byte[] { 1, 2, 3, 4 }, "d7d818d8644401020304")]
        [InlineData(new ulong[] { 32, 1, 1 }, new object[] { 1, "lorem ipsum" }, "d820c1c182016b6c6f72656d20697073756d")]
        public static void ReadTag_NestedTags_HappyPath(ulong[] expectedTags, object expectedValue, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            foreach (ulong expectedTag in expectedTags)
            {
                Assert.Equal(CborReaderState.Tag, reader.Peek());
                CborTag tag = reader.ReadTag();
                Assert.Equal(expectedTag, (ulong)tag);
            }

            Helpers.VerifyValue(reader, expectedValue);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        // all possible definite-length encodings for the value 23
        [InlineData("17")]
        [InlineData("1817")]
        [InlineData("190017")]
        [InlineData("1a00000017")]
        [InlineData("1b0000000000000017")]
        public static void ReadUInt64_SingleValue_ShouldSupportNonCanonicalEncodings(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            ulong result = reader.ReadUInt64();
            Assert.Equal(23ul, result);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }

        [Theory]
        // all possible definite-length encodings for the value -24
        [InlineData("37")]
        [InlineData("3817")]
        [InlineData("390017")]
        [InlineData("3a00000017")]
        [InlineData("3b0000000000000017")]
        public static void ReadInt64_SingleValue_ShouldSupportNonCanonicalEncodings(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            long result = reader.ReadInt64();
            Assert.Equal(-24, result);
            Assert.Equal(CborReaderState.Finished, reader.Peek());
        }


        [Theory]
        [InlineData("1b8000000000000000")] // long.MaxValue + 1
        [InlineData("3b8000000000000000")] // long.MinValue - 1
        [InlineData("1bffffffffffffffff")] // ulong.MaxValue
        public static void ReadInt64_OutOfRangeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<OverflowException>(() => reader.ReadInt64());
        }

        [Theory]
        [InlineData("20")] // -1
        [InlineData("3863")] // -100
        [InlineData("3b7fffffffffffffff")] // long.MinValue
        public static void ReadUInt64_OutOfRangeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<OverflowException>(() => reader.ReadUInt64());
        }

        [Theory]
        [InlineData("c2")]
        public static void ReadTag_NoSubsequentData_ShouldPeekEndOfData(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            reader.ReadTag();
            Assert.Equal(CborReaderState.EndOfData, reader.Peek());
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            InvalidOperationException exn = Assert.Throws<InvalidOperationException>(() => reader.ReadInt64());

            Assert.Equal("Data item major type mismatch.", exn.Message);
        }

        [Theory]
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void ReadTag_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            InvalidOperationException exn = Assert.Throws<InvalidOperationException>(() => reader.ReadTag());

            Assert.Equal("Data item major type mismatch.", exn.Message);
        }

        [Fact]
        public static void ReadTag_NestedTagWithMissingPayload_ShouldThrowFormatException()
        {
            byte[] data = "9fc2ff".HexToByteArray();
            var reader = new CborReader(data);

            reader.ReadStartArray();
            reader.ReadTag();
            Assert.Equal(CborReaderState.FormatError, reader.Peek());
            Assert.Throws<FormatException>(() => reader.ReadEndArray());
        }

        [Theory]
        [InlineData("8201c202")] // definite length array
        [InlineData("9f01c202ff")] // equivalent indefinite-length array
        public static void ReadTag_CallingEndReadArrayPrematurely_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            // encoding is valid CBOR, so should not throw FormatException
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            reader.ReadStartArray();
            reader.ReadInt64();
            reader.ReadTag();
            Assert.Equal(CborReaderState.UnsignedInteger, reader.Peek());
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndArray());
        }

        [Theory]
        [InlineData("a102c202")] // definite length map
        [InlineData("bf02c202ff")] // equivalent indefinite-length map
        public static void ReadTag_CallingEndReadMapPrematurely_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            // encoding is valid CBOR, so should not throw FormatException
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            reader.ReadStartMap();
            reader.ReadInt64();
            reader.ReadTag();
            Assert.Equal(CborReaderState.UnsignedInteger, reader.Peek());
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndArray());
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
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            InvalidOperationException exn = Assert.Throws<InvalidOperationException>(() => reader.ReadUInt64());

            Assert.Equal("Data item major type mismatch.", exn.Message);
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
        public static void ReadCborNegativeIntegerEncoding_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            InvalidOperationException exn = Assert.Throws<InvalidOperationException>(() => reader.ReadCborNegativeIntegerEncoding());

            Assert.Equal("Data item major type mismatch.", exn.Message);
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
        public static void ReadInt64_InvalidData_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            Assert.Throws<FormatException>(() => reader.ReadInt64());
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
        public static void ReadCborNegativeIntegerEncoding_InvalidData_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadCborNegativeIntegerEncoding());
        }

        [Theory]
        [InlineData("1f")]
        [InlineData("3f")]
        public static void ReadInt64_IndefiniteLengthIntegers_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadInt64());
        }

        [Fact]
        public static void ReadUInt64_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadUInt64());
        }

        [Fact]
        public static void ReadCborNegativeIntegerEncoding_EmptyBuffer_ShouldThrowFormatException()
        {
            byte[] encoding = Array.Empty<byte>();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadCborNegativeIntegerEncoding());
        }
    }
}
