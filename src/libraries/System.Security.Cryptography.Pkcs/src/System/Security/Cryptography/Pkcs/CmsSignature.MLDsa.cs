// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.Pkcs
{
    internal partial class CmsSignature
    {
        static partial void PrepareRegistrationMLDsa(Dictionary<string, CmsSignature> lookup)
        {
            lookup.Add(Oids.MLDsa44, new MLDsaCmsSignature(Oids.MLDsa44));
            lookup.Add(Oids.MLDsa65, new MLDsaCmsSignature(Oids.MLDsa65));
            lookup.Add(Oids.MLDsa87, new MLDsaCmsSignature(Oids.MLDsa87));
        }

        private sealed class MLDsaCmsSignature : CmsSignature
        {
            private string _signatureAlgorithm;

            internal MLDsaCmsSignature(string signatureAlgorithm)
            {
                _signatureAlgorithm = signatureAlgorithm;
            }

            protected override bool VerifyKeyType(object key) => key is MLDsa;
            internal override bool NeedsHashedMessage => false;

            internal override RSASignaturePadding? SignaturePadding => null;

            internal override bool VerifySignature(
#if NET || NETSTANDARD2_1
                ReadOnlySpan<byte> valueHash,
                ReadOnlyMemory<byte> signature,
#else
                byte[] valueHash,
                byte[] signature,
#endif
                string? digestAlgorithmOid,
                ReadOnlyMemory<byte>? signatureParameters,
                X509Certificate2 certificate)
            {
                if (signatureParameters.HasValue)
                {
                    throw new CryptographicException(
                        SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, _signatureAlgorithm));
                }

                MLDsa? publicKey = certificate.GetMLDsaPublicKey();

                if (publicKey is null)
                {
                    return false;
                }

                using (publicKey)
                {
                    return publicKey.VerifyData(
                        valueHash,
#if NET || NETSTANDARD2_1
                        signature.Span
#else
                        signature
#endif
                    );
                }
            }

            protected override bool Sign(
#if NET || NETSTANDARD2_1
                ReadOnlySpan<byte> dataHash,
#else
                byte[] dataHash,
#endif
                string? hashAlgorithmOid,
                X509Certificate2 certificate,
                object? key,
                bool silent,
                [NotNullWhen(true)] out string? signatureAlgorithm,
                [NotNullWhen(true)] out byte[]? signatureValue,
                out byte[]? signatureParameters)
            {
                signatureParameters = null;
                signatureAlgorithm = _signatureAlgorithm;

                using (GetSigningKey(key, certificate, silent, static cert => cert.GetMLDsaPublicKey(), out MLDsa? signingKey))
                {
                    if (signingKey is null)
                    {
                        signatureValue = null;
                        return false;
                    }

                    // Don't pool because we will likely return this buffer to the caller.
                    byte[] signature = new byte[signingKey.Algorithm.SignatureSizeInBytes];
                    signingKey.SignData(dataHash, signature);

                    if (key != null)
                    {
                        using (MLDsa certKey = certificate.GetMLDsaPublicKey()!)
                        {
                            if (!certKey.VerifyData(dataHash, signature))
                            {
                                signatureValue = null;
                                return false;
                            }
                        }
                    }

                    signatureValue = signature;
                    return true;
                }
            }
        }
    }
}
