// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#if SYSTEM_PRIVATE_CORELIB
using Internal.Runtime.CompilerServices;
#endif

namespace System
{
    internal static class HexConverter
    {
        public enum Casing : uint
        {
            // Output [ '0' .. '9' ] and [ 'A' .. 'F' ].
            Upper = 0,

            // Output [ '0' .. '9' ] and [ 'a' .. 'f' ].
            // This works because values in the range [ 0x30 .. 0x39 ] ([ '0' .. '9' ])
            // already have the 0x20 bit set, so ORing them with 0x20 is a no-op,
            // while outputs in the range [ 0x41 .. 0x46 ] ([ 'A' .. 'F' ])
            // don't have the 0x20 bit set, so ORing them maps to
            // [ 0x61 .. 0x66 ] ([ 'a' .. 'f' ]), which is what we want.
            Lower = 0x2020U,
        }

        // We want to pack the incoming byte into a single integer [ 0000 HHHH 0000 LLLL ],
        // where HHHH and LLLL are the high and low nibbles of the incoming byte. Then
        // subtract this integer from a constant minuend as shown below.
        //
        //   [ 1000 1001 1000 1001 ]
        // - [ 0000 HHHH 0000 LLLL ]
        // =========================
        //   [ *YYY **** *ZZZ **** ]
        //
        // The end result of this is that YYY is 0b000 if HHHH <= 9, and YYY is 0b111 if HHHH >= 10.
        // Similarly, ZZZ is 0b000 if LLLL <= 9, and ZZZ is 0b111 if LLLL >= 10.
        // (We don't care about the value of asterisked bits.)
        //
        // To turn a nibble in the range [ 0 .. 9 ] into hex, we calculate hex := nibble + 48 (ascii '0').
        // To turn a nibble in the range [ 10 .. 15 ] into hex, we calculate hex := nibble - 10 + 65 (ascii 'A').
        //                                                                => hex := nibble + 55.
        // The difference in the starting ASCII offset is (55 - 48) = 7, depending on whether the nibble is <= 9 or >= 10.
        // Since 7 is 0b111, this conveniently matches the YYY or ZZZ value computed during the earlier subtraction.

        // The commented out code below is code that directly implements the logic described above.

        // uint packedOriginalValues = (((uint)value & 0xF0U) << 4) + ((uint)value & 0x0FU);
        // uint difference = 0x8989U - packedOriginalValues;
        // uint add7Mask = (difference & 0x7070U) >> 4; // line YYY and ZZZ back up with the packed values
        // uint packedResult = packedOriginalValues + add7Mask + 0x3030U /* ascii '0' */;

        // The code below is equivalent to the commented out code above but has been tweaked
        // to allow codegen to make some extra optimizations.

        // The low byte of the packed result contains the hex representation of the incoming byte's low nibble.
        // The adjacent byte of the packed result contains the hex representation of the incoming byte's high nibble.

        // Finally, write to the output buffer starting with the *highest* index so that codegen can
        // elide all but the first bounds check. (This only works if 'startingIndex' is a compile-time constant.)

        // The JIT can elide bounds checks if 'startingIndex' is constant and if the caller is
        // writing to a span of known length (or the caller has already checked the bounds of the
        // furthest access).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToBytesBuffer(byte value, Span<byte> buffer, int startingIndex = 0, Casing casing = Casing.Upper)
        {
            uint difference = (((uint)value & 0xF0U) << 4) + ((uint)value & 0x0FU) - 0x8989U;
            uint packedResult = ((((uint)(-(int)difference) & 0x7070U) >> 4) + difference + 0xB9B9U) | (uint)casing;

            buffer[startingIndex + 1] = (byte)packedResult;
            buffer[startingIndex] = (byte)(packedResult >> 8);
        }

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToCharsBuffer(byte value, Span<char> buffer, int startingIndex = 0, Casing casing = Casing.Upper)
        {
            uint difference = (((uint)value & 0xF0U) << 4) + ((uint)value & 0x0FU) - 0x8989U;
            uint packedResult = ((((uint)(-(int)difference) & 0x7070U) >> 4) + difference + 0xB9B9U) | (uint)casing;

            buffer[startingIndex + 1] = (char)(packedResult & 0xFF);
            buffer[startingIndex] = (char)(packedResult >> 8);
        }

        private static unsafe int ToCharsBufferAvx2(byte* bytes, int bytesCount, char* chars, Casing casing)
        {
            Debug.Assert(Avx2.IsSupported);
            Debug.Assert(bytesCount >= 32);

            Vector256<byte> x00 = Vector256<byte>.Zero;
            Vector256<byte> x0F = Vector256.Create((byte)0x0F);
            Vector256<byte> hexLookupTable = casing == Casing.Lower ?
                CreateVector(LowerHexLookupTable) :
                CreateVector(UpperHexLookupTable);

            int bytesToRead = RoundDownToNext32(bytesCount);
            byte* eof = bytes + bytesToRead;
            byte* charsAsByte = (byte*)chars;
            do
            {
                Vector256<byte> value = Avx.LoadVector256(bytes);
                bytes += 32;

                Vector256<byte> hiShift = Avx2.ShiftRightLogical(value.AsInt16(), 4).AsByte();
                Vector256<byte> loHalf = Avx2.And(value, x0F);
                Vector256<byte> hiHalf = Avx2.And(hiShift, x0F);
                Vector256<byte> lo02 = Avx2.UnpackLow(hiHalf, loHalf);
                Vector256<byte> hi13 = Avx2.UnpackHigh(hiHalf, loHalf);

                Vector256<byte> resLo = Avx2.Shuffle(hexLookupTable, lo02);
                Vector256<byte> resHi = Avx2.Shuffle(hexLookupTable, hi13);

                Vector256<byte> ae = Avx2.UnpackLow(resLo, x00);
                Vector256<byte> bf = Avx2.UnpackHigh(resLo, x00);
                Vector256<byte> cg = Avx2.UnpackLow(resHi, x00);
                Vector256<byte> dh = Avx2.UnpackHigh(resHi, x00);

                Vector256<byte> ab = Avx2.Permute2x128(ae, bf, 0b0010_0000);
                Vector256<byte> ef = Avx2.Permute2x128(ae, bf, 0b0011_0001);
                Vector256<byte> cd = Avx2.Permute2x128(cg, dh, 0b0010_0000);
                Vector256<byte> gh = Avx2.Permute2x128(cg, dh, 0b0011_0001);

                Avx.Store(charsAsByte, ab);
                charsAsByte += 32;
                Avx.Store(charsAsByte, cd);
                charsAsByte += 32;
                Avx.Store(charsAsByte, ef);
                charsAsByte += 32;
                Avx.Store(charsAsByte, gh);
                charsAsByte += 32;
            } while (bytes != eof);

            return bytesToRead;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EncodeToUtf16Core(byte* bytes, int bytesCount, Span<char> chars, Casing casing)
        {
            int pos = 0;
            if (Avx2.IsSupported && bytesCount >= 32)
            {
                Debug.Assert(!chars.IsEmpty);
                fixed (char* charPtr = &MemoryMarshal.GetReference(chars))
                {
                    pos = ToCharsBufferAvx2(bytes, bytesCount, charPtr, casing);
                }
                Debug.Assert(pos == (bytesCount / 32) * 32);
            }
            for (; pos < bytesCount; ++pos)
            {
                ToCharsBuffer(bytes[pos], chars, pos * 2, casing);
            }
        }

        public static unsafe void EncodeToUtf16(ReadOnlySpan<byte> bytes, Span<char> chars, Casing casing = Casing.Upper)
        {
            Debug.Assert(chars.Length >= bytes.Length * 2);
            fixed (byte* bytesPtr = bytes)
            {
                EncodeToUtf16Core(bytesPtr, bytes.Length, chars, casing);
            }
        }

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        public static unsafe string ToString(ReadOnlySpan<byte> bytes, Casing casing = Casing.Upper)
        {
#if NET45 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472 || NETSTANDARD1_0 || NETSTANDARD1_3 || NETSTANDARD2_0
            Span<char> result = stackalloc char[0];
            if (bytes.Length > 16)
            {
                var array = new char[bytes.Length * 2];
                result = array.AsSpan();
            }
            else
            {
                result = stackalloc char[bytes.Length * 2];
            }

            int pos = 0;
            foreach (byte b in bytes)
            {
                ToCharsBuffer(b, result, pos, casing);
                pos += 2;
            }
            return result.ToString();
#else
            fixed (byte* bytesPtr = bytes)
            {
                return string.Create(bytes.Length * 2, (Ptr: (IntPtr)bytesPtr, bytes.Length, casing), (chars, args) =>
                {
                    EncodeToUtf16Core((byte*)args.Ptr, args.Length, chars, args.casing);
                });
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToCharUpper(int value)
        {
            value &= 0xF;
            value += '0';

            if (value > '9')
            {
                value += ('A' - ('9' + 1));
            }

            return (char)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToCharLower(int value)
        {
            value &= 0xF;
            value += '0';

            if (value > '9')
            {
                value += ('a' - ('9' + 1));
            }

            return (char)value;
        }

        public static bool TryDecodeFromUtf16(ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            return TryDecodeFromUtf16(chars, bytes, out _);
        }

        public static unsafe bool TryDecodeFromUtf16(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsProcessed)
        {
            Debug.Assert(chars.Length % 2 == 0, "Un-even number of characters provided");
            Debug.Assert(chars.Length / 2 == bytes.Length, "Target buffer not right-sized for provided characters");

            int i = 0;
            int j = 0;
            if (Avx2.IsSupported && bytes.Length > 32)
            {
                fixed (char* charPtr = &MemoryMarshal.GetReference(chars))
                fixed (byte* bytePtr = &MemoryMarshal.GetReference(bytes))
                {
                    j = DecodeFromUtf16Avx2(charPtr, bytePtr, bytes.Length);
                    Debug.Assert(j % 32 == 0);
                    Debug.Assert(j <= bytes.Length);
                    i = j * 2;
                }
            }

            int byteLo = 0;
            int byteHi = 0;
            while (j < bytes.Length)
            {
                byteLo = FromChar(chars[i + 1]);
                byteHi = FromChar(chars[i]);

                // byteHi hasn't been shifted to the high half yet, so the only way the bitwise or produces this pattern
                // is if either byteHi or byteLo was not a hex character.
                if ((byteLo | byteHi) == 0xFF)
                    break;

                bytes[j++] = (byte)((byteHi << 4) | byteLo);
                i += 2;
            }

            if (byteLo == 0xFF)
                i++;

            charsProcessed = i;
            return (byteLo | byteHi) != 0xFF;
        }

        private static unsafe int DecodeFromUtf16Avx2(char* chars, byte* bytes, int bytesCount)
        {
            Debug.Assert(Avx2.IsSupported);

            Vector256<byte> x0F = Vector256.Create((byte) 0x0F);
            Vector256<byte> xF0 = Vector256.Create((byte) 0xF0);
            Vector256<byte> digHexSelector = CreateVector(UpperLowerDigHexSelector);
            Vector256<byte> digits = CreateVector(Digits);
            Vector256<byte> hexs = CreateVector(Hexs);
            Vector256<byte> evenBytes = CreateVector(EvenBytes);
            Vector256<byte> oddBytes = CreateVector(OddBytes);

            int bytesToWrite = RoundDownToNext32(bytesCount);
            byte* eof = bytes + bytesToWrite;
            byte* dest = bytes;
            byte* charsAsByte = (byte*)chars;
            int leftOk, rightOk;
            while (dest != eof)
            {
                Vector256<short> a = Avx.LoadVector256(charsAsByte).AsInt16();
                charsAsByte += 32;
                Vector256<short> b = Avx.LoadVector256(charsAsByte).AsInt16();
                charsAsByte += 32;
                Vector256<short> c = Avx.LoadVector256(charsAsByte).AsInt16();
                charsAsByte += 32;
                Vector256<short> d = Avx.LoadVector256(charsAsByte).AsInt16();
                charsAsByte += 32;

                Vector256<byte> ab = Avx2.PackUnsignedSaturate(a, b);
                Vector256<byte> cd = Avx2.PackUnsignedSaturate(c, d);

                Vector256<byte> inputLeft = Avx2.Permute4x64(ab.AsUInt64(), 0b11_01_10_00).AsByte();
                Vector256<byte> inputRight = Avx2.Permute4x64(cd.AsUInt64(), 0b11_01_10_00).AsByte();

                Vector256<byte> loNibbleLeft = Avx2.And(inputLeft, x0F);
                Vector256<byte> loNibbleRight = Avx2.And(inputRight, x0F);

                Vector256<byte> hiNibbleLeft = Avx2.And(inputLeft, xF0);
                Vector256<byte> hiNibbleRight = Avx2.And(inputRight, xF0);

                Vector256<byte> leftDigits = Avx2.Shuffle(digits, loNibbleLeft);
                Vector256<byte> leftHex = Avx2.Shuffle(hexs, loNibbleLeft);

                Vector256<byte> hiNibbleShLeft = Avx2.ShiftRightLogical(hiNibbleLeft.AsInt16(), 4).AsByte();
                Vector256<byte> hiNibbleShRight = Avx2.ShiftRightLogical(hiNibbleRight.AsInt16(), 4).AsByte();

                Vector256<byte> rightDigits = Avx2.Shuffle(digits, loNibbleRight);
                Vector256<byte> rightHex = Avx2.Shuffle(hexs, loNibbleRight);

                Vector256<byte> magicLeft = Avx2.Shuffle(digHexSelector, hiNibbleShLeft);
                Vector256<byte> magicRight = Avx2.Shuffle(digHexSelector, hiNibbleShRight);

                Vector256<byte> valueLeft = Avx2.BlendVariable(leftDigits, leftHex, magicLeft);
                Vector256<byte> valueRight = Avx2.BlendVariable(rightDigits, rightHex, magicRight);

                Vector256<byte> errLeft = Avx2.ShiftLeftLogical(magicLeft.AsInt16(), 7).AsByte();
                Vector256<byte> errRight = Avx2.ShiftLeftLogical(magicRight.AsInt16(), 7).AsByte();

                Vector256<byte> evenBytesLeft = Avx2.Shuffle(valueLeft, evenBytes);
                Vector256<byte> oddBytesLeft = Avx2.Shuffle(valueLeft, oddBytes);
                Vector256<byte> evenBytesRight = Avx2.Shuffle(valueRight, evenBytes);
                Vector256<byte> oddBytesRight = Avx2.Shuffle(valueRight, oddBytes);

                evenBytesLeft = Avx2.ShiftLeftLogical(evenBytesLeft.AsUInt16(), 4).AsByte();
                evenBytesRight = Avx2.ShiftLeftLogical(evenBytesRight.AsUInt16(), 4).AsByte();

                evenBytesLeft = Avx2.Or(evenBytesLeft, oddBytesLeft);
                evenBytesRight = Avx2.Or(evenBytesRight, oddBytesRight);

                Vector256<byte> result = Merge(evenBytesLeft, evenBytesRight);

                Vector256<byte> validationResultLeft = Avx2.Or(errLeft, valueLeft);
                Vector256<byte> validationResultRight = Avx2.Or(errRight, valueRight);

                leftOk = Avx2.MoveMask(validationResultLeft);
                rightOk = Avx2.MoveMask(validationResultRight);

                if ((leftOk | rightOk) != 0) break;

                Avx.Store(dest, result);
                dest += 32;
            }

            return bytesToWrite - (int) (eof - dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromChar(int c)
        {
            return c >= CharToHexLookup.Length ? 0xFF : CharToHexLookup[c];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromUpperChar(int c)
        {
            return c > 71 ? 0xFF : CharToHexLookup[c];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromLowerChar(int c)
        {
            if ((uint)(c - '0') <= '9' - '0')
                return c - '0';

            if ((uint)(c - 'a') <= 'f' - 'a')
                return c - 'a' + 10;

            return 0xFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHexChar(int c)
        {
            return FromChar(c) != 0xFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHexUpperChar(int c)
        {
            return (uint)(c - '0') <= 9 || (uint)(c - 'A') <= ('F' - 'A');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHexLowerChar(int c)
        {
            return (uint)(c - '0') <= 9 || (uint)(c - 'a') <= ('f' - 'a');
        }

        /// <summary>Map from an ASCII char to its hex value, e.g. arr['b'] == 11. 0xFF means it's not a hex digit.</summary>
        public static ReadOnlySpan<byte> CharToHexLookup => new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 15
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 31
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 47
            0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 63
            0xFF, 0xA,  0xB,  0xC,  0xD,  0xE,  0xF,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 79
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 95
            0xFF, 0xa,  0xb,  0xc,  0xd,  0xe,  0xf,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 111
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 127
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 143
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 159
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 175
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 191
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 207
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 223
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 239
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF  // 255
        };

        private static ReadOnlySpan<byte> LowerHexLookupTable => new byte[]
        {
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66
        };

        private static ReadOnlySpan<byte> UpperHexLookupTable => new byte[]
        {
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46
        };

        private static ReadOnlySpan<byte> UpperLowerDigHexSelector => new byte[]
        {
            0x01, 0x01, 0x01, 0x00, 0x80, 0x01, 0x80, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x00, 0x80, 0x01, 0x80, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01
        };

        private static ReadOnlySpan<byte> Digits => new byte[]
        {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        private static ReadOnlySpan<byte> Hexs => new byte[]
        {
            0xFF, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        private static ReadOnlySpan<byte> EvenBytes => new byte[]
        {
            0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30,
            0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30
        };

        private static ReadOnlySpan<byte> OddBytes => new byte[]
        {
            1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31,
            1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RoundDownToNext32(int x)
            => x & 0x7FFFFFE0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<byte> CreateVector(ReadOnlySpan<byte> data)
            => Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(data));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<byte> Merge(Vector256<byte> a, Vector256<byte> b)
        {
            Vector256<ulong> a1 = Avx2.Permute4x64(a.AsUInt64(), 0b11_10_10_00);
            Vector256<ulong> b1 = Avx2.Permute4x64(b.AsUInt64(), 0b11_00_01_00);
            return Avx2.Blend(a1.AsUInt32(), b1.AsUInt32(), 0b1111_0000).AsByte();
        }
    }
}
