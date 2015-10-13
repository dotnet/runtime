// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public class DerEncoderTests
    {
        [Theory]
        [InlineData("00", 0x02, "01", "00")]
        [InlineData("01", 0x02, "01", "01")]
        [InlineData("7F", 0x02, "01", "7F")]
        [InlineData("80", 0x02, "02", "0080")]
        [InlineData("FF", 0x02, "02", "00FF")]
        [InlineData("00FF", 0x02, "02", "00FF")]
        [InlineData("01FF", 0x02, "02", "01FF")]
        [InlineData("7FFF", 0x02, "02", "7FFF")]
        [InlineData("8000", 0x02, "03", "008000")]
        [InlineData("FFFF", 0x02, "03", "00FFFF")]
        [InlineData("00FFFF", 0x02, "03", "00FFFF")]
        [InlineData("7FFFFF", 0x02, "03", "7FFFFF")]
        [InlineData("800000", 0x02, "04", "00800000")]
        [InlineData("FFFFFF", 0x02, "04", "00FFFFFF")]
        [InlineData("00FFFFFF", 0x02, "04", "00FFFFFF")]
        [InlineData("7FFFFFFF", 0x02, "04", "7FFFFFFF")]
        [InlineData("80000000", 0x02, "05", "0080000000")]
        [InlineData("FFFFFFFF", 0x02, "05", "00FFFFFFFF")]
        [InlineData("0123456789ABCDEF", 0x02, "08", "0123456789ABCDEF")]
        [InlineData("FEDCBA9876543210", 0x02, "09", "00FEDCBA9876543210")]
        public static void ValidateUintEncodings(string hexRaw, byte tag, string hexLength, string hexValue)
        {
            byte[] raw = hexRaw.HexToByteArray();
            byte[] length = hexLength.HexToByteArray();
            byte[] value = hexValue.HexToByteArray();

            byte[][] segments = DerEncoder.SegmentedEncodeUnsignedInteger(raw);

            Assert.Equal(3, segments.Length);

            Assert.Equal(new[] { tag }, segments[0]);
            Assert.Equal(length, segments[1]);
            Assert.Equal(value, segments[2]);
        }

        [Fact]
        public static void ConstructSequence()
        {
            byte[] expected =
            {
                /* SEQUENCE */     0x30, 0x07,
                /* INTEGER(0) */   0x02, 0x01, 0x00,
                /* INTEGER(256) */ 0x02, 0x02, 0x01, 0x00,
            };

            byte[] encoded = DerEncoder.ConstructSequence(
                DerEncoder.SegmentedEncodeUnsignedInteger(new byte[] { 0x00 }),
                DerEncoder.SegmentedEncodeUnsignedInteger(new byte[] { 0x01, 0x00 }));

            Assert.Equal(expected, encoded);
        }
    }
}
