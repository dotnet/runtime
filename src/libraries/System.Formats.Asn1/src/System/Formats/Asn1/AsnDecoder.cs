// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;

namespace System.Formats.Asn1
{
    /// <summary>
    ///   Provides stateless methods for decoding BER-, CER-, or DER-encoded ASN.1 data.
    /// </summary>
    public static partial class AsnDecoder
    {
        // T-REC-X.690-201508 sec 9.2
        internal const int MaxCERSegmentSize = 1000;

        // T-REC-X.690-201508 sec 8.1.5 says only 0000 is legal.
        internal const int EndOfContentsEncodedLength = 2;

        /// <summary>
        ///   Attempts locate the contents range for the encoded value at the beginning of the
        ///   <paramref name="source"/> buffer using the specified encoding rules.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="tag">
        ///   When this method returns, the tag identifying the content.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="contentOffset">
        ///   When this method returns, the offset of the content payload relative to the start of
        ///   <paramref name="source"/>.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="contentLength">
        ///   When this method returns, the number of bytes in the content payload (which may be 0).
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="source"/> represents a valid structural
        ///   encoding for the specified encoding rules; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     This method performs very little validation on the contents.
        ///     If the encoded value uses a definite length, the contents are not inspected at all.
        ///     If the encoded value uses an indefinite length, the contents are only inspected
        ///     as necessary to determine the location of the relevant end-of-contents marker.
        ///   </para>
        ///   <para>
        ///     When the encoded value uses an indefinite length, the <paramref name="bytesConsumed"/>
        ///     value will be larger than the sum of <paramref name="contentOffset"/> and
        ///     <paramref name="contentLength"/> to account for the end-of-contents marker.
        ///   </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        public static bool TryReadEncodedValue(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out Asn1Tag tag,
            out int contentOffset,
            out int contentLength,
            out int bytesConsumed)
        {
            CheckEncodingRules(ruleSet);

            if (Asn1Tag.TryDecode(source, out Asn1Tag localTag, out int tagLength) &&
                TryReadLength(source.Slice(tagLength), ruleSet, out int? encodedLength, out int lengthLength))
            {
                int headerLength = tagLength + lengthLength;

                LengthValidity validity = ValidateLength(
                    source.Slice(headerLength),
                    ruleSet,
                    localTag,
                    encodedLength,
                    out int len,
                    out int consumed);

                if (validity == LengthValidity.Valid)
                {
                    tag = localTag;
                    contentOffset = headerLength;
                    contentLength = len;
                    bytesConsumed = headerLength + consumed;
                    return true;
                }
            }

            tag = default;
            contentOffset = contentLength = bytesConsumed = 0;
            return false;
        }

        /// <summary>
        ///   Locates the contents range for the encoded value at the beginning of the
        ///   <paramref name="source"/> buffer using the specified encoding rules.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="contentOffset">
        ///   When this method returns, the offset of the content payload relative to the start of
        ///   <paramref name="source"/>.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="contentLength">
        ///   When this method returns, the number of bytes in the content payload (which may be 0).
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   The tag identifying the content.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     This method performs very little validation on the contents.
        ///     If the encoded value uses a definite length, the contents are not inspected at all.
        ///     If the encoded value uses an indefinite length, the contents are only inspected
        ///     as necessary to determine the location of the relevant end-of-contents marker.
        ///   </para>
        ///   <para>
        ///     When the encoded value uses an indefinite length, the <paramref name="bytesConsumed"/>
        ///     value will be larger than the sum of <paramref name="contentOffset"/> and
        ///     <paramref name="contentLength"/> to account for the end-of-contents marker.
        ///   </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   <paramref name="source"/> does not represent a value encoded under the specified
        ///   encoding rules.
        /// </exception>
        public static Asn1Tag ReadEncodedValue(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int contentOffset,
            out int contentLength,
            out int bytesConsumed)
        {
            CheckEncodingRules(ruleSet);

            Asn1Tag tag = Asn1Tag.Decode(source, out int tagLength);
            int? encodedLength = ReadLength(source.Slice(tagLength), ruleSet, out int lengthLength);
            int headerLength = tagLength + lengthLength;

            LengthValidity validity = ValidateLength(
                source.Slice(headerLength),
                ruleSet,
                tag,
                encodedLength,
                out int len,
                out int consumed);

            if (validity == LengthValidity.Valid)
            {
                contentOffset = headerLength;
                contentLength = len;
                bytesConsumed = headerLength + consumed;
                return tag;
            }

            throw GetValidityException(validity);
        }

        private static ReadOnlySpan<byte> GetPrimitiveContentSpan(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out int bytesConsumed)
        {
            CheckEncodingRules(ruleSet);

            Asn1Tag localTag = Asn1Tag.Decode(source, out int tagLength);
            int? encodedLength = ReadLength(source.Slice(tagLength), ruleSet, out int lengthLength);
            int headerLength = tagLength + lengthLength;

            // Get caller(-of-my-caller) errors out of the way, first.
            CheckExpectedTag(localTag, expectedTag, tagNumber);

            // T-REC-X.690-201508 sec 8.1.3.2 says primitive encodings must use a definite form,
            // and the caller says they only want primitive values.
            if (localTag.IsConstructed)
            {
                throw new AsnContentException(
                    SR.Format(SR.ContentException_PrimitiveEncodingRequired, tagNumber));
            }

            if (encodedLength == null)
            {
                throw new AsnContentException();
            }

            ReadOnlySpan<byte> ret = Slice(source, headerLength, encodedLength.Value);
            bytesConsumed = headerLength + ret.Length;
            return ret;
        }

        private static bool TryReadLength(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int? length,
            out int bytesRead)
        {
            return DecodeLength(source, ruleSet, out length, out bytesRead) == LengthDecodeStatus.Success;
        }

        private static int? ReadLength(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed)
        {
            LengthDecodeStatus status = DecodeLength(source, ruleSet, out int? length, out bytesConsumed);

            switch (status)
            {
                case LengthDecodeStatus.Success:
                    return length;
                case LengthDecodeStatus.LengthTooBig:
                    throw new AsnContentException(SR.ContentException_LengthTooBig);
                case LengthDecodeStatus.LaxEncodingProhibited:
                case LengthDecodeStatus.DerIndefinite:
                    throw new AsnContentException(SR.ContentException_LengthRuleSetConstraint);
                case LengthDecodeStatus.NeedMoreData:
                case LengthDecodeStatus.ReservedValue:
                    throw new AsnContentException();
                default:
                    Debug.Fail($"No handler is present for status {status}.");
                    goto case LengthDecodeStatus.NeedMoreData;
            }
        }

        private static LengthDecodeStatus DecodeLength(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int? length,
            out int bytesRead)
        {
            length = null;
            bytesRead = 0;

            AssertEncodingRules(ruleSet);

            if (source.IsEmpty)
            {
                return LengthDecodeStatus.NeedMoreData;
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
                    return LengthDecodeStatus.DerIndefinite;
                }

                // Null length == indefinite.
                return LengthDecodeStatus.Success;
            }

            if (lengthOrLengthLength < MultiByteMarker)
            {
                length = lengthOrLengthLength;
                return LengthDecodeStatus.Success;
            }

            if (lengthOrLengthLength == 0xFF)
            {
                bytesRead = 0;
                return LengthDecodeStatus.ReservedValue;
            }

            byte lengthLength = (byte)(lengthOrLengthLength & ~MultiByteMarker);

            // +1 for lengthOrLengthLength
            if (lengthLength + 1 > source.Length)
            {
                bytesRead = 0;
                return LengthDecodeStatus.NeedMoreData;
            }

            // T-REC-X.690-201508 sec 9.1 (CER: Length forms)
            // T-REC-X.690-201508 sec 10.1 (DER: Length forms)
            bool minimalRepresentation =
                ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER;

            // The ITU-T specifications technically allow lengths up to ((2^128) - 1), but
            // since Span's length is a signed Int32 we're limited to identifying memory
            // that is within ((2^31) - 1) bytes of the tag start.
            if (minimalRepresentation && lengthLength > sizeof(int))
            {
                bytesRead = 0;
                return LengthDecodeStatus.LengthTooBig;
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
                        return LengthDecodeStatus.LaxEncodingProhibited;
                    }

                    if (!minimalRepresentation && current != 0)
                    {
                        // Under BER rules we could have had padding zeros, so
                        // once the first data bits come in check that we fit within
                        // sizeof(int) due to Span bounds.

                        if (lengthLength - i > sizeof(int))
                        {
                            bytesRead = 0;
                            return LengthDecodeStatus.LengthTooBig;
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
                return LengthDecodeStatus.LengthTooBig;
            }

            if (minimalRepresentation && parsedLength < MultiByteMarker)
            {
                bytesRead = 0;
                return LengthDecodeStatus.LaxEncodingProhibited;
            }

            Debug.Assert(bytesRead > 0);
            length = (int)parsedLength;
            return LengthDecodeStatus.Success;
        }

        private static Asn1Tag ReadTagAndLength(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int? contentsLength,
            out int bytesRead)
        {
            Asn1Tag tag = Asn1Tag.Decode(source, out int tagBytesRead);
            int? length = ReadLength(source.Slice(tagBytesRead), ruleSet, out int lengthBytesRead);

            int allBytesRead = tagBytesRead + lengthBytesRead;

            if (tag.IsConstructed)
            {
                // T-REC-X.690-201508 sec 9.1 (CER: Length forms) says constructed is always indefinite.
                if (ruleSet == AsnEncodingRules.CER && length != null)
                {
                    throw GetValidityException(LengthValidity.CerRequiresIndefinite);
                }
            }
            else if (length == null)
            {
                // T-REC-X.690-201508 sec 8.1.3.2 says primitive encodings must use a definite form.
                throw GetValidityException(LengthValidity.PrimitiveEncodingRequiresDefinite);
            }

            bytesRead = allBytesRead;
            contentsLength = length;
            return tag;
        }

        private static void ValidateEndOfContents(Asn1Tag tag, int? length, int headerLength)
        {
            // T-REC-X.690-201508 sec 8.1.5 excludes the BER 8100 length form for 0.
            if (tag.IsConstructed || length != 0 || headerLength != EndOfContentsEncodedLength)
            {
                throw new AsnContentException();
            }
        }

        private static LengthValidity ValidateLength(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag localTag,
            int? encodedLength,
            out int actualLength,
            out int bytesConsumed)
        {
            if (localTag.IsConstructed)
            {
                // T-REC-X.690-201508 sec 9.1 (CER: Length forms) says constructed is always indefinite.
                if (ruleSet == AsnEncodingRules.CER && encodedLength != null)
                {
                    actualLength = bytesConsumed = 0;
                    return LengthValidity.CerRequiresIndefinite;
                }
            }
            else if (encodedLength == null)
            {
                // T-REC-X.690-201508 sec 8.1.3.2 says primitive encodings must use a definite form.
                actualLength = bytesConsumed = 0;
                return LengthValidity.PrimitiveEncodingRequiresDefinite;
            }

            if (encodedLength != null)
            {
                int len = encodedLength.Value;
                int totalLength = len;

                if (totalLength > source.Length)
                {
                    actualLength = bytesConsumed = 0;
                    return LengthValidity.LengthExceedsInput;
                }

                actualLength = len;
                bytesConsumed = len;
                return LengthValidity.Valid;
            }

            // Assign actualLength first, so no assignments leak if SeekEndOfContents
            // throws an exception.
            actualLength = SeekEndOfContents(source, ruleSet);
            bytesConsumed = actualLength + EndOfContentsEncodedLength;
            return LengthValidity.Valid;
        }

        private static AsnContentException GetValidityException(LengthValidity validity)
        {
            Debug.Assert(validity != LengthValidity.Valid);

            switch (validity)
            {
                case LengthValidity.CerRequiresIndefinite:
                    return new AsnContentException(SR.ContentException_CerRequiresIndefiniteLength);
                case LengthValidity.LengthExceedsInput:
                    return new AsnContentException(SR.ContentException_LengthExceedsPayload);
                case LengthValidity.PrimitiveEncodingRequiresDefinite:
                    return new AsnContentException();
                default:
                    Debug.Fail($"No handler for validity {validity}.");
                    goto case LengthValidity.PrimitiveEncodingRequiresDefinite;
            }
        }

        private static int GetPrimitiveIntegerSize(Type primitiveType)
        {
            if (primitiveType == typeof(byte) || primitiveType == typeof(sbyte))
                return 1;
            if (primitiveType == typeof(short) || primitiveType == typeof(ushort))
                return 2;
            if (primitiveType == typeof(int) || primitiveType == typeof(uint))
                return 4;
            if (primitiveType == typeof(long) || primitiveType == typeof(ulong))
                return 8;
            return 0;
        }

        /// <summary>
        /// Get the number of bytes between the start of <paramref name="source" /> and
        /// the End-of-Contents marker
        /// </summary>
        private static int SeekEndOfContents(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet)
        {
            ReadOnlySpan<byte> cur = source;
            int totalLen = 0;

            // Our reader is bounded by int.MaxValue.
            // The most aggressive data input would be a one-byte tag followed by
            // indefinite length "ad infinitum", which would be half the input.
            // So the depth marker can never overflow the signed integer space.
            int depth = 1;

            while (!cur.IsEmpty)
            {
                Asn1Tag tag = ReadTagAndLength(cur, ruleSet, out int? length, out int bytesRead);

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
                    cur = cur.Slice(bytesRead);
                    totalLen += bytesRead;
                }
                else
                {
                    // This will throw an AsnContentException if the length exceeds our bounds.
                    ReadOnlySpan<byte> tlv = Slice(cur, 0, bytesRead + length.Value);

                    // No exception? Then slice the data and continue.
                    cur = cur.Slice(tlv.Length);
                    totalLen += tlv.Length;
                }
            }

            throw new AsnContentException();
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

            throw new AsnContentException();
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
                throw new AsnContentException(SR.ContentException_LengthExceedsPayload);
            }

            return source.Slice(offset, length);
        }

        private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> source, int offset, int? length)
        {
            Debug.Assert(offset >= 0);

            if (length == null)
            {
                return source.Slice(offset);
            }

            int lengthVal = length.Value;

            if (lengthVal < 0 || source.Length - offset < lengthVal)
            {
                throw new AsnContentException(SR.ContentException_LengthExceedsPayload);
            }

            return source.Slice(offset, lengthVal);
        }

        internal static ReadOnlyMemory<byte> Slice(ReadOnlyMemory<byte> bigger, ReadOnlySpan<byte> smaller)
        {
            if (smaller.IsEmpty)
            {
                return default;
            }

            if (bigger.Span.Overlaps(smaller, out int offset))
            {
                return bigger.Slice(offset, smaller.Length);
            }

            Debug.Fail("AsnReader asked for a matching slice from a non-overlapping input");
            throw new AsnContentException();
        }

        [Conditional("DEBUG")]
        private static void AssertEncodingRules(AsnEncodingRules ruleSet)
        {
            Debug.Assert(ruleSet >= AsnEncodingRules.BER && ruleSet <= AsnEncodingRules.DER);
        }

        internal static void CheckEncodingRules(AsnEncodingRules ruleSet)
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
                    SR.Argument_UniversalValueIsFixed,
                    nameof(expectedTag));
            }

            if (expectedTag.TagClass != tag.TagClass || expectedTag.TagValue != tag.TagValue)
            {
                throw new AsnContentException(
                    SR.Format(
                        SR.ContentException_WrongTag,
                        tag.TagClass,
                        tag.TagValue,
                        expectedTag.TagClass,
                        expectedTag.TagValue));
            }
        }

        private enum LengthDecodeStatus
        {
            NeedMoreData,
            DerIndefinite,
            ReservedValue,
            LengthTooBig,
            LaxEncodingProhibited,
            Success,
        }

        private enum LengthValidity
        {
            CerRequiresIndefinite,
            PrimitiveEncodingRequiresDefinite,
            LengthExceedsInput,
            Valid,
        }
    }

    /// <summary>
    ///   A stateful, forward-only reader for BER-, CER-, or DER-encoded ASN.1 data.
    /// </summary>
    public partial class AsnReader
    {
        internal const int MaxCERSegmentSize = AsnDecoder.MaxCERSegmentSize;

        private ReadOnlyMemory<byte> _data;
        private readonly AsnReaderOptions _options;

        /// <summary>
        ///   Gets the encoding rules in use by this reader.
        /// </summary>
        /// <value>
        ///   The encoding rules in use by this reader.
        /// </value>
        public AsnEncodingRules RuleSet { get; }

        /// <summary>
        ///   Gets an indication of whether the reader has remaining data available to process.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if there is more data available for the reader to process;
        ///   otherwise, <see langword="false"/>.
        /// </value>
        public bool HasData => !_data.IsEmpty;

        /// <summary>
        ///   Construct an <see cref="AsnReader"/> over <paramref name="data"/> with a given ruleset.
        /// </summary>
        /// <param name="data">The data to read.</param>
        /// <param name="ruleSet">The encoding constraints for the reader.</param>
        /// <param name="options">Additional options for the reader.</param>
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
        public AsnReader(ReadOnlyMemory<byte> data, AsnEncodingRules ruleSet, AsnReaderOptions options = default)
        {
            AsnDecoder.CheckEncodingRules(ruleSet);

            _data = data;
            RuleSet = ruleSet;
            _options = options;
        }

        /// <summary>
        ///   Throws a standardized <see cref="AsnContentException"/> if the reader has remaining
        ///   data, performs no function if <see cref="HasData"/> returns <see langword="false"/>.
        /// </summary>
        /// <remarks>
        ///   This method provides a standardized target and standardized exception for reading a
        ///   "closed" structure, such as the nested content for an explicitly tagged value.
        /// </remarks>
        public void ThrowIfNotEmpty()
        {
            if (HasData)
            {
                throw new AsnContentException(SR.ContentException_TooMuchData);
            }
        }

        /// <summary>
        ///   Read the encoded tag at the next data position, without advancing the reader.
        /// </summary>
        /// <returns>
        ///   The decoded tag value.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   a tag could not be decoded at the reader's current position.
        /// </exception>
        public Asn1Tag PeekTag()
        {
            return Asn1Tag.Decode(_data.Span, out _);
        }

        /// <summary>
        ///   Get a <see cref="ReadOnlyMemory{T}"/> view of the next encoded value without
        ///   advancing the reader. For indefinite length encodings this includes the
        ///   End of Contents marker.
        /// </summary>
        /// <returns>
        ///   The bytes of the next encoded value.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   The reader is positioned at a point where the tag or length is invalid
        ///   under the current encoding rules.
        /// </exception>
        /// <seealso cref="PeekContentBytes"/>
        /// <seealso cref="ReadEncodedValue"/>
        public ReadOnlyMemory<byte> PeekEncodedValue()
        {
            AsnDecoder.ReadEncodedValue(_data.Span, RuleSet, out _, out _, out int bytesConsumed);
            return _data.Slice(0, bytesConsumed);
        }

        /// <summary>
        ///   Get a <see cref="ReadOnlyMemory{T}"/> view of the content octets (bytes) of the
        ///   next encoded value without advancing the reader.
        /// </summary>
        /// <returns>
        ///   The bytes of the contents octets of the next encoded value.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   The reader is positioned at a point where the tag or length is invalid
        ///   under the current encoding rules.
        /// </exception>
        /// <seealso cref="PeekEncodedValue"/>
        public ReadOnlyMemory<byte> PeekContentBytes()
        {
            AsnDecoder.ReadEncodedValue(
                _data.Span,
                RuleSet,
                out int contentOffset,
                out int contentLength,
                out _);

            return _data.Slice(contentOffset, contentLength);
        }

        /// <summary>
        ///   Get a <see cref="ReadOnlyMemory{T}"/> view of the next encoded value,
        ///   and advance the reader past it. For an indefinite length encoding this includes
        ///   the End of Contents marker.
        /// </summary>
        /// <returns>
        ///   A <see cref="ReadOnlyMemory{T}"/> view of the next encoded value.
        /// </returns>
        /// <seealso cref="PeekEncodedValue"/>
        public ReadOnlyMemory<byte> ReadEncodedValue()
        {
            ReadOnlyMemory<byte> encodedValue = PeekEncodedValue();
            _data = _data.Slice(encodedValue.Length);
            return encodedValue;
        }

        private AsnReader CloneAtSlice(int start, int length)
        {
            return new AsnReader(_data.Slice(start, length), RuleSet, _options);
        }
    }
}
