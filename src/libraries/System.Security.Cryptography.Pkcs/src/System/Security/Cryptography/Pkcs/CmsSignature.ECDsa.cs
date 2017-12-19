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
        static partial void PrepareRegistrationECDsa(Dictionary<string, CmsSignature> lookup)
        {
            lookup.Add(Oids.ECDsaWithSha1, new ECDsaCmsSignature(Oids.ECDsaWithSha1, HashAlgorithmName.SHA1));
            lookup.Add(Oids.ECDsaWithSha256, new ECDsaCmsSignature(Oids.ECDsaWithSha256, HashAlgorithmName.SHA256));
            lookup.Add(Oids.ECDsaWithSha384, new ECDsaCmsSignature(Oids.ECDsaWithSha384, HashAlgorithmName.SHA384));
            lookup.Add(Oids.ECDsaWithSha512, new ECDsaCmsSignature(Oids.ECDsaWithSha512, HashAlgorithmName.SHA512));
            lookup.Add(Oids.EcPublicKey, new ECDsaCmsSignature(null, default));
        }

        private partial class ECDsaCmsSignature : CmsSignature
        {
            private readonly HashAlgorithmName _expectedDigest;
            private readonly string _signatureAlgorithm;

            internal ECDsaCmsSignature(string signatureAlgorithm, HashAlgorithmName expectedDigest)
            {
                _signatureAlgorithm = signatureAlgorithm;
                _expectedDigest = expectedDigest;
            }

            internal override bool VerifySignature(
#if netcoreapp
                ReadOnlySpan<byte> valueHash,
                ReadOnlyMemory<byte> signature,
#else
                byte[] valueHash,
                byte[] signature,
#endif
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

                ECDsa key = certificate.GetECDsaPublicKey();

                if (key == null)
                {
                    return false;
                }

                // 2 * KeySizeBytes => 2 * KeySizeBits / 8 => KeySizeBits / 4
                int bufSize = key.KeySize / 4;

#if netcoreapp
                ArrayPool<byte> pool = ArrayPool<byte>.Shared;
                byte[] rented = pool.Rent(bufSize);
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
#if netcoreapp
                }
                finally
                {
                    ieee.Clear();
                    pool.Return(rented);
                }
#endif
            }

            protected override bool Sign(
#if netcoreapp
                ReadOnlySpan<byte> dataHash,
#else
                byte[] dataHash,
#endif
                HashAlgorithmName hashAlgorithmName,
                X509Certificate2 certificate,
                bool silent,
                out Oid signatureAlgorithm,
                out byte[] signatureValue)
            {
                // If there's no private key, fall back to the public key for a "no private key" exception.
                ECDsa key =
                    PkcsPal.Instance.GetPrivateKeyForSigning<ECDsa>(certificate, silent) ??
                    certificate.GetECDsaPublicKey();

                if (key == null)
                {
                    signatureAlgorithm = null;
                    signatureValue = null;
                    return false;
                }

                string oidValue =
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

                signatureAlgorithm = new Oid(oidValue, oidValue);

#if netcoreapp
                ArrayPool<byte> pool = ArrayPool<byte>.Shared;
                // The signature size is twice the KeySize.
                // 2 * KeySizeInBytes => 2 * KeySizeInBits / 8 => KeySizeInBits / 4
                byte[] rented = pool.Rent(key.KeySize / 4);
                int bytesWritten = 0;

                try
                {
                    if (key.TrySignHash(dataHash, rented, out bytesWritten))
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
#endif

                signatureValue = DsaIeeeToDer(key.SignHash(
#if netcoreapp
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
