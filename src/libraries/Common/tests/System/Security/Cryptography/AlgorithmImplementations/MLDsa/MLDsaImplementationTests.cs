// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public class MLDsaImplementationTests : MLDsaTestsBase
    {
        protected override MLDsa GenerateKey(MLDsaAlgorithm algorithm) => MLDsa.GenerateKey(algorithm);
        protected override MLDsa ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> seed) => MLDsa.ImportMLDsaPrivateSeed(algorithm, seed);
        protected override MLDsa ImportPrivateKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => MLDsa.ImportMLDsaPrivateKey(algorithm, source);
        protected override MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => MLDsa.ImportMLDsaPublicKey(algorithm, source);

        [Fact]
        public static void GenerateImport_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.GenerateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaPrivateSeed(null, default(ReadOnlySpan<byte>)));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaPublicKey(null, default(ReadOnlySpan<byte>)));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaPrivateKey(null, default(ReadOnlySpan<byte>)));

            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaPrivateSeed(null, (byte[]?)null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaPublicKey(null, (byte[]?)null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLDsa.ImportMLDsaPrivateKey(null, (byte[]?)null));
        }

        [Fact]
        public static void Import_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () => MLDsa.ImportMLDsaPrivateSeed(MLDsaAlgorithm.MLDsa44, (byte[]?)null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => MLDsa.ImportMLDsaPublicKey(MLDsaAlgorithm.MLDsa44, (byte[]?)null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => MLDsa.ImportMLDsaPrivateKey(MLDsaAlgorithm.MLDsa44, (byte[]?)null));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ImportMLDsaPrivateKey_WrongSize(MLDsaAlgorithm algorithm)
        {
            int privateKeySize = algorithm.PrivateKeySizeInBytes;

            // ML-DSA key size is wrong when importing algorithm key. Throw an argument exception.
            Action<Func<MLDsa>> assertDirectImport = import => AssertExtensions.Throws<ArgumentException>("source", import);

            // ML-DSA key size is wrong when importing SPKI/PKCS8/PEM. Throw a cryptographic exception unless platform is not supported.
            // Note: this is the algorithm key size, not the PKCS#8 key size.
            Action<Func<MLDsa>> assertEmbeddedImport = import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import()));

            MLDsaTestHelpers.AssertImportPrivateKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[privateKeySize + 1]);
            MLDsaTestHelpers.AssertImportPrivateKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[privateKeySize - 1]);
            MLDsaTestHelpers.AssertImportPrivateKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ImportMLDsaPrivateSeed_WrongSize(MLDsaAlgorithm algorithm)
        {
            int privateSeedSize = algorithm.PrivateSeedSizeInBytes;

            // ML-DSA key size is wrong when importing algorithm key. Throw an argument exception.
            Action<Func<MLDsa>> assertDirectImport = import => AssertExtensions.Throws<ArgumentException>("source", import);

            // ML-DSA key size is wrong when importing SPKI/PKCS8/PEM. Throw a cryptographic exception unless platform is not supported.
            // Note: this is the algorithm key size, not the PKCS#8 key size.
            Action<Func<MLDsa>> assertEmbeddedImport = import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import()));

            MLDsaTestHelpers.AssertImportPrivateSeed(assertDirectImport, assertEmbeddedImport, algorithm, new byte[privateSeedSize + 1]);
            MLDsaTestHelpers.AssertImportPrivateSeed(assertDirectImport, assertEmbeddedImport, algorithm, new byte[privateSeedSize - 1]);
            MLDsaTestHelpers.AssertImportPrivateSeed(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ImportMLDsaPublicKey_WrongSize(MLDsaAlgorithm algorithm)
        {
            int publicKeySize = algorithm.PublicKeySizeInBytes;

            // ML-DSA key size is wrong when importing algorithm key. Throw an argument exception.
            Action<Func<MLDsa>> assertDirectImport = import => AssertExtensions.Throws<ArgumentException>("source", import);

            // ML-DSA key size is wrong when importing SPKI/PKCS8/PEM. Throw a cryptographic exception unless platform is not supported.
            // Note: this is the algorithm key size, not the PKCS#8 key size.
            Action<Func<MLDsa>> assertEmbeddedImport = import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import()));

            MLDsaTestHelpers.AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[publicKeySize + 1]);
            MLDsaTestHelpers.AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[publicKeySize - 1]);
            MLDsaTestHelpers.AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => MLDsa.ImportSubjectPublicKeyInfo(null));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => MLDsa.ImportPkcs8PrivateKey(null));
        }

        [Fact]
        public static void ImportFromPem_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => MLDsa.ImportFromPem(null));
        }

        [Fact]
        public static void ImportEncrypted_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => MLDsa.ImportEncryptedPkcs8PrivateKey("", null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => MLDsa.ImportFromEncryptedPem(null, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => MLDsa.ImportFromEncryptedPem(null, (string)null));
        }

        [Fact]
        public static void ImportEncrypted_NullPassword()
        {
            AssertExtensions.Throws<ArgumentNullException>("password", () => MLDsa.ImportEncryptedPkcs8PrivateKey(null, null));
            AssertExtensions.Throws<ArgumentNullException>("password", () => MLDsa.ImportFromEncryptedPem("", (string)null));

            AssertExtensions.Throws<ArgumentNullException>("passwordBytes", () => MLDsa.ImportFromEncryptedPem("", (byte[])null));
        }

        [Fact]
        public static void UseAfterDispose()
        {
            MLDsa mldsa = MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa44);
            mldsa.Dispose();
            mldsa.Dispose(); // no throw

            MLDsaTestHelpers.VerifyDisposed(mldsa);
        }

        [Fact]
        public static void ArgumentValidation_MalformedAsnEncoding()
        {
            // Generate a valid ASN.1 encoding
            byte[] encodedBytes = CreateAsn1EncodedBytes();
            int actualEncodedLength = encodedBytes.Length;

            // Add a trailing byte so the length indicated in the encoding will be smaller than the actual data.
            Array.Resize(ref encodedBytes, actualEncodedLength + 1);
            AssertThrows(encodedBytes);

            // Remove the last byte so the length indicated in the encoding will be larger than the actual data.
            Array.Resize(ref encodedBytes, actualEncodedLength - 1);
            AssertThrows(encodedBytes);

            static void AssertThrows(byte[] encodedBytes)
            {
                MLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                    import => Assert.Throws<CryptographicException>(() => import(encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(encodedBytes))));

                MLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                    import => Assert.Throws<CryptographicException>(() => import(encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(encodedBytes))));

                MLDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                    import => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encodedBytes))));
            }
        }

        [Fact]
        public static void ImportSpki_BerEncoding()
        {
            // Valid BER but invalid DER - uses indefinite length encoding
            byte[] indefiniteLengthOctet = [0x04, 0x80, 0x01, 0x02, 0x03, 0x04, 0x00, 0x00];
            MLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Throws<CryptographicException>(() => import(indefiniteLengthOctet))));
        }

        [Fact]
        public static void ImportPkcs8_BerEncoding()
        {
            // Seed is DER encoded, so create a BER encoding from it by making it use indefinite length encoding.
            byte[] seedPkcs8 = MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed;

            // Two 0x00 bytes at the end signal the end of the indefinite length encoding
            byte[] indefiniteLengthOctet = new byte[seedPkcs8.Length + 2];
            seedPkcs8.CopyTo(indefiniteLengthOctet);
            indefiniteLengthOctet[1] = 0b1000_0000; // change length to indefinite

            MLDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(export =>
                    WithDispose(import(indefiniteLengthOctet), mldsa =>
                        AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.PrivateSeed, export(mldsa)))));
        }

        [Fact]
        public static void ImportPkcs8_WrongTypeInAsn()
        {
            // Create an incorrect ASN.1 structure to pass into the import methods.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            AlgorithmIdentifierAsn algorithmIdentifier = new AlgorithmIdentifierAsn
            {
                Algorithm = MLDsaTestHelpers.AlgorithmToOid(MLDsaAlgorithm.MLDsa44),
            };
            algorithmIdentifier.Encode(writer);
            byte[] wrongAsnType = writer.Encode();

            MLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(wrongAsnType))));

            MLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(wrongAsnType))));

            MLDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", wrongAsnType))));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_AlgorithmErrorsInAsn()
        {
#if !NETFRAMEWORK // Does not support exporting RSA SPKI
            if (!OperatingSystem.IsBrowser())
            {
                // RSA key
                using RSA rsa = RSA.Create();
                byte[] rsaSpkiBytes = rsa.ExportSubjectPublicKeyInfo();
                MLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(rsaSpkiBytes))));
            }
#endif

            // Create an invalid ML-DSA SPKI with parameters
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = MLDsaTestHelpers.AlgorithmToOid(MLDsaAlgorithm.MLDsa44),
                    Parameters = MLDsaTestHelpers.s_derBitStringFoo, // <-- Invalid
                },
                SubjectPublicKey = new byte[MLDsaAlgorithm.MLDsa44.PublicKeySizeInBytes]
            };

            MLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(spki.Encode()))));

            spki.Algorithm.Parameters = AsnUtils.DerNull;

            MLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(spki.Encode()))));

            // Sanity check
            spki.Algorithm.Parameters = null;
            MLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(import => AssertThrowIfNotSupported(() => import(spki.Encode())));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_AlgorithmErrorsInAsn()
        {
#if !NETFRAMEWORK // Does not support exporting RSA PKCS#8 private key
            if (!OperatingSystem.IsBrowser())
            {
                // RSA key isn't valid for ML-DSA
                using RSA rsa = RSA.Create();
                byte[] rsaPkcs8Bytes = rsa.ExportPkcs8PrivateKey();
                MLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(rsaPkcs8Bytes))));
            }
#endif

            // Create an invalid ML-DSA PKCS8 with parameters
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            MLDsaPrivateKeyAsn seed = new MLDsaPrivateKeyAsn
            {
                Seed = new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes],
            };
            seed.Encode(writer);

            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = MLDsaTestHelpers.AlgorithmToOid(MLDsaAlgorithm.MLDsa44),
                    Parameters = MLDsaTestHelpers.s_derBitStringFoo, // <-- Invalid
                },
                PrivateKey = writer.Encode(),
            };

            MLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode()))));

            pkcs8.PrivateKeyAlgorithm.Parameters = AsnUtils.DerNull;

            MLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode()))));

            // Sanity check
            pkcs8.PrivateKeyAlgorithm.Parameters = null;
            MLDsaTestHelpers.AssertImportPkcs8PrivateKey(import => AssertThrowIfNotSupported(() => import(pkcs8.Encode())));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_KeyErrorsInAsn()
        {
            AssertInvalidAsn(new MLDsaPrivateKeyAsn
            {
                Both = new MLDsaPrivateKeyBothAsn()
            });

            AssertInvalidAsn(new MLDsaPrivateKeyAsn
            {
                Both = new MLDsaPrivateKeyBothAsn
                {
                    Seed = new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes],
                }
            });

            AssertInvalidAsn(new MLDsaPrivateKeyAsn
            {
                Both = new MLDsaPrivateKeyBothAsn
                {
                    ExpandedKey = new byte[MLDsaAlgorithm.MLDsa44.PrivateKeySizeInBytes],
                }
            });

            AssertInvalidAsn(new MLDsaPrivateKeyAsn
            {
                Both = new MLDsaPrivateKeyBothAsn
                {
                    Seed = new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes - 1],
                    ExpandedKey = new byte[MLDsaAlgorithm.MLDsa44.PrivateKeySizeInBytes],
                }
            });

            AssertInvalidAsn(new MLDsaPrivateKeyAsn
            {
                Both = new MLDsaPrivateKeyBothAsn
                {
                    Seed = new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes],
                    ExpandedKey = new byte[MLDsaAlgorithm.MLDsa44.PrivateKeySizeInBytes - 1],
                }
            });

            AssertInvalidAsn(new MLDsaPrivateKeyAsn
            {
                Both = new MLDsaPrivateKeyBothAsn
                {
                    // This will also fail because the seed and expanded key mismatch
                    Seed = new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes],
                    ExpandedKey = new byte[MLDsaAlgorithm.MLDsa44.PrivateKeySizeInBytes],
                }
            });

            static void AssertInvalidAsn(MLDsaPrivateKeyAsn privateKeyAsn)
            {
                PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
                {
                    PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                    {
                        Algorithm = MLDsaTestHelpers.AlgorithmToOid(MLDsaAlgorithm.MLDsa44),
                        Parameters = null,
                    },
                };

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                privateKeyAsn.Encode(writer);
                pkcs8.PrivateKey = writer.Encode();

                MLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode()))));
            }
        }

        [Fact]
        public static void ImportFromPem_MalformedPem()
        {
            AssertThrows(WritePemRaw("UNKNOWN LABEL", []));
            AssertThrows(string.Empty);
            AssertThrows(WritePemRaw("ENCRYPTED PRIVATE KEY", []));
            AssertThrows(WritePemRaw("PUBLIC KEY", []) + '\n' + WritePemRaw("PUBLIC KEY", []));
            AssertThrows(WritePemRaw("PRIVATE KEY", []) + '\n' + WritePemRaw("PUBLIC KEY", []));
            AssertThrows(WritePemRaw("PUBLIC KEY", []) + '\n' + WritePemRaw("PRIVATE KEY", []));
            AssertThrows(WritePemRaw("PRIVATE KEY", []) + '\n' + WritePemRaw("PRIVATE KEY", []));
            AssertThrows(WritePemRaw("PRIVATE KEY", "%"));
            AssertThrows(WritePemRaw("PUBLIC KEY", "%"));

            static void AssertThrows(string pem)
            {
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportFromPem(pem)));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportFromPem(pem.AsSpan())));
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_MalformedPem()
        {
            AssertThrows(WritePemRaw("UNKNOWN LABEL", []));
            AssertThrows(WritePemRaw("CERTIFICATE", []));
            AssertThrows(string.Empty);
            AssertThrows(WritePemRaw("ENCRYPTED PRIVATE KEY", []) + '\n' + WritePemRaw("ENCRYPTED PRIVATE KEY", []));
            AssertThrows(WritePemRaw("ENCRYPTED PRIVATE KEY", "%"));

            static void AssertThrows(string encryptedPem)
            {
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER")));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"u8)));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportFromEncryptedPem(encryptedPem.AsSpan(), "PLACEHOLDER")));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => MLDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"u8.ToArray())));
            }
        }

        [ConditionalTheory(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void AlgorithmMatches_GenerateKey(MLDsaAlgorithm algorithm)
        {
            byte[] publicKey = new byte[algorithm.PublicKeySizeInBytes];
            byte[] privateKey = new byte[algorithm.PrivateKeySizeInBytes];
            byte[] privateSeed = new byte[algorithm.PrivateSeedSizeInBytes];
            AssertThrowIfNotSupported(() =>
            {
                using MLDsa mldsa = MLDsa.GenerateKey(algorithm);
                mldsa.ExportMLDsaPublicKey(publicKey);
                mldsa.ExportMLDsaPrivateKey(privateKey);
                mldsa.ExportMLDsaPrivateSeed(privateSeed);
                Assert.Equal(algorithm, mldsa.Algorithm);
            });

            MLDsaTestHelpers.AssertImportPublicKey(import =>
                AssertThrowIfNotSupported(() =>
                    WithDispose(import(), mldsa => 
                        Assert.Equal(algorithm, mldsa.Algorithm))), algorithm, publicKey);

            MLDsaTestHelpers.AssertImportPrivateKey(import =>
                AssertThrowIfNotSupported(() =>
                    WithDispose(import(), mldsa => 
                        Assert.Equal(algorithm, mldsa.Algorithm))), algorithm, privateKey);

            MLDsaTestHelpers.AssertImportPrivateSeed(import =>
                AssertThrowIfNotSupported(() =>
                    WithDispose(import(), mldsa => Assert.Equal(algorithm, mldsa.Algorithm))), algorithm, privateSeed);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void RoundTrip_Import_Export_PublicKey(MLDsaKeyInfo info)
        {
            MLDsaTestHelpers.AssertImportPublicKey(import =>
                MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                    WithDispose(import(), mldsa =>
                        AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)))),
                info.Algorithm,
                info.PublicKey);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void RoundTrip_Import_Export_PrivateKey(MLDsaKeyInfo info)
        {
            MLDsaTestHelpers.AssertImportPrivateKey(import =>
                MLDsaTestHelpers.AssertExportMLDsaPrivateKey(export =>
                    WithDispose(import(), mldsa =>
                        AssertExtensions.SequenceEqual(info.PrivateKey, export(mldsa)))),
                info.Algorithm,
                info.PrivateKey);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void RoundTrip_Import_Export_PrivateSeed(MLDsaKeyInfo info)
        {
            MLDsaTestHelpers.AssertImportPrivateSeed(import =>
                MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(export =>
                    WithDispose(import(), mldsa =>
                        AssertExtensions.SequenceEqual(info.PrivateSeed, export(mldsa)))),
                info.Algorithm,
                info.PrivateSeed);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void RoundTrip_Import_Export_SPKI(MLDsaKeyInfo info)
        {
            MLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(import =>
                MLDsaTestHelpers.AssertExportSubjectPublicKeyInfo(export =>
                    WithDispose(import(info.Pkcs8PublicKey), mldsa =>
                        AssertExtensions.SequenceEqual(info.Pkcs8PublicKey, export(mldsa)))));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void RoundTrip_Import_Export_PublicPem(MLDsaKeyInfo info)
        {
            MLDsaTestHelpers.AssertImportFromPem(import =>
                MLDsaTestHelpers.AssertExportToPublicKeyPem(export =>
                    WithDispose(import(info.PublicKeyPem), mldsa =>
                        Assert.Equal(info.PublicKeyPem, export(mldsa)))));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void RoundTrip_Import_Export_Pkcs8PrivateKey(MLDsaKeyInfo info)
        {
            MLDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                MLDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                    WithDispose(import(info.Pkcs8PrivateKey_Seed), mldsa =>
                        AssertExtensions.SequenceEqual(info.Pkcs8PrivateKey_Seed, export(mldsa)))));

            MLDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                MLDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                    WithDispose(import(info.Pkcs8PrivateKey_Expanded), mldsa =>
                        AssertExtensions.SequenceEqual(info.Pkcs8PrivateKey_Expanded, export(mldsa)))));

            MLDsaTestHelpers.AssertImportPkcs8PrivateKey(import =>
                MLDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
                    WithDispose(import(info.Pkcs8PrivateKey_Both), mldsa =>
                        // We will only export seed instead of both since either is valid.
                        AssertExtensions.SequenceEqual(info.Pkcs8PrivateKey_Seed, export(mldsa)))));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void RoundTrip_Import_Export_Pkcs8PrivateKeyPem(MLDsaKeyInfo info)
        {
            MLDsaTestHelpers.AssertImportFromPem(import =>
                MLDsaTestHelpers.AssertExportToPrivateKeyPem(export =>
                    WithDispose(import(info.PrivateKeyPem_Seed), mldsa =>
                        Assert.Equal(info.PrivateKeyPem_Seed, export(mldsa)))));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void RoundTrip_EncryptedPrivateKey(MLDsaKeyInfo info)
        {
            // Load key
            using MLDsa mldsa = MLDsa.ImportEncryptedPkcs8PrivateKey(info.EncryptionPassword, info.Pkcs8EncryptedPrivateKey_Seed);

            byte[] privateKey = new byte[mldsa.Algorithm.PrivateKeySizeInBytes];
            mldsa.ExportMLDsaPrivateKey(privateKey);
            AssertExtensions.SequenceEqual(info.PrivateKey, privateKey);

            byte[] privateSeed = new byte[mldsa.Algorithm.PrivateSeedSizeInBytes];
            mldsa.ExportMLDsaPrivateSeed(privateSeed);
            AssertExtensions.SequenceEqual(info.PrivateSeed, privateSeed);

            byte[] publicKey = new byte[mldsa.Algorithm.PublicKeySizeInBytes];
            mldsa.ExportMLDsaPublicKey(publicKey);
            AssertExtensions.SequenceEqual(info.PublicKey, publicKey);

            MLDsaTestHelpers.EncryptionPasswordType validPasswordTypes = MLDsaTestHelpers.GetValidPasswordTypes(info.EncryptionParameters);

            MLDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
                MLDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(import =>
                {
                    // Roundtrip it using encrypted PKCS#8
                    using MLDsa roundTrippedMLDsa = import(info.EncryptionPassword, export(mldsa, info.EncryptionPassword, info.EncryptionParameters));

                    // The keys should be the same
                    Assert.Equal(info.Algorithm, roundTrippedMLDsa.Algorithm);

                    byte[] roundTrippedPrivateKey = new byte[roundTrippedMLDsa.Algorithm.PrivateKeySizeInBytes];
                    roundTrippedMLDsa.ExportMLDsaPrivateKey(roundTrippedPrivateKey);
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedPrivateKey);

                    byte[] roundTrippedPrivateSeed = new byte[roundTrippedMLDsa.Algorithm.PrivateSeedSizeInBytes];
                    roundTrippedMLDsa.ExportMLDsaPrivateSeed(roundTrippedPrivateSeed);
                    AssertExtensions.SequenceEqual(privateSeed, roundTrippedPrivateSeed);

                    byte[] roundTrippedPublicKey = new byte[roundTrippedMLDsa.Algorithm.PublicKeySizeInBytes];
                    roundTrippedMLDsa.ExportMLDsaPublicKey(roundTrippedPublicKey);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedPublicKey);
                }, validPasswordTypes), validPasswordTypes);

            MLDsaTestHelpers.AssertExportToEncryptedPem(export =>
                MLDsaTestHelpers.AssertImportFromEncryptedPem(import =>
                {
                    // Roundtrip it using encrypted PEM
                    using MLDsa roundTrippedMLDsa = import(export(mldsa, info.EncryptionPassword, info.EncryptionParameters), info.EncryptionPassword);

                    // The keys should be the same
                    Assert.Equal(info.Algorithm, roundTrippedMLDsa.Algorithm);

                    byte[] roundTrippedPrivateKey = new byte[roundTrippedMLDsa.Algorithm.PrivateKeySizeInBytes];
                    roundTrippedMLDsa.ExportMLDsaPrivateKey(roundTrippedPrivateKey);
                    AssertExtensions.SequenceEqual(privateKey, roundTrippedPrivateKey);

                    byte[] roundTrippedPrivateSeed = new byte[roundTrippedMLDsa.Algorithm.PrivateSeedSizeInBytes];
                    roundTrippedMLDsa.ExportMLDsaPrivateSeed(roundTrippedPrivateSeed);
                    AssertExtensions.SequenceEqual(privateSeed, roundTrippedPrivateSeed);

                    byte[] roundTrippedPublicKey = new byte[roundTrippedMLDsa.Algorithm.PublicKeySizeInBytes];
                    roundTrippedMLDsa.ExportMLDsaPublicKey(roundTrippedPublicKey);
                    AssertExtensions.SequenceEqual(publicKey, roundTrippedPublicKey);
                }, validPasswordTypes), validPasswordTypes);
        }

        /// <summary>
        /// Asserts that on platforms that do not support ML-DSA, the input test throws PlatformNotSupportedException.
        /// If the test does pass, it implies that the test is validating code after the platform check.
        /// </summary>
        /// <param name="test">The test to run.</param>
        private static void AssertThrowIfNotSupported(Action test)
        {
            if (MLDsa.IsSupported)
            {
                test();
            }
            else
            {
                try
                {
                    test();
                }
                catch (PlatformNotSupportedException pnse)
                {
                    Assert.Contains("MLDsa", pnse.Message);
                }
                catch (ThrowsException te) when (te.InnerException is PlatformNotSupportedException pnse)
                {
                    Assert.Contains("MLDsa", pnse.Message);
                }
            }
        }

        private static void WithDispose<T>(T disposable, Action<T> callback)
            where T : IDisposable
        {
            using (disposable)
            {
                callback(disposable);
            }
        }

        private static string WritePemRaw(string label, ReadOnlySpan<char> data) =>
            $"-----BEGIN {label}-----\n{data.ToString()}\n-----END {label}-----";

        private static byte[] CreateAsn1EncodedBytes()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteOctetString("some data"u8);
            byte[] encodedBytes = writer.Encode();
            return encodedBytes;
        }
    }
}
