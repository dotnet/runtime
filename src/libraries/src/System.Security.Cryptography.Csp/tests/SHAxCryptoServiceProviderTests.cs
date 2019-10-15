// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.SHA1.Tests
{
    /// <summary>
    /// Since SHAxCryptoServiceProvider types wraps IncrementalHash from Algorithms assembly, we only test minimally here.
    /// </summary>
    public class SHAxCryptoServiceProviderTests
    {
        [Fact]
        public void Sha1_Empty()
        {
            Verify<SHA1CryptoServiceProvider>(string.Empty, "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        [Fact]
        public void Sha256_Empty()
        {
            Verify<SHA256CryptoServiceProvider>(string.Empty, "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
        }

        [Fact]
        public void Sha384_Empty()
        {
            Verify<SHA384CryptoServiceProvider>(string.Empty, "38B060A751AC96384CD9327EB1B1E36A21FDB71114BE07434C0CC7BF63F6E1DA274EDEBFE76F65FBD51AD2F14898B95B");
        }

        [Fact]
        public void Sha512_Empty()
        {
            Verify<SHA512CryptoServiceProvider>(string.Empty, "CF83E1357EEFB8BDF1542850D66D8007D620E4050B5715DC83F4A921D36CE9CE47D0D13C5D85F2B0FF8318D2877EEC2F63B931BD47417A81A538327AF927DA3E");
        }

        [Fact]
        public void Sha1_Rfc3174_2()
        {
            Verify<SHA1CryptoServiceProvider>(
                "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
                "84983E441C3BD26EBAAE4AA1F95129E5E54670F1");
        }

        [Fact]
        public void Sha256_Rfc3174_2()
        {
            Verify<SHA256CryptoServiceProvider>(
                "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
                "248D6A61D20638B8E5C026930C3E6039A33CE45964FF2167F6ECEDD419DB06C1");
        }

        [Fact]
        public void Sha384_Rfc3174_2()
        {
            Verify<SHA384CryptoServiceProvider>(
                "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
                "3391FDDDFC8DC7393707A65B1B4709397CF8B1D162AF05ABFE8F450DE5F36BC6B0455A8520BC4E6F5FE95B1FE3C8452B");
        }

        [Fact]
        public void Sha512_Rfc3174_2()
        {
            Verify<SHA512CryptoServiceProvider>(
                "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
                "204A8FC6DDA82F0A0CED7BEB8E08A41657C16EF468B228A8279BE331A703C33596FD15C13B1B07F9AA1D3BEA57789CA031AD85C7A71DD70354EC631238CA3445");
        }

        private void Verify<T>(string rawText, string expected) where T:HashAlgorithm, new()
        {
            byte[] inputBytes = ByteUtils.AsciiBytes(rawText);
            byte[] expectedBytes = ByteUtils.HexToByteArray(expected);

            using (HashAlgorithm hash = new T())
            {
                Assert.True(hash.HashSize > 0);
                byte[] actual = hash.ComputeHash(inputBytes, 0, inputBytes.Length);

                Assert.Equal(expectedBytes, actual);

                actual = hash.Hash;
                Assert.Equal(expectedBytes, actual);
            }
        }
    }
}
