// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Text;
using Test.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    internal static partial class MLDsaTestHelpers
    {
        internal static bool MLDsaIsNotSupported => !MLDsa.IsSupported;

        // TODO (https://github.com/dotnet/runtime/issues/118609): Windows currently does not support PKCS#8 export when imported as private key.
        internal static bool SupportsExportingPrivateKeyPkcs8 => MLDsa.IsSupported && !PlatformDetection.IsWindows;

        internal static bool ExternalMuIsSupported => MLDsa.IsSupported && !PlatformDetection.IsWindows;

        // DER encoding of ASN.1 BitString "foo"
        internal static readonly ReadOnlyMemory<byte> s_derBitStringFoo = new byte[] { 0x03, 0x04, 0x00, 0x66, 0x6f, 0x6f };

        private const int NTE_NOT_SUPPORTED = unchecked((int)0x80090029);

        internal static void VerifyDisposed(MLDsa mldsa)
        {
            PbeParameters pbeParams = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 10);
            byte[] signature = new byte[mldsa.Algorithm.SignatureSizeInBytes];
            byte[] bigBuffer = new byte[10000];
            byte[] mu = new byte[64];

            Assert.Throws<ObjectDisposedException>(() => mldsa.SignData(Array.Empty<byte>()));
            Assert.Throws<ObjectDisposedException>(() => mldsa.SignData(ReadOnlySpan<byte>.Empty, signature));
            Assert.Throws<ObjectDisposedException>(() => mldsa.VerifyData(Array.Empty<byte>(), signature));
            Assert.Throws<ObjectDisposedException>(() => mldsa.VerifyData(ReadOnlySpan<byte>.Empty, signature));
            Assert.Throws<ObjectDisposedException>(() => mldsa.SignPreHash(mu, HashInfo.Sha512.Oid));
            Assert.Throws<ObjectDisposedException>(() => mldsa.SignPreHash(mu, signature, HashInfo.Sha512.Oid));
            Assert.Throws<ObjectDisposedException>(() => mldsa.VerifyPreHash(mu, signature, HashInfo.Sha512.Oid));
            Assert.Throws<ObjectDisposedException>(() => mldsa.VerifyPreHash(new ReadOnlySpan<byte>(mu), signature, HashInfo.Sha512.Oid));
            Assert.Throws<ObjectDisposedException>(() => mldsa.SignMu(mu));
            Assert.Throws<ObjectDisposedException>(() => mldsa.SignMu(new ReadOnlySpan<byte>(mu)));
            Assert.Throws<ObjectDisposedException>(() => mldsa.VerifyMu(mu, signature));
            Assert.Throws<ObjectDisposedException>(() => mldsa.VerifyMu(new ReadOnlySpan<byte>(mu), signature));

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaPrivateSeed());
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaPrivateSeed(new byte[mldsa.Algorithm.PrivateSeedSizeInBytes]));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaPublicKey());
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaPublicKey(new byte[mldsa.Algorithm.PublicKeySizeInBytes]));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaPrivateKey());
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportMLDsaPrivateKey(new byte[mldsa.Algorithm.PrivateKeySizeInBytes]));

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => mldsa.TryExportPkcs8PrivateKey(bigBuffer, out _));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportPkcs8PrivateKeyPem());

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportEncryptedPkcs8PrivateKey([1, 2, 3], pbeParams));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportEncryptedPkcs8PrivateKey("123", pbeParams));
            Assert.Throws<ObjectDisposedException>(() => mldsa.TryExportEncryptedPkcs8PrivateKey([1, 2, 3], pbeParams, bigBuffer, out _));
            Assert.Throws<ObjectDisposedException>(() => mldsa.TryExportEncryptedPkcs8PrivateKey("123", pbeParams, bigBuffer, out _));

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportEncryptedPkcs8PrivateKeyPem([1, 2, 3], pbeParams));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportEncryptedPkcs8PrivateKeyPem("123", pbeParams));

            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => mldsa.TryExportSubjectPublicKeyInfo(bigBuffer, out _));
            Assert.Throws<ObjectDisposedException>(() => mldsa.ExportSubjectPublicKeyInfoPem());

            // Doesn't throw:
            Assert.NotNull(mldsa.Algorithm);
        }

        internal static void AssertImportPublicKey(Action<Func<MLDsa>> test, MLDsaAlgorithm algorithm, byte[] publicKey) =>
            AssertImportPublicKey(test, test, algorithm, publicKey);

        internal static void AssertImportPublicKey(Action<Func<MLDsa>> testDirectCall, Action<Func<MLDsa>> testEmbeddedCall, MLDsaAlgorithm algorithm, byte[] publicKey)
        {
            testDirectCall(() => MLDsa.ImportMLDsaPublicKey(algorithm, publicKey));

            if (publicKey?.Length == 0)
            {
                testDirectCall(() => MLDsa.ImportMLDsaPublicKey(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => MLDsa.ImportMLDsaPublicKey(algorithm, ReadOnlySpan<byte>.Empty));
                testDirectCall(() => MLDsa.ImportMLDsaPublicKey(algorithm, default(ReadOnlySpan<byte>)));
            }
            else
            {
                testDirectCall(() => MLDsa.ImportMLDsaPublicKey(algorithm, publicKey));
                testDirectCall(() => MLDsa.ImportMLDsaPublicKey(algorithm, publicKey.AsSpan()));
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

        internal delegate MLDsa ImportSubjectKeyPublicInfoCallback(byte[] spki);
        internal static void AssertImportSubjectKeyPublicInfo(Action<ImportSubjectKeyPublicInfoCallback> test) =>
            AssertImportSubjectKeyPublicInfo(test, test);

        internal static void AssertImportSubjectKeyPublicInfo(
            Action<ImportSubjectKeyPublicInfoCallback> testDirectCall,
            Action<ImportSubjectKeyPublicInfoCallback> testEmbeddedCall)
        {
            testDirectCall(spki => MLDsa.ImportSubjectPublicKeyInfo(spki));
            testDirectCall(spki => MLDsa.ImportSubjectPublicKeyInfo(spki.AsSpan()));

            testEmbeddedCall(spki => MLDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki)));
            testEmbeddedCall(spki => MLDsa.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki).AsSpan()));
        }

        internal static void AssertImportPrivateKey(Action<Func<MLDsa>> test, MLDsaAlgorithm algorithm, byte[] privateKey) =>
            AssertImportPrivateKey(test, test, algorithm, privateKey);

        internal static void AssertImportPrivateKey(Action<Func<MLDsa>> testDirectCall, Action<Func<MLDsa>> testEmbeddedCall, MLDsaAlgorithm algorithm, byte[] privateKey)
        {
            testDirectCall(() => MLDsa.ImportMLDsaPrivateKey(algorithm, privateKey));

            if (privateKey?.Length == 0)
            {
                testDirectCall(() => MLDsa.ImportMLDsaPrivateKey(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => MLDsa.ImportMLDsaPrivateKey(algorithm, ReadOnlySpan<byte>.Empty));
                testDirectCall(() => MLDsa.ImportMLDsaPrivateKey(algorithm, default(ReadOnlySpan<byte>)));
            }
            else
            {
                testDirectCall(() => MLDsa.ImportMLDsaPrivateKey(algorithm, privateKey));
                testDirectCall(() => MLDsa.ImportMLDsaPrivateKey(algorithm, privateKey.AsSpan()));
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            MLDsaPrivateKeyAsn privateKeyAsn = new MLDsaPrivateKeyAsn
            {
                ExpandedKey = privateKey
            };
            privateKeyAsn.Encode(writer);

            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = AlgorithmToOid(algorithm),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                PrivateKey = writer.Encode(),
            };

            AssertImportPkcs8PrivateKey(import =>
                testEmbeddedCall(() => import(pkcs8.Encode())));
        }

        internal static void AssertImportPrivateSeed(Action<Func<MLDsa>> test, MLDsaAlgorithm algorithm, byte[] privateSeed) =>
            AssertImportPrivateSeed(test, test, algorithm, privateSeed);

        internal static void AssertImportPrivateSeed(Action<Func<MLDsa>> testDirectCall, Action<Func<MLDsa>> testEmbeddedCall, MLDsaAlgorithm algorithm, byte[] privateSeed)
        {
            testDirectCall(() => MLDsa.ImportMLDsaPrivateSeed(algorithm, privateSeed));

            if (privateSeed?.Length == 0)
            {
                testDirectCall(() => MLDsa.ImportMLDsaPrivateSeed(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => MLDsa.ImportMLDsaPrivateSeed(algorithm, ReadOnlySpan<byte>.Empty));
                testDirectCall(() => MLDsa.ImportMLDsaPrivateSeed(algorithm, default(ReadOnlySpan<byte>)));
            }
            else
            {
                testDirectCall(() => MLDsa.ImportMLDsaPrivateSeed(algorithm, privateSeed));
                testDirectCall(() => MLDsa.ImportMLDsaPrivateSeed(algorithm, privateSeed.AsSpan()));
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            MLDsaPrivateKeyAsn privateKey = new MLDsaPrivateKeyAsn
            {
                Seed = privateSeed,
            };
            privateKey.Encode(writer);

            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = AlgorithmToOid(algorithm),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                PrivateKey = writer.Encode(),
            };

            AssertImportPkcs8PrivateKey(import =>
                testEmbeddedCall(() => import(pkcs8.Encode())));
        }

        internal delegate MLDsa ImportPkcs8PrivateKeyCallback(ReadOnlySpan<byte> pkcs8);
        internal static void AssertImportPkcs8PrivateKey(Action<ImportPkcs8PrivateKeyCallback> callback) =>
            AssertImportPkcs8PrivateKey(callback, callback);

        internal static void AssertImportPkcs8PrivateKey(
            Action<ImportPkcs8PrivateKeyCallback> testDirectCall,
            Action<ImportPkcs8PrivateKeyCallback> testEmbeddedCall)
        {
            testDirectCall(pkcs8 => MLDsa.ImportPkcs8PrivateKey(pkcs8));
            testDirectCall(pkcs8 => MLDsa.ImportPkcs8PrivateKey(pkcs8.ToArray()));

            AssertImportFromPem(importPem =>
            {
                testEmbeddedCall(pkcs8 => importPem(PemEncoding.WriteString("PRIVATE KEY", pkcs8)));
            });
        }

        internal static void AssertImportFromPem(Action<Func<string, MLDsa>> callback)
        {
            callback(static (string pem) => MLDsa.ImportFromPem(pem));
            callback(static (string pem) => MLDsa.ImportFromPem(pem.AsSpan()));
        }

        internal static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> test,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All) =>
            AssertImportEncryptedPkcs8PrivateKey(test, test, passwordTypeToTest);

        internal delegate MLDsa ImportEncryptedPkcs8PrivateKeyCallback(string password, ReadOnlySpan<byte> pkcs8);
        internal static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> testDirectCall,
            Action<ImportEncryptedPkcs8PrivateKeyCallback> testEmbeddedCall,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypeToTest & EncryptionPasswordType.Char) != 0)
            {
                testDirectCall((password, pkcs8) => MLDsa.ImportEncryptedPkcs8PrivateKey(password, pkcs8.ToArray()));
                testDirectCall((password, pkcs8) => MLDsa.ImportEncryptedPkcs8PrivateKey(password.AsSpan(), pkcs8));
            }

            if ((passwordTypeToTest & EncryptionPasswordType.Byte) != 0)
            {
                testDirectCall((password, pkcs8) =>
                    MLDsa.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(password), pkcs8.ToArray()));
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

        internal delegate MLDsa ImportFromEncryptedPemCallback(string source, string password);
        internal static void AssertImportFromEncryptedPem(
            Action<ImportFromEncryptedPemCallback> callback,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypeToTest & EncryptionPasswordType.Char) != 0)
            {
                callback(static (string pem, string password) => MLDsa.ImportFromEncryptedPem(pem, password));
                callback(static (string pem, string password) => MLDsa.ImportFromEncryptedPem(pem.AsSpan(), password));
            }

            if ((passwordTypeToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback(static (string pem, string password) =>
                    MLDsa.ImportFromEncryptedPem(pem, Encoding.UTF8.GetBytes(password)));
                callback(static (string pem, string password) =>
                    MLDsa.ImportFromEncryptedPem(pem.AsSpan(), Encoding.UTF8.GetBytes(password)));
            }
        }

        internal static void AssertExportMLDsaPublicKey(Action<Func<MLDsa, byte[]>> callback)
        {
            callback(mldsa =>
            {
                byte[] buffer = new byte[mldsa.Algorithm.PublicKeySizeInBytes];
                mldsa.ExportMLDsaPublicKey(buffer.AsSpan());
                return buffer;
            });

            AssertExportSubjectPublicKeyInfo(exportSpki =>
                callback(mldsa =>
                    SubjectPublicKeyInfoAsn.Decode(exportSpki(mldsa), AsnEncodingRules.DER).SubjectPublicKey.Span.ToArray()));
        }

        internal static void AssertExportMLDsaPrivateKey(Action<Func<MLDsa, byte[]>> callback) =>
            AssertExportMLDsaPrivateKey(callback, callback);

        internal static void AssertExportMLDsaPrivateKey(Action<Func<MLDsa, byte[]>> directCallback, Action<Func<MLDsa, byte[]>> indirectCallback)
        {
            directCallback(mldsa =>
            {
                byte[] buffer = new byte[mldsa.Algorithm.PrivateKeySizeInBytes];
                mldsa.ExportMLDsaPrivateKey(buffer.AsSpan());
                return buffer;
            });

            AssertExportPkcs8PrivateKey(exportPkcs8 =>
                indirectCallback(mldsa =>
                    MLDsaPrivateKeyAsn.Decode(
                        PrivateKeyInfoAsn.Decode(
                            exportPkcs8(mldsa), AsnEncodingRules.DER).PrivateKey, AsnEncodingRules.DER).ExpandedKey?.ToArray()));
        }

        internal static void AssertExportMLDsaPrivateSeed(Action<Func<MLDsa, byte[]>> callback) =>
            AssertExportMLDsaPrivateSeed(callback, callback);

        internal static void AssertExportMLDsaPrivateSeed(Action<Func<MLDsa, byte[]>> directCallback, Action<Func<MLDsa, byte[]>> indirectCallback)
        {
            directCallback(mldsa =>
            {
                byte[] buffer = new byte[mldsa.Algorithm.PrivateSeedSizeInBytes];
                mldsa.ExportMLDsaPrivateSeed(buffer.AsSpan());
                return buffer;
            });

            AssertExportPkcs8PrivateKey(exportPkcs8 =>
                indirectCallback(mldsa =>
                    MLDsaPrivateKeyAsn.Decode(
                        PrivateKeyInfoAsn.Decode(
                            exportPkcs8(mldsa), AsnEncodingRules.DER).PrivateKey, AsnEncodingRules.DER).Seed?.ToArray()));
        }

        internal static void AssertExportPkcs8PrivateKey(MLDsa mldsa, Action<byte[]> callback) =>
            AssertExportPkcs8PrivateKey(export => callback(export(mldsa)));

        internal static void AssertExportPkcs8PrivateKey(Action<Func<MLDsa, byte[]>> callback)
        {
            callback(mldsa => DoTryUntilDone(mldsa.TryExportPkcs8PrivateKey));
            callback(mldsa => mldsa.ExportPkcs8PrivateKey());
            callback(mldsa => DecodePem(mldsa.ExportPkcs8PrivateKeyPem()));

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

        internal static void AssertExportSubjectPublicKeyInfo(MLDsa mldsa, Action<byte[]> callback) =>
            AssertExportSubjectPublicKeyInfo(export => callback(export(mldsa)));

        internal static void AssertExportSubjectPublicKeyInfo(Action<Func<MLDsa, byte[]>> callback)
        {
            callback(mldsa => DoTryUntilDone(mldsa.TryExportSubjectPublicKeyInfo));
            callback(mldsa => mldsa.ExportSubjectPublicKeyInfo());
            callback(mldsa => DecodePem(mldsa.ExportSubjectPublicKeyInfoPem()));

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
            MLDsa mldsa,
            string password,
            PbeParameters pbeParameters,
            Action<byte[]> callback) =>
            AssertEncryptedExportPkcs8PrivateKey(export => callback(export(mldsa, password, pbeParameters)));

        internal delegate byte[] ExportEncryptedPkcs8PrivateKeyCallback(MLDsa mldsa, string password, PbeParameters pbeParameters);
        internal static void AssertEncryptedExportPkcs8PrivateKey(
            Action<ExportEncryptedPkcs8PrivateKeyCallback> callback,
            EncryptionPasswordType passwordTypesToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypesToTest & EncryptionPasswordType.Char) != 0)
            {
                callback((mldsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        mldsa.TryExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters, destination, out bytesWritten)));
                callback((mldsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        mldsa.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten)));

                callback((mldsa, password, pbeParameters) => mldsa.ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters));
                callback((mldsa, password, pbeParameters) => mldsa.ExportEncryptedPkcs8PrivateKey(password, pbeParameters));

                callback((mldsa, password, pbeParameters) => DecodePem(mldsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters)));
                callback((mldsa, password, pbeParameters) => DecodePem(mldsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters)));
            }

            if ((passwordTypesToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback((mldsa, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        mldsa.TryExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters, destination, out bytesWritten)));

                callback((mldsa, password, pbeParameters) =>
                    mldsa.ExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters));

                callback((mldsa, password, pbeParameters) =>
                    DecodePem(mldsa.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters)));
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

        internal delegate string ExportToPemCallback(MLDsa mldsa, string password, PbeParameters pbeParameters);
        internal static void AssertExportToEncryptedPem(
            Action<ExportToPemCallback> callback,
            EncryptionPasswordType passwordTypesToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypesToTest & EncryptionPasswordType.Char) != 0)
            {
                callback((mldsa, password, pbeParameters) =>
                    mldsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters));
                callback((mldsa, password, pbeParameters) =>
                    mldsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters));
            }

            if ((passwordTypesToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback((mldsa, password, pbeParameters) =>
                    mldsa.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters));
            }
        }

        internal static void AssertExportToPrivateKeyPem(Action<Func<MLDsa, string>> callback) =>
            callback(mldsa => mldsa.ExportPkcs8PrivateKeyPem());

        internal static void AssertExportToPublicKeyPem(Action<Func<MLDsa, string>> callback) =>
            callback(mldsa => mldsa.ExportSubjectPublicKeyInfoPem());

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

        // CryptographicException can only have both HRESULT and Message set starting in .NET Core 3.0+.
        // To work around this, the product code throws an exception derived from CryptographicException
        // that has both set. This assert checks for that instead.
        internal static void AssertThrowsCryptographicExceptionWithHResult(Action export)
        {
            CryptographicException ce = Assert.ThrowsAny<CryptographicException>(export);
            Assert.Equal(NTE_NOT_SUPPORTED, ce.HResult);
        }

        internal static CngProperty GetCngProperty(MLDsaAlgorithm algorithm)
        {
            string parameterSetValue = algorithm.Name switch
            {
                "ML-DSA-44" => "44",
                "ML-DSA-65" => "65",
                "ML-DSA-87" => "87",
                _ => throw new XunitException("Unknown algorithm."),
            };

            byte[] byteValue = new byte[(parameterSetValue.Length + 1) * 2]; // Null terminator
            int written = Encoding.Unicode.GetBytes(parameterSetValue, 0, parameterSetValue.Length, byteValue, 0);
            Assert.Equal(byteValue.Length - 2, written);

            return new CngProperty(
                "ParameterSetName",
                byteValue,
                CngPropertyOptions.None);
        }

        internal static string? AlgorithmToOid(MLDsaAlgorithm algorithm)
        {
            return algorithm?.Name switch
            {
                "ML-DSA-44" => "2.16.840.1.101.3.4.3.17",
                "ML-DSA-65" => "2.16.840.1.101.3.4.3.18",
                "ML-DSA-87" => "2.16.840.1.101.3.4.3.19",
                _ => throw new XunitException("Unknown algorithm."),
            };
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
