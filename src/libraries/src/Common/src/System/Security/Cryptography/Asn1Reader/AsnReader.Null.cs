// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a NULL with tag UNIVERSAL 5.
        /// </summary>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public void ReadNull() => ReadNull(Asn1Tag.Null);

        /// <summary>
        ///   Reads the next value as a NULL with a specified tag.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
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
        public void ReadNull(Asn1Tag expectedTag)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.Null);

            // T-REC-X.690-201508 sec 8.8.1
            // T-REC-X.690-201508 sec 8.8.2
            if (tag.IsConstructed || length != 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            _data = _data.Slice(headerLength);
        }
    }
}
