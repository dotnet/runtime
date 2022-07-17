// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class ShroudedKeyBagTests
    {
        private static readonly PbeParameters s_win7Pbe = new PbeParameters(
            PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
            HashAlgorithmName.SHA1,
            2000);

        private static readonly PbeParameters s_pbkdf2Pbe = new PbeParameters(
            PbeEncryptionAlgorithm.Aes256Cbc,
            HashAlgorithmName.SHA256,
            2000);

        private static readonly ReadOnlyMemory<byte> s_derNull = new byte[] { 0x05, 0x00 };

        [Fact]
        public static void BuildWithCharsFactoryReadDirect()
        {
            using (RSA rsa = RSA.Create())
            {
                Pkcs12SafeContents contents = new Pkcs12SafeContents();
                Pkcs12ShroudedKeyBag keyBag = contents.AddShroudedKey(rsa, nameof(rsa), s_win7Pbe);

                using (RSA rsa2 = RSA.Create())
                {
                    rsa2.ImportEncryptedPkcs8PrivateKey(
                        (ReadOnlySpan<char>)nameof(rsa),
                        keyBag.EncryptedPkcs8PrivateKey.Span,
                        out _);

                    byte[] sig = new byte[rsa.KeySize / 8];

                    Assert.True(rsa2.TrySignData(
                        keyBag.EncryptedPkcs8PrivateKey.Span,
                        sig,
                        HashAlgorithmName.MD5,
                        RSASignaturePadding.Pkcs1,
                        out int sigLen));

                    Assert.Equal(sig.Length, sigLen);

                    Assert.True(rsa.VerifyData(
                        keyBag.EncryptedPkcs8PrivateKey.Span,
                        sig,
                        HashAlgorithmName.MD5,
                        RSASignaturePadding.Pkcs1));
                }
            }
        }

        [Fact]
        public static void BuildWithBytesFactoryReadDirect()
        {
            using (RSA rsa = RSA.Create())
            {
                Pkcs12SafeContents contents = new Pkcs12SafeContents();
                byte[] encryptionKey = new byte[] { 1, 2, 3, 4, 5 };

                Pkcs12ShroudedKeyBag keyBag = contents.AddShroudedKey(rsa, encryptionKey, s_pbkdf2Pbe);

                using (RSA rsa2 = RSA.Create())
                {
                    rsa2.ImportEncryptedPkcs8PrivateKey(
                        encryptionKey,
                        keyBag.EncryptedPkcs8PrivateKey.Span,
                        out _);

                    byte[] sig = new byte[rsa.KeySize / 8];

                    Assert.True(rsa2.TrySignData(
                        keyBag.EncryptedPkcs8PrivateKey.Span,
                        sig,
                        HashAlgorithmName.MD5,
                        RSASignaturePadding.Pkcs1,
                        out int sigLen));

                    Assert.Equal(sig.Length, sigLen);

                    Assert.True(rsa.VerifyData(
                        keyBag.EncryptedPkcs8PrivateKey.Span,
                        sig,
                        HashAlgorithmName.MD5,
                        RSASignaturePadding.Pkcs1));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void SkipCopyHonored(bool skipCopy)
        {
            Pkcs12ShroudedKeyBag shroudedKeyBag = new Pkcs12ShroudedKeyBag(s_derNull, skipCopy);

            if (skipCopy)
            {
                Assert.True(
                    s_derNull.Span.Overlaps(shroudedKeyBag.EncryptedPkcs8PrivateKey.Span),
                    "Same memory");
            }
            else
            {
                Assert.False(
                    s_derNull.Span.Overlaps(shroudedKeyBag.EncryptedPkcs8PrivateKey.Span),
                    "Same memory");
            }
        }

        [Theory]
        // No data
        [InlineData("", false)]
        // Length exceeds payload
        [InlineData("0401", false)]
        // Two values (aka length undershoots payload)
        [InlineData("0400020100", false)]
        // No length
        [InlineData("04", false)]
        // Legal
        [InlineData("0400", true)]
        // A legal tag-length-value, but not a legal BIT STRING value.
        [InlineData("0300", true)]
        // SEQUENCE (indefinite length) {
        //   Constructed OCTET STRING (indefinite length) {
        //     OCTET STRING (inefficient encoded length 01): 07
        //   }
        // }
        [InlineData("30802480048200017F00000000", true)]
        // Previous example, trailing byte
        [InlineData("30802480048200017F0000000000", false)]
        public static void CtorEnsuresValidBerValue(string inputHex, bool expectSuccess)
        {
            byte[] data = inputHex.HexToByteArray();
            Func<Pkcs12ShroudedKeyBag> func = () => new Pkcs12ShroudedKeyBag(data, skipCopy: true);

            if (!expectSuccess)
            {
                Assert.ThrowsAny<CryptographicException>(func);
            }
            else
            {
                // Assert.NoThrow
                func();
            }
        }
    }
}
