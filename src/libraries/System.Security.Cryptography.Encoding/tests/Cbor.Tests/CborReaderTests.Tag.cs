﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
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

            Assert.Equal(CborReaderState.Tag, reader.PeekState());
            CborTag tag = reader.ReadTag();
            Assert.Equal(expectedTag, (ulong)tag);

            Helpers.VerifyValue(reader, expectedValue);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
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
                Assert.Equal(CborReaderState.Tag, reader.PeekState());
                CborTag tag = reader.ReadTag();
                Assert.Equal(expectedTag, (ulong)tag);
            }

            Helpers.VerifyValue(reader, expectedValue);
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
        }

        [Theory]
        [InlineData("c2")]
        public static void ReadTag_NoSubsequentData_ShouldPeekEndOfData(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborReader(data);
            reader.ReadTag();
            Assert.Equal(CborReaderState.EndOfData, reader.PeekState());
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
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<InvalidOperationException>(() => reader.ReadTag());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData(2, "c202")]
        [InlineData(0, "c074323031332d30332d32315432303a30343a30305a")]
        [InlineData(1, "c11a514b67b0")]
        [InlineData(23, "d74401020304")]
        [InlineData(32, "d82076687474703a2f2f7777772e6578616d706c652e636f6d")]
        [InlineData(int.MaxValue, "da7fffffff02")]
        [InlineData(ulong.MaxValue, "dbffffffffffffffff820102")]
        public static void PeekTag_SingleValue_HappyPath(ulong expectedTag, string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Equal(CborReaderState.Tag, reader.PeekState());
            CborTag tag = reader.PeekTag();
            Assert.Equal(expectedTag, (ulong)tag);
            Assert.Equal(CborReaderState.Tag, reader.PeekState());
            Assert.Equal(0, reader.BytesRead);
        }

        [Theory]
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        [InlineData("f6")] // null
        [InlineData("80")] // []
        [InlineData("a0")] // {}
        [InlineData("f97e00")] // NaN
        [InlineData("fb3ff199999999999a")] // 1.1
        public static void PeekTag_InvalidTypes_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.PeekTag());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Fact]
        public static void ReadTag_NestedTagWithMissingPayload_ShouldThrowFormatException()
        {
            byte[] encoding = "9fc2ff".HexToByteArray();
            var reader = new CborReader(encoding);

            reader.ReadStartArray();
            reader.ReadTag();

            int bytesRemaining = reader.BytesRemaining;

            Assert.Equal(CborReaderState.FormatError, reader.PeekState());
            Assert.Throws<FormatException>(() => reader.ReadEndArray());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
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

            int bytesRemaining = reader.BytesRemaining;
            Assert.Equal(CborReaderState.UnsignedInteger, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndArray());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
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

            int bytesRemaining = reader.BytesRemaining;
            Assert.Equal(CborReaderState.UnsignedInteger, reader.PeekState());
            Assert.Throws<InvalidOperationException>(() => reader.ReadEndArray());
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("2013-03-21T20:04:00Z", "c074323031332d30332d32315432303a30343a30305a")]
        [InlineData("2020-04-09T14:31:21.3535941+01:00", "c07821323032302d30342d30395431343a33313a32312e333533353934312b30313a3030")]
        [InlineData("2020-04-09T11:41:19.12-08:00", "c0781c323032302d30342d30395431313a34313a31392e31322d30383a3030")]
        [InlineData("2020-04-09T11:41:19.12-08:00", "c07f781c323032302d30342d30395431313a34313a31392e31322d30383a3030ff")] // indefinite-length date string
        public static void ReadDateTimeOffset_SingleValue_HappyPath(string expectedValueString, string hexEncoding)
        {
            DateTimeOffset expectedValue = DateTimeOffset.Parse(expectedValueString);
            byte[] data = hexEncoding.HexToByteArray();

            var reader = new CborReader(data);

            DateTimeOffset result = reader.ReadDateTimeOffset();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Equal(expectedValue, result);
            Assert.Equal(expectedValue.Offset, result.Offset);
        }

        [Theory]
        [InlineData("c01a514b67b0")] // string datetime tag with unix time payload
        public static void ReadDateTimeOffset_InvalidTagPayload_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadDateTimeOffset());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("c07330392f30342f323032302031393a35313a3530")] // 0("09/04/2020 19:51:50")
        [InlineData("c06e4c617374204368726973746d6173")] // 0("Last Christmas")
        public static void ReadDateTimeOffset_InvalidDateString_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadDateTimeOffset());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("01")] // numeric value without tag
        [InlineData("c301")] // non-datetime tag
        public static void ReadDateTimeOffset_InvalidTag_ShouldThrowInvalidOperationxception(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadDateTimeOffset());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("81c07330392f30342f323032302031393a35313a3530")] // [0("09/04/2020 19:51:50")]
        [InlineData("81c06e4c617374204368726973746d6173")] // [0("Last Christmas")]
        public static void ReadDateTimeOffset_InvalidFormat_ShouldRollbackToInitialState(string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray());

            reader.ReadStartArray();
            int bytesRead = reader.BytesRead;
            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadDateTimeOffset());

            Assert.Equal(bytesRead, reader.BytesRead);
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
            Assert.Equal(CborReaderState.Tag, reader.PeekState());
            Assert.Equal(CborTag.DateTimeString, reader.ReadTag());
        }

        [Theory]
        [InlineData("2013-03-21T20:04:00Z", "c11a514b67b0")]
        [InlineData("2013-03-21T20:04:00.5Z", "c1fb41d452d9ec200000")]
        [InlineData("2020-04-09T13:31:21Z", "c11a5e8f23a9")]
        [InlineData("1970-01-01T00:00:00Z", "c100")]
        [InlineData("1969-12-31T23:59:59Z", "c120")]
        [InlineData("1960-01-01T00:00:00Z", "c13a12cff77f")]
        public static void ReadUnixTimeSeconds_SingleValue_HappyPath(string expectedValueString, string hexEncoding)
        {
            DateTimeOffset expectedValue = DateTimeOffset.Parse(expectedValueString);
            byte[] data = hexEncoding.HexToByteArray();

            var reader = new CborReader(data);

            DateTimeOffset result = reader.ReadUnixTimeSeconds();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Equal(expectedValue, result);
            Assert.Equal(TimeSpan.Zero, result.Offset);
        }

        [Theory]
        [InlineData("c174323031332d30332d32315432303a30343a30305a")] // epoch datetime tag with string payload
        public static void ReadUnixTimeSeconds_InvalidTagPayload_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadUnixTimeSeconds());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("c1f97e00")] // 0(NaN)
        [InlineData("c1f9fc00")] // 0(-Infinity)
        public static void ReadUnixTimeSeconds_InvalidFloatPayload_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadUnixTimeSeconds());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("01")] // numeric value without tag
        [InlineData("c301")] // non-datetime tag
        public static void ReadUnixTimeSeconds_InvalidTag_ShouldThrowInvalidOperationxception(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadUnixTimeSeconds());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("81c17330392f30342f323032302031393a35313a3530")] // [1("09/04/2020 19:51:50")]
        [InlineData("81c16e4c617374204368726973746d6173")] // [1("Last Christmas")]
        public static void ReadUnixTimeSeconds_InvalidFormat_ShouldRollbackToInitialState(string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray());

            reader.ReadStartArray();
            int bytesRead = reader.BytesRead;
            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadUnixTimeSeconds());

            Assert.Equal(bytesRead, reader.BytesRead);
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
            Assert.Equal(CborReaderState.Tag, reader.PeekState());
            Assert.Equal(CborTag.UnixTimeSeconds, reader.ReadTag());
        }

        [Theory]
        [InlineData("0", "c240")]
        [InlineData("0", "c24100")]
        [InlineData("1", "c24101")]
        [InlineData("1", "c2420001")] // should recognize leading zeroes in buffer
        [InlineData("-1", "c34100")]
        [InlineData("255", "c241ff")]
        [InlineData("-256", "c341ff")]
        [InlineData("256", "c2420100")]
        [InlineData("-257", "c3420100")]
        [InlineData("9223372036854775807", "c2487fffffffffffffff")]
        [InlineData("-9223372036854775808", "c3487fffffffffffffff")]
        [InlineData("18446744073709551616", "c249010000000000000000")]
        [InlineData("-18446744073709551617", "c349010000000000000000")]
        [InlineData("1", "c25f4101ff")] // indefinite-length buffer
        public static void ReadBigInteger_SingleValue_HappyPath(string expectedValueString, string hexEncoding)
        {
            BigInteger expectedValue = BigInteger.Parse(expectedValueString);
            byte[] data = hexEncoding.HexToByteArray();

            var reader = new CborReader(data);

            BigInteger result = reader.ReadBigInteger();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Equal(expectedValue, result);
        }


        [Theory]
        [InlineData("01")]
        [InlineData("c001")]
        public static void ReadBigInteger_InvalidCborTag_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<InvalidOperationException>(() => reader.ReadBigInteger());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("c280")]
        [InlineData("c301")]
        public static void ReadBigInteger_InvalidTagPayload_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);

            Assert.Throws<FormatException>(() => reader.ReadBigInteger());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("81c280")]
        [InlineData("81c301")]
        public static void ReadBigInteger_InvalidTagPayload_ShouldRollbackToInitialState(string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray());

            reader.ReadStartArray();
            int bytesRead = reader.BytesRead;
            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadBigInteger());

            Assert.Equal(bytesRead, reader.BytesRead);
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
            Assert.Equal(CborReaderState.Tag, reader.PeekState());
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
        [InlineData("3.9614081247908796757769715711", "c482381bc24c7fffffff7fffffff7fffffff")] // maximal number of fractional digits
        [InlineData("2000000000", "c4820902")] // encoding with positive exponent representation in payload (2 * 10^9)
        public static void ReadDecimal_SingleValue_HappyPath(string expectedStringValue, string hexEncoding)
        {
            decimal expectedValue = decimal.Parse(expectedStringValue, Globalization.CultureInfo.InvariantCulture);
            byte[] data = hexEncoding.HexToByteArray();

            var reader = new CborReader(data);

            decimal result = reader.ReadDecimal();
            Assert.Equal(CborReaderState.Finished, reader.PeekState());
            Assert.Equal(expectedValue, result);
        }

        [Theory]
        [InlineData("c482181d02")] // 2 * 10^29
        [InlineData("c482381c02")] // 2 * 10^-29
        [InlineData("c48201c24cffffffffffffffffffffffff")] // decimal.MaxValue * 10^1
        [InlineData("c48200c24d01000000000000000000000000")] // (decimal.MaxValue + 1) * 10^0
        [InlineData("c48200c34cffffffffffffffffffffffff")] // (decimal.MinValue - 1) * 10^0
        public static void ReadDecimal_LargeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<OverflowException>(() => reader.ReadDecimal());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("c201")]
        public static void ReadDecimal_InvalidTag_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<InvalidOperationException>(() => reader.ReadDecimal());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("c401")] // 4(1)
        [InlineData("c480")] // 4([])
        [InlineData("c48101")] // 4([1])
        [InlineData("c4820160")] // 4([1, ""])
        [InlineData("c4826001")] // 4(["", 1])
        public static void ReadDecimal_InvalidFormat_ShouldThrowFormatException(string hexEncoding)
        {
            byte[] encoding = hexEncoding.HexToByteArray();
            var reader = new CborReader(encoding);
            Assert.Throws<FormatException>(() => reader.ReadDecimal());
            Assert.Equal(encoding.Length, reader.BytesRemaining);
        }

        [Theory]
        [InlineData("81c401")] // 4(1)
        [InlineData("81c480")] // [4([])]
        [InlineData("81c4826001")] // [4(["", 1])]
        // decimal using an invalid biginteger encoding,
        // in this case two nested state rollbacks will take place
        [InlineData("81c48201c260")] // [4([1, 2("")])]
        public static void ReadDecimal_InvalidTagPayload_ShouldRollbackToInitialState(string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray());

            reader.ReadStartArray();

            int bytesRead = reader.BytesRead;
            int bytesRemaining = reader.BytesRemaining;
            Assert.Throws<FormatException>(() => reader.ReadDecimal());

            Assert.Equal(bytesRead, reader.BytesRead);
            Assert.Equal(bytesRemaining, reader.BytesRemaining);
            Assert.Equal(CborReaderState.Tag, reader.PeekState());
            Assert.Equal(CborTag.DecimalFraction, reader.ReadTag());
        }

        [Theory]
        [MemberData(nameof(SupportedConformanceTaggedValues))]
        public static void ReadTaggedValue_SupportedConformance_ShouldSucceed(CborConformanceLevel level, object expectedValue, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), level);
            Helpers.VerifyValue(reader, expectedValue);
        }

        public static IEnumerable<object[]> SupportedConformanceTaggedValues =>
            from l in new[] { CborConformanceLevel.Lax, CborConformanceLevel.Strict, CborConformanceLevel.Rfc7049Canonical }
            from v in TaggedValues
            select new object[] { l, v.value, v.hexEncoding };

        [Theory]
        [MemberData(nameof(UnsupportedConformanceTaggedValues))]
        public static void ReadTaggedValue_UnsupportedConformance_ShouldThrowFormatException(CborConformanceLevel level, object expectedValue, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), level);
            Assert.Throws<FormatException>(() => Helpers.VerifyValue(reader, expectedValue));
            Assert.Equal(0, reader.BytesRead);
        }

        public static IEnumerable<object[]> UnsupportedConformanceTaggedValues =>
            from l in new[] { CborConformanceLevel.Ctap2Canonical }
            from v in TaggedValues
            select new object[] { l, v.value, v.hexEncoding };

        [Theory]
        [MemberData(nameof(TaggedValuesAnyConformance))]
        public static void PeekTag_UnsupportedConformanceLevel_ShouldSucceed(CborConformanceLevel level, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), level);
            reader.PeekTag();
        }

        public static IEnumerable<object[]> TaggedValuesAnyConformance =>
            from l in new[] { CborConformanceLevel.Ctap2Canonical }
            from v in TaggedValues
            select new object[] { l, v.hexEncoding };

        [Theory]
        [MemberData(nameof(UnsupportedConformanceInvalidTypes))]
        public static void PeekTag_InvalidType_UnsupportedConformanceLevel_ShouldThrowInvalidOperationException(CborConformanceLevel level, string hexEncoding)
        {
            var reader = new CborReader(hexEncoding.HexToByteArray(), level);
            Assert.Throws<InvalidOperationException>(() => reader.PeekTag());
        }

        public static IEnumerable<object[]> UnsupportedConformanceInvalidTypes =>
            from l in new[] { CborConformanceLevel.Ctap2Canonical }
            from e in new[] { "01", "40", "60" }
            select new object[] { l, e };

        private static (object value, string hexEncoding)[] TaggedValues =>
            new (object, string)[]
            {
                (new object[] { CborTag.MimeMessage, 42 }, "d824182a"),
                (42.0m, "c482201901a4"),
                ((BigInteger)1, "c24101"),
                (DateTimeOffset.UnixEpoch, "c0781c313937302d30312d30315430303a30303a30302e303030303030305a"),
            };
    }
}
