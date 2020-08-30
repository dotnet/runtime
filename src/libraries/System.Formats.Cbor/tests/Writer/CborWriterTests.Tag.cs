// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
{
    public partial class CborWriterTests
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
        public static void WriteTag_SingleValue_HappyPath(ulong tag, object value, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteTag((CborTag)tag);
            Helpers.WriteValue(writer, value);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(new ulong[] { 1, 2, 3 }, 2, "c1c2c302")]
        [InlineData(new ulong[] { 0, 0, 0 }, "2013-03-21T20:04:00Z", "c0c0c074323031332d30332d32315432303a30343a30305a")]
        [InlineData(new ulong[] { int.MaxValue, ulong.MaxValue }, 1363896240, "da7fffffffdbffffffffffffffff1a514b67b0")]
        [InlineData(new ulong[] { 23, 24, 100 }, new byte[] { 1, 2, 3, 4 }, "d7d818d8644401020304")]
        [InlineData(new ulong[] { 32, 1, 1 }, new object[] { 1, "lorem ipsum" }, "d820c1c182016b6c6f72656d20697073756d")]
        public static void WriteTag_NestedTags_HappyPath(ulong[] tags, object value, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            foreach (ulong tag in tags)
            {
                writer.WriteTag((CborTag)tag);
            }
            Helpers.WriteValue(writer, value);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }

        [Theory]
        [InlineData(new ulong[] { 2 })]
        [InlineData(new ulong[] { 1, 2, 3 })]
        public static void WriteTag_NoValue_ShouldThrowInvalidOperationException(ulong[] tags)
        {
            var writer = new CborWriter();

            foreach (ulong tag in tags)
            {
                writer.WriteTag((CborTag)tag);
            }

            Assert.Throws<InvalidOperationException>(() => writer.Encode());
        }

        [Fact]
        public static void WriteTag_NoValueInNestedContext_ShouldThrowInvalidOperationException()
        {
            var writer = new CborWriter();

            writer.WriteStartArray(null);
            writer.WriteTag(CborTag.Uri);
            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }

        [Theory]
        [InlineData("2013-03-21T20:04:00Z", "c074323031332d30332d32315432303a30343a30305a")]
        [InlineData("2020-04-09T14:31:21.3535941+01:00", "c07821323032302d30342d30395431343a33313a32312e333533353934312b30313a3030")]
        [InlineData("2020-04-09T11:41:19.12-08:00", "c0781c323032302d30342d30395431313a34313a31392e31322d30383a3030")]
        public static void WriteDateTimeOffset_SingleValue_HappyPath(string valueString, string expectedHexEncoding)
        {
            DateTimeOffset value = DateTimeOffset.Parse(valueString);
            var writer = new CborWriter();
            writer.WriteDateTimeOffset(value);

            byte[] encoding = writer.Encode();
            AssertHelper.HexEqual(expectedHexEncoding.HexToByteArray(), encoding);
        }

        [Theory]
        [InlineData(1363896240, "c11a514b67b0")]
        [InlineData(1586439081, "c11a5e8f23a9")]
        [InlineData(0, "c100")]
        [InlineData(-1, "c120")]
        [InlineData(-315619200, "c13a12cff77f")]
        public static void WriteUnixTimeSeconds_Long_SingleValue_HappyPath(long value, string expectedHexEncoding)
        {
            var writer = new CborWriter();
            writer.WriteUnixTimeSeconds(value);

            byte[] encoding = writer.Encode();
            AssertHelper.HexEqual(expectedHexEncoding.HexToByteArray(), encoding);
        }

        [Theory]
        [InlineData(1363896240, "c1fb41d452d9ec000000")]
        [InlineData(1586439081, "c1fb41d7a3c8ea400000")]
        [InlineData(0, "c1f90000")]
        [InlineData(-1, "c1f9bc00")]
        [InlineData(-315619200, "c1facd967fbc")]
        [InlineData(1363896240.5, "c1fb41d452d9ec200000")]
        [InlineData(15870467036.15, "c1fb420d8fa0dee13333")]
        public static void WriteUnixTimeSeconds_Double_SingleValue_HappyPath(double value, string expectedHexEncoding)
        {
            var writer = new CborWriter();
            writer.WriteUnixTimeSeconds(value);

            byte[] encoding = writer.Encode();
            AssertHelper.HexEqual(expectedHexEncoding.HexToByteArray(), encoding);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public static void WriteUnixTimeSeconds_Double_InvalidInput_ShouldThrowArgumentException(double value)
        {
            var writer = new CborWriter();
            Assert.Throws<ArgumentException>(() => writer.WriteUnixTimeSeconds(value));
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
        public static void WriteInteger_SingleValue_HappyPath(string valueString, string expectedHexEncoding)
        {
            BigInteger value = BigInteger.Parse(valueString);

            var writer = new CborWriter();
            writer.WriteBigInteger(value);

            byte[] encoding = writer.Encode();
            AssertHelper.HexEqual(expectedHexEncoding.HexToByteArray(), encoding);
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
        public static void WriteDecimal_SingleValue_HappyPath(string stringValue, string expectedHexEncoding)
        {
            decimal value = decimal.Parse(stringValue, Globalization.CultureInfo.InvariantCulture);
            var writer = new CborWriter();
            writer.WriteDecimal(value);
            byte[] encoding = writer.Encode();
            AssertHelper.HexEqual(expectedHexEncoding.HexToByteArray(), encoding);
        }

        [Theory]
        [MemberData(nameof(UnsupportedConformanceTaggedValues))]
        public static void WriteTaggedValue_UnsupportedConformance_ShouldThrowInvalidOperationException(CborConformanceMode mode, object value)
        {
            var writer = new CborWriter(mode);
            Assert.Throws<InvalidOperationException>(() => Helpers.WriteValue(writer, value));
            Assert.Equal(0, writer.BytesWritten);
        }

        public static IEnumerable<object[]> UnsupportedConformanceTaggedValues =>
            from l in new[] { CborConformanceMode.Ctap2Canonical }
            from v in TaggedValues
            select new object[] { l, v };

        [Theory]
        [MemberData(nameof(SupportedConformanceTaggedValues))]
        public static void WriteTaggedValue_SupportedConformance_ShouldSucceed(CborConformanceMode mode, object value)
        {
            var writer = new CborWriter(mode);
            Helpers.WriteValue(writer, value);
        }

        public static IEnumerable<object[]> SupportedConformanceTaggedValues =>
            from l in new[] { CborConformanceMode.Lax, CborConformanceMode.Strict, CborConformanceMode.Canonical }
            from v in TaggedValues
            select new object[] { l, v };

        private static object[] TaggedValues =>
            new object[]
            {
                new object[] { CborTag.MimeMessage, 42 },
                42.0m,
                (BigInteger)1,
                DateTimeOffset.UnixEpoch,
            };
    }
}
