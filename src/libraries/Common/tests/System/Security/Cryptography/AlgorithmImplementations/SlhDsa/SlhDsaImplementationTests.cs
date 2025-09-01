// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public sealed class SlhDsaImplementationTests : SlhDsaTests
    {
        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void SlhDsaIsOnlyPublicAncestor_GenerateKey(SlhDsaAlgorithm algorithm)
        {
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.GenerateKey(algorithm));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public static void SlhDsaIsOnlyPublicAncestor_Import(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            // Tests ImportPublicKey, ImportSPKI, ImportPem (with PUBLIC KEY)
            SlhDsaTestHelpers.AssertImportPublicKey(
                AssertSlhDsaIsOnlyPublicAncestor, info.Algorithm, info.PublicKey);

            // Tests ImportPrivateKey, ImportPKCS8PrivateKey, ImportPem (with PRIVATE KEY)
            SlhDsaTestHelpers.AssertImportPrivateKey(
                AssertSlhDsaIsOnlyPublicAncestor, info.Algorithm, info.PrivateKey);

            // Tests ImportEncryptedPKCS8PrivateKey, ImportEncryptedPem (with ENCRYPTED PRIVATE KEY)
            SlhDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(import =>
                AssertSlhDsaIsOnlyPublicAncestor(() => import(info.EncryptionPassword, info.Pkcs8EncryptedPrivateKey)));
        }

        private static void AssertSlhDsaIsOnlyPublicAncestor(Func<SlhDsa> createKey)
        {
            using SlhDsa key = createKey();
            Type keyType = key.GetType();
            while (keyType != null && keyType != typeof(SlhDsa))
            {
                AssertExtensions.FalseExpression(keyType.IsPublic);
                keyType = keyType.BaseType;
            }

            Assert.Equal(typeof(SlhDsa), keyType);
        }

        #region Roundtrip by exporting then importing

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_PublicKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);

            SlhDsaTestHelpers.AssertExportSlhDsaPublicKey(export =>
            {
                // Roundtrip using public key. First export it.
                byte[] exportedPublicKey = export(slhDsa);
                SlhDsaTestHelpers.AssertImportPublicKey(import =>
                {
                    // Then import it.
                    using SlhDsa roundTrippedSlhDsa = import();

                    // Verify the roundtripped object has the same key
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(slhDsa.ExportSlhDsaPublicKey(), roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                    Assert.Throws<CryptographicException>(() => roundTrippedSlhDsa.ExportSlhDsaPrivateKey());
                }, algorithm, exportedPublicKey);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_PrivateKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);

            SlhDsaTestHelpers.AssertExportSlhDsaPrivateKey(export =>
            {
                // Roundtrip using private key. First export it.
                byte[] exportedPrivateKey = export(slhDsa);
                SlhDsaTestHelpers.AssertImportPrivateKey(import =>
                {
                    // Then import it.
                    using SlhDsa roundTrippedSlhDsa = import();

                    // Verify the roundtripped object has the same key
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(slhDsa.ExportSlhDsaPrivateKey(), roundTrippedSlhDsa.ExportSlhDsaPrivateKey());
                    AssertExtensions.SequenceEqual(slhDsa.ExportSlhDsaPublicKey(), roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                }, algorithm, exportedPrivateKey);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_Pkcs8PrivateKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] privateKey = slhDsa.ExportSlhDsaPrivateKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            SlhDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using PKCS#8
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedSlhDsa.ExportSlhDsaPrivateKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_SPKI(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();
            byte[] privateKey = slhDsa.ExportSlhDsaPrivateKey();

            SlhDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using SPKI
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedSlhDsa.ExportSlhDsaPrivateKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_EncryptedPkcs8PrivateKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] privateKey = slhDsa.ExportSlhDsaPrivateKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 1);

            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
                SlhDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using encrypted PKCS#8
                    using SlhDsa roundTrippedSlhDsa = import("PLACEHOLDER", export(slhDsa, "PLACEHOLDER", pbeParameters));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedSlhDsa.ExportSlhDsaPrivateKey());
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_Pkcs8PrivateKeyPem(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] privateKey = slhDsa.ExportSlhDsaPrivateKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            SlhDsaTestHelpers.AssertExportToPrivateKeyPem(export =>
                SlhDsaTestHelpers.AssertImportFromPem(import =>
                {
                    // Roundtrip it using PEM
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedSlhDsa.ExportSlhDsaPrivateKey());
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_SPKIPem(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] privateKey = slhDsa.ExportSlhDsaPrivateKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            SlhDsaTestHelpers.AssertExportToPublicKeyPem(export =>
                SlhDsaTestHelpers.AssertImportFromPem(import =>
                {
                    // Roundtrip it using PEM
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                    Assert.Throws<CryptographicException>(() => roundTrippedSlhDsa.ExportSlhDsaPrivateKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_EncryptedPkcs8PrivateKeyPem(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] privateKey = slhDsa.ExportSlhDsaPrivateKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 1);

            SlhDsaTestHelpers.AssertExportToEncryptedPem(export =>
                SlhDsaTestHelpers.AssertImportFromEncryptedPem(import =>
                {
                    // Roundtrip it using encrypted PKCS#8
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa, "PLACEHOLDER", pbeParameters), "PLACEHOLDER");

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedSlhDsa.ExportSlhDsaPrivateKey());
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                }));
        }

        #endregion Roundtrip by exporting then importing

        #region Roundtrip by importing then exporting

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Import_Export_PublicKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            SlhDsaTestHelpers.AssertImportPublicKey(import =>
                SlhDsaTestHelpers.AssertExportSlhDsaPublicKey(export =>
                    SlhDsaTestHelpers.WithDispose(import(), slhDsa =>
                        AssertExtensions.SequenceEqual(info.PublicKey, export(slhDsa)))),
                info.Algorithm,
                info.PublicKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Import_Export_PrivateKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            SlhDsaTestHelpers.AssertImportPrivateKey(import =>
                SlhDsaTestHelpers.AssertExportSlhDsaPrivateKey(export =>
                    SlhDsaTestHelpers.WithDispose(import(), slhDsa =>
                        AssertExtensions.SequenceEqual(info.PrivateKey, export(slhDsa)))),
                info.Algorithm,
                info.PrivateKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Import_Export_SPKI(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            SlhDsaTestHelpers.AssertImportSubjectKeyPublicInfo(import =>
                SlhDsaTestHelpers.AssertExportSubjectPublicKeyInfo(export =>
                    SlhDsaTestHelpers.WithDispose(import(info.Pkcs8PublicKey), slhDsa =>
                        AssertExtensions.SequenceEqual(info.Pkcs8PublicKey, export(slhDsa)))));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Import_Export_PublicPem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            SlhDsaTestHelpers.AssertImportFromPem(import =>
                SlhDsaTestHelpers.AssertExportToPublicKeyPem(export =>
                    SlhDsaTestHelpers.WithDispose(import(info.PublicKeyPem), slhDsa =>
                        Assert.Equal(info.PublicKeyPem, export(slhDsa)))));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Import_Export_Pkcs8PrivateKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                SlhDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                    SlhDsaTestHelpers.WithDispose(import(info.Pkcs8PrivateKey), slhDsa =>
                        AssertExtensions.SequenceEqual(info.Pkcs8PrivateKey, export(slhDsa)))));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Import_Export_Pkcs8PublicKeyPem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            SlhDsaTestHelpers.AssertImportFromPem(import =>
                SlhDsaTestHelpers.AssertExportToPrivateKeyPem(export =>
                    SlhDsaTestHelpers.WithDispose(import(info.PrivateKeyPem), slhDsa =>
                        Assert.Equal(info.PrivateKeyPem, export(slhDsa)))));
        }

        #endregion Roundtrip by importing then exporting

        #region IETF samples

        [Fact]
        public static void ImportPkcs8PrivateKeyIetf()
        {
            using SlhDsa slhDsa = SlhDsa.ImportPkcs8PrivateKey(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] privateKey = slhDsa.ExportSlhDsaPrivateKey();
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, privateKey);
        }

        [Fact]
        public static void ImportPkcs8PublicKeyIetf()
        {
            using SlhDsa slhDsa = SlhDsa.ImportSubjectPublicKeyInfo(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyPkcs8);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, publicKey);
        }

        [Fact]
        public static void ImportPemPrivateKeyIetf()
        {
            string pem = PemEncoding.WriteString("PRIVATE KEY", SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8);
            using SlhDsa slhDsa = SlhDsa.ImportFromPem(pem);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] privateKey = slhDsa.ExportSlhDsaPrivateKey();
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, privateKey);
        }

        [Fact]
        public static void ImportPemPublicKeyIetf()
        {
            string pem = PemEncoding.WriteString("PUBLIC KEY", SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyPkcs8);
            using SlhDsa slhDsa = SlhDsa.ImportFromPem(pem);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, publicKey);
        }

        #endregion IETF samples

        #region NIST test vectors
        public static IEnumerable<object[]> NistKeyGenTestVectorsData =>
            from vector in SlhDsaTestData.NistKeyGenTestVectors
            select new object[] { vector };

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(NistKeyGenTestVectorsData))]
        public void NistKeyGenerationTest(SlhDsaTestData.SlhDsaKeyGenTestVector vector)
        {
            byte[] skSeed = vector.SecretKeySeed;
            byte[] skPrf = vector.SecretKeyPrf;
            byte[] pkSeed = vector.PublicKeySeed;

            byte[] sk = vector.PrivateKey;
            byte[] pk = vector.PublicKey;

            // Sanity test for input vectors: SLH-DSA keys are composed of skSeed, skPrf and pkSeed
            AssertExtensions.SequenceEqual(skSeed.AsSpan(), sk.AsSpan(0, skSeed.Length));
            AssertExtensions.SequenceEqual(skPrf.AsSpan(), sk.AsSpan(skSeed.Length, skPrf.Length));
            AssertExtensions.SequenceEqual(pkSeed.AsSpan(), sk.AsSpan(skSeed.Length + skPrf.Length, pkSeed.Length));
            AssertExtensions.SequenceEqual(pkSeed.AsSpan(), pk.AsSpan(0, pkSeed.Length));

            // Import private key and verify exports
            using (SlhDsa privateSlhDsa = ImportSlhDsaPrivateKey(vector.Algorithm, sk))
            {
                byte[] pubKey = privateSlhDsa.ExportSlhDsaPublicKey();
                AssertExtensions.SequenceEqual(pk, pubKey);

                byte[] privateKey = privateSlhDsa.ExportSlhDsaPrivateKey();
                AssertExtensions.SequenceEqual(sk, privateKey);
            }

            // Import public key and verify exports
            using (SlhDsa publicSlhDsa = ImportSlhDsaPublicKey(vector.Algorithm, pk))
            {
                byte[] pubKey = publicSlhDsa.ExportSlhDsaPublicKey();
                AssertExtensions.SequenceEqual(pk, pubKey);

                byte[] privateKey = new byte[vector.Algorithm.PrivateKeySizeInBytes];
                Assert.Throws<CryptographicException>(() => publicSlhDsa.ExportSlhDsaPrivateKey(privateKey));
            }
        }

        #endregion NIST test vectors

        [Fact]
        public static void ImportPkcs8_BerEncoding()
        {
            // Private key is DER encoded, so create a BER encoding from it by making it use indefinite length encoding.
            byte[] privateKeyPkcs8 = SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8;

            // Two 0x00 bytes at the end signal the end of the indefinite length encoding
            byte[] indefiniteLengthOctet = new byte[privateKeyPkcs8.Length + 2];
            privateKeyPkcs8.CopyTo(indefiniteLengthOctet);
            indefiniteLengthOctet[1] = 0b1000_0000; // change length to indefinite

            SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                SlhDsaTestHelpers.AssertExportSlhDsaPrivateKey(export =>
                    SlhDsaTestHelpers.WithDispose(import(indefiniteLengthOctet), slhDsa =>
                        AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, export(slhDsa)))));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void ImportPkcs8WithTrailingData(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);

            byte[] privateKey = slhDsa.ExportPkcs8PrivateKey();
            Array.Resize(ref privateKey, privateKey.Length + 1);
            AssertExtensions.Throws<CryptographicException>(() => SlhDsa.ImportPkcs8PrivateKey(privateKey));

            byte[] publicKey = slhDsa.ExportSubjectPublicKeyInfo();
            Array.Resize(ref publicKey, publicKey.Length + 1);
            AssertExtensions.Throws<CryptographicException>(() => SlhDsa.ImportSubjectPublicKeyInfo(publicKey));

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 42);
            byte[] encryptedPrivateKeyCharPassword = slhDsa.ExportEncryptedPkcs8PrivateKey("PLACEHOLDER", pbeParameters);
            Array.Resize(ref encryptedPrivateKeyCharPassword, encryptedPrivateKeyCharPassword.Length + 1);
            AssertExtensions.Throws<CryptographicException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("PLACEHOLDER", encryptedPrivateKeyCharPassword));

            byte[] encryptedPrivateKeyBytePassword = slhDsa.ExportEncryptedPkcs8PrivateKey("PLACEHOLDER"u8, pbeParameters);
            Array.Resize(ref encryptedPrivateKeyBytePassword, encryptedPrivateKeyBytePassword.Length + 1);
            AssertExtensions.Throws<CryptographicException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("PLACEHOLDER"u8, encryptedPrivateKeyBytePassword));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ExportEncryptedPkcs8PrivateKey_PbeParameters(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaPrivateKey(info.Algorithm, info.PrivateKey);
            SlhDsaTestHelpers.EncryptionPasswordType passwordTypeToTest =
                SlhDsaTestHelpers.GetValidPasswordTypes(info.EncryptionParameters);

            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
            {
                byte[] pkcs8 = export(slhDsa, info.EncryptionPassword, info.EncryptionParameters);

                // Verify that the Asn1 structure matches the provided parameters
                EncryptedPrivateKeyInfoAsn epki = EncryptedPrivateKeyInfoAsn.Decode(pkcs8, AsnEncodingRules.BER);
                AsnUtils.AssertEncryptedPkcs8PrivateKeyContents(epki, info.EncryptionParameters);
            }, passwordTypeToTest);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ExportKey_DestinationTooSmall(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaPrivateKey(info.Algorithm, info.PrivateKey);
            byte[] pkcs8PrivateKey = slhDsa.ExportPkcs8PrivateKey();
            byte[] spki = slhDsa.ExportSubjectPublicKeyInfo();
            byte[] encryptedPkcs8 = slhDsa.ExportEncryptedPkcs8PrivateKey(info.EncryptionPassword, info.EncryptionParameters);
            byte[] largeBuffer = new byte[2 * Math.Max(pkcs8PrivateKey.Length, Math.Max(spki.Length, encryptedPkcs8.Length))];

            int bytesWritten = -1;

            // TryExportPkcs8PrivateKey
            AssertExtensions.FalseExpression(slhDsa.TryExportPkcs8PrivateKey(Span<byte>.Empty, out bytesWritten));               // Empty
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportPkcs8PrivateKey(largeBuffer, out bytesWritten));                     // Too large
            AssertExtensions.Equal(pkcs8PrivateKey.Length, bytesWritten);
            AssertExtensions.FalseExpression(
                slhDsa.TryExportPkcs8PrivateKey(pkcs8PrivateKey.AsSpan(0, pkcs8PrivateKey.Length - 1), out bytesWritten));       // Too small
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportPkcs8PrivateKey(pkcs8PrivateKey, out bytesWritten));                 // Exact size
            AssertExtensions.Equal(pkcs8PrivateKey.Length, bytesWritten);

            // TryExportSubjectPublicKeyInfo
            AssertExtensions.FalseExpression(slhDsa.TryExportSubjectPublicKeyInfo(Span<byte>.Empty, out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportSubjectPublicKeyInfo(largeBuffer, out bytesWritten));
            AssertExtensions.Equal(spki.Length, bytesWritten);
            AssertExtensions.FalseExpression(
                slhDsa.TryExportSubjectPublicKeyInfo(spki.AsSpan(0, spki.Length - 1), out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportSubjectPublicKeyInfo(spki, out bytesWritten));
            AssertExtensions.Equal(spki.Length, bytesWritten);

            // TryExportEncryptedPkcs8PrivateKey (string password)
            AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER", info.EncryptionParameters, Span<byte>.Empty, out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER", info.EncryptionParameters, largeBuffer, out bytesWritten));
            AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
            AssertExtensions.FalseExpression(
                slhDsa.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER", info.EncryptionParameters, encryptedPkcs8.AsSpan(0, encryptedPkcs8.Length - 1), out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER", info.EncryptionParameters, encryptedPkcs8, out bytesWritten));
            AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);

            if (info.EncryptionParameters.EncryptionAlgorithm is not PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
            {
                // TryExportEncryptedPkcs8PrivateKey (byte[] password)
                AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER"u8, info.EncryptionParameters, Span<byte>.Empty, out bytesWritten));
                AssertExtensions.Equal(0, bytesWritten);
                AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER"u8, info.EncryptionParameters, largeBuffer, out bytesWritten));
                AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
                AssertExtensions.FalseExpression(
                    slhDsa.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER"u8, info.EncryptionParameters, encryptedPkcs8.AsSpan(0, encryptedPkcs8.Length - 1), out bytesWritten));
                AssertExtensions.Equal(0, bytesWritten);
                AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER"u8, info.EncryptionParameters, encryptedPkcs8, out bytesWritten));
                AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
            }
        }

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) => SlhDsa.GenerateKey(algorithm);
        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaPublicKey(algorithm, source);
        protected override SlhDsa ImportSlhDsaPrivateKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaPrivateKey(algorithm, source);
    }
}
