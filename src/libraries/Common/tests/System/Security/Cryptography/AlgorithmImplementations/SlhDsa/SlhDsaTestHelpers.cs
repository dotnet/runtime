// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Text;
using Test.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    internal static class SlhDsaTestHelpers
    {
        public static bool SlhDsaIsNotSupported => !SlhDsa.IsSupported;

        // DER encoding of ASN.1 BitString "foo"
        internal static readonly ReadOnlyMemory<byte> s_derBitStringFoo = new byte[] { 0x03, 0x04, 0x00, 0x66, 0x6f, 0x6f };

        internal static void VerifyDisposed(SlhDsa slhDsa)
        {
            // A signature-sized buffer can be reused for keys as well
            byte[] tempBuffer = new byte[slhDsa.Algorithm.SignatureSizeInBytes];
            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 32);

            Assert.Throws<ObjectDisposedException>(() => slhDsa.SignData(ReadOnlySpan<byte>.Empty, tempBuffer.AsSpan(), ReadOnlySpan<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.SignData(Array.Empty<byte>(), Array.Empty<byte>()));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, tempBuffer.AsSpan(), ReadOnlySpan<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.VerifyData(Array.Empty<byte>(), tempBuffer, Array.Empty<byte>()));

            Assert.Throws<ObjectDisposedException>(() => slhDsa.SignPreHash(ReadOnlySpan<byte>.Empty, tempBuffer.AsSpan(), "1.0", ReadOnlySpan<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.SignPreHash(Array.Empty<byte>(), "1.0"));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.VerifyPreHash(ReadOnlySpan<byte>.Empty, tempBuffer.AsSpan(), "1.0", ReadOnlySpan<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.VerifyPreHash(Array.Empty<byte>(), tempBuffer.AsSpan(), "1.0", Array.Empty<byte>()));

            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKeyPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPublicKey(tempBuffer.AsSpan(0, slhDsa.Algorithm.PublicKeySizeInBytes)));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaSecretKey(tempBuffer.AsSpan(0, slhDsa.Algorithm.SecretKeySizeInBytes)));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfoPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters, [], out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, pbeParameters, [], out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportPkcs8PrivateKey(tempBuffer, out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportSubjectPublicKeyInfo([], out _));
        }

        internal static void AssertImportPublicKey(Action<Func<SlhDsa>> test, SlhDsaAlgorithm algorithm, byte[] publicKey) =>
            AssertImportPublicKey(test, test, algorithm, publicKey);

        internal static void AssertImportPublicKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, SlhDsaAlgorithm algorithm, byte[] publicKey)
        {
            testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, publicKey));

            if (publicKey?.Length == 0)
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, publicKey.AsSpan()));
            }

            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = AlgorithmToOid(algorithm),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                SubjectPublicKey = publicKey,
            };

            AssertImportSubjectKeyPublicInfo(import => testEmbeddedCall(() => import(spki.Encode())));
        }

        internal delegate SlhDsa ImportSubjectKeyPublicInfoCallback(byte[] spki);
        internal static void AssertImportSubjectKeyPublicInfo(Action<ImportSubjectKeyPublicInfoCallback> test) =>
            AssertImportSubjectKeyPublicInfo(test, test);

        internal static void AssertImportSubjectKeyPublicInfo(
            Action<ImportSubjectKeyPublicInfoCallback> testDirectCall,
            Action<ImportSubjectKeyPublicInfoCallback> testEmbeddedCall)
        {
            testDirectCall(spki => SlhDsa.ImportSubjectPublicKeyInfo(spki));
            testDirectCall(spki => SlhDsa.ImportSubjectPublicKeyInfo(spki.AsSpan()));

            testEmbeddedCall(spki => SlhDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki)));
            testEmbeddedCall(spki => SlhDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki).AsSpan()));
        }

        internal static void AssertImportSecretKey(Action<Func<SlhDsa>> test, SlhDsaAlgorithm algorithm, byte[] secretKey) =>
            AssertImportSecretKey(test, test, algorithm, secretKey);

        internal static void AssertImportSecretKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, SlhDsaAlgorithm algorithm, byte[] secretKey)
        {
            testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, secretKey));

            if (secretKey?.Length == 0)
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, secretKey.AsSpan()));
            }

            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = AlgorithmToOid(algorithm),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                PrivateKey = secretKey,
            };

            AssertImportPkcs8PrivateKey(import =>
                testEmbeddedCall(() => import(pkcs8.Encode())));
        }

        internal delegate SlhDsa ImportPkcs8PrivateKeyCallback(ReadOnlySpan<byte> pkcs8);
        internal static void AssertImportPkcs8PrivateKey(Action<ImportPkcs8PrivateKeyCallback> callback) =>
            AssertImportPkcs8PrivateKey(callback, callback);

        internal static void AssertImportPkcs8PrivateKey(
            Action<ImportPkcs8PrivateKeyCallback> testDirectCall,
            Action<ImportPkcs8PrivateKeyCallback> testEmbeddedCall)
        {
            testDirectCall(pkcs8 => SlhDsa.ImportPkcs8PrivateKey(pkcs8));
            testDirectCall(pkcs8 => SlhDsa.ImportPkcs8PrivateKey(pkcs8.ToArray()));

            AssertImportFromPem(importPem =>
            {
                testEmbeddedCall(pkcs8 => importPem(PemEncoding.WriteString("PRIVATE KEY", pkcs8)));
            });
        }

        internal static void AssertImportFromPem(Action<Func<string, SlhDsa>> callback)
        {
            callback(static (string pem) => SlhDsa.ImportFromPem(pem));
            callback(static (string pem) => SlhDsa.ImportFromPem(pem.AsSpan()));
        }

        internal static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> test,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All) =>
            AssertImportEncryptedPkcs8PrivateKey(test, test, passwordTypeToTest);

        internal delegate SlhDsa ImportEncryptedPkcs8PrivateKeyCallback(string password, ReadOnlySpan<byte> pkcs8);
        internal static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> testDirectCall,
            Action<ImportEncryptedPkcs8PrivateKeyCallback> testEmbeddedCall,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypeToTest & EncryptionPasswordType.Char) != 0)
            {
                testDirectCall((password, pkcs8) => SlhDsa.ImportEncryptedPkcs8PrivateKey(password, pkcs8.ToArray()));
                testDirectCall((password, pkcs8) => SlhDsa.ImportEncryptedPkcs8PrivateKey(password.AsSpan(), pkcs8));
            }

            if ((passwordTypeToTest & EncryptionPasswordType.Byte) != 0)
            {
                testDirectCall((password, pkcs8) =>
                    SlhDsa.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(password), pkcs8.ToArray()));
            }

            AssertImportFromEncryptedPem(
                importPem =>
                {
                    testEmbeddedCall(
                        (string password, ReadOnlySpan<byte> pkcs8) =>
                        {
                            string pem = PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", pkcs8);
                            return importPem(pem, password);
                        });
                },
                passwordTypeToTest);
        }

        internal delegate SlhDsa ImportFromEncryptedPemCallback(string source, string password);
        internal static void AssertImportFromEncryptedPem(
            Action<ImportFromEncryptedPemCallback> callback,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypeToTest & EncryptionPasswordType.Char) != 0)
            {
                callback(static (string pem, string password) => SlhDsa.ImportFromEncryptedPem(pem, password));
                callback(static (string pem, string password) => SlhDsa.ImportFromEncryptedPem(pem.AsSpan(), password));
            }

            if ((passwordTypeToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback(static (string pem, string password) =>
                    SlhDsa.ImportFromEncryptedPem(pem, Encoding.UTF8.GetBytes(password)));
                callback(static (string pem, string password) =>
                    SlhDsa.ImportFromEncryptedPem(pem.AsSpan(), Encoding.UTF8.GetBytes(password)));
            }
        }

        internal static void AssertExportSlhDsaPublicKey(Action<Func<SlhDsa, byte[]>> callback)
        {
            callback(slhDsa => slhDsa.ExportSlhDsaPublicKey());
            callback(
                slhDsa =>
                {
                    byte[] buffer = new byte[slhDsa.Algorithm.PublicKeySizeInBytes];
                    slhDsa.ExportSlhDsaPublicKey(buffer.AsSpan());
                    return buffer;
                });

            AssertExportSubjectPublicKeyInfo(exportSpki =>
                callback(slhDsa =>
                    SubjectPublicKeyInfoAsn.Decode(exportSpki(slhDsa), AsnEncodingRules.DER).SubjectPublicKey.Span.ToArray()));
        }

        internal static void AssertExportSlhDsaSecretKey(Action<Func<SlhDsa, byte[]>> callback)
        {
            callback(slhDsa => slhDsa.ExportSlhDsaSecretKey());
            callback(
                slhDsa =>
                {
                    byte[] buffer = new byte[slhDsa.Algorithm.SecretKeySizeInBytes];
                    slhDsa.ExportSlhDsaSecretKey(buffer.AsSpan());
                    return buffer;
                });

            AssertExportPkcs8PrivateKey(exportPkcs8 =>
                callback(slhDsa =>
                    PrivateKeyInfoAsn.Decode(exportPkcs8(slhDsa), AsnEncodingRules.DER).PrivateKey.Span.ToArray()));
        }

        internal static void AssertExportPkcs8PrivateKey(SlhDsa slhDsa, Action<byte[]> callback) =>
            AssertExportPkcs8PrivateKey(export => callback(export(slhDsa)));

        internal static void AssertExportPkcs8PrivateKey(Action<Func<SlhDsa, byte[]>> callback)
        {
            callback(slhDsa => DoTryUntilDone(slhDsa.TryExportPkcs8PrivateKey));
            callback(slhDsa => slhDsa.ExportPkcs8PrivateKey());
            callback(slhDsa => DecodePem(slhDsa.ExportPkcs8PrivateKeyPem()));

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

        internal static void AssertExportSubjectPublicKeyInfo(SlhDsa slhDsa, Action<byte[]> callback) =>
            AssertExportSubjectPublicKeyInfo(export => callback(export(slhDsa)));

        internal static void AssertExportSubjectPublicKeyInfo(Action<Func<SlhDsa, byte[]>> callback)
        {
            callback(slhDsa => DoTryUntilDone(slhDsa.TryExportSubjectPublicKeyInfo));
            callback(slhDsa => slhDsa.ExportSubjectPublicKeyInfo());
            callback(slhDsa => DecodePem(slhDsa.ExportSubjectPublicKeyInfoPem()));

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("PUBLIC KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

        internal static void AssertEncryptedExportPkcs8PrivateKey(
            SlhDsa slhDsa,
            string password,
            PbeParameters pbeParameters,
            Action<byte[]> callback) =>
            AssertEncryptedExportPkcs8PrivateKey(export => callback(export(slhDsa, password, pbeParameters)));

        internal delegate byte[] ExportEncryptedPkcs8PrivateKeyCallback(SlhDsa slhDsa, string password, PbeParameters pbeParameters);
        internal static void AssertEncryptedExportPkcs8PrivateKey(
            Action<ExportEncryptedPkcs8PrivateKeyCallback> callback,
            EncryptionPasswordType passwordTypesToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypesToTest & EncryptionPasswordType.Char) != 0)
            {
                callback((slhDsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        slhDsa.TryExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters, destination, out bytesWritten)));
                callback((slhDsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        slhDsa.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten)));

                callback((slhDsa, password, pbeParameters) => slhDsa.ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters));
                callback((slhDsa, password, pbeParameters) => slhDsa.ExportEncryptedPkcs8PrivateKey(password, pbeParameters));

                callback((slhDsa, password, pbeParameters) => DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters)));
                callback((slhDsa, password, pbeParameters) => DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters)));
            }

            if ((passwordTypesToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback((slhDsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        slhDsa.TryExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters, destination, out bytesWritten)));

                callback((slhDsa, password, pbeParameters) =>
                    slhDsa.ExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters));

                callback((slhDsa, password, pbeParameters) =>
                    DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters)));
            }

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("ENCRYPTED PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

        internal delegate string ExportToPemCallback(SlhDsa slhDsa, string password, PbeParameters pbeParameters);
        internal static void AssertExportToEncryptedPem(
            Action<ExportToPemCallback> callback,
            EncryptionPasswordType passwordTypesToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypesToTest & EncryptionPasswordType.Char) != 0)
            {
                callback((slhDsa, password, pbeParameters) =>
                    slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters));
                callback((slhDsa, password, pbeParameters) =>
                    slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters));
            }

            if ((passwordTypesToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback((slhDsa, password, pbeParameters) =>
                    slhDsa.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters));
            }
        }

        internal static void AssertExportToPrivateKeyPem(Action<Func<SlhDsa, string>> callback) =>
            callback(slhDsa => slhDsa.ExportPkcs8PrivateKeyPem());

        internal static void AssertExportToPublicKeyPem(Action<Func<SlhDsa, string>> callback) =>
            callback(slhDsa => slhDsa.ExportSubjectPublicKeyInfoPem());

        [Flags]
        internal enum EncryptionPasswordType
        {
            Byte = 1,
            Char = 2,
            All = Char | Byte,
        }

        internal static EncryptionPasswordType GetValidPasswordTypes(PbeParameters pbeParameters)
            => pbeParameters.EncryptionAlgorithm == PbeEncryptionAlgorithm.TripleDes3KeyPkcs12
            ? EncryptionPasswordType.Char
            : EncryptionPasswordType.All;

        internal delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);
        internal static byte[] DoTryUntilDone(TryExportFunc func)
        {
            byte[] buffer = new byte[512];
            int written;

            while (!func(buffer, out written))
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            return buffer.AsSpan(0, written).ToArray();
        }

        internal static void WithDispose<T>(T disposable, Action<T> callback)
            where T : IDisposable
        {
            using (disposable)
            {
                callback(disposable);
            }
        }

        internal static string AlgorithmToOid(SlhDsaAlgorithm algorithm)
        {
            return algorithm?.Name switch
            {
                "SLH-DSA-SHA2-128s" => "2.16.840.1.101.3.4.3.20",
                "SLH-DSA-SHA2-128f" => "2.16.840.1.101.3.4.3.21",
                "SLH-DSA-SHA2-192s" => "2.16.840.1.101.3.4.3.22",
                "SLH-DSA-SHA2-192f" => "2.16.840.1.101.3.4.3.23",
                "SLH-DSA-SHA2-256s" => "2.16.840.1.101.3.4.3.24",
                "SLH-DSA-SHA2-256f" => "2.16.840.1.101.3.4.3.25",
                "SLH-DSA-SHAKE-128s" => "2.16.840.1.101.3.4.3.26",
                "SLH-DSA-SHAKE-128f" => "2.16.840.1.101.3.4.3.27",
                "SLH-DSA-SHAKE-192s" => "2.16.840.1.101.3.4.3.28",
                "SLH-DSA-SHAKE-192f" => "2.16.840.1.101.3.4.3.29",
                "SLH-DSA-SHAKE-256s" => "2.16.840.1.101.3.4.3.30",
                "SLH-DSA-SHAKE-256f" => "2.16.840.1.101.3.4.3.31",
                _ => throw new XunitException($"Unknown algorithm: '{algorithm?.Name}'."),
            };
        }

        internal const string Md5Oid = "1.2.840.113549.2.5";
        internal const string Sha1Oid = "1.3.14.3.2.26";
        internal const string Sha256Oid = "2.16.840.1.101.3.4.2.1";
        internal const string Sha384Oid = "2.16.840.1.101.3.4.2.2";
        internal const string Sha512Oid = "2.16.840.1.101.3.4.2.3";
        internal const string Sha3_256Oid = "2.16.840.1.101.3.4.2.8";
        internal const string Sha3_384Oid = "2.16.840.1.101.3.4.2.9";
        internal const string Sha3_512Oid = "2.16.840.1.101.3.4.2.10";
        internal const string Shake128Oid = "2.16.840.1.101.3.4.2.11";
        internal const string Shake256Oid = "2.16.840.1.101.3.4.2.12";
    }
}
