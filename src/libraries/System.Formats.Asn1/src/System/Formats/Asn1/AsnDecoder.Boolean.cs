// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Reads a Boolean value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 1).
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
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
        public static bool ReadBoolean(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            // T-REC-X.690-201508 sec 8.2.1
            ReadOnlySpan<byte> contents = GetPrimitiveContentSpan(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.Boolean,
                UniversalTagNumber.Boolean,
                out int consumed);

            // T-REC-X.690-201508 sec 8.2.1
            if (contents.Length != 1)
            {
                throw new AsnContentException();
            }

            byte val = contents[0];

            // T-REC-X.690-201508 sec 8.2.2
            if (val == 0)
            {
                bytesConsumed = consumed;
                return false;
            }

            // T-REC-X.690-201508 sec 11.1
            if (val != 0xFF && (ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER))
            {
                throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
            }

            bytesConsumed = consumed;
            return true;
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a Boolean with a specified tag.
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 1).
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
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
        public bool ReadBoolean(Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.ReadBoolean(_data.Span, RuleSet, out int bytesConsumed, expectedTag);
            _data = _data.Slice(bytesConsumed);
            return ret;
        }
    }
}
