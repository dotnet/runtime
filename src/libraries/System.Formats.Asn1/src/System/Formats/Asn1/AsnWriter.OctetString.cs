// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write an Octet String with tag UNIVERSAL 4.
        /// </summary>
        /// <param name="octetString">The value to write.</param>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteOctetString(Asn1Tag,ReadOnlySpan{byte})"/>
        public void WriteOctetString(ReadOnlySpan<byte> octetString)
        {
            WriteOctetString(Asn1Tag.PrimitiveOctetString, octetString);
        }

        /// <summary>
        ///   Write an Octet String value with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="octetString">The value to write.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteOctetString(Asn1Tag tag, ReadOnlySpan<byte> octetString)
        {
            CheckUniversalTag(tag, UniversalTagNumber.OctetString);

            // Primitive or constructed, doesn't matter.
            WriteOctetStringCore(tag, octetString);
        }

        // T-REC-X.690-201508 sec 8.7
        private void WriteOctetStringCore(Asn1Tag tag, ReadOnlySpan<byte> octetString)
        {
            if (RuleSet == AsnEncodingRules.CER)
            {
                // If it's bigger than a primitive segment, use the constructed encoding
                // T-REC-X.690-201508 sec 9.2
                if (octetString.Length > AsnReader.MaxCERSegmentSize)
                {
                    WriteConstructedCerOctetString(tag, octetString);
                    return;
                }
            }

            // Clear the constructed flag, if present.
            WriteTag(tag.AsPrimitive());
            WriteLength(octetString.Length);
            octetString.CopyTo(_buffer.AsSpan(_offset));
            _offset += octetString.Length;
        }

        // T-REC-X.690-201508 sec 9.2, 8.7
        private void WriteConstructedCerOctetString(Asn1Tag tag, ReadOnlySpan<byte> payload)
        {
            const int MaxCERSegmentSize = AsnReader.MaxCERSegmentSize;
            Debug.Assert(payload.Length > MaxCERSegmentSize);

            WriteTag(tag.AsConstructed());
            WriteLength(-1);

            int fullSegments = Math.DivRem(payload.Length, MaxCERSegmentSize, out int lastSegmentSize);

            // The tag size is 1 byte.
            // The length will always be encoded as 82 03 E8 (3 bytes)
            // And 1000 content octets (by T-REC-X.690-201508 sec 9.2)
            const int FullSegmentEncodedSize = 1004;
            Debug.Assert(
                FullSegmentEncodedSize == 1 + 1 + MaxCERSegmentSize + GetEncodedLengthSubsequentByteCount(MaxCERSegmentSize));

            int remainingEncodedSize;

            if (lastSegmentSize == 0)
            {
                remainingEncodedSize = 0;
            }
            else
            {
                // One byte of tag, and minimum one byte of length.
                remainingEncodedSize = 2 + lastSegmentSize + GetEncodedLengthSubsequentByteCount(lastSegmentSize);
            }

            // Reduce the number of copies by pre-calculating the size.
            // +2 for End-Of-Contents
            int expectedSize = fullSegments * FullSegmentEncodedSize + remainingEncodedSize + 2;
            EnsureWriteCapacity(expectedSize);

            byte[] ensureNoExtraCopy = _buffer;
            int savedOffset = _offset;

            ReadOnlySpan<byte> remainingData = payload;
            Span<byte> dest;
            Asn1Tag primitiveOctetString = Asn1Tag.PrimitiveOctetString;

            while (remainingData.Length > MaxCERSegmentSize)
            {
                // T-REC-X.690-201508 sec 8.7.3.2-note2
                WriteTag(primitiveOctetString);
                WriteLength(MaxCERSegmentSize);

                dest = _buffer.AsSpan(_offset);
                remainingData.Slice(0, MaxCERSegmentSize).CopyTo(dest);

                _offset += MaxCERSegmentSize;
                remainingData = remainingData.Slice(MaxCERSegmentSize);
            }

            WriteTag(primitiveOctetString);
            WriteLength(remainingData.Length);
            dest = _buffer.AsSpan(_offset);
            remainingData.CopyTo(dest);
            _offset += remainingData.Length;

            WriteEndOfContents();

            Debug.Assert(_offset - savedOffset == expectedSize, $"expected size was {expectedSize}, actual was {_offset - savedOffset}");
            Debug.Assert(_buffer == ensureNoExtraCopy, $"_buffer was replaced during {nameof(WriteConstructedCerOctetString)}");
        }
    }
}
