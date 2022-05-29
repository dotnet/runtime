// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// This class facilitates building a distinguished name for an X.509 certificate.
    /// </summary>
    /// <remarks>
    /// When constructing the SEQUENCE OF Relative Distinguished Names, this builder
    /// constructs the SEQUENCE OF in the opposite order which they were added to the
    /// builder. For example:
    /// <code>
    /// builder.AddCommonName("Contoso");
    /// builder.AddCountryOrRegion("US");
    /// </code>
    /// Will produce a SEQUENCE OF with the country or region first, and the common
    /// name second. Because <see cref="X500DistinguishedName" /> follows RFC 4514
    /// when converting a Distinguished Name to a string, it starts with the last
    /// Relative Distinuished Name component, moving backward toward the first.
    /// Because the builder creates the SEQUENCE OF in reverse, and
    /// <see cref="X500DistinguishedName" /> is also in reverse, it gives the appearence
    /// of the added attributes and the string representation of the Distinguished Name in
    /// the same order.
    /// </remarks>
    public sealed class X500DistinguishedNameBuilder
    {
        private readonly List<byte[]> _encodedComponents = new List<byte[]>();
        private readonly AsnWriter _writer = new AsnWriter(AsnEncodingRules.DER);

        /// <summary>
        /// Adds a Relative Distinguished Name attribute identified by an OID.
        /// </summary>
        /// <param name="oidValue">The OID of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <param name="stringEncodingType">
        /// The encoding type to use when encoding the <paramref name="value" />
        /// in to the attribute.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oidValue" /> or <paramref name="value" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   <paramref name="oidValue" /> is an empty string or not a valid OID.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="stringEncodingType" /> is not a type for character strings.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="value" /> is not encodable as defined by <paramref name="stringEncodingType" />.
        /// </para>
        /// </exception>
        public void Add(string oidValue, string value, UniversalTagNumber? stringEncodingType = null)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentException.ThrowIfNullOrEmpty(oidValue);

            UniversalTagNumber tag = GetAndValidateTagNumber(stringEncodingType);
            EncodeComponent(oidValue, value, tag);
        }

        /// <summary>
        /// Adds a Relative Distinguished Name attribute identified by an OID.
        /// </summary>
        /// <param name="oid">The OID of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <param name="stringEncodingType">
        /// The encoding type to use when encoding the <paramref name="value" />
        /// in to the attribute.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oid" /> or <paramref name="value" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   <paramref name="oid" /> does not contain a valid OID.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="stringEncodingType" /> is not a type for character strings.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="value" /> is not encodable as defined by <paramref name="stringEncodingType" />.
        /// </para>
        /// </exception>
        public void Add(Oid oid, string value, UniversalTagNumber? stringEncodingType = null)
        {
            ArgumentNullException.ThrowIfNull(oid);
            ArgumentNullException.ThrowIfNull(value);

            if (string.IsNullOrEmpty(oid.Value))
                throw new ArgumentException(SR.Format(SR.Arg_EmptyOrNullString_Named, "oid.Value"), nameof(oid));

            UniversalTagNumber tag = GetAndValidateTagNumber(stringEncodingType);
            EncodeComponent(oid.Value, value, tag);
        }

        /// <summary>
        /// Adds an email address attribute.
        /// </summary>
        /// <param name="emailAddress">The email address to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="emailAddress" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="emailAddress" /> is empty or exceeds 255 characters.
        /// </exception>
        /// <remarks>
        /// This encodes an attribute with the OID 1.2.840.113549.1.9.1 as an IA5String.
        /// </remarks>
        public void AddEmailAddress(string emailAddress)
        {
            //RFC 2585:
            // emailAddress ATTRIBUTE ::= {
            //   WITH SYNTAX IA5String (SIZE(1..pkcs-9-ub-emailAddress))
            //   EQUALITY MATCHING RULE pkcs9CaseIgnoreMatch
            //   ID pkcs-9-at-emailAdress
            // }
            // pkcs-9-ub-pkcs9String  INTEGER ::= 255
            // pkcs-9-ub-emailAddress INTEGER ::= pkcs-9-ub-pkcs9String

            // We don't attempt to do any input validation that the email resembles an email
            // address, only that it conforms to the ASN.1 syntax.

            ArgumentException.ThrowIfNullOrEmpty(emailAddress);

            if (emailAddress.Length > 255)
            {
                throw new ArgumentException(SR.Argument_X500_EmailTooLong, nameof(emailAddress));
            }

            EncodeComponent(Oids.EmailAddress, emailAddress, UniversalTagNumber.IA5String);
        }

        /// <summary>
        /// Adds a common name attribute.
        /// </summary>
        /// <param name="commonName">The common name to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="commonName" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="commonName" /> is empty.
        /// </exception>
        /// <remarks>
        /// This encodes an attribute with the OID 2.5.4.3 as a UTF8String.
        /// </remarks>
        public void AddCommonName(string commonName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-commonName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(commonName);
            EncodeComponent(Oids.CommonName, commonName, UniversalTagNumber.UTF8String);
        }

        /// <summary>
        /// Adds a locality name attribute.
        /// </summary>
        /// <param name="localityName">The locality name to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localityName" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="localityName" /> is empty.
        /// </exception>
        /// <remarks>
        /// This encodes an attribute with the OID 2.5.4.7 as a UTF8String.
        /// </remarks>
        public void AddLocalityName(string localityName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-localityName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(localityName);
            EncodeComponent(Oids.LocalityName, localityName, UniversalTagNumber.UTF8String);
        }

        /// <summary>
        /// Adds a country or region attribute.
        /// </summary>
        /// <param name="twoLetterCode">The two letter code of the country or region.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="twoLetterCode" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="twoLetterCode" /> is not exactly two characters, or contains
        /// characters that are not A through Z.
        /// </exception>
        /// <remarks>
        /// <para>This encodes an attribute with the OID 2.5.4.6 as a PrintableString.</para>
        /// <para>
        /// <paramref name="twoLetterCode" /> should be a two letter ISO 3166 alpha-2 code, and
        /// will be normalized to upper case characters.
        /// </para>
        /// </remarks>
        public void AddCountryOrRegion(string twoLetterCode)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-countryName
            // WITH SYNTAX CountryName
            // CountryName ::= PrintableString(SIZE (2))
            // An attribute value for country name is a string chosen from ISO 3166-1 alpha-2 or ISO 3166-3 alpha-2.
            // We can't reasonably enforce the ISO 3166 list, but we can ensure it's a two letter ISO code
            // and consists of alpha characters.

            ArgumentException.ThrowIfNullOrEmpty(twoLetterCode);
            ReadOnlySpan<char> twoLetterCodeSpan = twoLetterCode;

            // This could be a surrogate pair, but since we are encoding as a PrintableString,
            // those will be prohibited, so "Length" should be fine for checking the length of
            // the string.
            // Input must be A-Z per ISO 3166.
            if (twoLetterCode.Length != 2 || !char.IsAsciiLetter(twoLetterCode[0]) || !char.IsAsciiLetter(twoLetterCode[1]))
            {
                throw new ArgumentException(SR.Argument_X500_InvalidCountryOrRegion, nameof(twoLetterCode));
            }

            Span<char> fixupTwoLetterCode = stackalloc char[2];
            int written = twoLetterCodeSpan.ToUpperInvariant(fixupTwoLetterCode);
            Debug.Assert(written == 2);

            EncodeComponent(
                Oids.CountryOrRegionName,
                fixupTwoLetterCode,
                UniversalTagNumber.PrintableString,
                nameof(twoLetterCode));
        }

        /// <summary>
        /// Adds an organization name attribute.
        /// </summary>
        /// <param name="organizationName">The organization name to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="organizationName" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="organizationName" /> is empty.
        /// </exception>
        /// <remarks>
        /// This encodes an attribute with the OID 2.5.4.10 as a UTF8String.
        /// </remarks>
        public void AddOrganizationName(string organizationName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-organizationName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(organizationName);
            EncodeComponent(Oids.Organization, organizationName, UniversalTagNumber.UTF8String);
        }

        /// <summary>
        /// Adds an organizational unit name attribute.
        /// </summary>
        /// <param name="organizationalUnitName">The organizational unit name to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="organizationalUnitName" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="organizationalUnitName" /> is empty.
        /// </exception>
        /// <remarks>
        /// This encodes an attribute with the OID 2.5.4.11 as a UTF8String.
        /// </remarks>
        public void AddOrganizationalUnitName(string organizationalUnitName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-organizationalUnitName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(organizationalUnitName);
            EncodeComponent(Oids.OrganizationalUnit, organizationalUnitName, UniversalTagNumber.UTF8String);
        }

        /// <summary>
        /// Adds a state or province name attribute.
        /// </summary>
        /// <param name="stateOrProvinceName">The state or province name to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stateOrProvinceName" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="stateOrProvinceName" /> is empty.
        /// </exception>
        /// <remarks>
        /// This encodes an attribute with the OID 2.5.4.8 as a UTF8String.
        /// </remarks>
        public void AddStateOrProvinceName(string stateOrProvinceName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-stateOrProvinceName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(stateOrProvinceName);
            EncodeComponent(Oids.StateOrProvinceName, stateOrProvinceName, UniversalTagNumber.UTF8String);
        }

        /// <summary>
        /// Adds a domain component attribute.
        /// </summary>
        /// <param name="domainComponent">The domain component to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="domainComponent" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="domainComponent" /> is empty.
        /// </exception>
        /// <remarks>
        /// This encodes an attribute with the OID 0.9.2342.19200300.100.1.25 as an IA5String.
        /// </remarks>
        public void AddDomainComponent(string domainComponent)
        {
            // RFC 5912
            // id-domainComponent      AttributeType ::=
            //  { itu-t(0) data(9) pss(2342) ucl(19200300) pilot(100)
            //  pilotAttributeType(1) 25 }
            //  at-domainComponent ATTRIBUTE ::= {TYPE IA5String
            //  IDENTIFIED BY id-domainComponent }

            ArgumentException.ThrowIfNullOrEmpty(domainComponent);
            EncodeComponent(Oids.DomainComponent, domainComponent, UniversalTagNumber.IA5String);
        }

        /// <summary>
        /// Builds an <see cref="X500DistinguishedName" /> that represents the encoded attributes.
        /// </summary>
        /// <returns>
        /// An <see cref="X500DistinguishedName" /> that represents the encoded attributes.
        /// </returns>
        public X500DistinguishedName Build()
        {
            _writer.Reset();

            using (_writer.PushSequence())
            {
                for (int i = _encodedComponents.Count - 1; i >= 0; i--)
                {
                    _writer.WriteEncodedValue(_encodedComponents[i]);
                }
            }

            byte[] rented = CryptoPool.Rent(_writer.GetEncodedLength());
            int encoded = _writer.Encode(rented);
            X500DistinguishedName name = new X500DistinguishedName(rented.AsSpan(0, encoded));
            CryptoPool.Return(rented, clearSize: 0); // Distinguished Names do not contain sensitive information.
            return name;
        }

        private void EncodeComponent(
            string oid,
            ReadOnlySpan<char> value,
            UniversalTagNumber stringEncodingType,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            _writer.Reset();

            using (_writer.PushSetOf())
            using (_writer.PushSequence())
            {
                _writer.WriteObjectIdentifier(oid);

                try
                {
                    _writer.WriteCharacterString(stringEncodingType, value);
                }
                catch (EncoderFallbackException)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_Asn1_InvalidStringContents, stringEncodingType), paramName);
                }
            }

            _encodedComponents.Add(_writer.Encode());
        }

        private static UniversalTagNumber GetAndValidateTagNumber(UniversalTagNumber? stringEncodingType)
        {
            switch (stringEncodingType)
            {
                case null:
                    return UniversalTagNumber.UTF8String;
                case UniversalTagNumber.UTF8String:
                case UniversalTagNumber.NumericString:
                case UniversalTagNumber.PrintableString:
                case UniversalTagNumber.IA5String:
                case UniversalTagNumber.VisibleString:
                case UniversalTagNumber.BMPString:
                case UniversalTagNumber.T61String:
                    return stringEncodingType.GetValueOrDefault();
                default:
                    throw new ArgumentException(SR.Argument_Asn1_InvalidCharacterString, nameof(stringEncodingType));
            }
        }
    }
}
