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

#if NET10_0_OR_GREATER
        [Theory]
        [InlineData(0)]
        [InlineData(17)]
        [InlineData(32)]
        [InlineData(RSASignaturePadding.PssSaltLengthMax)]
        public void CoseSigner_PssPaddingWithInvalidSaltLength(int saltLength)
        {
            Assert.Throws<ArgumentException>("signaturePadding", () => new CoseSigner(RSA.Create(), RSASignaturePadding.CreatePss(saltLength), HashAlgorithmName.SHA256));
        }
#endif
    }
}
