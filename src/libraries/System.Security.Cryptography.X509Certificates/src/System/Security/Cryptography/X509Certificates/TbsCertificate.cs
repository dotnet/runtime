// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    /*
        TBSCertificate  ::=  SEQUENCE  {
            version         [0]  Version DEFAULT v1,
            serialNumber         CertificateSerialNumber,
            signature            AlgorithmIdentifier,
            issuer               Name,
            validity             Validity,
            subject              Name,
            subjectPublicKeyInfo SubjectPublicKeyInfo,
            issuerUniqueID  [1]  IMPLICIT UniqueIdentifier OPTIONAL,
                                -- If present, version MUST be v2 or v3
            subjectUniqueID [2]  IMPLICIT UniqueIdentifier OPTIONAL,
                                -- If present, version MUST be v2 or v3
            extensions      [3]  Extensions OPTIONAL
                                -- If present, version MUST be v3 --  }
    */
    internal sealed class TbsCertificate
    {
        // If TbsCertificate is made public API, consider having Version nullable to be
        // automatically assigned a value per RFC 3280.
        public byte Version { get; set; }
        public byte[] SerialNumber { get; set; }
        public byte[] SignatureAlgorithm { get; set; }
        public X500DistinguishedName Issuer { get; set; }
        public DateTimeOffset NotBefore { get; set; }
        public DateTimeOffset NotAfter { get; set; }
        public X500DistinguishedName Subject { get; set; }
        public PublicKey PublicKey { get; set; }
        // We don't support the IssuerUniqueId or SubjectUniqueId fields.
        // They might be needed if TbsCertificate becomes public API.
        public Collection<X509Extension> Extensions { get; } = new Collection<X509Extension>();

        private byte[] Encode(X509SignatureGenerator signatureGenerator, HashAlgorithmName hashAlgorithm)
        {
            // State validation should be runtime checks if/when this becomes public API
            Debug.Assert(Subject != null);
            Debug.Assert(PublicKey != null);

            // Under a public API model we could allow these to be null for a self-signed case.
            Debug.Assert(SerialNumber != null);
            Debug.Assert(Issuer != null);

            List<byte[][]> encodedFields = new List<byte[][]>();

            byte version = Version;

            if (version != 0)
            {
                byte[][] encodedVersion = DerEncoder.ConstructSegmentedSequence(
                    DerEncoder.SegmentedEncodeUnsignedInteger(new[] { version }));

                encodedVersion[0][0] = DerSequenceReader.ContextSpecificConstructedTag0;

                encodedFields.Add(encodedVersion);
            }

            encodedFields.Add(DerEncoder.SegmentedEncodeUnsignedInteger(SerialNumber));

            // SignatureAlgorithm: Use the specified value, or ask the generator (without mutating the class)
            byte[] signatureAlgorithm = SignatureAlgorithm ?? signatureGenerator.GetSignatureAlgorithmIdentifier(hashAlgorithm);
            EncodingHelpers.ValidateSignatureAlgorithm(signatureAlgorithm);

            encodedFields.Add(signatureAlgorithm.WrapAsSegmentedForSequence());

            // For public API allowing self-sign ease-of-use, this could be (Issuer ?? Subject).
            encodedFields.Add(Issuer.RawData.WrapAsSegmentedForSequence());

            encodedFields.Add(
                DerEncoder.ConstructSegmentedSequence(
                    EncodeValidityField(NotBefore, nameof(NotBefore)),
                    EncodeValidityField(NotAfter, nameof(NotAfter))));

            encodedFields.Add(Subject.RawData.WrapAsSegmentedForSequence());

            encodedFields.Add(PublicKey.SegmentedEncodeSubjectPublicKeyInfo());

            // Issuer and Subject Unique ID values would go here, if they were supported.

            if (Extensions.Count > 0)
            {
                Debug.Assert(version >= 2);

                List<byte[][]> encodedExtensions = new List<byte[][]>(Extensions.Count);

                // extensions[3]  Extensions OPTIONAL
                //
                // Since this doesn't say IMPLICIT, it will look like
                //
                // A3 [length]
                //   30 [length]
                //     First Extension
                //     Second Extension
                //     ...

                // An interesting quirk of skipping null values here is that
                // Extensions.Count == 0 => no extensions
                // Extensions.ContainsOnly(null) => empty extensions list

                HashSet<string> usedOids = new HashSet<string>(Extensions.Count);

                foreach (X509Extension extension in Extensions)
                {
                    if (extension == null)
                        continue;

                    if (!usedOids.Add(extension.Oid.Value))
                    {
                        throw new InvalidOperationException(
                            SR.Format(SR.Cryptography_CertReq_DuplicateExtension, extension.Oid.Value));
                    }

                    encodedExtensions.Add(extension.SegmentedEncodedX509Extension());
                }

                byte[][] extensionField = DerEncoder.ConstructSegmentedSequence(
                    DerEncoder.ConstructSegmentedSequence(encodedExtensions));
                extensionField[0][0] = DerSequenceReader.ContextSpecificConstructedTag3;

                encodedFields.Add(extensionField);
            }

            return DerEncoder.ConstructSequence(encodedFields);
        }

        private static byte[][] EncodeValidityField(DateTimeOffset validityField, string propertyName)
        {
            /* https://tools.ietf.org/html/rfc3280#section-4.1.2.5
             4.1.2.5  Validity

                The certificate validity period is the time interval during which the
                CA warrants that it will maintain information about the status of the
                certificate.  The field is represented as a SEQUENCE of two dates:
                the date on which the certificate validity period begins (notBefore)
                and the date on which the certificate validity period ends
                (notAfter).  Both notBefore and notAfter may be encoded as UTCTime or
                GeneralizedTime.

                CAs conforming to this profile MUST always encode certificate
                validity dates through the year 2049 as UTCTime; certificate validity
                dates in 2050 or later MUST be encoded as GeneralizedTime.

                The validity period for a certificate is the period of time from
                notBefore through notAfter, inclusive.
            */

            DateTime utcValue = validityField.UtcDateTime;

            // On the one hand, GeneralizedTime easily goes back to 1000, and possibly to 0000;
            // but on the other, dates before computers are just a bit beyond the pale.
            if (utcValue.Year < 1950)
            {
                throw new ArgumentOutOfRangeException(propertyName, utcValue, SR.Cryptography_CertReq_DateTooOld);
            }

            // Since the date encoding is effectively a DER rule (ensuring that two encoders
            // produce the same result), no option exists to encode the validity field as a
            // GeneralizedTime when it fits in the UTCTime constraint.
            if (utcValue.Year < 2050)
            {
                return DerEncoder.SegmentedEncodeUtcTime(utcValue);
            }

            return DerEncoder.SegmentedEncodeGeneralizedTime(utcValue);
        }

        internal byte[] Sign(X509SignatureGenerator signatureGenerator, HashAlgorithmName hashAlgorithm)
        {
            if (signatureGenerator == null)
                throw new ArgumentNullException(nameof(signatureGenerator));

            byte[] encoded = Encode(signatureGenerator, hashAlgorithm);
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
