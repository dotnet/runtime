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
        /// Encode the segments { tag, length, value } of a bit string value based upon a NamedBitList.
        /// ((<paramref name="bigEndianBytes"/>[0] >> 7) &amp; 1) is considered the "leading" bit, proceeding
        /// through the array for up to <paramref name="namedBitsCount"/>.
        /// </summary>
        /// <param name="bigEndianBytes">
        /// The data in big endian order, the most significant bit of byte 0 is the leading bit
        /// (corresponds to the named value for "bit 0"). Any bits beyond <paramref name="namedBitsCount"/>
        /// are ignored, and any missing bits are assumed to be unset.
        /// </param>
        /// <param name="namedBitsCount">
        /// The total number of named bits.  Since the bits are numbered with a zero index, this should be
        /// one higher than the largest defined bit. (namedBitsCount=10 covers bits 0-9)
        /// </param>
        /// <returns>
        /// A triplet of { tag }, { length }, { data }.  All trailing unset named bits are removed. 
        /// </returns>
        internal static byte[][] SegmentedEncodeNamedBitList(byte[] bigEndianBytes, int namedBitsCount)
        {
            Debug.Assert(bigEndianBytes != null, "bigEndianBytes != null");
            Debug.Assert(namedBitsCount > 0, "namedBitsCount > 0");

            // The encoding that this follows is the DER encoding for NamedBitList, which is different than
            // (unnamed) BIT STRING.
            //
            // X.690 (08/2015) setion 11.2.2 (Unused bits) says:
            //    Where ITU-T Rec. X.680 | ISO/IEC 8824-1, 22.7, applies, the bitstring shall have all
            //    trailing 0 bits removed before it is encoded.
            //        NOTE 1 – In the case where a size constraint has been applied, the abstract value
            //            delivered by a decoder to the application will be one of those satisfying the
            //            size constraint and differing from the transmitted value only in the number of
            //            trailing 0 bits.
            //        NOTE 2 – If a bitstring value has no 1 bits, then an encoder shall encode the value
            //            with a length of 1 and an initial octet set to 0.
            //
            // X.680 (08/2015) section 22.7 says:
            //    When a "NamedBitList" is used in defining a bitstring type ASN.1 encoding rules are free
            //    to add (or remove) arbitrarily any trailing 0 bits to (or from) values that are being
            //    encoded or decoded
            //
            // Therefore, if 16 bits are defined, and only bit 7 is set, instead of { 00 01 00 } the encoding
            // should be { 00 01 }
            //
            // And, if 8 bits are defined, and only bit 6 is set, instead of { 00 02 } it should be { 01 02 },
            // signifiying that the last bit was omitted.

            int lastSetBit = -1;

            int lastBitProvided = (bigEndianBytes.Length * 8) - 1;
            int lastPossibleBit = Math.Min(lastBitProvided, namedBitsCount - 1);

            for (int currentBit = lastPossibleBit; currentBit >= 0; currentBit--)
            {
                int currentByte = currentBit / 8;

                // As we loop through the numbered bits we need to figure out
                // 1) which indexed byte it would be in (currentByte)
                // 2) How many bits from the right it is (shiftIndex)
                // 
                // For example:
                // currentBit 0 => currentByte 0, shiftIndex 7 (1 << 7)
                // currentBit 1 => currentByte 0, shiftIndex 6 (1 << 6)
                // currentBit 7 => currentByte 0, shiftIndex 0 (1 << 0)
                // currentBit 8 => currentByte 1, shiftIndex 7 (1 << 7)
                // etc
                int shiftIndex = 7 - (currentBit % 8);
                int testValue = 1 << shiftIndex;
                byte dataByte = bigEndianBytes[currentByte];

                if ((dataByte & testValue) == testValue)
                {
                    lastSetBit = currentBit;
                    break;
                }
            }

            byte[] dataSegment;

            if (lastSetBit >= 0)
            {
                // Bits are zero-indexed, so lastSetBit=0 means "1 semantic bit", and
                // "1 semantic bit" requires a byte to write it down.
                int semanticBits = lastSetBit + 1;
                int semanticBytes = (7 + semanticBits) / 8;

                // For a lastSetBit of  : 0 1 2 3 4 5 6 7 8 9 A B C D E F
                // unused bits should be: 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0
                int unusedBits = 7 - (lastSetBit % 8);

                // We need to make a mask of the bits to keep around for the last
                // byte, ensuring we clear out any bits that were set, but beyond
                // the namedBitsCount limit.
                // 
                // For example:
                // lastSetBit 0 => mask 0b10000000
                // lastSetBit 1 => mask 0b11000000
                // lastSetBit 7 => mask 0b11111111
                // lastSetBit 8 => mask 0b10000000
                byte lastByteSemanticMask = unchecked((byte)(-1 << unusedBits));

                // Semantic bytes plus the "how many unused bits" prefix byte.
                dataSegment = new byte[semanticBytes + 1];
                dataSegment[0] = (byte)unusedBits;
                
                Debug.Assert(semanticBytes <= bigEndianBytes.Length);

                Buffer.BlockCopy(bigEndianBytes, 0, dataSegment, 1, semanticBytes);

                // But the last byte might have too many bits set, trim it down
                // to only the ones we knew about (all "don't care" values must be 0)
                dataSegment[semanticBytes] &= lastByteSemanticMask;
            }
            else
            {
                // No bits being set is encoded as just "no unused bits",
                // with no semantic payload.
                dataSegment = new byte[] { 0x00 };
            }

            return new byte[][]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.BitString },
                EncodeLength(dataSegment.Length),
                dataSegment
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
