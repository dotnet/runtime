// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal : PkcsPal
    {
        public override DecryptorPal Decode(
            byte[] encodedMessage,
            out int version,
            out ContentInfo contentInfo,
            out AlgorithmIdentifier contentEncryptionAlgorithm,
            out X509Certificate2Collection originatorCerts,
            out CryptographicAttributeObjectCollection unprotectedAttributes)
        {
            // Read using BER because the CMS specification says the encoding is BER.
            AsnReader reader = new AsnReader(encodedMessage, AsnEncodingRules.BER);

            ContentInfoAsn.Decode(
                reader,
                out ContentInfoAsn parsedContentInfo);

            if (parsedContentInfo.ContentType != Oids.Pkcs7Enveloped)
            {
                throw new CryptographicException(SR.Cryptography_Cms_InvalidMessageType);
            }

            byte[] copy = parsedContentInfo.Content.ToArray();

            EnvelopedDataAsn data = EnvelopedDataAsn.Decode(copy, AsnEncodingRules.BER);

            version = data.Version;

            contentInfo = new ContentInfo(
                new Oid(data.EncryptedContentInfo.ContentType),
                data.EncryptedContentInfo.EncryptedContent?.ToArray() ?? Array.Empty<byte>());

            contentEncryptionAlgorithm =
                data.EncryptedContentInfo.ContentEncryptionAlgorithm.ToPresentationObject();

            originatorCerts = new X509Certificate2Collection();

            if (data.OriginatorInfo.HasValue && data.OriginatorInfo.Value.CertificateSet != null)
            {
                foreach (CertificateChoiceAsn certChoice in data.OriginatorInfo.Value.CertificateSet)
                {
                    if (certChoice.Certificate != null)
                    {
                        originatorCerts.Add(new X509Certificate2(certChoice.Certificate.Value.ToArray()));
                    }
                }
            }

            unprotectedAttributes = SignerInfo.MakeAttributeCollection(data.UnprotectedAttributes);

            var recipientInfos = new List<RecipientInfo>();

            foreach (RecipientInfoAsn recipientInfo in data.RecipientInfos)
            {
                if (recipientInfo.Ktri.HasValue)
                {
                    recipientInfos.Add(new KeyTransRecipientInfo(new ManagedKeyTransPal(recipientInfo.Ktri.Value)));
                }
                else if (recipientInfo.Kari.HasValue)
                {
                    for (int i = 0; i < recipientInfo.Kari.Value.RecipientEncryptedKeys.Length; i++)
                    {
                        recipientInfos.Add(
                            new KeyAgreeRecipientInfo(new ManagedKeyAgreePal(recipientInfo.Kari.Value, i)));
                    }
                }
                else
                {
                    Debug.Fail($"{nameof(RecipientInfoAsn)} deserialized with an unknown recipient type");
                    throw new CryptographicException();
                }
            }

            return new ManagedDecryptorPal(copy, data, new RecipientInfoCollection(recipientInfos));
        }
    }
}
