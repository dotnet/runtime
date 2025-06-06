// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

// PQC types are used throughout, but only when the caller requests them.
#pragma warning disable SYSLIB5006

namespace System.Security.Cryptography.X509Certificates.Tests.Common
{
    // This class represents only a portion of what is required to be a proper Certificate Authority.
    //
    // Please do not use it as the basis for any real Public/Private Key Infrastructure (PKI) system
    // without understanding all of the portions of proper CA management that you're skipping.
    //
    // At minimum, read the current baseline requirements of the CA/Browser Forum.

    [Flags]
    public enum PkiOptions
    {
        None = 0,

        IssuerRevocationViaCrl = 1 << 0,
        IssuerRevocationViaOcsp = 1 << 1,
        EndEntityRevocationViaCrl = 1 << 2,
        EndEntityRevocationViaOcsp = 1 << 3,

        CrlEverywhere = IssuerRevocationViaCrl | EndEntityRevocationViaCrl,
        OcspEverywhere = IssuerRevocationViaOcsp | EndEntityRevocationViaOcsp,
        AllIssuerRevocation = IssuerRevocationViaCrl | IssuerRevocationViaOcsp,
        AllEndEntityRevocation = EndEntityRevocationViaCrl | EndEntityRevocationViaOcsp,
        AllRevocation = CrlEverywhere | OcspEverywhere,

        IssuerAuthorityHasDesignatedOcspResponder = 1 << 16,
        RootAuthorityHasDesignatedOcspResponder = 1 << 17,
        NoIssuerCertDistributionUri = 1 << 18,
        NoRootCertDistributionUri = 1 << 18,
    }

    internal sealed class CertificateAuthority : IDisposable
    {
        private static readonly Asn1Tag s_context0 = new Asn1Tag(TagClass.ContextSpecific, 0);
        private static readonly Asn1Tag s_context1 = new Asn1Tag(TagClass.ContextSpecific, 1);
        private static readonly Asn1Tag s_context2 = new Asn1Tag(TagClass.ContextSpecific, 2);
        private static readonly KeyFactory[] s_variantKeyFactories = KeyFactory.BuildVariantFactories();

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
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
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
        private byte[] _certData;
        private X509Extension _cdpExtension;
        private X509Extension _aiaExtension;
        private X509AuthorityKeyIdentifierExtension _akidExtension;

        private List<(byte[], DateTimeOffset)> _revocationList;
        private byte[] _crl;
        private int _crlNumber;
        private DateTimeOffset _crlExpiry;
        private X509Certificate2 _ocspResponder;
        private byte[] _dnHash;

        internal string AiaHttpUri { get; }
        internal string CdpUri { get; }
        internal string OcspUri { get; }

        internal bool CorruptRevocationSignature { get; set; }
        internal DateTimeOffset? RevocationExpiration { get; set; }
        internal bool CorruptRevocationIssuerName { get; set; }
        internal bool OmitNextUpdateInCrl { get; set; }

        // All keys created in this method are smaller than recommended,
        // but they only live for a few seconds (at most),
        // and never communicate out of process.
        const int DefaultKeySize = 1024;

        internal CertificateAuthority(
            X509Certificate2 cert,
            string aiaHttpUrl,
            string cdpUrl,
            string ocspUrl)
        {
            _cert = cert;
            AiaHttpUri = aiaHttpUrl;
            CdpUri = cdpUrl;
            OcspUri = ocspUrl;
        }

        public void Dispose()
        {
            _cert.Dispose();
            _ocspResponder?.Dispose();
        }

        internal string SubjectName => _cert.Subject;
        internal bool HasOcspDelegation => _ocspResponder != null;
        internal string OcspResponderSubjectName => (_ocspResponder ?? _cert).Subject;

        internal X509Certificate2 CloneIssuerCert()
        {
            return X509CertificateLoader.LoadCertificate(_cert.RawData);
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

            byte[] serial = certificate.SerialNumberBytes.ToArray();
            _revocationList.Add((serial, revocationTime));
            _crl = null;
        }

        internal X509Certificate2 CreateSubordinateCA(
            string subject,
            PublicKey publicKey,
            int? depthLimit = null)
        {
            return CreateCertificate(
                subject,
                publicKey,
                TimeSpan.FromMinutes(1),
                new X509ExtensionCollection() {
                    new X509BasicConstraintsExtension(
                        certificateAuthority: true,
                        depthLimit.HasValue,
                        depthLimit.GetValueOrDefault(),
                        critical: true),
                    s_caKeyUsage });
        }

        internal X509Certificate2 CreateEndEntity(string subject, PublicKey publicKey, X509ExtensionCollection extensions)
        {
            return CreateCertificate(
                subject,
                publicKey,
                TimeSpan.FromSeconds(2),
                extensions);
        }

        internal X509Certificate2 CreateOcspSigner(string subject, RSA publicKey)
        {
            return CreateOcspSigner(
                subject,
                X509SignatureGenerator.CreateForRSA(publicKey, RSASignaturePadding.Pkcs1).PublicKey);
        }

        internal X509Certificate2 CreateOcspSigner(string subject, PublicKey publicKey)
        {
            return CreateCertificate(
                subject,
                publicKey,
                TimeSpan.FromSeconds(1),
                new X509ExtensionCollection() { s_eeConstraints, s_eeKeyUsage, s_ocspResponderEku },
                ocspResponder: true);
        }

        internal void RebuildRootWithRevocation()
        {
            if (_cdpExtension == null && CdpUri != null)
            {
                _cdpExtension = CreateCdpExtension(CdpUri);
            }

            if (_aiaExtension == null && (OcspUri != null || AiaHttpUri != null))
            {
                _aiaExtension = CreateAiaExtension(AiaHttpUri, OcspUri);
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

            var req = new CertificateRequest(subjectName, _cert.PublicKey, HashAlgorithmIfNeeded(_cert.GetKeyAlgorithm()));

            foreach (X509Extension ext in _cert.Extensions)
            {
                req.CertificateExtensions.Add(ext);
            }

            req.CertificateExtensions.Add(cdpExtension);
            req.CertificateExtensions.Add(aiaExtension);

            byte[] serial = _cert.SerialNumberBytes.ToArray();

            X509Certificate2 dispose = _cert;

            using (dispose)
            using (KeyHolder key = new KeyHolder(_cert))
            using (X509Certificate2 tmp = req.Create(
                subjectName,
                key.GetGenerator(),
                new DateTimeOffset(_cert.NotBefore),
                new DateTimeOffset(_cert.NotAfter),
                serial))
            {
                _cert = key.OntoCertificate(tmp);
            }
        }

        private X509Certificate2 CreateCertificate(
            string subject,
            PublicKey publicKey,
            TimeSpan nestingBuffer,
            X509ExtensionCollection extensions,
            bool ocspResponder = false)
        {
            if (_cdpExtension == null && CdpUri != null)
            {
                _cdpExtension = CreateCdpExtension(CdpUri);
            }

            if (_aiaExtension == null && (OcspUri != null || AiaHttpUri != null))
            {
                _aiaExtension = CreateAiaExtension(AiaHttpUri, OcspUri);
            }

            if (_akidExtension == null)
            {
                _akidExtension = CreateAkidExtension();
            }

            CertificateRequest request = new CertificateRequest(
                new X500DistinguishedName(subject),
                publicKey,
                HashAlgorithmIfNeeded(_cert.GetKeyAlgorithm()),
                RSASignaturePadding.Pkcs1);

            foreach (X509Extension extension in extensions)
            {
                request.CertificateExtensions.Add(extension);
            }

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

            byte[] serial = new byte[sizeof(long)];
            RandomNumberGenerator.Fill(serial);

            using (KeyHolder key = new KeyHolder(_cert))
            {
                return request.Create(
                    _cert.SubjectName,
                    key.GetGenerator(),
                    _cert.NotBefore.Add(nestingBuffer),
                    _cert.NotAfter.Subtract(nestingBuffer),
                    serial);
            }
        }

        internal byte[] GetCertData()
        {
            return (_certData ??= _cert.RawData);
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
            X509AuthorityKeyIdentifierExtension akid = _akidExtension ??= CreateAkidExtension();

            if (OmitNextUpdateInCrl)
            {
                crl = BuildCrlManually(now, newExpiry, akid);
            }
            else
            {
                CertificateRevocationListBuilder builder = new CertificateRevocationListBuilder();

                if (_revocationList is not null)
                {
                    foreach ((byte[] serial, DateTimeOffset when) in _revocationList)
                    {
                        builder.AddEntry(serial, when);
                    }
                }

                DateTimeOffset thisUpdate;
                DateTimeOffset nextUpdate;

                if (RevocationExpiration.HasValue)
                {
                    nextUpdate = RevocationExpiration.GetValueOrDefault();
                    thisUpdate = _cert.NotBefore;
                }
                else
                {
                    thisUpdate = now;
                    nextUpdate = newExpiry;
                }

                using (KeyHolder key = new KeyHolder(_cert))
                {
                    crl = builder.Build(
                        CorruptRevocationIssuerName ? s_nonParticipatingName : _cert.SubjectName,
                        key.GetGenerator(),
                        _crlNumber,
                        nextUpdate,
                        HashAlgorithmIfNeeded(key.ToPublicKey().Oid.Value),
                        _akidExtension,
                        thisUpdate);
                }
            }

            if (CorruptRevocationSignature)
            {
                crl[^2] ^= 0xFF;
            }

            _crl = crl;
            _crlExpiry = newExpiry;
            _crlNumber++;
            return crl;
        }

        private byte[] BuildCrlManually(
            DateTimeOffset now,
            DateTimeOffset newExpiry,
            X509AuthorityKeyIdentifierExtension akidExtension)
        {
            using KeyHolder key = new KeyHolder(_cert);
            byte[] signatureAlgId = key.GetSignatureAlgorithmIdentifier();

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // TBSCertList
            using (writer.PushSequence())
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
                    if (!OmitNextUpdateInCrl)
                    {
                        writer.WriteUtcTime(RevocationExpiration.Value);
                    }
                }
                else
                {
                    // thisUpdate
                    writer.WriteUtcTime(now);

                    // nextUpdate
                    if (!OmitNextUpdateInCrl)
                    {
                        writer.WriteUtcTime(newExpiry);
                    }
                }

                // revokedCertificates (don't write down if empty)
                if (_revocationList?.Count > 0)
                {
                    // SEQUENCE OF
                    using (writer.PushSequence())
                    {
                        foreach ((byte[] serial, DateTimeOffset when) in _revocationList)
                        {
                            // Anonymous CRL Entry type
                            using (writer.PushSequence())
                            {
                                writer.WriteInteger(serial);
                                writer.WriteUtcTime(when);
                            }
                        }
                    }
                }

                // extensions [0] EXPLICIT Extensions
                using (writer.PushSequence(s_context0))
                {
                    // Extensions (SEQUENCE OF)
                    using (writer.PushSequence())
                    {
                        // Authority Key Identifier Extension
                        using (writer.PushSequence())
                        {
                            writer.WriteObjectIdentifier(akidExtension.Oid.Value);

                            if (akidExtension.Critical)
                            {
                                writer.WriteBoolean(true);
                            }

                            writer.WriteOctetString(akidExtension.RawData);
                        }

                        // CRL Number Extension
                        using (writer.PushSequence())
                        {
                            writer.WriteObjectIdentifier("2.5.29.20");

                            using (writer.PushOctetString())
                            {
                                writer.WriteInteger(_crlNumber);
                            }
                        }
                    }
                }
            }

            byte[] tbsCertList = writer.Encode();
            writer.Reset();

            byte[] signature = key.Sign(tbsCertList);

            if (CorruptRevocationSignature)
            {
                signature[5] ^= 0xFF;
            }

            // CertificateList
            using (writer.PushSequence())
            {
                writer.WriteEncodedValue(tbsCertList);
                writer.WriteEncodedValue(signatureAlgId);
                writer.WriteBitString(signature);
            }

            return writer.Encode();
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

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            /*
   ResponseData ::= SEQUENCE {
      version              [0] EXPLICIT Version DEFAULT v1,
      responderID              ResponderID,
      producedAt               GeneralizedTime,
      responses                SEQUENCE OF SingleResponse,
      responseExtensions   [1] EXPLICIT Extensions OPTIONAL }
                 */
            using (writer.PushSequence())
            {
                // Skip version (v1)

                /*
ResponderID ::= CHOICE {
  byName               [1] Name,
  byKey                [2] KeyHash }
                 */

                using (writer.PushSequence(s_context1))
                {
                    if (CorruptRevocationIssuerName)
                    {
                        writer.WriteEncodedValue(s_nonParticipatingName.RawData);
                    }
                    else
                    {
                        writer.WriteEncodedValue(responder.SubjectName.RawData);
                    }
                }

                writer.WriteGeneralizedTime(now, omitFractionalSeconds: true);

                using (writer.PushSequence())
                {
                    /*
SingleResponse ::= SEQUENCE {
  certID                       CertID,
  certStatus                   CertStatus,
  thisUpdate                   GeneralizedTime,
  nextUpdate         [0]       EXPLICIT GeneralizedTime OPTIONAL,
  singleExtensions   [1]       EXPLICIT Extensions OPTIONAL }
                     */
                    using (writer.PushSequence())
                    {
                        writer.WriteEncodedValue(certId.Span);

                        if (status == CertStatus.OK)
                        {
                            writer.WriteNull(s_context0);
                        }
                        else if (status == CertStatus.Revoked)
                        {
                            writer.PushSequence(s_context1);

                            // Fractional seconds "MUST NOT" be used here. Android and macOS 13+ enforce this and
                            // reject GeneralizedTime's with fractional seconds, so omit them.
                            // RFC 6960: 4.2.2.1:
                            // The format for GeneralizedTime is as specified in Section 4.1.2.5.2 of [RFC5280].
                            // RFC 5280 4.1.2.5.2:
                            // For the purposes of this profile, GeneralizedTime values MUST be
                            // expressed in Greenwich Mean Time (Zulu) and MUST include seconds
                            // (i.e., times are YYYYMMDDHHMMSSZ), even where the number of seconds
                            // is zero. GeneralizedTime values MUST NOT include fractional seconds.
                            writer.WriteGeneralizedTime(revokedTime, omitFractionalSeconds: true);
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

                            using (writer.PushSequence(s_context0))
                            {
                                writer.WriteGeneralizedTime(
                                    RevocationExpiration.Value,
                                    omitFractionalSeconds: true);
                            }
                        }
                        else
                        {
                            writer.WriteGeneralizedTime(now, omitFractionalSeconds: true);
                        }
                    }
                }

                if (!nonceExtension.IsEmpty)
                {
                    using (writer.PushSequence(s_context1))
                    using (writer.PushSequence())
                    {
                        writer.WriteEncodedValue(nonceExtension.Span);
                    }
                }
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
            using (writer.PushSequence())
            {
                writer.WriteEncodedValue(tbsResponseData);

                using (KeyHolder key = new KeyHolder(responder))
                {
                    writer.WriteEncodedValue(key.GetSignatureAlgorithmIdentifier());

                    byte[] signature = key.Sign(tbsResponseData);

                    if (CorruptRevocationSignature)
                    {
                        signature[5] ^= 0xFF;
                    }

                    writer.WriteBitString(signature);
                }

                if (_ocspResponder != null)
                {
                    using (writer.PushSequence(s_context0))
                    using (writer.PushSequence())
                    {
                        writer.WriteEncodedValue(_ocspResponder.RawData);
                        writer.PopSequence();
                    }
                }
            }

            byte[] responseBytes = writer.Encode();
            writer.Reset();

            using (writer.PushSequence())
            {
                writer.WriteEnumeratedValue(OcspResponseStatus.Successful);

                using (writer.PushSequence(s_context0))
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.1.1");
                    writer.WriteOctetString(responseBytes);
                }
            }

            return writer.Encode();
        }

        private CertStatus CheckRevocation(ReadOnlyMemory<byte> certId, ref DateTimeOffset revokedTime)
        {
            AsnReader reader = new AsnReader(certId, AsnEncodingRules.DER);
            AsnReader idReader = reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            AsnReader algIdReader = idReader.ReadSequence();

            if (algIdReader.ReadObjectIdentifier() != "1.3.14.3.2.26")
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
                _dnHash = SHA1.HashData(_cert.SubjectName.RawData);
            }

            if (!idReader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> reqDn))
            {
                idReader.ThrowIfNotEmpty();
            }

            if (!reqDn.Span.SequenceEqual(_dnHash))
            {
                return CertStatus.Unknown;
            }

            if (!idReader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> reqKeyHash))
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

        private static X509Extension CreateAiaExtension(string certLocation, string ocspStem)
        {
            string[] ocsp = null;
            string[] caIssuers = null;

            if (ocspStem is not null)
            {
                ocsp = new[] { ocspStem };
            }

            if (certLocation is not null)
            {
                caIssuers = new[] { certLocation };
            }

            return new X509AuthorityInformationAccessExtension(ocsp, caIssuers);
        }

        private static X509Extension CreateCdpExtension(string cdp)
        {
            return CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(new[] { cdp });
        }

        private X509AuthorityKeyIdentifierExtension CreateAkidExtension()
        {
            X509SubjectKeyIdentifierExtension skid =
                _cert.Extensions.OfType<X509SubjectKeyIdentifierExtension>().SingleOrDefault();

            if (skid is null)
            {
                return X509AuthorityKeyIdentifierExtension.CreateFromCertificate(
                    _cert,
                    includeKeyIdentifier: false,
                    includeIssuerAndSerial: true);
            }

            return X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(skid);
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

        internal static void BuildPrivatePki(
            PkiOptions pkiOptions,
            out RevocationResponder responder,
            out CertificateAuthority rootAuthority,
            out CertificateAuthority[] intermediateAuthorities,
            out X509Certificate2 endEntityCert,
            int intermediateAuthorityCount,
            string testName = null,
            bool registerAuthorities = true,
            bool pkiOptionsInSubject = false,
            string subjectName = null,
            KeyFactory keyFactory = null,
            X509ExtensionCollection extensions = null)
        {
            bool rootDistributionViaHttp = !pkiOptions.HasFlag(PkiOptions.NoRootCertDistributionUri);
            bool issuerRevocationViaCrl = pkiOptions.HasFlag(PkiOptions.IssuerRevocationViaCrl);
            bool issuerRevocationViaOcsp = pkiOptions.HasFlag(PkiOptions.IssuerRevocationViaOcsp);
            bool issuerDistributionViaHttp = !pkiOptions.HasFlag(PkiOptions.NoIssuerCertDistributionUri);
            bool endEntityRevocationViaCrl = pkiOptions.HasFlag(PkiOptions.EndEntityRevocationViaCrl);
            bool endEntityRevocationViaOcsp = pkiOptions.HasFlag(PkiOptions.EndEntityRevocationViaOcsp);

            Assert.True(
                issuerRevocationViaCrl || issuerRevocationViaOcsp ||
                    endEntityRevocationViaCrl || endEntityRevocationViaOcsp,
                "At least one revocation mode is enabled");

            // default to client
            extensions ??= new X509ExtensionCollection() { s_eeConstraints, s_eeKeyUsage, s_tlsClientEku };

            if (keyFactory is null)
            {
                // This could use any of the non-cryptographic hashes, but that complicates the code sharing for this file,
                // so use IncrementalHash(SHA256) as it's inbox.
                //
                // System.HashCode isn't suitable because it's randomized, and we want the algorithm to
                // be consistent for any given test from run to run.
                using (IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                {
                    // The use of AsBytes means that the hash value will differ between Big and Little Endian
                    // platforms, but that's OK: a failing test in a given configuration will continue to fail
                    // in that configuration.
                    hasher.AppendData(MemoryMarshal.AsBytes(new ReadOnlySpan<PkiOptions>(ref pkiOptions)));
                    hasher.AppendData(MemoryMarshal.AsBytes(new ReadOnlySpan<int>(ref intermediateAuthorityCount)));
                    hasher.AppendData(MemoryMarshal.AsBytes(testName.AsSpan()));
                    hasher.AppendData(MemoryMarshal.AsBytes(subjectName.AsSpan()));

                    Span<byte> hash = stackalloc byte[256 / 8];
                    int written = hasher.GetCurrentHash(hash);
                    Debug.Assert(written == hash.Length);

                    // Using mod here will create an imbalance any time s_variantKeyFactories isn't a power of 2,
                    // but that's OK.
                    keyFactory = s_variantKeyFactories[hash[0] % s_variantKeyFactories.Length];
                }
            }

            using (KeyHolder rootKey = KeyHolder.CreateKey(keyFactory))
            using (KeyHolder eeKey = KeyHolder.CreateKey(keyFactory))
            {
                CertificateRequest rootReq = rootKey.CreateRequest(
                    BuildSubject("A Revocation Test Root", testName, pkiOptions, pkiOptionsInSubject));

                X509BasicConstraintsExtension caConstraints =
                    new X509BasicConstraintsExtension(true, false, 0, true);

                rootReq.CertificateExtensions.Add(caConstraints);
                var rootSkid = new X509SubjectKeyIdentifierExtension(rootReq.PublicKey, false);
                rootReq.CertificateExtensions.Add(
                    rootSkid);

                DateTimeOffset start = DateTimeOffset.UtcNow;
                DateTimeOffset end = start.AddMonths(3);

                // Don't dispose this, it's being transferred to the CertificateAuthority
                X509Certificate2 rootCert = rootReq.CreateSelfSigned(start.AddDays(-2), end.AddDays(2));
                responder = RevocationResponder.CreateAndListen();

                string certUrl = $"{responder.UriPrefix}cert/{rootSkid.SubjectKeyIdentifier}.cer";
                string cdpUrl = $"{responder.UriPrefix}crl/{rootSkid.SubjectKeyIdentifier}.crl";
                string ocspUrl = $"{responder.UriPrefix}ocsp/{rootSkid.SubjectKeyIdentifier}";

                rootAuthority = new CertificateAuthority(
                    rootCert,
                    rootDistributionViaHttp ? certUrl : null,
                    issuerRevocationViaCrl || (endEntityRevocationViaCrl && intermediateAuthorityCount == 0) ? cdpUrl : null,
                    issuerRevocationViaOcsp || (endEntityRevocationViaOcsp && intermediateAuthorityCount == 0) ? ocspUrl : null);

                CertificateAuthority issuingAuthority = rootAuthority;
                intermediateAuthorities = new CertificateAuthority[intermediateAuthorityCount];

                for (int intermediateIndex = 0; intermediateIndex < intermediateAuthorityCount; intermediateIndex++)
                {
                    using KeyHolder intermediateKey = KeyHolder.CreateKey(keyFactory);

                    // Don't dispose this, it's being transferred to the CertificateAuthority
                    X509Certificate2 intermedCert;

                    {
                        X509Certificate2 intermedPub = issuingAuthority.CreateSubordinateCA(
                            BuildSubject($"A Revocation Test CA {intermediateIndex}", testName, pkiOptions, pkiOptionsInSubject),
                            intermediateKey.ToPublicKey());
                        intermedCert = intermediateKey.OntoCertificate(intermedPub);
                        intermedPub.Dispose();
                    }

                    X509SubjectKeyIdentifierExtension intermedSkid =
                        intermedCert.Extensions.OfType<X509SubjectKeyIdentifierExtension>().Single();

                    certUrl = $"{responder.UriPrefix}cert/{intermedSkid.SubjectKeyIdentifier}.cer";
                    cdpUrl = $"{responder.UriPrefix}crl/{intermedSkid.SubjectKeyIdentifier}.crl";
                    ocspUrl = $"{responder.UriPrefix}ocsp/{intermedSkid.SubjectKeyIdentifier}";

                    CertificateAuthority intermediateAuthority = new CertificateAuthority(
                        intermedCert,
                        issuerDistributionViaHttp ? certUrl : null,
                        endEntityRevocationViaCrl ? cdpUrl : null,
                        endEntityRevocationViaOcsp ? ocspUrl : null);

                    issuingAuthority = intermediateAuthority;
                    intermediateAuthorities[intermediateIndex] = intermediateAuthority;
                }

                endEntityCert = issuingAuthority.CreateEndEntity(
                    BuildSubject(subjectName ?? "A Revocation Test Cert", testName, pkiOptions, pkiOptionsInSubject),
                    eeKey.ToPublicKey(),
                    extensions);

                X509Certificate2 tmp = endEntityCert;
                endEntityCert = eeKey.OntoCertificate(endEntityCert);
                tmp.Dispose();
            }

            if (registerAuthorities)
            {
                responder.AddCertificateAuthority(rootAuthority);

                foreach (CertificateAuthority authority in intermediateAuthorities)
                {
                    responder.AddCertificateAuthority(authority);
                }
            }
        }

        internal static void BuildPrivatePki(
            PkiOptions pkiOptions,
            out RevocationResponder responder,
            out CertificateAuthority rootAuthority,
            out CertificateAuthority intermediateAuthority,
            out X509Certificate2 endEntityCert,
            string testName = null,
            bool registerAuthorities = true,
            bool pkiOptionsInSubject = false,
            string subjectName = null,
            KeyFactory keyFactory = null,
            X509ExtensionCollection extensions = null)
        {
            BuildPrivatePki(
                pkiOptions,
                out responder,
                out rootAuthority,
                out CertificateAuthority[] intermediateAuthorities,
                out endEntityCert,
                intermediateAuthorityCount: 1,
                testName: testName,
                registerAuthorities: registerAuthorities,
                pkiOptionsInSubject: pkiOptionsInSubject,
                subjectName: subjectName,
                keyFactory: keyFactory,
                extensions: extensions);

            intermediateAuthority = intermediateAuthorities.Single();
        }

        private static string BuildSubject(
            string cn,
            string testName,
            PkiOptions pkiOptions,
            bool includePkiOptions)
        {
            string testNamePart = !string.IsNullOrWhiteSpace(testName) ? $", O=\"{testName}\"" : "";
            string pkiOptionsPart = includePkiOptions ? $", OU=\"{pkiOptions}\"" : "";

            return $"CN=\"{cn}\"" + testNamePart + pkiOptionsPart;
        }

        private static HashAlgorithmName HashAlgorithmIfNeeded(string publicKeyOid)
        {
            const string Rsa = "1.2.840.113549.1.1.1";
            const string RsaPss = "1.2.840.113549.1.1.10";
            const string EcPublicKey = "1.2.840.10045.2.1";
            const string Dsa = "1.2.840.10040.4.1";

            return publicKeyOid switch
            {
                Rsa or RsaPss or EcPublicKey or Dsa => HashAlgorithmName.SHA256,
                _ => default,
            };
        }

        internal static X509Certificate2 CloneWithPrivateKey(X509Certificate2 cert, object key)
        {
            return key switch
            {
                RSA rsa => cert.CopyWithPrivateKey(rsa),
                ECDsa ecdsa => cert.CopyWithPrivateKey(ecdsa),
                MLDsa mldsa => cert.CopyWithPrivateKey(mldsa),
                SlhDsa slhDsa => cert.CopyWithPrivateKey(slhDsa),
                DSA dsa => cert.CopyWithPrivateKey(dsa),
                _ => throw new InvalidOperationException(
                    $"Had no handler for key of type {key?.GetType().FullName ?? "null"}")
            };
        }

        internal sealed class KeyFactory
        {
            internal static KeyFactory RSA { get; } =
                new(() => Cryptography.RSA.Create(DefaultKeySize));

            internal static KeyFactory ECDsa { get; } =
                new(() => Cryptography.ECDsa.Create(ECCurve.NamedCurves.nistP384));

            internal static KeyFactory MLDsa { get; } =
                new(() => Cryptography.MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa65));

            internal static KeyFactory SlhDsa { get; } =
                new(() => Cryptography.SlhDsa.GenerateKey(SlhDsaAlgorithm.SlhDsaSha2_128f));

            private Func<IDisposable> _factory;

            private KeyFactory(Func<IDisposable> factory)
            {
                _factory = factory;
            }

            internal IDisposable CreateKey()
            {
                return _factory();
            }

            internal static KeyFactory RSASize(int keySize)
            {
                return new KeyFactory(() => Cryptography.RSA.Create(keySize));
            }

            internal static KeyFactory[] BuildVariantFactories()
            {
                List<KeyFactory> factories = [RSA, ECDsa];

                // TODO: MLDsa certificate support on Windows is not available yet. Remove this once it is.
                if (Cryptography.MLDsa.IsSupported && !PlatformDetection.IsWindows)
                {
                    factories.Add(MLDsa);
                }

                if (Cryptography.SlhDsa.IsSupported)
                {
                    factories.Add(SlhDsa);
                }

                return factories.ToArray();
            }
        }

        private sealed class KeyHolder : IDisposable
        {
            private readonly IDisposable _key;
            private X509SignatureGenerator _generator;

            internal KeyHolder(IDisposable key)
            {
                _key = key;
            }

            internal KeyHolder(X509Certificate2 cert)
            {
                // We're always in the context of signing something, so EC-DH does not apply.
                _key =
                    cert.GetRSAPrivateKey() ??
                    cert.GetECDsaPrivateKey() ??
                    cert.GetMLDsaPrivateKey() ??
                    cert.GetSlhDsaPrivateKey() ??
                    (IDisposable)cert.GetDSAPrivateKey() ??
                    throw new NotSupportedException();
            }

            public void Dispose()
            {
                _key?.Dispose();
            }

            internal static KeyHolder CreateKey(KeyFactory factory)
            {
                return new KeyHolder(factory.CreateKey());
            }

            internal CertificateRequest CreateRequest(string subject)
            {
                return _key switch
                {
                    RSA rsa => new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                    ECDsa ecdsa => new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256),
                    MLDsa mldsa => new CertificateRequest(subject, mldsa),
                    SlhDsa slhDsa => new CertificateRequest(subject, slhDsa),
                    _ => throw new NotSupportedException(),
                };
            }

            internal X509SignatureGenerator GetGenerator()
            {
                return _generator ??= _key switch
                {
                    RSA rsa => X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1),
                    ECDsa ecdsa => X509SignatureGenerator.CreateForECDsa(ecdsa),
                    MLDsa mldsa => X509SignatureGenerator.CreateForMLDsa(mldsa),
                    SlhDsa slhDsa => X509SignatureGenerator.CreateForSlhDsa(slhDsa),
                    _ => throw new NotSupportedException(),
                };
            }

            internal PublicKey ToPublicKey()
            {
                return GetGenerator().PublicKey;
            }

            internal X509Certificate2 OntoCertificate(X509Certificate2 cert)
            {
                return CloneWithPrivateKey(cert, _key);
            }

            internal byte[] Sign(byte[] data)
            {
                X509SignatureGenerator generator = GetGenerator();
                return generator.SignData(data, HashAlgorithmIfNeeded(generator.PublicKey.Oid.Value));
            }

            internal byte[] GetSignatureAlgorithmIdentifier()
            {
                X509SignatureGenerator generator = GetGenerator();

                return generator.GetSignatureAlgorithmIdentifier(
                    HashAlgorithmIfNeeded(generator.PublicKey.Oid.Value));
            }
        }
    }
}
