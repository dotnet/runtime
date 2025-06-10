// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        // TODO MLDsaCng doesn't have a public DuplicateHandle like OpenSSL since CngKey does that
        // internally with CngKey.Handle. Is there something else we should test for copy/duplication?
    }
}
