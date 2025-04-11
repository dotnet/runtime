// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaOpenSslConstructionTests : SlhDsaConstructionTestsBase
    {
        [Fact]
        public void SlhDsaOpenSsl_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("pkeyHandle", static () => new SlhDsaOpenSsl(null));
        }

        [Fact]
        public void SlhDsaOpenSsl_WrongAlgorithm()
        {
            using RSAOpenSsl rsa = new RSAOpenSsl();
            using SafeEvpPKeyHandle rsaHandle = rsa.DuplicateKeyHandle();
            Assert.Throws<CryptographicException>(() => new SlhDsaOpenSsl(rsaHandle));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void SlhDsaOpenSsl_DuplicateKeyHandle(SlhDsaAlgorithm algorithm)
        {
            SlhDsaOpenSsl orig;
            using (SafeEvpPKeyHandle key = Interop.Crypto.SlhDsaGenerateKey(algorithm.Name))
            {
                orig = new SlhDsaOpenSsl(key);
            }

            SlhDsaOpenSsl dup;
            using (SafeEvpPKeyHandle key = orig.DuplicateKeyHandle())
            {
                dup = new SlhDsaOpenSsl(key);
            }

            Span<byte> msg = [42];
            Span<byte> ctx = [1, 2, 3];
            byte[] sig = new byte[algorithm.SignatureSizeInBytes];

            // Both can sign/verify
            Assert.Equal(algorithm.SignatureSizeInBytes, orig.SignData(msg, sig, ctx));
            AssertExtensions.TrueExpression(dup.VerifyData(msg, sig, ctx));

            sig.AsSpan().Clear();
            Assert.Equal(algorithm.SignatureSizeInBytes, dup.SignData(msg, sig, ctx));
            AssertExtensions.TrueExpression(orig.VerifyData(msg, sig, ctx));

            // Disposing the original key should not affect the duplicate
            orig.Dispose();

            AssertExtensions.TrueExpression(dup.VerifyData(msg, sig, ctx));

            sig.AsSpan().Clear();
            Assert.Equal(algorithm.SignatureSizeInBytes, dup.SignData(msg, sig, ctx));
            AssertExtensions.TrueExpression(dup.VerifyData(msg, sig, ctx));
        }

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.SlhDsaGenerateKey(algorithm.Name);
            return new SlhDsaOpenSsl(key);
        }

        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: false);
            return new SlhDsaOpenSsl(key);
        }

        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new SlhDsaOpenSsl(key);
        }
    }
}
