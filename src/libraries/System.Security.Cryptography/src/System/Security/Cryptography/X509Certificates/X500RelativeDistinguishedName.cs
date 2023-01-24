// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    ///   Represents a Relative Distinguished Name component of an X.500 Distinguished Name.
    /// </summary>
    /// <seealso cref="X500DistinguishedName"/>
    public sealed class X500RelativeDistinguishedName
    {
        private Oid? _singleElementType;
        private readonly ReadOnlyMemory<byte> _singleElementValue;

        /// <summary>
        ///   Gets the encoded representation of this Relative Distinguished Name.
        /// </summary>
        /// <value>
        ///   The encoded representation of this Relative Distinguished Name.
        /// </value>
        public ReadOnlyMemory<byte> RawData { get; }

        internal X500RelativeDistinguishedName(ReadOnlyMemory<byte> rawData)
        {
            RawData = rawData;

            ReadOnlySpan<byte> rawDataSpan = rawData.Span;
            AsnValueReader outer = new AsnValueReader(rawDataSpan, AsnEncodingRules.DER);

            // Windows does not enforce the sort order on multi-value RDNs.
            AsnValueReader rdn = outer.ReadSetOf(skipSortOrderValidation: true);
            AsnValueReader typeAndValue = rdn.ReadSequence();

            Oid firstType = Oids.GetSharedOrNewOid(ref typeAndValue);
            ReadOnlySpan<byte> firstValue = typeAndValue.ReadEncodedValue();
            typeAndValue.ThrowIfNotEmpty();

            if (rdn.HasData)
            {
                do
                {
                    typeAndValue = rdn.ReadSequence();

                    // Check that the attribute type is a valid OID,
                    // if it's from the cache, even better (faster, lower alloc).
                    if (Oids.GetSharedOrNullOid(ref typeAndValue) is null)
                    {
                        typeAndValue.ReadObjectIdentifier();
                    }

                    typeAndValue.ReadEncodedValue();
                    typeAndValue.ThrowIfNotEmpty();
                }
                while (rdn.HasData);
            }
            else
            {
                _singleElementType = firstType;

                bool overlaps = rawDataSpan.Overlaps(firstValue, out int offset);
                Debug.Assert(overlaps, "AsnValueReader.ReadEncodedValue returns a slice of the source");
                Debug.Assert(offset > 0);

                _singleElementValue = rawData.Slice(offset, firstValue.Length);
            }
        }

        /// <summary>
        ///   Gets a value that indicates whether this Relative Distinguished Name is composed
        ///   of multiple attributes or only a single attribute.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Relative Distinguished Name is composed of multiple
        ///   attributes; <see langword="false"/> if it is composed of only a single attribute.
        /// </value>
        public bool HasMultipleElements => _singleElementType is null;

        /// <summary>
        ///   Gets the object identifier (OID) identifying the single attribute value for this
        ///   Relative Distinguished Name (RDN), when the RDN only contains one attribute.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   The Relative Distinguished Name has multiple attributes (<see cref="HasMultipleElements"/>
        ///   is <see langword="true" />).
        /// </exception>
        public Oid GetSingleElementType()
        {
            if (_singleElementType is null)
            {
                throw new InvalidOperationException(SR.Cryptography_X500_MultiValued);
            }

            return _singleElementType;
        }

        /// <summary>
        ///   Gets the textual representation of the value for the Relative Distinguished Name (RDN),
        ///   when the RDN only contains one attribute.
        /// </summary>
        /// <returns>
        ///   The decoded text representing the attribute value.
        ///   If the attribute value is an <c>OCTET STRING</c>, or other non-text data type,
        ///   this method returns <see langword="null" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The attribute is identified as a textual value, but the value did not successfully decode.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The Relative Distinguished Name has multiple attributes (<see cref="HasMultipleElements"/>
        ///   is <see langword="true" />).
        /// </exception>
        public string? GetSingleElementValue()
        {
            if (_singleElementValue.IsEmpty)
            {
                throw new InvalidOperationException(SR.Cryptography_X500_MultiValued);
            }

            // X.520 defines a few non-textual attributes, such as objectIdentifier (2.5.4.106),
            // which Windows renders textually as the bytes in hexadecimal preceded by an octothorpe,
            // e.g. #06032A0304 for an objectIdentifier attribute whose value is the OID 1.2.3.4
            //
            // For these, we return null, and then let the X500Name.Format code handle the hex fallback.

            try
            {
                AsnValueReader reader = new AsnValueReader(_singleElementValue.Span, AsnEncodingRules.DER);
                Asn1Tag tag = reader.PeekTag();

                if (tag.TagClass == TagClass.Universal)
                {
                    switch ((UniversalTagNumber)tag.TagValue)
                    {
                        case UniversalTagNumber.BMPString:
                        case UniversalTagNumber.UTF8String:
                        case UniversalTagNumber.IA5String:
                        case UniversalTagNumber.PrintableString:
                        case UniversalTagNumber.NumericString:
                        case UniversalTagNumber.T61String:
                            return reader.ReadAnyAsnString();
                    }
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            return null;
        }
    }
}
