// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseSignerTests
    {
        [Fact]
        public void CoseSigner_ECDsa_Success()
        {
            var signer = new CoseSigner(CoseTestHelpers.ES256, HashAlgorithmName.SHA256);
            Assert.NotNull(signer.ProtectedHeaders);
            Assert.NotNull(signer.UnprotectedHeaders);
            Assert.Null(signer.RSASignaturePadding);
            Assert.Same(CoseTestHelpers.ES256, signer.Key);
        }

        [Fact]
        public void CoseSigner_RSA_Success()
        {
            var signer = new CoseSigner(CoseTestHelpers.RSAKey, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA256);
            Assert.NotNull(signer.ProtectedHeaders);
            Assert.NotNull(signer.UnprotectedHeaders);
            Assert.NotNull(signer.RSASignaturePadding);
            Assert.Same(CoseTestHelpers.RSAKey, signer.Key);
        }

        [Fact]
        public void CoseSigner_CoseKey_ECDsa_Success()
        {
            CoseKey key = new CoseKey(CoseTestHelpers.ES256, HashAlgorithmName.SHA256);
            var signer = new CoseSigner(key);
            Assert.NotNull(signer.ProtectedHeaders);
            Assert.NotNull(signer.UnprotectedHeaders);
            Assert.Null(signer.RSASignaturePadding);
            Assert.Same(CoseTestHelpers.ES256, signer.Key);
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public void CoseSigner_CoseKey_MLDsa_Success()
        {
            CoseKey key = new CoseKey(CoseTestHelpers.MLDsa44Key);
            var signer = new CoseSigner(key);
            Assert.NotNull(signer.ProtectedHeaders);
            Assert.NotNull(signer.UnprotectedHeaders);
            Assert.Null(signer.RSASignaturePadding);
            Assert.Null(signer.Key);
        }

        [Fact]
        public void CoseSigner_RSAKeyNeedsSignaturePadding()
        {
            Assert.Throws<ArgumentException>("key", () => new CoseSigner(CoseTestHelpers.RSAKey, HashAlgorithmName.SHA256));

            var signer = new CoseSigner(CoseTestHelpers.RSAKey, RSASignaturePadding.Pss, HashAlgorithmName.SHA256);
            Assert.Equal(signer.RSASignaturePadding, RSASignaturePadding.Pss);
        }

        [Fact]
        public void CoseSigner_UnsupportedKeyThrows()
        {
            Assert.Throws<ArgumentException>("key", () => new CoseSigner(ECDiffieHellman.Create(), HashAlgorithmName.SHA256));
        }

        [Fact]
        public void CoseSigner_NullKey()
        {
            Assert.Throws<ArgumentNullException>("key", () => new CoseSigner((CoseKey)null!));
            Assert.Throws<ArgumentNullException>("key", () => new CoseSigner(null!, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>("key", () => new CoseSigner(null!, RSASignaturePadding.Pss, HashAlgorithmName.SHA256));
        }

        [Fact]
        public void CoseSigner_NullSignaturePadding()
        {
            Assert.Throws<ArgumentNullException>("signaturePadding", () => new CoseSigner(RSA.Create(), null!, HashAlgorithmName.SHA256));
        }

#if NET11_0_OR_GREATER
        [Theory]
        [InlineData("SHA256", 32)]
        [InlineData("SHA384", 48)]
        [InlineData("SHA512", 64)]
        public void CoseSigner_PssPaddingWithExplicitHashLength(string hashAlgorithmName, int saltLength)
        {
            CoseSigner signer = new CoseSigner(
                RSA.Create(),
                RSASignaturePadding.CreatePss(saltLength),
                new HashAlgorithmName(hashAlgorithmName));

            RSASignaturePadding padding = Assert.IsType<RSASignaturePadding>(signer.RSASignaturePadding);
            Assert.Equal(saltLength, padding.PssSaltLength);
        }

        [Theory]
        [InlineData("SHA256", 0)]
        [InlineData("SHA256", 17)]
        [InlineData("SHA256", 31)]
        [InlineData("SHA256", 33)]
        [InlineData("SHA256", RSASignaturePadding.PssSaltLengthMax)]
        [InlineData("SHA384", 32)]
        public void CoseSigner_PssPaddingWithInvalidSaltLength(string hashAlgorithmName, int saltLength)
        {
            Assert.Throws<ArgumentException>(
                "signaturePadding",
                () => new CoseSigner(
                    RSA.Create(),
                    RSASignaturePadding.CreatePss(saltLength),
                    new HashAlgorithmName(hashAlgorithmName)));
        }
#endif
    }
}
