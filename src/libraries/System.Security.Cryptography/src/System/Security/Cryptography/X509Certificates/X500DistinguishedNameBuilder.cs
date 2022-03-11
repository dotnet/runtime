// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X500DistinguishedNameBuilder
    {
        private readonly List<byte[]> _encodedComponents = new List<byte[]>();

        public void Add(string oidValue!!, string value!!, UniversalTagNumber? stringEncodingType = null)
        {
            UniversalTagNumber tag = GetAndValidateTagNumber(stringEncodingType);
            EncodeComponent(oidValue, value, tag);
        }

        public void Add(Oid oid!!, string value!!, UniversalTagNumber? stringEncodingType = null)
        {
            if (string.IsNullOrEmpty(oid.Value))
                throw new ArgumentException(SR.Format(SR.Arg_EmptyOrNullString_Named, "oid.Value"), nameof(oid));

            UniversalTagNumber tag = GetAndValidateTagNumber(stringEncodingType);
            EncodeComponent(oid.Value, value, tag);
        }

        public void AddEncoded(string oidValue!!, byte[] encodedValue!!)
        {
            EncodeComponent(oidValue, encodedValue);
        }

        public void AddEncoded(string oidValue!!, ReadOnlySpan<byte> encodedValue)
        {
            EncodeComponent(oidValue, encodedValue);
        }

        public void AddEncoded(Oid oid!!, byte[] encodedValue!!)
        {
            if (string.IsNullOrEmpty(oid.Value))
                throw new ArgumentException(SR.Format(SR.Arg_EmptyOrNullString_Named, "oid.Value"), nameof(oid));

            EncodeComponent(oid.Value, encodedValue);
        }

        public void AddEncoded(Oid oid!!, ReadOnlySpan<byte> encodedValue)
        {
            if (string.IsNullOrEmpty(oid.Value))
                throw new ArgumentException(SR.Format(SR.Arg_EmptyOrNullString_Named, "oid.Value"), nameof(oid));

            EncodeComponent(oid.Value, encodedValue);
        }

        public void AddEmailAddress(string emailAddress)
        {
            //RFC 5912:
            // id-emailAddress          AttributeType ::= { pkcs-9 1 }
            //   at-emailAddress ATTRIBUTE ::= {TYPE IA5String
            //       (SIZE (1..ub-emailaddress-length)) IDENTIFIED BY
            //       id-emailAddress }
            // ub-emailaddress-length INTEGER ::= 255

            ArgumentException.ThrowIfNullOrEmpty(emailAddress);

            if (emailAddress.Length > 255)
            {
                throw new ArgumentException(SR.Argument_X500_EmailTooLong, nameof(emailAddress));
            }

            EncodeComponent(Oids.EmailAddress, emailAddress, UniversalTagNumber.IA5String);
        }

        public void AddCommonName(string commonName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-commonName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(commonName);
            EncodeComponent(Oids.CommonName, commonName, UniversalTagNumber.UTF8String);
        }

        public void AddLocalityName(string localityName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-commonName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(localityName);
            EncodeComponent(Oids.LocalityName, localityName, UniversalTagNumber.UTF8String);
        }

        public void AddCountryOrRegion(string twoLetterCode)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-countryName
            // WITH SYNTAX CountryName
            // CountryName ::= PrintableString(SIZE (2))

            ArgumentException.ThrowIfNullOrEmpty(twoLetterCode);

            // This could be a surrogate pair, but since we are encoding as a PrintableString,
            // those will be prohibited, so "Length" should be fine for checking the length of
            // the string.
            if (twoLetterCode.Length != 2)
            {
                throw new ArgumentException(SR.Argument_X500_InvalidCountryOrRegion, nameof(twoLetterCode));
            }

            EncodeComponent(Oids.CountryOrRegionName, twoLetterCode, UniversalTagNumber.PrintableString);
        }

        public void AddOrganizationName(string organizationName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-organizationName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(organizationName);
            EncodeComponent(Oids.Organization, organizationName, UniversalTagNumber.UTF8String);
        }

        public void AddOrganizationalUnitName(string organizationalUnitName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-organizationalUnitName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(organizationalUnitName);
            EncodeComponent(Oids.OrganizationalUnit, organizationalUnitName, UniversalTagNumber.UTF8String);
        }

        public void AddStateOrProvinceName(string stateOrProvinceName)
        {
            // ITU T-REC X.520 Annex A:
            // id-at-stateOrProvinceName
            // WITH SYNTAX UnboundedDirectoryString

            ArgumentException.ThrowIfNullOrEmpty(stateOrProvinceName);
            EncodeComponent(Oids.StateOrProvinceName, stateOrProvinceName, UniversalTagNumber.UTF8String);
        }

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

        public X500DistinguishedName Build()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                foreach (byte[] component in _encodedComponents)
                {
                    writer.WriteEncodedValue(component);
                }
            }

            byte[] rented = CryptoPool.Rent(writer.GetEncodedLength());
            int encoded = writer.Encode(rented);
            X500DistinguishedName name = new X500DistinguishedName(rented.AsSpan(0, encoded));
            CryptoPool.Return(rented, clearSize: 0); // Distinguished Names do not contain sensitive information.
            return name;
        }

        private void EncodeComponent(
            string oid,
            string value,
            UniversalTagNumber stringEncodingType,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSetOf())
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(oid);

                try
                {
                    writer.WriteCharacterString(stringEncodingType, value);
                }
                catch (EncoderFallbackException)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_Asn1_InvalidStringContents, stringEncodingType), paramName);
                }
            }

            _encodedComponents.Add(writer.Encode());
        }

        private void EncodeComponent(string oid, ReadOnlySpan<byte> value)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSetOf())
            using (writer.PushSequence())
            {
                writer.WriteObjectIdentifier(oid);
                writer.WriteEncodedValue(value);
            }

            _encodedComponents.Add(writer.Encode());
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
