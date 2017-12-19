// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

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
            lookup.Add(Oids.DsaPublicKey, new DSACmsSignature(null, default));
        }

        private class DSACmsSignature : CmsSignature
        {
            private readonly HashAlgorithmName _expectedDigest;
            private readonly string _signatureAlgorithm;

            internal DSACmsSignature(string signatureAlgorithm, HashAlgorithmName expectedDigest)
            {
                _signatureAlgorithm = signatureAlgorithm;
                _expectedDigest = expectedDigest;
            }

            internal override bool VerifySignature(
                ReadOnlySpan<byte> valueHash,
                ReadOnlyMemory<byte> signature,
                string digestAlgorithmOid,
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

                DSA dsa = certificate.GetDSAPublicKey();

                if (dsa == null)
                {
                    return false;
                }

                DSAParameters dsaParameters = dsa.ExportParameters(false);
                int bufSize = 2 * dsaParameters.Q.Length;

                ArrayPool<byte> pool = ArrayPool<byte>.Shared;
                byte[] rented = pool.Rent(bufSize);
                Span<byte> ieee = new Span<byte>(rented, 0, bufSize);

                try
                {
                    if (!DsaDerToIeee(signature, ieee))
                    {
                        return false;
                    }

                    return dsa.VerifySignature(valueHash, ieee);
                }
                finally
                {
                    ieee.Clear();
                    pool.Return(rented);
                }
            }

            protected override bool Sign(
                ReadOnlySpan<byte> dataHash,
                HashAlgorithmName hashAlgorithmName,
                X509Certificate2 certificate,
                bool silent,
                out Oid signatureAlgorithm,
                out byte[] signatureValue)
            {
                // If there's no private key, fall back to the public key for a "no private key" exception.
                DSA dsa =
                    PkcsPal.Instance.GetPrivateKeyForSigning<DSA>(certificate, silent) ??
                    certificate.GetDSAPublicKey();

                if (dsa == null)
                {
                    signatureAlgorithm = null;
                    signatureValue = null;
                    return false;
                }

                string oidValue =
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

                signatureAlgorithm = new Oid(oidValue, oidValue);

                ArrayPool<byte> pool = ArrayPool<byte>.Shared;
                // The Q size cannot be bigger than the KeySize.
                byte[] rented = pool.Rent(dsa.KeySize / 8);
                int bytesWritten = 0;

                try
                {
                    if (dsa.TryCreateSignature(dataHash, rented, out bytesWritten))
                    {
                        signatureValue = DsaIeeeToDer(new ReadOnlySpan<byte>(rented, 0, bytesWritten));
                        return true;
                    }
                }
                finally
                {
                    Array.Clear(rented, 0, bytesWritten);
                    pool.Return(rented);
                }

                signatureValue = null;
                return false;
            }
        }
    }
}
