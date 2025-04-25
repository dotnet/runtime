// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using System.Formats.Asn1;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.Asn1;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    internal static class SlhDsaTestHelpers
    {
        /// <summary>
        /// Gets the negation of <see cref="SlhDsa.IsSupported"/>. This can be used to conditionally skip tests.
        /// </summary>
        public static bool IsNotSupported => !SlhDsa.IsSupported;

        public static void VerifyDisposed(SlhDsa slhDsa)
        {
            // A signature-sized buffer can be reused for keys as well
            byte[] tempBuffer = new byte[slhDsa.Algorithm.SignatureSizeInBytes];
            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 32);

            Assert.Throws<ObjectDisposedException>(() => slhDsa.SignData([], tempBuffer, []));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.VerifyData([], tempBuffer, []));

            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKeyPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPublicKey(tempBuffer.AsSpan(..slhDsa.Algorithm.PublicKeySizeInBytes)));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaSecretKey(tempBuffer.AsSpan(..slhDsa.Algorithm.SecretKeySizeInBytes)));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfoPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters, [], out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, pbeParameters, [], out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportPkcs8PrivateKey(tempBuffer, out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportSubjectPublicKeyInfo([], out _));
        }

        public static void AssertImportPublicKey(Action<Func<SlhDsa>> test, SlhDsaAlgorithm algorithm, byte[] publicKey) =>
            AssertImportPublicKey(test, test, algorithm, publicKey);

        public static void AssertImportPublicKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, SlhDsaAlgorithm algorithm, byte[] publicKey)
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
                    Algorithm = AlgorithmToOid(algorithm) ?? throw new XunitException("Cannot create PKCS#8 private key because algorithm is unknown."),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                SubjectPublicKey = publicKey,
            };

            AssertImportSubjectKeyPublicInfo(import => testEmbeddedCall(() => import(spki.Encode())));
        }

        internal delegate SlhDsa ImportSubjectKeyPublicInfoCallback(byte[] spki);
        public static void AssertImportSubjectKeyPublicInfo(Action<ImportSubjectKeyPublicInfoCallback> test) =>
            AssertImportSubjectKeyPublicInfo(test, test);

        public static void AssertImportSubjectKeyPublicInfo(
            Action<ImportSubjectKeyPublicInfoCallback> testDirectCall,
            Action<ImportSubjectKeyPublicInfoCallback> testEmbeddedCall)
        {
            testDirectCall(spki => SlhDsa.ImportSubjectPublicKeyInfo(spki));
            testDirectCall(spki => SlhDsa.ImportSubjectPublicKeyInfo(spki.AsSpan()));

            testEmbeddedCall(spki => SlhDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki)));
            testEmbeddedCall(spki => SlhDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki).AsSpan()));
        }

        public static void AssertImportSecretKey(Action<Func<SlhDsa>> test, SlhDsaAlgorithm algorithm, byte[] secretKey) =>
            AssertImportSecretKey(test, test, algorithm, secretKey);

        public static void AssertImportSecretKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, SlhDsaAlgorithm algorithm, byte[] secretKey)
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
                    Algorithm = AlgorithmToOid(algorithm) ?? throw new XunitException("Cannot create PKCS#8 private key because algorithm is unknown."),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                PrivateKey = secretKey,
            };

            AssertImportPkcs8PrivateKey(import =>
                testEmbeddedCall(() => import(pkcs8.Encode())));
        }

        internal delegate SlhDsa ImportPkcs8PrivateKeyCallback(ReadOnlySpan<byte> pkcs8);
        public static void AssertImportPkcs8PrivateKey(Action<ImportPkcs8PrivateKeyCallback> callback) =>
            AssertImportPkcs8PrivateKey(callback, callback);

        public static void AssertImportPkcs8PrivateKey(
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

        public static void AssertImportFromPem(Action<Func<string, SlhDsa>> callback)
        {
            callback(static (string pem) => SlhDsa.ImportFromPem(pem));
            callback(static (string pem) => SlhDsa.ImportFromPem(pem.AsSpan()));
        }

        public static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> test,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All) =>
            AssertImportEncryptedPkcs8PrivateKey(test, test, passwordTypeToTest);

        internal delegate SlhDsa ImportEncryptedPkcs8PrivateKeyCallback(string password, ReadOnlySpan<byte> pkcs8);
        public static void AssertImportEncryptedPkcs8PrivateKey(
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

            AssertImportFromEncryptedPem(importPem =>
            {
                testEmbeddedCall((string password, ReadOnlySpan<byte> pkcs8) =>
                {
                    string pem = PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", pkcs8);
                    return importPem(pem, password);
                });
            });
        }

        internal delegate SlhDsa ImportFrpmEncryptedPemCallback(string source, string password);
        public static void AssertImportFromEncryptedPem(
            Action<ImportFrpmEncryptedPemCallback> callback,
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

        public static void AssertEncryptedPkcs8PrivateKeyContents(PbeParameters pbeParameters, ReadOnlyMemory<byte> contents)
        {
            EncryptedPrivateKeyInfoAsn epki = EncryptedPrivateKeyInfoAsn.Decode(contents, AsnEncodingRules.BER);
            AlgorithmIdentifierAsn algorithmIdentifier = epki.EncryptionAlgorithm;

            if (pbeParameters.EncryptionAlgorithm == PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
            {
                // pbeWithSHA1And3-KeyTripleDES-CBC
                Assert.Equal("1.2.840.113549.1.12.1.3", algorithmIdentifier.Algorithm);
                PBEParameter pbeParameterAsn = PBEParameter.Decode(algorithmIdentifier.Parameters.Value, AsnEncodingRules.BER);

                Assert.Equal(pbeParameters.IterationCount, pbeParameterAsn.IterationCount);
            }
            else
            {
                Assert.Equal("1.2.840.113549.1.5.13", algorithmIdentifier.Algorithm); // PBES2
                PBES2Params pbes2Params = PBES2Params.Decode(algorithmIdentifier.Parameters.Value, AsnEncodingRules.BER);
                Assert.Equal("1.2.840.113549.1.5.12", pbes2Params.KeyDerivationFunc.Algorithm); // PBKDF2
                Pbkdf2Params pbkdf2Params = Pbkdf2Params.Decode(
                    pbes2Params.KeyDerivationFunc.Parameters.Value,
                    AsnEncodingRules.BER);
                string expectedEncryptionOid = pbeParameters.EncryptionAlgorithm switch
                {
                    PbeEncryptionAlgorithm.Aes128Cbc => "2.16.840.1.101.3.4.1.2",
                    PbeEncryptionAlgorithm.Aes192Cbc => "2.16.840.1.101.3.4.1.22",
                    PbeEncryptionAlgorithm.Aes256Cbc => "2.16.840.1.101.3.4.1.42",
                    _ => throw new CryptographicException(),
                };

                Assert.Equal(pbeParameters.IterationCount, pbkdf2Params.IterationCount);
                Assert.Equal(pbeParameters.HashAlgorithm, GetHashAlgorithmFromPbkdf2Params(pbkdf2Params));
                Assert.Equal(expectedEncryptionOid, pbes2Params.EncryptionScheme.Algorithm);
            }
        }

        public static void AssertExportSlhDsaPublicKey(Action<Func<SlhDsa, byte[]>> callback)
        {
            callback(slhDsa => slhDsa.ExportSlhDsaPublicKey());
            callback(slhDsa =>
            {
                byte[] buffer = new byte[slhDsa.Algorithm.PublicKeySizeInBytes];
                slhDsa.ExportSlhDsaPublicKey(buffer.AsSpan());
                return buffer;
            });

            AssertExportSubjectPublicKeyInfo(exportSpki =>
                callback(slhDsa =>
                    SubjectPublicKeyInfoAsn.Decode(exportSpki(slhDsa), AsnEncodingRules.DER).SubjectPublicKey.Span.ToArray()));
        }

        public static void AssertExportSlhDsaSecretKey(Action<Func<SlhDsa, byte[]>> callback)
        {
            callback(slhDsa => slhDsa.ExportSlhDsaSecretKey());
            callback(slhDsa =>
            {
                byte[] buffer = new byte[slhDsa.Algorithm.SecretKeySizeInBytes];
                slhDsa.ExportSlhDsaSecretKey(buffer.AsSpan());
                return buffer;
            });

            AssertExportPkcs8PrivateKey(exportPkcs8 =>
                callback(slhDsa =>
                    PrivateKeyInfoAsn.Decode(exportPkcs8(slhDsa), AsnEncodingRules.DER).PrivateKey.Span.ToArray()));
        }

        public static void AssertExportPkcs8PrivateKey(SlhDsa slhDsa, Action<byte[]> callback) =>
            AssertExportPkcs8PrivateKey(export => callback(export(slhDsa)));

        public static void AssertExportPkcs8PrivateKey(Action<Func<SlhDsa, byte[]>> callback)
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

        public static void AssertExportSubjectPublicKeyInfo(SlhDsa slhDsa, Action<byte[]> callback) =>
            AssertExportSubjectPublicKeyInfo(export => callback(export(slhDsa)));

        public static void AssertExportSubjectPublicKeyInfo(Action<Func<SlhDsa, byte[]>> callback)
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

        public static void AssertEncryptedExportPkcs8PrivateKey(
            SlhDsa slhDsa,
            string password,
            PbeParameters pbeParameters,
            Action<byte[]> callback) =>
            AssertEncryptedExportPkcs8PrivateKey(export => callback(export(slhDsa, password, pbeParameters)));

        internal delegate byte[] ExportEncryptedPkcs8PrivateKeyCallback(SlhDsa slhDsa, string password, PbeParameters pbeParameters);
        public static void AssertEncryptedExportPkcs8PrivateKey(
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
        public static void AssertExportToEncryptedPem(
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

        public static void AssertExportToPrivateKeyPem(Action<Func<SlhDsa, string>> callback) =>
            callback(slhDsa => slhDsa.ExportPkcs8PrivateKeyPem());

        public static void AssertExportToPublicKeyPem(Action<Func<SlhDsa, string>> callback) =>
            callback(slhDsa => slhDsa.ExportSubjectPublicKeyInfoPem());

        [Flags]
        internal enum EncryptionPasswordType
        {
            Byte = 1,
            Char = 2,
            All = Char | Byte,
        }

        public static EncryptionPasswordType GetValidPasswordTypes(PbeParameters pbeParameters)
            => pbeParameters.EncryptionAlgorithm == PbeEncryptionAlgorithm.TripleDes3KeyPkcs12
            ? EncryptionPasswordType.Char
            : EncryptionPasswordType.All;

        public delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);
        public static byte[] DoTryUntilDone(TryExportFunc func)
        {
            byte[] buffer = new byte[512];
            int written;

            while (!func(buffer, out written))
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            return buffer.AsSpan(0, written).ToArray();
        }

        private static HashAlgorithmName GetHashAlgorithmFromPbkdf2Params(Pbkdf2Params pbkdf2Params)
        {
            return pbkdf2Params.Prf.Algorithm switch
            {
                "1.2.840.113549.2.7" => HashAlgorithmName.SHA1,
                "1.2.840.113549.2.9" => HashAlgorithmName.SHA256,
                "1.2.840.113549.2.10" => HashAlgorithmName.SHA384,
                "1.2.840.113549.2.11" => HashAlgorithmName.SHA512,
                string other => throw new XunitException($"Unknown hash algorithm OID '{other}'."),
            };
        }

        public static string? AlgorithmToOid(SlhDsaAlgorithm algorithm)
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
                _ => null,
            };
        }

        extension(SubjectPublicKeyInfoAsn subjectPublicKeyInfoAsn)
        {
            internal byte[] Encode()
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                subjectPublicKeyInfoAsn.Encode(writer);
                byte[] encoded = writer.Encode();
                return encoded;
            }
        }

        extension(PrivateKeyInfoAsn privateKeyInfoAsn)
        {
            internal byte[] Encode()
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                privateKeyInfoAsn.Encode(writer);
                byte[] encoded = writer.Encode();
                return encoded;
            }
        }
    }
}
