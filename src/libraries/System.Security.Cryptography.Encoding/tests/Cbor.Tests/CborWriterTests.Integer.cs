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
        public static void WriteInt64_SingleValue_HappyPath(long input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            writer.WriteInt64(input);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
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
        public static void WriteUInt64_SingleValue_HappyPath(ulong input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            using var writer = new CborWriter();
            writer.WriteUInt64(input);
            AssertHelper.HexEqual(expectedEncoding, writer.ToArray());
        }

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
    }

    internal static class AssertHelper
    {
        /// <summary>
        /// Assert equality by comparing hex string representations
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        public static void HexEqual(byte[] expected, byte[] actual) => Assert.Equal(expected.ByteArrayToHex(), actual.ByteArrayToHex());
    }
}
