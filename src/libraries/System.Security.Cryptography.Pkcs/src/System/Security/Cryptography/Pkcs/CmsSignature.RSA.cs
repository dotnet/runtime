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

            private protected static bool SignCore(
#if NETCOREAPP || NETSTANDARD2_1
                ReadOnlySpan<byte> dataHash,
#else
                byte[] dataHash,
#endif
                HashAlgorithmName hashAlgorithmName,
                X509Certificate2 certificate,
                AsymmetricAlgorithm? key,
                bool silent,
                RSASignaturePadding signaturePadding,
                [NotNullWhen(true)] out byte[]? signatureValue)
            {
                RSA certPublicKey = certificate.GetRSAPublicKey()!;

                // If there's no private key, fall back to the public key for a "no private key" exception.
                RSA? privateKey = key as RSA ??
                    PkcsPal.Instance.GetPrivateKeyForSigning<RSA>(certificate, silent) ??
                    certPublicKey;

                if (privateKey is null)
                {
                    signatureValue = null;
                    return false;
                }

#if NETCOREAPP || NETSTANDARD2_1
                byte[] signature = new byte[privateKey.KeySize / 8];

                bool signed = privateKey.TrySignHash(
                    dataHash,
                    signature,
                    hashAlgorithmName,
                    signaturePadding,
                    out int bytesWritten);

                if (signed && signature.Length == bytesWritten)
                {
                    signatureValue = signature;

                    if (key is not null && !certPublicKey.VerifyHash(dataHash, signatureValue, hashAlgorithmName, signaturePadding))
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
                    signaturePadding);

                if (key is not null && !certPublicKey.VerifyHash(dataHash, signatureValue, hashAlgorithmName, signaturePadding))
                {
                    // key did not match certificate
                    signatureValue = null;
                    return false;
                }

                return true;
            }
        }

        private sealed class RSAPkcs1CmsSignature : RSACmsSignature
        {
            internal override RSASignaturePadding? SignaturePadding => RSASignaturePadding.Pkcs1;

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
                [NotNullWhen(true)] out byte[]? signatureValue,
                out byte[]? signatureParameters)
            {
                bool result = SignCore(
                    dataHash,
                    hashAlgorithmName,
                    certificate,
                    key,
                    silent,
                    RSASignaturePadding.Pkcs1,
                    out signatureValue);

                signatureAlgorithm = result ? Oids.Rsa : null;
                signatureParameters = null;
                return result;
            }
        }

        private sealed class RSAPssCmsSignature : RSACmsSignature
        {
            // SEQUENCE
            private static readonly byte[] s_rsaPssSha1Parameters = new byte[] { 0x30, 0x00 };

            // SEQUENCE
            //  [0]
            //    SEQUENCE
            //      OBJECT IDENTIFIER 2.16.840.1.101.3.4.2.1
            //  [1]
            //    SEQUENCE
            //      OBJECT IDENTIFIER 1.2.840.113549.1.1.8
            //      SEQUENCE
            //        OBJECT IDENTIFIER 2.16.840.1.101.3.4.2.1
            //  [2]
            //    INTEGER 32
            private static readonly byte[] s_rsaPssSha256Parameters = new byte[] {
                0x30, 0x30, 0xA0, 0x0D, 0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02,
                0x01, 0xA1, 0x1A, 0x30, 0x18, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08,
                0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0xA2, 0x03, 0x02,
                0x01, 0x20,
            };

            // SEQUENCE
            //  [0]
            //    SEQUENCE
            //      OBJECT IDENTIFIER 2.16.840.1.101.3.4.2.2
            //  [1]
            //    SEQUENCE
            //      OBJECT IDENTIFIER 1.2.840.113549.1.1.8
            //      SEQUENCE
            //        OBJECT IDENTIFIER 2.16.840.1.101.3.4.2.2
            //  [2]
            //    INTEGER 48
            private static readonly byte[] s_rsaPssSha384Parameters = new byte[] {
                0x30, 0x30, 0xA0, 0x0D, 0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02,
                0x02, 0xA1, 0x1A, 0x30, 0x18, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08,
                0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0xA2, 0x03, 0x02,
                0x01, 0x30,
            };

            // SEQUENCE
            //  [0]
            //    SEQUENCE
            //      OBJECT IDENTIFIER 2.16.840.1.101.3.4.2.3
            //  [1]
            //    SEQUENCE
            //      OBJECT IDENTIFIER 1.2.840.113549.1.1.8
            //      SEQUENCE
            //        OBJECT IDENTIFIER 2.16.840.1.101.3.4.2.3
            //  [2]
            //    INTEGER 64
            private static readonly byte[] s_rsaPssSha512Parameters = new byte[] {
                0x30, 0x30, 0xA0, 0x0D, 0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02,
                0x03, 0xA1, 0x1A, 0x30, 0x18, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08,
                0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0xA2, 0x03, 0x02,
                0x01, 0x40,
            };

            internal override RSASignaturePadding? SignaturePadding => RSASignaturePadding.Pss;

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
                [NotNullWhen(true)] out string? signatureAlgorithm,
                [NotNullWhen(true)] out byte[]? signatureValue,
                out byte[]? signatureParameters)
            {
                bool result = SignCore(
                    dataHash,
                    hashAlgorithmName,
                    certificate,
                    key,
                    silent,
                    RSASignaturePadding.Pss,
                    out signatureValue);

                if (result)
                {
                    signatureAlgorithm = Oids.RsaPss;

                    if (hashAlgorithmName == HashAlgorithmName.SHA1)
                    {
                        signatureParameters = s_rsaPssSha1Parameters;
                    }
                    else if (hashAlgorithmName == HashAlgorithmName.SHA256)
                    {
                        signatureParameters = s_rsaPssSha256Parameters;
                    }
                    else if (hashAlgorithmName == HashAlgorithmName.SHA384)
                    {
                        signatureParameters = s_rsaPssSha384Parameters;
                    }
                    else if (hashAlgorithmName == HashAlgorithmName.SHA512)
                    {
                        signatureParameters = s_rsaPssSha512Parameters;
                    }
                    else
                    {
                        // The only hash algorithm we don't support is MD5.
                        // We shouldn't get here with anything other than MD5.
                        Debug.Assert(hashAlgorithmName == HashAlgorithmName.MD5, $"Unsupported digest algorithm '{hashAlgorithmName.Name}'");
                        signatureAlgorithm = null;
                        signatureParameters = null;
                        return false;
                    }
                }
                else
                {
                    signatureAlgorithm = null;
                    signatureParameters = null;
                }

                return result;
            }
        }
    }
}
