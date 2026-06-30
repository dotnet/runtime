// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;
using static System.Security.Cryptography.Tests.CompositeMLDsaTestData;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(CompositeMLDsa), nameof(CompositeMLDsa.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public sealed class CompositeMLDsaCngTests_AllowPlaintextExport : CompositeMLDsaTestsBase
    {
        protected override CompositeMLDsa GenerateKey(CompositeMLDsaAlgorithm algorithm) =>
            CompositeMLDsaTestHelpers.GenerateKey(algorithm, CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);

        protected override CompositeMLDsa ImportPrivateKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLDsaTestHelpers.ImportPrivateKey(algorithm, source, CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);

        protected override CompositeMLDsa ImportPublicKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLDsaTestHelpers.ImportPublicKey(algorithm, source);
    }

    // Windows Insider builds don't support PKCS#8 export so we can't implement encrypted exports
    [ActiveIssue("https://github.com/dotnet/runtime/issues/117000")]
    [ConditionalClass(typeof(CompositeMLDsa), nameof(CompositeMLDsa.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public sealed class CompositeMLDsaCngTests_AllowExport : CompositeMLDsaTestsBase
    {
        protected override CompositeMLDsa GenerateKey(CompositeMLDsaAlgorithm algorithm) =>
            CompositeMLDsaTestHelpers.GenerateKey(algorithm, CngExportPolicies.AllowExport);

        protected override CompositeMLDsa ImportPrivateKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLDsaTestHelpers.ImportPrivateKey(algorithm, source, CngExportPolicies.AllowExport);

        protected override CompositeMLDsa ImportPublicKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLDsaTestHelpers.ImportPublicKey(algorithm, source);
    }

    [ConditionalClass(typeof(CompositeMLDsa), nameof(CompositeMLDsa.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public sealed class CompositeMLDsaCngTests
    {
        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportPrivateKey_NoExportFlag(CompositeMLDsaTestVector info)
        {
            using (CompositeMLDsa dsa = CompositeMLDsaTestHelpers.ImportPrivateKey(info.Algorithm, info.SecretKey, CngExportPolicies.None))
            {
                CompositeMLDsaTestHelpers.AssertExportPublicKey(
                    export => AssertExtensions.SequenceEqual(info.PublicKey, export(dsa)));

                CompositeMLDsaTestHelpers.AssertExportPrivateKey(
                    export => Assert.Throws<CryptographicException>(() => export(dsa)),
                    export => CompositeMLDsaTestHelpers.AssertThrowsCryptographicExceptionWithHResult(() => export(dsa)));

                byte[] signature = new byte[info.Algorithm.MaxSignatureSizeInBytes];
                int written = dsa.SignData("test"u8, signature);
                AssertExtensions.TrueExpression(dsa.VerifyData("test"u8, signature.AsSpan(0, written)));
            }
        }

        [ConditionalFact(typeof(CompositeMLDsa), nameof(CompositeMLDsa.IsSupported))]
        public void ImportPrivateKey_Persisted()
        {
            CompositeMLDsaTestVector testVector = GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256);

            CngKey key = PqcBlobHelpers.EncodeCompositeMLDsaBlob(
                PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, out var parameterSet)
                    ? parameterSet
                    : throw new XunitException("Unsupported algorithm."),
                testVector.SecretKey,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                blob =>
                {
                    CngProperty dsaBlob = new CngProperty(
                        Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                        blob.ToArray(),
                        CngPropertyOptions.None);

                    CngKeyCreationParameters creationParams = new();
                    creationParams.Parameters.Add(dsaBlob);
                    creationParams.ExportPolicy = CngExportPolicies.AllowPlaintextExport;
                    creationParams.KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey;

                    CngKey key = CngKey.Create(CngAlgorithm.CompositeMLDsa, $"CompositeMLDsaCngTests_{nameof(ImportPrivateKey_Persisted)}", creationParams);
                    return key;
                });

            try
            {
                using (CompositeMLDsa dsa = new CompositeMLDsaCng(key))
                {
                    CompositeMLDsaTestHelpers.AssertExportPublicKey(export =>
                        AssertExtensions.SequenceEqual(testVector.PublicKey, export(dsa)));

                    CompositeMLDsaTestHelpers.AssertExportPrivateKey(export =>
                        AssertExtensions.SequenceEqual(testVector.SecretKey, export(dsa)));

                    byte[] signature = new byte[CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256.MaxSignatureSizeInBytes];
                    int written = dsa.SignData("test"u8, signature);
                    AssertExtensions.TrueExpression(dsa.VerifyData("test"u8, signature.AsSpan(0, written)));
                }
            }
            finally
            {
                key.Delete();
            }
        }

        [Fact]
        public void CompositeMLDsaCng_WrongAlgorithm()
        {
            using RSACng rsa = new RSACng();
            using CngKey key = rsa.Key;
            Assert.Throws<ArgumentException>("key", () => new CompositeMLDsaCng(key));
        }

        [Theory]
        [InlineData(default(string))]
        [InlineData($"CompositeMLDsaCngTests_{nameof(CompositeMLDsaCng_DuplicateHandle)}")]
        public void CompositeMLDsaCng_DuplicateHandle(string? name)
        {
            CngProperty parameterSet = CompositeMLDsaTestHelpers.GetCngProperty(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256);
            CngKeyCreationParameters creationParams = new();
            creationParams.Parameters.Add(parameterSet);

            CngKey key = CngKey.Create(CngAlgorithm.CompositeMLDsa, name, creationParams);

            try
            {
                IEnumerable<CompositeMLDsaCng> generateFive = Enumerable.Range(0, 5).Select(_ => new CompositeMLDsaCng(key));
                List<CompositeMLDsaCng> disposables = new List<CompositeMLDsaCng>(10);
                disposables.AddRange(generateFive);
                CompositeMLDsaCng dsa = new CompositeMLDsaCng(key);
                disposables.AddRange(generateFive);

                foreach (CompositeMLDsaCng disposable in disposables)
                {
                    disposable.Dispose();
                }

                byte[] signature = new byte[CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256.MaxSignatureSizeInBytes];
                int written = dsa.SignData("test"u8, signature);
                AssertExtensions.TrueExpression(dsa.VerifyData("test"u8, signature.AsSpan(0, written)));
            }
            finally
            {
                key.Delete();
            }
        }

        [Fact]
        public static void CompositeMLDsaCng_GetKey()
        {
            CngProperty parameterSet = CompositeMLDsaTestHelpers.GetCngProperty(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256);
            CngKeyCreationParameters creationParams = new();
            creationParams.Parameters.Add(parameterSet);

            using (CngKey key = CngKey.Create(CngAlgorithm.CompositeMLDsa, keyName: null, creationParams))
            using (CompositeMLDsaCng dsaKey = new(key))
            using (CngKey getKey1 = dsaKey.GetKey())
            {
                using (CngKey getKey2 = dsaKey.GetKey())
                {
                    Assert.NotSame(key, getKey1);
                    Assert.NotSame(getKey1, getKey2);
                }

                Assert.Equal(key.Algorithm, getKey1.Algorithm); // Assert.NoThrow on getKey1.Algorithm
            }
        }
    }
}
