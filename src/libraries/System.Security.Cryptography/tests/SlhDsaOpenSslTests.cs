// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    /// <summary>
    /// Tests for <see cref="SlhDsaOpenSsl"/> that depend on OpenSSL support for SLH-DSA.
    /// </summary>
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public sealed class SlhDsaOpenSslTests : SlhDsaTests
    {
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
                byte[] oneSignature;

                using (one)
                {
                    oneSignature = one.SignData(data, context);
                    VerifyInstanceIsUsable(one);
                    VerifyInstanceIsUsable(two);
                }

                SlhDsaTestHelpers.VerifyDisposed(one);
                Assert.Throws<ObjectDisposedException>(() => one.DuplicateKeyHandle());

                VerifyInstanceIsUsable(two);
                ExerciseSuccessfulVerify(two, data, oneSignature, context);
            }

            SlhDsaTestHelpers.VerifyDisposed(two);
            Assert.Throws<ObjectDisposedException>(() => two.DuplicateKeyHandle());
        }

        private static void VerifyInstanceIsUsable(SlhDsaOpenSsl slhDsa)
        {
            _ = slhDsa.ExportSlhDsaPrivateKey(); // does not throw

            // usable
            byte[] data = [1, 2, 3];
            byte[] context = [4];
            byte[] signature = slhDsa.SignData(data, context);

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

        protected override SlhDsa ImportSlhDsaPrivateKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new SlhDsaOpenSsl(key);
        }
    }
}
