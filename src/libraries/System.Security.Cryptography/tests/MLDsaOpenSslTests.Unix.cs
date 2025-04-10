// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public sealed class MLDsaOpenSslTests : MLDsaTestsBase
    {
        protected override MLDsa GenerateKey(MLDsaAlgorithm algorithm)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.MLDsaGenerateKey(algorithm.Name, ReadOnlySpan<byte>.Empty);
            return new MLDsaOpenSsl(key);
        }

        protected override MLDsa ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            SafeEvpPKeyHandle key = Interop.Crypto.MLDsaGenerateKey(algorithm.Name, source);
            return new MLDsaOpenSsl(key);
        }

        protected override MLDsa ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new MLDsaOpenSsl(key);
        }

        protected override MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: false);
            return new MLDsaOpenSsl(key);
        }

        [Fact]
        public void MLDsaOpenSsl_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("pkeyHandle", static () => new MLDsaOpenSsl(null));
        }

        [Fact]
        public void MLDsaOpenSsl_WrongAlgorithm()
        {
            using RSAOpenSsl rsa = new RSAOpenSsl();
            using SafeEvpPKeyHandle rsaHandle = rsa.DuplicateKeyHandle();
            Assert.Throws<CryptographicException>(() => new MLDsaOpenSsl(rsaHandle));
        }

        [Fact]
        public void MLDsaOpenSsl_DuplicateKeyHandle()
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(MLDsaAlgorithm.MLDsa44.Name);
            using MLDsaOpenSsl mldsa = new(key);
            SafeEvpPKeyHandle secondKey;

            using (secondKey = mldsa.DuplicateKeyHandle())
            {
                Assert.False(secondKey.IsInvalid, nameof(secondKey.IsInvalid));
            }

            Assert.True(secondKey.IsInvalid, nameof(secondKey.IsInvalid));
            Assert.False(key.IsInvalid, nameof(key.IsInvalid));

            byte[] seed = new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes];
            Assert.NotEqual(0, mldsa.ExportMLDsaPrivateSeed(seed)); // does not throw
        }
    }
}
