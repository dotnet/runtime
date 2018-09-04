// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class Rfc3161TimestampToken
    {
        private SignedCms _parsedDocument;
        private SignerInfo _signerInfo;
        private EssCertId? _essCertId;
        private EssCertIdV2? _essCertIdV2;

        public Rfc3161TimestampTokenInfo TokenInfo { get; private set; }

        private Rfc3161TimestampToken()
        {
        }

        /// <summary>
        /// Get a SignedCms representation of the RFC3161 Timestamp Token.
        /// </summary>
        /// <returns>The SignedCms representation of the RFC3161 Timestamp Token.</returns>
        /// <remarks>
        /// Successive calls to this method return the same object.
        /// The SignedCms class is mutable, but changes to that object are not reflected in the
        /// <see cref="Rfc3161TimestampToken"/> object which produced it.
        /// The value from calling <see cref="SignedCms.Encode"/> can be interpreted again as an
        /// <see cref="Rfc3161TimestampToken"/> via another call to <see cref="TryDecode"/>.
        /// </remarks>
        public SignedCms AsSignedCms() => _parsedDocument;

        private X509Certificate2 GetSignerCertificate(X509Certificate2Collection extraCandidates)
        {
            Debug.Assert(_signerInfo != null, "_signerInfo != null");
            X509Certificate2 signerCert = _signerInfo.Certificate;

            if (signerCert != null)
            {
                if (CheckCertificate(signerCert, _signerInfo, in _essCertId, in _essCertIdV2, TokenInfo))
                {
                    return signerCert;
                }

                // SignedCms will not try another certificate in this state, so just fail.
                return null;
            }

            if (extraCandidates == null || extraCandidates.Count == 0)
            {
                return null;
            }

            foreach (X509Certificate2 candidate in extraCandidates)
            {
                if (CheckCertificate(candidate, _signerInfo, in _essCertId, in _essCertIdV2, TokenInfo))
                {
                    return candidate;
                }
            }

            return null;
        }

        public bool VerifySignatureForData(
            ReadOnlySpan<byte> data,
            out X509Certificate2 signerCertificate,
            X509Certificate2Collection extraCandidates = null)
        {
            signerCertificate = null;

            X509Certificate2 cert = GetSignerCertificate(extraCandidates);

            if (cert == null)
            {
                return false;
            }

            bool ret = VerifyData(data);

            if (ret)
            {
                signerCertificate = cert;
            }

            return ret;
        }

        public bool VerifySignatureForHash(
            ReadOnlySpan<byte> hash,
            HashAlgorithmName hashAlgorithm,
            out X509Certificate2 signerCertificate,
            X509Certificate2Collection extraCandidates = null)
        {
            signerCertificate = null;

            X509Certificate2 cert = GetSignerCertificate(extraCandidates);

            if (cert == null)
            {
                return false;
            }

            bool ret = VerifyHash(hash, PkcsHelpers.GetOidFromHashAlgorithm(hashAlgorithm));

            if (ret)
            {
                signerCertificate = cert;
            }

            return ret;
        }

        public bool VerifySignatureForHash(
            ReadOnlySpan<byte> hash,
            Oid hashAlgorithmId,
            out X509Certificate2 signerCertificate,
            X509Certificate2Collection extraCandidates = null)
        {
            if (hashAlgorithmId == null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithmId));
            }

            signerCertificate = null;

            X509Certificate2 cert = GetSignerCertificate(extraCandidates);

            if (cert == null)
            {
                return false;
            }

            bool ret = VerifyHash(hash, hashAlgorithmId.Value);

            if (ret)
            {
                // REVIEW: Should this return the cert, or new X509Certificate2(cert.RawData)?
                // SignedCms.SignerInfos builds new objects each call, which makes
                // ReferenceEquals(cms.SignerInfos[0].Certificate, cms.SignerInfos[0].Certificate) be false.
                // So maybe it's weird to give back a cert we've copied from that?
                signerCertificate = cert;
            }

            return ret;
        }

        public bool VerifySignatureForSignerInfo(
            SignerInfo signerInfo,
            out X509Certificate2 signerCertificate,
            X509Certificate2Collection extraCandidates = null)
        {
            if (signerInfo == null)
            {
                throw new ArgumentNullException(nameof(signerInfo));
            }

            return VerifySignatureForData(
                signerInfo.GetSignatureMemory().Span,
                out signerCertificate,
                extraCandidates);
        }

        internal bool VerifyHash(ReadOnlySpan<byte> hash, string hashAlgorithmId)
        {
            return
                hash.SequenceEqual(TokenInfo.GetMessageHash().Span) &&
                hashAlgorithmId == TokenInfo.HashAlgorithmId.Value;
        }

        private bool VerifyData(ReadOnlySpan<byte> data)
        {
            Oid hashAlgorithmId = TokenInfo.HashAlgorithmId;
            HashAlgorithmName hashAlgorithmName = PkcsHelpers.GetDigestAlgorithm(hashAlgorithmId);

            using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithmName))
            {
                hasher.AppendData(data);

                // SHA-2-512 is the biggest hash we currently know about.
                Span<byte> stackSpan = stackalloc byte[512 / 8];

                if (hasher.TryGetHashAndReset(stackSpan, out int bytesWritten))
                {
                    return VerifyHash(stackSpan.Slice(0, bytesWritten), hashAlgorithmId.Value);
                }

                // Something we understood, but is bigger than 512-bit.
                // Allocate at runtime, trip in a debug build so we can re-evaluate this.
                Debug.Fail(
                    $"TryGetHashAndReset did not fit in {stackSpan.Length} for hash {hashAlgorithmId.Value}");

                return VerifyHash(hasher.GetHashAndReset(), hashAlgorithmId.Value);
            }
        }

        private static bool CheckCertificate(
            X509Certificate2 tsaCertificate,
            SignerInfo signer,
            in EssCertId? certId,
            in EssCertIdV2? certId2,
            Rfc3161TimestampTokenInfo tokenInfo)
        {
            Debug.Assert(tsaCertificate != null);
            Debug.Assert(signer != null);
            Debug.Assert(tokenInfo != null);
            // certId and certId2 are allowed to be null, they get checked in CertMatchesIds.

            if (!CertMatchesIds(tsaCertificate, certId, certId2))
            {
                return false;
            }

            // Nothing in RFC3161 actually mentions checking the certificate's validity
            // against the TSTInfo timestamp value, but it seems sensible.
            //
            // Accuracy is ignored here, for better replicability in user code.

            if (tsaCertificate.NotAfter < tokenInfo.Timestamp ||
                tsaCertificate.NotBefore > tokenInfo.Timestamp)
            {
                return false;
            }

            // https://tools.ietf.org/html/rfc3161#section-2.3
            //
            // The TSA MUST sign each time-stamp message with a key reserved
            // specifically for that purpose.  A TSA MAY have distinct private keys,
            // e.g., to accommodate different policies, different algorithms,
            // different private key sizes or to increase the performance. The
            // corresponding certificate MUST contain only one instance of the
            // extended key usage field extension as defined in [RFC2459] Section
            // 4.2.1.13 with KeyPurposeID having value:
            //
            // id-kp-timeStamping. This extension MUST be critical.

            using (var ekuExts = tsaCertificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().GetEnumerator())
            {
                if (!ekuExts.MoveNext())
                {
                    return false;
                }

                X509EnhancedKeyUsageExtension ekuExt = ekuExts.Current;

                if (!ekuExt.Critical)
                {
                    return false;
                }

                bool hasPurpose = false;

                foreach (Oid oid in ekuExt.EnhancedKeyUsages)
                {
                    if (oid.Value == Oids.TimeStampingPurpose)
                    {
                        hasPurpose = true;
                        break;
                    }
                }

                if (!hasPurpose)
                {
                    return false;
                }

                if (ekuExts.MoveNext())
                {
                    return false;
                }
            }

            try
            {
                signer.CheckSignature(new X509Certificate2Collection(tsaCertificate), true);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static bool TryDecode(ReadOnlyMemory<byte> source, out Rfc3161TimestampToken token, out int bytesConsumed)
        {
            bytesConsumed = 0;
            token = null;

            try
            {
                AsnReader reader = new AsnReader(source, AsnEncodingRules.BER);
                int bytesActuallyRead = reader.PeekEncodedValue().Length;
                
                ContentInfoAsn.Decode(
                    reader,
                    out ContentInfoAsn contentInfo);

                // https://tools.ietf.org/html/rfc3161#section-2.4.2
                //
                // A TimeStampToken is as follows.  It is defined as a ContentInfo
                // ([CMS]) and SHALL encapsulate a signed data content type.
                //
                // TimeStampToken::= ContentInfo
                //   --contentType is id-signedData([CMS])
                //   --content is SignedData ([CMS])
                if (contentInfo.ContentType != Oids.Pkcs7Signed)
                {
                    return false;
                }

                SignedCms cms = new SignedCms();
                cms.Decode(source);

                // The fields of type EncapsulatedContentInfo of the SignedData
                // construct have the following meanings:
                //
                // eContentType is an object identifier that uniquely specifies the
                // content type.  For a time-stamp token it is defined as:
                //
                // id-ct-TSTInfo  OBJECT IDENTIFIER ::= { iso(1) member-body(2)
                // us(840) rsadsi(113549) pkcs(1) pkcs-9(9) smime(16) ct(1) 4}
                //
                // eContent is the content itself, carried as an octet string.
                // The eContent SHALL be the DER-encoded value of TSTInfo.
                if (cms.ContentInfo.ContentType.Value != Oids.TstInfo)
                {
                    return false;
                }

                // RFC3161:
                // The time-stamp token MUST NOT contain any signatures other than the
                // signature of the TSA.  The certificate identifier (ESSCertID) of the
                // TSA certificate MUST be included as a signerInfo attribute inside a
                // SigningCertificate attribute.

                // RFC5816 says that ESSCertIDv2 should be allowed instead.

                SignerInfoCollection signerInfos = cms.SignerInfos;

                if (signerInfos.Count != 1)
                {
                    return false;
                }

                SignerInfo signer = signerInfos[0];
                EssCertId? certId;
                EssCertIdV2? certId2;

                if (!TryGetCertIds(signer, out certId, out certId2))
                {
                    return false;
                }

                X509Certificate2 signerCert = signer.Certificate;

                if (signerCert == null &&
                    signer.SignerIdentifier.Type == SubjectIdentifierType.IssuerAndSerialNumber)
                {
                    // If the cert wasn't provided, but the identifier was IssuerAndSerialNumber,
                    // and the ESSCertId(V2) has specified an issuerSerial value, ensure it's a match.
                    X509IssuerSerial issuerSerial = (X509IssuerSerial)signer.SignerIdentifier.Value;

                    if (certId.HasValue && certId.Value.IssuerSerial != null)
                    {
                        if (!IssuerAndSerialMatch(
                            certId.Value.IssuerSerial.Value,
                            issuerSerial.IssuerName,
                            issuerSerial.SerialNumber))
                        {
                            return false;
                        }
                    }

                    if (certId2.HasValue && certId2.Value.IssuerSerial != null)
                    {
                        if (!IssuerAndSerialMatch(
                            certId2.Value.IssuerSerial.Value,
                            issuerSerial.IssuerName,
                            issuerSerial.SerialNumber))
                        {
                            return false;
                        }
                    }
                }

                Rfc3161TimestampTokenInfo tokenInfo;

                if (Rfc3161TimestampTokenInfo.TryDecode(cms.ContentInfo.Content, out tokenInfo, out _))
                {
                    if (signerCert != null &&
                        !CheckCertificate(signerCert, signer, in certId, in certId2, tokenInfo))
                    {
                        return false;
                    }

                    token = new Rfc3161TimestampToken
                    {
                        _parsedDocument = cms,
                        _signerInfo = signer,
                        _essCertId = certId,
                        _essCertIdV2 = certId2,
                        TokenInfo = tokenInfo,
                    };

                    bytesConsumed = bytesActuallyRead;
                    return true;
                }
            }
            catch (CryptographicException)
            {
            }

            return false;
        }

        private static bool IssuerAndSerialMatch(
            CadesIssuerSerial issuerSerial,
            string issuerDirectoryName,
            string serialNumber)
        {
            GeneralNameAsn[] issuerNames = issuerSerial.Issuer;

            if (issuerNames == null || issuerNames.Length != 1)
            {
                return false;
            }

            GeneralNameAsn requiredName = issuerNames[0];

            if (requiredName.DirectoryName == null)
            {
                return false;
            }

            if (issuerDirectoryName != new X500DistinguishedName(requiredName.DirectoryName.Value.ToArray()).Name)
            {
                return false;
            }

            return serialNumber == issuerSerial.SerialNumber.Span.ToBigEndianHex();
        }

        private static bool IssuerAndSerialMatch(
            CadesIssuerSerial issuerSerial,
            ReadOnlySpan<byte> issuerDirectoryName,
            ReadOnlySpan<byte> serialNumber)
        {
            GeneralNameAsn[] issuerNames = issuerSerial.Issuer;

            if (issuerNames == null || issuerNames.Length != 1)
            {
                return false;
            }

            GeneralNameAsn requiredName = issuerNames[0];

            if (requiredName.DirectoryName == null)
            {
                return false;
            }

            if (!requiredName.DirectoryName.Value.Span.SequenceEqual(issuerDirectoryName))
            {
                return false;
            }

            return serialNumber.SequenceEqual(issuerSerial.SerialNumber.Span);
        }

        private static bool CertMatchesIds(X509Certificate2 signerCert, in EssCertId? certId, in EssCertIdV2? certId2)
        {
            Debug.Assert(signerCert != null);
            Debug.Assert(certId.HasValue || certId2.HasValue);
            byte[] serialNumber = null;

            if (certId.HasValue)
            {
                Span<byte> thumbprint = stackalloc byte[20];

                if (!signerCert.TryGetCertHash(HashAlgorithmName.SHA1, thumbprint, out int written) ||
                    written != thumbprint.Length ||
                    !thumbprint.SequenceEqual(certId.Value.Hash.Span))
                {
                    return false;
                }

                if (certId.Value.IssuerSerial.HasValue)
                {
                    serialNumber = signerCert.GetSerialNumber();
                    Array.Reverse(serialNumber);

                    if (!IssuerAndSerialMatch(
                        certId.Value.IssuerSerial.Value,
                        signerCert.IssuerName.RawData,
                        serialNumber))
                    {
                        return false;
                    }
                }
            }

            if (certId2.HasValue)
            {
                HashAlgorithmName alg;
                // SHA-2-512 is the biggest we know about.
                Span<byte> thumbprint = stackalloc byte[512 / 8];

                try
                {
                    alg = PkcsHelpers.GetDigestAlgorithm(certId2.Value.HashAlgorithm.Algorithm);

                    if (signerCert.TryGetCertHash(alg, thumbprint, out int written))
                    {
                        thumbprint = thumbprint.Slice(0, written);
                    }
                    else
                    {
                        Debug.Fail(
                            $"TryGetCertHash did not fit in {thumbprint.Length} for hash {certId2.Value.HashAlgorithm.Algorithm.Value}");

                        thumbprint = signerCert.GetCertHash(alg);
                    }
                }
                catch (CryptographicException)
                {
                    return false;
                }

                if (!thumbprint.SequenceEqual(certId2.Value.Hash.Span))
                {
                    return false;
                }

                if (certId2.Value.IssuerSerial.HasValue)
                {
                    if (serialNumber == null)
                    {
                        serialNumber = signerCert.GetSerialNumber();
                        Array.Reverse(serialNumber);
                    }

                    if (!IssuerAndSerialMatch(
                        certId2.Value.IssuerSerial.Value,
                        signerCert.IssuerName.RawData,
                        serialNumber))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryGetCertIds(SignerInfo signer, out EssCertId? certId, out EssCertIdV2? certId2)
        {
            // RFC 5035 says that SigningCertificateV2 (contains ESSCertIDv2) is a signed
            // attribute, with OID 1.2.840.113549.1.9.16.2.47, and that it must not be multiply defined.

            // RFC 2634 says that SigningCertificate (contains ESSCertID) is a signed attribute,
            // with OID 1.2.840.113549.1.9.16.2.12, and that it must not be multiply defined.
            certId = null;
            certId2 = null;

            foreach (CryptographicAttributeObject attrSet in signer.SignedAttributes)
            {
                string setOid = attrSet.Oid?.Value;

                if (setOid != null &&
                    setOid != Oids.SigningCertificate &&
                    setOid != Oids.SigningCertificateV2)
                {
                    continue;
                }

                foreach (AsnEncodedData attr in attrSet.Values)
                {
                    string attrOid = attr.Oid?.Value;

                    if (attrOid == Oids.SigningCertificate)
                    {
                        if (certId != null)
                        {
                            return false;
                        }

                        try
                        {
                            SigningCertificateAsn signingCert = SigningCertificateAsn.Decode(
                                attr.RawData,
                                AsnEncodingRules.BER);

                            if (signingCert.Certs.Length < 1)
                            {
                                return false;
                            }

                            // The first one is the signing cert, the rest constrain the chain.
                            certId = signingCert.Certs[0];
                        }
                        catch (CryptographicException)
                        {
                            return false;
                        }
                    }

                    if (attrOid == Oids.SigningCertificateV2)
                    {
                        if (certId2 != null)
                        {
                            return false;
                        }

                        try
                        {
                            SigningCertificateV2Asn signingCert = SigningCertificateV2Asn.Decode(
                                attr.RawData,
                                AsnEncodingRules.BER);

                            if (signingCert.Certs.Length < 1)
                            {
                                return false;
                            }

                            // The first one is the signing cert, the rest constrain the chain.
                            certId2 = signingCert.Certs[0];
                        }
                        catch (CryptographicException)
                        {
                            return false;
                        }
                    }
                }
            }

            return certId2 != null || certId != null;
        }
    }
}
