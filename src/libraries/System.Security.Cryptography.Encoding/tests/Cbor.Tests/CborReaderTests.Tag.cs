// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Numerics;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    public partial class CborReaderTests
    {
        // Data points taken from https://tools.ietf.org/html/rfc7049#appendix-A
        // Additional pairs generated using http://cbor.me/

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
        [InlineData("2013-03-21T20:04:00Z", "c074323031332d30332d32315432303a30343a30305a")]
        [InlineData("2013-03-21T20:04:00Z", "c11a514b67b0")]
        [InlineData("2013-03-21T20:04:00.5Z", "c1fb41d452d9ec200000")]
        [InlineData("2020-04-09T14:31:21.3535941+01:00", "c07821323032302d30342d30395431343a33313a32312e333533353934312b30313a3030")]
        [InlineData("2020-04-09T11:41:19.12-08:00", "c0781c323032302d30342d30395431313a34313a31392e31322d30383a3030")]
        [InlineData("2020-04-09T13:31:21Z", "c11a5e8f23a9")]
        [InlineData("1970-01-01T00:00:00Z", "c100")]
        [InlineData("1969-12-31T23:59:59Z", "c120")]
        [InlineData("1960-01-01T00:00:00Z", "c13a12cff77f")]
        public static void ReadDateTimeOffset_SingleValue_HappyPath(string expectedValueString, string hexEncoding)
        {
            DateTimeOffset expectedValue = DateTimeOffset.Parse(expectedValueString);
            byte[] data = hexEncoding.HexToByteArray();

            var reader = new CborReader(data);

            DateTimeOffset result = reader.ReadDateTimeOffset();
            Assert.Equal(CborReaderState.Finished, reader.Peek());
            Assert.Equal(expectedValue, result);
            Assert.Equal(expectedValue.Offset, result.Offset);
        }

        [Theory]
        [InlineData("c01a514b67b0")] // string datetime tag with unix time payload
        [InlineData("c174323031332d30332d32315432303a30343a30305a")] // epoch datetime tag with string payload
        public static void ReadDateTimeOffset_InvalidTagPayload_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadDateTimeOffset());
        }

        [Theory]
        [InlineData("c07330392f30342f323032302031393a35313a3530")] // 0("09/04/2020 19:51:50")
        [InlineData("c06e4c617374204368726973746d6173")] // 0("Last Christmas")
        public static void ReadDateTimeOffset_InvalidDateString_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadDateTimeOffset());
        }

        [Theory]
        [InlineData("01")] // numeric value without tag
        [InlineData("c301")] // non-datetime tag
        public static void ReadDateTimeOffset_InvalidTag_ShouldThrowInvalidOperationxception(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<InvalidOperationException>(() => reader.ReadDateTimeOffset());
        }

        [Theory]
        [InlineData("0", "c24100")]
        [InlineData("1", "c24101")]
        [InlineData("-1", "c34100")]
        [InlineData("255", "c241ff")]
        [InlineData("-256", "c341ff")]
        [InlineData("256", "c2420100")]
        [InlineData("-257", "c3420100")]
        [InlineData("9223372036854775807", "c2487fffffffffffffff")]
        [InlineData("-9223372036854775808", "c3487fffffffffffffff")]
        [InlineData("18446744073709551616", "c249010000000000000000")]
        [InlineData("-18446744073709551617", "c349010000000000000000")]
        public static void ReadBigInteger_SingleValue_HappyPath(string expectedValueString, string hexEncoding)
        {
            BigInteger expectedValue = BigInteger.Parse(expectedValueString);
            byte[] data = hexEncoding.HexToByteArray();

            var reader = new CborReader(data);

            BigInteger result = reader.ReadBigInteger();
            Assert.Equal(CborReaderState.Finished, reader.Peek());
            Assert.Equal(expectedValue, result);
        }


        [Theory]
        [InlineData("01")]
        [InlineData("c001")]
        public static void ReadBigInteger_InvalidCborTag_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<InvalidOperationException>(() => reader.ReadBigInteger());
        }

        [Theory]
        [InlineData("c280")]
        [InlineData("c301")]
        public static void ReadBigInteger_InvalidTagPayload_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);

            Assert.Throws<FormatException>(() => reader.ReadBigInteger());
        }

        [Theory]
        [InlineData("0", "c4820000")]
        [InlineData("1", "c4820001")]
        [InlineData("-1", "c4820020")]
        [InlineData("1.1", "c482200b")]
        [InlineData("1.000", "c482221903e8")]
        [InlineData("273.15", "c48221196ab3")]
        [InlineData("79228162514264337593543950335", "c48200c24cffffffffffffffffffffffff")] // decimal.MaxValue
        [InlineData("7922816251426433759354395033.5", "c48220c24cffffffffffffffffffffffff")]
        [InlineData("-79228162514264337593543950335", "c48200c34cfffffffffffffffffffffffe")] // decimal.MinValue
        public static void ReadDecimal_SingleValue_HappyPath(string expectedStringValue, string hexEncoding)
        {
            decimal expectedValue = Decimal.Parse(expectedStringValue);
            byte[] data = hexEncoding.HexToByteArray();

            var reader = new CborReader(data);

            decimal result = reader.ReadDecimal();
            Assert.Equal(CborReaderState.Finished, reader.Peek());
            Assert.Equal(expectedValue, result);
        }
    }
}
