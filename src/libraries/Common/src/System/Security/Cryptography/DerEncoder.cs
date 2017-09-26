// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

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
        private const byte ConstructedSetTag = ConstructedFlag | (byte)DerSequenceReader.DerTag.Set;

        private static readonly byte[][] s_nullTlv =
        {
            new byte[] { (byte)DerSequenceReader.DerTag.Null },
            new byte[] { 0 },
            Array.Empty<byte>(),
        };

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
        /// Encode the segments { tag, length, value } of a boolean.
        /// </summary>
        /// <param name="value">The boolean to encode</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeBoolean(bool value)
        {
            // BER says FALSE is zero, TRUE is other.
            // DER says TRUE is 0xFF.
            byte[] data =
            {
                (byte)(value ? 0xFF : 0x00),
            };

            return new byte[][]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.Boolean }, 
                new byte[] { 0x01 }, 
                data, 
            };
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of an unsigned integer.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeUnsignedInteger(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return SegmentedEncodeUnsignedInteger(bytes);
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of an unsigned integer.
        /// </summary>
        /// <param name="bigEndianBytes">The value to encode, in big integer representation.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeUnsignedInteger(ReadOnlySpan<byte> bigEndianBytes)
        {
            Debug.Assert(!bigEndianBytes.IsEmpty, "The span must not be empty.");

            int start = 0;
            int end = start + bigEndianBytes.Length;

            // Remove any leading zeroes.
            while (start < end && bigEndianBytes[start] == 0)
            {
                start++;
            }

            // All zeroes, just back up one and let it flow through the normal flow.
            if (start == end)
            {
                start--;
                Debug.Assert(start >= 0);
            }

            // If the first byte is bigger than 0x7F it will look like a negative number, since
            // we're unsigned, insert a zero-padding byte.
            int length = end - start;
            int writeStart = bigEndianBytes[start] > 0x7F ? 1 : 0;
            var dataBytes = new byte[length + writeStart];
            bigEndianBytes.Slice(start, length).CopyTo(new Span<byte>(dataBytes).Slice(writeStart));

            return new[]
            {
                new[] { (byte)DerSequenceReader.DerTag.Integer },
                EncodeLength(dataBytes.Length),
                dataBytes,
            };
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of a BIT STRING which is wrapped over
        /// other DER-encoded data.
        /// </summary>
        /// <param name="childSegments"></param>
        /// <remarks>
        /// Despite containing other DER-encoded data this does not get the constructed bit,
        /// because it doesn't when encoding public keys in SubjectPublicKeyInfo</remarks>
        /// <returns></returns>
        internal static byte[][] SegmentedEncodeBitString(params byte[][][] childSegments)
        {
            return SegmentedEncodeBitString(ConcatenateArrays(childSegments));
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of a bit string where all bits are significant.
        /// </summary>
        /// <param name="data">The data to encode</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeBitString(byte[] data)
        {
            return SegmentedEncodeBitString(0, data);
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of a bit string where the least significant
        /// <paramref name="unusedBits"/> of the last byte are padding.
        /// </summary>
        /// <param name="unusedBits">The number of padding bits (0-7) in the last byte</param>
        /// <param name="data">The data to encode</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeBitString(int unusedBits, byte[] data)
        {
            Debug.Assert(data != null);
            Debug.Assert(unusedBits >= 0);
            Debug.Assert(unusedBits <= 7);
            Debug.Assert(unusedBits == 0 || data.Length > 0);

            byte[] encodedData = new byte[data.Length + 1];

            // Copy data to encodedData, but leave a one byte gap for unusedBits.
            Buffer.BlockCopy(data, 0, encodedData, 1, data.Length);
            encodedData[0] = (byte)unusedBits;

            // We need to make a mask of the bits to keep around for the last
            // byte, ensuring we clear out any bits that were set, but reported
            // as padding by unusedBits
            // 
            // For example:
            // unusedBits 0 => mask 0b11111111
            // unusedBits 1 => mask 0b11111110
            // unusedBits 7 => mask 0b10000000
            byte lastByteSemanticMask = unchecked((byte)(-1 << unusedBits));

            // Since encodedData.Length is data.Length + 1, "encodedDate.Length - 1" is just "data.Length".
            encodedData[data.Length] &= lastByteSemanticMask;

            return new byte[][]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.BitString },
                EncodeLength(encodedData.Length),
                encodedData,
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
            // X.690 (08/2015) section 11.2.2 (Unused bits) says:
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
            // signifying that the last bit was omitted.

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
        /// Encode the segments { tag, length, value } of an octet string (byte array).
        /// </summary>
        /// <param name="data">The data to encode</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeOctetString(byte[] data)
        {
            Debug.Assert(data != null);

            // Because this is not currently public API the data array is not being cloned.
            return new byte[][]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.OctetString }, 
                EncodeLength(data.Length),
                data,
            };
        }

        internal static byte[][] SegmentedEncodeNull()
        {
            return s_nullTlv;
        }

        /// <summary>
        /// Encode an object identifier (Oid).
        /// </summary>
        /// <returns>The encoded OID</returns>
        internal static byte[] EncodeOid(string oidValue)
        {
            return ConcatenateArrays(SegmentedEncodeOid(oidValue));
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of an object identifier (Oid).
        /// </summary>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeOid(Oid oid)
        {
            Debug.Assert(oid != null);

            // All exceptions past this point should just be "CryptographicException", because that's
            // how they'd come back from Desktop/Windows, since it was a non-success result of calling
            // CryptEncodeObject.
            string oidValue = oid.Value;

            return SegmentedEncodeOid(oidValue);
        }

        /// <summary>
        /// Encode the segments { tag, length, value } of an object identifier (Oid).
        /// </summary>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeOid(string oidValue)
        {
            // All exceptions herein should just be "CryptographicException", because that's
            // how they'd come back from Desktop/Windows, since it was a non-success result of calling
            // CryptEncodeObject.

            if (string.IsNullOrEmpty(oidValue))
                throw new CryptographicException(SR.Argument_InvalidOidValue);
            if (oidValue.Length < 3 /* "1.1" is the shortest value */)
                throw new CryptographicException(SR.Argument_InvalidOidValue);
            if (oidValue[1] != '.')
                throw new CryptographicException(SR.Argument_InvalidOidValue);

            int firstRid;

            switch (oidValue[0])
            {
                case '0':
                    firstRid = 0;
                    break;
                case '1':
                    firstRid = 1;
                    break;
                case '2':
                    firstRid = 2;
                    break;
                default:
                    throw new CryptographicException(SR.Argument_InvalidOidValue);
            }

            int startPos = 2;

            // The first two RIDs are special:
            // ITU X.690 8.19.4:
            //   The numerical value of the first subidentifier is derived from the values of the first two
            //   object identifier components in the object identifier value being encoded, using the formula:
            //       (X*40) + Y
            //   where X is the value of the first object identifier component and Y is the value of the
            //   second object identifier component.
            //       NOTE – This packing of the first two object identifier components recognizes that only
            //          three values are allocated from the root node, and at most 39 subsequent values from
            //          nodes reached by X = 0 and X = 1.

            BigInteger rid = ParseOidRid(oidValue, ref startPos);
            rid += 40 * firstRid;

            // The worst case is "1.1.1.1.1", which takes 4 bytes (5 rids, with the first two condensed)
            // Longer numbers get smaller: "2.1.127" is only 2 bytes. (81d (0x51) and 127 (0x7F))
            // So length / 2 should prevent any reallocations.
            List<byte> encodedBytes = new List<byte>(oidValue.Length / 2);

            EncodeRid(encodedBytes, ref rid);

            while (startPos < oidValue.Length)
            {
                rid = ParseOidRid(oidValue, ref startPos);

                EncodeRid(encodedBytes, ref rid);
            }

            return new byte[][]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.ObjectIdentifier }, 
                EncodeLength(encodedBytes.Count),
                encodedBytes.ToArray(),
            };
        }

        /// <summary>
        /// Encode a character string as a UTF8String value.
        /// </summary>
        /// <param name="chars">The characters to be encoded.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeUtf8String(char[] chars)
        {
            Debug.Assert(chars != null);

            return SegmentedEncodeUtf8String(chars, 0, chars.Length);
        }

        /// <summary>
        /// Encode a substring as a UTF8String value.
        /// </summary>
        /// <param name="chars">The characters whose substring is to be encoded.</param>
        /// <param name="offset">The character offset into <paramref name="chars"/> at which to start.</param>
        /// <param name="count">The total number of characters from <paramref name="chars"/> to read.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeUtf8String(char[] chars, int offset, int count)
        {
            Debug.Assert(chars != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(offset <= chars.Length);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= chars.Length - offset);

            // ITU-T X.690 says that ISO/IEC 10646 Annex D should be used; but no announcers
            // or escape sequences, and each character shall be encoded in the smallest number of
            // bytes possible.
            //
            // Thankfully, that's just Encoding.UTF8.

            byte[] encodedBytes = System.Text.Encoding.UTF8.GetBytes(chars, offset, count);

            return new byte[][]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.UTF8String },
                EncodeLength(encodedBytes.Length),
                encodedBytes,
            };
        }

        /// <summary>
        /// Make a constructed SEQUENCE of the byte-triplets of the contents, but leave
        /// the value in a segmented form (to be included in a larger SEQUENCE).
        /// </summary>
        /// <param name="items">Series of Tag-Length-Value triplets to build into one sequence.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] ConstructSegmentedSequence(params byte[][][] items)
        {
            return ConstructSegmentedSequence((IEnumerable<byte[][]>)items);
        }

        internal static byte[][] ConstructSegmentedSequence(IEnumerable<byte[][]> items)
        {
            Debug.Assert(items != null);

            byte[] data = ConcatenateArrays(items);

            return new byte[][]
            {
                new byte[] { ConstructedSequenceTag }, 
                EncodeLength(data.Length),
                data,
            };
        }

        /// <summary>
        /// Make a context-specific tagged value which is constructed of other DER encoded values.
        /// Logically the same as a SEQUENCE, but providing context as to data interpretation (and usually
        /// indicates an optional element adjacent to another SEQUENCE).
        /// </summary>
        /// <param name="contextId">The value's context ID</param>
        /// <param name="items">Series of Tag-Length-Value triplets to build into one sequence.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] ConstructSegmentedContextSpecificValue(int contextId, params byte[][][] items)
        {
            Debug.Assert(items != null);
            Debug.Assert(contextId >= 0 && contextId <= 30);

            byte[] data = ConcatenateArrays(items);

            byte tagId = (byte)(
                DerSequenceReader.ConstructedFlag |
                DerSequenceReader.ContextSpecificTagFlag |
                contextId);

            return new byte[][]
            {
                new byte[] { tagId },
                EncodeLength(data.Length),
                data,
            };
        }

        /// <summary>
        /// Make a constructed SET of the byte-triplets of the contents, but leave
        /// the value in a segmented form (to be included in a larger SEQUENCE).
        /// </summary>
        /// <param name="items">Series of Tag-Length-Value triplets to build into one set.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] ConstructSegmentedSet(params byte[][][] items)
        {
            Debug.Assert(items != null);

            byte[][][] sortedItems = (byte[][][])items.Clone();
            Array.Sort(sortedItems, AsnSetValueComparer.Instance);
            byte[] data = ConcatenateArrays(sortedItems);

            return new byte[][]
            {
                new byte[] { ConstructedSetTag },
                EncodeLength(data.Length),
                data,
            };
        }

        /// <summary>
        /// Make a constructed SET of the byte-triplets of the contents, but leave
        /// the value in a segmented form (to be included in a larger SEQUENCE).
        /// 
        /// This method assumes that the data is presorted, and writes it as-is.
        /// </summary>
        /// <param name="items">Series of Tag-Length-Value triplets to build into one set.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] ConstructSegmentedPresortedSet(params byte[][][] items)
        {
            Debug.Assert(items != null);

            byte[] data = ConcatenateArrays(items);

            return new byte[][]
            {
                new byte[] { ConstructedSetTag },
                EncodeLength(data.Length),
                data,
            };
        }

        /// <summary>
        /// Test to see if the input characters contains only characters permitted by the ASN.1
        /// PrintableString restricted character set.
        /// </summary>
        /// <param name="chars">The characters to test.</param>
        /// <returns>
        /// <c>true</c> if all of the characters in <paramref name="chars"/> are valid PrintableString characters,
        /// <c>false</c> otherwise.
        /// </returns>
        internal static bool IsValidPrintableString(char[] chars)
        {
            Debug.Assert(chars != null);

            return IsValidPrintableString(chars, 0, chars.Length);
        }

        /// <summary>
        /// Test to see if the input substring contains only characters permitted by the ASN.1
        /// PrintableString restricted character set.
        /// </summary>
        /// <param name="chars">The character string to test.</param>
        /// <param name="offset">The starting character position within <paramref name="chars"/>.</param>
        /// <param name="count">The number of characters from <paramref name="chars"/> to read.</param>
        /// <returns>
        /// <c>true</c> if all of the indexed characters in <paramref name="chars"/> are valid PrintableString
        /// characters, <c>false</c> otherwise.
        /// </returns>
        internal static bool IsValidPrintableString(char[] chars, int offset, int count)
        {
            Debug.Assert(chars != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(offset <= chars.Length);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= chars.Length - offset);

            int end = count + offset;

            for (int i = offset; i < end; i++)
            {
                if (!IsPrintableStringCharacter(chars[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Encode a character string as a PrintableString value.
        /// </summary>
        /// <param name="chars">The characters to be encoded.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodePrintableString(char[] chars)
        {
            Debug.Assert(chars != null);

            return SegmentedEncodePrintableString(chars, 0, chars.Length);
        }

        /// <summary>
        /// Encode a substring as a PrintableString value.
        /// </summary>
        /// <param name="chars">The character string whose substring is to be encoded.</param>
        /// <param name="offset">The character offset into <paramref name="chars"/> at which to start.</param>
        /// <param name="count">The total number of characters from <paramref name="chars"/> to read.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodePrintableString(char[] chars, int offset, int count)
        {
            Debug.Assert(chars != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(offset <= chars.Length);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= chars.Length);
            Debug.Assert(offset + count <= chars.Length);

            Debug.Assert(IsValidPrintableString(chars, offset, count));

            // ITU-T X.690 (08/2015) 8.23.5 says to encode PrintableString as ISO/IEC 2022 using an implicit
            // context of G0=6 (ANSI/ASCII) and no explicit escape sequences are allowed.
            //
            // That's a long-winded way of saying that it's just ASCII.  Since we've already established
            // that it fits the PrintableString character restrictions, we can just convert char to byte.
            byte[] encodedString = new byte[count];

            for (int i = 0; i < count; i++)
            {
                encodedString[i] = (byte)chars[i + offset];
            }

            return new byte[][]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.PrintableString },
                EncodeLength(encodedString.Length),
                encodedString,
            };
        }

        /// <summary>
        /// Encode a string of characters as a IA5String value.
        /// </summary>
        /// <param name="chars">The characters to be encoded.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeIA5String(char[] chars)
        {
            Debug.Assert(chars != null);

            return SegmentedEncodeIA5String(chars, 0, chars.Length);
        }

        /// <summary>
        /// Encode a substring as a IA5String value.
        /// </summary>
        /// <param name="chars">The characters whose substring is to be encoded.</param>
        /// <param name="offset">The character offset into <paramref name="chars"/> at which to start.</param>
        /// <param name="count">The total number of characters from <paramref name="chars"/> to read.</param>
        /// <returns>The encoded segments { tag, length, value }</returns>
        internal static byte[][] SegmentedEncodeIA5String(char[] chars, int offset, int count)
        {
            Debug.Assert(chars != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(offset <= chars.Length);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= chars.Length);
            Debug.Assert(offset + count <= chars.Length);

            // ITU-T X.690 (08/2015) 8.23.5 says to encode IA5String as ISO/IEC 2022 using an implicit
            // context of G0=6 (ANSI/ASCII) and no explicit escape sequences are allowed.
            //
            // That's a long-winded way of saying that it's just ASCII 0x00-0x7F.
            byte[] encodedString = new byte[count];

            for (int i = 0; i < count; i++)
            {
                char c = chars[i + offset];

                // IA5 is ASCII 0x00-0x7F.
                if (c > 127)
                {
                    throw new CryptographicException(SR.Cryptography_Invalid_IA5String);
                }

                encodedString[i] = (byte)c;
            }

            return new byte[][]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.IA5String },
                EncodeLength(encodedString.Length),
                encodedString,
            };
        }

        internal static byte[][] SegmentedEncodeUtcTime(DateTime utcTime)
        {
            Debug.Assert(utcTime.Kind == DateTimeKind.Utc);

            // Low-allocating encoding of YYMMDDHHmmssZ
            byte[] asciiDateBytes = new byte[13];

            int year = utcTime.Year;
            int month = utcTime.Month;
            int day = utcTime.Day;
            int hour = utcTime.Hour;
            int minute = utcTime.Minute;
            int second = utcTime.Second;

            const byte Char0 = (byte)'0';

            asciiDateBytes[1] = (byte)(Char0 + (year % 10));
            year /= 10;
            asciiDateBytes[0] = (byte)(Char0 + (year % 10));

            asciiDateBytes[3] = (byte)(Char0 + (month % 10));
            month /= 10;
            asciiDateBytes[2] = (byte)(Char0 + (month % 10));

            asciiDateBytes[5] = (byte)(Char0 + (day % 10));
            day /= 10;
            asciiDateBytes[4] = (byte)(Char0 + (day % 10));

            asciiDateBytes[7] = (byte)(Char0 + (hour % 10));
            hour /= 10;
            asciiDateBytes[6] = (byte)(Char0 + (hour % 10));

            asciiDateBytes[9] = (byte)(Char0 + (minute % 10));
            minute /= 10;
            asciiDateBytes[8] = (byte)(Char0 + (minute % 10));

            asciiDateBytes[11] = (byte)(Char0 + (second % 10));
            second /= 10;
            asciiDateBytes[10] = (byte)(Char0 + (second % 10));

            asciiDateBytes[12] = (byte)'Z';

            return new[]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.UTCTime },
                EncodeLength(asciiDateBytes.Length),
                asciiDateBytes,
            };
        }

        internal static byte[][] SegmentedEncodeGeneralizedTime(DateTime utcTime)
        {
            Debug.Assert(utcTime.Kind == DateTimeKind.Utc);

            // Low-allocating encoding of YYYYMMDDHHmmssZ
            byte[] asciiDateBytes = new byte[15];

            int year = utcTime.Year;
            int month = utcTime.Month;
            int day = utcTime.Day;
            int hour = utcTime.Hour;
            int minute = utcTime.Minute;
            int second = utcTime.Second;

            const byte Char0 = (byte)'0';

            asciiDateBytes[3] = (byte)(Char0 + (year % 10));
            year /= 10;
            asciiDateBytes[2] = (byte)(Char0 + (year % 10));
            year /= 10;
            asciiDateBytes[1] = (byte)(Char0 + (year % 10));
            year /= 10;
            asciiDateBytes[0] = (byte)(Char0 + (year % 10));

            asciiDateBytes[5] = (byte)(Char0 + (month % 10));
            month /= 10;
            asciiDateBytes[4] = (byte)(Char0 + (month % 10));

            asciiDateBytes[7] = (byte)(Char0 + (day % 10));
            day /= 10;
            asciiDateBytes[6] = (byte)(Char0 + (day % 10));

            asciiDateBytes[9] = (byte)(Char0 + (hour % 10));
            hour /= 10;
            asciiDateBytes[8] = (byte)(Char0 + (hour % 10));

            asciiDateBytes[11] = (byte)(Char0 + (minute % 10));
            minute /= 10;
            asciiDateBytes[10] = (byte)(Char0 + (minute % 10));

            asciiDateBytes[13] = (byte)(Char0 + (second % 10));
            second /= 10;
            asciiDateBytes[12] = (byte)(Char0 + (second % 10));

            asciiDateBytes[14] = (byte)'Z';

            return new[]
            {
                new byte[] { (byte)DerSequenceReader.DerTag.GeneralizedTime },
                EncodeLength(asciiDateBytes.Length),
                asciiDateBytes,
            };
        }

        /// <summary>
        /// Make a constructed SEQUENCE of the byte-triplets of the contents.
        /// Each byte[][] should be a byte[][3] of {tag (1 byte), length (1-5 bytes), payload (variable)}.
        /// </summary>
        internal static byte[] ConstructSequence(params byte[][][] items)
        {
            return ConstructSequence((IEnumerable<byte[][]>)items);
        }

        /// <summary>
        /// Make a constructed SEQUENCE of the byte-triplets of the contents.
        /// Each byte[][] should be a byte[][3] of {tag (1 byte), length (1-5 bytes), payload (variable)}.
        /// </summary>
        internal static byte[] ConstructSequence(IEnumerable<byte[][]> items)
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

        private static BigInteger ParseOidRid(string oidValue, ref int startIndex)
        {
            Debug.Assert(startIndex < oidValue.Length);

            int endIndex = oidValue.IndexOf('.', startIndex);
            
            if (endIndex == -1)
            {
                endIndex = oidValue.Length;
            }

            Debug.Assert(endIndex > startIndex);

            // The following code is equivalent to
            // BigInteger.TryParse(temp, NumberStyles.None, CultureInfo.InvariantCulture, out value)
            // but doesn't require creating the intermediate substring (TryParse doesn't take start+end/length values)

            BigInteger value = BigInteger.Zero;

            for (int position = startIndex; position < endIndex; position++)
            {
                value *= 10;
                value += AtoI(oidValue[position]);
            }

            startIndex = endIndex + 1;
            return value;
        }

        private static int AtoI(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';

            throw new CryptographicException(SR.Argument_InvalidOidValue);
        }

        private static void EncodeRid(List<byte> encodedData, ref BigInteger rid)
        {
            BigInteger divisor = new BigInteger(128);
            BigInteger unencoded = rid;

            // The encoding is 7 bits of value and the 8th bit signifies "keep reading".
            // So, for the input 1079 (0b0000 0100 0011 0111) the partitioning is
            // 00|00 0100 0|011 0111, and the leading two bits are ignored.
            // Therefore the encoded is 0b1000 1000 0011 0111 = 0x8837

            Stack<byte> littleEndianBytes = new Stack<byte>();
            byte continuance = 0;

            do
            {
                BigInteger remainder;
                unencoded = BigInteger.DivRem(unencoded, divisor, out remainder);

                byte octet = (byte)remainder;
                octet |= continuance;

                // Any remaining (preceding) bytes need the continuance bit set.
                continuance = 0x80;
                littleEndianBytes.Push(octet);
            }
            while (unencoded != BigInteger.Zero);

            encodedData.AddRange(littleEndianBytes);
        }

        private static bool IsPrintableStringCharacter(char c)
        {
            // ITU-T X.680 (11/2008)
            // 41.4 (PrintableString)

            // Latin Capital, Latin Small, Digits
            if ((c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9'))
            {
                return true;
            }

            // Individually included characters
            switch (c)
            {
                case ' ':
                case '\'':
                case '(':
                case ')':
                case '+':
                case ',':
                case '-':
                case '.':
                case '/':
                case ':':
                case '=':
                case '?':
                    return true;
            }

            return false;
        }

        private static byte[] ConcatenateArrays(params byte[][][] segments)
        {
            return ConcatenateArrays((IEnumerable<byte[][]>)segments);
        }

        private static byte[] ConcatenateArrays(IEnumerable<byte[][]> segments)
        {
            int length = 0;

            foreach (byte[][] middleSegments in segments)
            {
                foreach (byte[] segment in middleSegments)
                {
                    length += segment.Length;
                }
            }

            byte[] concatenated = new byte[length];

            int offset = 0;

            foreach (byte[][] middleSegments in segments)
            {
                foreach (byte[] segment in middleSegments)
                {
                    Buffer.BlockCopy(segment, 0, concatenated, offset, segment.Length);
                    offset += segment.Length;
                }
            }

            return concatenated;
        }

        private class AsnSetValueComparer : IComparer<byte[][]>, IComparer
        {
            public static AsnSetValueComparer Instance { get; } = new AsnSetValueComparer();

            public int Compare(byte[][] x, byte[][] y)
            {
                Debug.Assert(x != null && y != null, "Comparing one more more null values");
                Debug.Assert(x.Length == 3, $"x.Length is {x.Length} when it should be 3");
                Debug.Assert(y.Length == 3, $"y.Length is {y.Length} when it should be 3");

                // ITU-T X.690 11.6 Set-of components (CER/DER)
                // The encodings of the component values of a set-of value shall appear in
                // ascending order, the encodings being compared as octet strings with the
                // shorter components being padded at their trailing end with 0-octets.
                //
                // ITU-T X.690 10.3 Set components (DER)
                // The encodings of the component values of a set value shall appear in an
                // order determined by their tags as specified in 8.6 of
                // Rec. ITU-T X.680 | ISO/IEC 8824-1.
                //
                // ITU-T X.680 8.6
                // The canonical order for tags is based on the outermost tag of each type
                // and is defined as follows:
                // a) those elements or alternatives with universal class tags shall appear
                //    first, followed by those with application class tags, followed by those
                //    with context-specific tags, followed by those with private class tags;
                // b) within each class of tags, the elements or alternatives shall appear
                //    in ascending order of their tag numbers.
                //
                // ITU-T X.690 8.1.2.2 says:
                //   Universal:        0b00xxxxxx
                //   Application:      0b01xxxxxx
                //   Context-Specific: 0b10xxxxxx
                //   Private:          0b11xxxxxx
                //
                // The net result is that the DER order is just lexicographic ordering by
                // the TLV (tag-length-value) encoding. The comment in 11.6 about treating
                // a shorter value as if it was right-padded with zeroes doesn't apply to DER,
                // due to the length encoding. But for indefinite-length CER it would.
                Debug.Assert(x[0].Length == 1, $"x[0].Length is {x[0].Length} when it should be 1");
                Debug.Assert(y[0].Length == 1, $"y[0].Length is {y[0].Length} when it should be 1");

                int comparison = x[0][0] - y[0][0];

                if (comparison != 0)
                    return comparison;

                // The length encoding will always sort lexicographically based on its value, due to the
                // length-or-length-length first byte. So skip over the [1] values and compare [2].Length.
                comparison = x[2].Length - y[2].Length;

                if (comparison != 0)
                    return comparison;

                for (int i = 0; i < x[2].Length; i++)
                {
                    comparison = x[2][i] - y[2][i];

                    if (comparison != 0)
                    {
                        return comparison;
                    }
                }

                return 0;
            }

            public int Compare(object x, object y)
            {
                return Compare(x as byte[][], y as byte[][]);
            }
        }
    }
}
