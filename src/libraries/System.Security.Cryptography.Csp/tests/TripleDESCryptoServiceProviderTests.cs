// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDES.Tests
{
    /// <summary>
    /// Since TripleDESCryptoServiceProvider wraps TripleDES from Algorithms assembly, we only test minimally here.
    /// </summary>
    public class TripleDESCryptoServiceProviderTests
    {
        [Fact]
        public static void VerifyDefaults()
        {
            using (var alg = new TripleDESCryptoServiceProvider())
            {
                Assert.Equal(64, alg.BlockSize);
                Assert.Equal(192, alg.KeySize);
                Assert.Equal(CipherMode.CBC, alg.Mode);
                Assert.Equal(PaddingMode.PKCS7, alg.Padding);
            }
        }

        [Fact]
        public static void TripleDESRoundTrip192BitsNoneCBC()
        {
            byte[] key = "b43eaf0260813fb47c87ae073a146006d359ad04061eb0e6".HexToByteArray();
            byte[] iv = "5fbc5bc21b8597d8".HexToByteArray();

            using (var alg = new TripleDESCryptoServiceProvider())
            {
                alg.Key = key;
                alg.IV = iv;
                alg.Padding = PaddingMode.None;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = "dea36279600f19c602b6ed9bf3ffdac5ebf25c1c470eb61c".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Fact]
        public static void TestShims()
        {
            using (var alg = new TripleDESCryptoServiceProvider())
            {
                alg.BlockSize = 64;
                Assert.Equal(64, alg.BlockSize);

                var emptyIV = new byte[8];
                alg.IV = emptyIV;
                Assert.Equal(emptyIV, alg.IV);
                alg.GenerateIV();
                Assert.NotEqual(emptyIV, alg.IV);

                alg.KeySize = 64 * 3;
                Assert.Equal(64 * 3, alg.KeySize);
                var knownKey = new byte[]
                {
                    /* k1 */ 0, 1, 2, 3, 4, 5, 6, 7,
                    /* k2 */ 0, 0, 0, 2, 4, 6, 0, 1,
                    /* k3 */ 0, 1, 2, 3, 4, 5, 6, 7,
                };
                alg.Key = knownKey;
                Assert.Equal(knownKey, alg.Key);
                alg.GenerateKey();
                Assert.NotEqual(knownKey, alg.Key);

                alg.Mode = CipherMode.ECB;
                Assert.Equal(CipherMode.ECB, alg.Mode);

                alg.Padding = PaddingMode.PKCS7;
                Assert.Equal(PaddingMode.PKCS7, alg.Padding);
            }
        }
    }
}
