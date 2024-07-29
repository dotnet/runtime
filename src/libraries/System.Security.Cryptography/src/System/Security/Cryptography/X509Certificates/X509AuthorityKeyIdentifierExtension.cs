// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    ///   Represents the Authority Key Identifier X.509 Extension (2.5.29.35).
    /// </summary>
    public sealed class X509AuthorityKeyIdentifierExtension : X509Extension
    {
        private bool _decoded;
        private X500DistinguishedName? _simpleIssuer;
        private ReadOnlyMemory<byte>? _keyIdentifier;
        private ReadOnlyMemory<byte>? _rawIssuer;
        private ReadOnlyMemory<byte>? _serialNumber;

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509AuthorityKeyIdentifierExtension" />
        ///   class.
        /// </summary>
        public X509AuthorityKeyIdentifierExtension()
            : base(Oids.AuthorityKeyIdentifierOid)
        {
            _decoded = true;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509AuthorityKeyIdentifierExtension" />
        ///   class from an encoded representation of the extension and an optional critical marker.
        /// </summary>
        /// <param name="rawData">
        ///   The encoded data used to create the extension.
        /// </param>
        /// <param name="critical">
        ///   <see langword="true" /> if the extension is critical;
        ///   otherwise, <see langword="false" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="rawData" /> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="rawData" /> did not decode as an Authority Key Identifier extension.
        /// </exception>
        public X509AuthorityKeyIdentifierExtension(byte[] rawData, bool critical = false)
            : base(Oids.AuthorityKeyIdentifierOid, rawData, critical)
        {
            Decode(RawData);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509AuthorityKeyIdentifierExtension" />
        ///   class from an encoded representation of the extension and an optional critical marker.
        /// </summary>
        /// <param name="rawData">
        ///   The encoded data used to create the extension.
        /// </param>
        /// <param name="critical">
        ///   <see langword="true" /> if the extension is critical;
        ///   otherwise, <see langword="false" />.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   <paramref name="rawData" /> did not decode as an Authority Key Identifier extension.
        /// </exception>
        public X509AuthorityKeyIdentifierExtension(ReadOnlySpan<byte> rawData, bool critical = false)
            : base(Oids.AuthorityKeyIdentifierOid, rawData, critical)
        {
            Decode(RawData);
        }

        /// <inheritdoc />
        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
        }

        /// <summary>
        ///   Gets a value whose contents represent the subject key identifier value
        ///   from this certificate's issuing Certificate Authority (CA), when specified.
        /// </summary>
        /// <value>
        ///   The subject key identifier from this certificate's issuing Certificate Authority (CA).
        /// </value>
        public ReadOnlyMemory<byte>? KeyIdentifier
        {
            get
            {
                if (!_decoded)
                {
                    Decode(RawData);
                }

                return _keyIdentifier;
            }
        }

        /// <summary>
        ///   Gets the <see cref="X509Certificate2.IssuerName"/> value from this certificate's
        ///   issuing Certificate Authority (CA), when available.
        /// </summary>
        /// <value>
        ///   The <see cref="X509Certificate2.IssuerName"/> value from this certificate's
        ///   issuing Certificate Authority (CA).
        /// </value>
        /// <remarks>
        ///   This property is <see langword="null" /> if any of the following are true:
        ///
        ///   <list type="bullet">
        ///     <item>The encoded extension does not include an <c>authorityCertIssuer</c> value.</item>
        ///     <item>The <c>authorityCertIssuer</c> value contains no <c>directoryName</c> values.</item>
        ///     <item>The <c>authorityCertIssuer</c> value contains multiple <c>directoryName</c> values.</item>
        ///     <item>
        ///       The <c>directoryName</c> value did not successfully decode as
        ///       an <see cref="X500DistinguishedName"/>.
        ///     </item>
        ///   </list>
        /// </remarks>
        /// <seealso cref="RawIssuer" />
        public X500DistinguishedName? NamedIssuer
        {
            get
            {
                if (!_decoded)
                {
                    Decode(RawData);
                }

                return _simpleIssuer;
            }
        }

        /// <summary>
        ///   Gets a value whose contents represent the encoded representation of the
        ///   <c>authorityCertIssuer</c> field from the extension,
        ///   or <see langword="null" /> when the extension does not contain an authority
        ///   certificate issuer field.
        /// </summary>
        /// <value>
        ///   The encoded <c>authorityCertIssuer</c> value.
        /// </value>
        public ReadOnlyMemory<byte>? RawIssuer
        {
            get
            {
                if (!_decoded)
                {
                    Decode(RawData);
                }

                return _rawIssuer;
            }
        }

        /// <summary>
        ///   Gets a value whose contents represent the serial number of this certificate's
        ///   issuing Certificate Authority (CA).
        /// </summary>
        /// <value>
        ///   The serial number from this certificate's issuing Certificate Authority (CA).
        /// </value>
        public ReadOnlyMemory<byte>? SerialNumber
        {
            get
            {
                if (!_decoded)
                {
                    Decode(RawData);
                }

                return _serialNumber;
            }
        }

        /// <summary>
        ///   Creates an <see cref="X509AuthorityKeyIdentifierExtension"/> that specifies
        ///   the key identifier value from a subject key identifier extension.
        /// </summary>
        /// <param name="subjectKeyIdentifier">
        ///   The subject key identifier extension from the Certificate Authority (CA) certificate
        ///   that will sign this extension.
        /// </param>
        /// <returns>
        ///   The configured extension.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="subjectKeyIdentifier"/> is <see langword="null" />.
        /// </exception>
        public static X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(
            X509SubjectKeyIdentifierExtension subjectKeyIdentifier)
        {
            ArgumentNullException.ThrowIfNull(subjectKeyIdentifier);

            return CreateFromSubjectKeyIdentifier(
                subjectKeyIdentifier.SubjectKeyIdentifierBytes.Span);
        }

        /// <summary>
        ///   Creates an <see cref="X509AuthorityKeyIdentifierExtension"/> that specifies
        ///   the provided key identifier value.
        /// </summary>
        /// <param name="subjectKeyIdentifier">
        ///   The subject key identifier value from the Certificate Authority (CA) certificate
        ///   that will sign this extension.
        /// </param>
        /// <returns>
        ///   The configured extension.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="subjectKeyIdentifier"/> is <see langword="null" />.
        /// </exception>
        public static X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(
        byte[] subjectKeyIdentifier)
        {
            ArgumentNullException.ThrowIfNull(subjectKeyIdentifier);

            return CreateFromSubjectKeyIdentifier(new ReadOnlySpan<byte>(subjectKeyIdentifier));
        }

        /// <summary>
        ///   Creates an <see cref="X509AuthorityKeyIdentifierExtension"/> that specifies
        ///   the provided key identifier value.
        /// </summary>
        /// <param name="subjectKeyIdentifier">
        ///   The subject key identifier value from the Certificate Authority (CA) certificate
        ///   that will sign this extension.
        /// </param>
        /// <returns>
        ///   The configured extension.
        /// </returns>
        /// <seealso cref="X509SubjectKeyIdentifierExtension.SubjectKeyIdentifierBytes"/>
        public static X509AuthorityKeyIdentifierExtension CreateFromSubjectKeyIdentifier(
            ReadOnlySpan<byte> subjectKeyIdentifier)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                writer.WriteOctetString(subjectKeyIdentifier, new Asn1Tag(TagClass.ContextSpecific, 0));
            }

            // Most KeyIdentifier values are computed from SHA-1 (20 bytes), which produces a 24-byte
            // value for this extension.
            // Let's go ahead and be really generous before moving to redundant array allocation.
            Span<byte> stackSpan = stackalloc byte[64];
            scoped ReadOnlySpan<byte> encoded;

            if (writer.TryEncode(stackSpan, out int written))
            {
                encoded = stackSpan.Slice(0, written);
            }
            else
            {
                encoded = writer.Encode();
            }

            return new X509AuthorityKeyIdentifierExtension(encoded);
        }

        /// <summary>
        ///   Creates an <see cref="X509AuthorityKeyIdentifierExtension"/> that specifies
        ///   the provided issuer name and serial number.
        /// </summary>
        /// <param name="issuerName">
        ///   The issuer name value from the Certificate Authority (CA) certificate that will
        ///   sign this extension.
        /// </param>
        /// <param name="serialNumber">
        ///   The serial number value from the Certificate Authority (CA) certificate that will
        ///   sign this extension.
        /// </param>
        /// <returns>
        ///   The configured extension.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="issuerName"/> or <paramref name="serialNumber"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="serialNumber"/> is invalid because the leading 9 bits are either
        ///   all zero or all one.
        /// </exception>
        public static X509AuthorityKeyIdentifierExtension CreateFromIssuerNameAndSerialNumber(
            X500DistinguishedName issuerName,
            byte[] serialNumber)
        {
            ArgumentNullException.ThrowIfNull(issuerName);
            ArgumentNullException.ThrowIfNull(serialNumber);

            return CreateFromIssuerNameAndSerialNumber(issuerName, new ReadOnlySpan<byte>(serialNumber));
        }

        /// <summary>
        ///   Creates an <see cref="X509AuthorityKeyIdentifierExtension"/> that specifies
        ///   the provided issuer name and serial number.
        /// </summary>
        /// <param name="issuerName">
        ///   The issuer name value from the Certificate Authority (CA) certificate that will
        ///   sign this extension.
        /// </param>
        /// <param name="serialNumber">
        ///   The serial number value from the Certificate Authority (CA) certificate that will
        ///   sign this extension.
        /// </param>
        /// <returns>
        ///   The configured extension.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="issuerName"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="serialNumber"/> is invalid because the leading 9 bits are either
        ///   all zero or all one.
        /// </exception>
        public static X509AuthorityKeyIdentifierExtension CreateFromIssuerNameAndSerialNumber(
            X500DistinguishedName issuerName,
            ReadOnlySpan<byte> serialNumber)
        {
            ArgumentNullException.ThrowIfNull(issuerName);

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4)))
                {
                    writer.WriteEncodedValue(issuerName.RawData);
                }

                try
                {
                    writer.WriteInteger(serialNumber, new Asn1Tag(TagClass.ContextSpecific, 2));
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(SR.Argument_InvalidSerialNumberBytes, nameof(serialNumber));
                }
            }

            return new X509AuthorityKeyIdentifierExtension(writer.Encode());
        }


        /// <summary>
        ///   Creates an <see cref="X509AuthorityKeyIdentifierExtension"/> that specifies
        ///   the provided key identifier, issuer name and serial number.
        /// </summary>
        /// <param name="keyIdentifier">
        ///   The subject key identifier value from the Certificate Authority (CA) certificate
        ///   that will sign this extension.
        /// </param>
        /// <param name="issuerName">
        ///   The issuer name value from the Certificate Authority (CA) certificate that will
        ///   sign this extension.
        /// </param>
        /// <param name="serialNumber">
        ///   The serial number value from the Certificate Authority (CA) certificate that will
        ///   sign this extension.
        /// </param>
        /// <returns>
        ///   The configured extension.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="keyIdentifier" />, <paramref name="issuerName"/> or
        ///   <paramref name="serialNumber"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="serialNumber"/> is invalid because the leading 9 bits are either
        ///   all zero or all one.
        /// </exception>
        public static X509AuthorityKeyIdentifierExtension Create(
            byte[] keyIdentifier,
            X500DistinguishedName issuerName,
            byte[] serialNumber)
        {
            ArgumentNullException.ThrowIfNull(keyIdentifier);
            ArgumentNullException.ThrowIfNull(issuerName);
            ArgumentNullException.ThrowIfNull(serialNumber);

            return Create(
                new ReadOnlySpan<byte>(keyIdentifier),
                issuerName,
                new ReadOnlySpan<byte>(serialNumber));
        }

        /// <summary>
        ///   Creates an <see cref="X509AuthorityKeyIdentifierExtension"/> that specifies
        ///   the provided key identifier, issuer name and serial number.
        /// </summary>
        /// <param name="keyIdentifier">
        ///   The subject key identifier value from the Certificate Authority (CA) certificate
        ///   that will sign this extension.
        /// </param>
        /// <param name="issuerName">
        ///   The issuer name value from the Certificate Authority (CA) certificate that will
        ///   sign this extension.
        /// </param>
        /// <param name="serialNumber">
        ///   The serial number value from the Certificate Authority (CA) certificate that will
        ///   sign this extension.
        /// </param>
        /// <returns>
        ///   The configured extension.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="issuerName"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="serialNumber"/> is invalid because the leading 9 bits are either
        ///   all zero or all one.
        /// </exception>
        public static X509AuthorityKeyIdentifierExtension Create(
            ReadOnlySpan<byte> keyIdentifier,
            X500DistinguishedName issuerName,
            ReadOnlySpan<byte> serialNumber)
        {
            ArgumentNullException.ThrowIfNull(issuerName);

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                writer.WriteOctetString(keyIdentifier, new Asn1Tag(TagClass.ContextSpecific, 0));

                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1)))
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4)))
                {
                    writer.WriteEncodedValue(issuerName.RawData);
                }

                try
                {
                    writer.WriteInteger(serialNumber, new Asn1Tag(TagClass.ContextSpecific, 2));
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(SR.Argument_InvalidSerialNumberBytes, nameof(serialNumber));
                }
            }

            return new X509AuthorityKeyIdentifierExtension(writer.Encode());
        }

        /// <summary>
        ///   Creates an <see cref="X509AuthorityKeyIdentifierExtension"/> based on values
        ///   from the provided certificate.
        /// </summary>
        /// <param name="certificate">
        ///   The Certificate Authority (CA) certificate that will sign this extension.
        /// </param>
        /// <param name="includeKeyIdentifier">
        ///   <see langword="true" /> to include the Subject Key Identifier value from the certificate
        ///   as the key identifier value in this extension; otherwise, <see langword="false" />.
        /// </param>
        /// <param name="includeIssuerAndSerial">
        ///   <see langword="true" /> to include the certificate's issuer name and serial number
        ///   in this extension; otherwise, <see langword="false" />.
        /// </param>
        /// <returns>
        ///   The configured extension.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificate"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="includeKeyIdentifier" /> is <see langword="true" />, but
        ///   <paramref name="certificate" /> does not contain a Subject Key Identifier extension.
        /// </exception>
        public static X509AuthorityKeyIdentifierExtension CreateFromCertificate(
            X509Certificate2 certificate,
            bool includeKeyIdentifier,
            bool includeIssuerAndSerial)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            if (includeKeyIdentifier)
            {
                X509SubjectKeyIdentifierExtension? skid =
                    (X509SubjectKeyIdentifierExtension?)certificate.Extensions[Oids.SubjectKeyIdentifier];

                if (skid is null)
                {
                    throw new CryptographicException(SR.Cryptography_X509_AKID_NoSKID);
                }

                ReadOnlySpan<byte> skidBytes = skid.SubjectKeyIdentifierBytes.Span;

                if (includeIssuerAndSerial)
                {
                    return Create(
                        skidBytes,
                        certificate.IssuerName,
                        certificate.SerialNumberBytes.Span);
                }

                return CreateFromSubjectKeyIdentifier(skidBytes);
            }
            else if (includeIssuerAndSerial)
            {
                return CreateFromIssuerNameAndSerialNumber(
                    certificate.IssuerName,
                    certificate.SerialNumberBytes.Span);
            }

            ReadOnlySpan<byte> emptyExtension = [0x30, 0x00];
            return new X509AuthorityKeyIdentifierExtension(emptyExtension);
        }

        private void Decode(ReadOnlySpan<byte> rawData)
        {
            _keyIdentifier = null;
            _simpleIssuer = null;
            _rawIssuer = null;
            _serialNumber = null;

            // https://datatracker.ietf.org/doc/html/rfc3280#section-4.2.1.1
            // AuthorityKeyIdentifier ::= SEQUENCE {
            //    keyIdentifier[0] KeyIdentifier OPTIONAL,
            //    authorityCertIssuer[1] GeneralNames OPTIONAL,
            //    authorityCertSerialNumber[2] CertificateSerialNumber OPTIONAL  }
            //
            // KeyIdentifier::= OCTET STRING

            try
            {
                AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.DER);
                AsnValueReader aki = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                Asn1Tag nextTag = default;

                if (aki.HasData)
                {
                    nextTag = aki.PeekTag();
                }

                if (nextTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    _keyIdentifier = aki.ReadOctetString(nextTag);

                    if (aki.HasData)
                    {
                        nextTag = aki.PeekTag();
                    }
                }

                if (nextTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
                {
                    byte[] rawIssuer = aki.PeekEncodedValue().ToArray();
                    _rawIssuer = rawIssuer;

                    AsnValueReader generalNames = aki.ReadSequence(nextTag);
                    bool foundIssuer = false;

                    // Walk all of the entities to make sure they decode legally, so no early abort.
                    while (generalNames.HasData)
                    {
                        GeneralNameAsn.Decode(ref generalNames, rawIssuer, out GeneralNameAsn decoded);

                        if (decoded.DirectoryName.HasValue)
                        {
                            if (!foundIssuer)
                            {
                                // Only ever try reading the first one.
                                // Don't just use a null check or we would load the last of an odd number.
                                foundIssuer = true;

                                _simpleIssuer = new X500DistinguishedName(
                                    decoded.DirectoryName.GetValueOrDefault().Span);
                            }
                            else
                            {
                                _simpleIssuer = null;
                            }
                        }
                    }

                    if (aki.HasData)
                    {
                        nextTag = aki.PeekTag();
                    }
                }

                if (nextTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
                {
                    _serialNumber = aki.ReadIntegerBytes(nextTag).ToArray();
                }

                aki.ThrowIfNotEmpty();
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            _decoded = true;
        }
    }
}
