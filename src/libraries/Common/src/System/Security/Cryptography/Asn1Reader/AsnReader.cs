// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Diagnostics;

namespace System.Security.Cryptography.Asn1
{
    /// <summary>
    ///   A stateful, forward-only reader for BER-, CER-, or DER-encoded ASN.1 data.
    /// </summary>
    internal partial class AsnReader
    {
        // T-REC-X.690-201508 sec 9.2
        internal const int MaxCERSegmentSize = 1000;

        // T-REC-X.690-201508 sec 8.1.5 says only 0000 is legal.
        private const int EndOfContentsEncodedLength = 2;

        private ReadOnlyMemory<byte> _data;

        /// <summary>
        ///   The <see cref="AsnEncodingRules"/> in use by this reader.
        /// </summary>
        public AsnEncodingRules RuleSet { get; }

        /// <summary>
        ///   An indication of whether or not the reader has remaining data available to process.
        /// </summary>
        public bool HasData => !_data.IsEmpty;

        /// <summary>
        ///   Construct an <see cref="AsnReader"/> over <paramref name="data"/> with a given ruleset.
        /// </summary>
        /// <param name="data">The data to read.</param>
        /// <param name="ruleSet">The encoding constraints for the reader.</param>
        /// <remarks>
        ///   This constructor does not evaluate <paramref name="data"/> for correctness,
        ///   any correctness checks are done as part of member methods.
        ///
        ///   This constructor does not copy <paramref name="data"/>. The caller is responsible for
        ///   ensuring that the values do not change until the reader is finished.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        public AsnReader(ReadOnlyMemory<byte> data, AsnEncodingRules ruleSet)
        {
            CheckEncodingRules(ruleSet);

            _data = data;
            RuleSet = ruleSet;
        }

        /// <summary>
        ///   Throws a standardized <see cref="CryptographicException"/> if the reader has remaining
        ///   data, performs no function if <see cref="HasData"/> returns <c>false</c>.
        /// </summary>
        /// <remarks>
        ///   This method provides a standardized target and standardized exception for reading a
        ///   "closed" structure, such as the nested content for an explicitly tagged value.
        /// </remarks>
        public void ThrowIfNotEmpty()
        {
            if (HasData)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        /// <summary>
        ///   Read the encoded tag at the next data position, without advancing the reader.
        /// </summary>
        /// <returns>
        ///   The decoded <see cref="Asn1Tag"/> value.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   a tag could not be decoded at the reader's current position.
        /// </exception>
        public Asn1Tag PeekTag()
        {
            if (Asn1Tag.TryDecode(_data.Span, out Asn1Tag tag, out int bytesRead))
            {
                return tag;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        /// <summary>
        ///   Get a <see cref="ReadOnlyMemory{T}"/> view of the next encoded value without
        ///   advancing the reader. For indefinite length encodings this includes the
        ///   End of Contents marker.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyMemory{T}"/> view of the next encoded value.</returns>
        /// <exception cref="CryptographicException">
        ///   The reader is positioned at a point where the tag or length is invalid
        ///   under the current encoding rules.
        /// </exception>
        /// <seealso cref="PeekContentBytes"/>
        /// <seealso cref="ReadEncodedValue"/>
        public ReadOnlyMemory<byte> PeekEncodedValue()
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int bytesRead);

            if (length == null)
            {
                int contentsLength = SeekEndOfContents(_data.Slice(bytesRead));
                return Slice(_data, 0, bytesRead + contentsLength + EndOfContentsEncodedLength);
            }

            return Slice(_data, 0, bytesRead + length.Value);
        }

        /// <summary>
        ///   Get a <see cref="ReadOnlyMemory{T}"/> view of the content octets (bytes) of the
        ///   next encoded value without advancing the reader.
        /// </summary>
        /// <returns>
        ///   A <see cref="ReadOnlyMemory{T}"/> view of the contents octets of the next encoded value.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The reader is positioned at a point where the tag or length is invalid
        ///   under the current encoding rules.
        /// </exception>
        /// <seealso cref="PeekEncodedValue"/>
        public ReadOnlyMemory<byte> PeekContentBytes()
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int bytesRead);

            if (length == null)
            {
                return Slice(_data, bytesRead, SeekEndOfContents(_data.Slice(bytesRead)));
            }

            return Slice(_data, bytesRead, length.Value);
        }

        /// <summary>
        ///   Get a <see cref="ReadOnlyMemory{T}"/> view of the next encoded value,
        ///   and advance the reader past it. For an indefinite length encoding this includes
        ///   the End of Contents marker.
        /// </summary>
        /// <returns>A <see cref="ReadOnlyMemory{T}"/> view of the next encoded value.</returns>
        /// <seealso cref="PeekEncodedValue"/>
        public ReadOnlyMemory<byte> ReadEncodedValue()
        {
            ReadOnlyMemory<byte> encodedValue = PeekEncodedValue();
            _data = _data.Slice(encodedValue.Length);
            return encodedValue;
        }

        private static bool TryReadLength(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int? length,
            out int bytesRead)
        {
            length = null;
            bytesRead = 0;

            CheckEncodingRules(ruleSet);

            if (source.IsEmpty)
            {
                return false;
            }

            // T-REC-X.690-201508 sec 8.1.3

            byte lengthOrLengthLength = source[bytesRead];
            bytesRead++;
            const byte MultiByteMarker = 0x80;

            // 0x00-0x7F are direct length values.
            // 0x80 is BER/CER indefinite length.
            // 0x81-0xFE says that the length takes the next 1-126 bytes.
            // 0xFF is forbidden.
            if (lengthOrLengthLength == MultiByteMarker)
            {
                // T-REC-X.690-201508 sec 10.1 (DER: Length forms)
                if (ruleSet == AsnEncodingRules.DER)
                {
                    bytesRead = 0;
                    return false;
                }

                // Null length == indefinite.
                return true;
            }

            if (lengthOrLengthLength < MultiByteMarker)
            {
                length = lengthOrLengthLength;
                return true;
            }

            if (lengthOrLengthLength == 0xFF)
            {
                bytesRead = 0;
                return false;
            }

            byte lengthLength = (byte)(lengthOrLengthLength & ~MultiByteMarker);

            // +1 for lengthOrLengthLength
            if (lengthLength + 1 > source.Length)
            {
                bytesRead = 0;
                return false;
            }

            // T-REC-X.690-201508 sec 9.1 (CER: Length forms)
            // T-REC-X.690-201508 sec 10.1 (DER: Length forms)
            bool minimalRepresentation =
                ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER;

            // The ITU-T specifications tecnically allow lengths up to ((2^128) - 1), but
            // since Span's length is a signed Int32 we're limited to identifying memory
            // that is within ((2^31) - 1) bytes of the tag start.
            if (minimalRepresentation && lengthLength > sizeof(int))
            {
                bytesRead = 0;
                return false;
            }

            uint parsedLength = 0;

            for (int i = 0; i < lengthLength; i++)
            {
                byte current = source[bytesRead];
                bytesRead++;

                if (parsedLength == 0)
                {
                    if (minimalRepresentation && current == 0)
                    {
                        bytesRead = 0;
                        return false;
                    }

                    if (!minimalRepresentation && current != 0)
                    {
                        // Under BER rules we could have had padding zeros, so
                        // once the first data bits come in check that we fit within
                        // sizeof(int) due to Span bounds.

                        if (lengthLength - i > sizeof(int))
                        {
                            bytesRead = 0;
                            return false;
                        }
                    }
                }

                parsedLength <<= 8;
                parsedLength |= current;
            }

            // This value cannot be represented as a Span length.
            if (parsedLength > int.MaxValue)
            {
                bytesRead = 0;
                return false;
            }

            if (minimalRepresentation && parsedLength < MultiByteMarker)
            {
                bytesRead = 0;
                return false;
            }

            Debug.Assert(bytesRead > 0);
            length = (int)parsedLength;
            return true;
        }

        internal Asn1Tag ReadTagAndLength(out int? contentsLength, out int bytesRead)
        {
            if (Asn1Tag.TryDecode(_data.Span, out Asn1Tag tag, out int tagBytesRead) &&
                TryReadLength(_data.Slice(tagBytesRead).Span, RuleSet, out int? length, out int lengthBytesRead))
            {
                int allBytesRead = tagBytesRead + lengthBytesRead;

                if (tag.IsConstructed)
                {
                    // T-REC-X.690-201508 sec 9.1 (CER: Length forms) says constructed is always indefinite.
                    if (RuleSet == AsnEncodingRules.CER && length != null)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }
                else if (length == null)
                {
                    // T-REC-X.690-201508 sec 8.1.3.2 says primitive encodings must use a definite form.
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                bytesRead = allBytesRead;
                contentsLength = length;
                return tag;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private static void ValidateEndOfContents(Asn1Tag tag, int? length, int headerLength)
        {
            // T-REC-X.690-201508 sec 8.1.5 excludes the BER 8100 length form for 0.
            if (tag.IsConstructed || length != 0 || headerLength != EndOfContentsEncodedLength)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        /// <summary>
        /// Get the number of bytes between the start of <paramref name="source" /> and
        /// the End-of-Contents marker
        /// </summary>
        private int SeekEndOfContents(ReadOnlyMemory<byte> source)
        {
            ReadOnlyMemory<byte> cur = source;
            int totalLen = 0;

            AsnReader tmpReader = new AsnReader(cur, RuleSet);
            // Our reader is bounded by int.MaxValue.
            // The most aggressive data input would be a one-byte tag followed by
            // indefinite length "ad infinitum", which would be half the input.
            // So the depth marker can never overflow the signed integer space.
            int depth = 1;

            while (tmpReader.HasData)
            {
                Asn1Tag tag = tmpReader.ReadTagAndLength(out int? length, out int bytesRead);

                if (tag == Asn1Tag.EndOfContents)
                {
                    ValidateEndOfContents(tag, length, bytesRead);

                    depth--;

                    if (depth == 0)
                    {
                        // T-REC-X.690-201508 sec 8.1.1.1 / 8.1.1.3 indicate that the
                        // End-of-Contents octets are "after" the contents octets, not
                        // "at the end" of them, so we don't include these bytes in the
                        // accumulator.
                        return totalLen;
                    }
                }

                // We found another indefinite length, that means we need to find another
                // EndOfContents marker to balance it out.
                if (length == null)
                {
                    depth++;
                    tmpReader._data = tmpReader._data.Slice(bytesRead);
                    totalLen += bytesRead;
                }
                else
                {
                    // This will throw a CryptographicException if the length exceeds our bounds.
                    ReadOnlyMemory<byte> tlv = Slice(tmpReader._data, 0, bytesRead + length.Value);
                    
                    // No exception? Then slice the data and continue.
                    tmpReader._data = tmpReader._data.Slice(tlv.Length);
                    totalLen += tlv.Length;
                }
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private static int ParseNonNegativeIntAndSlice(ref ReadOnlySpan<byte> data, int bytesToRead)
        {
            int value = ParseNonNegativeInt(Slice(data, 0, bytesToRead));
            data = data.Slice(bytesToRead);

            return value;
        }

        private static int ParseNonNegativeInt(ReadOnlySpan<byte> data)
        {
            if (Utf8Parser.TryParse(data, out uint value, out int consumed) &&
                value <= int.MaxValue &&
                consumed == data.Length)
            {
                return (int)value;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private static ReadOnlySpan<byte> SliceAtMost(ReadOnlySpan<byte> source, int longestPermitted)
        {
            int len = Math.Min(longestPermitted, source.Length);
            return source.Slice(0, len);
        }

        private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> source, int offset, int length)
        {
            Debug.Assert(offset >= 0);

            if (length < 0 || source.Length - offset < length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return source.Slice(offset, length);
        }

        private static ReadOnlyMemory<byte> Slice(ReadOnlyMemory<byte> source, int offset, int? length)
        {
            Debug.Assert(offset >= 0);

            if (length == null)
            {
                return source.Slice(offset);
            }

            int lengthVal = length.Value;

            if (lengthVal < 0 || source.Length - offset < lengthVal)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return source.Slice(offset, lengthVal);
        }

        private static void CheckEncodingRules(AsnEncodingRules ruleSet)
        {
            if (ruleSet != AsnEncodingRules.BER &&
                ruleSet != AsnEncodingRules.CER &&
                ruleSet != AsnEncodingRules.DER)
            {
                throw new ArgumentOutOfRangeException(nameof(ruleSet));
            }
        }

        private static void CheckExpectedTag(Asn1Tag tag, Asn1Tag expectedTag, UniversalTagNumber tagNumber)
        {
            if (expectedTag.TagClass == TagClass.Universal && expectedTag.TagValue != (int)tagNumber)
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_UniversalValueIsFixed,
                    nameof(expectedTag));
            }

            if (expectedTag.TagClass != tag.TagClass || expectedTag.TagValue != tag.TagValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }
    }
}
