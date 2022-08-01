// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Text.Unicode
{
    internal static partial class Utf8Utility
    {
        /// <summary>
        /// Given a machine-endian DWORD which four bytes of UTF-8 data, interprets the
        /// first three bytes as a three-byte UTF-8 subsequence and returns the UTF-16 representation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ExtractCharFromFirstThreeByteSequence(uint value)
        {
            Debug.Assert(UInt32BeginsWithUtf8ThreeByteMask(value));

            if (BitConverter.IsLittleEndian)
            {
                // value = [ ######## | 10xxxxxx 10yyyyyy 1110zzzz ]
                return ((value & 0x003F0_000u) >> 16)
                    | ((value & 0x0000_3F00u) >> 2)
                    | ((value & 0x0000_000Fu) << 12);
            }
            else
            {
                // value = [ 1110zzzz 10yyyyyy 10xxxxxx | ######## ]
                return ((value & 0x0F00_0000u) >> 12)
                    | ((value & 0x003F_0000u) >> 10)
                    | ((value & 0x0000_3F00u) >> 8);
            }
        }

        /// <summary>
        /// Given a machine-endian DWORD which four bytes of UTF-8 data, interprets the
        /// first two bytes as a two-byte UTF-8 subsequence and returns the UTF-16 representation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ExtractCharFromFirstTwoByteSequence(uint value)
        {
            Debug.Assert(UInt32BeginsWithUtf8TwoByteMask(value) && !UInt32BeginsWithOverlongUtf8TwoByteSequence(value));

            if (BitConverter.IsLittleEndian)
            {
                // value = [ ######## ######## | 10xxxxxx 110yyyyy ]
                uint leadingByte = (uint)(byte)value << 6;
                return (uint)(byte)(value >> 8) + leadingByte - (0xC0u << 6) - 0x80u; // remove header bits
            }
            else
            {
                // value = [ 110yyyyy 10xxxxxx | ######## ######## ]
                return (char)(((value & 0x1F00_0000u) >> 18) | ((value & 0x003F_0000u) >> 16));
            }
        }

        /// <summary>
        /// Given a machine-endian DWORD which represents four bytes of UTF-8 data, interprets the input as a
        /// four-byte UTF-8 sequence and returns the machine-endian DWORD of the UTF-16 representation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ExtractCharsFromFourByteSequence(uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                // input is UTF8 [ 10xxxxxx 10yyyyyy 10uuzzzz 11110uuu ] = scalar 000uuuuu zzzzyyyy yyxxxxxx
                // want to return UTF16 scalar 000uuuuuzzzzyyyyyyxxxxxx = [ 110111yy yyxxxxxx 110110ww wwzzzzyy ]
                // where wwww = uuuuu - 1
                uint retVal = (uint)(byte)value << 8; // retVal = [ 00000000 00000000 11110uuu 00000000 ]
                retVal |= (value & 0x0000_3F00u) >> 6; // retVal = [ 00000000 00000000 11110uuu uuzzzz00 ]
                retVal |= (value & 0x0030_0000u) >> 20; // retVal = [ 00000000 00000000 11110uuu uuzzzzyy ]
                retVal |= (value & 0x3F00_0000u) >> 8; // retVal = [ 00000000 00xxxxxx 11110uuu uuzzzzyy ]
                retVal |= (value & 0x000F_0000u) << 6; // retVal = [ 000000yy yyxxxxxx 11110uuu uuzzzzyy ]
                retVal -= 0x0000_0040u; // retVal = [ 000000yy yyxxxxxx 111100ww wwzzzzyy ]
                retVal -= 0x0000_2000u; // retVal = [ 000000yy yyxxxxxx 110100ww wwzzzzyy ]
                retVal += 0x0000_0800u; // retVal = [ 000000yy yyxxxxxx 110110ww wwzzzzyy ]
                retVal += 0xDC00_0000u; // retVal = [ 110111yy yyxxxxxx 110110ww wwzzzzyy ]
                return retVal;
            }
            else
            {
                // input is UTF8 [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ] = scalar 000uuuuu zzzzyyyy yyxxxxxx
                // want to return UTF16 scalar 000uuuuuxxxxxxxxxxxxxxxx = [ 110110wwwwxxxxxx 110111xxxxxxxxx ]
                // where wwww = uuuuu - 1
                uint retVal = value & 0xFF00_0000u; // retVal = [ 11110uuu 00000000 00000000 00000000 ]
                retVal |= (value & 0x003F_0000u) << 2; // retVal = [ 11110uuu uuzzzz00 00000000 00000000 ]
                retVal |= (value & 0x0000_3000u) << 4; // retVal = [ 11110uuu uuzzzzyy 00000000 00000000 ]
                retVal |= (value & 0x0000_0F00u) >> 2; // retVal = [ 11110uuu uuzzzzyy 000000yy yy000000 ]
                retVal |= (value & 0x0000_003Fu); // retVal = [ 11110uuu uuzzzzyy 000000yy yyxxxxxx ]
                retVal -= 0x2000_0000u; // retVal = [ 11010uuu uuzzzzyy 000000yy yyxxxxxx ]
                retVal -= 0x0040_0000u; // retVal = [ 110100ww wwzzzzyy 000000yy yyxxxxxx ]
                retVal += 0x0000_DC00u; // retVal = [ 110100ww wwzzzzyy 110111yy yyxxxxxx ]
                retVal += 0x0800_0000u; // retVal = [ 110110ww wwzzzzyy 110111yy yyxxxxxx ]
                return retVal;
            }
        }

        /// <summary>
        /// Given a 32-bit integer that represents a valid packed UTF-16 surrogate pair, all in machine-endian order,
        /// returns the packed 4-byte UTF-8 representation of this scalar value, also in machine-endian order.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ExtractFourUtf8BytesFromSurrogatePair(uint value)
        {
            Debug.Assert(IsWellFormedUtf16SurrogatePair(value));

            if (BitConverter.IsLittleEndian)
            {
                // input = [ 110111yyyyxxxxxx 110110wwwwzzzzyy ] = scalar (000uuuuu zzzzyyyy yyxxxxxx)
                // must return [ 10xxxxxx 10yyyyyy 10uuzzzz 11110uuu ], where wwww = uuuuu - 1

                value += 0x0000_0040u; // = [ 110111yyyyxxxxxx 11011uuuuuzzzzyy ]

                uint tempA = BinaryPrimitives.ReverseEndianness(value & 0x003F_0700u); // = [ 00000000 00000uuu 00xxxxxx 00000000 ]
                tempA = BitOperations.RotateLeft(tempA, 16); // = [ 00xxxxxx 00000000 00000000 00000uuu ]

                uint tempB = (value & 0x00FCu) << 6; // = [ 00000000 00000000 00uuzzzz 00000000 ]
                uint tempC = (value >> 6) & 0x000F_0000u; // = [ 00000000 0000yyyy 00000000 00000000 ]
                tempC |= tempB;

                uint tempD = (value & 0x03u) << 20; // = [ 00000000 00yy0000 00000000 00000000 ]
                tempD |= 0x8080_80F0u;

                return tempD | tempA | tempC; // = [ 10xxxxxx 10yyyyyy 10uuzzzz 11110uuu ]
            }
            else
            {
                // input = [ 110110wwwwzzzzyy 110111yyyyxxxxxx ], where wwww = uuuuu - 1
                // must return [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ], where wwww = uuuuu - 1

                value -= 0xD800_DC00u; // = [ 000000wwwwzzzzyy 000000yyyyxxxxxx ]
                value += 0x0040_0000u; // = [ 00000uuuuuzzzzyy 000000yyyyxxxxxx ]

                uint tempA = value & 0x0700_0000u; // = [ 00000uuu 00000000 00000000 00000000 ]
                uint tempB = (value >> 2) & 0x003F_0000u; // = [ 00000000 00uuzzzz 00000000 00000000 ]
                tempB |= tempA;

                uint tempC = (value << 2) & 0x0000_0F00u; // = [ 00000000 00000000 0000yyyy 00000000 ]
                uint tempD = (value >> 4) & 0x0000_3000u; // = [ 00000000 00000000 00yy0000 00000000 ]
                tempD |= tempC;

                uint tempE = (value & 0x3Fu) + 0xF080_8080u; // = [ 11110000 10000000 10000000 10xxxxxx ]
                return tempE | tempB | tempD; // = [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ]
            }
        }

        /// <summary>
        /// Given a machine-endian DWORD which represents two adjacent UTF-8 two-byte sequences,
        /// returns the machine-endian DWORD representation of that same data as two adjacent
        /// UTF-16 byte sequences.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ExtractTwoCharsPackedFromTwoAdjacentTwoByteSequences(uint value)
        {
            // We don't want to swap the position of the high and low WORDs,
            // as the buffer was read in machine order and will be written in
            // machine order.

            if (BitConverter.IsLittleEndian)
            {
                // value = [ 10xxxxxx 110yyyyy | 10xxxxxx 110yyyyy ]
                return ((value & 0x3F003F00u) >> 8) | ((value & 0x001F001Fu) << 6);
            }
            else
            {
                // value = [ 110yyyyy 10xxxxxx | 110yyyyy 10xxxxxx ]
                return ((value & 0x1F001F00u) >> 2) | (value & 0x003F003Fu);
            }
        }

        /// <summary>
        /// Given a machine-endian DWORD which represents two adjacent UTF-16 sequences,
        /// returns the machine-endian DWORD representation of that same data as two
        /// adjacent UTF-8 two-byte sequences.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ExtractTwoUtf8TwoByteSequencesFromTwoPackedUtf16Chars(uint value)
        {
            // stays in machine endian

            Debug.Assert(IsFirstCharTwoUtf8Bytes(value) && IsSecondCharTwoUtf8Bytes(value));

            if (BitConverter.IsLittleEndian)
            {
                // value = [ 00000YYY YYXXXXXX 00000yyy yyxxxxxx ]
                // want to return [ 10XXXXXX 110YYYYY 10xxxxxx 110yyyyy ]

                return ((value >> 6) & 0x001F_001Fu) + ((value << 8) & 0x3F00_3F00u) + 0x80C0_80C0u;
            }
            else
            {
                // value = [ 00000YYY YYXXXXXX 00000yyy yyxxxxxx ]
                // want to return [ 110YYYYY 10XXXXXX 110yyyyy 10xxxxxx ]

                return ((value << 2) & 0x1F00_1F00u) + (value & 0x003F_003Fu) + 0xC080_C080u;
            }
        }

        /// <summary>
        /// Given a machine-endian DWORD which represents two adjacent UTF-16 sequences,
        /// returns the machine-endian DWORD representation of the first UTF-16 char
        /// as a UTF-8 two-byte sequence packed into a WORD and zero-extended to DWORD.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ExtractUtf8TwoByteSequenceFromFirstUtf16Char(uint value)
        {
            // stays in machine endian

            Debug.Assert(IsFirstCharTwoUtf8Bytes(value));

            if (BitConverter.IsLittleEndian)
            {
                // value = [ ######## ######## 00000yyy yyxxxxxx ]
                // want to return [ ######## ######## 10xxxxxx 110yyyyy ]

                uint temp = (value << 2) & 0x1F00u; // [ 00000000 00000000 000yyyyy 00000000 ]
                value &= 0x3Fu; // [ 00000000 00000000 00000000 00xxxxxx ]
                return BinaryPrimitives.ReverseEndianness((ushort)(temp + value + 0xC080u)); // [ 00000000 00000000 10xxxxxx 110yyyyy ]
            }
            else
            {
                // value = [ 00000yyy yyxxxxxx ######## ######## ]
                // want to return [ ######## ######## 110yyyyy 10xxxxxx ]

                uint temp = (value >> 16) & 0x3Fu; // [ 00000000 00000000 00000000 00xxxxxx ]
                value = (value >> 14) & 0x1F00u; // [ 00000000 00000000 000yyyyy 0000000 ]
                return value + temp + 0xC080u;
            }
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the first UTF-16 character is ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFirstCharAscii(uint value)
        {
            // Little-endian: Given [ #### AAAA ], return whether AAAA is in range [ 0000..007F ].
            // Big-endian: Given [ AAAA #### ], return whether AAAA is in range [ 0000..007F ].

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && (value & 0xFF80u) == 0)
                || (!BitConverter.IsLittleEndian && value < 0x0080_0000u);
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the first UTF-16 character requires *at least* 3 bytes to encode in UTF-8.
        /// This also returns true if the first UTF-16 character is a surrogate character (well-formedness is not validated).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFirstCharAtLeastThreeUtf8Bytes(uint value)
        {
            // Little-endian: Given [ #### AAAA ], return whether AAAA is in range [ 0800..FFFF ].
            // Big-endian: Given [ AAAA #### ], return whether AAAA is in range [ 0800..FFFF ].

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && (value & 0xF800u) != 0)
                || (!BitConverter.IsLittleEndian && value >= 0x0800_0000u);
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the first UTF-16 character is a surrogate character (either high or low).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFirstCharSurrogate(uint value)
        {
            // Little-endian: Given [ #### AAAA ], return whether AAAA is in range [ D800..DFFF ].
            // Big-endian: Given [ AAAA #### ], return whether AAAA is in range [ D800..DFFF ].

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((value - 0xD800u) & 0xF800u) == 0)
                || (!BitConverter.IsLittleEndian && (value - 0xD800_0000u) < 0x0800_0000u);
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the first UTF-16 character would be encoded as exactly 2 bytes in UTF-8.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFirstCharTwoUtf8Bytes(uint value)
        {
            // Little-endian: Given [ #### AAAA ], return whether AAAA is in range [ 0080..07FF ].
            // Big-endian: Given [ AAAA #### ], return whether AAAA is in range [ 0080..07FF ].

            // TODO: I'd like to be able to write "(ushort)(value - 0x0080u) < 0x0780u" for the little-endian
            // case, but the JIT doesn't currently emit 16-bit comparisons efficiently.
            // Tracked as https://github.com/dotnet/runtime/issues/10337.

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((value - 0x0080u) & 0xFFFFu) < 0x0780u)
                || (!BitConverter.IsLittleEndian && UnicodeUtility.IsInRangeInclusive(value, 0x0080_0000u, 0x07FF_FFFFu));
        }

        /// <summary>
        /// Returns <see langword="true"/> iff the low byte of <paramref name="value"/>
        /// is a UTF-8 continuation byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowByteUtf8ContinuationByte(uint value)
        {
            // The JIT won't emit a single 8-bit signed cmp instruction (see IsUtf8ContinuationByte),
            // so the best we can do for now is the lea / cmp pair.
            // Tracked as https://github.com/dotnet/runtime/issues/10337.

            return (byte)(value - 0x80u) <= 0x3Fu;
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the second UTF-16 character is ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSecondCharAscii(uint value)
        {
            // Little-endian: Given [ BBBB #### ], return whether BBBB is in range [ 0000..007F ].
            // Big-endian: Given [ #### BBBB ], return whether BBBB is in range [ 0000..007F ].

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && value < 0x0080_0000u)
                || (!BitConverter.IsLittleEndian && (value & 0xFF80u) == 0);
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the second UTF-16 character requires *at least* 3 bytes to encode in UTF-8.
        /// This also returns true if the second UTF-16 character is a surrogate character (well-formedness is not validated).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSecondCharAtLeastThreeUtf8Bytes(uint value)
        {
            // Little-endian: Given [ BBBB #### ], return whether BBBB is in range [ 0800..FFFF ].
            // Big-endian: Given [ #### BBBB ], return whether ABBBBAAA is in range [ 0800..FFFF ].

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && (value & 0xF800_0000u) != 0)
                || (!BitConverter.IsLittleEndian && (value & 0xF800u) != 0);
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the second UTF-16 character is a surrogate character (either high or low).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSecondCharSurrogate(uint value)
        {
            // Little-endian: Given [ BBBB #### ], return whether BBBB is in range [ D800..DFFF ].
            // Big-endian: Given [ #### BBBB ], return whether BBBB is in range [ D800..DFFF ].

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && (value - 0xD800_0000u) < 0x0800_0000u)
                || (!BitConverter.IsLittleEndian && ((value - 0xD800u) & 0xF800u) == 0);
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the second UTF-16 character would be encoded as exactly 2 bytes in UTF-8.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSecondCharTwoUtf8Bytes(uint value)
        {
            // Little-endian: Given [ BBBB #### ], return whether BBBB is in range [ 0080..07FF ].
            // Big-endian: Given [ #### BBBB ], return whether BBBB is in range [ 0080..07FF ].

            // TODO: I'd like to be able to write "(ushort)(value - 0x0080u) < 0x0780u" for the big-endian
            // case, but the JIT doesn't currently emit 16-bit comparisons efficiently.
            // Tracked as https://github.com/dotnet/runtime/issues/10337.

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && UnicodeUtility.IsInRangeInclusive(value, 0x0080_0000u, 0x07FF_FFFFu))
                || (!BitConverter.IsLittleEndian && ((value - 0x0080u) & 0xFFFFu) < 0x0780u);
        }

        /// <summary>
        /// Returns <see langword="true"/> iff <paramref name="value"/> is a UTF-8 continuation byte;
        /// i.e., has binary representation 10xxxxxx, where x is any bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsUtf8ContinuationByte(in byte value)
        {
            // This API takes its input as a readonly ref so that the JIT can emit "cmp ModRM" statements
            // directly rather than bounce a temporary through a register. That is, we want the JIT to be
            // able to emit a single "cmp byte ptr [data], C0h" statement if we're querying a memory location
            // to see if it's a continuation byte. Data that's already enregistered will go through the
            // normal "cmp reg, C0h" code paths, perhaps with some extra unnecessary "movzx" instructions.
            //
            // The below check takes advantage of the two's complement representation of negative numbers.
            // [ 0b1000_0000, 0b1011_1111 ] is [ -127 (sbyte.MinValue), -65 ]

            return (sbyte)value < -64;
        }

        /// <summary>
        /// Given a 32-bit integer that represents two packed UTF-16 characters, all in machine-endian order,
        /// returns true iff the two characters represent a well-formed UTF-16 surrogate pair.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWellFormedUtf16SurrogatePair(uint value)
        {
            // Little-endian: Given [ LLLL HHHH ], validate that LLLL in [ DC00..DFFF ] and HHHH in [ D800..DBFF ].
            // Big-endian: Given [ HHHH LLLL ], validate that HHHH in [ D800..DBFF ] and LLLL in [ DC00..DFFF ].
            //
            // We're essentially performing a range check on each component of the input in parallel. The allowed range
            // ends up being "< 0x0400" after the beginning of the allowed range is subtracted from each element. We
            // can't perform the equivalent of two CMPs in parallel, but we can take advantage of the fact that 0x0400
            // is a whole power of 2, which means that a CMP is really just a glorified TEST operation. Two TESTs *can*
            // be performed in parallel. The logic below then becomes 3 operations: "add/lea; test; jcc".

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((value - 0xDC00_D800u) & 0xFC00_FC00u) == 0)
                || (!BitConverter.IsLittleEndian && ((value - 0xD800_DC00u) & 0xFC00_FC00u) == 0);
        }

        /// <summary>
        /// Converts a DWORD from machine-endian to little-endian.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ToLittleEndian(uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }
            else
            {
                return BinaryPrimitives.ReverseEndianness(value);
            }
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the first two bytes of the buffer are
        /// an overlong representation of a sequence that should be represented as one byte.
        /// This method *does not* validate that the sequence matches the appropriate
        /// 2-byte sequence mask (see <see cref="UInt32BeginsWithUtf8TwoByteMask"/>).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32BeginsWithOverlongUtf8TwoByteSequence(uint value)
        {
            // ASSUMPTION: Caller has already checked the '110yyyyy 10xxxxxx' mask of the input.
            Debug.Assert(UInt32BeginsWithUtf8TwoByteMask(value));

            // Per Table 3-7, first byte of two-byte sequence must be within range C2 .. DF.
            // Since we already validated it's 80 <= ?? <= DF (per mask check earlier), now only need
            // to check that it's < C2.

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((byte)value < 0xC2u))
                || (!BitConverter.IsLittleEndian && (value < 0xC200_0000u));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the first four bytes of the buffer match
        /// the UTF-8 4-byte sequence mask [ 11110www 10zzzzzz 10yyyyyy 10xxxxxx ]. This
        /// method *does not* validate that the sequence is well-formed; the caller must
        /// still perform overlong form or out-of-range checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32BeginsWithUtf8FourByteMask(uint value)
        {
            // The code in this method is equivalent to the code
            // below but is slightly more optimized.
            //
            // if (BitConverter.IsLittleEndian)
            // {
            //     const uint mask = 0xC0C0C0F8U;
            //     const uint comparand = 0x808080F0U;
            //     return ((value & mask) == comparand);
            // }
            // else
            // {
            //     const uint mask = 0xF8C0C0C0U;
            //     const uint comparand = 0xF0808000U;
            //     return ((value & mask) == comparand);
            // }

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && (((value - 0x8080_80F0u) & 0xC0C0_C0F8u) == 0))
                || (!BitConverter.IsLittleEndian && (((value - 0xF080_8080u) & 0xF8C0_C0C0u) == 0));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the first three bytes of the buffer match
        /// the UTF-8 3-byte sequence mask [ 1110zzzz 10yyyyyy 10xxxxxx ]. This method *does not*
        /// validate that the sequence is well-formed; the caller must still perform
        /// overlong form or surrogate checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32BeginsWithUtf8ThreeByteMask(uint value)
        {
            // The code in this method is equivalent to the code
            // below but is slightly more optimized.
            //
            // if (BitConverter.IsLittleEndian)
            // {
            //     const uint mask = 0x00C0C0F0U;
            //     const uint comparand = 0x008080E0U;
            //     return ((value & mask) == comparand);
            // }
            // else
            // {
            //     const uint mask = 0xF0C0C000U;
            //     const uint comparand = 0xE0808000U;
            //     return ((value & mask) == comparand);
            // }

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && (((value - 0x0080_80E0u) & 0x00C0_C0F0u) == 0))
                || (!BitConverter.IsLittleEndian && (((value - 0xE080_8000u) & 0xF0C0_C000u) == 0));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the first two bytes of the buffer match
        /// the UTF-8 2-byte sequence mask [ 110yyyyy 10xxxxxx ]. This method *does not*
        /// validate that the sequence is well-formed; the caller must still perform
        /// overlong form checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32BeginsWithUtf8TwoByteMask(uint value)
        {
            // The code in this method is equivalent to the code
            // below but is slightly more optimized.
            //
            // if (BitConverter.IsLittleEndian)
            // {
            //     const uint mask = 0x0000C0E0U;
            //     const uint comparand = 0x000080C0U;
            //     return ((value & mask) == comparand);
            // }
            // else
            // {
            //     const uint mask = 0xE0C00000U;
            //     const uint comparand = 0xC0800000U;
            //     return ((value & mask) == comparand);
            // }

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && (((value - 0x0000_80C0u) & 0x0000_C0E0u) == 0))
                || (!BitConverter.IsLittleEndian && (((value - 0xC080_0000u) & 0xE0C0_0000u) == 0));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the first two bytes of the buffer are
        /// an overlong representation of a sequence that should be represented as one byte.
        /// This method *does not* validate that the sequence matches the appropriate
        /// 2-byte sequence mask (see <see cref="UInt32BeginsWithUtf8TwoByteMask"/>).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32EndsWithOverlongUtf8TwoByteSequence(uint value)
        {
            // ASSUMPTION: Caller has already checked the '110yyyyy 10xxxxxx' mask of the input.
            Debug.Assert(UInt32EndsWithUtf8TwoByteMask(value));

            // Per Table 3-7, first byte of two-byte sequence must be within range C2 .. DF.
            // We already validated that it's 80 .. DF (per mask check earlier).
            // C2 = 1100 0010
            // DF = 1101 1111
            // This means that we can AND the leading byte with the mask 0001 1110 (1E),
            // and if the result is zero the sequence is overlong.

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((value & 0x001E_0000u) == 0))
                || (!BitConverter.IsLittleEndian && ((value & 0x1E00u) == 0));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the last two bytes of the buffer match
        /// the UTF-8 2-byte sequence mask [ 110yyyyy 10xxxxxx ]. This method *does not*
        /// validate that the sequence is well-formed; the caller must still perform
        /// overlong form checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32EndsWithUtf8TwoByteMask(uint value)
        {
            // The code in this method is equivalent to the code
            // below but is slightly more optimized.
            //
            // if (BitConverter.IsLittleEndian)
            // {
            //     const uint mask = 0xC0E00000U;
            //     const uint comparand = 0x80C00000U;
            //     return ((value & mask) == comparand);
            // }
            // else
            // {
            //     const uint mask = 0x0000E0C0U;
            //     const uint comparand = 0x0000C080U;
            //     return ((value & mask) == comparand);
            // }

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && (((value - 0x80C0_0000u) & 0xC0E0_0000u) == 0))
                || (!BitConverter.IsLittleEndian && (((value - 0x0000_C080u) & 0x0000_E0C0u) == 0));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD on a little-endian machine,
        /// returns <see langword="true"/> iff the first two bytes of the buffer are a well-formed
        /// UTF-8 two-byte sequence. This wraps the mask check and the overlong check into a
        /// single operation. Returns <see langword="false"/> if running on a big-endian machine.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32BeginsWithValidUtf8TwoByteSequenceLittleEndian(uint value)
        {
            // Per Table 3-7, valid 2-byte sequences are [ C2..DF ] [ 80..BF ].
            // In little-endian, that would be represented as:
            // [ ######## ######## 10xxxxxx 110yyyyy ].
            // Due to the little-endian representation we can perform a trick by ANDing the low
            // WORD with the bitmask [ 11000000 11111111 ] and checking that the value is within
            // the range [ 10000000_11000010, 10000000_11011111 ]. This performs both the
            // 2-byte-sequence bitmask check and overlong form validation with one comparison.

            Debug.Assert(BitConverter.IsLittleEndian);

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && UnicodeUtility.IsInRangeInclusive(value & 0xC0FFu, 0x80C2u, 0x80DFu))
                || (!BitConverter.IsLittleEndian && false);
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD on a little-endian machine,
        /// returns <see langword="true"/> iff the last two bytes of the buffer are a well-formed
        /// UTF-8 two-byte sequence. This wraps the mask check and the overlong check into a
        /// single operation. Returns <see langword="false"/> if running on a big-endian machine.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32EndsWithValidUtf8TwoByteSequenceLittleEndian(uint value)
        {
            // See comments in UInt32BeginsWithValidUtf8TwoByteSequenceLittleEndian.

            Debug.Assert(BitConverter.IsLittleEndian);

            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && UnicodeUtility.IsInRangeInclusive(value & 0xC0FF_0000u, 0x80C2_0000u, 0x80DF_0000u))
                || (!BitConverter.IsLittleEndian && false);
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the first byte of the buffer is ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32FirstByteIsAscii(uint value)
        {
            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((value & 0x80u) == 0))
                || (!BitConverter.IsLittleEndian && ((int)value >= 0));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the fourth byte of the buffer is ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32FourthByteIsAscii(uint value)
        {
            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((int)value >= 0))
                || (!BitConverter.IsLittleEndian && ((value & 0x80u) == 0));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the second byte of the buffer is ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32SecondByteIsAscii(uint value)
        {
            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((value & 0x8000u) == 0))
                || (!BitConverter.IsLittleEndian && ((value & 0x0080_0000u) == 0));
        }

        /// <summary>
        /// Given a UTF-8 buffer which has been read into a DWORD in machine endianness,
        /// returns <see langword="true"/> iff the third byte of the buffer is ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UInt32ThirdByteIsAscii(uint value)
        {
            // Return statement is written this way to work around https://github.com/dotnet/runtime/issues/4207.

            return (BitConverter.IsLittleEndian && ((value & 0x0080_0000u) == 0))
                || (!BitConverter.IsLittleEndian && ((value & 0x8000u) == 0));
        }

        /// <summary>
        /// Given a DWORD which represents a buffer of 2 packed UTF-16 values in machine endianness,
        /// converts those scalar values to their 3-byte UTF-8 representation and writes the
        /// resulting 6 bytes to the destination buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteTwoUtf16CharsAsTwoUtf8ThreeByteSequences(ref byte outputBuffer, uint value)
        {
            Debug.Assert(IsFirstCharAtLeastThreeUtf8Bytes(value) && !IsFirstCharSurrogate(value), "First half of value should've been 0800..D7FF or E000..FFFF");
            Debug.Assert(IsSecondCharAtLeastThreeUtf8Bytes(value) && !IsSecondCharSurrogate(value), "Second half of value should've been 0800..D7FF or E000..FFFF");

            if (BitConverter.IsLittleEndian)
            {
                // value = [ ZZZZYYYY YYXXXXXX zzzzyyyy yyxxxxxx ]
                // want to write [ 1110ZZZZ 10xxxxxx 10yyyyyy 1110zzzz ] [ 10XXXXXX 10YYYYYY ]

                uint tempA = ((value << 2) & 0x3F00u) | ((value & 0x3Fu) << 16); // = [ 00000000 00xxxxxx 00yyyyyy 00000000 ]
                uint tempB = ((value >> 4) & 0x0F00_0000u) | ((value >> 12) & 0x0Fu); // = [ 0000ZZZZ 00000000 00000000 0000zzzz ]
                Unsafe.WriteUnaligned<uint>(ref outputBuffer, tempA + tempB + 0xE080_80E0u); // = [ 1110ZZZZ 10xxxxxx 10yyyyyy 1110zzzz ]
                Unsafe.WriteUnaligned<ushort>(ref Unsafe.Add(ref outputBuffer, 4), (ushort)(((value >> 22) & 0x3Fu) + ((value >> 8) & 0x3F00u) + 0x8080u)); // = [ 10XXXXXX 10YYYYYY ]
            }
            else
            {
                // value = [ zzzzyyyy yyxxxxxx ZZZZYYYY YYXXXXXX ]
                // want to write [ 1110zzzz ] [ 10yyyyyy ] [ 10xxxxxx ] [ 1110ZZZZ ] [ 10YYYYYY ] [ 10XXXXXX ]

                Unsafe.Add(ref outputBuffer, 5) = (byte)((value & 0x3Fu) | 0x80u);
                Unsafe.Add(ref outputBuffer, 4) = (byte)(((value >>= 6) & 0x3Fu) | 0x80u);
                Unsafe.Add(ref outputBuffer, 3) = (byte)(((value >>= 6) & 0x0Fu) | 0xE0u);
                Unsafe.Add(ref outputBuffer, 2) = (byte)(((value >>= 4) & 0x3Fu) | 0x80u);
                Unsafe.Add(ref outputBuffer, 1) = (byte)(((value >>= 6) & 0x3Fu) | 0x80u);
                outputBuffer = (byte)((value >>= 6) | 0xE0u);
            }
        }

        /// <summary>
        /// Given a DWORD which represents a buffer of 2 packed UTF-16 values in machine endianness,
        /// converts the first UTF-16 value to its 3-byte UTF-8 representation and writes the
        /// resulting 3 bytes to the destination buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteFirstUtf16CharAsUtf8ThreeByteSequence(ref byte outputBuffer, uint value)
        {
            Debug.Assert(IsFirstCharAtLeastThreeUtf8Bytes(value) && !IsFirstCharSurrogate(value), "First half of value should've been 0800..D7FF or E000..FFFF");

            if (BitConverter.IsLittleEndian)
            {
                // value = [ ######## ######## zzzzyyyy yyxxxxxx ]
                // want to write [ 10yyyyyy 1110zzzz ] [ 10xxxxxx ]

                uint tempA = (value << 2) & 0x3F00u; // [ 00yyyyyy 00000000 ]
                uint tempB = ((uint)(ushort)value >> 12); // [ 00000000 0000zzzz ]
                Unsafe.WriteUnaligned<ushort>(ref outputBuffer, (ushort)(tempA + tempB + 0x80E0u)); // [ 10yyyyyy 1110zzzz ]
                Unsafe.Add(ref outputBuffer, 2) = (byte)((value & 0x3Fu) | ~0x7Fu); // [ 10xxxxxx ]
            }
            else
            {
                // value = [ zzzzyyyy yyxxxxxx ######## ######## ]
                // want to write [ 1110zzzz ] [ 10yyyyyy ] [ 10xxxxxx ]

                Unsafe.Add(ref outputBuffer, 2) = (byte)(((value >>= 16) & 0x3Fu) | 0x80u);
                Unsafe.Add(ref outputBuffer, 1) = (byte)(((value >>= 6) & 0x3Fu) | 0x80u);
                outputBuffer = (byte)((value >>= 6) | 0xE0u);
            }
        }
    }
}
