// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public sealed class MLKemOpenSslTests : MLKemBaseTests
    {
        public override MLKem GenerateKey(MLKemAlgorithm algorithm)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(algorithm.Name);
            return new MLKemOpenSsl(key);
        }

        public override MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> seed)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(algorithm.Name, seed);
            return new MLKemOpenSsl(key);
        }

        public override MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new MLKemOpenSsl(key);
        }

        public override MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: false);
            return new MLKemOpenSsl(key);
        }

        [Fact]
        public void MLKemOpenSsl_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("pkeyHandle", static () => new MLKemOpenSsl(null));
        }

        [Fact]
        public void MLKemOpenSsl_Ctor_InvalidHandle()
        {
            AssertExtensions.Throws<ArgumentException>("pkeyHandle", static () => new MLKemOpenSsl(new SafeEvpPKeyHandle()));
        }

        [Fact]
        public void MLKemOpenSsl_WrongAlgorithm()
        {
            using RSAOpenSsl rsa = new RSAOpenSsl();
            using SafeEvpPKeyHandle rsaHandle = rsa.DuplicateKeyHandle();
            Assert.Throws<CryptographicException>(() => new MLKemOpenSsl(rsaHandle));
        }

        [Fact]
        public void MLKemOpenSsl_DuplicateKeyHandle()
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(MLKemAlgorithm.MLKem512.Name);
            using MLKemOpenSsl kem = new(key);
            SafeEvpPKeyHandle secondKey;

            using (secondKey = kem.DuplicateKeyHandle())
            {
                Assert.False(secondKey.IsInvalid, nameof(secondKey.IsInvalid));
            }

            Assert.True(secondKey.IsInvalid, nameof(secondKey.IsInvalid));
            Assert.False(key.IsInvalid, nameof(key.IsInvalid));
            Assert.NotNull(kem.ExportPrivateSeed()); // Simple exercise to see the original KEM instance is still functional.
        }
    }
}
