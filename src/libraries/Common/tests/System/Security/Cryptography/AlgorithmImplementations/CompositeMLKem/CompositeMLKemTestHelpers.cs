// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Reflection;
using System.Security.Cryptography.Asn1;
using System.Text;
using Test.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    internal static class CompositeMLKemTestHelpers
    {
        private delegate AsnWriter WriteEncryptedPkcs8<T>(ReadOnlySpan<T> password, AsnWriter writer, PbeParameters pbeParameters);

        // DER encoding of ASN.1 BitString "foo"
        internal static readonly ReadOnlyMemory<byte> s_derBitStringFoo = new byte[] { 0x03, 0x04, 0x00, 0x66, 0x6f, 0x6f };
        private static readonly WriteEncryptedPkcs8<char> s_writeEncryptedPkcs8Char = GetWriteEncryptedPkcs8<char>();
        private static readonly WriteEncryptedPkcs8<byte> s_writeEncryptedPkcs8Byte = GetWriteEncryptedPkcs8<byte>();

        internal static void AssertImportEncapsulationKey(
            Action<Func<CompositeMLKem>> test,
            CompositeMLKemAlgorithm algorithm,
            byte[] encapsulationKey) =>
            AssertImportEncapsulationKey(test, test, algorithm, encapsulationKey);

        internal static void AssertImportEncapsulationKey(
            Action<Func<CompositeMLKem>> testDirectCall,
            Action<Func<CompositeMLKem>> testEmbeddedCall,
            CompositeMLKemAlgorithm algorithm,
            byte[] encapsulationKey)
        {
            testDirectCall(() => CompositeMLKem.ImportEncapsulationKey(algorithm, encapsulationKey));

            if (encapsulationKey?.Length == 0)
            {
                testDirectCall(() => CompositeMLKem.ImportEncapsulationKey(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => CompositeMLKem.ImportEncapsulationKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => CompositeMLKem.ImportEncapsulationKey(algorithm, encapsulationKey.AsSpan()));
            }

            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = AlgorithmToOid(algorithm),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                SubjectPublicKey = encapsulationKey,
            };

            AssertImportSubjectPublicKeyInfo(import => testEmbeddedCall(() => import(spki.Encode())));
        }

        internal delegate CompositeMLKem ImportSubjectPublicKeyInfoCallback(byte[] spki);

        internal static void AssertImportSubjectPublicKeyInfo(Action<ImportSubjectPublicKeyInfoCallback> test) =>
            AssertImportSubjectPublicKeyInfo(test, test);

        internal static void AssertImportSubjectPublicKeyInfo(
            Action<ImportSubjectPublicKeyInfoCallback> testDirectCall,
            Action<ImportSubjectPublicKeyInfoCallback> testEmbeddedCall)
        {
            testDirectCall(spki => CompositeMLKem.ImportSubjectPublicKeyInfo(spki));
            testDirectCall(spki => CompositeMLKem.ImportSubjectPublicKeyInfo(spki.AsSpan()));

            testEmbeddedCall(spki => CompositeMLKem.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki)));
            testEmbeddedCall(spki => CompositeMLKem.ImportFromPem(PemEncoding.WriteString("PUBLIC KEY", spki).AsSpan()));
        }

        internal static void AssertImportDecapsulationKey(
            Action<Func<CompositeMLKem>> test,
            CompositeMLKemAlgorithm algorithm,
            byte[] decapsulationKey) =>
            AssertImportDecapsulationKey(test, test, algorithm, decapsulationKey);

        internal static void AssertImportDecapsulationKey(
            Action<Func<CompositeMLKem>> testDirectCall,
            Action<Func<CompositeMLKem>> testEmbeddedCall,
            CompositeMLKemAlgorithm algorithm,
            byte[] decapsulationKey)
        {
            testDirectCall(() => CompositeMLKem.ImportDecapsulationKey(algorithm, decapsulationKey));

            if (decapsulationKey?.Length == 0)
            {
                testDirectCall(() => CompositeMLKem.ImportDecapsulationKey(algorithm, Array.Empty<byte>().AsSpan()));
                testDirectCall(() => CompositeMLKem.ImportDecapsulationKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => CompositeMLKem.ImportDecapsulationKey(algorithm, decapsulationKey.AsSpan()));
            }

            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = AlgorithmToOid(algorithm),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                PrivateKey = decapsulationKey,
            };

            AssertImportPkcs8PrivateKey(import => testEmbeddedCall(() => import(pkcs8.Encode())));
        }

        internal delegate CompositeMLKem ImportPkcs8PrivateKeyCallback(ReadOnlySpan<byte> pkcs8);

        internal static void AssertImportPkcs8PrivateKey(Action<ImportPkcs8PrivateKeyCallback> callback) =>
            AssertImportPkcs8PrivateKey(callback, callback);

        internal static void AssertImportPkcs8PrivateKey(
            Action<ImportPkcs8PrivateKeyCallback> testDirectCall,
            Action<ImportPkcs8PrivateKeyCallback> testEmbeddedCall)
        {
            testDirectCall(pkcs8 => CompositeMLKem.ImportPkcs8PrivateKey(pkcs8));
            testDirectCall(pkcs8 => CompositeMLKem.ImportPkcs8PrivateKey(pkcs8.ToArray()));

            AssertImportFromPem(importPem =>
            {
                testEmbeddedCall(pkcs8 => importPem(PemEncoding.WriteString("PRIVATE KEY", pkcs8)));
            });
        }

        internal static void AssertImportFromPem(Action<Func<string, CompositeMLKem>> callback)
        {
            callback(static (string pem) => CompositeMLKem.ImportFromPem(pem));
            callback(static (string pem) => CompositeMLKem.ImportFromPem(pem.AsSpan()));
        }

        internal delegate CompositeMLKem ImportEncryptedPkcs8PrivateKeyCallback(string password, ReadOnlySpan<byte> pkcs8);

        internal static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> test,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All) =>
            AssertImportEncryptedPkcs8PrivateKey(test, test, passwordTypeToTest);

        internal static void AssertImportEncryptedPkcs8PrivateKey(
            Action<ImportEncryptedPkcs8PrivateKeyCallback> testDirectCall,
            Action<ImportEncryptedPkcs8PrivateKeyCallback> testEmbeddedCall,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypeToTest & EncryptionPasswordType.Char) != 0)
            {
                testDirectCall((password, pkcs8) => CompositeMLKem.ImportEncryptedPkcs8PrivateKey(password, pkcs8.ToArray()));
                testDirectCall((password, pkcs8) => CompositeMLKem.ImportEncryptedPkcs8PrivateKey(password.AsSpan(), pkcs8));
            }

            if ((passwordTypeToTest & EncryptionPasswordType.Byte) != 0)
            {
                testDirectCall((password, pkcs8) =>
                    CompositeMLKem.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(password), pkcs8.ToArray()));
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

        internal delegate CompositeMLKem ImportFromEncryptedPemCallback(string source, string password);

        internal static void AssertImportFromEncryptedPem(
            Action<ImportFromEncryptedPemCallback> callback,
            EncryptionPasswordType passwordTypeToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypeToTest & EncryptionPasswordType.Char) != 0)
            {
                callback(static (string pem, string password) => CompositeMLKem.ImportFromEncryptedPem(pem, password));
                callback(static (string pem, string password) => CompositeMLKem.ImportFromEncryptedPem(pem.AsSpan(), password));
            }

            if ((passwordTypeToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback(static (string pem, string password) =>
                    CompositeMLKem.ImportFromEncryptedPem(pem, Encoding.UTF8.GetBytes(password)));
                callback(static (string pem, string password) =>
                    CompositeMLKem.ImportFromEncryptedPem(pem.AsSpan(), Encoding.UTF8.GetBytes(password)));
            }
        }

        internal static void AssertExportEncapsulationKey(Action<Func<CompositeMLKem, byte[]>> callback)
        {
            callback(kem =>
            {
                // For simplicity, use a large enough size for all algorithms.
                byte[] buffer = new byte[4096];

                int size = kem.ExportEncapsulationKey(buffer.AsSpan());
                Array.Resize(ref buffer, size);

                return buffer;
            });

            callback(kem => kem.ExportEncapsulationKey());
            callback(kem => DoTryUntilDone(kem.TryExportEncapsulationKey));

            AssertExportSubjectPublicKeyInfo(exportSpki =>
                callback(kem =>
                    SubjectPublicKeyInfoAsn.Decode(exportSpki(kem), AsnEncodingRules.DER).SubjectPublicKey.ToArray()));
        }

        internal static void AssertExportDecapsulationKey(Action<Func<CompositeMLKem, byte[]>> callback) =>
            AssertExportDecapsulationKey(callback, callback);

        internal static void AssertExportDecapsulationKey(
            Action<Func<CompositeMLKem, byte[]>> directCallback,
            Action<Func<CompositeMLKem, byte[]>> indirectCallback)
        {
            directCallback(kem =>
            {
                // For simplicity, use a large enough size for all algorithms.
                byte[] buffer = new byte[4096];

                int size = kem.ExportDecapsulationKey(buffer.AsSpan());
                Array.Resize(ref buffer, size);

                return buffer;
            });

            directCallback(kem => kem.ExportDecapsulationKey());
            directCallback(kem => DoTryUntilDone(kem.TryExportDecapsulationKey));

            AssertExportPkcs8PrivateKey(exportPkcs8 =>
                indirectCallback(kem =>
                    PrivateKeyInfoAsn.Decode(exportPkcs8(kem), AsnEncodingRules.DER).PrivateKey.ToArray()));
        }

        internal static void AssertExportPkcs8PrivateKey(CompositeMLKem kem, Action<byte[]> callback) =>
            AssertExportPkcs8PrivateKey(export => callback(export(kem)));

        internal static void AssertExportPkcs8PrivateKey(Action<Func<CompositeMLKem, byte[]>> callback)
        {
            callback(kem => DoTryUntilDone(kem.TryExportPkcs8PrivateKey));
            callback(kem => kem.ExportPkcs8PrivateKey());
            callback(kem => DecodePem(kem.ExportPkcs8PrivateKeyPem(), "PRIVATE KEY"));
        }

        internal static void AssertExportSubjectPublicKeyInfo(CompositeMLKem kem, Action<byte[]> callback) =>
            AssertExportSubjectPublicKeyInfo(export => callback(export(kem)));

        internal static void AssertExportSubjectPublicKeyInfo(Action<Func<CompositeMLKem, byte[]>> callback)
        {
            callback(kem => DoTryUntilDone(kem.TryExportSubjectPublicKeyInfo));
            callback(kem => kem.ExportSubjectPublicKeyInfo());
            callback(kem => DecodePem(kem.ExportSubjectPublicKeyInfoPem(), "PUBLIC KEY"));
        }

        internal delegate byte[] ExportEncryptedPkcs8PrivateKeyCallback(CompositeMLKem kem, string password, PbeParameters pbeParameters);

        internal static void AssertEncryptedExportPkcs8PrivateKey(
            CompositeMLKem kem,
            string password,
            PbeParameters pbeParameters,
            Action<byte[]> callback) =>
            AssertEncryptedExportPkcs8PrivateKey(export => callback(export(kem, password, pbeParameters)));

        internal static void AssertEncryptedExportPkcs8PrivateKey(
            Action<ExportEncryptedPkcs8PrivateKeyCallback> callback,
            EncryptionPasswordType passwordTypesToTest = EncryptionPasswordType.All)
        {
            if ((passwordTypesToTest & EncryptionPasswordType.Char) != 0)
            {
                callback((kem, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        kem.TryExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters, destination, out bytesWritten)));
                callback((kem, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        kem.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten)));

                callback((kem, password, pbeParameters) => kem.ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters));
                callback((kem, password, pbeParameters) => kem.ExportEncryptedPkcs8PrivateKey(password, pbeParameters));

                callback((kem, password, pbeParameters) =>
                    DecodePem(kem.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters), "ENCRYPTED PRIVATE KEY"));
                callback((kem, password, pbeParameters) =>
                    DecodePem(kem.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters), "ENCRYPTED PRIVATE KEY"));
            }

            if ((passwordTypesToTest & EncryptionPasswordType.Byte) != 0)
            {
                callback((kem, password, pbeParameters) =>
                    DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                        kem.TryExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters, destination, out bytesWritten)));

                callback((kem, password, pbeParameters) =>
                    kem.ExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters));

                callback((kem, password, pbeParameters) =>
                    DecodePem(kem.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters), "ENCRYPTED PRIVATE KEY"));
            }
        }

        internal static byte[] CreateEncryptedPkcs8PrivateKey(
            string algorithmOid,
            byte[] privateKey,
            PbeParameters pbeParameters,
            EncryptionPasswordType passwordType = EncryptionPasswordType.Char)
        {
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                Version = 0,
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = algorithmOid,
                    Parameters = null,
                },
                PrivateKey = privateKey,
            };

            AsnWriter pkcs8Writer = new(AsnEncodingRules.DER);
            AsnWriter? encryptedWriter = null;

            try
            {
                pkcs8.Encode(pkcs8Writer);

                encryptedWriter = passwordType switch
                {
                    EncryptionPasswordType.Char => s_writeEncryptedPkcs8Char("PLACEHOLDER", pkcs8Writer, pbeParameters),
                    EncryptionPasswordType.Byte => s_writeEncryptedPkcs8Byte("PLACEHOLDER"u8, pkcs8Writer, pbeParameters),
                    _ => throw new XunitException("Exactly one password type is required."),
                };

                return encryptedWriter.Encode();
            }
            finally
            {
                encryptedWriter?.Reset();
                pkcs8Writer.Reset();
            }
        }

        private static WriteEncryptedPkcs8<T> GetWriteEncryptedPkcs8<T>()
        {
            Type keyFormatHelper = typeof(PbeParameters).Assembly.GetType(
                "System.Security.Cryptography.KeyFormatHelper",
                throwOnError: true)!;
            MethodInfo method = keyFormatHelper.GetMethod(
                "WriteEncryptedPkcs8",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                new[] { typeof(ReadOnlySpan<T>), typeof(AsnWriter), typeof(PbeParameters) },
                modifiers: null)!;

            return (WriteEncryptedPkcs8<T>)method.CreateDelegate(typeof(WriteEncryptedPkcs8<T>));
        }

        internal static void VerifyDisposed(CompositeMLKem kem)
        {
            CompositeMLKemAlgorithm algorithm = kem.Algorithm;
            byte[] ciphertext = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecret = new byte[algorithm.SharedSecretSizeInBytes];
            byte[] tempBuffer = new byte[4096];

            Assert.Throws<ObjectDisposedException>(() => kem.Encapsulate(out _, out _));
            Assert.Throws<ObjectDisposedException>(() => kem.Encapsulate(ciphertext, sharedSecret));
            Assert.Throws<ObjectDisposedException>(() => kem.Decapsulate(ciphertext));
            Assert.Throws<ObjectDisposedException>(() => kem.Decapsulate(new ReadOnlySpan<byte>(ciphertext), sharedSecret));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncapsulationKey());
            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncapsulationKey(tempBuffer));
            Assert.Throws<ObjectDisposedException>(() => kem.TryExportEncapsulationKey(tempBuffer, out _));
            Assert.Throws<ObjectDisposedException>(() => kem.ExportDecapsulationKey());
            Assert.Throws<ObjectDisposedException>(() => kem.ExportDecapsulationKey(tempBuffer));
            Assert.Throws<ObjectDisposedException>(() => kem.TryExportDecapsulationKey(tempBuffer, out _));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => kem.ExportPkcs8PrivateKeyPem());
            Assert.Throws<ObjectDisposedException>(() => kem.TryExportPkcs8PrivateKey(tempBuffer, out _));
            Assert.Throws<ObjectDisposedException>(() => kem.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => kem.ExportSubjectPublicKeyInfoPem());
            Assert.Throws<ObjectDisposedException>(() => kem.TryExportSubjectPublicKeyInfo(tempBuffer, out _));

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 42);

            AssertEncryptedExportPkcs8PrivateKey(export =>
                Assert.Throws<ObjectDisposedException>(() => export(kem, "PLACEHOLDER", pbeParameters)));
        }

        internal static string AlgorithmToOid(CompositeMLKemAlgorithm algorithm)
        {
            return algorithm?.Name switch
            {
                "MLKEM768-RSA2048-SHA3-256"                 => "1.3.6.1.5.5.7.6.55",
                "MLKEM768-RSA3072-SHA3-256"                 => "1.3.6.1.5.5.7.6.56",
                "MLKEM768-RSA4096-SHA3-256"                 => "1.3.6.1.5.5.7.6.57",
                "MLKEM768-X25519-SHA3-256"                  => "1.3.6.1.5.5.7.6.58",
                "MLKEM768-ECDH-P256-SHA3-256"               => "1.3.6.1.5.5.7.6.59",
                "MLKEM768-ECDH-P384-SHA3-256"               => "1.3.6.1.5.5.7.6.60",
                "MLKEM768-ECDH-brainpoolP256r1-SHA3-256"    => "1.3.6.1.5.5.7.6.61",
                "MLKEM1024-RSA3072-SHA3-256"                => "1.3.6.1.5.5.7.6.62",
                "MLKEM1024-ECDH-P384-SHA3-256"              => "1.3.6.1.5.5.7.6.63",
                "MLKEM1024-ECDH-brainpoolP384r1-SHA3-256"   => "1.3.6.1.5.5.7.6.64",
                "MLKEM1024-X448-SHA3-256"                   => "1.3.6.1.5.5.7.6.65",
                "MLKEM1024-ECDH-P521-SHA3-256"              => "1.3.6.1.5.5.7.6.66",

                _ => throw new XunitException("Unknown algorithm."),
            };
        }

        internal static byte[] DecodePem(string pem, string expectedLabel)
        {
            PemFields fields = PemEncoding.Find(pem.AsSpan());
            Assert.Equal(Index.FromStart(0), fields.Location.Start);
            Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
            Assert.Equal(expectedLabel, pem.AsSpan()[fields.Label].ToString());
            return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
        }

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
