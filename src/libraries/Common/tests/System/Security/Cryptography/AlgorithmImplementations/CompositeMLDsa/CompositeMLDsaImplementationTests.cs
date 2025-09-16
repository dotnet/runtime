// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(CompositeMLDsa), nameof(CompositeMLDsa.IsSupported))]
    public sealed class CompositeMLDsaImplementationTests : CompositeMLDsaTestsBase
    {
        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void CompositeMLDsaIsOnlyPublicAncestor_GenerateKey(CompositeMLDsaAlgorithm algorithm)
        {
            AssertCompositeMLDsaIsOnlyPublicAncestor(() => CompositeMLDsa.GenerateKey(algorithm));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void CompositeMLDsaIsOnlyPublicAncestor_Import(CompositeMLDsaTestData.CompositeMLDsaTestVector info)
        {
            CompositeMLDsaTestHelpers.AssertImportPublicKey(
                AssertCompositeMLDsaIsOnlyPublicAncestor, info.Algorithm, info.PublicKey);

            CompositeMLDsaTestHelpers.AssertImportPrivateKey(
                AssertCompositeMLDsaIsOnlyPublicAncestor, info.Algorithm, info.SecretKey);
        }

        private static void AssertCompositeMLDsaIsOnlyPublicAncestor(Func<CompositeMLDsa> createKey)
        {
            using CompositeMLDsa key = createKey();
            Type keyType = key.GetType();
            while (keyType != null && keyType != typeof(CompositeMLDsa))
            {
                AssertExtensions.FalseExpression(keyType.IsPublic);
                keyType = keyType.BaseType;
            }

            Assert.Equal(typeof(CompositeMLDsa), keyType);
        }

        #region Roundtrip by exporting then importing

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Export_Import_PublicKey(CompositeMLDsaAlgorithm algorithm)
        {
            // Generate new key
            using CompositeMLDsa dsa = GenerateKey(algorithm);

            CompositeMLDsaTestHelpers.AssertExportPublicKey(
                export =>
                {
                    // Roundtrip using public key. First export it.
                    byte[] exportedPublicKey = export(dsa);
                    CompositeMLDsaTestHelpers.AssertImportPublicKey(
                        import =>
                        {
                            // Then import it.
                            using CompositeMLDsa roundTrippedDsa = import();

                            // Verify the roundtripped object has the same key
                            Assert.Equal(algorithm, roundTrippedDsa.Algorithm);
                            AssertExtensions.SequenceEqual(dsa.ExportCompositeMLDsaPublicKey(), roundTrippedDsa.ExportCompositeMLDsaPublicKey());
                            Assert.Throws<CryptographicException>(() => roundTrippedDsa.ExportCompositeMLDsaPrivateKey());
                        },
                        algorithm,
                        exportedPublicKey);
                });
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Export_Import_PrivateKey(CompositeMLDsaAlgorithm algorithm)
        {
            // Generate new key
            using CompositeMLDsa dsa = GenerateKey(algorithm);

            CompositeMLDsaTestHelpers.AssertExportPrivateKey(
                export =>
                {
                    // Roundtrip using secret key. First export it.
                    byte[] exportedSecretKey = export(dsa);
                    CompositeMLDsaTestHelpers.AssertImportPrivateKey(
                        import =>
                        {
                            // Then import it.
                            using CompositeMLDsa roundTrippedDsa = import();

                            // Verify the roundtripped object has the same key
                            Assert.Equal(algorithm, roundTrippedDsa.Algorithm);
                            AssertExtensions.SequenceEqual(dsa.ExportCompositeMLDsaPrivateKey(), roundTrippedDsa.ExportCompositeMLDsaPrivateKey());
                            AssertExtensions.SequenceEqual(dsa.ExportCompositeMLDsaPublicKey(), roundTrippedDsa.ExportCompositeMLDsaPublicKey());
                        },
                        algorithm,
                        exportedSecretKey);
                });
        }

        #endregion Roundtrip by exporting then importing

        #region Roundtrip by importing then exporting

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Import_Export_PublicKey(CompositeMLDsaTestData.CompositeMLDsaTestVector info)
        {
            CompositeMLDsaTestHelpers.AssertImportPublicKey(import =>
                CompositeMLDsaTestHelpers.AssertExportPublicKey(export =>
                    CompositeMLDsaTestHelpers.WithDispose(import(), dsa =>
                        CompositeMLDsaTestHelpers.AssertPublicKeyEquals(info.Algorithm, info.PublicKey, export(dsa)))),
                info.Algorithm,
                info.PublicKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Import_Export_PrivateKey(CompositeMLDsaTestData.CompositeMLDsaTestVector info)
        {
            CompositeMLDsaTestHelpers.AssertImportPrivateKey(import =>
                CompositeMLDsaTestHelpers.AssertExportPrivateKey(export =>
                    CompositeMLDsaTestHelpers.WithDispose(import(), dsa =>
                        CompositeMLDsaTestHelpers.AssertPrivateKeyEquals(info.Algorithm, info.SecretKey, export(dsa)))),
                info.Algorithm,
                info.SecretKey);
        }

        #endregion Roundtrip by importing then exporting

        protected override CompositeMLDsa GenerateKey(CompositeMLDsaAlgorithm algorithm) =>
            CompositeMLDsa.GenerateKey(algorithm);

        protected override CompositeMLDsa ImportPublicKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, source);

        protected override CompositeMLDsa ImportPrivateKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, source);
    }
}
