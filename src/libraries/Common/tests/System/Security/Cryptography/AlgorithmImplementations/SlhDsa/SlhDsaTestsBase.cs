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
    public abstract class SlhDsaTestsBase
    {
        protected abstract SlhDsa GenerateKey(SlhDsaAlgorithm algorithm);
        protected abstract SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        protected abstract SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        protected static void ExerciseSuccessfulVerify(SlhDsa slhDsa, byte[] data, byte[] signature, byte[] context)
        {
            AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, context));

            if (data.Length > 0)
            {
                AssertExtensions.FalseExpression(slhDsa.VerifyData([], signature, context));
                AssertExtensions.FalseExpression(slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                data[0] ^= 1;
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, context));
                data[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(slhDsa.VerifyData([], signature, context));
                AssertExtensions.TrueExpression(slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                AssertExtensions.FalseExpression(slhDsa.VerifyData([0], signature, context));
                AssertExtensions.FalseExpression(slhDsa.VerifyData([1, 2, 3], signature, context));
            }

            signature[0] ^= 1;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, context));
            signature[0] ^= 1;

            if (context.Length > 0)
            {
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, []));
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                context[0] ^= 1;
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, context));
                context[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, []));
                AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, [0]));
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, [1, 2, 3]));
            }

            AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, context));
        }

        protected static void VerifyDisposed(SlhDsa slhDsa)
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

        protected static void AssertExportPkcs8PrivateKey(SlhDsa slhDsa, Action<byte[]> callback)
        {
            callback(DoTryUntilDone(slhDsa.TryExportPkcs8PrivateKey));
            callback(slhDsa.ExportPkcs8PrivateKey());
            callback(DecodePem(slhDsa.ExportPkcs8PrivateKeyPem()));

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

        protected static void AssertExportSubjectPublicKeyInfo(SlhDsa slhDsa, Action<byte[]> callback)
        {
            callback(DoTryUntilDone(slhDsa.TryExportSubjectPublicKeyInfo));
            callback(slhDsa.ExportSubjectPublicKeyInfo());
            callback(DecodePem(slhDsa.ExportSubjectPublicKeyInfoPem()));

            static byte[] DecodePem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("PUBLIC KEY", pem.AsSpan()[fields.Label].ToString());
                return Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString());
            }
        }

        protected static void AssertEncryptedExportPkcs8PrivateKey(
            SlhDsa slhDsa,
            string password,
            PbeParameters pbeParameters,
            Action<byte[]> callback)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            callback(DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
            {
                return slhDsa.TryExportEncryptedPkcs8PrivateKey(
                    password.AsSpan(),
                    pbeParameters,
                    destination,
                    out bytesWritten);
            }));

            callback(slhDsa.ExportEncryptedPkcs8PrivateKey(password, pbeParameters));
            callback(slhDsa.ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters));
            callback(DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password, pbeParameters)));
            callback(DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters)));

            // PKCS12 PBE requires char-passwords, so don't run byte-password callbacks.
            if (pbeParameters.EncryptionAlgorithm != PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
            {
                callback(DoTryUntilDone((Span<byte> destination, out int bytesWritten) =>
                {
                    return slhDsa.TryExportEncryptedPkcs8PrivateKey(
                        new ReadOnlySpan<byte>(passwordBytes),
                        pbeParameters,
                        destination,
                        out bytesWritten);
                }));

                callback(slhDsa.ExportEncryptedPkcs8PrivateKey(new ReadOnlySpan<byte>(passwordBytes), pbeParameters));
                callback(DecodePem(slhDsa.ExportEncryptedPkcs8PrivateKeyPem(new ReadOnlySpan<byte>(passwordBytes), pbeParameters)));
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

        protected static void AssertEncryptedPkcs8PrivateKeyContents(PbeParameters pbeParameters, ReadOnlyMemory<byte> contents)
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

        protected static string WritePem(string label, byte[] contents)
        {
            PemEncoding.Write(label, contents);
            string base64 = Convert.ToBase64String(contents, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN {label}-----\n{base64}\n-----END {label}-----";
        }
    }
}
