// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Rsa.Tests;
using System.Text;
using Test.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    internal static class CompositeMLDsaTestHelpers
    {
        // DER encoding of ASN.1 BitString "foo"
        internal static readonly ReadOnlyMemory<byte> s_derBitStringFoo = new byte[] { 0x03, 0x04, 0x00, 0x66, 0x6f, 0x6f };

        internal static readonly Dictionary<CompositeMLDsaAlgorithm, MLDsaAlgorithm> MLDsaAlgorithms = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,            MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,         MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithEd25519,               MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,             MLDsaAlgorithm.MLDsa44 },

            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,            MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,         MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,            MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,         MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,             MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,             MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,  MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithEd25519,               MLDsaAlgorithm.MLDsa65 },

            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,             MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,  MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithEd448,                 MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,            MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,            MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,             MLDsaAlgorithm.MLDsa87 },
        };

        internal static void AssertImportPublicKey(Action<Func<CompositeMLDsa>> test, CompositeMLDsaAlgorithm algorithm, byte[] publicKey) =>
            AssertImportPublicKey(test, test, algorithm, publicKey);

        internal static void AssertImportPublicKey(Action<Func<CompositeMLDsa>> testDirectCall, Action<Func<CompositeMLDsa>> testEmbeddedCall, CompositeMLDsaAlgorithm algorithm, byte[] publicKey)
        {
            testDirectCall(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, publicKey));

            if (publicKey?.Length == 0)
            {
                testDirectCall(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, publicKey.AsSpan()));
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

            AssertImportSubjectPublicKeyInfo(import => testEmbeddedCall(() => import(spki.Encode())));
        }

        internal delegate CompositeMLDsa ImportSubjectPublicKeyInfoCallback(byte[] spki);
        internal static void AssertImportSubjectPublicKeyInfo(Action<ImportSubjectPublicKeyInfoCallback> test) =>
            AssertImportSubjectPublicKeyInfo(test, test);

        internal static void AssertImportSubjectPublicKeyInfo(
            Action<ImportSubjectPublicKeyInfoCallback> testDirectCall,
            Action<ImportSubjectPublicKeyInfoCallback> testEmbeddedCall)
        {
            testDirectCall(spki => CompositeMLDsa.ImportSubjectPublicKeyInfo(spki));
            testDirectCall(spki => CompositeMLDsa.ImportSubjectPublicKeyInfo(spki.AsSpan()));

            testEmbeddedCall(spki => CompositeMLDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki)));
            testEmbeddedCall(spki => CompositeMLDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki).AsSpan()));
        }

        internal static void AssertImportPrivateKey(Action<Func<CompositeMLDsa>> test, CompositeMLDsaAlgorithm algorithm, byte[] privateKey) =>
            AssertImportPrivateKey(test, test, algorithm, privateKey);

        internal static void AssertImportPrivateKey(Action<Func<CompositeMLDsa>> testDirectCall, Action<Func<CompositeMLDsa>> testEmbeddedCall, CompositeMLDsaAlgorithm algorithm, byte[] privateKey)
        {
            testDirectCall(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, privateKey));

            if (privateKey?.Length == 0)
            {
                testDirectCall(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, privateKey.AsSpan()));
            }

            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = AlgorithmToOid(algorithm),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                PrivateKey = privateKey,
            };

            AssertImportPkcs8PrivateKey(import => testEmbeddedCall(() => import(pkcs8.Encode())));
        }

        internal delegate CompositeMLDsa ImportPkcs8PrivateKeyCallback(ReadOnlySpan<byte> pkcs8);
        internal static void AssertImportPkcs8PrivateKey(Action<ImportPkcs8PrivateKeyCallback> callback) =>
            AssertImportPkcs8PrivateKey(callback, callback);

        internal static void AssertImportPkcs8PrivateKey(
            Action<ImportPkcs8PrivateKeyCallback> testDirectCall,
            Action<ImportPkcs8PrivateKeyCallback> testEmbeddedCall)
        {
            testDirectCall(pkcs8 => CompositeMLDsa.ImportPkcs8PrivateKey(pkcs8));
            testDirectCall(pkcs8 => CompositeMLDsa.ImportPkcs8PrivateKey(pkcs8.ToArray()));

            AssertImportFromPem(importPem =>
            {
                testEmbeddedCall(pkcs8 => importPem(PemEncoding.WriteString("PRIVATE KEY", pkcs8)));
            });
        }

        internal static void AssertImportFromPem(Action<Func<string, CompositeMLDsa>> callback)
        {
            callback(static (string pem) => CompositeMLDsa.ImportFromPem(pem));
            callback(static (string pem) => CompositeMLDsa.ImportFromPem(pem.AsSpan()));
        }

        internal static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> test,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All) =>
            AssertImportEncryptedPkcs8PrivateKey(test, test, passwordTypeToTest);

        internal delegate CompositeMLDsa ImportEncryptedPkcs8PrivateKeyCallback(string password, ReadOnlySpan<byte> pkcs8);
        internal static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> testDirectCall,
            Action<ImportEncryptedPkcs8PrivateKeyCallback> testEmbeddedCall,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypeToTest & EncryptionPasswordType.Char) != 0)
            {
                testDirectCall((password, pkcs8) => CompositeMLDsa.ImportEncryptedPkcs8PrivateKey(password, pkcs8.ToArray()));
                testDirectCall((password, pkcs8) => CompositeMLDsa.ImportEncryptedPkcs8PrivateKey(password.AsSpan(), pkcs8));
            }

            if ((passwordTypeToTest & EncryptionPasswordType.Byte) != 0)
            {
                testDirectCall((password, pkcs8) =>
                    CompositeMLDsa.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(password), pkcs8.ToArray()));
            }

            AssertImportFromEncryptedPem(importPem =>
            {
                testEmbeddedCall((string password, ReadOnlySpan<byte> pkcs8) =>
                {
                    string pem = PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", pkcs8);
                    return importPem(pem, password);
                });
            }, passwordTypeToTest);
        }

        internal delegate CompositeMLDsa ImportFromEncryptedPemCallback(string source, string password);
        internal static void AssertImportFromEncryptedPem(
            Action<ImportFromEncryptedPemCallback> callback,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypeToTest & EncryptionPasswordType.Char) != 0)
            {
                callback(static (string pem, string password) => CompositeMLDsa.ImportFromEncryptedPem(pem, password));
                callback(static (string pem, string password) => CompositeMLDsa.ImportFromEncryptedPem(pem.AsSpan(), password));
            }

            if ((passwordTypeToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback(static (string pem, string password) =>
                    CompositeMLDsa.ImportFromEncryptedPem(pem, Encoding.UTF8.GetBytes(password)));
                callback(static (string pem, string password) =>
                    CompositeMLDsa.ImportFromEncryptedPem(pem.AsSpan(), Encoding.UTF8.GetBytes(password)));
            }
        }

        internal static void AssertExportPublicKey(Action<Func<CompositeMLDsa, byte[]>> callback)
        {
            callback(dsa =>
            {
                // For simplicity, use a large enough size for all algorithms.
                byte[] buffer = new byte[4096];

                int size = dsa.ExportCompositeMLDsaPublicKey(buffer.AsSpan());
                Array.Resize(ref buffer, size);

                return buffer;
            });

            callback(dsa => dsa.ExportCompositeMLDsaPublicKey());
            callback(dsa => DoTryUntilDone(dsa.TryExportCompositeMLDsaPublicKey));

            AssertExportSubjectPublicKeyInfo(exportSpki =>
                callback(dsa =>
                    SubjectPublicKeyInfoAsn.Decode(exportSpki(dsa), AsnEncodingRules.DER).SubjectPublicKey.ToArray()));
        }

        internal static void AssertExportPrivateKey(Action<Func<CompositeMLDsa, byte[]>> callback) =>
            AssertExportPrivateKey(callback, callback);

        internal static void AssertExportPrivateKey(Action<Func<CompositeMLDsa, byte[]>> directCallback, Action<Func<CompositeMLDsa, byte[]>> indirectCallback)
        {
            directCallback(dsa =>
            {
                // For simplicity, use a large enough size for all algorithms.
                byte[] buffer = new byte[4096];

                int size = dsa.ExportCompositeMLDsaPrivateKey(buffer.AsSpan());
                Array.Resize(ref buffer, size);

                return buffer;
            });

            directCallback(dsa => dsa.ExportCompositeMLDsaPrivateKey());
            directCallback(dsa => DoTryUntilDone(dsa.TryExportCompositeMLDsaPrivateKey));

            AssertExportPkcs8PrivateKey(exportPkcs8 =>
                indirectCallback(dsa =>
                    PrivateKeyInfoAsn.Decode(
                        exportPkcs8(dsa), AsnEncodingRules.DER).PrivateKey.ToArray()));
        }

        internal static void AssertExportPkcs8PrivateKey(CompositeMLDsa dsa, Action<byte[]> callback) =>
            AssertExportPkcs8PrivateKey(export => callback(export(dsa)));

        internal static void AssertExportPkcs8PrivateKey(Action<Func<CompositeMLDsa, byte[]>> callback)
        {
            callback(dsa => DoTryUntilDone(dsa.TryExportPkcs8PrivateKey));
            callback(dsa => dsa.ExportPkcs8PrivateKey());
            callback(dsa => DecodePem(dsa.ExportPkcs8PrivateKeyPem()));

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

        internal static void AssertExportSubjectPublicKeyInfo(CompositeMLDsa dsa, Action<byte[]> callback) =>
            AssertExportSubjectPublicKeyInfo(export => callback(export(dsa)));

        internal static void AssertExportSubjectPublicKeyInfo(Action<Func<CompositeMLDsa, byte[]>> callback)
        {
            callback(dsa => DoTryUntilDone(dsa.TryExportSubjectPublicKeyInfo));
            callback(dsa => dsa.ExportSubjectPublicKeyInfo());
            callback(dsa => DecodePem(dsa.ExportSubjectPublicKeyInfoPem()));

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
            CompositeMLDsa dsa,
            string password,
            PbeParameters pbeParameters,
            Action<byte[]> callback) =>
            AssertEncryptedExportPkcs8PrivateKey(export => callback(export(dsa, password, pbeParameters)));

        internal delegate byte[] ExportEncryptedPkcs8PrivateKeyCallback(CompositeMLDsa dsa, string password, PbeParameters pbeParameters);
        internal static void AssertEncryptedExportPkcs8PrivateKey(
            Action<ExportEncryptedPkcs8PrivateKeyCallback> callback,
            EncryptionPasswordType passwordTypesToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypesToTest & EncryptionPasswordType.Char) != 0)
            {
                callback((dsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        dsa.TryExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters, destination, out bytesWritten)));
                callback((dsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        dsa.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten)));

                callback((dsa, password, pbeParameters) => dsa.ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters));
                callback((dsa, password, pbeParameters) => dsa.ExportEncryptedPkcs8PrivateKey(password, pbeParameters));

                callback((dsa, password, pbeParameters) => DecodePem(dsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters)));
                callback((dsa, password, pbeParameters) => DecodePem(dsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters)));
            }

            if ((passwordTypesToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback((dsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        dsa.TryExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters, destination, out bytesWritten)));

                callback((dsa, password, pbeParameters) =>
                    dsa.ExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters));

                callback((dsa, password, pbeParameters) =>
                    DecodePem(dsa.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters)));
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

        internal delegate string ExportToPemCallback(CompositeMLDsa dsa, string password, PbeParameters pbeParameters);
        internal static void AssertExportToEncryptedPem(
            Action<ExportToPemCallback> callback,
            EncryptionPasswordType passwordTypesToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypesToTest & EncryptionPasswordType.Char) != 0)
            {
                callback((dsa, password, pbeParameters) =>
                    dsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters));
                callback((dsa, password, pbeParameters) =>
                    dsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters));
            }

            if ((passwordTypesToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback((dsa, password, pbeParameters) =>
                    dsa.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters));
            }
        }

        internal static void AssertExportToPrivateKeyPem(Action<Func<CompositeMLDsa, string>> callback) =>
            callback(dsa => dsa.ExportPkcs8PrivateKeyPem());

        internal static void AssertExportToPublicKeyPem(Action<Func<CompositeMLDsa, string>> callback) =>
            callback(dsa => dsa.ExportSubjectPublicKeyInfoPem());

        internal class RsaAlgorithm(int keySizeInBits)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;
        }

        internal class ECDsaAlgorithm(int keySizeInBits, bool isSec)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;
            internal bool IsSec { get; } = isSec;
        }

        internal class EdDsaAlgorithm(int keySizeInBits)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;
        }

        internal static void ExecuteComponentAction(
            CompositeMLDsaAlgorithm algo,
            Action<RsaAlgorithm> rsaFunc,
            Action<ECDsaAlgorithm> ecdsaFunc,
            Action<EdDsaAlgorithm> eddsaFunc)
        {
            ExecuteComponentFunc(
                algo,
                info => { rsaFunc(info); return true; },
                info => { ecdsaFunc(info); return true; },
                info => { eddsaFunc(info); return true; });
        }

        internal static T ExecuteComponentFunc<T>(
            CompositeMLDsaAlgorithm algo,
            Func<RsaAlgorithm, T> rsaFunc,
            Func<ECDsaAlgorithm, T> ecdsaFunc,
            Func<EdDsaAlgorithm, T> eddsaFunc)
        {
            if (algo == CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15 ||
                algo == CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss)
            {
                return rsaFunc(new RsaAlgorithm(2048));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss ||
                     algo == CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss)
            {
                return rsaFunc(new RsaAlgorithm(3072));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss ||
                     algo == CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss)
            {
                return rsaFunc(new RsaAlgorithm(4096));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256)
            {
                return ecdsaFunc(new ECDsaAlgorithm(256, isSec: true));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1)
            {
                return ecdsaFunc(new ECDsaAlgorithm(256, isSec: false));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384)
            {
                return ecdsaFunc(new ECDsaAlgorithm(384, isSec: true));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1)
            {
                return ecdsaFunc(new ECDsaAlgorithm(384, isSec: false));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521)
            {
                return ecdsaFunc(new ECDsaAlgorithm(521, isSec: true));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa44WithEd25519 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa65WithEd25519)
            {
                return eddsaFunc(new EdDsaAlgorithm(256));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa87WithEd448)
            {
                return eddsaFunc(new EdDsaAlgorithm(456));
            }
            else
            {
                throw new XunitException($"Unsupported algorithm: {algo}");
            }
        }

        internal static int ExpectedPublicKeySizeLowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            return MLDsaAlgorithms[algorithm].PublicKeySizeInBytes +
                ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8,
                    ecdsa => 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);
        }

        internal static int ExpectedPublicKeySizeUpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            return MLDsaAlgorithms[algorithm].PublicKeySizeInBytes +
                ExecuteComponentFunc(
                    algorithm,
                    rsa => (rsa.KeySizeInBits / 8) + 52, // Add max ASN.1 overhead
                    ecdsa => 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);
        }

        internal static int ExpectedPrivateKeySizeLowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            return MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes +
                ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8,
                    ecdsa =>
                        // ECPrivateKey size with parameters and without public key.
                        // These are derived using the size table in the spec (Table 4).
                        algorithm.Name switch
                        {
                            "MLDSA44-ECDSA-P256-SHA256" or
                            "MLDSA65-ECDSA-P256-SHA512" =>
                                51,
                            "MLDSA65-ECDSA-P384-SHA512" or
                            "MLDSA87-ECDSA-P384-SHA512" =>
                                64,
                            "MLDSA87-ECDSA-P521-SHA512" =>
                                82,
                            "MLDSA65-ECDSA-brainpoolP256r1-SHA512" =>
                                52,
                            "MLDSA87-ECDSA-brainpoolP384r1-SHA512" =>
                                68,
                            _ =>
                                throw new XunitException($"Unknown algorithm {algorithm.Name}."),
                        },
                    eddsa => eddsa.KeySizeInBits / 8);
        }

        internal static int ExpectedPrivateKeySizeUpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            return MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes +
                ExecuteComponentFunc(
                    algorithm,
                    rsa => (rsa.KeySizeInBits / 8) * 9 / 2 + 101, // Add max ASN.1 overhead
                    ecdsa =>
                        // ECPrivateKey size with parameters and without public key.
                        // These are derived using the size table in the spec (Table 4).
                        algorithm.Name switch
                        {
                            "MLDSA44-ECDSA-P256-SHA256" or
                            "MLDSA65-ECDSA-P256-SHA512" =>
                                51,
                            "MLDSA65-ECDSA-P384-SHA512" or
                            "MLDSA87-ECDSA-P384-SHA512" =>
                                64,
                            "MLDSA87-ECDSA-P521-SHA512" =>
                                82,
                            "MLDSA65-ECDSA-brainpoolP256r1-SHA512" =>
                                52,
                            "MLDSA87-ECDSA-brainpoolP384r1-SHA512" =>
                                68,
                            _ =>
                                throw new XunitException($"Unknown algorithm {algorithm.Name}."),
                        },
                    eddsa => eddsa.KeySizeInBits / 8);
        }

        internal static bool IsECDsa(CompositeMLDsaAlgorithm algorithm) => ExecuteComponentFunc(algorithm, rsa => false, ecdsa => true, eddsa => false);

        internal static void WithDispose<T>(T disposable, Action<T> callback)
            where T : IDisposable
        {
            using (disposable)
            {
                callback(disposable);
            }
        }

        internal static void AssertPublicKeyEquals(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
        {
            AssertExtensions.SequenceEqual(expected, actual);
        }

        internal static void AssertPrivateKeyEquals(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
        {
            ReadOnlySpan<byte> expectedMLDsaKey = expected.Slice(0, MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes);
            ReadOnlySpan<byte> actualMLDsaKey = actual.Slice(0, MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes);

            AssertExtensions.SequenceEqual(expectedMLDsaKey, actualMLDsaKey);

            byte[] expectedTradKey = expected.Slice(expectedMLDsaKey.Length).ToArray();
            byte[] actualTradKey = actual.Slice(actualMLDsaKey.Length).ToArray();

            ExecuteComponentAction(
                algorithm,
                _ =>
                {
                    RSAParameters expectedRsaParameters = RSAParametersFromRawPrivateKey(expectedTradKey);
                    RSAParameters actualRsaParameters = RSAParametersFromRawPrivateKey(actualTradKey);

                    RSATestHelpers.AssertKeyEquals(expectedRsaParameters, actualRsaParameters);
                },
                _ => Assert.Equal(expectedTradKey, actualTradKey),
                _ => Assert.Equal(expectedTradKey, actualTradKey));
        }

        private static RSAParameters RSAParametersFromRawPrivateKey(ReadOnlySpan<byte> key)
        {
            RSAParameters parameters = default;

            AsnValueReader reader = new AsnValueReader(key, AsnEncodingRules.BER);
            AsnValueReader sequenceReader = reader.ReadSequence(Asn1Tag.Sequence);

            if (!sequenceReader.TryReadInt32(out int version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            const int MaxSupportedVersion = 0;

            if (version > MaxSupportedVersion)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_RSAPrivateKey_VersionTooNew,
                        version,
                        MaxSupportedVersion));
            }

            parameters.Modulus = sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes();

            int modulusLength = parameters.Modulus.Length;
            int halfModulusLength = modulusLength / 2;

            if (parameters.Modulus.Length != modulusLength)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
            }

            parameters.Exponent = sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes();

            // We're not pinning and clearing the arrays here because this is a test helper.
            // In production code, you should always pin and clear sensitive data.
            parameters.D = new byte[modulusLength];
            parameters.P = new byte[halfModulusLength];
            parameters.Q = new byte[halfModulusLength];
            parameters.DP = new byte[halfModulusLength];
            parameters.DQ = new byte[halfModulusLength];
            parameters.InverseQ = new byte[halfModulusLength];

            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.D);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.P);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.Q);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.DP);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.DQ);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.InverseQ);

            sequenceReader.ThrowIfNotEmpty();
            reader.ThrowIfNotEmpty();

            return parameters;
        }

        private static byte[] ToUnsignedIntegerBytes(this ReadOnlySpan<byte> span)
        {
            if (span.Length > 1 && span[0] == 0)
            {
                return span.Slice(1).ToArray();
            }

            return span.ToArray();
        }

        private static void ToUnsignedIntegerBytes(this ReadOnlySpan<byte> span, Span<byte> destination)
        {
            int length = destination.Length;

            if (span.Length == length)
            {
                span.CopyTo(destination);
                return;
            }

            if (span.Length == length + 1)
            {
                if (span[0] == 0)
                {
                    span.Slice(1).CopyTo(destination);
                    return;
                }
            }

            if (span.Length > length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            destination.Slice(0, destination.Length - span.Length).Clear();
            span.CopyTo(destination.Slice(length - span.Length));
        }

        internal static void VerifyDisposed(CompositeMLDsa dsa)
        {
            // A signature-sized buffer can be reused for keys as well
            byte[] tempBuffer = new byte[dsa.Algorithm.MaxSignatureSizeInBytes];

            Assert.Throws<ObjectDisposedException>(() => dsa.SignData([], tempBuffer, []));
            Assert.Throws<ObjectDisposedException>(() => dsa.SignData([]));
            Assert.Throws<ObjectDisposedException>(() => dsa.VerifyData(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => dsa.VerifyData(Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>()));

            Assert.Throws<ObjectDisposedException>(() => dsa.TryExportCompositeMLDsaPrivateKey([], out _));
            Assert.Throws<ObjectDisposedException>(() => dsa.ExportCompositeMLDsaPrivateKey());
            Assert.Throws<ObjectDisposedException>(() => dsa.TryExportCompositeMLDsaPublicKey([], out _));
            Assert.Throws<ObjectDisposedException>(() => dsa.ExportCompositeMLDsaPublicKey());
        }

        internal static string? AlgorithmToOid(CompositeMLDsaAlgorithm algorithm)
        {
            return algorithm?.Name switch
            {
                "MLDSA44-RSA2048-PSS-SHA256" => "2.16.840.1.114027.80.9.1.20",
                "MLDSA44-RSA2048-PKCS15-SHA256" => "2.16.840.1.114027.80.9.1.21",
                "MLDSA44-Ed25519-SHA512" => "2.16.840.1.114027.80.9.1.22",
                "MLDSA44-ECDSA-P256-SHA256" => "2.16.840.1.114027.80.9.1.23",
                "MLDSA65-RSA3072-PSS-SHA512" => "2.16.840.1.114027.80.9.1.24",
                "MLDSA65-RSA3072-PKCS15-SHA512" => "2.16.840.1.114027.80.9.1.25",
                "MLDSA65-RSA4096-PSS-SHA512" => "2.16.840.1.114027.80.9.1.26",
                "MLDSA65-RSA4096-PKCS15-SHA512" => "2.16.840.1.114027.80.9.1.27",
                "MLDSA65-ECDSA-P256-SHA512" => "2.16.840.1.114027.80.9.1.28",
                "MLDSA65-ECDSA-P384-SHA512" => "2.16.840.1.114027.80.9.1.29",
                "MLDSA65-ECDSA-brainpoolP256r1-SHA512" => "2.16.840.1.114027.80.9.1.30",
                "MLDSA65-Ed25519-SHA512" => "2.16.840.1.114027.80.9.1.31",
                "MLDSA87-ECDSA-P384-SHA512" => "2.16.840.1.114027.80.9.1.32",
                "MLDSA87-ECDSA-brainpoolP384r1-SHA512" => "2.16.840.1.114027.80.9.1.33",
                "MLDSA87-Ed448-SHAKE256" => "2.16.840.1.114027.80.9.1.34",
                "MLDSA87-RSA3072-PSS-SHA512" => "2.16.840.1.114027.80.9.1.35",
                "MLDSA87-RSA4096-PSS-SHA512" => "2.16.840.1.114027.80.9.1.36",
                "MLDSA87-ECDSA-P521-SHA512" => "2.16.840.1.114027.80.9.1.37",

                _ => throw new XunitException("Unknown algorithm."),
            };
        }

        private delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);
        private static byte[] DoTryUntilDone(TryExportFunc func)
        {
            byte[] buffer = new byte[512];
            int written;

            while (!func(buffer, out written))
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            return buffer.AsSpan(0, written).ToArray();
        }

        internal static EncryptionPasswordType GetValidPasswordTypes(PbeParameters pbeParameters)
            => pbeParameters.EncryptionAlgorithm == PbeEncryptionAlgorithm.TripleDes3KeyPkcs12
            ? EncryptionPasswordType.Char
            : EncryptionPasswordType.All;

        [Flags]
        internal enum EncryptionPasswordType
        {
            Byte = 1,
            Char = 2,
            All = Char | Byte,
        }
    }
}
