// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public sealed class MLDsaCngTests_AllowPlaintextExport : MLDsaTestsBase
    {
        protected override MLDsaCng GenerateKey(MLDsaAlgorithm algorithm) =>
            MLDsaTestHelpers.GenerateKey(algorithm, CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);

        protected override MLDsaCng ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            MLDsaTestHelpers.ImportPrivateSeed(algorithm, source, CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);

        protected override MLDsaCng ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            MLDsaTestHelpers.ImportSecretKey(algorithm, source, CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);

        protected override MLDsaCng ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            MLDsaTestHelpers.ImportPublicKey(algorithm, source);
    }

    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public sealed class MLDsaCngTests_AllowExport : MLDsaTestsBase
    {
        protected override MLDsaCng GenerateKey(MLDsaAlgorithm algorithm) =>
            MLDsaTestHelpers.GenerateKey(algorithm, CngExportPolicies.AllowExport);

        protected override MLDsaCng ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            MLDsaTestHelpers.ImportPrivateSeed(algorithm, source, CngExportPolicies.AllowExport);

        protected override MLDsaCng ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            MLDsaTestHelpers.ImportSecretKey(algorithm, source, CngExportPolicies.AllowExport);

        protected override MLDsaCng ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            MLDsaTestHelpers.ImportPublicKey(algorithm, source);
    }

    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public sealed class MLDsaCngTests
    {
        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void ImportPrivateKey_NoExportFlag(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = MLDsaTestHelpers.ImportSecretKey(info.Algorithm, info.SecretKey, CngExportPolicies.None);

            MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaSecretKey(export =>
                AssertExtensions.ThrowsContains<CryptographicException>(() => export(mldsa), "The requested operation is not supported"));

            MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(export =>
                AssertExtensions.ThrowsContains<CryptographicException>(() => export(mldsa), "The requested operation is not supported"));

            byte[] signature = new byte[info.Algorithm.SignatureSizeInBytes];
            mldsa.SignData("test"u8, signature);
            AssertExtensions.TrueExpression(mldsa.VerifyData("test"u8, signature));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void ImportPrivateSeed_NoExportFlag(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = MLDsaTestHelpers.ImportPrivateSeed(info.Algorithm, info.PrivateSeed, CngExportPolicies.None);

            MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaSecretKey(export =>
                AssertExtensions.ThrowsContains<CryptographicException>(() => export(mldsa), "The requested operation is not supported"));

            MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(export =>
                AssertExtensions.ThrowsContains<CryptographicException>(() => export(mldsa), "The requested operation is not supported"));

            byte[] signature = new byte[info.Algorithm.SignatureSizeInBytes];
            mldsa.SignData("test"u8, signature);
            AssertExtensions.TrueExpression(mldsa.VerifyData("test"u8, signature));
        }

        [Fact]
        public void ImportPrivateSeed_Persisted()
        {
            CngKey key = PqcBlobHelpers.EncodeMLDsaBlob(
                PqcBlobHelpers.GetMLDsaParameterSet(MLDsaAlgorithm.MLDsa44),
                MLDsaTestsData.IetfMLDsa44.PrivateSeed,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB,
                blob =>
                {
                    CngProperty mldsaBlob = new CngProperty(
                        Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB,
                        blob.ToArray(),
                        CngPropertyOptions.None);

                    CngKeyCreationParameters creationParams = new();
                    creationParams.Parameters.Add(mldsaBlob);
                    creationParams.ExportPolicy = CngExportPolicies.AllowPlaintextExport;
                    creationParams.KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey;

                    CngKey key = CngKey.Create(CngAlgorithm.MLDsa, $"MLDsaCngTests_{nameof(ImportPrivateSeed_Persisted)}", creationParams);
                    return key;
                });

            try
            {
                using (MLDsa mldsa = new MLDsaCng(key))
                {
                    MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                        AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.PublicKey, export(mldsa)));

                    MLDsaTestHelpers.AssertExportMLDsaSecretKey(
                        export => AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.SecretKey, export(mldsa)),
                        // Seed is preferred in PKCS#8, so secret key won't be available
                        export => Assert.Null(export(mldsa)));

                    MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(export =>
                        AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.PrivateSeed, export(mldsa)));

                    byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];
                    mldsa.SignData("test"u8, signature);
                    AssertExtensions.TrueExpression(mldsa.VerifyData("test"u8, signature));
                }
            }
            finally
            {
                key.Delete();
            }
        }

        [Fact]
        public void ImportSecretKey_Persisted()
        {
            CngKey key = PqcBlobHelpers.EncodeMLDsaBlob(
                PqcBlobHelpers.GetMLDsaParameterSet(MLDsaAlgorithm.MLDsa44),
                MLDsaTestsData.IetfMLDsa44.SecretKey,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                blob =>
                {
                    CngProperty mldsaBlob = new CngProperty(
                        Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                        blob.ToArray(),
                        CngPropertyOptions.None);

                    CngKeyCreationParameters creationParams = new();
                    creationParams.Parameters.Add(mldsaBlob);
                    creationParams.ExportPolicy = CngExportPolicies.AllowPlaintextExport;
                    creationParams.KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey;

                    CngKey key = CngKey.Create(CngAlgorithm.MLDsa, $"MLDsaCngTests_{nameof(ImportSecretKey_Persisted)}", creationParams);
                    return key;
                });

            try
            {
                using (MLDsa mldsa = new MLDsaCng(key))
                {
                    MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                        AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.PublicKey, export(mldsa)));

                    MLDsaTestHelpers.AssertExportMLDsaSecretKey(export =>
                        AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.SecretKey, export(mldsa)));

                    MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(
                        export => Assert.Throws<CryptographicException>(() => export(mldsa)),
                        // Seed is is not available in PKCS#8
                        export => Assert.Null(export(mldsa)));

                    byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];
                    mldsa.SignData("test"u8, signature);
                    AssertExtensions.TrueExpression(mldsa.VerifyData("test"u8, signature));
                }
            }
            finally
            {
                key.Delete();
            }
        }

        [Fact]
        public void MLDsaCng_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", static () => new MLDsaCng(null));
        }

        [Fact]
        public void MLDsaCng_WrongAlgorithm()
        {
            using RSACng rsa = new RSACng();
            using CngKey key = rsa.Key;
            Assert.Throws<ArgumentException>("key", () => new MLDsaCng(key));
        }

        [Theory]
        [InlineData(default(string))]
        [InlineData($"MLDsaCngTests_{nameof(MLDsaCng_DuplicateHandle)}")]
        public void MLDsaCng_DuplicateHandle(string? name)
        {
            CngProperty parameterSet = MLDsaTestHelpers.GetCngProperty(MLDsaAlgorithm.MLDsa44);
            CngKeyCreationParameters creationParams = new();
            creationParams.Parameters.Add(parameterSet);

            CngKey key = CngKey.Create(CngAlgorithm.MLDsa, name, creationParams);

            try
            {
                IEnumerable<MLDsaCng> generateFive = Enumerable.Range(0, 5).Select(_ => new MLDsaCng(key));
                List<MLDsaCng> disposables = new List<MLDsaCng>(10);
                disposables.AddRange(generateFive);
                MLDsaCng mldsa = new MLDsaCng(key);
                disposables.AddRange(generateFive);

                foreach (MLDsaCng disposable in disposables)
                {
                    disposable.Dispose();
                }

                byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];
                mldsa.SignData("test"u8, signature);
                AssertExtensions.TrueExpression(mldsa.VerifyData("test"u8, signature));
            }
            finally
            {
                key.Delete();
            }
        }
    }
}
