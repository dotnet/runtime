// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Enable CHECK_ACCURATE_ENSURE to ensure that the AsnWriter is not ever
// abusing the normal EnsureWriteCapacity + ArrayPool behaviors of rounding up.
//#define CHECK_ACCURATE_ENSURE

using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    internal sealed class AsnWriter : IDisposable
    {
        private byte[] _buffer;
        private int _offset;
        private Stack<(Asn1Tag,int)> _nestingStack;

        public AsnEncodingRules RuleSet { get; }

        public AsnWriter(AsnEncodingRules ruleSet)
        {
            if (ruleSet != AsnEncodingRules.BER &&
                ruleSet != AsnEncodingRules.CER &&
                ruleSet != AsnEncodingRules.DER)
            {
                throw new ArgumentOutOfRangeException(nameof(ruleSet));
            }

            RuleSet = ruleSet;
        }

        public void Dispose()
        {
            _nestingStack = null;

            if (_buffer != null)
            {
                Array.Clear(_buffer, 0, _offset);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
                _offset = 0;
            }
        }

        private void EnsureWriteCapacity(int pendingCount)
        {
            if (pendingCount < 0)
            {
                throw new OverflowException();
            }

            if (_buffer == null || _buffer.Length - _offset < pendingCount)
            {
#if CHECK_ACCURATE_ENSURE
// A debug paradigm to make sure that throughout the execution nothing ever writes
// past where the buffer was "allocated".  This causes quite a number of reallocs
// and copies, so it's a #define opt-in.
                byte[] newBytes = new byte[_offset + pendingCount];

                if (_buffer != null)
                {
                    Buffer.BlockCopy(_buffer, 0, newBytes, 0, _offset);
                }
#else
                const int BlockSize = 1024;
                // While the ArrayPool may have similar logic, make sure we don't run into a lot of
                // "grow a little" by asking in 1k steps.
                int blocks = checked(_offset + pendingCount + (BlockSize - 1)) / BlockSize;
                byte[] newBytes = ArrayPool<byte>.Shared.Rent(BlockSize * blocks);

                if (_buffer != null)
                {
                    Buffer.BlockCopy(_buffer, 0, newBytes, 0, _offset);
                    Array.Clear(_buffer, 0, _offset);
                    ArrayPool<byte>.Shared.Return(_buffer);
                }
#endif

#if DEBUG
                // Ensure no "implicit 0" is happening
                for (int i = _offset; i < newBytes.Length; i++)
                {
                    newBytes[i] ^= 0xFF;
                }
#endif

                _buffer = newBytes;
            }
        }

        private void WriteTag(Asn1Tag tag)
        {
            int spaceRequired = tag.CalculateEncodedSize();
            EnsureWriteCapacity(spaceRequired);

            if (!tag.TryWrite(_buffer.AsSpan(_offset, spaceRequired), out int written) ||
                written != spaceRequired)
            {
                Debug.Fail($"TryWrite failed or written was wrong value ({written} vs {spaceRequired})");
                throw new CryptographicException();
            }

            _offset += spaceRequired;
        }

        // T-REC-X.690-201508 sec 8.1.3
        private void WriteLength(int length)
        {
            const byte MultiByteMarker = 0x80;
            Debug.Assert(length >= -1);

            // If the indefinite form has been requested.
            // T-REC-X.690-201508 sec 8.1.3.6
            if (length == -1)
            {
                EnsureWriteCapacity(1);
                _buffer[_offset] = MultiByteMarker;
                _offset++;
                return;
            }

            Debug.Assert(length >= 0);

            // T-REC-X.690-201508 sec 8.1.3.3, 8.1.3.4
            if (length < MultiByteMarker)
            {
                // Pre-allocate the pending data since we know how much.
                EnsureWriteCapacity(1 + length);
                _buffer[_offset] = (byte)length;
                _offset++;
                return;
            }

            // The rest of the method implements T-REC-X.680-201508 sec 8.1.3.5
            int lengthLength = GetEncodedLengthSubsequentByteCount(length);

            // Pre-allocate the pending data since we know how much.
            EnsureWriteCapacity(lengthLength + 1 + length);
            _buffer[_offset] = (byte)(MultiByteMarker | lengthLength);

            // No minus one because offset didn't get incremented yet.
            int idx = _offset + lengthLength;

            int remaining = length;

            do
            {
                _buffer[idx] = (byte)remaining;
                remaining >>= 8;
                idx--;
            } while (remaining > 0);

            Debug.Assert(idx == _offset);
            _offset += lengthLength + 1;
        }

        // T-REC-X.690-201508 sec 8.1.3.5
        private static int GetEncodedLengthSubsequentByteCount(int length)
        {
            if (length <= 0x7F)
                return 0;
            if (length <= byte.MaxValue)
                return 1;
            if (length <= ushort.MaxValue)
                return 2;
            if (length <= 0x00FFFFFF)
                return 3;

            return 4;
        }

        public void WriteEncodedValue(ReadOnlyMemory<byte> preEncodedValue)
        {
            AsnReader reader = new AsnReader(preEncodedValue, RuleSet);

            // Is it legal under the current rules?
            ReadOnlyMemory<byte> parsedBack = reader.GetEncodedValue();

            if (reader.HasData)
            {
                throw new ArgumentException(SR.Cryptography_WriteEncodedValue_OneValueAtATime, nameof(preEncodedValue));
            }

            Debug.Assert(parsedBack.Length == preEncodedValue.Length);

            EnsureWriteCapacity(preEncodedValue.Length);
            preEncodedValue.Span.CopyTo(_buffer.AsSpan(_offset));
            _offset += preEncodedValue.Length;
        }

        // T-REC-X.690-201508 sec 8.1.5
        private void WriteEndOfContents()
        {
            EnsureWriteCapacity(2);
            _buffer[_offset++] = 0;
            _buffer[_offset++] = 0;
        }

        public void WriteBoolean(bool value)
        {
            WriteBooleanCore(Asn1Tag.Boolean, value);
        }

        public void WriteBoolean(Asn1Tag tag, bool value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Boolean);

            WriteBooleanCore(tag.AsPrimitive(), value);
        }

        // T-REC-X.690-201508 sec 11.1, 8.2
        private void WriteBooleanCore(Asn1Tag tag, bool value)
        {
            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(1);
            // Ensured by WriteLength
            Debug.Assert(_offset < _buffer.Length);
            _buffer[_offset] = (byte)(value ? 0xFF : 0x00);
            _offset++;
        }

        public void WriteInteger(long value)
        {
            WriteIntegerCore(Asn1Tag.Integer, value);
        }

        public void WriteInteger(ulong value)
        {
            WriteNonNegativeIntegerCore(Asn1Tag.Integer, value);
        }

        public void WriteInteger(BigInteger value)
        {
            WriteIntegerCore(Asn1Tag.Integer, value);
        }

        public void WriteInteger(ReadOnlySpan<byte> value)
        {
            WriteIntegerCore(Asn1Tag.Integer, value);
        }

        public void WriteIntegerUnsigned(ReadOnlySpan<byte> value)
        {
            WriteIntegerUnsignedCore(Asn1Tag.Integer, value);
        }

        public void WriteInteger(Asn1Tag tag, long value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Integer);

            WriteIntegerCore(tag.AsPrimitive(), value);
        }

        // T-REC-X.690-201508 sec 8.3
        private void WriteIntegerCore(Asn1Tag tag, long value)
        {
            if (value >= 0)
            {
                WriteNonNegativeIntegerCore(tag, (ulong)value);
                return;
            }

            int valueLength;

            if (value >= sbyte.MinValue)
                valueLength = 1;
            else if (value >= short.MinValue)
                valueLength = 2;
            else if (value >= unchecked((long)0xFFFFFFFF_FF800000))
                valueLength = 3;
            else if (value >= int.MinValue)
                valueLength = 4;
            else if (value >= unchecked((long)0xFFFFFF80_00000000))
                valueLength = 5;
            else if (value >= unchecked((long)0xFFFF8000_00000000))
                valueLength = 6;
            else if (value >= unchecked((long)0xFF800000_00000000))
                valueLength = 7;
            else
                valueLength = 8;

            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(valueLength);

            long remaining = value;
            int idx = _offset + valueLength - 1;

            do
            {
                _buffer[idx] = (byte)remaining;
                remaining >>= 8;
                idx--;
            } while (idx >= _offset);

#if DEBUG
            if (valueLength > 1)
            {
                // T-REC-X.690-201508 sec 8.3.2
                // Cannot start with 9 bits of 1 (or 9 bits of 0, but that's not this method).
                Debug.Assert(_buffer[_offset] != 0xFF || _buffer[_offset + 1] < 0x80);
            }
#endif

            _offset += valueLength;
        }

        public void WriteInteger(Asn1Tag tag, ulong value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Integer);

            WriteNonNegativeIntegerCore(tag.AsPrimitive(), value);
        }

        // T-REC-X.690-201508 sec 8.3
        private void WriteNonNegativeIntegerCore(Asn1Tag tag, ulong value)
        {
            int valueLength;

            // 0x80 needs two bytes: 0x00 0x80
            if (value < 0x80)
                valueLength = 1;
            else if (value < 0x8000)
                valueLength = 2;
            else if (value < 0x800000)
                valueLength = 3;
            else if (value < 0x80000000)
                valueLength = 4;
            else if (value < 0x80_00000000)
                valueLength = 5;
            else if (value < 0x8000_00000000)
                valueLength = 6;
            else if (value < 0x800000_00000000)
                valueLength = 7;
            else if (value < 0x80000000_00000000)
                valueLength = 8;
            else
                valueLength = 9;

            // Clear the constructed bit, if it was set.
            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(valueLength);

            ulong remaining = value;
            int idx = _offset + valueLength - 1;

            do
            {
                _buffer[idx] = (byte)remaining;
                remaining >>= 8;
                idx--;
            } while (idx >= _offset);

#if DEBUG
            if (valueLength > 1)
            {
                // T-REC-X.690-201508 sec 8.3.2
                // Cannot start with 9 bits of 0 (or 9 bits of 1, but that's not this method).
                Debug.Assert(_buffer[_offset] != 0 || _buffer[_offset + 1] > 0x7F);
            }
#endif

            _offset += valueLength;
        }

        public void WriteInteger(Asn1Tag tag, BigInteger value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Integer);

            WriteIntegerCore(tag.AsPrimitive(), value);
        }

        public void WriteInteger(Asn1Tag tag, ReadOnlySpan<byte> value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Integer);

            WriteIntegerCore(tag.AsPrimitive(), value);
        }

        public void WriteIntegerUnsigned(Asn1Tag tag, ReadOnlySpan<byte> value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Integer);

            WriteIntegerUnsignedCore(tag.AsPrimitive(), value);
        }

        private void WriteIntegerUnsignedCore(Asn1Tag tag, ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // T-REC-X.690-201508 sec 8.3.2
            if (value.Length > 1 && value[0] == 0 && value[1] < 0x80)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);

            if (value[0] >= 0x80)
            {
                WriteLength(checked(value.Length + 1));
                _buffer[_offset] = 0;
                _offset++;
            }
            else
            {
                WriteLength(value.Length);
            }

            value.CopyTo(_buffer.AsSpan(_offset));
            _offset += value.Length;
        }

        private void WriteIntegerCore(Asn1Tag tag, ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // T-REC-X.690-201508 sec 8.3.2
            if (value.Length > 1)
            {
                ushort bigEndianValue = (ushort)(value[0] << 8 | value[1]);
                const ushort RedundancyMask = 0b1111_1111_1000_0000;
                ushort masked = (ushort)(bigEndianValue & RedundancyMask);

                // If the first 9 bits are all 0 or are all 1, the value is invalid.
                if (masked == 0 || masked == RedundancyMask)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(value.Length);
            // WriteLength ensures the content-space
            value.CopyTo(_buffer.AsSpan(_offset));
            _offset += value.Length;
        }

        // T-REC-X.690-201508 sec 8.3
        private void WriteIntegerCore(Asn1Tag tag, BigInteger value)
        {
            // TODO: Split this for netstandard vs netcoreapp for span-perf?.
            byte[] encoded = value.ToByteArray();
            Array.Reverse(encoded);

            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(encoded.Length);
            Buffer.BlockCopy(encoded, 0, _buffer, _offset, encoded.Length);
            _offset += encoded.Length;
        }

        public void WriteBitString(ReadOnlySpan<byte> bitString, int unusedBitCount=0)
        {
            WriteBitStringCore(Asn1Tag.PrimitiveBitString, bitString, unusedBitCount);
        }

        public void WriteBitString(Asn1Tag tag, ReadOnlySpan<byte> bitString, int unusedBitCount=0)
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

            // T-REC-X.690-201508 sec 8.6.2.3
            if (bitString.Length == 0 && unusedBitCount != 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // If 3 bits are "unused" then build a mask for them to check for 0.
            // 1 << 3 => 0b0000_1000
            // subtract 1 => 0b000_0111
            int mask = (1 << unusedBitCount) - 1;
            byte lastByte = bitString.IsEmpty ? (byte)0 : bitString[bitString.Length - 1];

            if ((lastByte & mask) != 0)
            {
                // T-REC-X.690-201508 sec 11.2
                //
                // This could be ignored for BER, but since DER is more common and
                // it likely suggests a program error on the caller, leave it enabled for
                // BER for now.
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

        // T-REC-X.690-201508 sec 9.2, 8.6
        private void WriteConstructedCerBitString(Asn1Tag tag, ReadOnlySpan<byte> payload, int unusedBitCount)
        {
            const int MaxCERSegmentSize = AsnReader.MaxCERSegmentSize;
            // Every segment has an "unused bit count" byte.
            const int MaxCERContentSize = MaxCERSegmentSize - 1;
            Debug.Assert(payload.Length > MaxCERContentSize);

            WriteTag(tag.AsConstructed());
            // T-REC-X.690-201508 sec 9.1
            // Constructed CER uses the indefinite form.
            WriteLength(-1);

            int fullSegments = Math.DivRem(payload.Length, MaxCERContentSize, out int lastContentSize);

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
            int expectedSize = fullSegments * FullSegmentEncodedSize + remainingEncodedSize + 2;
            EnsureWriteCapacity(expectedSize);

            byte[] ensureNoExtraCopy = _buffer;
            int savedOffset = _offset;

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

        public void WriteNamedBitList(object enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            WriteNamedBitList(Asn1Tag.PrimitiveBitString, enumValue);
        }

        public void WriteNamedBitList<TEnum>(TEnum enumValue) where TEnum : struct
        {
            WriteNamedBitList(Asn1Tag.PrimitiveBitString, enumValue);
        }

        public void WriteNamedBitList(Asn1Tag tag, object enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            WriteNamedBitList(tag, enumValue.GetType(), enumValue);
        }

        public void WriteNamedBitList<TEnum>(Asn1Tag tag, TEnum enumValue) where TEnum : struct
        {
            WriteNamedBitList(tag, typeof(TEnum), enumValue);
        }

        private void WriteNamedBitList(Asn1Tag tag, Type tEnum, object enumValue)
        {
            Type backingType = tEnum.GetEnumUnderlyingType();

            if (!tEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(SR.Cryptography_Asn_NamedBitListRequiresFlagsEnum, nameof(tEnum));
            }

            ulong integralValue;

            if (backingType == typeof(ulong))
            {
                integralValue = Convert.ToUInt64(enumValue);
            }
            else
            {
                // All other types fit in a (signed) long.
                long numericValue = Convert.ToInt64(enumValue);
                integralValue = unchecked((ulong)numericValue);
            }

            WriteNamedBitList(tag, integralValue);
        }

        // T-REC-X.680-201508 sec 22
        // T-REC-X.690-201508 sec 8.6, 11.2.2
        private void WriteNamedBitList(Asn1Tag tag, ulong integralValue)
        {
            Span<byte> temp = stackalloc byte[sizeof(ulong)];
            // Reset to all zeros, since we're just going to or-in bits we need.
            temp.Clear();

            int indexOfHighestSetBit = -1;

            for (int i = 0; integralValue != 0; integralValue >>= 1, i++)
            {
                if ((integralValue & 1) != 0)
                {
                    temp[i / 8] |= (byte)(0x80 >> (i % 8));
                    indexOfHighestSetBit = i;
                }
            }

            if (indexOfHighestSetBit < 0)
            {
                // No bits were set; this is an empty bit string.
                // T-REC-X.690-201508 sec 11.2.2-note2
                WriteBitString(tag, ReadOnlySpan<byte>.Empty);
            }
            else
            {
                // At least one bit was set.
                // Determine the shortest length necessary to represent the bit string.

                // Since "bit 0" gets written down 0 => 1.
                // Since "bit 8" is in the second byte 8 => 2.
                // That makes the formula ((bit / 8) + 1) instead of ((bit + 7) / 8).
                int byteLen = (indexOfHighestSetBit / 8) + 1;
                int unusedBitCount = 7 - (indexOfHighestSetBit % 8);

                WriteBitString(
                    tag,
                    temp.Slice(0, byteLen),
                    unusedBitCount);
            }
        }

        public void WriteOctetString(ReadOnlySpan<byte> octetString)
        {
            WriteOctetString(Asn1Tag.PrimitiveOctetString, octetString);
        }

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

        public void WriteNull()
        {
            WriteNullCore(Asn1Tag.Null);
        }

        public void WriteNull(Asn1Tag tag)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Null);

            WriteNullCore(tag.AsPrimitive());
        }

        // T-REC-X.690-201508 sec 8.8
        private void WriteNullCore(Asn1Tag tag)
        {
            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(0);
        }

        public void WriteObjectIdentifier(Oid oid)
        {
            if (oid == null)
                throw new ArgumentNullException(nameof(oid));
            if (oid.Value == null)
                throw new CryptographicException(SR.Argument_InvalidOidValue);

            WriteObjectIdentifier(oid.Value);
        }

        public void WriteObjectIdentifier(string oidValue)
        {
            if (oidValue == null)
                throw new ArgumentNullException(nameof(oidValue));

            WriteObjectIdentifier(oidValue.AsSpan());
        }

        public void WriteObjectIdentifier(ReadOnlySpan<char> oidValue)
        {
            WriteObjectIdentifierCore(Asn1Tag.ObjectIdentifier, oidValue);
        }

        public void WriteObjectIdentifier(Asn1Tag tag, Oid oid)
        {
            if (oid == null)
                throw new ArgumentNullException(nameof(oid));
            if (oid.Value == null)
                throw new CryptographicException(SR.Argument_InvalidOidValue);

            WriteObjectIdentifier(tag, oid.Value);
        }

        public void WriteObjectIdentifier(Asn1Tag tag, string oidValue)
        {
            if (oidValue == null)
                throw new ArgumentNullException(nameof(oidValue));

            WriteObjectIdentifier(tag, oidValue.AsSpan());
        }

        public void WriteObjectIdentifier(Asn1Tag tag, ReadOnlySpan<char> oidValue)
        {
            CheckUniversalTag(tag, UniversalTagNumber.ObjectIdentifier);

            WriteObjectIdentifierCore(tag.AsPrimitive(), oidValue);
        }

        // T-REC-X.690-201508 sec 8.19
        private void WriteObjectIdentifierCore(Asn1Tag tag, ReadOnlySpan<char> oidValue)
        {
            // T-REC-X.690-201508 sec 8.19.4
            // The first character is in { 0, 1, 2 }, the second will be a '.', and a third (digit)
            // will also exist.
            if (oidValue.Length < 3)
                throw new CryptographicException(SR.Argument_InvalidOidValue);
            if (oidValue[1] != '.')
                throw new CryptographicException(SR.Argument_InvalidOidValue);

            // The worst case is "1.1.1.1.1", which takes 4 bytes (5 components, with the first two condensed)
            // Longer numbers get smaller: "2.1.127" is only 2 bytes. (81d (0x51) and 127 (0x7F))
            // So length / 2 should prevent any reallocations.
            byte[] tmp = ArrayPool<byte>.Shared.Rent(oidValue.Length / 2);
            int tmpOffset = 0;

            try
            {
                int firstComponent;

                switch (oidValue[0])
                {
                    case '0':
                        firstComponent = 0;
                        break;
                    case '1':
                        firstComponent = 1;
                        break;
                    case '2':
                        firstComponent = 2;
                        break;
                    default:
                        throw new CryptographicException(SR.Argument_InvalidOidValue);
                }

                // The first two components are special:
                // ITU X.690 8.19.4:
                //   The numerical value of the first subidentifier is derived from the values of the first two
                //   object identifier components in the object identifier value being encoded, using the formula:
                //       (X*40) + Y
                //   where X is the value of the first object identifier component and Y is the value of the
                //   second object identifier component.
                //       NOTE - This packing of the first two object identifier components recognizes that only
                //          three values are allocated from the root node, and at most 39 subsequent values from
                //          nodes reached by X = 0 and X = 1.

                // skip firstComponent and the trailing .
                ReadOnlySpan<char> remaining = oidValue.Slice(2);

                BigInteger subIdentifier = ParseSubIdentifier(ref remaining);
                subIdentifier += 40 * firstComponent;

                int localLen = EncodeSubIdentifier(tmp.AsSpan(tmpOffset), ref subIdentifier);
                tmpOffset += localLen;

                while (!remaining.IsEmpty)
                {
                    subIdentifier = ParseSubIdentifier(ref remaining);
                    localLen = EncodeSubIdentifier(tmp.AsSpan(tmpOffset), ref subIdentifier);
                    tmpOffset += localLen;
                }

                Debug.Assert(!tag.IsConstructed);
                WriteTag(tag);
                WriteLength(tmpOffset);
                Buffer.BlockCopy(tmp, 0, _buffer, _offset, tmpOffset);
                _offset += tmpOffset;
            }
            finally
            {
                Array.Clear(tmp, 0, tmpOffset);
                ArrayPool<byte>.Shared.Return(tmp);
            }
        }

        private static BigInteger ParseSubIdentifier(ref ReadOnlySpan<char> oidValue)
        {
            int endIndex = oidValue.IndexOf('.');

            if (endIndex == -1)
            {
                endIndex = oidValue.Length;
            }
            else if (endIndex == 0 || endIndex == oidValue.Length - 1)
            {
                throw new CryptographicException(SR.Argument_InvalidOidValue);
            }

            // The following code is equivalent to
            // BigInteger.TryParse(temp, NumberStyles.None, CultureInfo.InvariantCulture, out value)
            // TODO: Split this for netstandard vs netcoreapp for span-perf?.
            BigInteger value = BigInteger.Zero;

            for (int position = 0; position < endIndex; position++)
            {
                if (position > 0 && value == 0)
                {
                    // T-REC X.680-201508 sec 12.26
                    throw new CryptographicException(SR.Argument_InvalidOidValue);
                }

                value *= 10;
                value += AtoI(oidValue[position]);
            }

            oidValue = oidValue.Slice(Math.Min(oidValue.Length, endIndex + 1));
            return value;
        }

        private static int AtoI(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            throw new CryptographicException(SR.Argument_InvalidOidValue);
        }

        // ITU-T-X.690-201508 sec 8.19.5
        private static int EncodeSubIdentifier(Span<byte> dest, ref BigInteger subIdentifier)
        {
            Debug.Assert(dest.Length > 0);

            if (subIdentifier.IsZero)
            {
                dest[0] = 0;
                return 1;
            }

            BigInteger unencoded = subIdentifier;
            int idx = 0;

            do
            {
                BigInteger cur = unencoded & 0x7F;
                byte curByte = (byte)cur;

                if (subIdentifier != unencoded)
                {
                    curByte |= 0x80;
                }

                unencoded >>= 7;
                dest[idx] = curByte;
                idx++;
            }
            while (unencoded != BigInteger.Zero);

            Reverse(dest.Slice(0, idx));
            return idx;
        }

        public void WriteEnumeratedValue(object enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            WriteEnumeratedValue(Asn1Tag.Enumerated, enumValue);
        }

        public void WriteEnumeratedValue<TEnum>(TEnum value) where TEnum : struct
        {
            WriteEnumeratedValue(Asn1Tag.Enumerated, value);
        }

        public void WriteEnumeratedValue(Asn1Tag tag, object enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            WriteEnumeratedValue(tag.AsPrimitive(), enumValue.GetType(), enumValue);
        }

        public void WriteEnumeratedValue<TEnum>(Asn1Tag tag, TEnum value) where TEnum : struct
        {
            WriteEnumeratedValue(tag.AsPrimitive(), typeof(TEnum), value);
        }

        // T-REC-X.690-201508 sec 8.4
        private void WriteEnumeratedValue(Asn1Tag tag, Type tEnum, object enumValue)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Enumerated);

            Type backingType = tEnum.GetEnumUnderlyingType();

            if (tEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_EnumeratedValueRequiresNonFlagsEnum,
                    nameof(tEnum));
            }

            if (backingType == typeof(ulong))
            {
                ulong numericValue = Convert.ToUInt64(enumValue);
                // T-REC-X.690-201508 sec 8.4
                WriteNonNegativeIntegerCore(tag, numericValue);
            }
            else
            {
                // All other types fit in a (signed) long.
                long numericValue = Convert.ToInt64(enumValue);
                // T-REC-X.690-201508 sec 8.4
                WriteIntegerCore(tag, numericValue);
            }
        }

        public void PushSequence()
        {
            PushSequenceCore(Asn1Tag.Sequence);
        }

        public void PushSequence(Asn1Tag tag)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Sequence);

            // Assert the constructed flag, in case it wasn't.
            PushSequenceCore(tag.AsConstructed());
        }

        // T-REC-X.690-201508 sec 8.9, 8.10
        private void PushSequenceCore(Asn1Tag tag)
        {
            PushTag(tag.AsConstructed());
        }

        public void PopSequence()
        {
            PopSequence(Asn1Tag.Sequence);
        }

        public void PopSequence(Asn1Tag tag)
        {
            // PopSequence shouldn't be used to pop a SetOf.
            CheckUniversalTag(tag, UniversalTagNumber.Sequence);

            // Assert the constructed flag, in case it wasn't.
            PopSequenceCore(tag.AsConstructed());
        }

        // T-REC-X.690-201508 sec 8.9, 8.10
        private void PopSequenceCore(Asn1Tag tag)
        {
            PopTag(tag);
        }

        public void PushSetOf()
        {
            PushSetOf(Asn1Tag.SetOf);
        }

        public void PushSetOf(Asn1Tag tag)
        {
            CheckUniversalTag(tag, UniversalTagNumber.SetOf);

            // Assert the constructed flag, in case it wasn't.
            PushSetOfCore(tag.AsConstructed());
        }

        // T-REC-X.690-201508 sec 8.12
        // The writer claims SetOf, and not Set, so as to avoid the field
        // ordering clause of T-REC-X.690-201508 sec 9.3
        private void PushSetOfCore(Asn1Tag tag)
        {
            PushTag(tag);
        }

        public void PopSetOf()
        {
            PopSetOfCore(Asn1Tag.SetOf);
        }

        public void PopSetOf(Asn1Tag tag)
        {
            CheckUniversalTag(tag, UniversalTagNumber.SetOf);

            // Assert the constructed flag, in case it wasn't.
            PopSetOfCore(tag.AsConstructed());
        }

        // T-REC-X.690-201508 sec 8.12
        private void PopSetOfCore(Asn1Tag tag)
        {
            // T-REC-X.690-201508 sec 11.6
            bool sortContents = RuleSet == AsnEncodingRules.CER || RuleSet == AsnEncodingRules.DER;

            PopTag(tag, sortContents);
        }

        public void WriteUtcTime(DateTimeOffset value)
        {
            WriteUtcTimeCore(Asn1Tag.UtcTime, value);
        }

        public void WriteUtcTime(Asn1Tag tag, DateTimeOffset value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.UtcTime);

            // Clear the constructed flag, if present.
            WriteUtcTimeCore(tag.AsPrimitive(), value);
        }

        public void WriteUtcTime(DateTimeOffset value, int minLegalYear)
        {
            // ensure that value is bounded within a century
            if (minLegalYear <= value.Year && value.Year < minLegalYear + 100)
            {
                WriteUtcTime(value);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        // T-REC-X.680-201508 sec 47
        // T-REC-X.690-201508 sec 11.8
        private void WriteUtcTimeCore(Asn1Tag tag, DateTimeOffset value)
        {
            // Because UtcTime is IMPLICIT VisibleString it technically can have
            // a constructed form.
            // DER says character strings must be primitive.
            // CER says character strings <= 1000 encoded bytes must be primitive.
            // So we'll just make BER be primitive, too.
            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);

            // BER allows for omitting the seconds, but that's not an option we need to expose.
            // BER allows for non-UTC values, but that's also not an option we need to expose.
            // So the format is always yyMMddHHmmssZ (13)
            const int UtcTimeValueLength = 13;
            WriteLength(UtcTimeValueLength);

            DateTimeOffset normalized = value.ToUniversalTime();

            int year = normalized.Year;
            int month = normalized.Month;
            int day = normalized.Day;
            int hour = normalized.Hour;
            int minute = normalized.Minute;
            int second = normalized.Second;

            Span<byte> baseSpan = _buffer.AsSpan(_offset);
            StandardFormat format = new StandardFormat('D', 2);

            if (!Utf8Formatter.TryFormat(year % 100, baseSpan.Slice(0, 2), out _, format) ||
                !Utf8Formatter.TryFormat(month, baseSpan.Slice(2, 2), out _, format) ||
                !Utf8Formatter.TryFormat(day, baseSpan.Slice(4, 2), out _, format) ||
                !Utf8Formatter.TryFormat(hour, baseSpan.Slice(6, 2), out _, format) ||
                !Utf8Formatter.TryFormat(minute, baseSpan.Slice(8, 2), out _, format) ||
                !Utf8Formatter.TryFormat(second, baseSpan.Slice(10, 2), out _, format))
            {
                Debug.Fail($"Utf8Formatter.TryFormat failed to build components of {normalized:O}");
                throw new CryptographicException();
            }

            _buffer[_offset + 12] = (byte)'Z';

            _offset += UtcTimeValueLength;
        }

        public void WriteGeneralizedTime(DateTimeOffset value, bool omitFractionalSeconds = false)
        {
            WriteGeneralizedTimeCore(Asn1Tag.GeneralizedTime, value, omitFractionalSeconds);
        }

        public void WriteGeneralizedTime(Asn1Tag tag, DateTimeOffset value, bool omitFractionalSeconds = false)
        {
            CheckUniversalTag(tag, UniversalTagNumber.GeneralizedTime);

            // Clear the constructed flag, if present.
            WriteGeneralizedTimeCore(tag.AsPrimitive(), value, omitFractionalSeconds);
        }

        // T-REC-X.680-201508 sec 46
        // T-REC-X.690-201508 sec 11.7
        private void WriteGeneralizedTimeCore(Asn1Tag tag, DateTimeOffset value, bool omitFractionalSeconds)
        {
            // GeneralizedTime under BER allows many different options:
            // * (HHmmss), (HHmm), (HH)
            // * "(value).frac", "(value),frac"
            // * frac == 0 may be omitted or emitted
            // non-UTC offset in various formats
            //
            // We're not allowing any of them.
            // Just encode as the CER/DER common restrictions.
            //
            // This results in the following formats:
            // yyyyMMddHHmmssZ
            // yyyyMMddHHmmss.f?Z
            //
            // where "f?" is anything from "f" to "fffffff" (tenth of a second down to 100ns/1-tick)
            // with no trailing zeros.
            DateTimeOffset normalized = value.ToUniversalTime();

            if (normalized.Year > 9999)
            {
                // This is unreachable since DateTimeOffset guards against this internally.
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            // We're only loading in sub-second ticks.
            // Ticks are defined as 1e-7 seconds, so their printed form
            // is at the longest "0.1234567", or 9 bytes.
            Span<byte> fraction = stackalloc byte[0];

            if (!omitFractionalSeconds)
            {
                long floatingTicks = normalized.Ticks % TimeSpan.TicksPerSecond;

                if (floatingTicks != 0)
                {
                    // We're only loading in sub-second ticks.
                    // Ticks are defined as 1e-7 seconds, so their printed form
                    // is at the longest "0.1234567", or 9 bytes.
                    fraction = stackalloc byte[9];

                    decimal decimalTicks = floatingTicks;
                    decimalTicks /= TimeSpan.TicksPerSecond;

                    if (!Utf8Formatter.TryFormat(decimalTicks, fraction, out int bytesWritten, new StandardFormat('G')))
                    {
                        Debug.Fail($"Utf8Formatter.TryFormat could not format {floatingTicks} / TicksPerSecond");
                        throw new CryptographicException();
                    }

                    Debug.Assert(bytesWritten > 2, $"{bytesWritten} should be > 2");
                    Debug.Assert(fraction[0] == (byte)'0');
                    Debug.Assert(fraction[1] == (byte)'.');

                    fraction = fraction.Slice(1, bytesWritten - 1);
                }
            }

            // yyyy, MM, dd, hh, mm, ss
            const int IntegerPortionLength = 4 + 2 + 2 + 2 + 2 + 2;
            // Z, and the optional fraction.
            int totalLength = IntegerPortionLength + 1 + fraction.Length;

            // Because GeneralizedTime is IMPLICIT VisibleString it technically can have
            // a constructed form.
            // DER says character strings must be primitive.
            // CER says character strings <= 1000 encoded bytes must be primitive.
            // So we'll just make BER be primitive, too.
            Debug.Assert(!tag.IsConstructed);
            WriteTag(tag);
            WriteLength(totalLength);

            int year = normalized.Year;
            int month = normalized.Month;
            int day = normalized.Day;
            int hour = normalized.Hour;
            int minute = normalized.Minute;
            int second = normalized.Second;

            Span<byte> baseSpan = _buffer.AsSpan(_offset);
            StandardFormat d4 = new StandardFormat('D', 4);
            StandardFormat d2 = new StandardFormat('D', 2);

            if (!Utf8Formatter.TryFormat(year, baseSpan.Slice(0, 4), out _, d4) ||
                !Utf8Formatter.TryFormat(month, baseSpan.Slice(4, 2), out _, d2) ||
                !Utf8Formatter.TryFormat(day, baseSpan.Slice(6, 2), out _, d2) ||
                !Utf8Formatter.TryFormat(hour, baseSpan.Slice(8, 2), out _, d2) ||
                !Utf8Formatter.TryFormat(minute, baseSpan.Slice(10, 2), out _, d2) ||
                !Utf8Formatter.TryFormat(second, baseSpan.Slice(12, 2), out _, d2))
            {
                Debug.Fail($"Utf8Formatter.TryFormat failed to build components of {normalized:O}");
                throw new CryptographicException();
            }

            _offset += IntegerPortionLength;
            fraction.CopyTo(baseSpan.Slice(IntegerPortionLength));
            _offset += fraction.Length;

            _buffer[_offset] = (byte)'Z';
            _offset++;
        }

        /// <summary>
        /// Transfer the encoded representation of the data to <paramref name="dest"/>.
        /// </summary>
        /// <param name="dest">Write destination.</param>
        /// <param name="bytesWritten">The number of bytes which were written to <paramref name="dest"/>.</param>
        /// <returns><c>true</c> if the encode succeeded, <c>false</c> if <paramref name="dest"/> is too small.</returns>
        /// <exception cref="InvalidOperationException">
        ///   A <see cref="PushSequence()"/> or <see cref="PushSetOf()"/> has not been closed via
        ///   <see cref="PopSequence()"/> or <see cref="PopSetOf()"/>.
        /// </exception>
        public bool TryEncode(Span<byte> dest, out int bytesWritten)
        {
            if ((_nestingStack?.Count ?? 0) != 0)
                throw new InvalidOperationException(SR.Cryptography_AsnWriter_EncodeUnbalancedStack);

            // If the stack is closed out then everything is a definite encoding (BER, DER) or a
            // required indefinite encoding (CER). So we're correctly sized up, and ready to copy.
            if (dest.Length < _offset)
            {
                bytesWritten = 0;
                return false;
            }

            if (_offset == 0)
            {
                bytesWritten = 0;
                return true;
            }

            bytesWritten = _offset;
            _buffer.AsSpan(0, _offset).CopyTo(dest);
            return true;
        }

        public byte[] Encode()
        {
            if ((_nestingStack?.Count ?? 0) != 0)
            {
                throw new InvalidOperationException(SR.Cryptography_AsnWriter_EncodeUnbalancedStack);
            }

            if (_offset == 0)
            {
                return Array.Empty<byte>();
            }

            // If the stack is closed out then everything is a definite encoding (BER, DER) or a
            // required indefinite encoding (CER). So we're correctly sized up, and ready to copy.
            return _buffer.AsSpan(0, _offset).ToArray();
        }

        public ReadOnlySpan<byte> EncodeAsSpan()
        {
            if ((_nestingStack?.Count ?? 0) != 0)
            {
                throw new InvalidOperationException(SR.Cryptography_AsnWriter_EncodeUnbalancedStack);
            }

            if (_offset == 0)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            // If the stack is closed out then everything is a definite encoding (BER, DER) or a
            // required indefinite encoding (CER). So we're correctly sized up, and ready to copy.
            return new ReadOnlySpan<byte>(_buffer, 0, _offset);
        }

        private void PushTag(Asn1Tag tag)
        {
            if (_nestingStack == null)
            {
                _nestingStack = new Stack<(Asn1Tag,int)>();
            }

            Debug.Assert(tag.IsConstructed);
            WriteTag(tag);
            _nestingStack.Push((tag, _offset));
            // Indicate that the length is indefinite.
            // We'll come back and clean this up (as appropriate) in PopTag.
            WriteLength(-1);
        }

        private void PopTag(Asn1Tag tag, bool sortContents=false)
        {
            if (_nestingStack == null || _nestingStack.Count == 0)
            {
                throw new ArgumentException(SR.Cryptography_AsnWriter_PopWrongTag, nameof(tag));
            }

            (Asn1Tag stackTag, int lenOffset) = _nestingStack.Peek();

            Debug.Assert(tag.IsConstructed);
            if (stackTag != tag)
            {
                throw new ArgumentException(SR.Cryptography_AsnWriter_PopWrongTag, nameof(tag));
            }

            _nestingStack.Pop();

            if (sortContents)
            {
                SortContents(_buffer, lenOffset + 1, _offset);
            }

            // BER could use the indefinite encoding that CER does.
            // But since the definite encoding form is easier to read (doesn't require a contextual
            // parser to find the end-of-contents marker) some ASN.1 readers (including the previous
            // incarnation of AsnReader) may choose not to support it.
            //
            // So, BER will use the DER rules here, in the interest of broader compatibility.

            // T-REC-X.690-201508 sec 9.1 (constructed CER => indefinite length)
            // T-REC-X.690-201508 sec 8.1.3.6
            if (RuleSet == AsnEncodingRules.CER)
            {
                WriteEndOfContents();
                return;
            }

            int containedLength = _offset - 1 - lenOffset;
            Debug.Assert(containedLength >= 0);

            int shiftSize = GetEncodedLengthSubsequentByteCount(containedLength);

            // Best case, length fits in the compact byte
            if (shiftSize == 0)
            {
                _buffer[lenOffset] = (byte)containedLength;
                return;
            }

            // We're currently at the end, so ensure we have room for N more bytes.
            EnsureWriteCapacity(shiftSize);

            // Buffer.BlockCopy correctly does forward-overlapped, so use it.
            int start = lenOffset + 1;
            Buffer.BlockCopy(_buffer, start, _buffer, start + shiftSize, containedLength);

            int tmp = _offset;
            _offset = lenOffset;
            WriteLength(containedLength);
            Debug.Assert(_offset - lenOffset - 1 == shiftSize);
            _offset = tmp + shiftSize;
        }

        public void WriteCharacterString(UniversalTagNumber encodingType, string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            WriteCharacterString(encodingType, str.AsSpan());
        }

        public void WriteCharacterString(UniversalTagNumber encodingType, ReadOnlySpan<char> str)
        {
            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);

            WriteCharacterStringCore(new Asn1Tag(encodingType), encoding, str);
        }

        public void WriteCharacterString(Asn1Tag tag, UniversalTagNumber encodingType, string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            WriteCharacterString(tag, encodingType, str.AsSpan());
        }

        public void WriteCharacterString(Asn1Tag tag, UniversalTagNumber encodingType, ReadOnlySpan<char> str)
        {
            CheckUniversalTag(tag, encodingType);

            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
            WriteCharacterStringCore(tag, encoding, str);
        }

        // T-REC-X.690-201508 sec 8.23
        private void WriteCharacterStringCore(Asn1Tag tag, Text.Encoding encoding, ReadOnlySpan<char> str)
        {
            int size = -1;

            // T-REC-X.690-201508 sec 9.2
            if (RuleSet == AsnEncodingRules.CER)
            {
                // TODO: Split this for netstandard vs netcoreapp for span?.
                unsafe
                {
                    fixed (char* strPtr = &MemoryMarshal.GetReference(str))
                    {
                        size = encoding.GetByteCount(strPtr, str.Length);

                        // If it exceeds the primitive segment size, use the constructed encoding.
                        if (size > AsnReader.MaxCERSegmentSize)
                        {
                            WriteConstructedCerCharacterString(tag, encoding, str, size);
                            return;
                        }
                    }
                }
            }

            // TODO: Split this for netstandard vs netcoreapp for span?.
            unsafe
            {
                fixed (char* strPtr = &MemoryMarshal.GetReference(str))
                {
                    if (size < 0)
                    {
                        size = encoding.GetByteCount(strPtr, str.Length);
                    }

                    // Clear the constructed tag, if present.
                    WriteTag(tag.AsPrimitive());
                    WriteLength(size);
                    Span<byte> dest = _buffer.AsSpan(_offset, size);

                    fixed (byte* destPtr = &MemoryMarshal.GetReference(dest))
                    {
                        int written = encoding.GetBytes(strPtr, str.Length, destPtr, dest.Length);

                        if (written != size)
                        {
                            Debug.Fail($"Encoding produced different answer for GetByteCount ({size}) and GetBytes ({written})");
                            throw new InvalidOperationException();
                        }
                    }

                    _offset += size;
                }
            }
        }

        private void WriteConstructedCerCharacterString(Asn1Tag tag, Text.Encoding encoding, ReadOnlySpan<char> str, int size)
        {
            Debug.Assert(size > AsnReader.MaxCERSegmentSize);

            byte[] tmp;

            // TODO: Split this for netstandard vs netcoreapp for span?.
            unsafe
            {
                fixed (char* strPtr = &MemoryMarshal.GetReference(str))
                {
                    tmp = ArrayPool<byte>.Shared.Rent(size);

                    fixed (byte* destPtr = tmp)
                    {
                        int written = encoding.GetBytes(strPtr, str.Length, destPtr, tmp.Length);

                        if (written != size)
                        {
                            Debug.Fail(
                                $"Encoding produced different answer for GetByteCount ({size}) and GetBytes ({written})");
                            throw new InvalidOperationException();
                        }
                    }
                }
            }

            WriteConstructedCerOctetString(tag, tmp.AsSpan(0, size));
            Array.Clear(tmp, 0, size);
            ArrayPool<byte>.Shared.Return(tmp);
        }

        private static void SortContents(byte[] buffer, int start, int end)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(end >= start);

            int len = end - start;

            if (len == 0)
            {
                return;
            }

            // Since BER can read everything and the reader does not mutate data
            // just use a BER reader for identifying the positions of the values
            // within this memory segment.
            //
            // Since it's not mutating, any restrictions imposed by CER or DER will
            // still be maintained.
            var reader = new AsnReader(new ReadOnlyMemory<byte>(buffer, start, len), AsnEncodingRules.BER);

            List<(int, int)> positions = new List<(int, int)>();

            int pos = start;

            while (reader.HasData)
            {
                ReadOnlyMemory<byte> encoded = reader.GetEncodedValue();
                positions.Add((pos, encoded.Length));
                pos += encoded.Length;
            }

            Debug.Assert(pos == end);

            var comparer = new ArrayIndexSetOfValueComparer(buffer);
            positions.Sort(comparer);

            byte[] tmp = ArrayPool<byte>.Shared.Rent(len);

            pos = 0;

            foreach ((int offset, int length) in positions)
            {
                Buffer.BlockCopy(buffer, offset, tmp, pos, length);
                pos += length;
            }

            Debug.Assert(pos == len);

            Buffer.BlockCopy(tmp, 0, buffer, start, len);
            Array.Clear(tmp, 0, len);
            ArrayPool<byte>.Shared.Return(tmp);
        }

        internal static void Reverse(Span<byte> span)
        {
            int i = 0;
            int j = span.Length - 1;

            while (i < j)
            {
                byte tmp = span[i];
                span[i] = span[j];
                span[j] = tmp;

                i++;
                j--;
            }
        }

        private static void CheckUniversalTag(Asn1Tag tag, UniversalTagNumber universalTagNumber)
        {
            if (tag.TagClass == TagClass.Universal && tag.TagValue != (int)universalTagNumber)
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_UniversalValueIsFixed,
                    nameof(tag));
            }
        }

        private class ArrayIndexSetOfValueComparer : IComparer<(int, int)>
        {
            private readonly byte[] _data;

            public ArrayIndexSetOfValueComparer(byte[] data)
            {
                _data = data;
            }

            public int Compare((int, int) x, (int, int) y)
            {
                (int xOffset, int xLength) = x;
                (int yOffset, int yLength) = y;

                int value =
                    SetOfValueComparer.Instance.Compare(
                        new ReadOnlyMemory<byte>(_data, xOffset, xLength),
                        new ReadOnlyMemory<byte>(_data, yOffset, yLength));

                if (value == 0)
                {
                    // Whichever had the lowest index wins (once sorted, stay sorted)
                    return xOffset - yOffset;
                }

                return value;
            }
        }
    }
}
