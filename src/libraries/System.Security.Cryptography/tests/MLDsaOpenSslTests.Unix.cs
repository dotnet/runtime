// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
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
            using SafeEvpPKeyHandle key = Interop.Crypto.MLDsaGenerateKey(algorithm.Name, source);
            return new MLDsaOpenSsl(key);
        }

        protected override MLDsa ImportPrivateKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new MLDsaOpenSsl(key);
        }

        protected override MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: false);
            return new MLDsaOpenSsl(key);
        }

        [Fact]
        public void MLDsaOpenSsl_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("pkeyHandle", static () => new MLDsaOpenSsl(null));
        }

        [Fact]
        public void MLDsaOpenSsl_Ctor_InvalidHandle()
        {
            AssertExtensions.Throws<ArgumentException>("pkeyHandle", static () => new MLDsaOpenSsl(new SafeEvpPKeyHandle()));
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
            using SafeEvpPKeyHandle key = Interop.Crypto.MLDsaGenerateKey(MLDsaAlgorithm.MLDsa44.Name, ReadOnlySpan<byte>.Empty);
            using MLDsaOpenSsl mldsa = new(key);
            Assert.Same(MLDsaAlgorithm.MLDsa44, mldsa.Algorithm);

            SafeEvpPKeyHandle secondKey;

            using (secondKey = mldsa.DuplicateKeyHandle())
            {
                AssertExtensions.FalseExpression(secondKey.IsInvalid);
            }

            AssertExtensions.TrueExpression(secondKey.IsInvalid);
            AssertExtensions.FalseExpression(key.IsInvalid);

            VerifyInstanceIsUsable(mldsa);
        }

        [Fact]
        public void MLDsaOpenSsl_DuplicateKeyHandleLifetime()
        {
            MLDsaOpenSsl one;
            MLDsaOpenSsl two;

            using (SafeEvpPKeyHandle key = Interop.Crypto.MLDsaGenerateKey(MLDsaAlgorithm.MLDsa44.Name, ReadOnlySpan<byte>.Empty))
            {
                one = new MLDsaOpenSsl(key);
                Assert.Same(MLDsaAlgorithm.MLDsa44, one.Algorithm);
            }

            using (SafeEvpPKeyHandle dup = one.DuplicateKeyHandle())
            {
                two = new MLDsaOpenSsl(dup);
                Assert.Same(MLDsaAlgorithm.MLDsa44, one.Algorithm);
            }

            using (two)
            {
                byte[] data = [ 1, 1, 2, 3, 5, 8 ];
                byte[] context = [ 13, 21 ];
                byte[] oneSignature;

                using (one)
                {
                    oneSignature = one.SignData(data, context);
                    VerifyInstanceIsUsable(one);
                    VerifyInstanceIsUsable(two);
                }

                MLDsaTestHelpers.VerifyDisposed(one);
                Assert.Throws<ObjectDisposedException>(() => one.DuplicateKeyHandle());

                VerifyInstanceIsUsable(two);
                ExerciseSuccessfulVerify(two, data, oneSignature, context);
            }

            MLDsaTestHelpers.VerifyDisposed(two);
            Assert.Throws<ObjectDisposedException>(() => two.DuplicateKeyHandle());
        }

        private static void VerifyInstanceIsUsable(MLDsaOpenSsl mldsa)
        {
            byte[] seed = mldsa.ExportMLDsaPrivateSeed();
            Assert.Equal(mldsa.Algorithm.PrivateSeedSizeInBytes, seed.Length);

            byte[] privateKey = mldsa.ExportMLDsaPrivateKey();
            Assert.Equal(mldsa.Algorithm.PrivateKeySizeInBytes, privateKey.Length);

            // usable
            byte[] data = [ 1, 2, 3 ];
            byte[] context = [ 4 ];
            byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];
            mldsa.SignData(data, signature, context);
            ExerciseSuccessfulVerify(mldsa, data, signature, context);

            byte[] hash = HashInfo.Sha256.GetHash(data);
            signature.AsSpan().Fill(0);
            mldsa.SignPreHash(hash.AsSpan(), signature, HashInfo.Sha256.Oid, context);
            ExerciseSuccessfulVerifyPreHash(mldsa, HashInfo.Sha256.Oid, hash, signature, context);
        }
    }
}
