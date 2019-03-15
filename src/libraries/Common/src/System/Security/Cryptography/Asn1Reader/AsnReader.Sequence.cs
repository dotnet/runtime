// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a SEQUENCE or SEQUENCE-OF with tag UNIVERSAL 16
        ///   and returns the result as an <see cref="AsnReader"/> positioned at the first
        ///   value in the sequence (or with <see cref="HasData"/> == <c>false</c>).
        /// </summary>
        /// <returns>
        ///   an <see cref="AsnReader"/> positioned at the first
        ///   value in the sequence (or with <see cref="HasData"/> == <c>false</c>).
        /// </returns>
        /// <remarks>
        ///   the nested content is not evaluated by this method, and may contain data
        ///   which is not valid under the current encoding rules.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <see cref="ReadSequence(Asn1Tag)"/>
        public AsnReader ReadSequence() => ReadSequence(Asn1Tag.Sequence);

        /// <summary>
        ///   Reads the next value as a SEQUENCE or SEQUENCE-OF with the specified tag
        ///   and returns the result as an <see cref="AsnReader"/> positioned at the first
        ///   value in the sequence (or with <see cref="HasData"/> == <c>false</c>).
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <returns>
        ///   an <see cref="AsnReader"/> positioned at the first
        ///   value in the sequence (or with <see cref="HasData"/> == <c>false</c>).
        /// </returns>
        /// <remarks>
        ///   the nested content is not evaluated by this method, and may contain data
        ///   which is not valid under the current encoding rules.
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
        public AsnReader ReadSequence(Asn1Tag expectedTag)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.Sequence);

            // T-REC-X.690-201508 sec 8.9.1
            // T-REC-X.690-201508 sec 8.10.1
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

            _data = _data.Slice(headerLength + contents.Length + suffix);
            return new AsnReader(contents, RuleSet);
        }
    }
}
