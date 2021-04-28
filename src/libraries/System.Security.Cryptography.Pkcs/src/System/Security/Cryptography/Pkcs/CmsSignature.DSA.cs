// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Pkcs
{
    internal partial class CmsSignature
    {
        static partial void PrepareRegistrationDsa(Dictionary<string, CmsSignature> lookup)
        {
            lookup.Add(Oids.DsaWithSha1, new DSACmsSignature(Oids.DsaWithSha1, HashAlgorithmName.SHA1));
            lookup.Add(Oids.DsaWithSha256, new DSACmsSignature(Oids.DsaWithSha256, HashAlgorithmName.SHA256));
            lookup.Add(Oids.DsaWithSha384, new DSACmsSignature(Oids.DsaWithSha384, HashAlgorithmName.SHA384));
            lookup.Add(Oids.DsaWithSha512, new DSACmsSignature(Oids.DsaWithSha512, HashAlgorithmName.SHA512));
            lookup.Add(Oids.Dsa, new DSACmsSignature(null, default));
        }

        private sealed class DSACmsSignature : CmsSignature
        {
            private readonly HashAlgorithmName _expectedDigest;
            private readonly string? _signatureAlgorithm;

            internal DSACmsSignature(string? signatureAlgorithm, HashAlgorithmName expectedDigest)
            {
                _signatureAlgorithm = signatureAlgorithm;
                _expectedDigest = expectedDigest;
            }

            protected override bool VerifyKeyType(AsymmetricAlgorithm key)
            {
                return (key as DSA) != null;
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

                DSA? dsa = certificate.GetDSAPublicKey();

                if (dsa == null)
                {
                    return false;
                }

                DSAParameters dsaParameters = dsa.ExportParameters(false);
                int bufSize = 2 * dsaParameters.Q!.Length;

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

                    return dsa.VerifySignature(valueHash, ieee);
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
                AsymmetricAlgorithm? key,
                bool silent,
                [NotNullWhen(true)] out string? signatureAlgorithm,
                [NotNullWhen(true)] out byte[]? signatureValue)
            {
                // If there's no private key, fall back to the public key for a "no private key" exception.
                DSA? dsa = key as DSA ??
                    PkcsPal.Instance.GetPrivateKeyForSigning<DSA>(certificate, silent) ??
                    certificate.GetDSAPublicKey();

                if (dsa == null)
                {
                    signatureAlgorithm = null;
                    signatureValue = null;
                    return false;
                }

                string? oidValue =
                    hashAlgorithmName == HashAlgorithmName.SHA1 ? Oids.DsaWithSha1 :
                    hashAlgorithmName == HashAlgorithmName.SHA256 ? Oids.DsaWithSha256 :
                    hashAlgorithmName == HashAlgorithmName.SHA384 ? Oids.DsaWithSha384 :
                    hashAlgorithmName == HashAlgorithmName.SHA512 ? Oids.DsaWithSha512 :
                    null;

                if (oidValue == null)
                {
                    signatureAlgorithm = null;
                    signatureValue = null;
                    return false;
                }

                signatureAlgorithm = oidValue;

#if NETCOREAPP || NETSTANDARD2_1
                // The Q size cannot be bigger than the KeySize.
                byte[] rented = CryptoPool.Rent(dsa.KeySize / 8);
                int bytesWritten = 0;

                try
                {
                    if (dsa.TryCreateSignature(dataHash, rented, out bytesWritten))
                    {
                        var signature = new ReadOnlySpan<byte>(rented, 0, bytesWritten);

                        if (key != null && !certificate.GetDSAPublicKey()!.VerifySignature(dataHash, signature))
                        {
                            // key did not match certificate
                            signatureValue = null;
                            return false;
                        }

                        signatureValue = DsaIeeeToDer(signature);
                        return true;
                    }
                }
                finally
                {
                    CryptoPool.Return(rented, bytesWritten);
                }

                signatureValue = null;
                return false;
#else
                byte[] signature = dsa.CreateSignature(dataHash);
                signatureValue = DsaIeeeToDer(new ReadOnlySpan<byte>(signature));
                return true;
#endif
            }
        }
    }
}
