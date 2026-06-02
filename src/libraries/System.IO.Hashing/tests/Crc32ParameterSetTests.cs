// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Hashing.Tests
{
    public static class Crc32ParameterSetTests
    {
        [Theory]
        [InlineData(0x814141ab, 0x00000000, false, 0x00000000, 0x3010bf7f, 0x00000000, "CRC-32/AIXM")]
        [InlineData(0xf4acfb13, 0xffffffff, true, 0xffffffff, 0x1697d06a, 0x904cddbf, "CRC-32/AUTOSAR")]
        [InlineData(0xa833982b, 0xffffffff, true, 0xffffffff, 0x87315576, 0x45270551, "CRC-32/BASE91-D")]
        [InlineData(0x04c11db7, 0xffffffff, false, 0xffffffff, 0xfc891918, 0xc704dd7b, "CRC-32/BZIP2")]
        [InlineData(0x8001801b, 0x00000000, true, 0x00000000, 0x6ec2edc4, 0x00000000, "CRC-32/CD-ROM-EDC")]
        [InlineData(0x04c11db7, 0x00000000, false, 0xffffffff, 0x765e7680, 0xc704dd7b, "CRC-32/CKSUM")]
        [InlineData(0x1edc6f41, 0xffffffff, true, 0xffffffff, 0xe3069283, 0xb798b438, "CRC-32/ISCSI")]
        [InlineData(0x04c11db7, 0xffffffff, true, 0xffffffff, 0xcbf43926, 0xdebb20e3, "CRC-32/ISO-HDLC")]
        [InlineData(0x04c11db7, 0xffffffff, true, 0x00000000, 0x340bc6d9, 0x00000000, "CRC-32/JAMCRC")]
        [InlineData(0x741b8cd7, 0xffffffff, true, 0x00000000, 0xd2c22f51, 0x00000000, "CRC-32/MEF")]
        [InlineData(0x04c11db7, 0xffffffff, false, 0x00000000, 0x0376e6e7, 0x00000000, "CRC-32/MPEG-2")]
        [InlineData(0x000000af, 0x00000000, false, 0x00000000, 0xbd0be338, 0x00000000, "CRC-32/XFER")]
        public static void KnownAnswers(
            uint poly,
            uint init,
            bool refInOut,
            uint xorOut,
            uint check,
            uint residue,
            string displayName)
        {
            _ = displayName;
            Crc32ParameterSet crc32 = Crc32ParameterSet.Create(poly, init, xorOut, refInOut);
            Assert.Equal(poly, crc32.Polynomial);
            Assert.Equal(init, crc32.InitialValue);
            Assert.Equal(refInOut, crc32.ReflectValues);
            Assert.Equal(xorOut, crc32.FinalXorValue);

            Crc32 hasher = new Crc32(crc32);
            hasher.Append("123456789"u8);
            Assert.Equal(check, hasher.GetCurrentHashAsUInt32());
            byte[] ret = hasher.GetCurrentHash();
            hasher.Append(ret);
            uint final = hasher.GetCurrentHashAsUInt32();

            // https://reveng.sourceforge.io/crc-catalogue/all.htm defines the residue
            // as the value before the final XOR is applied, so we need to final-XOR here
            // to get the values to match.
            uint finalResidue = final ^ xorOut;
            Assert.Equal(residue, finalResidue);
        }
    }
}
