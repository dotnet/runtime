// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Asn1
{
    public sealed partial class AsnWriter
    {
        /// <summary>
        ///   Begin writing an Octet String value with a specified tag.
        /// </summary>
        /// <returns>
        ///   A disposable value which will automatically call <see cref="PopOctetString"/>.
        /// </returns>
        /// <remarks>
        ///   This method is just an accelerator for writing an Octet String value where the
        ///   contents are also ASN.1 data encoded under the same encoding system.
        ///   When <see cref="PopOctetString"/> is called the entire nested contents are
        ///   normalized as a single Octet String value, encoded correctly for the current encoding
        ///   rules.
        ///   This method does not necessarily create a Constructed encoding, and it is not invalid to
        ///   write values other than Octet String inside this Push/Pop.
        /// </remarks>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 4).</param>
        /// <seealso cref="PopOctetString"/>
        public Scope PushOctetString(Asn1Tag? tag = null)
        {
            CheckUniversalTag(tag, UniversalTagNumber.OctetString);

            return PushTag(
                tag?.AsConstructed() ?? Asn1Tag.ConstructedOctetString,
                UniversalTagNumber.OctetString);
        }

        /// <summary>
        ///   Indicate that the open Octet String with the tag UNIVERSAL 4 is closed,
        ///   returning the writer to the parent context.
        /// </summary>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 4).</param>
        /// <remarks>
        ///   In <see cref="AsnEncodingRules.BER"/> and <see cref="AsnEncodingRules.DER"/> modes
        ///   the encoded contents will remain in a single primitive Octet String.
        ///   In <see cref="AsnEncodingRules.CER"/> mode the contents will be broken up into
        ///   multiple segments, when required.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   the writer is not currently positioned within an Octet String with the specified tag.
        /// </exception>
        public void PopOctetString(Asn1Tag? tag = default)
        {
            CheckUniversalTag(tag, UniversalTagNumber.OctetString);
            PopTag(tag?.AsConstructed() ?? Asn1Tag.ConstructedOctetString, UniversalTagNumber.OctetString);
        }

        /// <summary>
        ///   Write an Octet String value with a specified tag.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 4).</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public void WriteOctetString(ReadOnlySpan<byte> value, Asn1Tag? tag = null)
        {
            CheckUniversalTag(tag, UniversalTagNumber.OctetString);

            // Primitive or constructed, doesn't matter.
            WriteOctetStringCore(tag ?? Asn1Tag.PrimitiveOctetString, value);
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
