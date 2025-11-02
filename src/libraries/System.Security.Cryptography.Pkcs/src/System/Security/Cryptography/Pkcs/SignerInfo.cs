// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
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

        private readonly string _digestAlgorithm;
        private readonly AttributeAsn[]? _signedAttributes;
        private readonly ReadOnlyMemory<byte>? _signedAttributesMemory;
        private readonly string _signatureAlgorithm;
        private readonly ReadOnlyMemory<byte>? _signatureAlgorithmParameters;
        private readonly ReadOnlyMemory<byte> _signature;
        private readonly AttributeAsn[]? _unsignedAttributes;

        private readonly SignedCms _document;
        private X509Certificate2? _signerCertificate;
        private SignerInfo? _parentSignerInfo;
        private CryptographicAttributeObjectCollection? _parsedSignedAttrs;
        private CryptographicAttributeObjectCollection? _parsedUnsignedAttrs;

        internal SignerInfo(ref SignerInfoAsn parsedData, SignedCms ownerDocument)
        {
            Version = parsedData.Version;
            SignerIdentifier = new SubjectIdentifier(parsedData.Sid);
            _digestAlgorithm = parsedData.DigestAlgorithm.Algorithm;
            _signedAttributesMemory = parsedData.SignedAttributes;
            _signatureAlgorithm = parsedData.SignatureAlgorithm.Algorithm;
            _signatureAlgorithmParameters = parsedData.SignatureAlgorithm.Parameters;
            _signature = parsedData.SignatureValue;
            _unsignedAttributes = parsedData.UnsignedAttributes;

            if (_signedAttributesMemory.HasValue)
            {
                SignedAttributesSet signedSet = SignedAttributesSet.Decode(
                    _signedAttributesMemory.Value,
                    AsnEncodingRules.BER);

                _signedAttributes = signedSet.SignedAttributes;
                Debug.Assert(_signedAttributes != null);
            }

            _document = ownerDocument;
        }

        public CryptographicAttributeObjectCollection SignedAttributes =>
            _parsedSignedAttrs ??= PkcsHelpers.MakeAttributeCollection(_signedAttributes);

        public CryptographicAttributeObjectCollection UnsignedAttributes =>
            _parsedUnsignedAttrs ??= PkcsHelpers.MakeAttributeCollection(_unsignedAttributes);

        internal ReadOnlyMemory<byte> GetSignatureMemory() => _signature;

#if NET || NETSTANDARD2_1
        public byte[] GetSignature() => _signature.ToArray();
#endif

        public X509Certificate2? Certificate =>
            _signerCertificate ??= FindSignerCertificate();

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

        public Oid DigestAlgorithm => new Oid(_digestAlgorithm, null);

#if NET || NETSTANDARD2_1
        public
#else
        internal
#endif
        Oid SignatureAlgorithm => new Oid(_signatureAlgorithm, null);

        private delegate void WithSelfInfoDelegate(ref SignerInfoAsn mySigned);

        private void WithSelfInfo(WithSelfInfoDelegate action)
        {
            if (_parentSignerInfo == null)
            {
                int myIdx = _document.SignerInfos.FindIndexForSigner(this);

                if (myIdx < 0)
                {
                    throw new CryptographicException(SR.Cryptography_Cms_SignerNotFound);
                }

                ref SignedDataAsn signedData = ref _document.GetRawData();
                ref SignerInfoAsn mySigner = ref signedData.SignerInfos[myIdx];

                action(ref mySigner);

                // Re-normalize the document
                _document.Reencode();
            }
            else
            {
                // we are one level deep, we need to update signer and counter signer attributes
                int parentIdx = _document.SignerInfos.FindIndexForSigner(_parentSignerInfo);

                if (parentIdx == -1)
                {
                    throw new CryptographicException(SR.Cryptography_Cms_NoSignerAtIndex);
                }

                ref SignedDataAsn documentData = ref _document.GetRawData();
                ref SignerInfoAsn parentData = ref documentData.SignerInfos[parentIdx];

                if (parentData.UnsignedAttributes == null)
                {
                    throw new CryptographicException(SR.Cryptography_Cms_NoSignerAtIndex);
                }

                ref AttributeAsn[] unsignedAttrs = ref parentData.UnsignedAttributes!;

                for (int i = 0; i < unsignedAttrs.Length; i++)
                {
                    ref AttributeAsn attributeAsn = ref unsignedAttrs[i];

                    if (attributeAsn.AttrType == Oids.CounterSigner)
                    {
                        for (int j = 0; j < attributeAsn.AttrValues.Length; j++)
                        {
                            ref ReadOnlyMemory<byte> counterSignerBytes = ref attributeAsn.AttrValues[j];
                            SignerInfoAsn counterSigner = SignerInfoAsn.Decode(counterSignerBytes, AsnEncodingRules.BER);

                            var counterSignerId = new SubjectIdentifier(counterSigner.Sid);

                            if (SignerIdentifier.IsEquivalentTo(counterSignerId))
                            {
                                // counterSigner represent the current state of `this`
                                action(ref counterSigner);

                                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                                counterSigner.Encode(writer);
                                counterSignerBytes = writer.Encode();

                                // Re-normalize the document
                                _document.Reencode();

                                return;
                            }
                        }
                    }
                }

                throw new CryptographicException(SR.Cryptography_Cms_NoSignerAtIndex);
            }
        }

#if NET || NETSTANDARD2_1
        public
#else
        internal
#endif
        void AddUnsignedAttribute(AsnEncodedData unsignedAttribute)
        {
            WithSelfInfo((ref SignerInfoAsn mySigner) =>
                {
                    AddUnsignedAttribute(ref mySigner, unsignedAttribute);
                });
        }

        private static void AddUnsignedAttribute(ref SignerInfoAsn mySigner, AsnEncodedData unsignedAttribute)
        {
            int existingAttribute = mySigner.UnsignedAttributes == null ? -1 : FindAttributeIndexByOid(mySigner.UnsignedAttributes, unsignedAttribute.Oid!);

            if (existingAttribute == -1)
            {
                // create a new attribute
                AttributeAsn newUnsignedAttr = new AttributeAsn(unsignedAttribute);
                int newAttributeIdx;

                if (mySigner.UnsignedAttributes == null)
                {
                    newAttributeIdx = 0;
                    mySigner.UnsignedAttributes = new AttributeAsn[1];
                }
                else
                {
                    newAttributeIdx = mySigner.UnsignedAttributes.Length;
                    Array.Resize(ref mySigner.UnsignedAttributes, newAttributeIdx + 1);
                }

                mySigner.UnsignedAttributes[newAttributeIdx] = newUnsignedAttr;
            }
            else
            {
                // merge with existing attribute
                ref AttributeAsn modifiedAttr = ref mySigner.UnsignedAttributes![existingAttribute];
                int newIndex = modifiedAttr.AttrValues.Length;
                Array.Resize(ref modifiedAttr.AttrValues, newIndex + 1);
                modifiedAttr.AttrValues[newIndex] = unsignedAttribute.RawData;
            }
        }

#if NET || NETSTANDARD2_1
        public
#else
        internal
#endif
        void RemoveUnsignedAttribute(AsnEncodedData unsignedAttribute)
        {
            WithSelfInfo((ref SignerInfoAsn mySigner) =>
                {
                    RemoveUnsignedAttribute(ref mySigner, unsignedAttribute);
                });
        }

        private static void RemoveUnsignedAttribute(ref SignerInfoAsn mySigner, AsnEncodedData unsignedAttribute)
        {
            (int outerIndex, int innerIndex) = FindAttributeLocation(mySigner.UnsignedAttributes, unsignedAttribute, out bool isOnlyValue);

            if (outerIndex == -1 || innerIndex == -1)
            {
                throw new CryptographicException(SR.Cryptography_Cms_NoAttributeFound);
            }

            if (isOnlyValue)
            {
                PkcsHelpers.RemoveAt(ref mySigner.UnsignedAttributes!, outerIndex);
            }
            else
            {
                PkcsHelpers.RemoveAt(ref mySigner.UnsignedAttributes![outerIndex].AttrValues, innerIndex);
            }
        }

        private SignerInfoCollection GetCounterSigners(AttributeAsn[] unsignedAttrs)
        {
            // Since each "attribute" can have multiple "attribute values" there's no real
            // correlation to a predictive size here.
            List<SignerInfo> signerInfos = new List<SignerInfo>();

            foreach (AttributeAsn attributeAsn in unsignedAttrs)
            {
                if (attributeAsn.AttrType == Oids.CounterSigner)
                {
                    foreach (ReadOnlyMemory<byte> attrValue in attributeAsn.AttrValues)
                    {
                        SignerInfoAsn parsedData = SignerInfoAsn.Decode(attrValue, AsnEncodingRules.BER);

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

#if NET
        [Obsolete(Obsoletions.SignerInfoCounterSigMessage, DiagnosticId = Obsoletions.SignerInfoCounterSigDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
 #endif
        [EditorBrowsable(EditorBrowsableState.Never)]
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

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            newSignerInfo.Encode(writer);

            newUnsignedAttr = new AttributeAsn
            {
                AttrType = Oids.CounterSigner,
                AttrValues = new[] { new ReadOnlyMemory<byte>(writer.Encode()) },
            };

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
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            // The SignerInfo class is a projection of data contained within the SignedCms.
            // The projection is applied at construction time, and is not live.
            // So RemoveCounterSignature modifies _document, not this.
            // (Because that's what .NET Framework does)

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

                if (attributeAsn.AttrType == Oids.CounterSigner)
                {
                    if (index < csIndex + attributeAsn.AttrValues.Length)
                    {
                        removeAttrIdx = i;
                        removeValueIndex = index - csIndex;
                        if (removeValueIndex == 0 && attributeAsn.AttrValues.Length == 1)
                        {
                            removeWholeAttr = true;
                        }
                        break;
                    }

                    csIndex += attributeAsn.AttrValues.Length;
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
                    PkcsHelpers.RemoveAt(ref myData.UnsignedAttributes, removeAttrIdx);
                }
            }
            else
            {
                PkcsHelpers.RemoveAt(ref unsignedAttrs[removeAttrIdx].AttrValues, removeValueIndex);
            }
        }

        public void RemoveCounterSignature(SignerInfo counterSignerInfo)
        {
            ArgumentNullException.ThrowIfNull(counterSignerInfo);

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
            ArgumentNullException.ThrowIfNull(extraStore);

            X509Certificate2? certificate = Certificate;

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
            if (_signatureAlgorithm != Oids.NoSignature)
            {
                throw new CryptographicException(SR.Cryptography_Pkcs_InvalidSignatureParameters);
            }

            if (!CheckHash(compatMode: false) && !CheckHash(compatMode: true))
            {
                throw new CryptographicException(SR.Cryptography_BadSignature);
            }
        }

        private bool CheckHash(bool compatMode)
        {
            // compatMode only affects attribute processing so if there are none then
            // compatMode true and false are the same. So short circuit when true.
            if (_signedAttributes == null && compatMode)
            {
                return false;
            }

            Debug.Assert(_signatureAlgorithm == Oids.NoSignature);

            // The signature is a hash of the message or signed attributes.
            return VerifyHashedMessage(
                compatMode,
                _signature,
                static (signature, contentToVerify) => signature.Span.SequenceEqual(contentToVerify));
        }

        private X509Certificate2? FindSignerCertificate()
        {
            return FindSignerCertificate(SignerIdentifier, _document.Certificates);
        }

        private static X509Certificate2? FindSignerCertificate(
            SubjectIdentifier signerIdentifier,
            X509Certificate2Collection? extraStore)
        {
            if (extraStore == null || extraStore.Count == 0)
            {
                return null;
            }

            X509Certificate2Collection? filtered = null;
            X509Certificate2? match = null;

            switch (signerIdentifier.Type)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                {
                    X509IssuerSerial issuerSerial = (X509IssuerSerial)signerIdentifier.Value!;
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
                    filtered = extraStore.Find(X509FindType.FindBySubjectKeyIdentifier, signerIdentifier.Value!, false);

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

        private ReadOnlyMemory<byte> GetContentForVerification(out ReadOnlyMemory<byte>? additionalContent)
        {
            additionalContent = null;
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
                        // Unwrap the OCTET STRING manually, because of PKCS#7 compatibility.
                        // https://tools.ietf.org/html/rfc5652#section-5.2.1
                        ReadOnlyMemory<byte> hashableContent = SignedCms.GetContent(
                            embeddedContent.Value,
                            documentData.EncapContentInfo.ContentType);

                        additionalContent = hashableContent;
                    }
                }

                return _document.GetHashableContentMemory();
            }
            else
            {
                // We are a counter-signer, so the content is the signature of the parent
                return _parentSignerInfo._signature;
            }
        }

        private CmsHash GetContentHash(ReadOnlyMemory<byte> content, ReadOnlyMemory<byte>? additionalContent)
        {
            CmsHash hasher = CmsHash.Create(DigestAlgorithm, forVerification: true);
            if (additionalContent.HasValue)
            {
                hasher.AppendData(additionalContent.Value.Span);
            }

            hasher.AppendData(content.Span);

            return hasher;
        }

        private static bool VerifyAttributes<TState>(
            ReadOnlySpan<byte> digest,
            AttributeAsn[] signedAttributes,
            bool compatMode,
            bool needsContentAttr,
            TState state,
            VerifyCallback<TState> verify)
        {
            bool hasMatchingDigestAttr = false;
            bool hasContentAttr = false;

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // Some CMS implementations exist which do not sort the attributes prior to
            // generating the signature.  While they are not, technically, validly signed,
            // Windows and OpenSSL both support trying in the document order rather than
            // a sorted order.  To accomplish this we will build as a SEQUENCE OF, but feed
            // the SET OF into the hasher.
            using (compatMode ? writer.PushSequence() : writer.PushSetOf())
            {
                foreach (AttributeAsn attr in signedAttributes)
                {
                    attr.Encode(writer);

                    // .NET Framework doesn't seem to validate the content type attribute,
                    // so we won't, either.

                    if (attr.AttrType == Oids.MessageDigest)
                    {
                        CryptographicAttributeObject obj = PkcsHelpers.MakeAttribute(attr);

                        if (obj.Values.Count != 1)
                        {
                            throw new CryptographicException(SR.Cryptography_BadHashValue);
                        }

                        var digestAttr = (Pkcs9MessageDigest)obj.Values[0];

                        if (!digest.SequenceEqual(digestAttr.MessageDigest))
                        {
                            throw new CryptographicException(SR.Cryptography_BadHashValue);
                        }

                        hasMatchingDigestAttr = true;
                    }
                    else if (attr.AttrType == Oids.ContentType)
                    {
                        hasContentAttr = true;
                    }
                }
            }

            // Message-digest is required when signed attributes are present.
            if (!hasMatchingDigestAttr || (needsContentAttr && !hasContentAttr))
            {
                throw new CryptographicException(SR.Cryptography_Cms_MissingAuthenticatedAttribute);
            }

            if (compatMode)
            {
                int encodedLength = writer.GetEncodedLength();
                byte[]? rented = null;
                Span<byte> encoded = encodedLength <= 256
                    ? stackalloc byte[256]
                    : (rented = CryptoPool.Rent(encodedLength));

                try
                {
                    encoded = encoded.Slice(0, encodedLength);
                    writer.Encode(encoded);
                    encoded[0] = 0x31;
                    return verify(state, encoded);
                }
                finally
                {
                    if (rented != null)
                    {
                        CryptoPool.Return(rented);
                    }
                }
            }
            else
            {
#if NET9_0_OR_GREATER
                return writer.Encode((state, verify), static (state, encoded) => state.verify(state.state, encoded));
#else
                return verify(state, writer.Encode());
#endif
            }
        }

        private delegate bool VerifyCallback<TState>(TState state, ReadOnlySpan<byte> contentToVerify);

        private bool VerifyHashedMessage<TState>(bool compatMode, TState state, VerifyCallback<TState> verify)
        {
            ReadOnlyMemory<byte> content = GetContentForVerification(out ReadOnlyMemory<byte>? additionalContent);

            using (CmsHash hasher = GetContentHash(content, additionalContent))
            {
#if NET || NETSTANDARD2_1
                // SHA-2-512 is the biggest digest type we know about.
                Span<byte> contentHash = stackalloc byte[512 / 8];

                if (hasher.TryGetHashAndReset(contentHash, out int bytesWritten))
                {
                    contentHash = contentHash.Slice(0, bytesWritten);
                }
                else
                {
                    contentHash = hasher.GetHashAndReset();
                }
#else
                byte[] contentHash = hasher.GetHashAndReset();
#endif

                // If there are no signed attributes, we can just verify the content directly.
                if (_signedAttributes == null)
                {
                    // A Counter-Signer always requires signed attributes.
                    if (_parentSignerInfo != null)
                    {
                        throw new CryptographicException(SR.Cryptography_Cms_MissingAuthenticatedAttribute);
                    }

                    return verify(state, contentHash);
                }

                // Since there are signed attributes, we need to verify those instead.
                return
                    VerifyAttributes(
                        contentHash,
                        _signedAttributes,
                        compatMode,
                        needsContentAttr: false,
                        (state, hasher, verify),
                        static (state, span) =>
                        {
                            CmsHash hasher = state.hasher;
                            hasher.AppendData(span);

#if NET || NETSTANDARD2_1
                            // SHA-2-512 is the biggest digest type we know about.
                            Span<byte> attrHash = stackalloc byte[512 / 8];

                            if (hasher.TryGetHashAndReset(attrHash, out int bytesWritten))
                            {
                                attrHash = attrHash.Slice(0, bytesWritten);
                            }
                            else
                            {
                                attrHash = hasher.GetHashAndReset();
                            }
#else
                            byte[] attrHash = hasher.GetHashAndReset();
#endif

                            return state.verify(state.state, attrHash);
                        });
            }
        }

        private bool VerifyPureMessage<TState>(bool compatMode, TState state, VerifyCallback<TState> verify)
        {
            ReadOnlyMemory<byte> content = GetContentForVerification(out ReadOnlyMemory<byte>? additionalContent);

            // If there are no signed attributes, we can just verify the content directly.
            if (_signedAttributes == null)
            {
                // A Counter-Signer always requires signed attributes.
                if (_parentSignerInfo != null)
                {
                    throw new CryptographicException(SR.Cryptography_Cms_MissingAuthenticatedAttribute);
                }

                if (!additionalContent.HasValue)
                {
                    return verify(state, content.Span);
                }

                // If there are multiple pieces of content, concatenate them and verify.
                int contentToVerifyLength = content.Length + additionalContent.Value.Length;
                byte[] rented = CryptoPool.Rent(contentToVerifyLength);
                try
                {
                    additionalContent.Value.Span.CopyTo(rented);
                    content.Span.CopyTo(rented.AsSpan(additionalContent.Value.Length));
                    return verify(state, rented.AsSpan(0, contentToVerifyLength));
                }
                finally
                {
                    CryptoPool.Return(rented);
                }
            }

            // Since there are signed attributes, we need to verify those instead.
            using (CmsHash hasher = GetContentHash(content, additionalContent))
            {
#if NET || NETSTANDARD2_1
                // SHA-2-512 is the biggest digest type we know about.
                Span<byte> contentHash = stackalloc byte[512 / 8];

                if (hasher.TryGetHashAndReset(contentHash, out int bytesWritten))
                {
                    contentHash = contentHash.Slice(0, bytesWritten);
                }
                else
                {
                    contentHash = hasher.GetHashAndReset();
                }
#else
                byte[] contentHash = hasher.GetHashAndReset();
#endif

                return
                    VerifyAttributes(
                        contentHash,
                        _signedAttributes,
                        compatMode,
                        // IETF spec for SLH-DSA/ML-DSA requires that the content type be present but RFC 5652 says
                        // it is invalid for countersigners. We'll just ignore it for now, but if we decide to
                        // allow countersigners to omit the content type, we should check that `this` is a countersigner.
                        needsContentAttr: false,
                        (state, verify),
                        static (state, span) => state.verify(state.state, span));
            }
        }

        private void Verify(
            X509Certificate2Collection extraStore,
            X509Certificate2 certificate,
            bool verifySignatureOnly)
        {
            // SignatureAlgorithm always 'wins' so we don't need to pass in an rsaSignaturePadding
            CmsSignature? signatureProcessor = CmsSignature.ResolveAndVerifyKeyType(
                SignatureAlgorithm.Value!,
                key: null,
                rsaSignaturePadding: null);

            if (signatureProcessor == null)
            {
                throw new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, SignatureAlgorithm.Value);
            }

            bool signatureValid =
                VerifySignature(signatureProcessor, certificate, compatMode: false) ||
                VerifySignature(signatureProcessor, certificate, compatMode: true);

            if (!signatureValid)
            {
                throw new CryptographicException(SR.Cryptography_BadSignature);
            }

            if (!verifySignatureOnly)
            {
                X509Chain chain = new X509Chain();
                try
                {
                    chain.ChainPolicy.ExtraStore.AddRange(extraStore);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;

                    if (!chain.Build(certificate))
                    {
                        X509ChainStatus status = chain.ChainStatus.FirstOrDefault();
                        throw new CryptographicException(SR.Cryptography_Cms_TrustFailure, status.StatusInformation);
                    }
                }
                finally
                {
                    for (int i = 0; i < chain.ChainElements.Count; i++)
                    {
                        chain.ChainElements[i].Certificate.Dispose();
                    }

                    chain.Dispose();
                }

                // .NET Framework checks for either of these
                const X509KeyUsageFlags SufficientFlags =
                    X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.NonRepudiation;

                foreach (X509Extension ext in certificate.Extensions)
                {
                    if (ext.Oid!.Value == Oids.KeyUsage)
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

        private bool VerifySignature(
            CmsSignature signatureProcessor,
            X509Certificate2 certificate,
            bool compatMode)
        {
            // compatMode only affects attribute processing so if there are none then
            // compatMode true and false are the same. So short circuit when true.
            if (_signedAttributes == null && compatMode)
            {
                return false;
            }

            if (signatureProcessor.NeedsHashedMessage)
            {
                return VerifyHashedMessage(
                    compatMode,
                    (info: this, signatureProcessor, certificate),
                    static (state, contentToVerify) =>
                        state.signatureProcessor.VerifySignature(
#if NET || NETSTANDARD2_1
                            contentToVerify,
                            state.info._signature,
#else
                            contentToVerify.ToArray(),
                            state.info._signature.ToArray(),
#endif
                            state.info.DigestAlgorithm.Value,
                            state.info._signatureAlgorithmParameters,
                            state.certificate));
            }
            else
            {
                return VerifyPureMessage(
                    compatMode,
                    (info: this, signatureProcessor, certificate),
                    static (state, contentToVerify) =>
                        state.signatureProcessor.VerifySignature(
#if NET || NETSTANDARD2_1
                            contentToVerify,
                            state.info._signature,
#else
                            contentToVerify.ToArray(),
                            state.info._signature.ToArray(),
#endif
                            state.info.DigestAlgorithm.Value,
                            state.info._signatureAlgorithmParameters,
                            state.certificate));
            }
        }

        private static int FindAttributeIndexByOid(AttributeAsn[] attributes, Oid oid, int startIndex = 0)
        {
            if (attributes != null)
            {
                for (int i = startIndex; i < attributes.Length; i++)
                {
                    if (attributes[i].AttrType == oid.Value)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static int FindAttributeValueIndexByEncodedData(ReadOnlyMemory<byte>[] attributeValues, ReadOnlySpan<byte> asnEncodedData, out bool isOnlyValue)
        {
            if (attributeValues != null)
            {
                for (int i = 0; i < attributeValues.Length; i++)
                {
                    ReadOnlySpan<byte> data = attributeValues[i].Span;
                    if (data.SequenceEqual(asnEncodedData))
                    {
                        isOnlyValue = attributeValues.Length == 1;
                        return i;
                    }
                }
            }

            isOnlyValue = false;
            return -1;
        }

        private static (int, int) FindAttributeLocation(AttributeAsn[]? attributes, AsnEncodedData attribute, out bool isOnlyValue)
        {
            if (attributes != null)
            {
                for (int outerIndex = 0; ; outerIndex++)
                {
                    outerIndex = FindAttributeIndexByOid(attributes, attribute.Oid!, outerIndex);

                    if (outerIndex == -1)
                    {
                        break;
                    }

                    int innerIndex = FindAttributeValueIndexByEncodedData(attributes[outerIndex].AttrValues, attribute.RawData, out isOnlyValue);
                    if (innerIndex != -1)
                    {
                        return (outerIndex, innerIndex);
                    }
                }
            }

            isOnlyValue = false;
            return (-1, -1);
        }
    }
}
