// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal class Pkcs10CertificationRequestInfo
    {
        private static readonly byte[][] s_encodedVersion = DerEncoder.SegmentedEncodeUnsignedInteger(new byte[1]);

        internal X500DistinguishedName Subject { get; set; }
        internal PublicKey PublicKey { get; set; }
        internal Collection<X501Attribute> Attributes { get; } = new Collection<X501Attribute>();

        internal Pkcs10CertificationRequestInfo(
            X500DistinguishedName subject,
            PublicKey publicKey,
            IEnumerable<X501Attribute> attributes)
        {
            if (subject == null)
                throw new ArgumentNullException(nameof(subject));
            if (publicKey == null)
                throw new ArgumentNullException(nameof(publicKey));

            Subject = subject;
            PublicKey = publicKey;

            if (attributes != null)
            {
                Attributes.AddRange(attributes);
            }
        }

        private byte[] Encode()
        {
            // CertificationRequestInfo::= SEQUENCE {
            //   version INTEGER { v1(0) } (v1,...),
            //   subject Name,
            //   subjectPKInfo SubjectPublicKeyInfo{ { PKInfoAlgorithms } },
            //   attributes[0] Attributes{ { CRIAttributes } }
            // }
            //
            // Attributes { ATTRIBUTE:IOSet } ::= SET OF Attribute{{ IOSet }}

            byte[][] attrSet = Attributes.SegmentedEncodeAttributeSet();

            // Replace the tag with ContextSpecific0.
            attrSet[0][0] = DerSequenceReader.ContextSpecificConstructedTag0;

            return DerEncoder.ConstructSequence(
                s_encodedVersion,
                Subject.RawData.WrapAsSegmentedForSequence(),
                PublicKey.SegmentedEncodeSubjectPublicKeyInfo(),
                attrSet);
        }

        internal byte[] ToPkcs10Request(X509SignatureGenerator signatureGenerator, HashAlgorithmName hashAlgorithm)
        {
            // State validation should be runtime checks if/when this becomes public API
            Debug.Assert(signatureGenerator != null);
            Debug.Assert(Subject != null);
            Debug.Assert(PublicKey != null);

            // CertificationRequest ::= SEQUENCE {
            //   certificationRequestInfo CertificationRequestInfo,
            //   signatureAlgorithm AlgorithmIdentifier{ { SignatureAlgorithms } },
            //   signature BIT STRING
            //  }

            byte[] encoded = Encode();
            byte[] signature = signatureGenerator.SignData(encoded, hashAlgorithm);
            byte[] signatureAlgorithm = signatureGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);

            EncodingHelpers.ValidateSignatureAlgorithm(signatureAlgorithm);

            return DerEncoder.ConstructSequence(
                encoded.WrapAsSegmentedForSequence(),
                signatureAlgorithm.WrapAsSegmentedForSequence(),
                DerEncoder.SegmentedEncodeBitString(signature));
        }
    }
}
