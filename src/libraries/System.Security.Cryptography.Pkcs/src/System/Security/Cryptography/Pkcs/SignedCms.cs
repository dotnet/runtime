// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed partial class SignedCms
    {
        private SignedDataAsn _signedData;
        private bool _hasData;

        // A defensive copy of the relevant portions of the data to Decode
        private Memory<byte> _heldData;

        // Due to the way the underlying Windows CMS API behaves a copy of the content
        // bytes will be held separate once the content is "bound" (first signature or decode)
        private ReadOnlyMemory<byte>? _heldContent;

        // Similar to _heldContent, the Windows CMS API held this separate internally,
        // and thus we need to be reslilient against modification.
        private string _contentType;

        public int Version { get; private set; }
        public ContentInfo ContentInfo { get; private set; }
        public bool Detached { get; private set; }

        public SignedCms(SubjectIdentifierType signerIdentifierType, ContentInfo contentInfo, bool detached)
        {
            if (contentInfo == null)
                throw new ArgumentNullException(nameof(contentInfo));
            if (contentInfo.Content == null)
                throw new ArgumentNullException("contentInfo.Content");

            // signerIdentifierType is ignored.
            // In .NET Framework it is used for the signer type of a prompt-for-certificate signer.
            // In .NET Core we don't support prompting.
            //
            // .NET Framework turned any unknown value into IssuerAndSerialNumber, so no exceptions
            // are required, either.

            ContentInfo = contentInfo;
            Detached = detached;
            Version = 0;
        }

        public X509Certificate2Collection Certificates
        {
            get
            {
                var coll = new X509Certificate2Collection();

                if (!_hasData)
                {
                    return coll;
                }

                CertificateChoiceAsn[] certChoices = _signedData.CertificateSet;

                if (certChoices == null)
                {
                    return coll;
                }

                foreach (CertificateChoiceAsn choice in certChoices)
                {
                    coll.Add(new X509Certificate2(choice.Certificate.Value.ToArray()));
                }

                return coll;
            }
        }

        public SignerInfoCollection SignerInfos
        {
            get
            {
                if (!_hasData)
                {
                    return new SignerInfoCollection();
                }

                return new SignerInfoCollection(_signedData.SignerInfos, this);
            }
        }

        public byte[] Encode()
        {
            if (!_hasData)
            {
                throw new InvalidOperationException(SR.Cryptography_Cms_MessageNotSigned);
            }

            // Write as DER, so everyone can read it.
            AsnWriter writer = AsnSerializer.Serialize(_signedData, AsnEncodingRules.DER);
            byte[] signedData = writer.Encode();

            ContentInfoAsn contentInfo = new ContentInfoAsn
            {
                Content = signedData,
                ContentType = Oids.Pkcs7Signed,
            };

            // Write as DER, so everyone can read it.
            writer = AsnSerializer.Serialize(contentInfo, AsnEncodingRules.DER);
            return writer.Encode();
        }

        public void Decode(byte[] encodedMessage)
        {
            if (encodedMessage == null)
                throw new ArgumentNullException(nameof(encodedMessage));

            // Windows (and thus NetFx) reads the leading data and ignores extra.
            // The deserializer will complain if too much data is given, so use the reader
            // to ask how much we want to deserialize.
            AsnReader reader = new AsnReader(encodedMessage, AsnEncodingRules.BER);
            ReadOnlyMemory<byte> cmsSegment = reader.GetEncodedValue();

            ContentInfoAsn contentInfo = AsnSerializer.Deserialize<ContentInfoAsn>(cmsSegment, AsnEncodingRules.BER);

            if (contentInfo.ContentType != Oids.Pkcs7Signed)
            {
                throw new CryptographicException(SR.Cryptography_Cms_InvalidMessageType);
            }

            // Hold a copy of the SignedData memory so we are protected against memory reuse by the caller.
            _heldData = contentInfo.Content.ToArray();
            _signedData = AsnSerializer.Deserialize<SignedDataAsn>(_heldData, AsnEncodingRules.BER);
            _contentType = _signedData.EncapContentInfo.ContentType;

            if (!Detached)
            {
                ReadOnlyMemory<byte>? content = _signedData.EncapContentInfo.Content;

                // This is in _heldData, so we don't need a defensive copy.
                _heldContent = content ?? ReadOnlyMemory<byte>.Empty;

                // The ContentInfo object/property DOES need a defensive copy, because
                // a) it is mutable by the user, and
                // b) it is no longer authoritative
                //
                // (and c: it takes a byte[] and we have a ReadOnlyMemory<byte>)
                ContentInfo = new ContentInfo(new Oid(_contentType), _heldContent.Value.ToArray());
            }
            else
            {
                // Hold a defensive copy of the content bytes, (Windows/NetFx compat)
                _heldContent = ContentInfo.Content.CloneByteArray();
            }

            Version = _signedData.Version;
            _hasData = true;
        }

        public void ComputeSignature()
        {
            throw new PlatformNotSupportedException(SR.Cryptography_Cms_NoSignerCert);
        }

        public void ComputeSignature(CmsSigner signer) => ComputeSignature(signer, true);

        public void ComputeSignature(CmsSigner signer, bool silent)
        {
            if (signer == null)
            {
                throw new ArgumentNullException(nameof(signer));
            }

            // While it shouldn't be possible to change the length of ContentInfo.Content
            // after it's built, use the property at this stage, then use the saved value
            // (if applicable) after this point.
            if (ContentInfo.Content.Length == 0)
            {
                throw new CryptographicException(SR.Cryptography_Cms_Sign_Empty_Content);
            }
            
            // If we had content already, use that now.
            // (The second signer doesn't inherit edits to signedCms.ContentInfo.Content)
            ReadOnlyMemory<byte> content = _heldContent ?? ContentInfo.Content;
            string contentType = _contentType ?? ContentInfo.ContentType.Value;

            X509Certificate2Collection chainCerts;
            SignerInfoAsn newSigner = signer.Sign(content, contentType, silent, out chainCerts);
            bool firstSigner = false;

            if (!_hasData)
            {
                firstSigner = true;

                _signedData = new SignedDataAsn
                {
                    DigestAlgorithms = Array.Empty<AlgorithmIdentifierAsn>(),
                    SignerInfos = Array.Empty<SignerInfoAsn>(),
                    EncapContentInfo = new EncapsulatedContentInfoAsn { ContentType = contentType },
                };

                // Since we're going to call Decode before this method exits we don't need to save
                // the copy of _heldContent or _contentType here if we're attached.
                if (!Detached)
                {
                    _signedData.EncapContentInfo.Content = content;
                }

                _hasData = true;
            }

            int newIdx = _signedData.SignerInfos.Length;
            Array.Resize(ref _signedData.SignerInfos, newIdx + 1);
            _signedData.SignerInfos[newIdx] = newSigner;
            UpdateCertificatesFromAddition(chainCerts);
            ConsiderDigestAddition(newSigner.DigestAlgorithm);
            UpdateMetadata();

            if (firstSigner)
            {
                Reencode();

                Debug.Assert(_heldContent != null);
                Debug.Assert(_contentType == contentType);
            }
        }

        public void RemoveSignature(int index)
        {
            if (!_hasData)
            {
                throw new InvalidOperationException(SR.Cryptography_Cms_MessageNotSigned);
            }

            if (index < 0 || index >= _signedData.SignerInfos.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            }

            AlgorithmIdentifierAsn signerAlgorithm = _signedData.SignerInfos[index].DigestAlgorithm;
            Helpers.RemoveAt(ref _signedData.SignerInfos, index);

            ConsiderDigestRemoval(signerAlgorithm);
            UpdateMetadata();
        }

        public void RemoveSignature(SignerInfo signerInfo)
        {
            if (signerInfo == null)
                throw new ArgumentNullException(nameof(signerInfo));

            int idx = SignerInfos.FindIndexForSigner(signerInfo);

            if (idx < 0)
            {
                throw new CryptographicException(SR.Cryptography_Cms_SignerNotFound);
            }

            RemoveSignature(idx);
        }

        internal ReadOnlySpan<byte> GetContentSpan() => _heldContent.Value.Span;

        internal void Reencode()
        {
            // When NetFx re-encodes it just resets the CMS handle, the ContentInfo property
            // does not get changed.
            // See ReopenToDecode
            ContentInfo save = ContentInfo;

            try
            {
                byte[] encoded = Encode();

                if (Detached)
                {
                    // At this point the _heldContent becomes whatever ContentInfo says it should be.
                    _heldContent = null;
                }

                Decode(encoded);
                Debug.Assert(_heldContent != null);
            }
            finally
            {
                ContentInfo = save;
            }
        }

        private void UpdateMetadata()
        {
            // Version 5: any certificate of type Other or CRL of type Other. We don't support this.
            // Version 4: any certificates are V2 attribute certificates.  We don't support this.
            // Version 3a: any certificates are V1 attribute certificates. We don't support this.
            // Version 3b: any signerInfos are v3
            // Version 3c: eContentType != data
            // Version 2: does not exist for signed-data
            // Version 1: default

            // The versions 3 are OR conditions, so we need to check the content type and the signerinfos.
            int version = 1;

            if ((_contentType ?? ContentInfo.ContentType.Value) != Oids.Pkcs7Data)
            {
                version = 3;
            }
            else if (_signedData.SignerInfos.Any(si => si.Version == 3))
            {
                version = 3;
            }

            Version = version;
            _signedData.Version = version;
        }

        private void ConsiderDigestAddition(AlgorithmIdentifierAsn candidate)
        {
            int curLength = _signedData.DigestAlgorithms.Length;

            for (int i = 0; i < curLength; i++)
            {
                ref AlgorithmIdentifierAsn alg = ref _signedData.DigestAlgorithms[i];

                if (candidate.Equals(ref alg))
                {
                    return;
                }
            }

            Array.Resize(ref _signedData.DigestAlgorithms, curLength + 1);
            _signedData.DigestAlgorithms[curLength] = candidate;
        }

        private void ConsiderDigestRemoval(AlgorithmIdentifierAsn candidate)
        {
            bool remove = true;

            for (int i = 0; i < _signedData.SignerInfos.Length; i++)
            {
                ref AlgorithmIdentifierAsn signerAlg = ref _signedData.SignerInfos[i].DigestAlgorithm;

                if (candidate.Equals(ref signerAlg))
                {
                    remove = false;
                    break;
                }
            }

            if (!remove)
            {
                return;
            }

            for (int i = 0; i < _signedData.DigestAlgorithms.Length; i++)
            {
                ref AlgorithmIdentifierAsn alg = ref _signedData.DigestAlgorithms[i];

                if (candidate.Equals(ref alg))
                {
                    Helpers.RemoveAt(ref _signedData.DigestAlgorithms, i);
                    break;
                }
            }
        }

        internal void UpdateCertificatesFromAddition(X509Certificate2Collection newCerts)
        {
            if (newCerts.Count == 0)
            {
                return;
            }

            int existingLength = _signedData.CertificateSet?.Length ?? 0;

            if (existingLength > 0 || newCerts.Count > 1)
            {
                var certs = new HashSet<X509Certificate2>(Certificates.OfType<X509Certificate2>());

                for (int i = 0; i < newCerts.Count; i++)
                {
                    X509Certificate2 candidate = newCerts[i];

                    if (!certs.Add(candidate))
                    {
                        newCerts.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (newCerts.Count == 0)
            {
                return;
            }

            if (_signedData.CertificateSet == null)
            {
                _signedData.CertificateSet = new CertificateChoiceAsn[newCerts.Count];
            }
            else
            {
                Array.Resize(ref _signedData.CertificateSet, existingLength + newCerts.Count);
            }

            for (int i = existingLength; i < _signedData.CertificateSet.Length; i++)
            {
                _signedData.CertificateSet[i] = new CertificateChoiceAsn
                {
                    Certificate = newCerts[i - existingLength].RawData
                };
            }
        }

        public void CheckSignature(bool verifySignatureOnly) =>
            CheckSignature(new X509Certificate2Collection(), verifySignatureOnly);

        public void CheckSignature(X509Certificate2Collection extraStore, bool verifySignatureOnly)
        {
            if (!_hasData)
                throw new InvalidOperationException(SR.Cryptography_Cms_MessageNotSigned);
            if (extraStore == null)
                throw new ArgumentNullException(nameof(extraStore));

            CheckSignatures(SignerInfos, extraStore, verifySignatureOnly);
        }

        private static void CheckSignatures(
            SignerInfoCollection signers,
            X509Certificate2Collection extraStore,
            bool verifySignatureOnly)
        {
            Debug.Assert(signers != null);

            if (signers.Count < 1)
            {
                throw new CryptographicException(SR.Cryptography_Cms_NoSignerAtIndex);
            }

            foreach (SignerInfo signer in signers)
            {
                signer.CheckSignature(extraStore, verifySignatureOnly);

                SignerInfoCollection counterSigners = signer.CounterSignerInfos;

                if (counterSigners.Count > 0)
                {
                    CheckSignatures(counterSigners, extraStore, verifySignatureOnly);
                }
            }
        }

        public void CheckHash()
        {
            if (!_hasData)
                throw new InvalidOperationException(SR.Cryptography_Cms_MessageNotSigned);

            SignerInfoCollection signers = SignerInfos;
            Debug.Assert(signers != null);

            if (signers.Count < 1)
            {
                throw new CryptographicException(SR.Cryptography_Cms_NoSignerAtIndex);
            }

            foreach (SignerInfo signer in signers)
            {
                if (signer.SignerIdentifier.Type == SubjectIdentifierType.NoSignature)
                {
                    signer.CheckHash();
                }
            }
        }

        internal ref SignedDataAsn GetRawData()
        {
            return ref _signedData;
        }
    }
}
