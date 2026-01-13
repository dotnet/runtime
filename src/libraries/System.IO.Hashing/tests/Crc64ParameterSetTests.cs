// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Hashing.Tests
{
    public static class Crc64ParameterSetTests
    {
        [Theory]
        [InlineData(0x42F0E1EBA9EA3693ul, 0x0000000000000000ul, false, false, 0x0000000000000000ul, 0x6C40DF5F0B497347ul, 0x0000000000000000ul, "CRC-64/ECMA-182")]
        [InlineData(0x000000000000001Bul, 0xFFFFFFFFFFFFFFFFul, true, true, 0xFFFFFFFFFFFFFFFFul, 0xB90956C775A41001ul, 0x5300000000000000ul, "CRC-64/GO-ISO")]
        [InlineData(0x259C84CBA6426349ul, 0xFFFFFFFFFFFFFFFFul, true, true, 0x0000000000000000ul, 0x75D4B74F024ECEEAul, 0x0000000000000000ul, "CRC-64/MS")]
        [InlineData(0xAD93D23594C93659ul, 0xFFFFFFFFFFFFFFFFul, true, true, 0xFFFFFFFFFFFFFFFFul, 0xAE8B14860A799888ul, 0xF310303B2B6F6E42uL, "CRC-64/NVME")]
        [InlineData(0xAD93D23594C935A9ul, 0x0000000000000000ul, true, true, 0x0000000000000000ul, 0xE9C6D914C4B8D9CAul, 0x0000000000000000ul, "CRC-64/REDIS")]
        [InlineData(0x42F0E1EBA9EA3693ul, 0xFFFFFFFFFFFFFFFFul, false, false, 0xFFFFFFFFFFFFFFFFul, 0x62EC59E3F1A4F00Aul, 0xFCACBEBD5931A992ul, "CRC-64/WE")]
        [InlineData(0x42F0E1EBA9EA3693ul, 0xFFFFFFFFFFFFFFFFul, true, true, 0xFFFFFFFFFFFFFFFFul, 0x995DC9BBDF1939FAul, 0x49958C9ABD7D353Ful, "CRC-64/XZ")]
        public static void KnownAnswers(
            ulong poly,
            ulong init,
            bool refIn,
            bool refOut,
            ulong xorOut,
            ulong check,
            ulong residue,
            string displayName)
        {
            _ = displayName;
            Crc64ParameterSet crc64 = Crc64ParameterSet.Create(poly, init, xorOut, refIn, refOut);
            Assert.Equal(poly, crc64.Polynomial);
            Assert.Equal(init, crc64.InitialValue);
            Assert.Equal(refIn, crc64.ReflectInput);
            Assert.Equal(refOut, crc64.ReflectOutput);
            Assert.Equal(xorOut, crc64.FinalXorValue);

            Crc64 hasher = new Crc64(crc64);
            hasher.Append("123456789"u8);
            Assert.Equal(check, hasher.GetCurrentHashAsUInt64());
            byte[] ret = hasher.GetCurrentHash();
            hasher.Append(ret);
            ulong final = hasher.GetCurrentHashAsUInt64();
            ulong finalResidue = final ^ xorOut;
            Assert.Equal(residue, finalResidue);
        }
    }
}
