// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests.Pkcs12
{
    public static class KeyBagTests
    {
        private static readonly ReadOnlyMemory<byte> s_derNull = new byte[] { 0x05, 0x00 };

        [Fact]
        public static void BuildWithFactoryReadDirect()
        {
            using (RSA rsa = RSA.Create())
            {
                Pkcs12SafeContents contents = new Pkcs12SafeContents();
                Pkcs12KeyBag keyBag = contents.AddKeyUnencrypted(rsa);

                using (RSA rsa2 = RSA.Create())
                {
                    rsa2.ImportPkcs8PrivateKey(
                        keyBag.Pkcs8PrivateKey.Span,
                        out _);

                    byte[] sig = new byte[rsa.KeySize / 8];

                    Assert.True(rsa2.TrySignData(
                        keyBag.Pkcs8PrivateKey.Span,
                        sig,
                        HashAlgorithmName.MD5,
                        RSASignaturePadding.Pkcs1,
                        out int sigLen));

                    Assert.Equal(sig.Length, sigLen);

                    Assert.True(rsa.VerifyData(
                        keyBag.Pkcs8PrivateKey.Span,
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
            Pkcs12KeyBag keyBag = new Pkcs12KeyBag(s_derNull, skipCopy);

            if (skipCopy)
            {
                Assert.True(
                    s_derNull.Span.Overlaps(keyBag.Pkcs8PrivateKey.Span),
                    "Same memory");
            }
            else
            {
                Assert.False(
                    s_derNull.Span.Overlaps(keyBag.Pkcs8PrivateKey.Span),
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
            Func<Pkcs12KeyBag> func = () => new Pkcs12KeyBag(data, skipCopy: true);

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
