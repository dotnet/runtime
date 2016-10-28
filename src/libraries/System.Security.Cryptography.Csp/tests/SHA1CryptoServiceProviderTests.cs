// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.SHA1.Tests
{
    /// <summary>
    /// Since SHA1CryptoServiceProvider wraps IncrementalHash from Algorithms assembly, we only test minimally here.
    /// </summary>
    public class MD5CryptoServiceProviderTests
    {
        [Fact]
        public void Sha1_Empty()
        {
            Verify(string.Empty, "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        [Fact]
        public void Sha1_Rfc3174_2()
        {
            Verify(
                "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
                "84983E441C3BD26EBAAE4AA1F95129E5E54670F1");
        }

        private void Verify(string rawText, string expected)
        {
            byte[] inputBytes = ByteUtils.AsciiBytes(rawText);
            byte[] expectedBytes = ByteUtils.HexToByteArray(expected);

            using (var hash = new SHA1CryptoServiceProvider())
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
