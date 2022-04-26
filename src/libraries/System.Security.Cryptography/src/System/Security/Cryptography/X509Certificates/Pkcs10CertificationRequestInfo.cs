// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class Pkcs10CertificationRequestInfo
    {
        internal X500DistinguishedName Subject { get; set; }
        internal PublicKey PublicKey { get; set; }
        internal Collection<X501Attribute> Attributes { get; } = new Collection<X501Attribute>();

        internal Pkcs10CertificationRequestInfo(
            X500DistinguishedName subject,
            PublicKey publicKey,
            IEnumerable<X501Attribute> attributes)
        {
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(publicKey);

            Subject = subject;
            PublicKey = publicKey;

            if (attributes != null)
            {
                Attributes.AddRange(attributes);
            }
        }

        internal byte[] ToPkcs10Request(X509SignatureGenerator signatureGenerator, HashAlgorithmName hashAlgorithm)
        {
            // State validation should be runtime checks if/when this becomes public API
            Debug.Assert(signatureGenerator != null);
            Debug.Assert(Subject != null);
            Debug.Assert(PublicKey != null);

            byte[] signatureAlgorithm = signatureGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
            AlgorithmIdentifierAsn signatureAlgorithmAsn;

            // Deserialization also does validation of the value (except for Parameters, which have to be validated separately).
            signatureAlgorithmAsn = AlgorithmIdentifierAsn.Decode(signatureAlgorithm, AsnEncodingRules.DER);
            if (signatureAlgorithmAsn.Parameters.HasValue)
            {
                Helpers.ValidateDer(signatureAlgorithmAsn.Parameters.Value.Span);
            }

            SubjectPublicKeyInfoAsn spki = default;
            spki.Algorithm = new AlgorithmIdentifierAsn { Algorithm = PublicKey.Oid!.Value!, Parameters = PublicKey.EncodedParameters.RawData };
            spki.SubjectPublicKey = PublicKey.EncodedKeyValue.RawData;

            var attributes = new AttributeAsn[Attributes.Count];
            for (int i = 0; i < attributes.Length; i++)
            {
                attributes[i] = new AttributeAsn(Attributes[i]);
            }

            CertificationRequestInfoAsn requestInfo = new CertificationRequestInfoAsn
            {
                Version = 0,
                Subject = this.Subject.RawData,
                SubjectPublicKeyInfo = spki,
                Attributes = attributes
            };

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            requestInfo.Encode(writer);
            byte[] encodedRequestInfo = writer.Encode();
            writer.Reset();

            CertificationRequestAsn certificationRequest = new CertificationRequestAsn
            {
                CertificationRequestInfo = requestInfo,
                SignatureAlgorithm = signatureAlgorithmAsn,
                SignatureValue = signatureGenerator.SignData(encodedRequestInfo, hashAlgorithm),
            };

            certificationRequest.Encode(writer);
            return writer.Encode();
        }
    }
}
