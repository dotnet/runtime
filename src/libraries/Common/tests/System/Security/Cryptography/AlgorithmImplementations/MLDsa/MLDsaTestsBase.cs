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
            byte[] signature = mldsa.SignData(data);
            ExerciseSuccessfulVerify(mldsa, data, signature, []);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyWithContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] context = [ 1, 1, 3, 5, 6 ];
            byte[] data = [ 1, 2, 3, 4, 5 ];
            byte[] signature = mldsa.SignData(data, context);

            ExerciseSuccessfulVerify(mldsa, data, signature, context);
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.SigningEmptyDataIsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/116461", TestPlatforms.Windows)]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyEmptyMessageNoContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] signature = mldsa.SignData([]);
            ExerciseSuccessfulVerify(mldsa, [], signature, []);
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.SigningEmptyDataIsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/116461", TestPlatforms.Windows)]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyEmptyMessageWithContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] signature = mldsa.SignData([], context);
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
                signature = mldsa.SignData(data);
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                publicKey = mldsa.ExportMLDsaPublicKey();
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
                signature = mldsaTmp.SignData(data);
                secretKey = mldsaTmp.ExportMLDsaSecretKey();
            }

            using (MLDsa mldsa = ImportSecretKey(algorithm, secretKey))
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                Span<byte> signatureSpan = signature.AsSpan();
                signatureSpan.Fill(0);
                mldsa.SignData(data, signatureSpan);

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
                signature = mldsaTmp.SignData(data);
                privateSeed = mldsaTmp.ExportMLDsaPrivateSeed();
            }

            using (MLDsa mldsa = ImportPrivateSeed(algorithm, privateSeed))
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                Span<byte> signatureSpan = signature.AsSpan();
                signatureSpan.Fill(0);
                mldsa.SignData(data, signatureSpan);

                ExerciseSuccessfulVerify(mldsa, data, signature, []);
            }
        }

        [Fact]
        public void ImportSecretKey_CannotReconstructSeed()
        {
            byte[] secretKey;
            using (MLDsa mldsaOriginal = GenerateKey(MLDsaAlgorithm.MLDsa44))
            {
                secretKey = mldsaOriginal.ExportMLDsaSecretKey();
            }

            using (MLDsa mldsa = ImportSecretKey(MLDsaAlgorithm.MLDsa44, secretKey))
            {
                Assert.Throws<CryptographicException>(() => mldsa.ExportMLDsaPrivateSeed(new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes]));
            }
        }

        [Fact]
        public void ImportSeed_CanReconstructSecretKey()
        {
            byte[] secretKey;
            byte[] seed;
            using (MLDsa mldsaOriginal = GenerateKey(MLDsaAlgorithm.MLDsa44))
            {
                secretKey = mldsaOriginal.ExportMLDsaSecretKey();
                seed = mldsaOriginal.ExportMLDsaPrivateSeed();
            }

            using (MLDsa mldsa = ImportPrivateSeed(MLDsaAlgorithm.MLDsa44, seed))
            {
                byte[] secretKey2 = mldsa.ExportMLDsaSecretKey();
                byte[] seed2 = mldsa.ExportMLDsaPrivateSeed();

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

            byte[] pubKey = mldsa.ExportMLDsaPublicKey();
            AssertExtensions.SequenceEqual(testCase.PublicKey, pubKey);

            byte[] secretKey = mldsa.ExportMLDsaSecretKey();
            AssertExtensions.SequenceEqual(testCase.SecretKey, secretKey);
            Assert.Throws<CryptographicException>(() => mldsa.ExportMLDsaPrivateSeed());

            Assert.Equal(testCase.ShouldPass, mldsa.VerifyData(testCase.Message, testCase.Signature, testCase.Context));
        }

        protected virtual void AssertExportPkcs8FromPublicKey(Action export) =>
            Assert.Throws<CryptographicException>(export);

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void ImportPublicKey_Export(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = ImportPublicKey(info.Algorithm, info.PublicKey);

            MLDsaTestHelpers.AssertExportMLDsaPublicKey(
                export => AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaSecretKey(
                export => Assert.Throws<CryptographicException>(() => export(mldsa)),
                export => AssertExportPkcs8FromPublicKey(() => export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(
                export => Assert.Throws<CryptographicException>(() => export(mldsa)),
                export => AssertExportPkcs8FromPublicKey(() => export(mldsa)));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void ImportPrivateKey_Export(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = ImportSecretKey(info.Algorithm, info.SecretKey);

            MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaSecretKey(export =>
                AssertExtensions.SequenceEqual(info.SecretKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(
                export => Assert.Throws<CryptographicException>(() => export(mldsa)),
                // Seed is is not available in PKCS#8
                export => Assert.Null(export(mldsa)));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void ImportPrivateSeed_Export(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = ImportPrivateSeed(info.Algorithm, info.PrivateSeed);

            MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaSecretKey(
                export => AssertExtensions.SequenceEqual(info.SecretKey, export(mldsa)),
                // Seed is preferred in PKCS#8, so secret key won't be available
                export => Assert.Null(export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(export =>
                AssertExtensions.SequenceEqual(info.PrivateSeed, export(mldsa)));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void SignData_PublicKeyOnlyThrows(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = ImportPublicKey(info.Algorithm, info.PublicKey);
            byte[] destination = new byte[info.Algorithm.SignatureSizeInBytes];
            CryptographicException ce =
                Assert.ThrowsAny<CryptographicException>(() => mldsa.SignData("hello"u8, destination));

            Assert.DoesNotContain("unknown", ce.Message, StringComparison.OrdinalIgnoreCase);
        }

        protected static void ExerciseSuccessfulVerify(MLDsa mldsa, byte[] data, byte[] signature, byte[] context)
        {
            ReadOnlySpan<byte> buffer = [0, 1, 2, 3];

            AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, context));

            if (data.Length > 0)
            {
                AssertExtensions.FalseExpression(mldsa.VerifyData(Array.Empty<byte>(), signature, context));
                AssertExtensions.FalseExpression(mldsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                data[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));
                data[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(Array.Empty<byte>(), signature, context));
                AssertExtensions.TrueExpression(mldsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                AssertExtensions.FalseExpression(mldsa.VerifyData(buffer.Slice(0, 1), signature, context));
                AssertExtensions.FalseExpression(mldsa.VerifyData(buffer.Slice(1, 3), signature, context));
            }

            signature[0] ^= 1;
            AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));
            signature[0] ^= 1;

            if (context.Length > 0)
            {
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, Array.Empty<byte>()));
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                context[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));
                context[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, Array.Empty<byte>()));
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, buffer.Slice(0, 1)));
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, buffer.Slice(1, 3)));
            }

            AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, context));
        }
    }
}
