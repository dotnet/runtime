// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Reads a Set-Of value from <paramref name="source"/> with a specified tag
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
        /// <param name="skipSortOrderValidation">
        ///   <see langword="true"/> to always accept the data in the order it is presented,
        ///   <see langword="false"/> to verify that the data is sorted correctly when the
        ///   encoding rules say sorting was required (CER and DER).
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 17).
        /// </param>
        /// <remarks>
        ///   The nested content is not evaluated by this method, except for minimal processing to
        ///   determine the location of an end-of-contents marker or verification of the content
        ///   sort order.
        ///   Therefore, the contents may contain data which is not valid under the current encoding rules.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public static void ReadSetOf(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int contentOffset,
            out int contentLength,
            out int bytesConsumed,
            bool skipSortOrderValidation = false,
            Asn1Tag? expectedTag = null)
        {
            Asn1Tag tag = ReadTagAndLength(source, ruleSet, out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag ?? Asn1Tag.SetOf, UniversalTagNumber.SetOf);

            // T-REC-X.690-201508 sec 8.12.1
            if (!tag.IsConstructed)
            {
                throw new AsnContentException(
                    SR.Format(
                        SR.ContentException_ConstructedEncodingRequired,
                        UniversalTagNumber.SetOf));
            }

            int suffix;
            ReadOnlySpan<byte> contents;

            if (length.HasValue)
            {
                suffix = 0;
                contents = Slice(source, headerLength, length.Value);
            }
            else
            {
                int actualLength = SeekEndOfContents(source.Slice(headerLength), ruleSet);
                contents = Slice(source, headerLength, actualLength);
                suffix = EndOfContentsEncodedLength;
            }

            if (!skipSortOrderValidation)
            {
                // T-REC-X.690-201508 sec 11.6
                // BER data is not required to be sorted.
                if (ruleSet == AsnEncodingRules.DER ||
                    ruleSet == AsnEncodingRules.CER)
                {
                    ReadOnlySpan<byte> remaining = contents;
                    ReadOnlySpan<byte> previous = default;

                    while (!remaining.IsEmpty)
                    {
                        ReadEncodedValue(remaining, ruleSet, out _, out _, out int consumed);

                        ReadOnlySpan<byte> current = remaining.Slice(0, consumed);
                        remaining = remaining.Slice(consumed);

                        if (SetOfValueComparer.Compare(current, previous) < 0)
                        {
                            throw new AsnContentException(SR.ContentException_SetOfNotSorted);
                        }

                        previous = current;
                    }
                }
            }

            contentOffset = headerLength;
            contentLength = contents.Length;
            bytesConsumed = headerLength + contents.Length + suffix;
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a SET-OF with the specified tag
        ///   and returns the result as a new reader positioned at the first
        ///   value in the set-of (or with <see cref="HasData"/> == <see langword="false"/>),
        ///   using the <see cref="AsnReaderOptions.SkipSetSortOrderVerification"/> value
        ///   from the constructor (default <see langword="false"/>).
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 17).
        /// </param>
        /// <returns>
        ///   A new reader positioned at the first
        ///   value in the set-of (or with <see cref="HasData"/> == <see langword="false"/>).
        /// </returns>
        /// <remarks>
        ///   the nested content is not evaluated by this method (aside from sort order, when
        ///   required), and may contain data which is not valid under the current encoding rules.
        /// </remarks>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public AsnReader ReadSetOf(Asn1Tag? expectedTag = null)
        {
            return ReadSetOf(_options.SkipSetSortOrderVerification, expectedTag);
        }

        /// <summary>
        ///   Reads the next value as a SET-OF with the specified tag
        ///   and returns the result as a new reader positioned at the first
        ///   value in the set-of (or with <see cref="HasData"/> == <see langword="false"/>).
        /// </summary>
        /// <param name="skipSortOrderValidation">
        ///   <see langword="true"/> to always accept the data in the order it is presented,
        ///   <see langword="false"/> to verify that the data is sorted correctly when the
        ///   encoding rules say sorting was required (CER and DER).
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 17).
        /// </param>
        /// <returns>
        ///   A new reader positioned at the first
        ///   value in the set-of (or with <see cref="HasData"/> == <see langword="false"/>).
        /// </returns>
        /// <remarks>
        ///   the nested content is not evaluated by this method (aside from sort order, when
        ///   required), and may contain data which is not valid under the current encoding rules.
        /// </remarks>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public AsnReader ReadSetOf(bool skipSortOrderValidation, Asn1Tag? expectedTag = null)
        {
            AsnDecoder.ReadSetOf(
                _data.Span,
                RuleSet,
                out int contentOffset,
                out int contentLength,
                out int bytesConsumed,
                skipSortOrderValidation,
                expectedTag);

            AsnReader ret = CloneAtSlice(contentOffset, contentLength);
            _data = _data.Slice(bytesConsumed);
            return ret;
        }
    }
}
