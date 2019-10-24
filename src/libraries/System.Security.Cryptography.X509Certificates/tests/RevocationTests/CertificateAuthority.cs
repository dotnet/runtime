// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Asn1;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.RevocationTests
{
    // This class represents only a portion of what is required to be a proper Certificate Authority.
    //
    // Please do not use it as the basis for any real Public/Private Key Infrastructure (PKI) system
    // without understanding all of the portions of proper CA management that you're skipping.
    //
    // At minimum, read the current baseline requirements of the CA/Browser Forum.
    internal sealed class CertificateAuthority : IDisposable
    {
        private static readonly Asn1Tag s_context0 = new Asn1Tag(TagClass.ContextSpecific, 0);
        private static readonly Asn1Tag s_context1 = new Asn1Tag(TagClass.ContextSpecific, 1);
        private static readonly Asn1Tag s_context2 = new Asn1Tag(TagClass.ContextSpecific, 2);
        private static readonly Asn1Tag s_context4 = new Asn1Tag(TagClass.ContextSpecific, 4);

        private static readonly X500DistinguishedName s_nonParticipatingName =
            new X500DistinguishedName("CN=The Ghost in the Machine");

        private static readonly X509BasicConstraintsExtension s_eeConstraints =
            new X509BasicConstraintsExtension(false, false, 0, false);

        private static readonly X509KeyUsageExtension s_caKeyUsage =
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                critical: false);

        private static readonly X509KeyUsageExtension s_eeKeyUsage =
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature,
                critical: false);

        private static readonly X509EnhancedKeyUsageExtension s_ocspResponderEku =
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.9", null),
                },
                critical: false);

        private static readonly X509EnhancedKeyUsageExtension s_tlsClientEku =
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.2", null)
                },
                false);

        private X509Certificate2 _cert;
        private X509Extension _cdpExtension;
        private X509Extension _aiaExtension;
        private X509Extension _akidExtension;

        private List<(byte[], DateTimeOffset)> _revocationList;
        private byte[] _crl;
        private int _crlNumber;
        private DateTimeOffset _crlExpiry;
        private X509Certificate2 _ocspResponder;
        private byte[] _dnHash;

        internal string CdpUri { get; }
        internal string OcspUri { get; }

        internal bool CorruptRevocationSignature { get; set; }
        internal DateTimeOffset? RevocationExpiration { get; set; }
        internal bool CorruptRevocationIssuerName { get; set; }

        internal CertificateAuthority(X509Certificate2 cert, string cdpUrl, string ocspUrl)
        {
            _cert = cert;
            CdpUri = cdpUrl;
            OcspUri = ocspUrl;
        }

        public void Dispose()
        {
            _cert.Dispose();
        }

        internal string SubjectName => _cert.Subject;
        internal bool HasOcspDelegation => _ocspResponder != null;
        internal string OcspResponderSubjectName => (_ocspResponder ?? _cert).Subject;

        internal X509Certificate2 CloneIssuerCert()
        {
            return new X509Certificate2(_cert.RawData);
        }

        internal void Revoke(X509Certificate2 certificate, DateTimeOffset revocationTime)
        {
            if (!certificate.IssuerName.RawData.SequenceEqual(_cert.SubjectName.RawData))
            {
                throw new ArgumentException("Certificate was not from this issuer", nameof(certificate));
            }

            if (_revocationList == null)
            {
                _revocationList = new List<(byte[], DateTimeOffset)>();
            }

            byte[] serial = certificate.GetSerialNumber();
            Array.Reverse(serial);
            _revocationList.Add((serial, revocationTime));
            _crl = null;
        }

        internal X509Certificate2 CreateSubordinateCA(
            string subject,
            RSA publicKey,
            int? depthLimit = null)
        {
            return CreateCertificate(
                subject,
                publicKey,
                TimeSpan.FromMinutes(1),
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    depthLimit.HasValue,
                    depthLimit.GetValueOrDefault(),
                    critical: true),
                s_caKeyUsage,
                ekuExtension: null);
        }

        internal X509Certificate2 CreateEndEntity(string subject, RSA publicKey)
        {
            return CreateCertificate(
                subject,
                publicKey,
                TimeSpan.FromSeconds(2),
                s_eeConstraints,
                s_eeKeyUsage,
                s_tlsClientEku);
        }

        internal X509Certificate2 CreateOcspSigner(string subject, RSA publicKey)
        {
            return CreateCertificate(
                subject,
                publicKey,
                TimeSpan.FromSeconds(1),
                s_eeConstraints,
                s_eeKeyUsage,
                s_ocspResponderEku,
                ocspResponder: true);
        }

        internal void RebuildRootWithRevocation()
        {
            if (_cdpExtension == null && CdpUri != null)
            {
                _cdpExtension = CreateCdpExtension(CdpUri);
            }

            if (_aiaExtension == null && OcspUri != null)
            {
                _aiaExtension = CreateAiaExtension(OcspUri);
            }

            RebuildRootWithRevocation(_cdpExtension, _aiaExtension);
        }

        private void RebuildRootWithRevocation(X509Extension cdpExtension, X509Extension aiaExtension)
        {
            X500DistinguishedName subjectName = _cert.SubjectName;

            if (!subjectName.RawData.SequenceEqual(_cert.IssuerName.RawData))
            {
                throw new InvalidOperationException();
            }

            var req = new CertificateRequest(subjectName, _cert.PublicKey, HashAlgorithmName.SHA256);

            foreach (X509Extension ext in _cert.Extensions)
            {
                req.CertificateExtensions.Add(ext);
            }

            req.CertificateExtensions.Add(cdpExtension);
            req.CertificateExtensions.Add(aiaExtension);

            byte[] serial = _cert.GetSerialNumber();
            Array.Reverse(serial);

            X509Certificate2 dispose = _cert;

            using (dispose)
            using (RSA rsa = _cert.GetRSAPrivateKey())
            using (X509Certificate2 tmp = req.Create(
                subjectName,
                X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
                new DateTimeOffset(_cert.NotBefore),
                new DateTimeOffset(_cert.NotAfter),
                serial))
            {
                _cert = tmp.CopyWithPrivateKey(rsa);
            }
        }

        private X509Certificate2 CreateCertificate(
            string subject,
            RSA publicKey,
            TimeSpan nestingBuffer,
            X509BasicConstraintsExtension basicConstraints,
            X509KeyUsageExtension keyUsage,
            X509EnhancedKeyUsageExtension ekuExtension,
            bool ocspResponder = false)
        {
            if (_cdpExtension == null && CdpUri != null)
            {
                _cdpExtension = CreateCdpExtension(CdpUri);
            }

            if (_aiaExtension == null && OcspUri != null)
            {
                _aiaExtension = CreateAiaExtension(OcspUri);
            }

            if (_akidExtension == null)
            {
                _akidExtension = CreateAkidExtension();
            }

            CertificateRequest request = new CertificateRequest(
                subject,
                publicKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(basicConstraints);
            request.CertificateExtensions.Add(keyUsage);

            // Windows does not accept OCSP Responder certificates which have
            // a CDP extension, or an AIA extension with an OCSP endpoint.
            if (!ocspResponder)
            {
                request.CertificateExtensions.Add(_cdpExtension);
                request.CertificateExtensions.Add(_aiaExtension);
            }

            request.CertificateExtensions.Add(_akidExtension);
            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            if (ekuExtension != null)
            {
                request.CertificateExtensions.Add(ekuExtension);
            }

            byte[] serial = new byte[sizeof(long)];
            RandomNumberGenerator.Fill(serial);

            return request.Create(
                _cert,
                _cert.NotBefore.Add(nestingBuffer),
                _cert.NotAfter.Subtract(nestingBuffer),
                serial);
        }

        internal byte[] GetCrl()
        {
            byte[] crl = _crl;
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (crl != null && now < _crlExpiry)
            {
                return crl;
            }

            DateTimeOffset newExpiry = now.AddSeconds(2);

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSequence();
                {
                    writer.WriteObjectIdentifier("1.2.840.113549.1.1.11");
                    writer.WriteNull();

                    writer.PopSequence();
                }

                byte[] signatureAlgId = writer.Encode();
                writer.Reset();

                // TBSCertList
                writer.PushSequence();
                {
                    // version v2(1)
                    writer.WriteInteger(1);

                    // signature (AlgorithmIdentifier)
                    writer.WriteEncodedValue(signatureAlgId);

                    // issuer
                    if (CorruptRevocationIssuerName)
                    {
                        writer.WriteEncodedValue(s_nonParticipatingName.RawData);
                    }
                    else
                    {
                        writer.WriteEncodedValue(_cert.SubjectName.RawData);
                    }

                    if (RevocationExpiration.HasValue)
                    {
                        // thisUpdate
                        writer.WriteUtcTime(_cert.NotBefore);

                        // nextUpdate
                        writer.WriteUtcTime(RevocationExpiration.Value);
                    }
                    else
                    {
                        // thisUpdate
                        writer.WriteUtcTime(now);

                        // nextUpdate
                        writer.WriteUtcTime(newExpiry);
                    }

                    // revokedCertificates (don't write down if empty)
                    if (_revocationList?.Count > 0)
                    {
                        // SEQUENCE OF
                        writer.PushSequence();
                        {
                            foreach ((byte[] serial, DateTimeOffset when) in _revocationList)
                            {
                                // Anonymous CRL Entry type
                                writer.PushSequence();
                                {
                                    writer.WriteInteger(serial);
                                    writer.WriteUtcTime(when);

                                    writer.PopSequence();
                                }
                            }

                            writer.PopSequence();
                        }
                    }

                    // extensions [0] EXPLICIT Extensions
                    writer.PushSequence(s_context0);
                    {
                        // Extensions (SEQUENCE OF)
                        writer.PushSequence();
                        {
                            if (_akidExtension == null)
                            {
                                _akidExtension = CreateAkidExtension();
                            }

                            // Authority Key Identifier Extension
                            writer.PushSequence();
                            {
                                writer.WriteObjectIdentifier(_akidExtension.Oid.Value);

                                if (_akidExtension.Critical)
                                {
                                    writer.WriteBoolean(true);
                                }

                                writer.WriteOctetString(_akidExtension.RawData);
                                writer.PopSequence();
                            }

                            // CRL Number Extension
                            writer.PushSequence();
                            {
                                writer.WriteObjectIdentifier("2.5.29.20");

                                using (AsnWriter nested = new AsnWriter(AsnEncodingRules.DER))
                                {
                                    nested.WriteInteger(_crlNumber);
                                    writer.WriteOctetString(nested.Encode());
                                }

                                writer.PopSequence();
                            }

                            writer.PopSequence();
                        }

                        writer.PopSequence(s_context0);
                    }

                    writer.PopSequence();
                }

                byte[] tbsCertList = writer.Encode();
                writer.Reset();

                byte[] signature;

                using (RSA key = _cert.GetRSAPrivateKey())
                {
                    signature =
                        key.SignData(tbsCertList, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    if (CorruptRevocationSignature)
                    {
                        signature[5] ^= 0xFF;
                    }
                }

                // CertificateList
                writer.PushSequence();
                {
                    writer.WriteEncodedValue(tbsCertList);
                    writer.WriteEncodedValue(signatureAlgId);
                    writer.WriteBitString(signature);

                    writer.PopSequence();
                }

                _crl = writer.Encode();
            }

            _crlExpiry = newExpiry;
            _crlNumber++;
            return _crl;
        }

        internal void DesignateOcspResponder(X509Certificate2 responder)
        {
            _ocspResponder = responder;
        }

        internal byte[] BuildOcspResponse(
            ReadOnlyMemory<byte> certId,
            ReadOnlyMemory<byte> nonceExtension)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            DateTimeOffset revokedTime = default;
            CertStatus status = CheckRevocation(certId, ref revokedTime);
            X509Certificate2 responder = (_ocspResponder ?? _cert);

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                /*
                 
   ResponseData ::= SEQUENCE {
      version              [0] EXPLICIT Version DEFAULT v1,
      responderID              ResponderID,
      producedAt               GeneralizedTime,
      responses                SEQUENCE OF SingleResponse,
      responseExtensions   [1] EXPLICIT Extensions OPTIONAL }
                 */
                writer.PushSequence();
                {
                    // Skip version (v1)

                    /*
   ResponderID ::= CHOICE {
      byName               [1] Name,
      byKey                [2] KeyHash }
                     */

                    writer.PushSequence(s_context1);
                    {
                        if (CorruptRevocationIssuerName)
                        {
                            writer.WriteEncodedValue(s_nonParticipatingName.RawData);
                        }
                        else
                        {
                            writer.WriteEncodedValue(responder.SubjectName.RawData);
                        }

                        writer.PopSequence(s_context1);
                    }

                    writer.WriteGeneralizedTime(now);

                    writer.PushSequence();
                    {
                        /*
   SingleResponse ::= SEQUENCE {
      certID                       CertID,
      certStatus                   CertStatus,
      thisUpdate                   GeneralizedTime,
      nextUpdate         [0]       EXPLICIT GeneralizedTime OPTIONAL,
      singleExtensions   [1]       EXPLICIT Extensions OPTIONAL }
                         */
                        writer.PushSequence();
                        {
                            writer.WriteEncodedValue(certId.Span);

                            if (status == CertStatus.OK)
                            {
                                writer.WriteNull(s_context0);
                            }
                            else if (status == CertStatus.Revoked)
                            {
                                writer.PushSequence(s_context1);
                                writer.WriteGeneralizedTime(revokedTime);
                                writer.PopSequence(s_context1);
                            }
                            else
                            {
                                Assert.Equal(CertStatus.Unknown, status);
                                writer.WriteNull(s_context2);
                            }

                            if (RevocationExpiration.HasValue)
                            {
                                writer.WriteGeneralizedTime(
                                    _cert.NotBefore,
                                    omitFractionalSeconds: true);

                                writer.PushSequence(s_context0);
                                {
                                    writer.WriteGeneralizedTime(
                                        RevocationExpiration.Value,
                                        omitFractionalSeconds: true);

                                    writer.PopSequence(s_context0);
                                }
                            }
                            else
                            {
                                writer.WriteGeneralizedTime(now, omitFractionalSeconds: true);
                            }

                            writer.PopSequence();
                        }

                        writer.PopSequence();
                    }

                    if (!nonceExtension.IsEmpty)
                    {
                        writer.PushSequence(s_context1);
                        {
                            writer.PushSequence();
                            writer.WriteEncodedValue(nonceExtension.Span);
                            writer.PopSequence();
                            writer.PopSequence(s_context1);
                        }
                    }

                    writer.PopSequence();
                }

                byte[] tbsResponseData = writer.Encode();
                writer.Reset();

                /*
                    BasicOCSPResponse       ::= SEQUENCE {
      tbsResponseData      ResponseData,
      signatureAlgorithm   AlgorithmIdentifier,
      signature            BIT STRING,
      certs            [0] EXPLICIT SEQUENCE OF Certificate OPTIONAL }
                 */
                writer.PushSequence();
                {
                    writer.WriteEncodedValue(tbsResponseData);

                    writer.PushSequence();
                    {
                        writer.WriteObjectIdentifier("1.2.840.113549.1.1.11");
                        writer.WriteNull();
                        writer.PopSequence();
                    }

                    using (RSA rsa = responder.GetRSAPrivateKey())
                    {
                        byte[] signature = rsa.SignData(
                            tbsResponseData,
                            HashAlgorithmName.SHA256,
                            RSASignaturePadding.Pkcs1);

                        if (CorruptRevocationSignature)
                        {
                            signature[5] ^= 0xFF;
                        }

                        writer.WriteBitString(signature);
                    }

                    if (_ocspResponder != null)
                    {
                        writer.PushSequence(s_context0);
                        {
                            writer.PushSequence();
                            {
                                writer.WriteEncodedValue(_ocspResponder.RawData);
                                writer.PopSequence();
                            }

                            writer.PopSequence(s_context0);
                        }
                    }

                    writer.PopSequence();
                }

                byte[] responseBytes = writer.Encode();
                writer.Reset();

                writer.PushSequence();
                {
                    writer.WriteEnumeratedValue(OcspResponseStatus.Successful);

                    writer.PushSequence(s_context0);
                    {
                        writer.PushSequence();
                        {
                            writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.1.1");
                            writer.WriteOctetString(responseBytes);
                            writer.PopSequence();
                        }

                        writer.PopSequence(s_context0);
                    }

                    writer.PopSequence();
                }

                return writer.Encode();
            }
        }

        private CertStatus CheckRevocation(ReadOnlyMemory<byte> certId, ref DateTimeOffset revokedTime)
        {
            AsnReader reader = new AsnReader(certId, AsnEncodingRules.DER);
            AsnReader idReader = reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            AsnReader algIdReader = idReader.ReadSequence();

            if (algIdReader.ReadObjectIdentifierAsString() != "1.3.14.3.2.26")
            {
                return CertStatus.Unknown;
            }

            if (algIdReader.HasData)
            {
                algIdReader.ReadNull();
                algIdReader.ThrowIfNotEmpty();
            }

            if (_dnHash == null)
            {
                using (HashAlgorithm hash = SHA1.Create())
                {
                    _dnHash = hash.ComputeHash(_cert.SubjectName.RawData);
                }
            }

            if (!idReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> reqDn))
            {
                idReader.ThrowIfNotEmpty();
            }

            if (!reqDn.Span.SequenceEqual(_dnHash))
            {
                return CertStatus.Unknown;
            }

            if (!idReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> reqKeyHash))
            {
                idReader.ThrowIfNotEmpty();
            }

            // We could check the key hash...

            ReadOnlyMemory<byte> reqSerial = idReader.ReadIntegerBytes();
            idReader.ThrowIfNotEmpty();

            if (_revocationList == null)
            {
                return CertStatus.OK;
            }

            ReadOnlySpan<byte> reqSerialSpan = reqSerial.Span;

            foreach ((byte[] serial, DateTimeOffset time) in _revocationList)
            {
                if (reqSerialSpan.SequenceEqual(serial))
                {
                    revokedTime = time;
                    return CertStatus.Revoked;
                }
            }

            return CertStatus.OK;
        }

        private static X509Extension CreateAiaExtension(string ocspStem)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                // AuthorityInfoAccessSyntax (SEQUENCE OF)
                writer.PushSequence();
                {
                    // AccessDescription
                    writer.PushSequence();
                    {
                        writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.1");

                        writer.WriteCharacterString(
                            new Asn1Tag(TagClass.ContextSpecific, 6),
                            UniversalTagNumber.IA5String,
                            ocspStem);

                        writer.PopSequence();
                    }

                    writer.PopSequence();
                }

                return new X509Extension("1.3.6.1.5.5.7.1.1", writer.Encode(), false);
            }
        }

        private static X509Extension CreateCdpExtension(string cdp)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                // SEQUENCE OF
                writer.PushSequence();
                {
                    // DistributionPoint
                    writer.PushSequence();
                    {
                        // Because DistributionPointName is a CHOICE type this tag is explicit.
                        // (ITU-T REC X.680-201508 C.3.2.2(g)(3rd bullet))
                        // distributionPoint [0] DistributionPointName
                        writer.PushSequence(s_context0);
                        {
                            // [0] DistributionPointName (GeneralNames (SEQUENCE OF))
                            writer.PushSequence(s_context0);
                            {
                                // GeneralName ([6]  IA5String)
                                writer.WriteCharacterString(
                                    new Asn1Tag(TagClass.ContextSpecific, 6),
                                    UniversalTagNumber.IA5String,
                                    cdp);

                                writer.PopSequence(s_context0);
                            }

                            writer.PopSequence(s_context0);
                        }

                        writer.PopSequence();
                    }

                    writer.PopSequence();
                }

                return new X509Extension("2.5.29.31", writer.Encode(), false);
            }
        }

        private X509Extension CreateAkidExtension()
        {
            X509SubjectKeyIdentifierExtension skid =
                _cert.Extensions.OfType<X509SubjectKeyIdentifierExtension>().SingleOrDefault();

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                // AuthorityKeyIdentifier
                writer.PushSequence();
                {
                    if (skid == null)
                    {
                        // authorityCertIssuer [1] GeneralNames (SEQUENCE OF)
                        writer.PushSequence(s_context1);
                        {
                            // directoryName [4] Name
                            byte[] dn = _cert.SubjectName.RawData;

                            if (!s_context4.TryEncode(dn, out int written) || written != 1)
                            {
                                throw new InvalidOperationException();
                            }

                            writer.WriteEncodedValue(dn);
                            writer.PopSequence(s_context1);
                        }

                        // authorityCertSerialNumber [2] CertificateSerialNumber (INTEGER)
                        byte[] serial = _cert.GetSerialNumber();
                        Array.Reverse(serial);
                        writer.WriteInteger(s_context2, serial);
                    }
                    else
                    {
                        // keyIdentifier [0] KeyIdentifier (OCTET STRING)
                        AsnReader reader = new AsnReader(skid.RawData, AsnEncodingRules.BER);
                        ReadOnlyMemory<byte> contents;

                        if (!reader.TryReadPrimitiveOctetStringBytes(out contents))
                        {
                            throw new InvalidOperationException();
                        }

                        reader.ThrowIfNotEmpty();
                        writer.WriteOctetString(s_context0, contents.Span);
                    }

                    writer.PopSequence();
                }

                return new X509Extension("2.5.29.35", writer.Encode(), false);
            }
        }

        private enum OcspResponseStatus
        {
            Successful,
        }

        private enum CertStatus
        {
            Unknown,
            OK,
            Revoked,
        }
    }
}
