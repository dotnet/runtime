// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

using Microsoft.Win32.SafeHandles;

using static Interop.Crypt32;
using System.Security.Cryptography.Asn1;

namespace Internal.Cryptography.Pal.Windows
{
    internal sealed partial class PkcsPalWindows : PkcsPal
    {
        public sealed unsafe override byte[] Encrypt(CmsRecipientCollection recipients, ContentInfo contentInfo, AlgorithmIdentifier contentEncryptionAlgorithm, X509Certificate2Collection originatorCerts, CryptographicAttributeObjectCollection unprotectedAttributes)
        {
            using (SafeCryptMsgHandle hCryptMsg = EncodeHelpers.CreateCryptMsgHandleToEncode(recipients, contentInfo.ContentType, contentEncryptionAlgorithm, originatorCerts, unprotectedAttributes))
            {
                byte[] encodedContent;
                if (contentInfo.ContentType.Value.Equals(Oids.Pkcs7Data, StringComparison.OrdinalIgnoreCase))
                {
                    unsafe
                    {
                        byte[] content = contentInfo.Content;
                        fixed (byte* pContent = content)
                        {
                            DATA_BLOB blob = new DATA_BLOB((IntPtr)pContent, (uint)(content.Length));
                            encodedContent = Interop.Crypt32.CryptEncodeObjectToByteArray(CryptDecodeObjectStructType.X509_OCTET_STRING, &blob);
                        }
                    }
                }
                else
                {
                    encodedContent = contentInfo.Content;

                    if (encodedContent.Length > 0)
                    {
                        // Windows will throw if it encounters indefinite length encoding.
                        // Let's reencode if that is the case
                        ReencodeIfUsingIndefiniteLengthEncodingOnOuterStructure(ref encodedContent);
                    }
                }

                if (encodedContent.Length > 0)
                {
                    // Pin to avoid copy during heap compaction
                    fixed (byte* pinnedContent = encodedContent)
                    {
                        try
                        {
                            if (!Interop.Crypt32.CryptMsgUpdate(hCryptMsg, encodedContent, encodedContent.Length, fFinal: true))
                                throw Marshal.GetLastWin32Error().ToCryptographicException();
                        }
                        finally
                        {
                            if (!object.ReferenceEquals(encodedContent, contentInfo.Content))
                            {
                                Array.Clear(encodedContent, 0, encodedContent.Length);
                            }
                        }
                    }
                }

                byte[] encodedMessage = hCryptMsg.GetMsgParamAsByteArray(CryptMsgParamType.CMSG_CONTENT_PARAM);
                return encodedMessage;
            }
        }

        private static void ReencodeIfUsingIndefiniteLengthEncodingOnOuterStructure(ref byte[] encodedContent)
        {
            AsnReader reader = new AsnReader(encodedContent, AsnEncodingRules.BER);
            Asn1Tag tag = reader.ReadTagAndLength(out int? contentsLength, out int _);

            if (contentsLength != null)
            {
                // definite length, do nothing
                return;
            }

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.BER))
            {
                // Tag doesn't matter here as we won't write it into the document
                writer.WriteOctetString(reader.PeekContentBytes().Span);
                encodedContent = writer.Encode();
            }
        }

        //
        // The methods in this class have some pretty terrifying contracts with each other regarding the lifetimes of the pointers they hand around so we'll encapsulate them in a nested class so that
        // only the top level method is accessible to everyone else.
        //
        private static class EncodeHelpers
        {
            public static SafeCryptMsgHandle CreateCryptMsgHandleToEncode(CmsRecipientCollection recipients, Oid innerContentType, AlgorithmIdentifier contentEncryptionAlgorithm, X509Certificate2Collection originatorCerts, CryptographicAttributeObjectCollection unprotectedAttributes)
            {
                using (HeapBlockRetainer hb = new HeapBlockRetainer())
                {
                    // Deep copy the CmsRecipients (and especially their underlying X509Certificate2 objects). This will prevent malicious callers from altering them or disposing them while we're performing 
                    // unsafe memory crawling inside them.
                    recipients = recipients.DeepCopy();

                    // We must keep all the certificates inside recipients alive until the call to CryptMsgOpenToEncode() finishes. The CMSG_ENVELOPED_ENCODE_INFO* structure we passed to it
                    // contains direct pointers to memory owned by the CERT_INFO memory block whose lifetime is that of the certificate. 
                    hb.KeepAlive(recipients);
                    unsafe
                    {
                        CMSG_ENVELOPED_ENCODE_INFO* pEnvelopedEncodeInfo = CreateCmsEnvelopedEncodeInfo(recipients, contentEncryptionAlgorithm, originatorCerts, unprotectedAttributes, hb);
                        SafeCryptMsgHandle hCryptMsg = Interop.Crypt32.CryptMsgOpenToEncode(MsgEncodingType.All, 0, CryptMsgType.CMSG_ENVELOPED, pEnvelopedEncodeInfo, innerContentType.Value, IntPtr.Zero);
                        if (hCryptMsg == null || hCryptMsg.IsInvalid)
                            throw Marshal.GetLastWin32Error().ToCryptographicException();

                        return hCryptMsg;
                    }
                }
            }

            //
            // This returns an allocated native memory block. Its lifetime (and that of any allocated subblocks it may point to) is that of "hb". 
            //
            private static unsafe CMSG_ENVELOPED_ENCODE_INFO* CreateCmsEnvelopedEncodeInfo(CmsRecipientCollection recipients, AlgorithmIdentifier contentEncryptionAlgorithm, X509Certificate2Collection originatorCerts, CryptographicAttributeObjectCollection unprotectedAttributes, HeapBlockRetainer hb)
            {
                CMSG_ENVELOPED_ENCODE_INFO* pEnvelopedEncodeInfo = (CMSG_ENVELOPED_ENCODE_INFO*)(hb.Alloc(sizeof(CMSG_ENVELOPED_ENCODE_INFO)));
                pEnvelopedEncodeInfo->cbSize = sizeof(CMSG_ENVELOPED_ENCODE_INFO);
                pEnvelopedEncodeInfo->hCryptProv = IntPtr.Zero;

                string algorithmOidValue = contentEncryptionAlgorithm.Oid.Value;
                pEnvelopedEncodeInfo->ContentEncryptionAlgorithm.pszObjId = hb.AllocAsciiString(algorithmOidValue);

                // Desktop compat: Though it seems like we could copy over the contents of contentEncryptionAlgorithm.Parameters, that property is for retrieving information from decoded Cms's only, and it 
                // massages the raw data so it wouldn't be usable here anyway. To hammer home that fact, the EncryptedCms constructor rather rudely forces contentEncryptionAlgorithm.Parameters to be the empty array.
                pEnvelopedEncodeInfo->ContentEncryptionAlgorithm.Parameters.cbData = 0;
                pEnvelopedEncodeInfo->ContentEncryptionAlgorithm.Parameters.pbData = IntPtr.Zero;

                pEnvelopedEncodeInfo->pvEncryptionAuxInfo = GenerateEncryptionAuxInfoIfNeeded(contentEncryptionAlgorithm, hb);

                int numRecipients = recipients.Count;
                pEnvelopedEncodeInfo->cRecipients = numRecipients;
                pEnvelopedEncodeInfo->rgpRecipients = IntPtr.Zero;

                CMSG_RECIPIENT_ENCODE_INFO* rgCmsRecipients = (CMSG_RECIPIENT_ENCODE_INFO*)(hb.Alloc(numRecipients, sizeof(CMSG_RECIPIENT_ENCODE_INFO)));
                for (int index = 0; index < numRecipients; index++)
                {
                    rgCmsRecipients[index] = EncodeRecipientInfo(recipients[index], contentEncryptionAlgorithm, hb);
                }
                pEnvelopedEncodeInfo->rgCmsRecipients = rgCmsRecipients;

                int numCertificates = originatorCerts.Count;
                pEnvelopedEncodeInfo->cCertEncoded = numCertificates;
                pEnvelopedEncodeInfo->rgCertEncoded = null;
                if (numCertificates != 0)
                {
                    DATA_BLOB* pCertEncoded = (DATA_BLOB*)(hb.Alloc(numCertificates, sizeof(DATA_BLOB)));
                    for (int i = 0; i < numCertificates; i++)
                    {
                        byte[] certEncoded = originatorCerts[i].Export(X509ContentType.Cert);
                        pCertEncoded[i].cbData = (uint)(certEncoded.Length);
                        pCertEncoded[i].pbData = hb.AllocBytes(certEncoded);
                    }
                    pEnvelopedEncodeInfo->rgCertEncoded = pCertEncoded;
                }

                pEnvelopedEncodeInfo->cCrlEncoded = 0;
                pEnvelopedEncodeInfo->rgCrlEncoded = null;

                pEnvelopedEncodeInfo->cAttrCertEncoded = 0;
                pEnvelopedEncodeInfo->rgAttrCertEncoded = null;

                int numUnprotectedAttributes = unprotectedAttributes.Count;
                pEnvelopedEncodeInfo->cUnprotectedAttr = numUnprotectedAttributes;
                pEnvelopedEncodeInfo->rgUnprotectedAttr = null;
                if (numUnprotectedAttributes != 0)
                {
                    CRYPT_ATTRIBUTE* pCryptAttribute = (CRYPT_ATTRIBUTE*)(hb.Alloc(numUnprotectedAttributes, sizeof(CRYPT_ATTRIBUTE)));
                    for (int i = 0; i < numUnprotectedAttributes; i++)
                    {
                        CryptographicAttributeObject attribute = unprotectedAttributes[i];
                        pCryptAttribute[i].pszObjId = hb.AllocAsciiString(attribute.Oid.Value);
                        AsnEncodedDataCollection values = attribute.Values;
                        int numValues = values.Count;
                        pCryptAttribute[i].cValue = numValues;
                        DATA_BLOB* pValues = (DATA_BLOB*)(hb.Alloc(numValues, sizeof(DATA_BLOB)));
                        for (int j = 0; j < numValues; j++)
                        {
                            byte[] rawData = values[j].RawData;
                            pValues[j].cbData = (uint)(rawData.Length);
                            pValues[j].pbData = hb.AllocBytes(rawData);
                        }
                        pCryptAttribute[i].rgValue = pValues;
                    }
                    pEnvelopedEncodeInfo->rgUnprotectedAttr = pCryptAttribute;
                }

                return pEnvelopedEncodeInfo;
            }

            //
            // This returns an allocated native memory block. Its lifetime (and that of any allocated subblocks it may point to) is that of "hb". 
            //
            private static CMSG_RECIPIENT_ENCODE_INFO EncodeRecipientInfo(CmsRecipient recipient, AlgorithmIdentifier contentEncryptionAlgorithm, HeapBlockRetainer hb)
            {
                CMsgCmsRecipientChoice recipientChoice;
                string keyAlgorithmOid = recipient.Certificate.GetKeyAlgorithm();
                AlgId algId = keyAlgorithmOid.ToAlgId();
                switch (algId)
                {
                    case AlgId.CALG_RSA_KEYX:
                        recipientChoice = CMsgCmsRecipientChoice.CMSG_KEY_TRANS_RECIPIENT;
                        break;

                    case AlgId.CALG_DH_SF:
                    case AlgId.CALG_DH_EPHEM:
                        recipientChoice = CMsgCmsRecipientChoice.CMSG_KEY_AGREE_RECIPIENT;
                        break;

                    default:
                        throw ErrorCode.CRYPT_E_UNKNOWN_ALGO.ToCryptographicException();
                }

                CMSG_RECIPIENT_ENCODE_INFO recipientEncodeInfo;
                recipientEncodeInfo.dwRecipientChoice = recipientChoice;

                unsafe
                {
                    switch (recipientChoice)
                    {
                        case CMsgCmsRecipientChoice.CMSG_KEY_TRANS_RECIPIENT:
                            recipientEncodeInfo.pCmsRecipientEncodeInfo = (IntPtr)EncodeKeyTransRecipientInfo(recipient, hb);
                            break;

                        case CMsgCmsRecipientChoice.CMSG_KEY_AGREE_RECIPIENT:
                            recipientEncodeInfo.pCmsRecipientEncodeInfo = (IntPtr)EncodeKeyAgreeRecipientInfo(recipient, contentEncryptionAlgorithm, hb);
                            break;

                        default:
                            throw ErrorCode.CRYPT_E_UNKNOWN_ALGO.ToCryptographicException();
                    }
                }
                return recipientEncodeInfo;
            }

            //
            // This returns an allocated native memory block. Its lifetime (and that of any allocated subblocks it may point to) is that of "hb". 
            //
            private static unsafe CMSG_KEY_TRANS_RECIPIENT_ENCODE_INFO* EncodeKeyTransRecipientInfo(CmsRecipient recipient, HeapBlockRetainer hb)
            {
                // "recipient" is a deep-cloned CmsRecipient object whose lifetime this class controls. Because of this, we can pull out the CERT_CONTEXT* and CERT_INFO* pointers 
                // and embed pointers to them in the memory block we return. Yes, this code is scary.
                //
                // (The use of SafeCertContextHandle here is about using a consistent pattern to get the CERT_CONTEXT (rather than the ugly (CERT_CONTEXT*)(recipient.Certificate.Handle) pattern.)
                // It's not about keeping the context alive.)
                using (SafeCertContextHandle hCertContext = recipient.Certificate.CreateCertContextHandle())
                {
                    CERT_CONTEXT* pCertContext = hCertContext.DangerousGetCertContext();
                    CERT_INFO* pCertInfo = pCertContext->pCertInfo;

                    CMSG_KEY_TRANS_RECIPIENT_ENCODE_INFO* pEncodeInfo = (CMSG_KEY_TRANS_RECIPIENT_ENCODE_INFO*)(hb.Alloc(sizeof(CMSG_KEY_TRANS_RECIPIENT_ENCODE_INFO)));

                    pEncodeInfo->cbSize = sizeof(CMSG_KEY_TRANS_RECIPIENT_ENCODE_INFO);

                    if (recipient.RSAEncryptionPadding is null)
                    {
                        CRYPT_ALGORITHM_IDENTIFIER algId = pCertInfo->SubjectPublicKeyInfo.Algorithm;
                        pEncodeInfo->KeyEncryptionAlgorithm = algId;
                    }
                    else if (recipient.RSAEncryptionPadding == RSAEncryptionPadding.Pkcs1)
                    {
                        pEncodeInfo->KeyEncryptionAlgorithm.pszObjId = hb.AllocAsciiString(Oids.Rsa);
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.cbData = (uint)s_rsaPkcsParameters.Length;
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.pbData = hb.AllocBytes(s_rsaPkcsParameters);
                    }
                    else if (recipient.RSAEncryptionPadding == RSAEncryptionPadding.OaepSHA1)
                    {
                        pEncodeInfo->KeyEncryptionAlgorithm.pszObjId = hb.AllocAsciiString(Oids.RsaOaep);
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.cbData = (uint)s_rsaOaepSha1Parameters.Length;
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.pbData = hb.AllocBytes(s_rsaOaepSha1Parameters);
                    }
                    else if (recipient.RSAEncryptionPadding == RSAEncryptionPadding.OaepSHA256)
                    {
                        pEncodeInfo->KeyEncryptionAlgorithm.pszObjId = hb.AllocAsciiString(Oids.RsaOaep);
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.cbData = (uint)s_rsaOaepSha256Parameters.Length;
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.pbData = hb.AllocBytes(s_rsaOaepSha256Parameters);
                    }
                    else if (recipient.RSAEncryptionPadding == RSAEncryptionPadding.OaepSHA384)
                    {
                        pEncodeInfo->KeyEncryptionAlgorithm.pszObjId = hb.AllocAsciiString(Oids.RsaOaep);
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.cbData = (uint)s_rsaOaepSha384Parameters.Length;
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.pbData = hb.AllocBytes(s_rsaOaepSha384Parameters);
                    }
                    else if (recipient.RSAEncryptionPadding == RSAEncryptionPadding.OaepSHA512)
                    {
                        pEncodeInfo->KeyEncryptionAlgorithm.pszObjId = hb.AllocAsciiString(Oids.RsaOaep);
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.cbData = (uint)s_rsaOaepSha512Parameters.Length;
                        pEncodeInfo->KeyEncryptionAlgorithm.Parameters.pbData = hb.AllocBytes(s_rsaOaepSha512Parameters);
                    }
                    else
                    {
                        throw ErrorCode.CRYPT_E_UNKNOWN_ALGO.ToCryptographicException();
                    }

                    pEncodeInfo->pvKeyEncryptionAuxInfo = IntPtr.Zero;
                    pEncodeInfo->hCryptProv = IntPtr.Zero;

                    pEncodeInfo->RecipientPublicKey = pCertInfo->SubjectPublicKeyInfo.PublicKey;

                    pEncodeInfo->RecipientId = EncodeRecipientId(recipient, hCertContext, pCertContext, pCertInfo, hb);

                    return pEncodeInfo;
                }
            }

            //
            // This returns an allocated native memory block. Its lifetime (and that of any allocated subblocks it may point to) is that of "hb". 
            //
            private static unsafe CMSG_KEY_AGREE_RECIPIENT_ENCODE_INFO* EncodeKeyAgreeRecipientInfo(CmsRecipient recipient, AlgorithmIdentifier contentEncryptionAlgorithm, HeapBlockRetainer hb)
            {
                // "recipient" is a deep-cloned CmsRecipient object whose lifetime this class controls. Because of this, we can pull out the CERT_CONTEXT* and CERT_INFO* pointers without
                // bringing in all the SafeCertContextHandle machinery, and embed pointers to them in the memory block we return. Yes, this code is scary.
                using (SafeCertContextHandle hCertContext = recipient.Certificate.CreateCertContextHandle())
                {
                    CERT_CONTEXT* pCertContext = hCertContext.DangerousGetCertContext();
                    CERT_INFO* pCertInfo = pCertContext->pCertInfo;

                    CMSG_KEY_AGREE_RECIPIENT_ENCODE_INFO* pEncodeInfo = (CMSG_KEY_AGREE_RECIPIENT_ENCODE_INFO*)(hb.Alloc(sizeof(CMSG_KEY_AGREE_RECIPIENT_ENCODE_INFO)));

                    pEncodeInfo->cbSize = sizeof(CMSG_KEY_AGREE_RECIPIENT_ENCODE_INFO);

                    pEncodeInfo->KeyEncryptionAlgorithm.pszObjId = hb.AllocAsciiString(Oids.Esdh);
                    pEncodeInfo->KeyEncryptionAlgorithm.Parameters.cbData = 0;
                    pEncodeInfo->KeyEncryptionAlgorithm.Parameters.pbData = IntPtr.Zero;

                    pEncodeInfo->pvKeyEncryptionAuxInfo = null;

                    string oidValue;
                    AlgId algId = contentEncryptionAlgorithm.Oid.Value.ToAlgId();
                    if (algId == AlgId.CALG_RC2)
                        oidValue = Oids.CmsRc2Wrap;
                    else
                        oidValue = Oids.Cms3DesWrap;

                    pEncodeInfo->KeyWrapAlgorithm.pszObjId = hb.AllocAsciiString(oidValue);
                    pEncodeInfo->KeyWrapAlgorithm.Parameters.cbData = 0;
                    pEncodeInfo->KeyWrapAlgorithm.Parameters.pbData = IntPtr.Zero;

                    pEncodeInfo->pvKeyWrapAuxInfo = GenerateEncryptionAuxInfoIfNeeded(contentEncryptionAlgorithm, hb);

                    pEncodeInfo->hCryptProv = IntPtr.Zero;
                    pEncodeInfo->dwKeySpec = 0;
                    pEncodeInfo->dwKeyChoice = CmsKeyAgreeKeyChoice.CMSG_KEY_AGREE_EPHEMERAL_KEY_CHOICE;
                    pEncodeInfo->pEphemeralAlgorithm = (CRYPT_ALGORITHM_IDENTIFIER*)(hb.Alloc(sizeof(CRYPT_ALGORITHM_IDENTIFIER)));
                    *(pEncodeInfo->pEphemeralAlgorithm) = pCertInfo->SubjectPublicKeyInfo.Algorithm;

                    pEncodeInfo->UserKeyingMaterial.cbData = 0;
                    pEncodeInfo->UserKeyingMaterial.pbData = IntPtr.Zero;

                    CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO* pEncryptedKey = (CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO*)(hb.Alloc(sizeof(CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO)));

                    pEncryptedKey->cbSize = sizeof(CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO);
                    pEncryptedKey->RecipientPublicKey = pCertInfo->SubjectPublicKeyInfo.PublicKey;

                    pEncryptedKey->RecipientId = EncodeRecipientId(recipient, hCertContext, pCertContext, pCertInfo, hb);
                    pEncryptedKey->Date = default(FILETIME);
                    pEncryptedKey->pOtherAttr = null;

                    CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO** ppEncryptedKey = (CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO**)(hb.Alloc(sizeof(CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO*)));
                    ppEncryptedKey[0] = pEncryptedKey;
                    pEncodeInfo->cRecipientEncryptedKeys = 1;
                    pEncodeInfo->rgpRecipientEncryptedKeys = ppEncryptedKey;

                    return pEncodeInfo;
                }
            }

            //
            // This returns an allocated native memory block. Its lifetime (and that of any allocated subblocks it may point to) is that of "hb". 
            //
            private static unsafe CERT_ID EncodeRecipientId(CmsRecipient recipient, SafeCertContextHandle hCertContext, CERT_CONTEXT* pCertContext, CERT_INFO* pCertInfo, HeapBlockRetainer hb)
            {
                CERT_ID recipientId = default(CERT_ID);
                SubjectIdentifierType type = recipient.RecipientIdentifierType;
                switch (type)
                {
                    case SubjectIdentifierType.IssuerAndSerialNumber:
                        {
                            recipientId.dwIdChoice = CertIdChoice.CERT_ID_ISSUER_SERIAL_NUMBER;
                            recipientId.u.IssuerSerialNumber.Issuer = pCertInfo->Issuer;
                            recipientId.u.IssuerSerialNumber.SerialNumber = pCertInfo->SerialNumber;
                            break;
                        }

                    case SubjectIdentifierType.SubjectKeyIdentifier:
                        {
                            byte[] ski = hCertContext.GetSubjectKeyIdentifer();
                            IntPtr pSki = hb.AllocBytes(ski);

                            recipientId.dwIdChoice = CertIdChoice.CERT_ID_KEY_IDENTIFIER;
                            recipientId.u.KeyId.cbData = (uint)(ski.Length);
                            recipientId.u.KeyId.pbData = pSki;
                            break;
                        }

                    default:
                        // The public contract for CmsRecipient guarantees that SubjectKeyIdentifier and IssuerAndSerialNumber are the only two possibilities.
                        Debug.Fail($"Unexpected SubjectIdentifierType: {type}");
                        throw new NotSupportedException(SR.Format(SR.Cryptography_Cms_Invalid_Subject_Identifier_Type, type));
                }

                return recipientId;
            }

            //
            // This returns an allocated native memory block. Its lifetime (and that of any allocated subblocks it may point to) is that of "hb". 
            //
            private static IntPtr GenerateEncryptionAuxInfoIfNeeded(AlgorithmIdentifier contentEncryptionAlgorithm, HeapBlockRetainer hb)
            {
                string algorithmOidValue = contentEncryptionAlgorithm.Oid.Value;
                AlgId algId = algorithmOidValue.ToAlgId();
                if (!(algId == AlgId.CALG_RC2 || algId == AlgId.CALG_RC4))
                    return IntPtr.Zero;

                unsafe
                {
                    CMSG_RC2_AUX_INFO* pRc2AuxInfo = (CMSG_RC2_AUX_INFO*)(hb.Alloc(sizeof(CMSG_RC2_AUX_INFO)));
                    pRc2AuxInfo->cbSize = sizeof(CMSG_RC2_AUX_INFO);
                    pRc2AuxInfo->dwBitLen = contentEncryptionAlgorithm.KeyLength;
                    if (pRc2AuxInfo->dwBitLen == 0)
                    {
                        // Desktop compat: If the caller didn't set the KeyLength property, set dwBitLength to the maxmium key length supported by RC2/RC4. The desktop queries CAPI for this but
                        // since that requires us to use a prohibited api (CryptAcquireContext), we'll just hardcode what CAPI returns for RC2 and RC4.
                        pRc2AuxInfo->dwBitLen = KeyLengths.DefaultKeyLengthForRc2AndRc4;
                    }
                    return (IntPtr)pRc2AuxInfo;
                }
            }
        }
    }
}
