// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    internal partial class CmsSignature
    {
        static partial void PrepareRegistrationECDsa(Dictionary<string, CmsSignature> lookup)
        {
            lookup.Add(Oids.ECDsaWithSha1, new ECDsaCmsSignature(Oids.ECDsaWithSha1, HashAlgorithmName.SHA1));
            lookup.Add(Oids.ECDsaWithSha256, new ECDsaCmsSignature(Oids.ECDsaWithSha256, HashAlgorithmName.SHA256));
            lookup.Add(Oids.ECDsaWithSha384, new ECDsaCmsSignature(Oids.ECDsaWithSha384, HashAlgorithmName.SHA384));
            lookup.Add(Oids.ECDsaWithSha512, new ECDsaCmsSignature(Oids.ECDsaWithSha512, HashAlgorithmName.SHA512));
            lookup.Add(Oids.EcPublicKey, new ECDsaCmsSignature(null, default));
        }

        private sealed partial class ECDsaCmsSignature : CmsSignature
        {
            private readonly HashAlgorithmName _expectedDigest;
            private readonly string? _signatureAlgorithm;

            internal ECDsaCmsSignature(string? signatureAlgorithm, HashAlgorithmName expectedDigest)
            {
                _signatureAlgorithm = signatureAlgorithm;
                _expectedDigest = expectedDigest;
            }

            protected override bool VerifyKeyType(AsymmetricAlgorithm key)
            {
                return (key as ECDsa) != null;
            }

            internal override bool VerifySignature(
#if NETCOREAPP || NETSTANDARD2_1
                ReadOnlySpan<byte> valueHash,
                ReadOnlyMemory<byte> signature,
#else
                byte[] valueHash,
                byte[] signature,
#endif
                string? digestAlgorithmOid,
                HashAlgorithmName digestAlgorithmName,
                ReadOnlyMemory<byte>? signatureParameters,
                X509Certificate2 certificate)
            {
                if (_expectedDigest != digestAlgorithmName)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_Cms_InvalidSignerHashForSignatureAlg,
                            digestAlgorithmOid,
                            _signatureAlgorithm));
                }

                ECDsa? key = certificate.GetECDsaPublicKey();

                if (key == null)
                {
                    return false;
                }

                int bufSize;
                checked
                {
                    // fieldSize = ceil(KeySizeBits / 8);
                    int fieldSize = (key.KeySize + 7) / 8;
                    bufSize = 2 * fieldSize;
                }

#if NETCOREAPP || NETSTANDARD2_1
                byte[] rented = CryptoPool.Rent(bufSize);
                Span<byte> ieee = new Span<byte>(rented, 0, bufSize);

                try
                {
#else
                byte[] ieee = new byte[bufSize];
#endif
                    if (!DsaDerToIeee(signature, ieee))
                    {
                        return false;
                    }

                    return key.VerifyHash(valueHash, ieee);
#if NETCOREAPP || NETSTANDARD2_1
                }
                finally
                {
                    CryptoPool.Return(rented, bufSize);
                }
#endif
            }

            protected override bool Sign(
#if NETCOREAPP || NETSTANDARD2_1
                ReadOnlySpan<byte> dataHash,
#else
                byte[] dataHash,
#endif
                HashAlgorithmName hashAlgorithmName,
                X509Certificate2 certificate,
                AsymmetricAlgorithm? certKey,
                bool silent,
                [NotNullWhen(true)] out string? signatureAlgorithm,
                [NotNullWhen(true)] out byte[]? signatureValue)
            {
                // If there's no private key, fall back to the public key for a "no private key" exception.
                ECDsa? key = certKey as ECDsa ??
                    PkcsPal.Instance.GetPrivateKeyForSigning<ECDsa>(certificate, silent) ??
                    certificate.GetECDsaPublicKey();

                if (key == null)
                {
                    signatureAlgorithm = null;
                    signatureValue = null;
                    return false;
                }

                string? oidValue =
                    hashAlgorithmName == HashAlgorithmName.SHA1 ? Oids.ECDsaWithSha1 :
                    hashAlgorithmName == HashAlgorithmName.SHA256 ? Oids.ECDsaWithSha256 :
                    hashAlgorithmName == HashAlgorithmName.SHA384 ? Oids.ECDsaWithSha384 :
                    hashAlgorithmName == HashAlgorithmName.SHA512 ? Oids.ECDsaWithSha512 :
                    null;

                if (oidValue == null)
                {
                    signatureAlgorithm = null;
                    signatureValue = null;
                    return false;
                }

                signatureAlgorithm = oidValue;

#if NETCOREAPP || NETSTANDARD2_1
                int bufSize;
                checked
                {
                    // fieldSize = ceil(KeySizeBits / 8);
                    int fieldSize = (key.KeySize + 7) / 8;
                    bufSize = 2 * fieldSize;
                }

                byte[] rented = CryptoPool.Rent(bufSize);
                int bytesWritten = 0;

                try
                {
                    if (key.TrySignHash(dataHash, rented, out bytesWritten))
                    {
                        var signedHash = new ReadOnlySpan<byte>(rented, 0, bytesWritten);

                        if (key != null && !certificate.GetECDsaPublicKey()!.VerifyHash(dataHash, signedHash))
                        {
                            // key did not match certificate
                            signatureValue = null;
                            return false;
                        }

                        signatureValue = DsaIeeeToDer(signedHash);
                        return true;
                    }
                }
                finally
                {
                    CryptoPool.Return(rented, bytesWritten);
                }
#endif

                signatureValue = DsaIeeeToDer(key.SignHash(
#if NETCOREAPP || NETSTANDARD2_1
                    dataHash.ToArray()
#else
                    dataHash
#endif
                    ));
                return true;
            }
        }
    }
}
