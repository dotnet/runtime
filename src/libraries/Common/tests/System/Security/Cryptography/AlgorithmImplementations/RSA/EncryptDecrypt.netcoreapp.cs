// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
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

        [Fact]
        public static void EncryptDefaultSpan()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                byte[] dest = new byte[rsa.KeySize / 8];

                Assert.True(
                    rsa.TryEncrypt(ReadOnlySpan<byte>.Empty, dest, RSAEncryptionPadding.Pkcs1, out int written));

                Assert.Equal(dest.Length, written);

                Assert.True(
                    rsa.TryEncrypt(ReadOnlySpan<byte>.Empty, dest, RSAEncryptionPadding.OaepSHA1, out written));

                Assert.Equal(dest.Length, written);
            }
        }

        private static void Decrypt_WrongKey(RSAEncryptionPadding padding)
        {
            using (RSA rsa1 = RSAFactory.Create())
            using (RSA rsa2 = RSAFactory.Create())
            {
                byte[] input = TestData.HelloBytes;
                byte[] encrypted = rsa1.Encrypt(input, padding);
                byte[] buf = new byte[encrypted.Length];
                buf.AsSpan().Fill(0xCA);

                int bytesWritten = 0;

                // PKCS#1 padding allows for an incorrect response when the random key produces
                // a start sequence of [ 00 02 !00 !00 !00 !00 !00 !00 !00 !00 ] with an eventual zero.
                // 1/256 * 1/256 * (255/256)^8 is the start.
                // 1 - (255/256)^(keySizeInBytes - 10) is the probability that there's an eventual zero.
                // For RSA 2048, this works out to 9.142e-6, or 1 in 109385.
                //
                // OAEP is harder to reason about, it requires a bunch of data that hashes to a value
                // that, XOR the remaining data, produces a value that is equivalent to a randomly
                // generated number (which then runs through a formula and XORs back to "the input").
                // The odds of that working are hard to say, but certainly much harder. Probably
                // somewhere between 1 in 2^160 ("you made the right SHA-1 output") and 1 in 2^2048
                // (you made the right private key). Since we already have the "if it succeeds, be wrong"
                // code, just use it for OAEP, too.

                try
                {
                    // Because buf.Length >= ((rsa.KeySize / 8) - 11) (because it's rsa.KeySize / 8)
                    // false should never be returned.
                    //
                    // But if the padding doesn't work out (109384 out of 109385, see above) we'll throw.
                    bool decrypted = rsa2.TryDecrypt(encrypted, buf, padding, out bytesWritten);
                    Assert.True(decrypted, "Pkcs1 TryDecrypt succeeded with a large buffer");

                    // If bytesWritten != input.Length, we got back gibberish, which is good.
                    // If bytesWritten == input.Length then make sure at least one of the bytes is wrong.

                    // Probability time again.
                    // For RSA-2048, producing the input (ASCII("Hello")) requires a buffer of
                    // [ 00 02 248-nonzero-bytes 00 48 65 6C 6C 6F ]
                    // (1/256)^8 * (255/256)^248 = 3.2e-20, so this will fail 1 in 3.125e19 runs.
                    // One run a second => one failure every 990 billion years.
                    // (If our implementation is bad/weird, then all of these odds are meaningless, hence the test)
                    //
                    // For RSA-1024 (less freedom) it's 1 in 5.363e19, so like 1.6 trillion years.

                    if (rsa2.TryDecrypt(encrypted, buf, padding, out bytesWritten)
                        && bytesWritten == input.Length)
                    {
                        // We'll get -here- 1 in 111014 runs (RSA-2048 Pkcs1).
                        Assert.NotEqual(input, buf.AsSpan(0, bytesWritten).ToArray());
                    }
                }
                catch (CryptographicException)
                {
                    // 109384 out of 109385 times (RSA-2048 Pkcs1) we get here.

                    Assert.Equal(0, bytesWritten);
                    Assert.True(buf.All(b => b == 0xCA));
                }
            }
        }
    }
}
