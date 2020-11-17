// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Cbor.Tests
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
            var writer = new CborWriter();
            writer.WriteInt64(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
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
        public static void WriteInt32_SingleValue_HappyPath(int input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteInt32(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
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
            var writer = new CborWriter();
            writer.WriteUInt64(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
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
        public static void WriteUInt32_SingleValue_HappyPath(uint input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteUInt32(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
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
        public static void WriteCborNegativeIntegerRepresentation_SingleValue_HappyPath(ulong input, string hexExpectedEncoding)
        {
            byte[] expectedEncoding = hexExpectedEncoding.HexToByteArray();
            var writer = new CborWriter();
            writer.WriteCborNegativeIntegerRepresentation(input);
            AssertHelper.HexEqual(expectedEncoding, writer.Encode());
        }
    }

    internal static class AssertHelper
    {
        /// <summary>
        /// Assert equality by comparing hex string representations
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        public static void HexEqual(ReadOnlyMemory<byte> expected, ReadOnlyMemory<byte> actual) => Assert.Equal(expected.ByteArrayToHex(), actual.ByteArrayToHex());
    }
}
