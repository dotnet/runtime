// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Xunit;
using Xunit.Sdk;

using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public sealed class MLKemCngPlaintextExportableTests : MLKemCngTests
    {
        protected override CngExportPolicies ExportPolicies => CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;
    }

    // ML-KEM as of Windows build 27881 does not have PKCS#8 exports, so we cannot implement encrypted exports.
    [ActiveIssue("https://github.com/dotnet/runtime/issues/116304")]
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public sealed class MLKemCngExportableTests : MLKemCngTests
    {
        protected override CngExportPolicies ExportPolicies => CngExportPolicies.AllowExport;
    }

    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static class MLKemCngNonExportableTests
    {
        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void MLKemCng_NonExportable_ExportPrivateSeedThrows(MLKemAlgorithm algorithm)
        {
            using CngKey key = MLKemCngTests.GenerateCngKey(algorithm, CngExportPolicies.None);
            using MLKemCng kem = new MLKemCng(key);
            Assert.Throws<CryptographicException>(() => kem.ExportPrivateSeed());
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void MLKemCng_NonExportable_ExportDecapsulationKeyThrows(MLKemAlgorithm algorithm)
        {
            using CngKey key = MLKemCngTests.GenerateCngKey(algorithm, CngExportPolicies.None);
            using MLKemCng kem = new MLKemCng(key);
            Assert.Throws<CryptographicException>(() => kem.ExportDecapsulationKey());
        }

        [Fact]
        public static void MLKemCng_NonExportable_ExportEncapsulationKeyAlwaysWorks()
        {
            using CngKey key = MLKemCngTests.ImportPrivateSeed(
                MLKemAlgorithm.MLKem512,
                MLKemTestData.MLKem512PrivateSeed,
                CngExportPolicies.None);

            using MLKemCng kem = new MLKemCng(key);
            byte[] exportedKey = kem.ExportEncapsulationKey();
            AssertExtensions.SequenceEqual(MLKemTestData.MLKem512EncapsulationKey, exportedKey);
        }
    }

    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static class MLKemCngContractTests
    {
        [Fact]
        public static void MLKemCng_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", static () => new MLKemCng(null));
        }

        [Fact]
        public static void MLKemCng_Ctor_KeyWrongAlgorithm()
        {
            using CngKey rsaKey = CngKey.Create(CngAlgorithm.Rsa, keyName: null);
            AssertExtensions.Throws<ArgumentException>("key", () => new MLKemCng(rsaKey));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void MLKemCng_GetKey(MLKemAlgorithm algorithm)
        {
            using CngKey key = MLKemCngTests.GenerateCngKey(
                algorithm,
                CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);

            using (MLKemCng mlKemKey = new(key))
            using (CngKey getKey1 = mlKemKey.GetKey())
            {
                using (CngKey getKey2 = mlKemKey.GetKey())
                {
                    Assert.NotSame(key, getKey1);
                    Assert.NotSame(getKey1, getKey2);
                }

                Assert.Equal(key.Algorithm, getKey1.Algorithm); // Assert.NoThrow on getKey1.Algorithm
            }
        }
    }

    public abstract class MLKemCngTests : MLKemBaseTests
    {
        protected abstract CngExportPolicies ExportPolicies { get; }

        public override MLKem GenerateKey(MLKemAlgorithm algorithm)
        {
            using (CngKey key = GenerateCngKey(algorithm, ExportPolicies))
            {
                return new MLKemCng(key);
            }
        }

        public override MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> seed)
        {
            using (CngKey key = ImportPrivateSeed(algorithm, seed, ExportPolicies))
            {
                return new MLKemCng(key);
            }
        }

        public override MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using (CngKey key = ImportMLKemKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC, algorithm, source, ExportPolicies))
            {
                return new MLKemCng(key);
            }
        }

        public override MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using (CngKey key = ImportMLKemKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC, algorithm, source, ExportPolicies))
            {
                return new MLKemCng(key);
            }
        }

        internal static CngKey GenerateCngKey(MLKemAlgorithm algorithm, CngExportPolicies exportPolicies)
        {
            CngProperty parameterSet = GetCngProperty(algorithm);

            CngKeyCreationParameters creationParameters = new();
            creationParameters.Parameters.Add(parameterSet);
            creationParameters.ExportPolicy = exportPolicies;

            return CngKey.Create(CngAlgorithm.MLKem, keyName: null, creationParameters);
        }

        internal static CngKey ImportPrivateSeed(
            MLKemAlgorithm algorithm,
            ReadOnlySpan<byte> seed,
            CngExportPolicies exportPolicies)
        {
            return ImportMLKemKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC, algorithm, seed, exportPolicies);
        }

        private static CngProperty GetCngProperty(MLKemAlgorithm algorithm)
        {
            string cngParameterSet;

            if (algorithm == MLKemAlgorithm.MLKem512)
            {
                cngParameterSet = "512\0";
            }
            else if (algorithm == MLKemAlgorithm.MLKem768)
            {
                cngParameterSet = "768\0";
            }
            else if (algorithm == MLKemAlgorithm.MLKem1024)
            {
                cngParameterSet = "1024\0";
            }
            else
            {
                throw new XunitException($"Unknown MLKemAlgorithm '{algorithm}'.");
            }

            byte[] byteValue = Encoding.Unicode.GetBytes(cngParameterSet);
            return new CngProperty("ParameterSetName", byteValue, CngPropertyOptions.None);
        }

        private static CngKey ImportMLKemKey(
            KeyBlobMagicNumber kind,
            MLKemAlgorithm algorithm,
            ReadOnlySpan<byte> key,
            CngExportPolicies exportPolicies)
        {
            return PqcBlobHelpers.EncodeMLKemBlob(
                kind,
                algorithm,
                key,
                (object)null,
                (_, blobKind, blob) =>
                {
                    if (blobKind == CngKeyBlobFormat.MLKemPublicBlob.Format)
                    {
                        return CngKey.Import(blob.ToArray(), CngKeyBlobFormat.MLKemPublicBlob);
                    }
                    else
                    {
                        CngProperty blobProperty = new CngProperty(
                            blobKind,
                            blob.ToArray(),
                            CngPropertyOptions.None);

                        CngKeyCreationParameters creationParameters = new();
                        creationParameters.ExportPolicy = exportPolicies;
                        creationParameters.Parameters.Add(blobProperty);
                        return CngKey.Create(CngAlgorithm.MLKem, keyName: null, creationParameters);
                    }
                });
        }
    }
}
