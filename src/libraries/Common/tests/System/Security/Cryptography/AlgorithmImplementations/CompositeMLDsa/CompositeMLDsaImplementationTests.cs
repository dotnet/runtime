// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.SLHDsa.Tests;
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

        [Fact]
        public static void UseAfterDispose()
        {
            CompositeMLDsa dsa = CompositeMLDsa.GenerateKey(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384);
            dsa.Dispose();
            dsa.Dispose(); // no throw

            CompositeMLDsaTestHelpers.VerifyDisposed(dsa);
        }

        [Fact]
        public static void ImportPkcs8_BerEncoding()
        {
            // Pkcs8 is DER encoded, so create a BER encoding from it by making it use a non-minimal length encoding.
            byte[] key = CompositeMLDsaTestData.AllIetfVectors[0].Pkcs8;

            byte[] nonMinimalEncoding = new byte[key.Length + 1];
            nonMinimalEncoding[0] = key[0]; // SEQUENCE tag

            // Test data uses long form, so we don't need to handle short form
            Debug.Assert((key[1] & 0b1000_0000) != 0);

            nonMinimalEncoding[1] = (byte)(key[1] + 1); // extra length byte to make it non-minimal
            nonMinimalEncoding[2] = 0; // padding byte

            // Copy the rest of the data
            key.AsSpan(2).CopyTo(nonMinimalEncoding.AsSpan(3));

            CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                CompositeMLDsaTestHelpers.AssertExportPrivateKey(export =>
                    CompositeMLDsaTestHelpers.WithDispose(import(nonMinimalEncoding), mldsa =>
                        AssertExtensions.SequenceEqual(CompositeMLDsaTestData.AllIetfVectors[0].SecretKey, export(mldsa)))));
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

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Export_Import_Pkcs8PrivateKey(CompositeMLDsaAlgorithm algorithm)
        {
            // Generate new key
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] privateKey = dsa.ExportCompositeMLDsaPrivateKey();
            byte[] publicKey = dsa.ExportCompositeMLDsaPublicKey();

            CompositeMLDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using PKCS#8
                    using CompositeMLDsa roundTrippedDsa = import(export(dsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedDsa.ExportCompositeMLDsaPublicKey());
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedDsa.ExportCompositeMLDsaPrivateKey());
                }));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Export_Import_SPKI(CompositeMLDsaAlgorithm algorithm)
        {
            // Generate new key
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] publicKey = dsa.ExportCompositeMLDsaPublicKey();
            byte[] privateKey = dsa.ExportCompositeMLDsaPrivateKey();

            CompositeMLDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using SPKI
                    using CompositeMLDsa roundTrippedDsa = import(export(dsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedDsa.ExportCompositeMLDsaPublicKey());
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedDsa.ExportCompositeMLDsaPrivateKey());
                }));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Export_Import_EncryptedPkcs8PrivateKey(CompositeMLDsaAlgorithm algorithm)
        {
            // Generate new key
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] privateKey = dsa.ExportCompositeMLDsaPrivateKey();
            byte[] publicKey = dsa.ExportCompositeMLDsaPublicKey();

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 1);

            CompositeMLDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
                CompositeMLDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using encrypted PKCS#8
                    using CompositeMLDsa roundTrippedDsa = import("PLACEHOLDER", export(dsa, "PLACEHOLDER", pbeParameters));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedDsa.Algorithm);
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedDsa.ExportCompositeMLDsaPrivateKey());
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedDsa.ExportCompositeMLDsaPublicKey());
                }));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Export_Import_Pkcs8PrivateKeyPem(CompositeMLDsaAlgorithm algorithm)
        {
            // Generate new key
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] privateKey = dsa.ExportCompositeMLDsaPrivateKey();
            byte[] publicKey = dsa.ExportCompositeMLDsaPublicKey();

            CompositeMLDsaTestHelpers.AssertExportToPrivateKeyPem(export =>
                CompositeMLDsaTestHelpers.AssertImportFromPem(import =>
                {
                    // Roundtrip it using PEM
                    using CompositeMLDsa roundTrippedDsa = import(export(dsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedDsa.Algorithm);
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedDsa.ExportCompositeMLDsaPrivateKey());
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedDsa.ExportCompositeMLDsaPublicKey());
                }));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Export_Import_SPKIPem(CompositeMLDsaAlgorithm algorithm)
        {
            // Generate new key
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] privateKey = dsa.ExportCompositeMLDsaPrivateKey();
            byte[] publicKey = dsa.ExportCompositeMLDsaPublicKey();

            CompositeMLDsaTestHelpers.AssertExportToPublicKeyPem(export =>
                CompositeMLDsaTestHelpers.AssertImportFromPem(import =>
                {
                    // Roundtrip it using PEM
                    using CompositeMLDsa roundTrippedDsa = import(export(dsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedDsa.ExportCompositeMLDsaPublicKey());
                    Assert.Throws<CryptographicException>(() => roundTrippedDsa.ExportCompositeMLDsaPrivateKey());
                }));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void RoundTrip_Export_Import_EncryptedPkcs8PrivateKeyPem(CompositeMLDsaAlgorithm algorithm)
        {
            // Generate new key
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] privateKey = dsa.ExportCompositeMLDsaPrivateKey();
            byte[] publicKey = dsa.ExportCompositeMLDsaPublicKey();

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 1);

            CompositeMLDsaTestHelpers.AssertExportToEncryptedPem(export =>
                CompositeMLDsaTestHelpers.AssertImportFromEncryptedPem(import =>
                {
                    // Roundtrip it using encrypted PKCS#8
                    using CompositeMLDsa roundTrippedDsa = import(export(dsa, "PLACEHOLDER", pbeParameters), "PLACEHOLDER");

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedDsa.Algorithm);
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedDsa.ExportCompositeMLDsaPrivateKey());
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedDsa.ExportCompositeMLDsaPublicKey());
                }));
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
