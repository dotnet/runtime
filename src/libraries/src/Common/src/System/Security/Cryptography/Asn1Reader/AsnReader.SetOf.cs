// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a SET-OF with the specified tag
        ///   and returns the result as an <see cref="AsnReader"/> positioned at the first
        ///   value in the set-of (or with <see cref="HasData"/> == <c>false</c>).
        /// </summary>
        /// <param name="skipSortOrderValidation">
        ///   <c>true</c> to always accept the data in the order it is presented,
        ///   <c>false</c> to verify that the data is sorted correctly when the
        ///   encoding rules say sorting was required (CER and DER).
        /// </param>
        /// <returns>
        ///   an <see cref="AsnReader"/> positioned at the first
        ///   value in the set-of (or with <see cref="HasData"/> == <c>false</c>).
        /// </returns>
        /// <remarks>
        ///   the nested content is not evaluated by this method (aside from sort order, when
        ///   required), and may contain data which is not valid under the current encoding rules.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public AsnReader ReadSetOf(bool skipSortOrderValidation = false) =>
            ReadSetOf(Asn1Tag.SetOf, skipSortOrderValidation);

        /// <summary>
        ///   Reads the next value as a SET-OF with the specified tag
        ///   and returns the result as an <see cref="AsnReader"/> positioned at the first
        ///   value in the set-of (or with <see cref="HasData"/> == <c>false</c>).
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="skipSortOrderValidation">
        ///   <c>true</c> to always accept the data in the order it is presented,
        ///   <c>false</c> to verify that the data is sorted correctly when the
        ///   encoding rules say sorting was required (CER and DER).
        /// </param>
        /// <returns>
        ///   an <see cref="AsnReader"/> positioned at the first
        ///   value in the set-of (or with <see cref="HasData"/> == <c>false</c>).
        /// </returns>
        /// <remarks>
        ///   the nested content is not evaluated by this method (aside from sort order, when
        ///   required), and may contain data which is not valid under the current encoding rules.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public AsnReader ReadSetOf(Asn1Tag expectedTag, bool skipSortOrderValidation = false)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.SetOf);

            // T-REC-X.690-201508 sec 8.12.1
            if (!tag.IsConstructed)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            int suffix = 0;

            if (length == null)
            {
                length = SeekEndOfContents(_data.Slice(headerLength));
                suffix = EndOfContentsEncodedLength;
            }

            ReadOnlyMemory<byte> contents = Slice(_data, headerLength, length.Value);

            if (!skipSortOrderValidation)
            {
                // T-REC-X.690-201508 sec 11.6
                // BER data is not required to be sorted.
                if (RuleSet == AsnEncodingRules.DER ||
                    RuleSet == AsnEncodingRules.CER)
                {
                    AsnReader reader = new AsnReader(contents, RuleSet);
                    ReadOnlyMemory<byte> current = ReadOnlyMemory<byte>.Empty;
                    SetOfValueComparer comparer = SetOfValueComparer.Instance;

                    while (reader.HasData)
                    {
                        ReadOnlyMemory<byte> previous = current;
                        current = reader.ReadEncodedValue();

                        if (comparer.Compare(current, previous) < 0)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }
                    }
                }
            }

            _data = _data.Slice(headerLength + contents.Length + suffix);
            return new AsnReader(contents, RuleSet);
        }
    }
}
