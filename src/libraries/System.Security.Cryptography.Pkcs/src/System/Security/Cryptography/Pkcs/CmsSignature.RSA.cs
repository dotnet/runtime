// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    internal partial class CmsSignature
    {
        static partial void PrepareRegistrationRsa(Dictionary<string, CmsSignature> lookup)
        {
            lookup.Add(Oids.Rsa, new RSAPkcs1CmsSignature(null, null));
            lookup.Add(Oids.RsaPkcs1Sha1, new RSAPkcs1CmsSignature(Oids.RsaPkcs1Sha1, HashAlgorithmName.SHA1));
            lookup.Add(Oids.RsaPkcs1Sha256, new RSAPkcs1CmsSignature(Oids.RsaPkcs1Sha256, HashAlgorithmName.SHA256));
            lookup.Add(Oids.RsaPkcs1Sha384, new RSAPkcs1CmsSignature(Oids.RsaPkcs1Sha384, HashAlgorithmName.SHA384));
            lookup.Add(Oids.RsaPkcs1Sha512, new RSAPkcs1CmsSignature(Oids.RsaPkcs1Sha512, HashAlgorithmName.SHA512));
            lookup.Add(Oids.RsaPss, new RSAPssCmsSignature());
        }

        private abstract class RSACmsSignature : CmsSignature
        {
            private readonly string? _signatureAlgorithm;
            private readonly HashAlgorithmName? _expectedDigest;

            protected RSACmsSignature(string? signatureAlgorithm, HashAlgorithmName? expectedDigest)
            {
                _signatureAlgorithm = signatureAlgorithm;
                _expectedDigest = expectedDigest;
            }

            protected override bool VerifyKeyType(AsymmetricAlgorithm key)
            {
                return (key as RSA) != null;
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
                if (_expectedDigest.HasValue && _expectedDigest.Value != digestAlgorithmName)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_Cms_InvalidSignerHashForSignatureAlg,
                            digestAlgorithmOid,
                            _signatureAlgorithm));
                }

                RSASignaturePadding padding = GetSignaturePadding(
                    signatureParameters,
                    digestAlgorithmOid,
                    digestAlgorithmName,
                    valueHash.Length);

                RSA? publicKey = certificate.GetRSAPublicKey();

                if (publicKey == null)
                {
                    return false;
                }

                return publicKey.VerifyHash(
                    valueHash,
#if NETCOREAPP || NETSTANDARD2_1
                    signature.Span,
#else
                    signature,
#endif
                    digestAlgorithmName,
                    padding);
            }

            protected abstract RSASignaturePadding GetSignaturePadding(
                ReadOnlyMemory<byte>? signatureParameters,
                string? digestAlgorithmOid,
                HashAlgorithmName digestAlgorithmName,
                int digestValueLength);
        }

        private sealed class RSAPkcs1CmsSignature : RSACmsSignature
        {
            public RSAPkcs1CmsSignature(string? signatureAlgorithm, HashAlgorithmName? expectedDigest)
                : base(signatureAlgorithm, expectedDigest)
            {
            }

            protected override RSASignaturePadding GetSignaturePadding(
                ReadOnlyMemory<byte>? signatureParameters,
                string? digestAlgorithmOid,
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
                RSA certPublicKey = certificate.GetRSAPublicKey()!;

                // If there's no private key, fall back to the public key for a "no private key" exception.
                RSA? privateKey = key as RSA ??
                    PkcsPal.Instance.GetPrivateKeyForSigning<RSA>(certificate, silent) ??
                    certPublicKey;

                if (privateKey == null)
                {
                    signatureAlgorithm = null;
                    signatureValue = null;
                    return false;
                }

                signatureAlgorithm = Oids.Rsa;

#if NETCOREAPP || NETSTANDARD2_1
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

                    if (key != null && !certPublicKey.VerifyHash(dataHash, signatureValue, hashAlgorithmName, RSASignaturePadding.Pkcs1))
                    {
                        // key did not match certificate
                        signatureValue = null;
                        return false;
                    }

                    return true;
                }
#endif
                signatureValue = privateKey.SignHash(
#if NETCOREAPP || NETSTANDARD2_1
                    dataHash.ToArray(),
#else
                    dataHash,
#endif
                    hashAlgorithmName,
                    RSASignaturePadding.Pkcs1);

                if (key != null && !certPublicKey.VerifyHash(dataHash, signatureValue, hashAlgorithmName, RSASignaturePadding.Pkcs1))
                {
                    // key did not match certificate
                    signatureValue = null;
                    return false;
                }

                return true;
            }
        }

        private sealed class RSAPssCmsSignature : RSACmsSignature
        {
            public RSAPssCmsSignature() : base(null, null)
            {
            }

            protected override RSASignaturePadding GetSignaturePadding(
                ReadOnlyMemory<byte>? signatureParameters,
                string? digestAlgorithmOid,
                HashAlgorithmName digestAlgorithmName,
                int digestValueLength)
            {
                if (signatureParameters == null)
                {
                    throw new CryptographicException(SR.Cryptography_Pkcs_PssParametersMissing);
                }

                PssParamsAsn pssParams = PssParamsAsn.Decode(signatureParameters.Value, AsnEncodingRules.DER);

                if (pssParams.HashAlgorithm.Algorithm != digestAlgorithmOid)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_Pkcs_PssParametersHashMismatch,
                            pssParams.HashAlgorithm.Algorithm,
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

                if (pssParams.MaskGenAlgorithm.Algorithm != Oids.Mgf1)
                {
                    throw new CryptographicException(
                        SR.Cryptography_Pkcs_PssParametersMgfNotSupported,
                        pssParams.MaskGenAlgorithm.Algorithm);
                }

                if (pssParams.MaskGenAlgorithm.Parameters == null)
                {
                    throw new CryptographicException(SR.Cryptography_Pkcs_InvalidSignatureParameters);
                }

                AlgorithmIdentifierAsn mgfParams = AlgorithmIdentifierAsn.Decode(
                    pssParams.MaskGenAlgorithm.Parameters.Value,
                    AsnEncodingRules.DER);

                if (mgfParams.Algorithm != digestAlgorithmOid)
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_Pkcs_PssParametersMgfHashMismatch,
                            mgfParams.Algorithm,
                            digestAlgorithmOid));
                }

                // When RSASignaturePadding supports custom salt sizes this return will look different.
                return RSASignaturePadding.Pss;
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
                out string signatureAlgorithm,
                out byte[] signatureValue)
            {
                Debug.Fail("RSA-PSS requires building parameters, which has no API.");
                throw new CryptographicException();
            }
        }
    }
}
