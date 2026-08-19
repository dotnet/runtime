// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLKemFactoryTests
    {
        [Fact]
        public static void NullArgumentValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLKem.GenerateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLKem.IsAlgorithmSupported(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLKem.ImportEncapsulationKey(null, null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLKem.ImportEncapsulationKey(null, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLKem.ImportDecapsulationKey(null, null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLKem.ImportDecapsulationKey(null, ReadOnlySpan<byte>.Empty));

            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLKem.ImportEncapsulationKey(CompositeMLKemAlgorithm.MLKem768WithX25519, null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLKem.ImportDecapsulationKey(CompositeMLKemAlgorithm.MLKem768WithX25519, null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLKem.ImportPkcs8PrivateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLKem.ImportSubjectPublicKeyInfo(null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLKem.ImportFromPem(null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLKem.ImportEncryptedPkcs8PrivateKey("PLACEHOLDER", null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLKem.ImportFromEncryptedPem(null, (string)null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLKem.ImportFromEncryptedPem(null, (byte[])null));

            AssertExtensions.Throws<ArgumentNullException>("password", static () => CompositeMLKem.ImportEncryptedPkcs8PrivateKey((string)null, null));
            AssertExtensions.Throws<ArgumentNullException>("password", static () => CompositeMLKem.ImportFromEncryptedPem(string.Empty, (string)null));

            AssertExtensions.Throws<ArgumentNullException>("passwordBytes", static () => CompositeMLKem.ImportFromEncryptedPem(string.Empty, (byte[])null));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void IsAlgorithmSupported_AgreesWithPlatform(CompositeMLKemAlgorithm algorithm)
        {
            // No platform implements Composite ML-KEM yet.
            AssertExtensions.FalseExpression(CompositeMLKem.IsAlgorithmSupported(algorithm));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void AlgorithmMatches_GenerateKey(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKem kem = CompositeMLKem.GenerateKey(algorithm);
            Assert.Equal(algorithm, kem.Algorithm);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ImportEncapsulationKey_InvalidSizes(CompositeMLKemAlgorithm algorithm)
        {
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeUpperBound(algorithm);

            AssertImportBadEncapsulationKey(algorithm, Array.Empty<byte>());
            AssertImportBadEncapsulationKey(algorithm, new byte[lowerBound - 1]);
            AssertImportBadEncapsulationKey(algorithm, new byte[upperBound + 1]);

            // The ML-KEM component alone is not a valid Composite ML-KEM encapsulation key.
            AssertImportBadEncapsulationKey(
                algorithm,
                new byte[CompositeMLKemTestData.GetMLKemAlgorithm(algorithm).EncapsulationKeySizeInBytes]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ImportDecapsulationKey_InvalidSizes(CompositeMLKemAlgorithm algorithm)
        {
            int lowerBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeUpperBound(algorithm);

            AssertImportBadDecapsulationKey(algorithm, Array.Empty<byte>());
            AssertImportBadDecapsulationKey(algorithm, new byte[lowerBound - 1]);
            AssertImportBadDecapsulationKey(algorithm, new byte[upperBound + 1]);

            // The ML-KEM private seed alone is not a valid Composite ML-KEM decapsulation key.
            AssertImportBadDecapsulationKey(
                algorithm,
                new byte[CompositeMLKemTestData.GetMLKemAlgorithm(algorithm).PrivateSeedSizeInBytes]);
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_UnknownAlgorithm()
        {
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    // ML-KEM is not Composite ML-KEM.
                    Algorithm = MLKemTestData.MlKem768Oid,
                    Parameters = null,
                },
                SubjectPublicKey = new byte[1216],
            };

            byte[] encoded = spki.Encode();

            CompositeMLKemTestHelpers.AssertImportSubjectPublicKeyInfo(import =>
            {
                CryptographicException ex = Assert.Throws<CryptographicException>(() => import(encoded));
                Assert.DoesNotContain(nameof(CompositeMLKem), ex.Message);
            });
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_UnknownAlgorithm()
        {
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    // ML-KEM is not Composite ML-KEM.
                    Algorithm = MLKemTestData.MlKem768Oid,
                    Parameters = null,
                },
                PrivateKey = new byte[96],
            };

            byte[] encoded = pkcs8.Encode();

            CompositeMLKemTestHelpers.AssertImportPkcs8PrivateKey(import =>
            {
                CryptographicException ex = Assert.Throws<CryptographicException>(() => import(encoded));
                Assert.DoesNotContain(nameof(CompositeMLKem), ex.Message);
            });
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_AlgorithmErrorsInAsn()
        {
            CompositeMLKemAlgorithm algorithm = CompositeMLKemAlgorithm.MLKem768WithX25519;

            // Create an invalid Composite ML-KEM SPKI with parameters
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = CompositeMLKemTestHelpers.AlgorithmToOid(algorithm),
                    Parameters = CompositeMLKemTestHelpers.s_derBitStringFoo, // <-- Invalid
                },
                SubjectPublicKey = new byte[CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm)],
            };

            CompositeMLKemTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => Assert.Throws<CryptographicException>(() => import(spki.Encode())));

            spki.Algorithm.Parameters = AsnUtils.DerNull;

            CompositeMLKemTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => Assert.Throws<CryptographicException>(() => import(spki.Encode())));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_AlgorithmErrorsInAsn()
        {
            CompositeMLKemAlgorithm algorithm = CompositeMLKemAlgorithm.MLKem768WithX25519;

            // Create an invalid Composite ML-KEM PKCS#8 with parameters
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = CompositeMLKemTestHelpers.AlgorithmToOid(algorithm),
                    Parameters = CompositeMLKemTestHelpers.s_derBitStringFoo, // <-- Invalid
                },
                PrivateKey = new byte[CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm)],
            };

            CompositeMLKemTestHelpers.AssertImportPkcs8PrivateKey(
                import => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode())));

            pkcs8.PrivateKeyAlgorithm.Parameters = AsnUtils.DerNull;

            CompositeMLKemTestHelpers.AssertImportPkcs8PrivateKey(
                import => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode())));
        }

        [Fact]
        public static void Import_EncodedKeyTrailingData()
        {
            AssertSubjectPublicKeyInfoImportThrows(AppendTrailingByte(CreateEmptySubjectPublicKeyInfo()));
            AssertPkcs8PrivateKeyImportThrows(AppendTrailingByte(CreateEmptyPkcs8PrivateKey()));
        }

        [Fact]
        public static void Import_EncodedKeyTruncated()
        {
            AssertSubjectPublicKeyInfoImportThrows(TruncateLastByte(CreateEmptySubjectPublicKeyInfo()));
            AssertPkcs8PrivateKeyImportThrows(TruncateLastByte(CreateEmptyPkcs8PrivateKey()));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires AES, which is not supported on Browser.")]
        public static void ImportEncryptedPkcs8PrivateKey_TrailingData()
        {
            byte[] encryptedPkcs8 = CompositeMLKemTestHelpers.CreateEncryptedPkcs8PrivateKey(
                CompositeMLKemTestHelpers.AlgorithmToOid(CompositeMLKemAlgorithm.MLKem768WithX25519),
                [],
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 42));

            AssertEncryptedPkcs8PrivateKeyImportThrows(AppendTrailingByte(encryptedPkcs8));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires AES, which is not supported on Browser.")]
        public static void ImportEncryptedPkcs8PrivateKey_Truncated()
        {
            byte[] encryptedPkcs8 = CompositeMLKemTestHelpers.CreateEncryptedPkcs8PrivateKey(
                CompositeMLKemTestHelpers.AlgorithmToOid(CompositeMLKemAlgorithm.MLKem768WithX25519),
                [],
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 42));

            AssertEncryptedPkcs8PrivateKeyImportThrows(TruncateLastByte(encryptedPkcs8));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires AES, which is not supported on Browser.")]
        public static void ImportEncryptedPkcs8PrivateKey_WrongPassword()
        {
            byte[] encryptedPkcs8 = CompositeMLKemTestHelpers.CreateEncryptedPkcs8PrivateKey(
                CompositeMLKemTestHelpers.AlgorithmToOid(CompositeMLKemAlgorithm.MLKem768WithX25519),
                [],
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 42));

            CompositeMLKemTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                import => Assert.Throws<CryptographicException>(() => import("WRONG PASSWORD", encryptedPkcs8)));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires AES, which is not supported on Browser.")]
        public static void ImportEncryptedPkcs8PrivateKey_NotCompositeMLKemKey()
        {
            byte[] encryptedPkcs8 = CompositeMLKemTestHelpers.CreateEncryptedPkcs8PrivateKey(
                "1.3.6.1.5.5.7.6.54",
                [],
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 42),
                CompositeMLKemTestHelpers.EncryptionPasswordType.Byte);

            CompositeMLKemTestHelpers.AssertImportEncryptedPkcs8PrivateKey(import =>
            {
                CryptographicException exception =
                    Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encryptedPkcs8));
                Assert.DoesNotContain(nameof(CompositeMLKem), exception.Message);
            });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires TripleDES, which is not supported on Browser.")]
        public static void ImportEncryptedPkcs8PrivateKey_BytePassword_RejectsPkcs12Kdf()
        {
            byte[] encryptedPkcs8 = CompositeMLKemTestHelpers.CreateEncryptedPkcs8PrivateKey(
                CompositeMLKemTestHelpers.AlgorithmToOid(CompositeMLKemAlgorithm.MLKem768WithX25519),
                [],
                new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42));

            CompositeMLKemTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                import => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encryptedPkcs8)),
                CompositeMLKemTestHelpers.EncryptionPasswordType.Byte);
        }

        [Fact]
        public static void Import_WrongAsnType()
        {
            // Create an incorrect ASN.1 structure to pass into the import methods.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            AlgorithmIdentifierAsn algorithmIdentifier = new AlgorithmIdentifierAsn
            {
                Algorithm = CompositeMLKemTestHelpers.AlgorithmToOid(CompositeMLKemAlgorithm.MLKem768WithX25519),
            };
            algorithmIdentifier.Encode(writer);
            byte[] wrongAsnType = writer.Encode();

            CompositeMLKemTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => Assert.Throws<CryptographicException>(() => import(wrongAsnType)));

            CompositeMLKemTestHelpers.AssertImportPkcs8PrivateKey(
                import => Assert.Throws<CryptographicException>(() => import(wrongAsnType)));

            CompositeMLKemTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                import => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", wrongAsnType)));
        }

        [Fact]
        public static void ImportSpki_BerEncoding()
        {
            CompositeMLKemAlgorithm algorithm = CompositeMLKemAlgorithm.MLKem768WithX25519;

            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = CompositeMLKemTestHelpers.AlgorithmToOid(algorithm),
                    Parameters = null,
                },
                SubjectPublicKey = new byte[CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm)],
            };

            byte[] berSpki = AsnUtils.ConvertDerToNonDerBer(spki.Encode());

            // SubjectPublicKeyInfo must be DER encoded.
            CompositeMLKemTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => Assert.Throws<CryptographicException>(() => import(berSpki)));
        }

        [Fact]
        public static void ImportFromPem_MissingRecognizedLabel()
        {
            AssertImportFromPemArgumentException(WritePemRaw("UNKNOWN LABEL", []));
            AssertImportFromPemArgumentException(string.Empty);
            AssertImportFromPemArgumentException(WritePemRaw("ENCRYPTED PRIVATE KEY", []));
            AssertImportFromPemArgumentException(WritePemRaw("PRIVATE KEY", "%"));
            AssertImportFromPemArgumentException(WritePemRaw("PUBLIC KEY", "%"));
        }

        [Fact]
        public static void ImportFromPem_MultipleRecognizedLabels()
        {
            AssertImportFromPemArgumentException(WritePemRaw("PUBLIC KEY", []) + '\n' + WritePemRaw("PUBLIC KEY", []));
            AssertImportFromPemArgumentException(WritePemRaw("PRIVATE KEY", []) + '\n' + WritePemRaw("PUBLIC KEY", []));
            AssertImportFromPemArgumentException(WritePemRaw("PUBLIC KEY", []) + '\n' + WritePemRaw("PRIVATE KEY", []));
            AssertImportFromPemArgumentException(WritePemRaw("PRIVATE KEY", []) + '\n' + WritePemRaw("PRIVATE KEY", []));
        }

        [Fact]
        public static void ImportFromEncryptedPem_MissingRecognizedLabel()
        {
            AssertImportFromEncryptedPemArgumentException(WritePemRaw("UNKNOWN LABEL", []));
            AssertImportFromEncryptedPemArgumentException(WritePemRaw("CERTIFICATE", []));
            AssertImportFromEncryptedPemArgumentException(string.Empty);
            AssertImportFromEncryptedPemArgumentException(WritePemRaw("ENCRYPTED PRIVATE KEY", "%"));
        }

        [Fact]
        public static void ImportFromEncryptedPem_MultipleRecognizedLabels()
        {
            AssertImportFromEncryptedPemArgumentException(
                WritePemRaw("ENCRYPTED PRIVATE KEY", []) + '\n' + WritePemRaw("ENCRYPTED PRIVATE KEY", []));
        }

        private static void AssertImportBadEncapsulationKey(CompositeMLKemAlgorithm algorithm, byte[] encapsulationKey)
        {
            CompositeMLKemTestHelpers.AssertImportEncapsulationKey(
                import => Assert.Throws<CryptographicException>(() => import()),
                algorithm,
                encapsulationKey);
        }

        private static void AssertImportBadDecapsulationKey(CompositeMLKemAlgorithm algorithm, byte[] decapsulationKey)
        {
            CompositeMLKemTestHelpers.AssertImportDecapsulationKey(
                import => Assert.Throws<CryptographicException>(() => import()),
                algorithm,
                decapsulationKey);
        }

        private static byte[] CreateEmptySubjectPublicKeyInfo()
        {
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = CompositeMLKemTestHelpers.AlgorithmToOid(CompositeMLKemAlgorithm.MLKem768WithX25519),
                    Parameters = null,
                },
                SubjectPublicKey = ReadOnlyMemory<byte>.Empty,
            };

            return spki.Encode();
        }

        private static byte[] CreateEmptyPkcs8PrivateKey()
        {
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = CompositeMLKemTestHelpers.AlgorithmToOid(CompositeMLKemAlgorithm.MLKem768WithX25519),
                    Parameters = null,
                },
                PrivateKey = ReadOnlyMemory<byte>.Empty,
            };

            return pkcs8.Encode();
        }

        private static byte[] AppendTrailingByte(byte[] encoded)
        {
            Array.Resize(ref encoded, encoded.Length + 1);
            return encoded;
        }

        private static byte[] TruncateLastByte(byte[] encoded)
        {
            Array.Resize(ref encoded, encoded.Length - 1);
            return encoded;
        }

        private static void AssertSubjectPublicKeyInfoImportThrows(byte[] encoded) =>
            CompositeMLKemTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => Assert.Throws<CryptographicException>(() => import(encoded)));

        private static void AssertPkcs8PrivateKeyImportThrows(byte[] encoded) =>
            CompositeMLKemTestHelpers.AssertImportPkcs8PrivateKey(
                import => Assert.Throws<CryptographicException>(() => import(encoded)));

        private static void AssertEncryptedPkcs8PrivateKeyImportThrows(byte[] encoded) =>
            CompositeMLKemTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                import => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encoded)));

        private static void AssertImportFromPemArgumentException(string pem)
        {
            AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLKem.ImportFromPem(pem));
            AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLKem.ImportFromPem(pem.AsSpan()));
        }

        private static void AssertImportFromEncryptedPemArgumentException(string encryptedPem)
        {
            AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLKem.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"));
            AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLKem.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"u8));
            AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLKem.ImportFromEncryptedPem(encryptedPem.AsSpan(), "PLACEHOLDER"));
            AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLKem.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"u8.ToArray()));
        }

        private static string WritePemRaw(string label, ReadOnlySpan<char> data) =>
            $"-----BEGIN {label}-----\n{data.ToString()}\n-----END {label}-----";
    }
}
