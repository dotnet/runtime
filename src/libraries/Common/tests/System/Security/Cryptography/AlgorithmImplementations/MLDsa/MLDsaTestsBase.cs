// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public abstract class MLDsaTestsBase
    {
        protected abstract MLDsa GenerateKey(MLDsaAlgorithm algorithm);
        protected abstract MLDsa ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> seed);
        protected abstract MLDsa ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        protected abstract MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void AlgorithmIsAssigned(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            Assert.Same(algorithm, mldsa.Algorithm);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyNoContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] data = [ 1, 2, 3, 4, 5 ];
            byte[] signature = new byte[mldsa.Algorithm.SignatureSizeInBytes];
            Assert.Equal(signature.Length, mldsa.SignData(data, signature));

            ExerciseSuccessfulVerify(mldsa, data, signature, []);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyWithContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] context = [ 1, 1, 3, 5, 6 ];
            byte[] data = [ 1, 2, 3, 4, 5 ];
            byte[] signature = new byte[mldsa.Algorithm.SignatureSizeInBytes];
            Assert.Equal(signature.Length, mldsa.SignData(data, signature, context));

            ExerciseSuccessfulVerify(mldsa, data, signature, context);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyEmptyMessageNoContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] signature = new byte[mldsa.Algorithm.SignatureSizeInBytes];
            Assert.Equal(signature.Length, mldsa.SignData([], signature));

            ExerciseSuccessfulVerify(mldsa, [], signature, []);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyEmptyMessageWithContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] signature = new byte[mldsa.Algorithm.SignatureSizeInBytes];
            Assert.Equal(signature.Length, mldsa.SignData([], signature, context));

            ExerciseSuccessfulVerify(mldsa, [], signature, context);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignExportPublicVerifyWithPublicOnly(MLDsaAlgorithm algorithm)
        {
            byte[] publicKey;
            byte[] data = [ 1, 2, 3, 4, 5 ];
            byte[] signature;

            using (MLDsa mldsa = GenerateKey(algorithm))
            {
                signature = new byte[algorithm.SignatureSizeInBytes];
                Assert.Equal(signature.Length, mldsa.SignData(data, signature));
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                publicKey = new byte[algorithm.PublicKeySizeInBytes];
                Assert.Equal(publicKey.Length, mldsa.ExportMLDsaPublicKey(publicKey));
            }

            using (MLDsa mldsaPub = ImportPublicKey(algorithm, publicKey))
            {
                ExerciseSuccessfulVerify(mldsaPub, data, signature, []);
            }
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateExportSecretKeySignAndVerify(MLDsaAlgorithm algorithm)
        {
            byte[] secretKey;
            byte[] data = [ 1, 2, 3, 4, 5 ];
            byte[] signature;

            using (MLDsa mldsaTmp = GenerateKey(algorithm))
            {
                signature = new byte[algorithm.SignatureSizeInBytes];
                Assert.Equal(signature.Length, mldsaTmp.SignData(data, signature));

                secretKey = new byte[algorithm.SecretKeySizeInBytes];
                Assert.Equal(secretKey.Length, mldsaTmp.ExportMLDsaSecretKey(secretKey));
            }

            using (MLDsa mldsa = ImportSecretKey(algorithm, secretKey))
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                signature.AsSpan().Fill(0);
                Assert.Equal(signature.Length, mldsa.SignData(data, signature));

                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));
                data[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature));
            }
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateExportPrivateSeedSignAndVerify(MLDsaAlgorithm algorithm)
        {
            byte[] privateSeed;
            byte[] data = [ 1, 2, 3, 4, 5 ];
            byte[] signature;

            using (MLDsa mldsaTmp = GenerateKey(algorithm))
            {
                signature = new byte[algorithm.SignatureSizeInBytes];
                Assert.Equal(signature.Length, mldsaTmp.SignData(data, signature));

                privateSeed = new byte[algorithm.PrivateSeedSizeInBytes];
                Assert.Equal(privateSeed.Length, mldsaTmp.ExportMLDsaPrivateSeed(privateSeed));
            }

            using (MLDsa mldsa = ImportPrivateSeed(algorithm, privateSeed))
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                signature.AsSpan().Fill(0);
                Assert.Equal(signature.Length, mldsa.SignData(data, signature));

                ExerciseSuccessfulVerify(mldsa, data, signature, []);
            }
        }

        [Fact]
        public void ImportSecretKey_CannotReconstructSeed()
        {
            byte[] secretKey = new byte[MLDsaAlgorithm.MLDsa44.SecretKeySizeInBytes];
            using (MLDsa mldsaOriginal = GenerateKey(MLDsaAlgorithm.MLDsa44))
            {
                Assert.Equal(secretKey.Length, mldsaOriginal.ExportMLDsaSecretKey(secretKey));
            }

            using (MLDsa mldsa = ImportSecretKey(MLDsaAlgorithm.MLDsa44, secretKey))
            {
                Assert.Throws<CryptographicException>(() => mldsa.ExportMLDsaPrivateSeed(new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes]));
            }
        }

        [Fact]
        public void ImportSeed_CanReconstructSecretKey()
        {
            byte[] secretKey = new byte[MLDsaAlgorithm.MLDsa44.SecretKeySizeInBytes];
            byte[] seed = new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes];
            using (MLDsa mldsaOriginal = GenerateKey(MLDsaAlgorithm.MLDsa44))
            {
                Assert.Equal(secretKey.Length, mldsaOriginal.ExportMLDsaSecretKey(secretKey));
                Assert.Equal(seed.Length, mldsaOriginal.ExportMLDsaPrivateSeed(seed));
            }

            using (MLDsa mldsa = ImportPrivateSeed(MLDsaAlgorithm.MLDsa44, seed))
            {
                byte[] secretKey2 = new byte[MLDsaAlgorithm.MLDsa44.SecretKeySizeInBytes];
                byte[] seed2 = new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes];

                Assert.Equal(secretKey2.Length, mldsa.ExportMLDsaSecretKey(secretKey2));
                Assert.Equal(seed2.Length, mldsa.ExportMLDsaPrivateSeed(seed2));

                AssertExtensions.SequenceEqual(secretKey, secretKey2);
                AssertExtensions.SequenceEqual(seed, seed2);
            }
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllNistTestCases), MemberType = typeof(MLDsaTestsData))]
        public void NistImportPublicKeyVerify(MLDsaNistTestCase testCase)
        {
            using MLDsa mldsa = ImportPublicKey(testCase.Algorithm, testCase.PublicKey);
            Assert.Equal(testCase.ShouldPass, mldsa.VerifyData(testCase.Message, testCase.Signature, testCase.Context));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllNistTestCases), MemberType = typeof(MLDsaTestsData))]
        public void NistImportSecretKeyVerifyExportsAndSignature(MLDsaNistTestCase testCase)
        {
            using MLDsa mldsa = ImportSecretKey(testCase.Algorithm, testCase.SecretKey);

            byte[] pubKey = new byte[testCase.Algorithm.PublicKeySizeInBytes];
            Assert.Equal(pubKey.Length, mldsa.ExportMLDsaPublicKey(pubKey));
            AssertExtensions.SequenceEqual(testCase.PublicKey, pubKey);

            byte[] secretKey = new byte[testCase.Algorithm.SecretKeySizeInBytes];
            Assert.Equal(secretKey.Length, mldsa.ExportMLDsaSecretKey(secretKey));

            byte[] seed = new byte[testCase.Algorithm.PrivateSeedSizeInBytes];
            Assert.Throws<CryptographicException>(() => mldsa.ExportMLDsaPrivateSeed(seed));

            Assert.Equal(testCase.ShouldPass, mldsa.VerifyData(testCase.Message, testCase.Signature, testCase.Context));
        }

        protected static void ExerciseSuccessfulVerify(MLDsa mldsa, byte[] data, byte[] signature, byte[] context)
        {
            AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, context));

            if (data.Length > 0)
            {
                AssertExtensions.FalseExpression(mldsa.VerifyData([], signature, context));
                AssertExtensions.FalseExpression(mldsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                data[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));
                data[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData([], signature, context));
                AssertExtensions.TrueExpression(mldsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                AssertExtensions.FalseExpression(mldsa.VerifyData([0], signature, context));
                AssertExtensions.FalseExpression(mldsa.VerifyData([1, 2, 3], signature, context));
            }

            signature[0] ^= 1;
            AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));
            signature[0] ^= 1;

            if (context.Length > 0)
            {
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, []));
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                context[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));
                context[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, []));
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, [0]));
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, [1, 2, 3]));
            }

            AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, context));
        }

        protected static void VerifyDisposed(MLDsa mldsa)
        {
            PbeParameters pbeParams = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 10);

            Assert.Throws<ObjectDisposedException>(() => mldsa.SignData([], new byte[mldsa.Algorithm.SignatureSizeInBytes]));
            Assert.Throws<ObjectDisposedException>(() => mldsa.VerifyData([], new byte[mldsa.Algorithm.SignatureSizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaPrivateSeed(new byte[mldsa.Algorithm.PrivateSeedSizeInBytes]));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaPublicKey(new byte[mldsa.Algorithm.PublicKeySizeInBytes]));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaSecretKey(new byte[mldsa.Algorithm.SecretKeySizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => mldsa.TryExportPkcs8PrivateKey(new byte[10000], out _));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportPkcs8PrivateKeyPem());

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportEncryptedPkcs8PrivateKey([1, 2, 3], pbeParams));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportEncryptedPkcs8PrivateKey("123", pbeParams));
            Assert.Throws<ObjectDisposedException>(() => mldsa.TryExportEncryptedPkcs8PrivateKey([1, 2, 3], pbeParams, new byte[10000], out _));
            Assert.Throws<ObjectDisposedException>(() => mldsa.TryExportEncryptedPkcs8PrivateKey("123", pbeParams, new byte[10000], out _));

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportEncryptedPkcs8PrivateKeyPem([1, 2, 3], pbeParams));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportEncryptedPkcs8PrivateKeyPem("123", pbeParams));

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => mldsa.TryExportSubjectPublicKeyInfo(new byte[10000], out _));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportSubjectPublicKeyInfoPem());
        }
    }
}
