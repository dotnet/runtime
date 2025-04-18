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
        public void SlhDsaOpenSsl_Ctor_InvalidHandle()
        {
            AssertExtensions.Throws<ArgumentException>("pkeyHandle", static () => new SlhDsaOpenSsl(new SafeEvpPKeyHandle()));
        }

        [Fact]
        public void SlhDsaOpenSsl_WrongAlgorithm()
        {
            using RSAOpenSsl rsa = new RSAOpenSsl();
            using SafeEvpPKeyHandle rsaHandle = rsa.DuplicateKeyHandle();
            Assert.Throws<CryptographicException>(() => new SlhDsaOpenSsl(rsaHandle));
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public void SlhDsaOpenSsl_DuplicateKeyHandle()
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.SlhDsaGenerateKey(SlhDsaAlgorithm.SlhDsaSha2_128s.Name);
            using SlhDsaOpenSsl slhDsa = new(key);
            Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            SafeEvpPKeyHandle secondKey;

            using (secondKey = slhDsa.DuplicateKeyHandle())
            {
                AssertExtensions.FalseExpression(secondKey.IsInvalid);
            }

            AssertExtensions.TrueExpression(secondKey.IsInvalid);
            AssertExtensions.FalseExpression(key.IsInvalid);

            VerifyInstanceIsUsable(slhDsa);
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public void SlhDsaOpenSsl_DuplicateKeyHandleLifetime()
        {
            SlhDsaOpenSsl one;
            SlhDsaOpenSsl two;

            using (SafeEvpPKeyHandle key = Interop.Crypto.SlhDsaGenerateKey(SlhDsaAlgorithm.SlhDsaSha2_128s.Name))
            {
                one = new SlhDsaOpenSsl(key);
                Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_128s, one.Algorithm);
            }

            using (SafeEvpPKeyHandle dup = one.DuplicateKeyHandle())
            {
                two = new SlhDsaOpenSsl(dup);
                Assert.Same(SlhDsaAlgorithm.SlhDsaSha2_128s, one.Algorithm);
            }

            using (two)
            {
                byte[] data = [1, 1, 2, 3, 5, 8];
                byte[] context = [13, 21];
                byte[] oneSignature = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.SignatureSizeInBytes];

                using (one)
                {
                    Assert.Equal(oneSignature.Length, one.SignData(data, oneSignature, context));
                    VerifyInstanceIsUsable(one);
                    VerifyInstanceIsUsable(two);
                }

                VerifyDisposed(one);
                Assert.Throws<ObjectDisposedException>(() => one.DuplicateKeyHandle());

                VerifyInstanceIsUsable(two);
                ExerciseSuccessfulVerify(two, data, oneSignature, context);
            }

            VerifyDisposed(two);
            Assert.Throws<ObjectDisposedException>(() => two.DuplicateKeyHandle());
        }

        private static void VerifyInstanceIsUsable(SlhDsaOpenSsl slhDsa)
        {
            byte[] secretKey = new byte[slhDsa.Algorithm.SecretKeySizeInBytes];
            Assert.Equal(slhDsa.Algorithm.SecretKeySizeInBytes, slhDsa.ExportSlhDsaSecretKey(secretKey)); // does not throw

            // usable
            byte[] data = [1, 2, 3];
            byte[] context = [4];
            byte[] signature = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.SignatureSizeInBytes];
            slhDsa.SignData(data, signature, context);

            ExerciseSuccessfulVerify(slhDsa, data, signature, context);
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
