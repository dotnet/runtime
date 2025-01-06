// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Reads a Sequence or Sequence-Of value from <paramref name="source"/> with a specified tag
        ///   under the specified encoding rules.
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
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 16).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   The next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   The length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   The contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <remarks>
        ///   The nested content is not evaluated by this method, except for minimal processing to
        ///   determine the location of an end-of-contents marker.
        ///   Therefore, the contents might contain data that's not valid under the current encoding rules.
        /// </remarks>
        public static void ReadSequence(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int contentOffset,
            out int contentLength,
            out int bytesConsumed,
            Asn1Tag? expectedTag = default)
        {
            Asn1Tag tag = ReadTagAndLength(source, ruleSet, out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag ?? Asn1Tag.Sequence, UniversalTagNumber.Sequence);

            // T-REC-X.690-201508 sec 8.9.1
            // T-REC-X.690-201508 sec 8.10.1
            if (!tag.IsConstructed)
            {
                throw new AsnContentException(
                    SR.Format(
                        SR.ContentException_ConstructedEncodingRequired,
                        UniversalTagNumber.Sequence));
            }

            if (length.HasValue)
            {
                if (length.Value > source.Length - headerLength)
                {
                    throw GetValidityException(LengthValidity.LengthExceedsInput);
                }

                contentLength = length.Value;
                contentOffset = headerLength;
                bytesConsumed = contentLength + headerLength;
            }
            else
            {
                int len = SeekEndOfContents(source.Slice(headerLength), ruleSet);

                contentLength = len;
                contentOffset = headerLength;
                bytesConsumed = len + headerLength + EndOfContentsEncodedLength;
            }
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a SEQUENCE or SEQUENCE-OF with the specified tag
        ///   and returns the result as a new reader positioned at the first
        ///   value in the sequence (or with <see cref="HasData"/> == <see langword="false"/>).
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 16).
        /// </param>
        /// <returns>
        ///   A new reader positioned at the first
        ///   value in the sequence (or with <see cref="HasData"/> == <see langword="false"/>).
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   The next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   The length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   The contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <remarks>
        ///   The nested content is not evaluated by this method, and might contain data
        ///   that's not valid under the current encoding rules.
        /// </remarks>
        public AsnReader ReadSequence(Asn1Tag? expectedTag = null)
        {
            AsnDecoder.ReadSequence(
                _data.Span,
                RuleSet,
                out int contentStart,
                out int contentLength,
                out int bytesConsumed,
                expectedTag);

            AsnReader ret = CloneAtSlice(contentStart, contentLength);
            _data = _data.Slice(bytesConsumed);
            return ret;
        }
    }
}
