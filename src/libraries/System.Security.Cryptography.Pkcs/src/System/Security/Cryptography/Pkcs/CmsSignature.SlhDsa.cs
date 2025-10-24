// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    internal partial class CmsSignature
    {
        static partial void PrepareRegistrationSlhDsa(Dictionary<string, CmsSignature> lookup)
        {
            lookup.Add(Oids.SlhDsaSha2_128s, new SlhDsaCmsSignature(Oids.SlhDsaSha2_128s));
            lookup.Add(Oids.SlhDsaShake128s, new SlhDsaCmsSignature(Oids.SlhDsaShake128s));
            lookup.Add(Oids.SlhDsaSha2_128f, new SlhDsaCmsSignature(Oids.SlhDsaSha2_128f));
            lookup.Add(Oids.SlhDsaShake128f, new SlhDsaCmsSignature(Oids.SlhDsaShake128f));
            lookup.Add(Oids.SlhDsaSha2_192s, new SlhDsaCmsSignature(Oids.SlhDsaSha2_192s));
            lookup.Add(Oids.SlhDsaShake192s, new SlhDsaCmsSignature(Oids.SlhDsaShake192s));
            lookup.Add(Oids.SlhDsaSha2_192f, new SlhDsaCmsSignature(Oids.SlhDsaSha2_192f));
            lookup.Add(Oids.SlhDsaShake192f, new SlhDsaCmsSignature(Oids.SlhDsaShake192f));
            lookup.Add(Oids.SlhDsaSha2_256s, new SlhDsaCmsSignature(Oids.SlhDsaSha2_256s));
            lookup.Add(Oids.SlhDsaShake256s, new SlhDsaCmsSignature(Oids.SlhDsaShake256s));
            lookup.Add(Oids.SlhDsaSha2_256f, new SlhDsaCmsSignature(Oids.SlhDsaSha2_256f));
            lookup.Add(Oids.SlhDsaShake256f, new SlhDsaCmsSignature(Oids.SlhDsaShake256f));
        }

        private sealed class SlhDsaCmsSignature : CmsSignature
        {
            private string _signatureAlgorithm;

            internal SlhDsaCmsSignature(string signatureAlgorithm)
            {
                _signatureAlgorithm = signatureAlgorithm;
            }

            protected override bool VerifyKeyType(object key) => key is SlhDsa;
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

                // The spec (as of May 5, 2025) has strength requirements on the hash, but we will
                // not enforce them here. If the callers wants to enforce them, they can do so by themselves.

                SlhDsa? publicKey = certificate.GetSlhDsaPublicKey();

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
                // The spec (as of May 5, 2025) has strength requirements on the hash, but we will
                // not enforce them here. It is up to the caller to choose an appropriate hash.

                signatureParameters = null;
                signatureAlgorithm = _signatureAlgorithm;

                using (GetSigningKey(key, certificate, silent, static cert => cert.GetSlhDsaPublicKey(), out SlhDsa? signingKey))
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
                        using (SlhDsa certKey = certificate.GetSlhDsaPublicKey()!)
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
