// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    internal partial class CmsSignature
    {
        static partial void PrepareRegistrationRsa(Dictionary<string, CmsSignature> lookup)
        {
            lookup.Add(Oids.Rsa, new RSAPkcs1CmsSignature());
            lookup.Add(Oids.RsaPss, new RSAPssCmsSignature());
        }

        private abstract class RSACmsSignature : CmsSignature
        {
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
                RSASignaturePadding padding = GetSignaturePadding(
                    signatureParameters,
                    digestAlgorithmOid,
                    digestAlgorithmName,
                    valueHash.Length);

                RSA publicKey = certificate.GetRSAPublicKey();

                if (publicKey == null)
                {
                    return false;
                }

                return publicKey.VerifyHash(
                    valueHash,
#if netcoreapp
                    signature.Span,
#else
                    signature,
#endif
                    digestAlgorithmName,
                    padding);
            }

            protected abstract RSASignaturePadding GetSignaturePadding(
                ReadOnlyMemory<byte>? signatureParameters,
                string digestAlgorithmOid,
                HashAlgorithmName digestAlgorithmName,
                int digestValueLength);
        }

        private sealed class RSAPkcs1CmsSignature : RSACmsSignature
        {
            protected override RSASignaturePadding GetSignaturePadding(
                ReadOnlyMemory<byte>? signatureParameters,
                string digestAlgorithmOid,
                HashAlgorithmName digestAlgorithmName,
                int digestValueLength)
            {
                if (signatureParameters == null)
                {
                    return RSASignaturePadding.Pkcs1;
                }

                Span<byte> expectedParameters = stackalloc byte[2];
                expectedParameters[0] = 0x05;
                expectedParameters[1] = 0x00;

                if (expectedParameters.SequenceEqual(signatureParameters.Value.Span))
                {
                    return RSASignaturePadding.Pkcs1;
                }

                throw new CryptographicException(SR.Cryptography_Pkcs_InvalidSignatureParameters);
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
                RSA privateKey =
                    PkcsPal.Instance.GetPrivateKeyForSigning<RSA>(certificate, silent) ??
                    certificate.GetRSAPublicKey();

                if (privateKey == null)
                {
                    signatureAlgorithm = null;
                    signatureValue = null;
                    return false;
                }

                signatureAlgorithm = new Oid(Oids.Rsa, Oids.Rsa);

#if netcoreapp
                byte[] signature = new byte[privateKey.KeySize / 8];

                bool signed = privateKey.TrySignHash(
                    dataHash,
                    signature,
                    hashAlgorithmName,
                    RSASignaturePadding.Pkcs1,
                    out int bytesWritten);

                if (signed && signature.Length == bytesWritten)
                {
                    signatureValue = signature;
                    return true;
                }
#endif
                signatureValue = privateKey.SignHash(
#if netcoreapp
                    dataHash.ToArray(),
#else
                    dataHash,
#endif
                    hashAlgorithmName,
                    RSASignaturePadding.Pkcs1);

                return true;
            }
        }

        private class RSAPssCmsSignature : RSACmsSignature
        {
            protected override RSASignaturePadding GetSignaturePadding(
                ReadOnlyMemory<byte>? signatureParameters,
                string digestAlgorithmOid,
                HashAlgorithmName digestAlgorithmName,
                int digestValueLength)
            {
                if (signatureParameters == null)
                {
                    throw new CryptographicException(SR.Cryptography_Pkcs_PssParametersMissing);
                }

                PssParamsAsn pssParams =
                    AsnSerializer.Deserialize<PssParamsAsn>(signatureParameters.Value, AsnEncodingRules.DER);

                if (pssParams.HashAlgorithm.Algorithm.Value != digestAlgorithmOid)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_Pkcs_PssParametersHashMismatch,
                            pssParams.HashAlgorithm.Algorithm.Value,
                            digestAlgorithmOid));
                }

                if (pssParams.TrailerField != 1)
                {
                    throw new CryptographicException(SR.Cryptography_Pkcs_InvalidSignatureParameters);
                }

                if (pssParams.SaltLength != digestValueLength)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_Pkcs_PssParametersSaltMismatch,
                            pssParams.SaltLength,
                            digestAlgorithmName.Name));
                }

                if (pssParams.MaskGenAlgorithm.Algorithm.Value != Oids.Mgf1)
                {
                    throw new CryptographicException(
                        SR.Cryptography_Pkcs_PssParametersMgfNotSupported,
                        pssParams.MaskGenAlgorithm.Algorithm.Value);
                }

                if (pssParams.MaskGenAlgorithm.Parameters == null)
                {
                    throw new CryptographicException(SR.Cryptography_Pkcs_InvalidSignatureParameters);
                }

                AlgorithmIdentifierAsn mgfParams = AsnSerializer.Deserialize<AlgorithmIdentifierAsn>(
                    pssParams.MaskGenAlgorithm.Parameters.Value,
                    AsnEncodingRules.DER);

                if (mgfParams.Algorithm.Value != digestAlgorithmOid)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_Pkcs_PssParametersMgfHashMismatch,
                            mgfParams.Algorithm.Value,
                            digestAlgorithmOid));
                }

                // When RSASignaturePadding supports custom salt sizes this return will look different.
                return RSASignaturePadding.Pss;
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
                Debug.Fail("RSA-PSS requires building parameters, which has no API.");
                throw new CryptographicException();
            }
        }
    }
}
