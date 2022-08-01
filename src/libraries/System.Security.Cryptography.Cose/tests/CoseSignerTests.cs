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
            var signer = new CoseSigner(ECDsa.Create(), HashAlgorithmName.SHA256);
            Assert.NotNull(signer.ProtectedHeaders);
            Assert.NotNull(signer.UnprotectedHeaders);
            Assert.Null(signer.RSASignaturePadding);
        }

        [Fact]
        public void CoseSigner_RSA_Success()
        {
            var signer = new CoseSigner(RSA.Create(), RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA256);
            Assert.NotNull(signer.ProtectedHeaders);
            Assert.NotNull(signer.UnprotectedHeaders);
            Assert.NotNull(signer.RSASignaturePadding);
        }

        [Fact]
        public void CoseSigner_RSAKeyNeedsSignaturePadding()
        {
            RSA rsa = RSA.Create();
            Assert.Throws<CryptographicException>(() => new CoseSigner(rsa, HashAlgorithmName.SHA256));

            var signer = new CoseSigner(rsa, RSASignaturePadding.Pss, HashAlgorithmName.SHA256);
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
            Assert.Throws<ArgumentNullException>("key", () => new CoseSigner(null!, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>("key", () => new CoseSigner(null!, RSASignaturePadding.Pss, HashAlgorithmName.SHA256));
        }
    }
}
