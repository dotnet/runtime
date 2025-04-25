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
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPublicKey(tempBuffer));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaSecretKey(tempBuffer));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfoPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters, [], out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, pbeParameters, [], out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportPkcs8PrivateKey(tempBuffer, out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportSubjectPublicKeyInfo([], out _));
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
            AssertEncryptedExportPkcs8PrivateKey(export =>
            {
                if (export(slhDsa, password, pbeParameters).TryGetValue(out byte[] exportedValue))
                {
                    callback(exportedValue);
                }
            });

        public static void AssertEncryptedExportPkcs8PrivateKey(Action<Func<SlhDsa, string, PbeParameters, Option<byte[]>>> callback)
        {
            callback((slhDsa, password, pbeParameters) =>
                Option.Some(DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                {
                    return slhDsa.TryExportEncryptedPkcs8PrivateKey(
                        password.AsSpan(),
                        pbeParameters,
                        destination,
                        out bytesWritten);
                })));

            callback((slhDsa, password, pbeParameters) => Option.Some(slhDsa.ExportEncryptedPkcs8PrivateKey(password, pbeParameters)));
            callback((slhDsa, password, pbeParameters) => Option.Some(slhDsa.ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters)));
            callback((slhDsa, password, pbeParameters) => Option.Some(DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters))));
            callback((slhDsa, password, pbeParameters) => Option.Some(DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters))));

            // PKCS12 PBE requires char-passwords, so don't run byte-password callbacks.
            callback((slhDsa, password, pbeParameters) =>
                pbeParameters.EncryptionAlgorithm != PbeEncryptionAlgorithm.TripleDes3KeyPkcs12
                ? Option.Some(DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                    slhDsa.TryExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters, destination, out bytesWritten)))
                : Option.None<byte[]>());

            callback((slhDsa, password, pbeParameters) =>
                pbeParameters.EncryptionAlgorithm != PbeEncryptionAlgorithm.TripleDes3KeyPkcs12
                ? Option.Some(slhDsa.ExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters))
                : Option.None<byte[]>());

            callback((slhDsa, password, pbeParameters) =>
                pbeParameters.EncryptionAlgorithm != PbeEncryptionAlgorithm.TripleDes3KeyPkcs12
                ? Option.Some(DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password)), pbeParameters)))
                : Option.None<byte[]>());

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("ENCRYPTED PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

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

        internal struct Option<T>
        {
            private Option(T value)
            {
                HasValue = true;
                Value = value;
            }

            public static Option<T> None => default;
            public static Option<T> Some(T value) => new(value);

            public T Value { init; get; }
            public bool HasValue { init; get; }
            public bool TryGetValue(out T value)
            {
                value = Value;
                return HasValue;
            }
        }

        internal static class Option
        {
            public static Option<T> Some<T>(T value) => Option<T>.Some(value);
            public static Option<T> None<T>() => Option<T>.None;
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
