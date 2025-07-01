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

            // Tests ImportSecretKey, ImportPKCS8PrivateKey, ImportPem (with PRIVATE KEY)
            SlhDsaTestHelpers.AssertImportSecretKey(
                AssertSlhDsaIsOnlyPublicAncestor, info.Algorithm, info.SecretKey);

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
                    Assert.Throws<CryptographicException>(() => roundTrippedSlhDsa.ExportSlhDsaSecretKey());
                }, algorithm, exportedPublicKey);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_SecretKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);

            SlhDsaTestHelpers.AssertExportSlhDsaSecretKey(export =>
            {
                // Roundtrip using secret key. First export it.
                byte[] exportedSecretKey = export(slhDsa);
                SlhDsaTestHelpers.AssertImportSecretKey(import =>
                {
                    // Then import it.
                    using SlhDsa roundTrippedSlhDsa = import();

                    // Verify the roundtripped object has the same key
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(slhDsa.ExportSlhDsaSecretKey(), roundTrippedSlhDsa.ExportSlhDsaSecretKey());
                    AssertExtensions.SequenceEqual(slhDsa.ExportSlhDsaPublicKey(), roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                }, algorithm, exportedSecretKey);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_Pkcs8PrivateKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] secretKey = slhDsa.ExportSlhDsaSecretKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            SlhDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using PKCS#8
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                    AssertExtensions.SequenceEqual(secretKey, roundTrippedSlhDsa.ExportSlhDsaSecretKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_SPKI(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();
            byte[] secretKey = slhDsa.ExportSlhDsaSecretKey();

            SlhDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using SPKI
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                    AssertExtensions.SequenceEqual(secretKey, roundTrippedSlhDsa.ExportSlhDsaSecretKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_EncryptedPkcs8PrivateKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] secretKey = slhDsa.ExportSlhDsaSecretKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 1);

            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
                SlhDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using encrypted PKCS#8
                    using SlhDsa roundTrippedSlhDsa = import("PLACEHOLDER", export(slhDsa, "PLACEHOLDER", pbeParameters));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(secretKey, roundTrippedSlhDsa.ExportSlhDsaSecretKey());
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_Pkcs8PrivateKeyPem(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] secretKey = slhDsa.ExportSlhDsaSecretKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            SlhDsaTestHelpers.AssertExportToPrivateKeyPem(export =>
                SlhDsaTestHelpers.AssertImportFromPem(import =>
                {
                    // Roundtrip it using PEM
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(secretKey, roundTrippedSlhDsa.ExportSlhDsaSecretKey());
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_SPKIPem(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] secretKey = slhDsa.ExportSlhDsaSecretKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            SlhDsaTestHelpers.AssertExportToPublicKeyPem(export =>
                SlhDsaTestHelpers.AssertImportFromPem(import =>
                {
                    // Roundtrip it using PEM
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa));

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedSlhDsa.ExportSlhDsaPublicKey());
                    Assert.Throws<CryptographicException>(() => roundTrippedSlhDsa.ExportSlhDsaSecretKey());
                }));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void RoundTrip_Export_Import_EncryptedPkcs8PrivateKeyPem(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] secretKey = slhDsa.ExportSlhDsaSecretKey();
            byte[] publicKey = slhDsa.ExportSlhDsaPublicKey();

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 1);

            SlhDsaTestHelpers.AssertExportToEncryptedPem(export =>
                SlhDsaTestHelpers.AssertImportFromEncryptedPem(import =>
                {
                    // Roundtrip it using encrypted PKCS#8
                    using SlhDsa roundTrippedSlhDsa = import(export(slhDsa, "PLACEHOLDER", pbeParameters), "PLACEHOLDER");

                    // The keys should be the same
                    Assert.Equal(algorithm, roundTrippedSlhDsa.Algorithm);
                    AssertExtensions.SequenceEqual(secretKey, roundTrippedSlhDsa.ExportSlhDsaSecretKey());
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
            SlhDsaTestHelpers.AssertImportSecretKey(import =>
                SlhDsaTestHelpers.AssertExportSlhDsaSecretKey(export =>
                    SlhDsaTestHelpers.WithDispose(import(), slhDsa =>
                        AssertExtensions.SequenceEqual(info.SecretKey, export(slhDsa)))),
                info.Algorithm,
                info.SecretKey);
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

            byte[] secretKey = slhDsa.ExportSlhDsaSecretKey();
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, secretKey);
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

            byte[] secretKey = slhDsa.ExportSlhDsaSecretKey();
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, secretKey);
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

            byte[] sk = vector.SecretKey;
            byte[] pk = vector.PublicKey;

            // Sanity test for input vectors: SLH-DSA keys are composed of skSeed, skPrf and pkSeed
            AssertExtensions.SequenceEqual(skSeed.AsSpan(), sk.AsSpan(0, skSeed.Length));
            AssertExtensions.SequenceEqual(skPrf.AsSpan(), sk.AsSpan(skSeed.Length, skPrf.Length));
            AssertExtensions.SequenceEqual(pkSeed.AsSpan(), sk.AsSpan(skSeed.Length + skPrf.Length, pkSeed.Length));
            AssertExtensions.SequenceEqual(pkSeed.AsSpan(), pk.AsSpan(0, pkSeed.Length));

            // Import secret key and verify exports
            using (SlhDsa secretSlhDsa = ImportSlhDsaSecretKey(vector.Algorithm, sk))
            {
                byte[] pubKey = secretSlhDsa.ExportSlhDsaPublicKey();
                AssertExtensions.SequenceEqual(pk, pubKey);

                byte[] secretKey = secretSlhDsa.ExportSlhDsaSecretKey();
                AssertExtensions.SequenceEqual(sk, secretKey);
            }

            // Import public key and verify exports
            using (SlhDsa publicSlhDsa = ImportSlhDsaPublicKey(vector.Algorithm, pk))
            {
                byte[] pubKey = publicSlhDsa.ExportSlhDsaPublicKey();
                AssertExtensions.SequenceEqual(pk, pubKey);

                byte[] secretKey = new byte[vector.Algorithm.SecretKeySizeInBytes];
                Assert.Throws<CryptographicException>(() => publicSlhDsa.ExportSlhDsaSecretKey(secretKey));
            }
        }

        #endregion NIST test vectors

        [Fact]
        public static void ImportPkcs8_BerEncoding()
        {
            // Secret key is DER encoded, so create a BER encoding from it by making it use indefinite length encoding.
            byte[] secretKeyPkcs8 = SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8;

            // Two 0x00 bytes at the end signal the end of the indefinite length encoding
            byte[] indefiniteLengthOctet = new byte[secretKeyPkcs8.Length + 2];
            secretKeyPkcs8.CopyTo(indefiniteLengthOctet);
            indefiniteLengthOctet[1] = 0b1000_0000; // change length to indefinite

            SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                SlhDsaTestHelpers.AssertExportSlhDsaSecretKey(export =>
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
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
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
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
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
        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaSecretKey(algorithm, source);
    }
}
