// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        public override unsafe byte[] Encrypt(
            CmsRecipientCollection recipients,
            ContentInfo contentInfo,
            AlgorithmIdentifier contentEncryptionAlgorithm,
            X509Certificate2Collection originatorCerts,
            CryptographicAttributeObjectCollection unprotectedAttributes)
        {
            byte[] encryptedContent = EncryptContent(
                contentInfo,
                contentEncryptionAlgorithm,
                out byte[] cek,
                out byte[] parameterBytes);

            // Pin the CEK to prevent it from getting copied during heap compaction.
            fixed (byte* pinnedCek = cek)
            {
                try
                {
                    return Encrypt(
                        recipients,
                        contentInfo,
                        contentEncryptionAlgorithm,
                        originatorCerts,
                        unprotectedAttributes,
                        encryptedContent,
                        cek,
                        parameterBytes);
                }
                finally
                {
                    Array.Clear(cek, 0, cek.Length);
                }
            }
        }

        private byte[] Encrypt(
            CmsRecipientCollection recipients,
            ContentInfo contentInfo,
            AlgorithmIdentifier contentEncryptionAlgorithm,
            X509Certificate2Collection originatorCerts,
            CryptographicAttributeObjectCollection unprotectedAttributes,
            byte[] encryptedContent,
            byte[] cek,
            byte[] parameterBytes)
        {
            EnvelopedDataAsn envelopedData = new EnvelopedDataAsn
            {
                EncryptedContentInfo =
                {
                    ContentType = contentInfo.ContentType.Value,

                    ContentEncryptionAlgorithm =
                    {
                        Algorithm = contentEncryptionAlgorithm.Oid,
                        Parameters = parameterBytes,
                    },

                    EncryptedContent = encryptedContent,
                },
            };

            if (unprotectedAttributes != null && unprotectedAttributes.Count > 0)
            {
                List<AttributeAsn> attrList = CmsSigner.BuildAttributes(unprotectedAttributes);

                envelopedData.UnprotectedAttributes = PkcsHelpers.NormalizeAttributeSet(attrList.ToArray());
            }

            if (originatorCerts != null && originatorCerts.Count > 0)
            {
                CertificateChoiceAsn[] certs = new CertificateChoiceAsn[originatorCerts.Count];

                for (int i = 0; i < originatorCerts.Count; i++)
                {
                    certs[i].Certificate = originatorCerts[i].RawData;
                }

                envelopedData.OriginatorInfo = new OriginatorInfoAsn
                {
                    CertificateSet = certs,
                };
            }

            envelopedData.RecipientInfos = new RecipientInfoAsn[recipients.Count];

            bool allRecipientsVersion0 = true;

            for (var i = 0; i < recipients.Count; i++)
            {
                CmsRecipient recipient = recipients[i];
                bool v0Recipient;

                switch (recipient.Certificate.GetKeyAlgorithm())
                {
                    case Oids.Rsa:
                        envelopedData.RecipientInfos[i].Ktri = MakeKtri(cek, recipient, out v0Recipient);
                        break;
                    default:
                        throw new CryptographicException(
                            SR.Cryptography_Cms_UnknownAlgorithm,
                            recipient.Certificate.GetKeyAlgorithm());
                }

                allRecipientsVersion0 = allRecipientsVersion0 && v0Recipient;
            }

            // https://tools.ietf.org/html/rfc5652#section-6.1
            //
            // v4 (RFC 3852):
            //   * OriginatorInfo contains certificates with type other (not supported)
            //   * OriginatorInfo contains crls with type other (not supported)
            // v3 (RFC 3369):
            //   * OriginatorInfo contains v2 attribute certificates (not supported)
            //   * Any PWRI (password) recipients are present (not supported)
            //   * Any ORI (other) recipients are present (not supported)
            // v2 (RFC 2630):
            //   * OriginatorInfo is present
            //   * Any RecipientInfo has a non-zero version number
            //   * UnprotectedAttrs is present
            // v1 (not defined for EnvelopedData)
            // v0 (RFC 2315):
            //   * Anything not already matched

            if (envelopedData.OriginatorInfo != null ||
                !allRecipientsVersion0 ||
                envelopedData.UnprotectedAttributes != null)
            {
                envelopedData.Version = 2;
            }

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                envelopedData.Encode(writer);
                return PkcsHelpers.EncodeContentInfo(writer.Encode(), Oids.Pkcs7Enveloped);
            }
        }

        private byte[] EncryptContent(
            ContentInfo contentInfo,
            AlgorithmIdentifier contentEncryptionAlgorithm,
            out byte[] cek,
            out byte[] parameterBytes)
        {
            using (SymmetricAlgorithm alg = OpenAlgorithm(contentEncryptionAlgorithm))
            using (ICryptoTransform encryptor = alg.CreateEncryptor())
            {
                cek = alg.Key;

                if (alg is RC2)
                {
                    Rc2CbcParameters rc2Params = new Rc2CbcParameters(alg.IV, alg.KeySize);

                    using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
                    {
                        rc2Params.Encode(writer);
                        parameterBytes = writer.Encode();
                    }
                }
                else
                {
                    parameterBytes = EncodeOctetString(alg.IV);
                }

                byte[] toEncrypt = contentInfo.Content;

                if (contentInfo.ContentType.Value == Oids.Pkcs7Data)
                {
                    return encryptor.OneShot(toEncrypt);
                }
                else
                {
                    if (contentInfo.Content.Length == 0)
                    {
                        return encryptor.OneShot(contentInfo.Content);
                    }
                    else
                    {
                        AsnReader reader = new AsnReader(contentInfo.Content, AsnEncodingRules.BER);
                        return encryptor.OneShot(reader.PeekContentBytes().ToArray());
                    }
                }
            }
        }
    }
}
