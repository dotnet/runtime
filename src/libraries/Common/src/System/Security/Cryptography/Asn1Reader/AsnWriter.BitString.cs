// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write a Bit String value with a tag UNIVERSAL 3.
        /// </summary>
        /// <param name="bitString">The value to write.</param>
        /// <param name="unusedBitCount">
        ///   The number of trailing bits which are not semantic.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="unusedBitCount"/> is not in the range [0,7]
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="bitString"/> has length 0 and <paramref name="unusedBitCount"/> is not 0 --OR--
        ///   <paramref name="bitString"/> is not empty and any of the bits identified by
        ///   <paramref name="unusedBitCount"/> is set
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteBitString(ReadOnlySpan<byte> bitString, int unusedBitCount = 0)
        {
            WriteBitStringCore(Asn1Tag.PrimitiveBitString, bitString, unusedBitCount);
        }

        /// <summary>
        ///   Write a Bit String value with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="bitString">The value to write.</param>
        /// <param name="unusedBitCount">
        ///   The number of trailing bits which are not semantic.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="unusedBitCount"/> is not in the range [0,7]
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="bitString"/> has length 0 and <paramref name="unusedBitCount"/> is not 0 --OR--
        ///   <paramref name="bitString"/> is not empty and any of the bits identified by
        ///   <paramref name="unusedBitCount"/> is set
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteBitString(Asn1Tag tag, ReadOnlySpan<byte> bitString, int unusedBitCount = 0)
        {
            CheckUniversalTag(tag, UniversalTagNumber.BitString);

            // Primitive or constructed, doesn't matter.
            WriteBitStringCore(tag, bitString, unusedBitCount);
        }

        // T-REC-X.690-201508 sec 8.6
        private void WriteBitStringCore(Asn1Tag tag, ReadOnlySpan<byte> bitString, int unusedBitCount)
        {
            // T-REC-X.690-201508 sec 8.6.2.2
            if (unusedBitCount < 0 || unusedBitCount > 7)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unusedBitCount),
                    unusedBitCount,
                    SR.Cryptography_Asn_UnusedBitCountRange);
            }

            CheckDisposed();

            // T-REC-X.690-201508 sec 8.6.2.3
            if (bitString.Length == 0 && unusedBitCount != 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            byte lastByte = bitString.IsEmpty ? (byte)0 : bitString[bitString.Length - 1];

            // T-REC-X.690-201508 sec 11.2
            //
            // This could be ignored for BER, but since DER is more common and
            // it likely suggests a program error on the caller, leave it enabled for
            // BER for now.
            if (!CheckValidLastByte(lastByte, unusedBitCount))
            {
                // TODO: Probably warrants a distinct message.
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (RuleSet == AsnEncodingRules.CER)
            {
                // T-REC-X.690-201508 sec 9.2
                //
                // If it's not within a primitive segment, use the constructed encoding.
                // (>= instead of > because of the unused bit count byte)
                if (bitString.Length >= AsnReader.MaxCERSegmentSize)
                {
                    WriteConstructedCerBitString(tag, bitString, unusedBitCount);
                    return;
                }
            }

            // Clear the constructed flag, if present.
            WriteTag(tag.AsPrimitive());
            // The unused bits byte requires +1.
            WriteLength(bitString.Length + 1);
            _buffer[_offset] = (byte)unusedBitCount;
            _offset++;
            bitString.CopyTo(_buffer.AsSpan(_offset));
            _offset += bitString.Length;
        }

#if netcoreapp || uap || NETCOREAPP
        /// <summary>
        ///   Write a Bit String value via a callback, with a tag UNIVERSAL 3.
        /// </summary>
        /// <param name="byteLength">The total number of bytes to write.</param>
        /// <param name="state">A state object to pass to <paramref name="action"/>.</param>
        /// <param name="action">A callback to invoke for populating the Bit String.</param>
        /// <param name="unusedBitCount">
        ///   The number of trailing bits which are not semantic.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="byteLength"/> is negative --OR--
        ///   <paramref name="unusedBitCount"/> is not in the range [0,7]
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="byteLength"/> is 0 and <paramref name="unusedBitCount"/> is not 0 --OR--
        ///   <paramref name="byteLength"/> is not 0 and any of the bits identified by
        ///   <paramref name="unusedBitCount"/> is set
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteBitString<TState>(
            int byteLength,
            TState state,
            SpanAction<byte, TState> action,
            int unusedBitCount = 0)
        {
            WriteBitStringCore(Asn1Tag.PrimitiveBitString, byteLength, state, action, unusedBitCount);
        }

        /// <summary>
        ///   Write a Bit String value via a callback, with a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="byteLength">The total number of bytes to write.</param>
        /// <param name="state">A state object to pass to <paramref name="action"/>.</param>
        /// <param name="action">A callback to invoke for populating the Bit String.</param>
        /// <param name="unusedBitCount">
        ///   The number of trailing bits which are not semantic.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="byteLength"/> is negative --OR--
        ///   <paramref name="unusedBitCount"/> is not in the range [0,7]
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="byteLength"/> is 0 and <paramref name="unusedBitCount"/> is not 0 --OR--
        ///   <paramref name="byteLength"/> is not 0 and any of the bits identified by
        ///   <paramref name="unusedBitCount"/> is set
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteBitString<TState>(
            Asn1Tag tag,
            int byteLength,
            TState state,
            SpanAction<byte, TState> action,
            int unusedBitCount = 0)
        {
            CheckUniversalTag(tag, UniversalTagNumber.BitString);

            // Primitive or constructed, doesn't matter.
            WriteBitStringCore(tag, byteLength, state, action, unusedBitCount);
        }

        // T-REC-X.690-201508 sec 8.6
        private void WriteBitStringCore<TState>(
            Asn1Tag tag,
            int byteLength,
            TState state,
            SpanAction<byte, TState> action,
            int unusedBitCount = 0)
        {
            if (byteLength == 0)
            {
                WriteBitStringCore(tag, ReadOnlySpan<byte>.Empty, unusedBitCount);
                return;
            }

            // T-REC-X.690-201508 sec 8.6.2.2
            if (unusedBitCount < 0 || unusedBitCount > 7)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unusedBitCount),
                    unusedBitCount,
                    SR.Cryptography_Asn_UnusedBitCountRange);
            }

            CheckDisposed();

            int savedOffset = _offset;
            Span<byte> scratchSpace;
            byte[] ensureNoExtraCopy = null;
            int expectedSize = 0;

            // T-REC-X.690-201508 sec 9.2
            //
            // If it's not within a primitive segment, use the constructed encoding.
            // (>= instead of > because of the unused bit count byte)
            bool segmentedWrite =
                RuleSet == AsnEncodingRules.CER && byteLength >= AsnReader.MaxCERSegmentSize;

            if (segmentedWrite)
            {
                // Rather than call the callback multiple times, grow the buffer to allow
                // for enough space for the final output, then return a buffer where the last segment
                // is in the correct place. (Data will shift backwards to the right spot while writing
                // other segments).
                expectedSize = DetermineCerBitStringTotalLength(tag, byteLength);
                EnsureWriteCapacity(expectedSize);
                int overhead = expectedSize - byteLength;

                // Start writing where the last content byte is in the correct place, which is
                // after all of the overhead, but ending before the two byte end-of-contents marker.
                int scratchStart = overhead - 2;
                ensureNoExtraCopy = _buffer;
                scratchSpace = _buffer.AsSpan(scratchStart, byteLength);

                // Don't let gapped-writes be unpredictable.
                scratchSpace.Clear();
            }
            else
            {
                WriteTag(tag.AsPrimitive());
                // The unused bits byte requires +1.
                WriteLength(byteLength + 1);

                _buffer[_offset] = (byte)unusedBitCount;
                _offset++;

                scratchSpace = _buffer.AsSpan(_offset, byteLength);
            }

            action(scratchSpace, state);

            // T-REC-X.690-201508 sec 11.2
            //
            // This could be ignored for BER, but since DER is more common and
            // it likely suggests a program error on the caller, leave it enabled for
            // BER for now.
            if (!CheckValidLastByte(scratchSpace[byteLength - 1], unusedBitCount))
            {
                // Since we are restoring _offset we won't clear this on a grow or Dispose,
                // so clear it now.
                _offset = savedOffset;
                scratchSpace.Clear();

                // TODO: Probably warrants a distinct message.
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (segmentedWrite)
            {
                WriteConstructedCerBitString(tag, scratchSpace, unusedBitCount);
                Debug.Assert(_offset - savedOffset == expectedSize, $"expected size was {expectedSize}, actual was {_offset - savedOffset}");
                Debug.Assert(_buffer == ensureNoExtraCopy, $"_buffer was replaced during while writing a bit string via callback");
            }
            else
            {
                _offset += byteLength;
            }
        }
#endif

        private static bool CheckValidLastByte(byte lastByte, int unusedBitCount)
        {
            // If 3 bits are "unused" then build a mask for them to check for 0.
            // 1 << 3 => 0b0000_1000
            // subtract 1 => 0b000_0111
            int mask = (1 << unusedBitCount) - 1;
            return ((lastByte & mask) == 0);
        }

        private static int DetermineCerBitStringTotalLength(Asn1Tag tag, int contentLength)
        {
            const int MaxCERSegmentSize = AsnReader.MaxCERSegmentSize;
            // Every segment has an "unused bit count" byte.
            const int MaxCERContentSize = MaxCERSegmentSize - 1;
            Debug.Assert(contentLength > MaxCERContentSize);

            int fullSegments = Math.DivRem(contentLength, MaxCERContentSize, out int lastContentSize);

            // The tag size is 1 byte.
            // The length will always be encoded as 82 03 E8 (3 bytes)
            // And 1000 content octets (by T-REC-X.690-201508 sec 9.2)
            const int FullSegmentEncodedSize = 1004;
            Debug.Assert(
                FullSegmentEncodedSize == 1 + 1 + MaxCERSegmentSize + GetEncodedLengthSubsequentByteCount(MaxCERSegmentSize));

            int remainingEncodedSize;

            if (lastContentSize == 0)
            {
                remainingEncodedSize = 0;
            }
            else
            {
                // One byte of tag, minimum one byte of length, and one byte of unused bit count.
                remainingEncodedSize = 3 + lastContentSize + GetEncodedLengthSubsequentByteCount(lastContentSize);
            }

            // Reduce the number of copies by pre-calculating the size.
            // +2 for End-Of-Contents
            // +1 for 0x80 indefinite length
            // +tag length
            return fullSegments * FullSegmentEncodedSize + remainingEncodedSize + 3 + tag.CalculateEncodedSize();
        }

        // T-REC-X.690-201508 sec 9.2, 8.6
        private void WriteConstructedCerBitString(Asn1Tag tag, ReadOnlySpan<byte> payload, int unusedBitCount)
        {
            const int MaxCERSegmentSize = AsnReader.MaxCERSegmentSize;
            // Every segment has an "unused bit count" byte.
            const int MaxCERContentSize = MaxCERSegmentSize - 1;
            Debug.Assert(payload.Length > MaxCERContentSize);

            int expectedSize = DetermineCerBitStringTotalLength(tag, payload.Length);
            EnsureWriteCapacity(expectedSize);
            int savedOffset = _offset;

            WriteTag(tag.AsConstructed());
            // T-REC-X.690-201508 sec 9.1
            // Constructed CER uses the indefinite form.
            WriteLength(-1);

            byte[] ensureNoExtraCopy = _buffer;

            ReadOnlySpan<byte> remainingData = payload;
            Span<byte> dest;
            Asn1Tag primitiveBitString = Asn1Tag.PrimitiveBitString;

            while (remainingData.Length > MaxCERContentSize)
            {
                // T-REC-X.690-201508 sec 8.6.4.1
                WriteTag(primitiveBitString);
                WriteLength(MaxCERSegmentSize);
                // 0 unused bits in this segment.
                _buffer[_offset] = 0;
                _offset++;

                dest = _buffer.AsSpan(_offset);
                remainingData.Slice(0, MaxCERContentSize).CopyTo(dest);

                remainingData = remainingData.Slice(MaxCERContentSize);
                _offset += MaxCERContentSize;
            }

            WriteTag(primitiveBitString);
            WriteLength(remainingData.Length + 1);

            _buffer[_offset] = (byte)unusedBitCount;
            _offset++;

            dest = _buffer.AsSpan(_offset);
            remainingData.CopyTo(dest);
            _offset += remainingData.Length;

            WriteEndOfContents();

            Debug.Assert(_offset - savedOffset == expectedSize, $"expected size was {expectedSize}, actual was {_offset - savedOffset}");
            Debug.Assert(_buffer == ensureNoExtraCopy, $"_buffer was replaced during {nameof(WriteConstructedCerBitString)}");
        }
    }
}
