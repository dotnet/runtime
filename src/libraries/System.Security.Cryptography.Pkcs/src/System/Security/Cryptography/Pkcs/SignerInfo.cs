// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class SignerInfo
    {
        public int Version { get; }
        public SubjectIdentifier SignerIdentifier { get; }

        private readonly Oid _digestAlgorithm;
        private readonly AttributeAsn[] _signedAttributes;
        private readonly Oid _signatureAlgorithm;
        private readonly ReadOnlyMemory<byte>? _signatureAlgorithmParameters;
        private readonly ReadOnlyMemory<byte> _signature;
        private readonly AttributeAsn[] _unsignedAttributes;

        private readonly SignedCms _document;
        private X509Certificate2 _signerCertificate;
        private SignerInfo _parentSignerInfo;
        private CryptographicAttributeObjectCollection _parsedSignedAttrs;
        private CryptographicAttributeObjectCollection _parsedUnsignedAttrs;

        internal SignerInfo(ref SignerInfoAsn parsedData, SignedCms ownerDocument)
        {
            Version = parsedData.Version;
            SignerIdentifier = new SubjectIdentifier(parsedData.Sid);
            _digestAlgorithm = parsedData.DigestAlgorithm.Algorithm;
            _signedAttributes = parsedData.SignedAttributes;
            _signatureAlgorithm = parsedData.SignatureAlgorithm.Algorithm;
            _signatureAlgorithmParameters = parsedData.SignatureAlgorithm.Parameters;
            _signature = parsedData.SignatureValue;
            _unsignedAttributes = parsedData.UnsignedAttributes;

            _document = ownerDocument;
        }

        public CryptographicAttributeObjectCollection SignedAttributes
        {
            get
            {
                if (_parsedSignedAttrs == null)
                {
                    _parsedSignedAttrs = MakeAttributeCollection(_signedAttributes);
                }

                return _parsedSignedAttrs;
            }
        }

        public CryptographicAttributeObjectCollection UnsignedAttributes
        {
            get
            {
                if (_parsedUnsignedAttrs == null)
                {
                    _parsedUnsignedAttrs = MakeAttributeCollection(_unsignedAttributes);
                }

                return _parsedUnsignedAttrs;
            }
        }

        public byte[] GetSignature() => _signature.ToArray();

        public X509Certificate2 Certificate
        {
            get
            {
                if (_signerCertificate == null)
                {
                    _signerCertificate = FindSignerCertificate();
                }

                return _signerCertificate;
            }
        }

        public SignerInfoCollection CounterSignerInfos
        {
            get
            {
                // We only support one level of counter signing.
                if (_parentSignerInfo != null ||
                    _unsignedAttributes == null ||
                    _unsignedAttributes.Length == 0)
                {
                    return new SignerInfoCollection();
                }

                return GetCounterSigners(_unsignedAttributes);
            }
        }

        public Oid DigestAlgorithm => new Oid(_digestAlgorithm);

        public Oid SignatureAlgorithm => new Oid(_signatureAlgorithm);

        private SignerInfoCollection GetCounterSigners(AttributeAsn[] unsignedAttrs)
        {
            // Since each "attribute" can have multiple "attribute values" there's no real
            // correlation to a predictive size here.
            List<SignerInfo> signerInfos = new List<SignerInfo>();

            foreach (AttributeAsn attributeAsn in unsignedAttrs)
            {
                if (attributeAsn.AttrType.Value == Oids.CounterSigner)
                {
                    AsnReader reader = new AsnReader(attributeAsn.AttrValues, AsnEncodingRules.BER);
                    AsnReader collReader = reader.ReadSetOf();

                    if (reader.HasData)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    while (collReader.HasData)
                    {
                        SignerInfoAsn parsedData =
                            AsnSerializer.Deserialize<SignerInfoAsn>(collReader.GetEncodedValue(), AsnEncodingRules.BER);

                        SignerInfo signerInfo = new SignerInfo(ref parsedData, _document)
                        {
                            _parentSignerInfo = this
                        };

                        signerInfos.Add(signerInfo);
                    }
                }
            }

            return new SignerInfoCollection(signerInfos.ToArray());
        }

        public void ComputeCounterSignature()
        {
            throw new PlatformNotSupportedException(SR.Cryptography_Cms_NoSignerCert);
        }

        public void ComputeCounterSignature(CmsSigner signer)
        {
            if (_parentSignerInfo != null)
                throw new CryptographicException(SR.Cryptography_Cms_NoCounterCounterSigner);
            if (signer == null)
                throw new ArgumentNullException(nameof(signer));

            signer.CheckCertificateValue();

            int myIdx = _document.SignerInfos.FindIndexForSigner(this);

            if (myIdx < 0)
            {
                throw new CryptographicException(SR.Cryptography_Cms_SignerNotFound);
            }

            // Make sure that we're using the most up-to-date version of this that we can.
            SignerInfo effectiveThis = _document.SignerInfos[myIdx];
            X509Certificate2Collection chain;
            SignerInfoAsn newSignerInfo = signer.Sign(effectiveThis._signature, null, false, out chain);

            AttributeAsn newUnsignedAttr;

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSetOf();
                AsnSerializer.Serialize(newSignerInfo, writer);
                writer.PopSetOf();

                newUnsignedAttr = new AttributeAsn
                {
                    AttrType = new Oid(Oids.CounterSigner, Oids.CounterSigner),
                    AttrValues = writer.Encode(),
                };
            }

            ref SignedDataAsn signedData = ref _document.GetRawData();
            ref SignerInfoAsn mySigner = ref signedData.SignerInfos[myIdx];

            int newExtensionIdx;

            if (mySigner.UnsignedAttributes == null)
            {
                mySigner.UnsignedAttributes = new AttributeAsn[1];
                newExtensionIdx = 0;
            }
            else
            {
                newExtensionIdx = mySigner.UnsignedAttributes.Length;
                Array.Resize(ref mySigner.UnsignedAttributes, newExtensionIdx + 1);
            }

            mySigner.UnsignedAttributes[newExtensionIdx] = newUnsignedAttr;
            _document.UpdateCertificatesFromAddition(chain);
            // Re-normalize the document
            _document.Reencode();
        }

        public void RemoveCounterSignature(int index)
        {
            if (index < 0)
            {
                // In NetFx RemoveCounterSignature doesn't bounds check, but the helper it calls does.
                // In the helper the argument is called "childIndex".
                throw new ArgumentOutOfRangeException("childIndex");
            }

            // The SignerInfo class is a projection of data contained within the SignedCms.
            // The projection is applied at construction time, and is not live.
            // So RemoveCounterSignature modifies _document, not this.
            // (Because that's what NetFx does)

            int myIdx = _document.SignerInfos.FindIndexForSigner(this);

            // We've been removed.
            if (myIdx < 0)
            {
                throw new CryptographicException(SR.Cryptography_Cms_SignerNotFound);
            }

            ref SignedDataAsn parentData = ref _document.GetRawData();
            ref SignerInfoAsn myData = ref parentData.SignerInfos[myIdx];

            if (myData.UnsignedAttributes == null)
            {
                throw new CryptographicException(SR.Cryptography_Cms_NoSignerAtIndex);
            }

            int removeAttrIdx = -1;
            int removeValueIndex = -1;
            bool removeWholeAttr = false;
            int csIndex = 0;

            AttributeAsn[] unsignedAttrs = myData.UnsignedAttributes;

            for (var i = 0; i < unsignedAttrs.Length; i++)
            {
                AttributeAsn attributeAsn = unsignedAttrs[i];

                if (attributeAsn.AttrType.Value == Oids.CounterSigner)
                {
                    AsnReader reader = new AsnReader(attributeAsn.AttrValues, AsnEncodingRules.BER);
                    AsnReader collReader = reader.ReadSetOf();

                    if (reader.HasData)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    int j = 0;

                    while (collReader.HasData)
                    {
                        collReader.GetEncodedValue();

                        if (csIndex == index)
                        {
                            removeAttrIdx = i;
                            removeValueIndex = j;
                        }

                        csIndex++;
                        j++;
                    }

                    if (removeValueIndex == 0 && j == 1)
                    {
                        removeWholeAttr = true;
                    }

                    if (removeAttrIdx >= 0)
                    {
                        break;
                    }
                }
            }

            if (removeAttrIdx < 0)
            {
                throw new CryptographicException(SR.Cryptography_Cms_NoSignerAtIndex);
            }

            // The easy path:
            if (removeWholeAttr)
            {
                // Empty needs to normalize to null.
                if (unsignedAttrs.Length == 1)
                {
                    myData.UnsignedAttributes = null;
                }
                else
                {
                    Helpers.RemoveAt(ref myData.UnsignedAttributes, removeAttrIdx);
                }
            }
            else
            {
                ref AttributeAsn modifiedAttr = ref unsignedAttrs[removeAttrIdx];

                using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
                {
                    writer.PushSetOf();

                    AsnReader reader = new AsnReader(modifiedAttr.AttrValues, writer.RuleSet);

                    int i = 0;

                    while (reader.HasData)
                    {
                        ReadOnlyMemory<byte> encodedValue = reader.GetEncodedValue();

                        if (i != removeValueIndex)
                        {
                            writer.WriteEncodedValue(encodedValue);
                        }

                        i++;
                    }

                    writer.PopSetOf();
                    modifiedAttr.AttrValues = writer.Encode();
                }
            }
        }

        public void RemoveCounterSignature(SignerInfo counterSignerInfo)
        {
            if (counterSignerInfo == null)
                throw new ArgumentNullException(nameof(counterSignerInfo));

            SignerInfoCollection docSigners = _document.SignerInfos;
            int index = docSigners.FindIndexForSigner(this);

            if (index < 0)
            {
                throw new CryptographicException(SR.Cryptography_Cms_SignerNotFound);
            }

            SignerInfo liveThis = docSigners[index];
            index = liveThis.CounterSignerInfos.FindIndexForSigner(counterSignerInfo);

            if (index < 0)
            {
                throw new CryptographicException(SR.Cryptography_Cms_SignerNotFound);
            }

            RemoveCounterSignature(index);
        }

        public void CheckSignature(bool verifySignatureOnly) =>
            CheckSignature(new X509Certificate2Collection(), verifySignatureOnly);

        public void CheckSignature(X509Certificate2Collection extraStore, bool verifySignatureOnly)
        {
            if (extraStore == null)
                throw new ArgumentNullException(nameof(extraStore));

            X509Certificate2 certificate = Certificate;

            if (certificate == null)
            {
                certificate = FindSignerCertificate(SignerIdentifier, extraStore);

                if (certificate == null)
                {
                    throw new CryptographicException(SR.Cryptography_Cms_SignerNotFound);
                }
            }

            Verify(extraStore, certificate, verifySignatureOnly);
        }

        public void CheckHash()
        {
            IncrementalHash hasher = PrepareDigest();
            byte[] expectedSignature = hasher.GetHashAndReset();

            if (!_signature.Span.SequenceEqual(expectedSignature))
            {
                throw new CryptographicException(SR.Cryptography_BadSignature);
            }
        }

        private X509Certificate2 FindSignerCertificate()
        {
            return FindSignerCertificate(SignerIdentifier, _document.Certificates);
        }

        private static X509Certificate2 FindSignerCertificate(
            SubjectIdentifier signerIdentifier,
            X509Certificate2Collection extraStore)
        {
            if (extraStore == null || extraStore.Count == 0)
            {
                return null;
            }

            X509Certificate2Collection filtered = null;
            X509Certificate2 match = null;

            switch (signerIdentifier.Type)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                {
                    X509IssuerSerial issuerSerial = (X509IssuerSerial)signerIdentifier.Value;
                    filtered = extraStore.Find(X509FindType.FindBySerialNumber, issuerSerial.SerialNumber, false);

                    foreach (X509Certificate2 cert in filtered)
                    {
                        if (cert.IssuerName.Name == issuerSerial.IssuerName)
                        {
                            match = cert;
                            break;
                        }
                    }

                    break;
                }
                case SubjectIdentifierType.SubjectKeyIdentifier:
                {
                    filtered = extraStore.Find(X509FindType.FindBySubjectKeyIdentifier, signerIdentifier.Value, false);

                    if (filtered.Count > 0)
                    {
                        match = filtered[0];
                    }

                    break;
                }
            }

            if (filtered != null)
            {
                foreach (X509Certificate2 cert in filtered)
                {
                    if (!ReferenceEquals(cert, match))
                    {
                        cert.Dispose();
                    }
                }
            }

            return match;
        }

        private IncrementalHash PrepareDigest()
        {
            HashAlgorithmName hashAlgorithmName = GetDigestAlgorithm();

            IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithmName);

            if (_parentSignerInfo == null)
            {
                // Windows compatibility: If a document was loaded in detached mode,
                // but had content, hash both parts of the content.
                if (_document.Detached)
                {
                    ref SignedDataAsn documentData = ref _document.GetRawData();
                    ReadOnlyMemory<byte>? embeddedContent = documentData.EncapContentInfo.Content;

                    if (embeddedContent != null)
                    {
                        hasher.AppendData(embeddedContent.Value.Span);
                    }

                }

                hasher.AppendData(_document.GetContentSpan());
            }
            else
            {
                hasher.AppendData(_parentSignerInfo._signature.Span);
            }

            // A Counter-Signer always requires signed attributes.
            // If any signed attributes are present, message-digest is required.
            bool invalid = _parentSignerInfo != null || _signedAttributes != null;

            if (_signedAttributes != null)
            {
                byte[] contentDigest = hasher.GetHashAndReset();

                using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
                {
                    writer.PushSetOf();

                    foreach (AttributeAsn attr in _signedAttributes)
                    {
                        AsnSerializer.Serialize(attr, writer);

                        // .NET Framework doesn't seem to validate the content type attribute,
                        // so we won't, either.

                        if (attr.AttrType.Value == Oids.MessageDigest)
                        {
                            CryptographicAttributeObject obj = MakeAttribute(attr);

                            if (obj.Values.Count != 1)
                            {
                                throw new CryptographicException(SR.Cryptography_BadHashValue);
                            }

                            var digestAttr = (Pkcs9MessageDigest)obj.Values[0];

                            if (!contentDigest.AsSpan().SequenceEqual(digestAttr.MessageDigest.AsReadOnlySpan()))
                            {
                                throw new CryptographicException(SR.Cryptography_BadHashValue);
                            }

                            invalid = false;
                        }
                    }

                    writer.PopSetOf();
                    Helpers.DigestWriter(hasher, writer);
                }
            }

            if (invalid)
            {
                throw new CryptographicException(SR.Cryptography_Cms_MissingAuthenticatedAttribute);
            }

            return hasher;
        }

        private void Verify(
            X509Certificate2Collection extraStore,
            X509Certificate2 certificate,
            bool verifySignatureOnly)
        {
            IncrementalHash hasher = PrepareDigest();
            CmsSignature signatureProcessor = CmsSignature.Resolve(SignatureAlgorithm.Value);

            if (signatureProcessor == null)
            {
                throw new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, SignatureAlgorithm.Value);
            }

#if netcoreapp
            // SHA-2-512 is the biggest digest type we know about.
            Span<byte> digestValue = stackalloc byte[512 / 8];
            ReadOnlySpan<byte> digest = digestValue;
            ReadOnlyMemory<byte> signature = _signature;

            if (hasher.TryGetHashAndReset(digestValue, out int bytesWritten))
            {
                digest = digestValue.Slice(0, bytesWritten);
            }
            else
            {
                digest = hasher.GetHashAndReset();
            }
#else
            byte[] digest = hasher.GetHashAndReset();
            byte[] signature = _signature.ToArray();
#endif

            bool signatureValid = signatureProcessor.VerifySignature(
                digest,
                signature,
                DigestAlgorithm.Value,
                hasher.AlgorithmName,
                _signatureAlgorithmParameters,
                certificate);

            if (!signatureValid)
            {
                throw new CryptographicException(SR.Cryptography_BadSignature);
            }

            if (!verifySignatureOnly)
            {
                X509Chain chain = new X509Chain();
                chain.ChainPolicy.ExtraStore.AddRange(extraStore);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                if (!chain.Build(certificate))
                {
                    X509ChainStatus status = chain.ChainStatus.FirstOrDefault();
                    throw new CryptographicException(SR.Cryptography_Cms_TrustFailure, status.StatusInformation);
                }

                // NetFx checks for either of these
                const X509KeyUsageFlags SufficientFlags =
                    X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.NonRepudiation;

                foreach (X509Extension ext in certificate.Extensions)
                {
                    if (ext.Oid.Value == Oids.KeyUsage)
                    {
                        if (!(ext is X509KeyUsageExtension keyUsage))
                        {
                            keyUsage = new X509KeyUsageExtension();
                            keyUsage.CopyFrom(ext);
                        }

                        if ((keyUsage.KeyUsages & SufficientFlags) == 0)
                        {
                            throw new CryptographicException(SR.Cryptography_Cms_WrongKeyUsage);
                        }
                    }
                }
            }
        }

        private HashAlgorithmName GetDigestAlgorithm()
        {
            return Helpers.GetDigestAlgorithm(DigestAlgorithm.Value);
        }

        private static CryptographicAttributeObjectCollection MakeAttributeCollection(AttributeAsn[] attributes)
        {
            var coll = new CryptographicAttributeObjectCollection();

            if (attributes == null)
                return coll;

            foreach (AttributeAsn attribute in attributes)
            {
                coll.AddWithoutMerge(MakeAttribute(attribute));
            }

            return coll;
        }

        private static CryptographicAttributeObject MakeAttribute(AttributeAsn attribute)
        {
            Oid type = new Oid(attribute.AttrType);

            ReadOnlyMemory<byte> attrSetBytes = attribute.AttrValues;

            AsnReader reader = new AsnReader(attrSetBytes, AsnEncodingRules.BER);
            AsnReader collReader = reader.ReadSetOf();

            if (reader.HasData)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            AsnEncodedDataCollection valueColl = new AsnEncodedDataCollection();

            while (collReader.HasData)
            {
                byte[] attrBytes = collReader.GetEncodedValue().ToArray();
                valueColl.Add(Helpers.CreateBestPkcs9AttributeObjectAvailable(type, attrBytes));
            }

            return new CryptographicAttributeObject(type, valueColl);
        }
    }
}
