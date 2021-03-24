// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Formats.Asn1
{
    /// <summary>
    ///   This type represents an ASN.1 tag, as described in ITU-T Recommendation X.680.
    /// </summary>
    // T-REC-X.690-201508 sec 8.1.2
    public readonly partial struct Asn1Tag : IEquatable<Asn1Tag>
    {
        private const byte ClassMask = 0b1100_0000;
        private const byte ConstructedMask = 0b0010_0000;
        private const byte ControlMask = ClassMask | ConstructedMask;
        private const byte TagNumberMask = 0b0001_1111;

        private readonly byte _controlFlags;

        /// <summary>
        ///   The tag class to which this tag belongs.
        /// </summary>
        public TagClass TagClass => (TagClass)(_controlFlags & ClassMask);

        /// <summary>
        ///   Indicates if the tag represents a constructed encoding (<see langword="true"/>), or
        ///   a primitive encoding (<see langword="false"/>).
        /// </summary>
        public bool IsConstructed => (_controlFlags & ConstructedMask) != 0;

        /// <summary>
        ///   The numeric value for this tag.
        /// </summary>
        /// <remarks>
        ///   If <see cref="TagClass"/> is <see cref="Asn1.TagClass.Universal"/>, this value can
        ///   be interpreted as a <see cref="UniversalTagNumber"/>.
        /// </remarks>
        public int TagValue { get; }

        private Asn1Tag(byte controlFlags, int tagValue)
        {
            _controlFlags = (byte)(controlFlags & ControlMask);
            TagValue = tagValue;
        }

        /// <summary>
        ///   Create an <see cref="Asn1Tag"/> for a tag from the UNIVERSAL class.
        /// </summary>
        /// <param name="universalTagNumber">
        ///   One of the enumeration values that specifies the semantic type for this tag.
        /// </param>
        /// <param name="isConstructed">
        ///   <see langword="true"/> for a constructed tag, <see langword="false"/> for a primitive tag.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="universalTagNumber"/> is not a known value.
        /// </exception>
        public Asn1Tag(UniversalTagNumber universalTagNumber, bool isConstructed = false)
            : this(isConstructed ? ConstructedMask : (byte)0, (int)universalTagNumber)
        {
            // T-REC-X.680-201508 sec 8.6 (Table 1)
            const UniversalTagNumber ReservedIndex = (UniversalTagNumber)15;

            if (universalTagNumber < UniversalTagNumber.EndOfContents ||
                universalTagNumber > UniversalTagNumber.RelativeObjectIdentifierIRI ||
                universalTagNumber == ReservedIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(universalTagNumber));
            }
        }

        /// <summary>
        ///   Create an <see cref="Asn1Tag"/> for a specified value within a specified tag class.
        /// </summary>
        /// <param name="tagClass">
        ///   The tag class for this tag.
        /// </param>
        /// <param name="tagValue">
        ///   The numeric value for this tag.
        /// </param>
        /// <param name="isConstructed">
        ///   <see langword="true"/> for a constructed tag, <see langword="false"/> for a primitive tag.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="tagClass"/> is not a known value.
        ///
        ///   -or-
        ///
        ///   <paramref name="tagValue" /> is negative.
        /// </exception>
        /// <remarks>
        ///   This constructor allows for the creation undefined UNIVERSAL class tags.
        /// </remarks>
        public Asn1Tag(TagClass tagClass, int tagValue, bool isConstructed = false)
            : this((byte)((byte)tagClass | (isConstructed ? ConstructedMask : 0)), tagValue)
        {
            switch (tagClass)
            {
                case TagClass.Universal:
                case TagClass.ContextSpecific:
                case TagClass.Application:
                case TagClass.Private:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tagClass));
            }

            if (tagValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tagValue));
            }
        }

        /// <summary>
        ///   Produces a tag with the same <see cref="TagClass"/> and
        ///   <see cref="TagValue"/> values, but whose <see cref="IsConstructed"/> is <see langword="true"/>.
        /// </summary>
        /// <returns>
        ///   A tag with the same <see cref="TagClass"/> and <see cref="TagValue"/>
        ///   values, but whose <see cref="IsConstructed"/> is <see langword="true"/>.
        /// </returns>
        public Asn1Tag AsConstructed()
        {
            return new Asn1Tag((byte)(_controlFlags | ConstructedMask), TagValue);
        }

        /// <summary>
        ///   Produces a tag with the same <see cref="TagClass"/> and
        ///   <see cref="TagValue"/> values, but whose <see cref="IsConstructed"/> is <see langword="false"/>.
        /// </summary>
        /// <returns>
        ///   A tag with the same <see cref="TagClass"/> and <see cref="TagValue"/>
        ///   values, but whose <see cref="IsConstructed"/> is <see langword="false"/>.
        /// </returns>
        public Asn1Tag AsPrimitive()
        {
            return new Asn1Tag((byte)(_controlFlags & ~ConstructedMask), TagValue);
        }

        /// <summary>
        ///   Attempts to read a BER-encoded tag which starts at <paramref name="source"/>.
        /// </summary>
        /// <param name="source">
        ///   The read only byte sequence whose beginning is a BER-encoded tag.
        /// </param>
        /// <param name="tag">
        ///   The decoded tag.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, contains the number of bytes that contributed
        ///   to the encoded tag, 0 on failure. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if a tag was correctly decoded; otherwise, <see langword="false" />.
        /// </returns>
        public static bool TryDecode(ReadOnlySpan<byte> source, out Asn1Tag tag, out int bytesConsumed)
        {
            tag = default(Asn1Tag);
            bytesConsumed = 0;

            if (source.IsEmpty)
            {
                return false;
            }

            byte first = source[bytesConsumed];
            bytesConsumed++;
            uint tagValue = (uint)(first & TagNumberMask);

            if (tagValue == TagNumberMask)
            {
                // Multi-byte encoding
                // T-REC-X.690-201508 sec 8.1.2.4
                const byte ContinuationFlag = 0x80;
                const byte ValueMask = ContinuationFlag - 1;

                tagValue = 0;
                byte current;

                do
                {
                    if (source.Length <= bytesConsumed)
                    {
                        bytesConsumed = 0;
                        return false;
                    }

                    current = source[bytesConsumed];
                    byte currentValue = (byte)(current & ValueMask);
                    bytesConsumed++;

                    // If TooBigToShift is shifted left 7, the content bit shifts out.
                    // So any value greater than or equal to this cannot be shifted without loss.
                    const int TooBigToShift = 0b00000010_00000000_00000000_00000000;

                    if (tagValue >= TooBigToShift)
                    {
                        bytesConsumed = 0;
                        return false;
                    }

                    tagValue <<= 7;
                    tagValue |= currentValue;

                    // The first byte cannot have the value 0 (T-REC-X.690-201508 sec 8.1.2.4.2.c)
                    if (tagValue == 0)
                    {
                        bytesConsumed = 0;
                        return false;
                    }
                }
                while ((current & ContinuationFlag) == ContinuationFlag);

                // This encoding is only valid for tag values greater than 30.
                // (T-REC-X.690-201508 sec 8.1.2.3, 8.1.2.4)
                if (tagValue <= 30)
                {
                    bytesConsumed = 0;
                    return false;
                }

                // There's not really any ambiguity, but prevent negative numbers from showing up.
                if (tagValue > int.MaxValue)
                {
                    bytesConsumed = 0;
                    return false;
                }
            }

            Debug.Assert(bytesConsumed > 0);
            tag = new Asn1Tag(first, (int)tagValue);
            return true;
        }

        /// <summary>
        ///   Reads a BER-encoded tag which starts at <paramref name="source"/>.
        /// </summary>
        /// <param name="source">
        ///   The read only byte sequence whose beginning is a BER-encoded tag.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, contains the number of bytes that contributed
        ///   to the encoded tag. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   The decoded tag.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   The provided data does not decode to a tag.
        /// </exception>
        public static Asn1Tag Decode(ReadOnlySpan<byte> source, out int bytesConsumed)
        {
            if (TryDecode(source, out Asn1Tag tag, out bytesConsumed))
            {
                return tag;
            }

            throw new AsnContentException(SR.ContentException_InvalidTag);
        }

        /// <summary>
        ///   Reports the number of bytes required for the BER-encoding of this tag.
        /// </summary>
        /// <returns>
        ///   The number of bytes required for the BER-encoding of this tag.
        /// </returns>
        /// <seealso cref="TryEncode(Span{byte},out int)"/>
        public int CalculateEncodedSize()
        {
            const int SevenBits = 0b0111_1111;
            const int FourteenBits = 0b0011_1111_1111_1111;
            const int TwentyOneBits = 0b0001_1111_1111_1111_1111_1111;
            const int TwentyEightBits = 0b0000_1111_1111_1111_1111_1111_1111_1111;

            if (TagValue < TagNumberMask)
                return 1;
            if (TagValue <= SevenBits)
                return 2;
            if (TagValue <= FourteenBits)
                return 3;
            if (TagValue <= TwentyOneBits)
                return 4;
            if (TagValue <= TwentyEightBits)
                return 5;

            return 6;
        }

        /// <summary>
        ///   Attempts to write the BER-encoded form of this tag to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">
        ///   The start of where the encoded tag should be written.
        /// </param>
        /// <param name="bytesWritten">
        ///   Receives the value from <see cref="CalculateEncodedSize"/> on success, 0 on failure.
        /// </param>
        /// <returns>
        ///   <see langword="false"/> if <paramref name="destination"/>.<see cref="Span{T}.Length"/> &lt;
        ///   <see cref="CalculateEncodedSize"/>(), <see langword="true"/> otherwise.
        /// </returns>
        public bool TryEncode(Span<byte> destination, out int bytesWritten)
        {
            int spaceRequired = CalculateEncodedSize();

            if (destination.Length < spaceRequired)
            {
                bytesWritten = 0;
                return false;
            }

            if (spaceRequired == 1)
            {
                byte value = (byte)(_controlFlags | TagValue);
                destination[0] = value;
                bytesWritten = 1;
                return true;
            }

            byte firstByte = (byte)(_controlFlags | TagNumberMask);
            destination[0] = firstByte;

            int remaining = TagValue;
            int idx = spaceRequired - 1;

            while (remaining > 0)
            {
                int segment = remaining & 0x7F;

                // The last byte doesn't get the marker, which we write first.
                if (remaining != TagValue)
                {
                    segment |= 0x80;
                }

                Debug.Assert(segment <= byte.MaxValue);
                destination[idx] = (byte)segment;
                remaining >>= 7;
                idx--;
            }

            Debug.Assert(idx == 0);
            bytesWritten = spaceRequired;
            return true;
        }

        /// <summary>
        ///   Writes the BER-encoded form of this tag to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">
        ///   The start of where the encoded tag should be written.
        /// </param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        /// <seealso cref="CalculateEncodedSize"/>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/>.<see cref="Span{T}.Length"/> &lt; <see cref="CalculateEncodedSize"/>.
        /// </exception>
        public int Encode(Span<byte> destination)
        {
            if (TryEncode(destination, out int bytesWritten))
            {
                return bytesWritten;
            }

            throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
        }

        /// <summary>
        ///   Tests if <paramref name="other"/> has the same encoding as this tag.
        /// </summary>
        /// <param name="other">
        ///   Tag to test for equality.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="other"/> has the same values for
        ///   <see cref="TagClass"/>, <see cref="TagValue"/>, and <see cref="IsConstructed"/>;
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public bool Equals(Asn1Tag other)
        {
            return _controlFlags == other._controlFlags && TagValue == other.TagValue;
        }

        /// <summary>
        ///   Tests if <paramref name="obj"/> is an <see cref="Asn1Tag"/> with the same
        ///   encoding as this tag.
        /// </summary>
        /// <param name="obj">Object to test for value equality</param>
        /// <returns>
        ///   <see langword="false"/> if <paramref name="obj"/> is not an <see cref="Asn1Tag"/>,
        ///   <see cref="Equals(Asn1Tag)"/> otherwise.
        /// </returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Asn1Tag tag && Equals(tag);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        ///   A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode()
        {
            // Most TagValue values will be in the 0-30 range,
            // the GetHashCode value only has collisions when TagValue is
            // between 2^29 and uint.MaxValue
            return (_controlFlags << 24) ^ TagValue;
        }

        /// <summary>
        ///   Tests if two <see cref="Asn1Tag"/> values have the same BER encoding.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> have the same
        ///   BER encoding, <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator ==(Asn1Tag left, Asn1Tag right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///   Tests if two <see cref="Asn1Tag"/> values have a different BER encoding.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> have a different
        ///   BER encoding, <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator !=(Asn1Tag left, Asn1Tag right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        ///   Tests if <paramref name="other"/> has the same <see cref="TagClass"/> and <see cref="TagValue"/>
        ///   values as this tag, and does not compare <see cref="IsConstructed"/>.
        /// </summary>
        /// <param name="other">Tag to test for concept equality.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="other"/> has the same <see cref="TagClass"/> and <see cref="TagValue"/>
        ///   as this tag, <see langword="false"/> otherwise.
        /// </returns>
        public bool HasSameClassAndValue(Asn1Tag other)
        {
            return TagValue == other.TagValue && TagClass == other.TagClass;
        }

        /// <summary>
        ///   Provides a text representation of this tag suitable for debugging.
        /// </summary>
        /// <returns>
        ///   A text representation of this tag suitable for debugging.
        /// </returns>
        public override string ToString()
        {
            const string ConstructedPrefix = "Constructed ";
            string classAndValue;

            if (TagClass == TagClass.Universal)
            {
                classAndValue = ((UniversalTagNumber)TagValue).ToString();
            }
            else
            {
                classAndValue = TagClass + "-" + TagValue;
            }

            if (IsConstructed)
            {
                return ConstructedPrefix + classAndValue;
            }

            return classAndValue;
        }
    }
}
