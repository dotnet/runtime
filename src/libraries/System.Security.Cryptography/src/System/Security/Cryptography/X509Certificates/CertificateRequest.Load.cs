// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed partial class CertificateRequest
    {
        private const CertificateRequestLoadOptions AllOptions =
            CertificateRequestLoadOptions.SkipSignatureValidation |
            CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions;

        public static CertificateRequest LoadSigningRequestPem(
            string pkcs10Pem,
            HashAlgorithmName signerHashAlgorithm,
            CertificateRequestLoadOptions options = CertificateRequestLoadOptions.Default,
            RSASignaturePadding? signerSignaturePadding = null)
        {
            ArgumentNullException.ThrowIfNull(pkcs10Pem);

            return LoadSigningRequestPem(
                pkcs10Pem.AsSpan(),
                signerHashAlgorithm,
                options,
                signerSignaturePadding);
        }

        public static CertificateRequest LoadSigningRequestPem(
            ReadOnlySpan<char> pkcs10Pem,
            HashAlgorithmName signerHashAlgorithm,
            CertificateRequestLoadOptions options = CertificateRequestLoadOptions.Default,
            RSASignaturePadding? signerSignaturePadding = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(signerHashAlgorithm.Name, nameof(signerHashAlgorithm));

            if ((options & ~AllOptions) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), options, SR.Argument_InvalidFlag);
            }

            foreach ((ReadOnlySpan<char> contents, PemFields fields) in new PemEnumerator(pkcs10Pem))
            {
                if (contents[fields.Label].SequenceEqual(PemLabels.Pkcs10CertificateRequest))
                {
                    byte[] rented = ArrayPool<byte>.Shared.Rent(fields.DecodedDataLength);

                    if (!Convert.TryFromBase64Chars(contents[fields.Base64Data], rented, out int bytesWritten) ||
                        bytesWritten != fields.DecodedDataLength)
                    {
                        Debug.Fail("Base64Decode failed, but PemEncoding said it was legal");
                        throw new UnreachableException();
                    }

                    try
                    {
                        return LoadSigningRequest(
                            rented.AsSpan(0, bytesWritten),
                            permitTrailingData: false,
                            signerHashAlgorithm,
                            out _,
                            options,
                            signerSignaturePadding);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }
            }

            throw new CryptographicException(
                SR.Format(SR.Cryptography_NoPemOfLabel, PemLabels.Pkcs10CertificateRequest));
        }

        public static CertificateRequest LoadSigningRequest(
            byte[] pkcs10,
            HashAlgorithmName signerHashAlgorithm,
            CertificateRequestLoadOptions options = CertificateRequestLoadOptions.Default,
            RSASignaturePadding? signerSignaturePadding = null)
        {
            ArgumentNullException.ThrowIfNull(pkcs10);

            return LoadSigningRequest(
                pkcs10,
                permitTrailingData: false,
                signerHashAlgorithm,
                out _,
                options,
                signerSignaturePadding);
        }

        public static CertificateRequest LoadSigningRequest(
            ReadOnlySpan<byte> pkcs10,
            HashAlgorithmName signerHashAlgorithm,
            out int bytesConsumed,
            CertificateRequestLoadOptions options = CertificateRequestLoadOptions.Default,
            RSASignaturePadding? signerSignaturePadding = null)
        {
            return LoadSigningRequest(
                pkcs10,
                permitTrailingData: true,
                signerHashAlgorithm,
                out bytesConsumed,
                options,
                signerSignaturePadding);
        }

        private static unsafe CertificateRequest LoadSigningRequest(
            ReadOnlySpan<byte> pkcs10,
            bool permitTrailingData,
            HashAlgorithmName signerHashAlgorithm,
            out int bytesConsumed,
            CertificateRequestLoadOptions options,
            RSASignaturePadding? signerSignaturePadding)
        {
            ArgumentException.ThrowIfNullOrEmpty(signerHashAlgorithm.Name, nameof(signerHashAlgorithm));

            if ((options & ~AllOptions) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), options, SR.Argument_InvalidFlag);
            }

            bool skipSignatureValidation =
                (options & CertificateRequestLoadOptions.SkipSignatureValidation) != 0;

            bool unsafeLoadCertificateExtensions =
                (options & CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions) != 0;

            try
            {
                AsnValueReader outer = new AsnValueReader(pkcs10, AsnEncodingRules.DER);
                int encodedLength = outer.PeekEncodedValue().Length;

                AsnValueReader pkcs10Asn = outer.ReadSequence();
                CertificateRequest req;

                if (!permitTrailingData)
                {
                    outer.ThrowIfNotEmpty();
                }

                fixed (byte* p10ptr = pkcs10)
                {
                    using (PointerMemoryManager<byte> manager = new PointerMemoryManager<byte>(p10ptr, encodedLength))
                    {
                        ReadOnlyMemory<byte> rebind = manager.Memory;
                        ReadOnlySpan<byte> encodedRequestInfo = pkcs10Asn.PeekEncodedValue();
                        CertificationRequestInfoAsn requestInfo;
                        AlgorithmIdentifierAsn algorithmIdentifier;
                        ReadOnlySpan<byte> signature;
                        int signatureUnusedBitCount;

                        CertificationRequestInfoAsn.Decode(ref pkcs10Asn, rebind, out requestInfo);
                        AlgorithmIdentifierAsn.Decode(ref pkcs10Asn, rebind, out algorithmIdentifier);

                        if (!pkcs10Asn.TryReadPrimitiveBitString(out signatureUnusedBitCount, out signature))
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        pkcs10Asn.ThrowIfNotEmpty();

                        if (requestInfo.Version < 0)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        // They haven't bumped from v0 to v1 as of 2022.
                        const int MaxSupportedVersion = 0;

                        if (requestInfo.Version != MaxSupportedVersion)
                        {
                            throw new CryptographicException(
                                SR.Format(
                                    SR.Cryptography_CertReq_Load_VersionTooNew,
                                    requestInfo.Version,
                                    MaxSupportedVersion));
                        }

                        PublicKey publicKey = PublicKey.DecodeSubjectPublicKeyInfo(ref requestInfo.SubjectPublicKeyInfo);

                        if (!skipSignatureValidation)
                        {
                            // None of the supported signature algorithms support signatures that are not full bytes.
                            // So, shortcut the verification on the bit length
                            if (signatureUnusedBitCount != 0 ||
                                !VerifyX509Signature(encodedRequestInfo, signature, publicKey, algorithmIdentifier))
                            {
                                throw new CryptographicException(SR.Cryptography_CertReq_SignatureVerificationFailed);
                            }
                        }

                        X500DistinguishedName subject = new X500DistinguishedName(requestInfo.Subject.Span);

                        req = new CertificateRequest(
                            subject,
                            publicKey,
                            signerHashAlgorithm,
                            signerSignaturePadding);

                        if (requestInfo.Attributes is not null)
                        {
                            bool foundCertExt = false;

                            foreach (AttributeAsn attr in requestInfo.Attributes)
                            {
                                if (attr.AttrType == Oids.Pkcs9ExtensionRequest)
                                {
                                    if (foundCertExt)
                                    {
                                        throw new CryptographicException(
                                            SR.Cryptography_CertReq_Load_DuplicateExtensionRequests);
                                    }

                                    foundCertExt = true;

                                    if (attr.AttrValues.Length != 1)
                                    {
                                        throw new CryptographicException(
                                            SR.Cryptography_CertReq_Load_DuplicateExtensionRequests);
                                    }

                                    AsnValueReader extsReader = new AsnValueReader(
                                        attr.AttrValues[0].Span,
                                        AsnEncodingRules.DER);

                                    AsnValueReader exts = extsReader.ReadSequence();
                                    extsReader.ThrowIfNotEmpty();

                                    // Minimum length is 1, so do..while
                                    do
                                    {
                                        X509ExtensionAsn.Decode(ref exts, rebind, out X509ExtensionAsn extAsn);

                                        if (unsafeLoadCertificateExtensions)
                                        {
                                            X509Extension ext = new X509Extension(
                                                extAsn.ExtnId,
                                                extAsn.ExtnValue.Span,
                                                extAsn.Critical);

                                            X509Extension? rich =
                                                X509Certificate2.CreateCustomExtensionIfAny(extAsn.ExtnId);

                                            if (rich is not null)
                                            {
                                                rich.CopyFrom(ext);
                                                req.CertificateExtensions.Add(rich);
                                            }
                                            else
                                            {
                                                req.CertificateExtensions.Add(ext);
                                            }
                                        }
                                    } while (exts.HasData);
                                }
                                else
                                {
                                    if (attr.AttrValues.Length == 0)
                                    {
                                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                                    }

                                    foreach (ReadOnlyMemory<byte> val in attr.AttrValues)
                                    {
                                        req.OtherRequestAttributes.Add(
                                            new AsnEncodedData(attr.AttrType, val.Span));
                                    }
                                }
                            }
                        }
                    }
                }

                bytesConsumed = encodedLength;
                return req;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static bool VerifyX509Signature(
            ReadOnlySpan<byte> toBeSigned,
            ReadOnlySpan<byte> signature,
            PublicKey publicKey,
            AlgorithmIdentifierAsn algorithmIdentifier)
        {
            RSA? rsa = publicKey.GetRSAPublicKey();
            ECDsa? ecdsa = publicKey.GetECDsaPublicKey();

            try
            {
                HashAlgorithmName hashAlg;

                if (algorithmIdentifier.Algorithm == Oids.RsaPss)
                {
                    if (rsa is null || !algorithmIdentifier.Parameters.HasValue)
                    {
                        return false;
                    }

                    PssParamsAsn pssParams = PssParamsAsn.Decode(
                        algorithmIdentifier.Parameters.GetValueOrDefault(),
                        AsnEncodingRules.DER);

                    RSASignaturePadding padding = pssParams.GetSignaturePadding();
                    hashAlg = HashAlgorithmName.FromOid(pssParams.HashAlgorithm.Algorithm);

                    return rsa.VerifyData(
                        toBeSigned,
                        signature,
                        hashAlg,
                        padding);
                }

                switch (algorithmIdentifier.Algorithm)
                {
                    case Oids.RsaPkcs1Sha256:
                    case Oids.ECDsaWithSha256:
                        hashAlg = HashAlgorithmName.SHA256;
                        break;
                    case Oids.RsaPkcs1Sha384:
                    case Oids.ECDsaWithSha384:
                        hashAlg = HashAlgorithmName.SHA384;
                        break;
                    case Oids.RsaPkcs1Sha512:
                    case Oids.ECDsaWithSha512:
                        hashAlg = HashAlgorithmName.SHA512;
                        break;
                    case Oids.RsaPkcs1Sha1:
                    case Oids.ECDsaWithSha1:
                        hashAlg = HashAlgorithmName.SHA1;
                        break;
                    default:
                        throw new NotSupportedException(
                            SR.Format(SR.Cryptography_UnknownKeyAlgorithm, algorithmIdentifier.Algorithm));
                }

                // All remaining supported algorithms have no defined parameters
                if (!algorithmIdentifier.HasNullEquivalentParameters())
                {
                    return false;
                }

                switch (algorithmIdentifier.Algorithm)
                {
                    case Oids.RsaPkcs1Sha256:
                    case Oids.RsaPkcs1Sha384:
                    case Oids.RsaPkcs1Sha512:
                    case Oids.RsaPkcs1Sha1:
                        if (rsa is null)
                        {
                            return false;
                        }

                        return rsa.VerifyData(toBeSigned, signature, hashAlg, RSASignaturePadding.Pkcs1);
                    case Oids.ECDsaWithSha256:
                    case Oids.ECDsaWithSha384:
                    case Oids.ECDsaWithSha512:
                    case Oids.ECDsaWithSha1:
                        if (ecdsa is null)
                        {
                            return false;
                        }

                        return ecdsa.VerifyData(toBeSigned, signature, hashAlg, DSASignatureFormat.Rfc3279DerSequence);
                    default:
                        Debug.Fail(
                            $"Algorithm ID {algorithmIdentifier.Algorithm} was in the first switch, but not the second");
                        return false;
                }
            }
            catch (AsnContentException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
            finally
            {
                rsa?.Dispose();
                ecdsa?.Dispose();
            }
        }
    }
}
