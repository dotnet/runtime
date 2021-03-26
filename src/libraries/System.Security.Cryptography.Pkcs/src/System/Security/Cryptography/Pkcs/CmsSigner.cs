// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class CmsSigner
    {
        private static readonly Oid s_defaultAlgorithm = Oids.Sha256Oid;

        private SubjectIdentifierType _signerIdentifierType;

        public X509Certificate2? Certificate { get; set; }
        public AsymmetricAlgorithm? PrivateKey { get; set; }
        public X509Certificate2Collection Certificates { get; private set; } = new X509Certificate2Collection();
        public Oid DigestAlgorithm { get; set; }
        public X509IncludeOption IncludeOption { get; set; }
        public CryptographicAttributeObjectCollection SignedAttributes { get; private set; } = new CryptographicAttributeObjectCollection();
        public CryptographicAttributeObjectCollection UnsignedAttributes { get; private set; } = new CryptographicAttributeObjectCollection();

        public SubjectIdentifierType SignerIdentifierType
        {
            get { return _signerIdentifierType; }
            set
            {
                if (value < SubjectIdentifierType.IssuerAndSerialNumber || value > SubjectIdentifierType.NoSignature)
                    throw new ArgumentException(SR.Format(SR.Cryptography_Cms_Invalid_Subject_Identifier_Type, value));
                _signerIdentifierType = value;
            }
        }

        public CmsSigner()
            : this(SubjectIdentifierType.IssuerAndSerialNumber, null)
        {
        }

        public CmsSigner(SubjectIdentifierType signerIdentifierType)
            : this(signerIdentifierType, null)
        {
        }

        public CmsSigner(X509Certificate2? certificate)
            : this(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
        {
        }

        // This can be implemented with NETCOREAPP2_0 with the cert creation API.
        // * Open the parameters as RSACSP (RSA PKCS#1 signature was hard-coded in netfx)
        //   * Which will fail on non-Windows
        // * Create a certificate with subject CN=CMS Signer Dummy Certificate
        //   * Need to check against .NET Framework to find out what the NotBefore/NotAfter values are
        //   * No extensions
        //
        // Since it would only work on Windows, it could also be just done as P/Invokes to
        // CertCreateSelfSignedCertificate on a split Windows/netstandard implementation.
        public CmsSigner(CspParameters parameters) => throw new PlatformNotSupportedException();

        public CmsSigner(SubjectIdentifierType signerIdentifierType, X509Certificate2? certificate) : this(signerIdentifierType, certificate, null)
        {
        }

        public CmsSigner(SubjectIdentifierType signerIdentifierType, X509Certificate2? certificate, AsymmetricAlgorithm? privateKey)
        {
            switch (signerIdentifierType)
            {
                case SubjectIdentifierType.Unknown:
                    _signerIdentifierType = SubjectIdentifierType.IssuerAndSerialNumber;
                    IncludeOption = X509IncludeOption.ExcludeRoot;
                    break;
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    _signerIdentifierType = signerIdentifierType;
                    IncludeOption = X509IncludeOption.ExcludeRoot;
                    break;
                case SubjectIdentifierType.SubjectKeyIdentifier:
                    _signerIdentifierType = signerIdentifierType;
                    IncludeOption = X509IncludeOption.ExcludeRoot;
                    break;
                case SubjectIdentifierType.NoSignature:
                    _signerIdentifierType = signerIdentifierType;
                    IncludeOption = X509IncludeOption.None;
                    break;
                default:
                    _signerIdentifierType = SubjectIdentifierType.IssuerAndSerialNumber;
                    IncludeOption = X509IncludeOption.ExcludeRoot;
                    break;
            }

            Certificate = certificate;
            DigestAlgorithm = s_defaultAlgorithm.CopyOid();
            PrivateKey = privateKey;
        }

        internal void CheckCertificateValue()
        {
            if (SignerIdentifierType == SubjectIdentifierType.NoSignature)
            {
                return;
            }

            if (Certificate == null)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_Cms_NoSignerCert);
            }

            if (PrivateKey == null && !Certificate.HasPrivateKey)
            {
                throw new CryptographicException(SR.Cryptography_Cms_Signing_RequiresPrivateKey);
            }
        }

        internal SignerInfoAsn Sign(
            ReadOnlyMemory<byte> data,
            string? contentTypeOid,
            bool silent,
            out X509Certificate2Collection chainCerts)
        {
            HashAlgorithmName hashAlgorithmName = PkcsHelpers.GetDigestAlgorithm(DigestAlgorithm);
            IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithmName);

            hasher.AppendData(data.Span);
            byte[] dataHash = hasher.GetHashAndReset();

            SignerInfoAsn newSignerInfo = default;
            newSignerInfo.DigestAlgorithm.Algorithm = DigestAlgorithm.Value!;

            // If the user specified attributes (not null, count > 0) we need attributes.
            // If the content type is null we're counter-signing, and need the message digest attr.
            // If the content type is otherwise not-data we need to record it as the content-type attr.
            if (SignedAttributes?.Count > 0 || contentTypeOid != Oids.Pkcs7Data)
            {
                List<AttributeAsn> signedAttrs = BuildAttributes(SignedAttributes);

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                writer.WriteOctetString(dataHash);

                signedAttrs.Add(
                    new AttributeAsn
                    {
                        AttrType = Oids.MessageDigest,
                        AttrValues = new[] { new ReadOnlyMemory<byte>(writer.Encode()) },
                    });

                if (contentTypeOid != null)
                {
                    writer.Reset();
                    writer.WriteObjectIdentifierForCrypto(contentTypeOid);

                    signedAttrs.Add(
                        new AttributeAsn
                        {
                            AttrType = Oids.ContentType,
                            AttrValues = new[] { new ReadOnlyMemory<byte>(writer.Encode()) },
                        });
                }

                // Use the serializer/deserializer to DER-normalize the attribute order.
                SignedAttributesSet signedAttrsSet = default;
                signedAttrsSet.SignedAttributes = PkcsHelpers.NormalizeAttributeSet(
                    signedAttrs.ToArray(),
                    normalized => hasher.AppendData(normalized));

                // Since this contains user data in a context where BER is permitted, use BER.
                // There shouldn't be any observable difference here between BER and DER, though,
                // since the top level fields were written by NormalizeSet.
                AsnWriter attrsWriter = new AsnWriter(AsnEncodingRules.BER);
                signedAttrsSet.Encode(attrsWriter);
                newSignerInfo.SignedAttributes = attrsWriter.Encode();

                dataHash = hasher.GetHashAndReset();
            }

            switch (SignerIdentifierType)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    byte[] serial = Certificate!.GetSerialNumber();
                    Array.Reverse(serial);

                    newSignerInfo.Sid.IssuerAndSerialNumber = new IssuerAndSerialNumberAsn
                    {
                        Issuer = Certificate.IssuerName.RawData,
                        SerialNumber = serial,
                    };

                    newSignerInfo.Version = 1;
                    break;
                case SubjectIdentifierType.SubjectKeyIdentifier:
                    newSignerInfo.Sid.SubjectKeyIdentifier = PkcsPal.Instance.GetSubjectKeyIdentifier(Certificate!);
                    newSignerInfo.Version = 3;
                    break;
                case SubjectIdentifierType.NoSignature:
                    newSignerInfo.Sid.IssuerAndSerialNumber = new IssuerAndSerialNumberAsn
                    {
                        Issuer = SubjectIdentifier.DummySignerEncodedValue,
                        SerialNumber = new byte[1],
                    };
                    newSignerInfo.Version = 1;
                    break;
                default:
                    Debug.Fail($"Unresolved SignerIdentifierType value: {SignerIdentifierType}");
                    throw new CryptographicException();
            }

            if (UnsignedAttributes != null && UnsignedAttributes.Count > 0)
            {
                List<AttributeAsn> attrs = BuildAttributes(UnsignedAttributes);

                newSignerInfo.UnsignedAttributes = PkcsHelpers.NormalizeAttributeSet(attrs.ToArray());
            }

            bool signed;
            string? signatureAlgorithm;
            ReadOnlyMemory<byte> signatureValue;

            if (SignerIdentifierType == SubjectIdentifierType.NoSignature)
            {
                signatureAlgorithm = Oids.NoSignature;
                signatureValue = dataHash;
                signed = true;
            }
            else
            {
                signed = CmsSignature.Sign(
                    dataHash,
                    hashAlgorithmName,
                    Certificate!,
                    PrivateKey,
                    silent,
                    out signatureAlgorithm,
                    out signatureValue);
            }

            if (!signed)
            {
                throw new CryptographicException(SR.Cryptography_Cms_CannotDetermineSignatureAlgorithm);
            }

            newSignerInfo.SignatureValue = signatureValue;
            newSignerInfo.SignatureAlgorithm.Algorithm = signatureAlgorithm!;

            X509Certificate2Collection certs = new X509Certificate2Collection();
            certs.AddRange(Certificates);

            if (SignerIdentifierType != SubjectIdentifierType.NoSignature)
            {
                if (IncludeOption == X509IncludeOption.EndCertOnly)
                {
                    certs.Add(Certificate!);
                }
                else if (IncludeOption != X509IncludeOption.None)
                {
                    X509Chain chain = new X509Chain();
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    chain.ChainPolicy.VerificationTime = Certificate!.NotBefore;

                    if (!chain.Build(Certificate!))
                    {
                        foreach (X509ChainStatus status in chain.ChainStatus)
                        {
                            if (status.Status == X509ChainStatusFlags.PartialChain)
                            {
                                if (chain.ChainElements.Count == 0)
                                {
                                    // On Android, we will fail with PartialChain to build a cert chain
                                    // even if the failure is an untrusted root cert since the underlying platform
                                    // does not provide a way to distinguish the failure.
                                    // In that case, just use the provided cert.
                                    certs.Add(Certificate!);
                                }
                                else
                                {
                                    throw new CryptographicException(SR.Cryptography_Cms_IncompleteCertChain);
                                }
                            }
                        }
                    }

                    X509ChainElementCollection elements = chain.ChainElements;
                    int count = elements.Count;
                    int last = count - 1;

                    if (last == 0)
                    {
                        // If there's always one cert treat it as EE, not root.
                        last = -1;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        X509Certificate2 cert = elements[i].Certificate;

                        if (i == last &&
                            IncludeOption == X509IncludeOption.ExcludeRoot &&
                            cert.SubjectName.RawData.AsSpan().SequenceEqual(cert.IssuerName.RawData))
                        {
                            break;
                        }

                        certs.Add(cert);
                    }
                }
            }

            chainCerts = certs;
            return newSignerInfo;
        }

        internal static List<AttributeAsn> BuildAttributes(CryptographicAttributeObjectCollection? attributes)
        {
            List<AttributeAsn> signedAttrs = new List<AttributeAsn>();

            if (attributes == null || attributes.Count == 0)
            {
                return signedAttrs;
            }

            foreach (CryptographicAttributeObject attributeObject in attributes)
            {
                AttributeAsn newAttr = new AttributeAsn
                {
                    AttrType = attributeObject.Oid!.Value!,
                    AttrValues = new ReadOnlyMemory<byte>[attributeObject.Values.Count],
                };

                for (int i = 0; i < attributeObject.Values.Count; i++)
                {
                    newAttr.AttrValues[i] = attributeObject.Values[i].RawData;
                }

                signedAttrs.Add(newAttr);
            }

            return signedAttrs;
        }
    }
}
