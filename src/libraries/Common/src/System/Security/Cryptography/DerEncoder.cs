// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Writes data encoded via the Distinguished Encoding Rules for Abstract
    /// Syntax Notation 1 (ASN.1) data.
    /// </summary>
    internal static class DerEncoder
    {
        private const byte ConstructedFlag = 0x20;
        private const byte ConstructedSequenceTag = ConstructedFlag | (byte)DerSequenceReader.DerTag.Sequence;

        private static byte[] EncodeLength(int length)
        {
            Debug.Assert(length >= 0);

            byte low = unchecked((byte)length);
            
            // If the length value fits in 7 bits, it's an answer all by itself.
            if (length < 0x80)
            {
                return new[] { low };
            }

            // If the length is more than 0x7F then it is stored as
            // 0x80 | lengthLength
            // big
            // endian
            // length

            // So:
            // 0 => 0x00.
            // 1 => 0x01.
            // 127 => 0x7F.
            // 128 => 0x81 0x80
            // 255 => 0x81 0xFF
            // 256 => 0x82 0x01 0x00
            // 65535 => 0x82 0xFF 0xFF
            // 65536 => 0x83 0x01 0x00 0x00
            // ...
            // int.MaxValue => 0x84 0x7F 0xFF 0xFF 0xFF
            //
            // Technically DER lengths can go longer than int.MaxValue, but since our
            // encoding input here is an int, our output will be no larger than that.

            if (length <= 0xFF)
            {
                return new byte[] { 0x81, low };
            }

            int remainder = length >> 8;
            byte midLow = unchecked((byte)remainder);

            if (length <= 0xFFFF)
            {
                return new byte[] { 0x82, midLow, low };
            }

            remainder >>= 8;
            byte midHigh = unchecked((byte)remainder);

            if (length <= 0xFFFFFF)
            {
                return new byte[] { 0x83, midHigh, midLow, low };
            }

            remainder >>= 8;
            byte high = unchecked((byte)remainder);

            // Since we know this was a non-negative signed number, the highest
            // legal value here is 0x7F.
            Debug.Assert(remainder < 0x80);

            return new byte[] { 0x84, high, midHigh, midLow, low };
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of an unsigned integer.
        /// </summary>
        /// <param name="bigEndianBytes">The value to encode, in big integer representation.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeUnsignedInteger(byte[] bigEndianBytes)
        {
            Debug.Assert(bigEndianBytes != null);

            return SegmentedEncodeUnsignedInteger(bigEndianBytes, 0, bigEndianBytes.Length);
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of an unsigned integer represented within a bounded array.
        /// </summary>
        /// <param name="bigEndianBytes">The value to encode, in big integer representation.</param>
        /// <param name="offset">The offset into bigEndianBytes to read</param>
        /// <param name="count">The count of bytes to read, must be greater than 0</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeUnsignedInteger(byte[] bigEndianBytes, int offset, int count)
        {
            Debug.Assert(bigEndianBytes != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count > 0);
            Debug.Assert(bigEndianBytes.Length > 0);
            Debug.Assert(bigEndianBytes.Length >= count - offset);

            int start = offset;
            int end = start + count;

            // Remove any leading zeroes.
            while (start < end && bigEndianBytes[start] == 0)
            {
                start++;
            }

            // All zeroes, just back up one and let it flow through the normal flow.
            if (start == end)
            {
                start--;
                Debug.Assert(start >= offset);
            }

            int length = end - start;
            byte[] dataBytes;
            int writeStart = 0;

            // If the first byte is bigger than 0x7F it will look like a negative number, since
            // we're unsigned, insert a zero-padding byte.
            if (bigEndianBytes[start] > 0x7F)
            {
                dataBytes = new byte[length + 1];
                writeStart = 1;
            }
            else
            {
                dataBytes = new byte[length];
            }

            Buffer.BlockCopy(bigEndianBytes, start, dataBytes, writeStart, length);

            return new[]
            {
                new[] { (byte)DerSequenceReader.DerTag.Integer },
                EncodeLength(dataBytes.Length),
                dataBytes,
            };
        }

        /// <summary>
        /// Make a constructed SEQUENCE of the byte-triplets of the contents.
        /// Each byte[][] should be a byte[][3] of {tag (1 byte), length (1-5 bytes), payload (variable)}.
        /// </summary>
        internal static byte[] ConstructSequence(params byte[][][] items)
        {
            // A more robust solution would be required for public API.  DerInteger(int), DerBoolean, etc,
            // which do not allow the user to specify lengths, but only the payload.  But for efficiency things
            // are tracked as just little segments of bytes, and they're not glued together until this method.

            int payloadLength = 0;

            foreach (byte[][] segments in items)
            {
                foreach (byte[] segment in segments)
                {
                    payloadLength += segment.Length;
                }
            }

            byte[] encodedLength = EncodeLength(payloadLength);
            
            // The tag (1) + the length of the length + the length of the payload
            byte[] encodedSequence = new byte[1 + encodedLength.Length + payloadLength];

            encodedSequence[0] = ConstructedSequenceTag;

            int writeStart = 1;

            Buffer.BlockCopy(encodedLength, 0, encodedSequence, writeStart, encodedLength.Length);

            writeStart += encodedLength.Length;

            foreach (byte[][] segments in items)
            {
                Debug.Assert(segments != null);
                Debug.Assert(segments.Length == 3);

                foreach (byte[] segment in segments)
                {
                    Debug.Assert(segment != null);

                    Buffer.BlockCopy(segment, 0, encodedSequence, writeStart, segment.Length);
                    writeStart += segment.Length;
                }
            }

            return encodedSequence;
        }
    }
}
