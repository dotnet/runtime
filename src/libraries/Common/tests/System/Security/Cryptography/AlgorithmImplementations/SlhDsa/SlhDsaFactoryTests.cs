// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public static class SlhDsaFactoryTests
    {
        [Fact]
        public static void NullArgumentValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => SlhDsa.GenerateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => SlhDsa.ImportSlhDsaPublicKey(null, []));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => SlhDsa.ImportSlhDsaSecretKey(null, []));

            AssertExtensions.Throws<ArgumentNullException>("source", static () => SlhDsa.ImportEncryptedPkcs8PrivateKey(string.Empty, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => SlhDsa.ImportFromEncryptedPem((string)null, string.Empty));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => SlhDsa.ImportFromEncryptedPem((string)null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => SlhDsa.ImportFromPem(null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => SlhDsa.ImportPkcs8PrivateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => SlhDsa.ImportSlhDsaPublicKey(SlhDsaAlgorithm.SlhDsaSha2_128f, null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => SlhDsa.ImportSlhDsaSecretKey(SlhDsaAlgorithm.SlhDsaSha2_128f, null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => SlhDsa.ImportSubjectPublicKeyInfo(null));

            AssertExtensions.Throws<ArgumentNullException>("password", static () => SlhDsa.ImportEncryptedPkcs8PrivateKey((string)null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("password", static () => SlhDsa.ImportFromEncryptedPem(string.Empty, (string)null));

            AssertExtensions.Throws<ArgumentNullException>("passwordBytes", static () => SlhDsa.ImportFromEncryptedPem(string.Empty, (byte[])null));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ArgumentValidation_WrongKeySizeForAlgorithm(SlhDsaAlgorithm algorithm)
        {
            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;

            // SLH-DSA key size is wrong when importing algorithm key. Throw an argument exception.
            Action<Func<SlhDsa>> assertDirectImport = import => AssertExtensions.Throws<ArgumentException>("source", import);

            // SLH-DSA key size is wrong when importing SPKI/PKCS8/PEM. Throw a cryptographic exception unless platform is not supported.
            // Note: this is the algorithm key size, not the PKCS#8 key size.
            Action<Func<SlhDsa>> assertEmbeddedImport = import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import()));

            SlhDsaTestHelpers.AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[publicKeySize + 1]);
            SlhDsaTestHelpers.AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[publicKeySize - 1]);
            SlhDsaTestHelpers.AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);

            SlhDsaTestHelpers.AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[secretKeySize + 1]);
            SlhDsaTestHelpers.AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[secretKeySize - 1]);
            SlhDsaTestHelpers.AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);
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
                SlhDsaTestHelpers.AssertImportSubjectKeyPublicInfo(
                    import => Assert.Throws<CryptographicException>(() => import(encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(encodedBytes))));

                SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(
                    import => Assert.Throws<CryptographicException>(() => import(encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(encodedBytes))));

                SlhDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                    import => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encodedBytes))));
            }
        }

        [Fact]
        public static void ImportSpki_BerEncoding()
        {
            // Valid BER but invalid DER - uses indefinite length encoding
            byte[] indefiniteLengthOctet = [0x04, 0x80, 0x01, 0x02, 0x03, 0x04, 0x00, 0x00];
            SlhDsaTestHelpers.AssertImportSubjectKeyPublicInfo(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Throws<CryptographicException>(() => import(indefiniteLengthOctet))));
        }

        [Fact]
        public static void ImportPkcs8_WrongTypeInAsn()
        {
            // Create an incorrect ASN.1 structure to pass into the import methods.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            AlgorithmIdentifierAsn algorithmIdentifier = new AlgorithmIdentifierAsn
            {
                Algorithm = SlhDsaTestHelpers.AlgorithmToOid(SlhDsaAlgorithm.SlhDsaSha2_128s),
            };
            algorithmIdentifier.Encode(writer);
            byte[] wrongAsnType = writer.Encode();

            SlhDsaTestHelpers.AssertImportSubjectKeyPublicInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(wrongAsnType))));

            SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(wrongAsnType))));

            SlhDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", wrongAsnType))));
        }

        [Fact]
        public static void ImportSubjectKeyPublicInfo_AlgorithmErrorsInAsn()
        {
#if !NETFRAMEWORK // Does not support exporting RSA SPKI
            if (!OperatingSystem.IsBrowser())
            {
                // RSA key
                using RSA rsa = RSA.Create();
                byte[] rsaSpkiBytes = rsa.ExportSubjectPublicKeyInfo();
                SlhDsaTestHelpers.AssertImportSubjectKeyPublicInfo(
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(rsaSpkiBytes))));
            }
#endif

            // Create an invalid SLH-DSA SPKI with parameters
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(SlhDsaAlgorithm.SlhDsaSha2_128s),
                    Parameters = SlhDsaTestHelpers.s_derBitStringFoo, // <-- Invalid
                },
                SubjectPublicKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes]
            };

            SlhDsaTestHelpers.AssertImportSubjectKeyPublicInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(spki.Encode()))));

            spki.Algorithm.Parameters = AsnUtils.DerNull;

            SlhDsaTestHelpers.AssertImportSubjectKeyPublicInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(spki.Encode()))));

            // Sanity check
            spki.Algorithm.Parameters = null;
            SlhDsaTestHelpers.AssertImportSubjectKeyPublicInfo(import => AssertThrowIfNotSupported(() => import(spki.Encode())));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_AlgorithmErrorsInAsn()
        {
#if !NETFRAMEWORK // Does not support exporting RSA PKCS#8 private key
            if (!OperatingSystem.IsBrowser())
            {
                // RSA key isn't valid for SLH-DSA
                using RSA rsa = RSA.Create();
                byte[] rsaPkcs8Bytes = rsa.ExportPkcs8PrivateKey();
                SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(rsaPkcs8Bytes))));
            }
#endif

            // Create an invalid SLH-DSA PKCS8 with parameters
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(SlhDsaAlgorithm.SlhDsaSha2_128s),
                    Parameters = SlhDsaTestHelpers.s_derBitStringFoo, // <-- Invalid
                },
                PrivateKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes]
            };

            SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode()))));

            pkcs8.PrivateKeyAlgorithm.Parameters = AsnUtils.DerNull;

            SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode()))));

            // Sanity check
            pkcs8.PrivateKeyAlgorithm.Parameters = null;
            SlhDsaTestHelpers.AssertImportPkcs8PrivateKey(import => AssertThrowIfNotSupported(() => import(pkcs8.Encode())));
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
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromPem(pem)));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromPem(pem.AsSpan())));
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
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER")));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"u8)));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromEncryptedPem(encryptedPem.AsSpan(), "PLACEHOLDER")));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"u8.ToArray())));
            }
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void AlgorithmMatches_GenerateKey(SlhDsaAlgorithm algorithm)
        {
            AssertThrowIfNotSupported(() =>
            {
                using SlhDsa slhDsa = SlhDsa.GenerateKey(algorithm);
                Assert.Equal(algorithm, slhDsa.Algorithm);
            });

            SlhDsaTestHelpers.AssertImportPublicKey(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Equal(algorithm, import().Algorithm)), algorithm, new byte[algorithm.PublicKeySizeInBytes]);

            SlhDsaTestHelpers.AssertImportSecretKey(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Equal(algorithm, import().Algorithm)), algorithm, new byte[algorithm.SecretKeySizeInBytes]);
        }

        /// <summary>
        /// Asserts that on platforms that do not support SLH-DSA, the input test throws PlatformNotSupportedException.
        /// If the test does pass, it implies that the test is validating code after the platform check.
        /// </summary>
        /// <param name="test">The test to run.</param>
        private static void AssertThrowIfNotSupported(Action test)
        {
            if (SlhDsa.IsSupported)
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
                    Assert.Contains("SlhDsa", pnse.Message);
                }
                catch (ThrowsException te) when (te.InnerException is PlatformNotSupportedException pnse)
                {
                    Assert.Contains("SlhDsa", pnse.Message);
                }
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
