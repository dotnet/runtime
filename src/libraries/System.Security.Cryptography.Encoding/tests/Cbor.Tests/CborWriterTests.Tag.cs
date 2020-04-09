// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
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
            using var writer = new CborWriter();
            writer.WriteTag((CborTag)tag);
            Helpers.WriteValue(writer, value);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
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
            using var writer = new CborWriter();
            foreach (var tag in tags)
            {
                writer.WriteTag((CborTag)tag);
            }
            Helpers.WriteValue(writer, value);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
        }

        [Theory]
        [InlineData(new ulong[] { 2 })]
        [InlineData(new ulong[] { 1, 2, 3 })]
        public static void WriteTag_NoValue_ShouldThrowInvalidOperationException(ulong[] tags)
        {
            using var writer = new CborWriter();

            foreach (ulong tag in tags)
            {
                writer.WriteTag((CborTag)tag);
            }

            InvalidOperationException exn = Assert.Throws<InvalidOperationException>(() => writer.ToArray());

            Assert.Equal("Buffer contains incomplete CBOR document.", exn.Message);
        }

        [Fact]
        public static void WriteTag_NoValueInNestedContext_ShouldThrowInvalidOperationException()
        {
            using var writer = new CborWriter();

            writer.WriteStartArrayIndefiniteLength();
            writer.WriteTag(CborTag.Uri);
            Assert.Throws<InvalidOperationException>(() => writer.WriteEndArray());
        }

        [Theory]
        [InlineData("2013-03-21T20:04:00.0000000+00:00", "c07821323031332d30332d32315432303a30343a30302e303030303030302b30303a3030")]
        [InlineData("2020-04-09T14:31:21.3535941+01:00", "c07821323032302d30342d30395431343a33313a32312e333533353934312b30313a3030")]
        public static void WriteDateTimeOffset_SingleValue_HappyPath(string valueString, string expectedHexEncoding)
        {
            DateTimeOffset value = DateTimeOffset.Parse(valueString);
            using var writer = new CborWriter();
            writer.WriteDateTimeOffset(value);

            byte[] encoding = writer.ToArray();
            AssertHelper.HexEqual(expectedHexEncoding.HexToByteArray(), encoding);
        }

        [Theory]
        [InlineData(1363896240, "c11a514b67b0")]
        [InlineData(1586439081, "c11a5e8f23a9")]
        public static void WriteUnixTimeSeconds_SingleValue_HappyPath(long value, string expectedHexEncoding)
        {
            using var writer = new CborWriter();
            writer.WriteUnixTimeSeconds(value);

            byte[] encoding = writer.ToArray();
            AssertHelper.HexEqual(expectedHexEncoding.HexToByteArray(), encoding);
        }
    }
}
