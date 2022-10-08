// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    public sealed partial class SignedCms
    {
        private SignedDataAsn _signedData;
        private bool _hasData;
        private readonly SubjectIdentifierType _signerIdentifierType;

        // A defensive copy of the relevant portions of the data to Decode
        private Memory<byte> _heldData;

        // Due to the way the underlying Windows CMS API behaves a copy of the content
        // bytes will be held separate once the content is "bound" (first signature or decode)
        private ReadOnlyMemory<byte>? _heldContent;

        // During decode, if the PKCS#7 fallback for a missing OCTET STRING is present, this
        // becomes true and GetHashableContentSpan behaves differently.
        // See https://tools.ietf.org/html/rfc5652#section-5.2.1
        private bool _hasPkcs7Content;

        // Similar to _heldContent, the Windows CMS API held this separate internally,
        // and thus we need to be reslilient against modification.
        private string? _contentType;

        public int Version { get; private set; }
        public ContentInfo ContentInfo { get; private set; }
        public bool Detached { get; private set; }

        public SignedCms(SubjectIdentifierType signerIdentifierType, ContentInfo contentInfo, bool detached)
        {
            if (contentInfo is null)
            {
                throw new ArgumentNullException(nameof(contentInfo));
            }

            if (contentInfo.Content == null)
                throw new ArgumentException(SR.Format(SR.Arg_EmptyOrNullString_Named, "contentInfo.Content"), nameof(contentInfo));

            // Normalize the subject identifier type the same way as .NET Framework.
            // This value is only used in the zero-argument ComputeSignature overload,
            // where it controls whether it succeeds (NoSignature) or throws (anything else),
            // but in case it ever applies to anything else, make sure we're storing it
            // faithfully.
            switch (signerIdentifierType)
            {
                case SubjectIdentifierType.NoSignature:
                case SubjectIdentifierType.IssuerAndSerialNumber:
                case SubjectIdentifierType.SubjectKeyIdentifier:
                    _signerIdentifierType = signerIdentifierType;
                    break;
                default:
                    _signerIdentifierType = SubjectIdentifierType.IssuerAndSerialNumber;
                    break;
            }

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

                CertificateChoiceAsn[]? certChoices = _signedData.CertificateSet;

                if (certChoices == null)
                {
                    return coll;
                }

                foreach (CertificateChoiceAsn choice in certChoices)
                {
                    if (choice.Certificate.HasValue)
                    {
                        coll.Add(new X509Certificate2(choice.Certificate.Value
#if NETCOREAPP
                            .Span
#else
                            .ToArray()
#endif
                            ));
                    }
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

            try
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                _signedData.Encode(writer);
                return PkcsHelpers.EncodeContentInfo(writer.Encode(), Oids.Pkcs7Signed);
            }
            catch (CryptographicException) when (!Detached)
            {
                // If we can't write the contents back out then the most likely culprit is an
                // indefinite length encoding in the content field.  To preserve as much input data
                // as possible while still maintaining our expectations of sorting any SET OF values,
                // do the following:
                // * Write the DER normalized version of the SignedData in detached mode.
                // * BER-decode that structure
                // * Copy the content field over
                // * BER-write the modified structure.

                SignedDataAsn copy = _signedData;
                copy.EncapContentInfo.Content = null;
                Debug.Assert(_signedData.EncapContentInfo.Content != null);

                AsnWriter detachedWriter = new AsnWriter(AsnEncodingRules.DER);
                copy.Encode(detachedWriter);
                copy = SignedDataAsn.Decode(detachedWriter.Encode(), AsnEncodingRules.BER);

                copy.EncapContentInfo.Content = _signedData.EncapContentInfo.Content;

                AsnWriter attachedWriter = new AsnWriter(AsnEncodingRules.BER);
                copy.Encode(attachedWriter);
                return PkcsHelpers.EncodeContentInfo(attachedWriter.Encode(), Oids.Pkcs7Signed);
            }
        }

        public void Decode(byte[] encodedMessage)
        {
            if (encodedMessage is null)
            {
                throw new ArgumentNullException(nameof(encodedMessage));
            }

            Decode(new ReadOnlySpan<byte>(encodedMessage));
        }

        public void Decode(ReadOnlySpan<byte> encodedMessage)
        {
            // Hold a copy of the SignedData memory so we are protected against memory reuse by the caller.
            _heldData = CopyContent(encodedMessage);
            _signedData = SignedDataAsn.Decode(_heldData, AsnEncodingRules.BER);
            _contentType = _signedData.EncapContentInfo.ContentType;
            _hasPkcs7Content = false;

            if (!Detached)
            {
                ReadOnlyMemory<byte>? content = _signedData.EncapContentInfo.Content;
                ReadOnlyMemory<byte> contentValue;

                if (content.HasValue)
                {
                    contentValue = GetContent(content.Value, _contentType);
                    // If no OCTET STRING was stripped off, we have PKCS7 interop concerns.
                    _hasPkcs7Content = content.Value.Length == contentValue.Length;
                }
                else
                {
                    contentValue = ReadOnlyMemory<byte>.Empty;
                }

                // This is in _heldData, so we don't need a defensive copy.
                _heldContent = contentValue;

                // The ContentInfo object/property DOES need a defensive copy, because
                // a) it is mutable by the user, and
                // b) it is no longer authoritative
                //
                // (and c: it takes a byte[] and we have a ReadOnlyMemory<byte>)
                ContentInfo = new ContentInfo(new Oid(_contentType), contentValue.ToArray());
            }
            else
            {
                // Hold a defensive copy of the content bytes, (Windows/NetFx compat)
                _heldContent = ContentInfo.Content.CloneByteArray();
            }

            Version = _signedData.Version;
            _hasData = true;

            static byte[] CopyContent(ReadOnlySpan<byte> encodedMessage)
            {
                unsafe
                {
                    fixed (byte* pin = encodedMessage)
                    {
                        using (var manager = new PointerMemoryManager<byte>(pin, encodedMessage.Length))
                        {
                            AsnValueReader reader = new AsnValueReader(encodedMessage, AsnEncodingRules.BER);

                            // Windows (and thus NetFx) reads the leading data and ignores extra.
                            // So use the Decode overload which doesn't throw on extra data.
                            ContentInfoAsn.Decode(
                                ref reader,
                                manager.Memory,
                                out ContentInfoAsn contentInfo);

                            if (contentInfo.ContentType != Oids.Pkcs7Signed)
                            {
                                throw new CryptographicException(SR.Cryptography_Cms_InvalidMessageType);
                            }

                            return contentInfo.Content.ToArray();
                        }
                    }
                }
            }
        }

        internal static ReadOnlyMemory<byte> GetContent(
            ReadOnlyMemory<byte> wrappedContent,
            string contentType)
        {
            // Read the input.
            //
            // PKCS7's id-data is written in both PKCS#7 and CMS as an OCTET STRING wrapping
            // the arbitrary bytes, so the OCTET STRING must always be present.
            //
            // For other types, CMS says to always write an OCTET STRING, and to put the properly
            // encoded data within it.
            // PKCS#7 originally ommitted the OCTET STRING wrapper for this model, so this is the
            // dynamic adapter.
            //
            // See https://tools.ietf.org/html/rfc5652#section-5.2.1
            byte[]? rented = null;
            int bytesWritten = 0;
            try
            {
                AsnReader reader = new AsnReader(wrappedContent, AsnEncodingRules.BER);

                if (reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> inner))
                {
                    return inner;
                }

                rented = CryptoPool.Rent(wrappedContent.Length);

                if (!reader.TryReadOctetString(rented, out bytesWritten))
                {
                    Debug.Fail($"TryCopyOctetStringBytes failed with an array larger than the encoded value");
                    throw new CryptographicException();
                }

                return rented.AsSpan(0, bytesWritten).ToArray();
            }
            catch (Exception) when (contentType != Oids.Pkcs7Data)
            {
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            finally
            {
                if (rented != null)
                {
                    CryptoPool.Return(rented, bytesWritten);
                }
            }

            // PKCS#7 encoding for something other than id-data.
            Debug.Assert(contentType != Oids.Pkcs7Data);
            return wrappedContent;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ComputeSignature() => ComputeSignature(new CmsSigner(_signerIdentifierType), true);

        public void ComputeSignature(CmsSigner signer) => ComputeSignature(signer, true);

        public void ComputeSignature(CmsSigner signer, bool silent)
        {
            if (signer is null)
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

            if (_hasData && signer.SignerIdentifierType == SubjectIdentifierType.NoSignature)
            {
                // Even if all signers have been removed, throw if doing a NoSignature signature
                // on a loaded (from file, or from first signature) document.
                //
                // This matches the .NET Framework behavior.
                throw new CryptographicException(SR.Cryptography_Cms_Sign_No_Signature_First_Signer);
            }

            if (signer.Certificate == null && signer.SignerIdentifierType != SubjectIdentifierType.NoSignature)
            {
                if (silent)
                {
                    // .NET Framework compatibility, silent disallows prompting, so throws InvalidOperationException
                    // in this state.
                    //
                    // The message is different than on NetFX, because the resource string was for
                    // enveloped CMS recipients (and the other site which would use that resource
                    // is unreachable code due to CmsRecipient's ctor guarding against null certificates)
                    throw new InvalidOperationException(SR.Cryptography_Cms_NoSignerCertSilent);
                }

                // Otherwise, PNSE. .NET Core doesn't support launching the cert picker UI.
                throw new PlatformNotSupportedException(SR.Cryptography_Cms_NoSignerCert);
            }

            // If we had content already, use that now.
            // (The second signer doesn't inherit edits to signedCms.ContentInfo.Content)
            ReadOnlyMemory<byte> content = _heldContent ?? ContentInfo.Content;
            string contentType = _contentType ?? ContentInfo.ContentType.Value ?? Oids.Pkcs7Data;

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
                    AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                    writer.WriteOctetString(content.Span);

                    _signedData.EncapContentInfo.Content = writer.Encode();
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
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
            }

            AlgorithmIdentifierAsn signerAlgorithm = _signedData.SignerInfos[index].DigestAlgorithm;
            PkcsHelpers.RemoveAt(ref _signedData.SignerInfos, index);

            ConsiderDigestRemoval(signerAlgorithm);
            UpdateMetadata();
        }

        public void RemoveSignature(SignerInfo signerInfo)
        {
            if (signerInfo is null)
            {
                throw new ArgumentNullException(nameof(signerInfo));
            }

            int idx = SignerInfos.FindIndexForSigner(signerInfo);

            if (idx < 0)
            {
                throw new CryptographicException(SR.Cryptography_Cms_SignerNotFound);
            }

            RemoveSignature(idx);
        }

        internal ReadOnlySpan<byte> GetHashableContentSpan()
        {
            Debug.Assert(_heldContent.HasValue);
            ReadOnlyMemory<byte> content = _heldContent.Value;
            ReadOnlySpan<byte> contentSpan = content.Span;

            if (!_hasPkcs7Content)
            {
                return contentSpan;
            }

            // In PKCS#7 compat, only return the contents within the outermost tag.
            // See https://tools.ietf.org/html/rfc5652#section-5.2.1
            try
            {
                AsnDecoder.ReadEncodedValue(
                    contentSpan,
                    AsnEncodingRules.BER,
                    out int contentOffset,
                    out int contentLength,
                    out _);

                return contentSpan.Slice(contentOffset, contentLength);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal void Reencode()
        {
            // When .NET Framework re-encodes it just resets the CMS handle, the ContentInfo property
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
                    PkcsHelpers.RemoveAt(ref _signedData.DigestAlgorithms, i);
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

        public void AddCertificate(X509Certificate2 certificate)
        {
            int existingLength = _signedData.CertificateSet?.Length ?? 0;

            byte[] rawData = certificate.RawData;

            if (existingLength > 0)
            {
                foreach (CertificateChoiceAsn cert in _signedData.CertificateSet!)
                {
                    if (cert.Certificate!.Value.Span.SequenceEqual(rawData))
                    {
                        throw new CryptographicException(SR.Cryptography_Cms_CertificateAlreadyInCollection);
                    }
                }
            }

            if (_signedData.CertificateSet == null)
            {
                _signedData.CertificateSet = new CertificateChoiceAsn[1];
            }
            else
            {
                Array.Resize(ref _signedData.CertificateSet, existingLength + 1);
            }

            _signedData.CertificateSet[existingLength] = new CertificateChoiceAsn
            {
                Certificate = rawData
            };

            Reencode();
        }

        public void RemoveCertificate(X509Certificate2 certificate)
        {
            int existingLength = _signedData.CertificateSet?.Length ?? 0;

            if (existingLength != 0)
            {
                int idx = 0;
                byte[] rawData = certificate.RawData;

                foreach (CertificateChoiceAsn cert in _signedData.CertificateSet!)
                {
                    if (cert.Certificate!.Value.Span.SequenceEqual(rawData))
                    {
                        PkcsHelpers.RemoveAt(ref _signedData.CertificateSet, idx);
                        Reencode();
                        return;
                    }

                    idx++;
                }
            }

            throw new CryptographicException(SR.Cryptography_Cms_NoCertificateFound);
        }
    }
}
