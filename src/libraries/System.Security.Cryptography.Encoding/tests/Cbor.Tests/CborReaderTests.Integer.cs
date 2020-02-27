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
        public static void Int64Reader_SingleValue_HappyPath(long expectedResult, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborValueReader(data);
            long actualResult = reader.ReadInt64();
            Assert.Equal(expectedResult, actualResult);
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
        public static void UInt64Reader_SingleValue_HappyPath(ulong expectedResult, string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            var reader = new CborValueReader(data);
            ulong actualResult = reader.ReadUInt64();
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData("1b8000000000000000")] // long.MaxValue + 1
        [InlineData("3b8000000000000000")] // long.MinValue - 1
        [InlineData("1bffffffffffffffff")] // ulong.MaxValue
        public static void Int64Reader_OutOfRangeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            Assert.Throws<OverflowException>(() =>
            {
                var reader = new CborValueReader(data);
                reader.ReadInt64();
            });
        }

        [Theory]
        [InlineData("20")] // -1
        [InlineData("3863")] // -100
        [InlineData("3b7fffffffffffffff")] // long.MinValue
        public static void UInt64Reader_OutOfRangeValues_ShouldThrowOverflowException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            Assert.Throws<OverflowException>(() =>
            {
                var reader = new CborValueReader(data);
                reader.ReadUInt64();
            });
        }

        [Theory]
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        public static void Int64Reader_StringValues_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            Assert.Throws<InvalidOperationException>(() =>
            {
                var reader = new CborValueReader(data);
                reader.ReadInt64();
            });
        }

        [Theory]
        [InlineData("40")] // empty text string
        [InlineData("60")] // empty byte string
        public static void UInt64Reader_StringValues_ShouldThrowInvalidOperationException(string hexEncoding)
        {
            byte[] data = hexEncoding.HexToByteArray();
            Assert.Throws<InvalidOperationException>(() =>
            {
                var reader = new CborValueReader(data);
                reader.ReadUInt64();
            });
        }
    }
}
