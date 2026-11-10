// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public static class X25519DiffieHellmanTests
    {
        private static readonly byte[] s_asnNull = [0x05, 0x00];

        private static readonly byte[] AliceSpki = X25519DiffieHellmanTestData.AliceSpki;
        private static readonly byte[] AlicePkcs8 = X25519DiffieHellmanTestData.AlicePkcs8;
        private static readonly byte[] AliceEncryptedPkcs8 = X25519DiffieHellmanTestData.AliceEncryptedPkcs8;

        [Fact]
        public static void ImportPrivateKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                X25519DiffieHellman.ImportPrivateKey((byte[])null));
        }

        [Fact]
        public static void ImportPrivateKey_WrongSize_Array()
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPrivateKey(new byte[X25519DiffieHellman.PrivateKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPrivateKey(new byte[X25519DiffieHellman.PrivateKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPrivateKey(Array.Empty<byte>()));
        }

        [Fact]
        public static void ImportPrivateKey_WrongSize_Span()
        {
            byte[] key = new byte[X25519DiffieHellman.PrivateKeySizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPrivateKey(key.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPrivateKey(key.AsSpan(0, key.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPrivateKey(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportPublicKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                X25519DiffieHellman.ImportPublicKey((byte[])null));
        }

        [Fact]
        public static void ImportPublicKey_WrongSize_Array()
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPublicKey(new byte[X25519DiffieHellman.PublicKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPublicKey(new byte[X25519DiffieHellman.PublicKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPublicKey(Array.Empty<byte>()));
        }

        [Fact]
        public static void ImportPublicKey_WrongSize_Span()
        {
            byte[] key = new byte[X25519DiffieHellman.PublicKeySizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPublicKey(key.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPublicKey(key.AsSpan(0, key.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                X25519DiffieHellman.ImportPublicKey(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                X25519DiffieHellman.ImportSubjectPublicKeyInfo((byte[])null));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_WrongAlgorithm()
        {
            byte[] ecP256Spki = Convert.FromBase64String(
                "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEuiPJ2IV089LVrXZGDo9Mc542UZZE" +
                "UtPQVd60Ckb/u5OXHAlmITVzFPThKI+N/bUMEnnHEmF8ZDUtLiQPBaKiMQ==");
            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportSubjectPublicKeyInfo(ecP256Spki));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_NotAsn()
        {
            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportSubjectPublicKeyInfo("potatoes"u8));
            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportSubjectPublicKeyInfo("potatoes"u8.ToArray()));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_WrongParameters()
        {
            // RFC 8410: AlgorithmIdentifier parameters MUST be absent
            byte[] spki = SpkiEncode(
                X25519DiffieHellmanTestData.X25519Oid,
                new byte[X25519DiffieHellman.PublicKeySizeInBytes],
                algorithmParameters: s_asnNull);

            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportSubjectPublicKeyInfo(spki));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_WrongSize()
        {
            byte[] spki = SpkiEncode(
                X25519DiffieHellmanTestData.X25519Oid,
                new byte[X25519DiffieHellman.PublicKeySizeInBytes - 1]);

            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportSubjectPublicKeyInfo(spki));

            spki = SpkiEncode(
                X25519DiffieHellmanTestData.X25519Oid,
                new byte[X25519DiffieHellman.PublicKeySizeInBytes + 1]);

            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportSubjectPublicKeyInfo(spki));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_TrailingData()
        {
            byte[] oversized = new byte[AliceSpki.Length + 1];
            AliceSpki.AsSpan().CopyTo(oversized);
            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportSubjectPublicKeyInfo(oversized));
            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(oversized)));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                X25519DiffieHellman.ImportPkcs8PrivateKey((byte[])null));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_WrongAlgorithm()
        {
            byte[] ecP256Key = Convert.FromBase64String(
                "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgZg/vYKeaTgco6dGx" +
                "6KCMw5/L7/Xu7j7idYWNSCBcod6hRANCAASc/jV6ZojlesoM+qNnSYZdc7Fkd4+E" +
                "2raDwlFPZGucEHDUmdCwaDx/hglDZaLimpD/67F5k5jUe+I3CkijLST7");

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(ecP256Key)));
            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportPkcs8PrivateKey(ecP256Key));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ImportPkcs8PrivateKey_BogusAsnChoice(bool useSpanImport)
        {
            // SEQUENCE {
            //   INTEGER 0
            //   SEQUENCE {
            //     OBJECT IDENTIFIER 1.3.101.110 (id-X25519)
            //   }
            //   PRINTABLE STRING "Potato"
            // }
            byte[] pkcs8 = "3014020100300506032b656e1306506F7461746F".HexToByteArray();

            if (useSpanImport)
            {
                Assert.Throws<CryptographicException>(() =>
                    X25519DiffieHellman.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(pkcs8)));
            }
            else
            {
                Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8));
            }
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_WrongKeySize()
        {
            byte[] pkcs8 = Pkcs8Encode(
                X25519DiffieHellmanTestData.X25519Oid,
                new byte[X25519DiffieHellman.PrivateKeySizeInBytes - 1]);

            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8));

            pkcs8 = Pkcs8Encode(
                X25519DiffieHellmanTestData.X25519Oid,
                new byte[X25519DiffieHellman.PrivateKeySizeInBytes + 1]);

            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_BadAlgorithmIdentifier()
        {
            // RFC 8410: AlgorithmIdentifier parameters MUST be absent
            byte[] pkcs8 = Pkcs8Encode(
                X25519DiffieHellmanTestData.X25519Oid,
                X25519DiffieHellmanTestData.AlicePrivateKey,
                algorithmParameters: s_asnNull);

            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8.AsSpan()));
            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_TrailingData()
        {
            byte[] oversized = new byte[AlicePkcs8.Length + 1];
            AlicePkcs8.AsSpan().CopyTo(oversized);

            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey(oversized.AsSpan()));
            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey(oversized));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_NotAsn()
        {
            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey("potatoes"u8));
            Assert.Throws<CryptographicException>(() => X25519DiffieHellman.ImportPkcs8PrivateKey("potatoes"u8.ToArray()));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_Array_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPkcs8PrivateKey(AlicePkcs8);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_Span_Roundtrip()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(AlicePkcs8));
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_WrongAlgorithm()
        {
            byte[] ecP256Key = Convert.FromBase64String(
                "MIHrMFYGCSqGSIb3DQEFDTBJMDEGCSqGSIb3DQEFDDAkBBCr0ipJGBOnThng8uXT" +
                "iyZWAgIIADAMBggqhkiG9w0CCQUAMBQGCCqGSIb3DQMHBAgNPETMQWxeYgSBkN4J" +
                "tW/1aNLGpRCBPvz2aHMulF/bBRRy3G8hwidysLR/mc0CaFWeltzZUpSGJgMSDJE4" +
                "/zQJXhyXcEApuChzg0H0o8cPK1SCyi4wScMokiUHskOhcxhyr1VQ7cFAT+qS+66C" +
                "gJoH9z0+/Z9WzLU8ix8F7B+HWwRhib5Cd6si+AX6DsNelMq2zP1NO7Un416dkg==");

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword,
                    ecP256Key));

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword.AsSpan(),
                    new ReadOnlySpan<byte>(ecP256Key)));

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPasswordBytes,
                    new ReadOnlySpan<byte>(ecP256Key)));
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_TrailingData()
        {
            byte[] oversized = new byte[AliceEncryptedPkcs8.Length + 1];
            AliceEncryptedPkcs8.AsSpan().CopyTo(oversized);

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword,
                    oversized));

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword.AsSpan(),
                    oversized));

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPasswordBytes,
                    oversized));
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_NotAsn()
        {
            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword,
                    "potatoes"u8.ToArray()));

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword.AsSpan(),
                    "potatoes"u8));

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPasswordBytes,
                    "potatoes"u8));
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_DoesNotProcessUnencryptedData()
        {
            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, AlicePkcs8));

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, AlicePkcs8));

            Assert.Throws<CryptographicException>(() =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(string.Empty, AlicePkcs8));
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_CharPassword()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword.AsSpan(), AliceEncryptedPkcs8);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_StringPassword()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword, AliceEncryptedPkcs8);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_BytePassword()
        {
            using X25519DiffieHellman xdh = X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                X25519DiffieHellmanTestData.EncryptedPrivateKeyPasswordBytes, AliceEncryptedPkcs8);
            AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_NullArgs()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey(
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword,
                    (byte[])null));

            AssertExtensions.Throws<ArgumentNullException>("password", static () =>
                X25519DiffieHellman.ImportEncryptedPkcs8PrivateKey((string)null, AliceEncryptedPkcs8));
        }

        [Fact]
        public static void ImportFromPem_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                X25519DiffieHellman.ImportFromPem((string)null));
        }

        [Fact]
        public static void ImportFromPem_PublicKey_Roundtrip()
        {
            string pem = WritePem("PUBLIC KEY", AliceSpki);
            AssertImportFromPem(importer =>
            {
                using X25519DiffieHellman xdh = importer(pem);
                byte[] exportedSpki = xdh.ExportSubjectPublicKeyInfo();
                AssertExtensions.SequenceEqual(AliceSpki, exportedSpki);
            });
        }

        [Fact]
        public static void ImportFromPem_PublicKey_IgnoresNotUnderstoodPems()
        {
            string pem = $"""
            -----BEGIN POTATO-----
            dmluY2U=
            -----END POTATO-----
            {WritePem("PUBLIC KEY", AliceSpki)}
            """;

            AssertImportFromPem(importer =>
            {
                using X25519DiffieHellman xdh = importer(pem);
                byte[] exportedSpki = xdh.ExportSubjectPublicKeyInfo();
                AssertExtensions.SequenceEqual(AliceSpki, exportedSpki);
            });
        }

        [Fact]
        public static void ImportFromPem_PrivateKey_Roundtrip()
        {
            string pem = WritePem("PRIVATE KEY", AlicePkcs8);
            AssertImportFromPem(importer =>
            {
                using X25519DiffieHellman xdh = importer(pem);
                AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
            });
        }

        [Fact]
        public static void ImportFromPem_PrivateKey_IgnoresNotUnderstoodPems()
        {
            string pem = $"""
            -----BEGIN UNKNOWN-----
            cGNq
            -----END UNKNOWN-----
            {WritePem("PRIVATE KEY", AlicePkcs8)}
            """;

            AssertImportFromPem(importer =>
            {
                using X25519DiffieHellman xdh = importer(pem);
                AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
            });
        }

        [Fact]
        public static void ImportFromPem_AmbiguousImportWithPublicKey_Throws()
        {
            string pem = $"""
            {WritePem("PUBLIC KEY", AliceSpki)}
            {WritePem("PUBLIC KEY", AliceSpki)}
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_AmbiguousImportWithPrivateKey_Throws()
        {
            string pem = $"""
            {WritePem("PUBLIC KEY", AliceSpki)}
            {WritePem("PRIVATE KEY", AlicePkcs8)}
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_AmbiguousImportWithEncryptedPrivateKey_Throws()
        {
            string pem = $"""
            {WritePem("PUBLIC KEY", AliceSpki)}
            {WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8)}
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_AmbiguousImportWithPrivateKeyAndEncryptedPrivateKey_Throws()
        {
            string pem = $"""
            {WritePem("PRIVATE KEY", AlicePkcs8)}
            {WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8)}
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKeyAndEncryptedPrivateKey_ImportsEncrypted()
        {
            string pem = $"""
            {WritePem("PRIVATE KEY", AlicePkcs8)}
            {WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8)}
            """;

            AssertImportFromEncryptedPem(importer =>
            {
                using X25519DiffieHellman xdh = importer(pem, X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword);
                AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
            });
        }

        [Fact]
        public static void ImportFromPem_EncryptedPrivateKey_Throws()
        {
            string pem = WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8);
            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_NoUnderstoodPem_Throws()
        {
            string pem = """
            -----BEGIN UNKNOWN-----
            cGNq
            -----END UNKNOWN-----
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source",
                static () => X25519DiffieHellman.ImportFromEncryptedPem(
                    (string)null,
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword));

            AssertExtensions.Throws<ArgumentNullException>("source",
                static () => X25519DiffieHellman.ImportFromEncryptedPem(
                    (string)null,
                    X25519DiffieHellmanTestData.EncryptedPrivateKeyPasswordBytes.ToArray()));
        }

        [Fact]
        public static void ImportFromEncryptedPem_NullPassword()
        {
            AssertExtensions.Throws<ArgumentNullException>("password",
                static () => X25519DiffieHellman.ImportFromEncryptedPem("the pem", (string)null));

            AssertExtensions.Throws<ArgumentNullException>("passwordBytes",
                static () => X25519DiffieHellman.ImportFromEncryptedPem("the pem", (byte[])null));
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_Roundtrip()
        {
            string pem = WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8);
            AssertImportFromEncryptedPem(importer =>
            {
                using X25519DiffieHellman xdh = importer(pem, X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword);
                AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_Ambiguous_Throws()
        {
            string pem = $"""
            {WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8)}
            {WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8)}
            """;
            AssertImportFromEncryptedPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source",
                    () => importer(pem, X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword));
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_DoesNotImportNonEncrypted()
        {
            string pem = WritePem("PRIVATE KEY", AlicePkcs8);
            AssertImportFromEncryptedPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source",
                    () => importer(pem, ""));
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_NoUnderstoodPem_Throws()
        {
            string pem = """
            -----BEGIN UNKNOWN-----
            cGNq
            -----END UNKNOWN-----
            """;
            AssertImportFromEncryptedPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source",
                    () => importer(pem, ""));
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_IgnoresNotUnderstoodPems()
        {
            string pem = $"""
            -----BEGIN UNKNOWN-----
            cGNq
            -----END UNKNOWN-----
            {WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8)}
            """;
            AssertImportFromEncryptedPem(importer =>
            {
                using X25519DiffieHellman xdh = importer(pem, X25519DiffieHellmanTestData.EncryptedPrivateKeyPassword);
                AssertExtensions.SequenceEqual(X25519DiffieHellmanTestData.AlicePrivateKey, xdh.ExportPrivateKey());
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_WrongPassword()
        {
            string pem = WritePem("ENCRYPTED PRIVATE KEY", AliceEncryptedPkcs8);
            AssertImportFromEncryptedPem(importer =>
            {
                Assert.Throws<CryptographicException>(
                    () => importer(pem, "WRONG"));
            });
        }

        private static void AssertImportFromPem(Action<Func<string, X25519DiffieHellman>> callback)
        {
            callback(static (string pem) => X25519DiffieHellman.ImportFromPem(pem));
            callback(static (string pem) => X25519DiffieHellman.ImportFromPem(pem.AsSpan()));
        }

        private static void AssertImportFromEncryptedPem(Action<Func<string, string, X25519DiffieHellman>> callback)
        {
            callback(static (string pem, string password) => X25519DiffieHellman.ImportFromEncryptedPem(pem, password));

            callback(static (string pem, string password) => X25519DiffieHellman.ImportFromEncryptedPem(
                pem.AsSpan(),
                password.AsSpan()));

            callback(static (string pem, string password) => X25519DiffieHellman.ImportFromEncryptedPem(
                pem,
                Encoding.UTF8.GetBytes(password)));

            callback(static (string pem, string password) => X25519DiffieHellman.ImportFromEncryptedPem(
                pem.AsSpan(),
                new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password))));
        }

        private static string WritePem(string label, byte[] contents)
        {
            string base64 = Convert.ToBase64String(contents, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN {label}-----\n{base64}\n-----END {label}-----";
        }

        private static byte[] SpkiEncode(string oid, byte[] publicKey, byte[] algorithmParameters = null)
        {
            AsnWriter writer = new(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(oid);

                    if (algorithmParameters is not null)
                    {
                        writer.WriteEncodedValue(algorithmParameters);
                    }
                }

                writer.WriteBitString(publicKey, 0);
            }

            return writer.Encode();
        }

        private static byte[] Pkcs8Encode(string oid, byte[] privateKey, byte[] algorithmParameters = null)
        {
            AsnWriter privateKeyWriter = new(AsnEncodingRules.DER);
            privateKeyWriter.WriteOctetString(privateKey);
            byte[] encodedPrivateKey = privateKeyWriter.Encode();

            AsnWriter writer = new(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                writer.WriteInteger(0);

                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(oid);

                    if (algorithmParameters is not null)
                    {
                        writer.WriteEncodedValue(algorithmParameters);
                    }
                }

                writer.WriteOctetString(encodedPrivateKey);
            }

            return writer.Encode();
        }
    }
}
