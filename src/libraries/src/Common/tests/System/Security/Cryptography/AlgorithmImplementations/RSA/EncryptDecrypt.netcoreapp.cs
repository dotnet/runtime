// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class EncryptDecrypt_Span : EncryptDecrypt
    {
        protected override byte[] Encrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding) =>
            TryWithOutputArray(dest => rsa.TryEncrypt(data, dest, padding, out int bytesWritten) ? (true, bytesWritten) : (false, 0));

        protected override byte[] Decrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding) =>
            TryWithOutputArray(dest => rsa.TryDecrypt(data, dest, padding, out int bytesWritten) ? (true, bytesWritten) : (false, 0));

        private static byte[] TryWithOutputArray(Func<byte[], (bool, int)> func)
        {
            for (int length = 1; ; length = checked(length * 2))
            {
                var result = new byte[length];
                var (success, bytesWritten) = func(result);
                if (success)
                {
                    Array.Resize(ref result, bytesWritten);
                    return result;
                }
            }
        }

        [Fact]
        public void Decrypt_VariousSizeSpans_Success()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                byte[] cipherBytes = Encrypt(rsa, TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);
                byte[] actual;
                int bytesWritten;

                // Too small
                actual = new byte[TestData.HelloBytes.Length - 1];
                Assert.False(rsa.TryDecrypt(cipherBytes, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
                Assert.Equal(0, bytesWritten);
                Assert.Equal<byte>(new byte[actual.Length], actual);

                // Just right.
                actual = new byte[TestData.HelloBytes.Length];
                bool decrypted = rsa.TryDecrypt(cipherBytes, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten);

                Assert.True(decrypted);
                Assert.Equal(TestData.HelloBytes.Length, bytesWritten);
                Assert.Equal<byte>(TestData.HelloBytes, actual);

                // Bigger than needed
                actual = new byte[TestData.HelloBytes.Length + 1000];
                Assert.True(rsa.TryDecrypt(cipherBytes, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
                Assert.Equal(TestData.HelloBytes.Length, bytesWritten);
                Assert.Equal<byte>(TestData.HelloBytes, actual.AsSpan(0, TestData.HelloBytes.Length).ToArray());
            }
        }

        [Fact]
        public void Encrypt_VariousSizeSpans_Success()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                byte[] cipherBytes = Encrypt(rsa, TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);
                byte[] actual;
                int bytesWritten;

                // Too small
                actual = new byte[cipherBytes.Length - 1];
                Assert.False(rsa.TryEncrypt(TestData.HelloBytes, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
                Assert.Equal(0, bytesWritten);

                // Just right
                actual = new byte[cipherBytes.Length];
                Assert.True(rsa.TryEncrypt(TestData.HelloBytes, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
                Assert.Equal(cipherBytes.Length, bytesWritten);

                // Bigger than needed
                actual = new byte[cipherBytes.Length + 1];
                Assert.True(rsa.TryEncrypt(TestData.HelloBytes, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
                Assert.Equal(cipherBytes.Length, bytesWritten);
            }
        }

        [Fact]
        public void Decrypt_WrongKey_Pkcs7()
        {
            Decrypt_WrongKey(RSAEncryptionPadding.Pkcs1);
        }

        [Fact]
        public void Decrypt_WrongKey_OAEP_SHA1()
        {
            Decrypt_WrongKey(RSAEncryptionPadding.OaepSHA1);
        }

        [ConditionalFact(nameof(SupportsSha2Oaep))]
        public void Decrypt_WrongKey_OAEP_SHA256()
        {
            Decrypt_WrongKey(RSAEncryptionPadding.OaepSHA256);
        }

        private static void Decrypt_WrongKey(RSAEncryptionPadding padding)
        {
            using (RSA rsa1 = RSAFactory.Create())
            using (RSA rsa2 = RSAFactory.Create())
            {
                byte[] encrypted = rsa1.Encrypt(TestData.HelloBytes, padding);
                byte[] buf = new byte[encrypted.Length];
                buf.AsSpan().Fill(0xCA);

                int bytesWritten = 0;

                Assert.ThrowsAny<CryptographicException>(
                    () => rsa2.TryDecrypt(encrypted, buf, padding, out bytesWritten));

                Assert.Equal(0, bytesWritten);
                Assert.True(buf.All(b => b == 0xCA));
            }
        }
    }
}
