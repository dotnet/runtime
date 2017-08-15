// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

                // Just right... but that may be insufficient on Unix, where with padding the output destination
                // may need to be larger than the actual decrypted content.
                actual = new byte[TestData.HelloBytes.Length];
                bool decrypted = rsa.TryDecrypt(cipherBytes, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten);
                if (RSAFactory.SupportsDecryptingIntoExactSpaceRequired || decrypted)
                {
                    Assert.True(decrypted);
                    Assert.Equal(TestData.HelloBytes.Length, bytesWritten);
                    Assert.Equal<byte>(TestData.HelloBytes, actual);
                }
                else
                {
                    Assert.Equal(0, bytesWritten);
                    Assert.Equal<byte>(new byte[actual.Length], actual);
                }

                // Bigger than needed
                actual = new byte[TestData.HelloBytes.Length + 1000];
                Assert.True(rsa.TryDecrypt(cipherBytes, actual, RSAEncryptionPadding.OaepSHA1, out bytesWritten));
                Assert.Equal(TestData.HelloBytes.Length, bytesWritten);
                Assert.Equal<byte>(TestData.HelloBytes, actual.AsSpan().Slice(0, TestData.HelloBytes.Length).ToArray());
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
    }
}
