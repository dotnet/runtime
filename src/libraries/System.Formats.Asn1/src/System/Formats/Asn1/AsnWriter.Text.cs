// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    public sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write the provided string using the specified encoding type using the specified
        ///   tag corresponding to the encoding type.
        /// </summary>
        /// <param name="encodingType">
        ///   One of the enumeration values representing the encoding to use.
        /// </param>
        /// <param name="value">The string to write.</param>
        /// <param name="tag">
        ///   The tag to write, or <see langword="null"/> for the universal tag that is appropriate to
        ///   the requested encoding type.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a restricted character string encoding type.
        ///
        ///   -or-
        ///
        ///   <paramref name="encodingType"/> is a restricted character string encoding type that is not
        ///   currently supported by this method.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public void WriteCharacterString(UniversalTagNumber encodingType, string value, Asn1Tag? tag = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            WriteCharacterString(encodingType, value.AsSpan(), tag);
        }

        /// <summary>
        ///   Write the provided string using the specified encoding type using the specified
        ///   tag corresponding to the encoding type.
        /// </summary>
        /// <param name="encodingType">
        ///   One of the enumeration values representing the encoding to use.
        /// </param>
        /// <param name="str">The string to write.</param>
        /// <param name="tag">
        ///   The tag to write, or <see langword="null"/> for the universal tag that is appropriate to
        ///   the requested encoding type.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a restricted character string encoding type.
        ///
        ///   -or-
        ///
        ///   <paramref name="encodingType"/> is a restricted character string encoding type that is not
        ///   currently supported by this method.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public void WriteCharacterString(UniversalTagNumber encodingType, ReadOnlySpan<char> str, Asn1Tag? tag = null)
        {
            CheckUniversalTag(tag, encodingType);

            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
            WriteCharacterStringCore(tag ?? new Asn1Tag(encodingType), encoding, str);
        }

        // T-REC-X.690-201508 sec 8.23
        private void WriteCharacterStringCore(Asn1Tag tag, Text.Encoding encoding, ReadOnlySpan<char> str)
        {
            int size = encoding.GetByteCount(str);

            // T-REC-X.690-201508 sec 9.2
            if (RuleSet == AsnEncodingRules.CER)
            {
                // If it exceeds the primitive segment size, use the constructed encoding.
                if (size > AsnReader.MaxCERSegmentSize)
                {
                    WriteConstructedCerCharacterString(tag, encoding, str, size);
                    return;
                }
            }

            // Clear the constructed tag, if present.
            WriteTag(tag.AsPrimitive());
            WriteLength(size);
            Span<byte> dest = _buffer.AsSpan(_offset, size);

            int written = encoding.GetBytes(str, dest);

            if (written != size)
            {
                Debug.Fail(
                    $"Encoding produced different answer for GetByteCount ({size}) and GetBytes ({written})");
                throw new InvalidOperationException();
            }

            _offset += size;
        }

        private void WriteConstructedCerCharacterString(Asn1Tag tag, Text.Encoding encoding, ReadOnlySpan<char> str, int size)
        {
            Debug.Assert(size > AsnReader.MaxCERSegmentSize);

            byte[] tmp = CryptoPool.Rent(size);
            int written = encoding.GetBytes(str, tmp);

            if (written != size)
            {
                Debug.Fail(
                    $"Encoding produced different answer for GetByteCount ({size}) and GetBytes ({written})");
                throw new InvalidOperationException();
            }

            WriteConstructedCerOctetString(tag, tmp.AsSpan(0, size));
            CryptoPool.Return(tmp, size);
        }
    }
}
