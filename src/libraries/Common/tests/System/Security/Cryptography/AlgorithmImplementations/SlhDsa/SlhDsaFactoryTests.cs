// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
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

            AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[publicKeySize + 1]);
            AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[publicKeySize - 1]);
            AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);

            AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[secretKeySize + 1]);
            AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[secretKeySize - 1]);
            AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);
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

            static void AssertThrows(string pem) =>
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromPem(pem)));
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
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromEncryptedPem(encryptedPem, "password")));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromEncryptedPem(encryptedPem, "password"u8)));
            }
        }

        [Fact]
        public static void ArgumentValidation_MalformedAsnEncoding()
        {
            // Generate a valid ASN.1 encoding
            byte[] encodedBytes = CreateAsn1EncodedBytes();

            // Remove the last byte so the length indicated in the encoding will be larger than the actual data.
            AssertThrows(encodedBytes.AsMemory(..^1));

            // Add a trailing byte so the length indicated in the encoding will be larger than the actual data.
            Array.Resize(ref encodedBytes, encodedBytes.Length + 1);
            AssertThrows(encodedBytes);

            static void AssertThrows(ReadOnlyMemory<byte> encodedBytes)
            {
                AssertImportSubjectKeyPublicInfo(
                    import => Assert.Throws<CryptographicException>(import),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(import)),
                    encodedBytes);

                AssertImportPkcs8PrivateKey(
                    import => Assert.Throws<CryptographicException>(import),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(import)),
                    encodedBytes);

                AssertImportEncryptedPkcs8PrivateKey(
                    import => Assert.Throws<CryptographicException>(import),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(import)),
                    encodedBytes);
            }
        }

        [Fact]
        public static void ImportSpki_BerEncoding()
        {
            // Valid BER but invalid DER - uses indefinite length encoding
            byte[] indefiniteLengthOctet = [0x04, 0x80, 0x01, 0x02, 0x03, 0x04, 0x00, 0x00];
            AssertImportSubjectKeyPublicInfo(import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(import)), indefiniteLengthOctet);
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

            AssertImportSubjectKeyPublicInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(import)),
                wrongAsnType);

            AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(import)),
                wrongAsnType);

            AssertImportEncryptedPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(import)),
                wrongAsnType);
        }

        [Fact]
        public static void ImportSubjectKeyPublicInfo_AlgorithmErrorsInAsn()
        {
            // RSA key
            using RSA rsa = RSA.Create();
            byte[] rsaSpkiBytes = rsa.ExportSubjectPublicKeyInfo();
            AssertImportSubjectKeyPublicInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import())),
                rsaSpkiBytes);

            // Create an invalid SLH-DSA PKCS8 with parameters
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(SlhDsaAlgorithm.SlhDsaSha2_128s),
                    Parameters = rsaSpkiBytes, // <-- Invalid
                },
                SubjectPublicKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes]
            };

            AssertImportSubjectKeyPublicInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import())),
                spki.Encode());

            // Sanity check
            spki.Algorithm.Parameters = null;
            AssertImportSubjectKeyPublicInfo(import => AssertThrowIfNotSupported(() => import()), spki.Encode());
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_AlgorithmErrorsInAsn()
        {
            if (!OperatingSystem.IsBrowser())
            {
                // RSA key isn't valid for SLH-DSA
                using RSA rsa = RSA.Create();
                byte[] rsaPkcs8Bytes = rsa.ExportPkcs8PrivateKey();
                AssertImportPkcs8PrivateKey(
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import())),
                    rsaPkcs8Bytes);
            }

            // Create an invalid SLH-DSA PKCS8 with parameters
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteBitString("random bitstring"u8);
            byte[] someEncodedBytes = writer.Encode();
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(SlhDsaAlgorithm.SlhDsaSha2_128s),
                    Parameters = someEncodedBytes, // <-- Invalid
                },
                PrivateKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes]
            };

            AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import())),
                pkcs8.Encode());

            // Sanity check
            pkcs8.PrivateKeyAlgorithm.Parameters = null;
            AssertImportPkcs8PrivateKey(import => AssertThrowIfNotSupported(() => import()), pkcs8.Encode());
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

            AssertImportPublicKey(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Equal(algorithm, import().Algorithm)), algorithm, new byte[algorithm.PublicKeySizeInBytes]);

            AssertImportSecretKey(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Equal(algorithm, import().Algorithm)), algorithm, new byte[algorithm.SecretKeySizeInBytes]);
        }

        private static void AssertImportPublicKey(Action<Func<SlhDsa>> test, SlhDsaAlgorithm algorithm, ReadOnlyMemory<byte> publicKey) =>
            AssertImportPublicKey(test, test, algorithm, publicKey);

        private static void AssertImportPublicKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, SlhDsaAlgorithm algorithm, ReadOnlyMemory<byte> publicKey)
        {
            if (publicKey.Length == 0)
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, Array.Empty<byte>()));
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, publicKey.Span));
            }

            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(algorithm) ?? throw new XunitException("Cannot create PKCS#8 private key because algorithm is unknown."),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                SubjectPublicKey = publicKey,
            };

            AssertImportSubjectKeyPublicInfo(testEmbeddedCall, testEmbeddedCall, spki.Encode());
        }

        private static void AssertImportSubjectKeyPublicInfo(Action<Func<SlhDsa>> test, ReadOnlyMemory<byte> spki) =>
            AssertImportSubjectKeyPublicInfo(test, test, spki);

        private static void AssertImportSubjectKeyPublicInfo(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, ReadOnlyMemory<byte> spki)
        {
            if (spki.Length == 0)
            {
                testDirectCall(() => SlhDsa.ImportSubjectPublicKeyInfo([]));
                testDirectCall(() => SlhDsa.ImportSubjectPublicKeyInfo(ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => SlhDsa.ImportSubjectPublicKeyInfo(spki.Span));
            }

            testEmbeddedCall(() => SlhDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki.Span)));
        }

        private static void AssertImportSecretKey(Action<Func<SlhDsa>> test, SlhDsaAlgorithm algorithm, ReadOnlyMemory<byte> secretKey) =>
            AssertImportSecretKey(test, test, algorithm, secretKey);

        private static void AssertImportSecretKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, SlhDsaAlgorithm algorithm, ReadOnlyMemory<byte> secretKey)
        {
            if (secretKey.Length == 0)
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, Array.Empty<byte>()));
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, secretKey.Span));
            }

            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(algorithm) ?? throw new XunitException("Cannot create PKCS#8 private key because algorithm is unknown."),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                PrivateKey = secretKey,
            };

            AssertImportPkcs8PrivateKey(testEmbeddedCall, testEmbeddedCall, pkcs8.Encode());
        }

        private static void AssertImportPkcs8PrivateKey(Action<Func<SlhDsa>> test, ReadOnlyMemory<byte> pkcs8) =>
            AssertImportPkcs8PrivateKey(test, test, pkcs8);

        private static void AssertImportPkcs8PrivateKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, ReadOnlyMemory<byte> pkcs8)
        {
            testDirectCall(() => SlhDsa.ImportPkcs8PrivateKey(pkcs8.Span));
            testEmbeddedCall(() => SlhDsa.ImportFromPem(PemEncoding.WriteString("PRIVATE KEY", pkcs8.Span)));
        }

        private static void AssertImportEncryptedPkcs8PrivateKey(Action<Func<SlhDsa>> test, ReadOnlyMemory<byte> pkcs8, bool onlyCharPassword = false) =>
            AssertImportEncryptedPkcs8PrivateKey(test, test, pkcs8, onlyCharPassword);

        private static void AssertImportEncryptedPkcs8PrivateKey(
            Action<Func<SlhDsa>> testDirectCall,
            Action<Func<SlhDsa>> testEmbeddedCall,
            ReadOnlyMemory<byte> pkcs8,
            bool onlyCharPassword = false)
        {
            if (pkcs8.Length == 0)
            {
                testDirectCall(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("password", []));
                testDirectCall(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("password", ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("password", pkcs8.Span));
            }

            testEmbeddedCall(() => SlhDsa.ImportFromEncryptedPem(PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", pkcs8.Span), "password"));

            if (!onlyCharPassword)
            {
                if (pkcs8.Length == 0)
                {
                    testDirectCall(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("password"u8, []));
                    testDirectCall(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("password"u8, ReadOnlySpan<byte>.Empty));
                }
                else
                {
                    testDirectCall(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("password"u8, pkcs8.Span));
                }

                testEmbeddedCall(() => SlhDsa.ImportFromEncryptedPem(PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", pkcs8.Span), "password"u8));
            }
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
            $"-----BEGIN {label}-----\n{data}\n-----END {label}-----";

        private static byte[] CreateAsn1EncodedBytes()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteOctetString("some data"u8);
            byte[] encodedBytes = writer.Encode();
            return encodedBytes;
        }
    }
}
