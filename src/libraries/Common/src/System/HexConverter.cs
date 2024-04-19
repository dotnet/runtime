// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
#if SYSTEM_PRIVATE_CORELIB
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text.Unicode;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToCharsBuffer(byte value, Span<char> buffer, int startingIndex = 0, Casing casing = Casing.Upper)
        {
            uint difference = (((uint)value & 0xF0U) << 4) + ((uint)value & 0x0FU) - 0x8989U;
            uint packedResult = ((((uint)(-(int)difference) & 0x7070U) >> 4) + difference + 0xB9B9U) | (uint)casing;

            buffer[startingIndex + 1] = (char)(packedResult & 0xFF);
            buffer[startingIndex] = (char)(packedResult >> 8);
        }

#if SYSTEM_PRIVATE_CORELIB
        // Converts Vector128<byte> into 2xVector128<byte> ASCII Hex representation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        internal static (Vector128<byte>, Vector128<byte>) AsciiToHexVector128(Vector128<byte> src, Vector128<byte> hexMap)
        {
            Debug.Assert(Ssse3.IsSupported || AdvSimd.Arm64.IsSupported);
            // The algorithm is simple: a single srcVec (contains the whole 16b Guid) is converted
            // into nibbles and then, via hexMap, converted into a HEX representation via
            // Shuffle(nibbles, srcVec). ASCII is then expanded to UTF-16.
            Vector128<byte> shiftedSrc = Vector128.ShiftRightLogical(src.AsUInt64(), 4).AsByte();
            Vector128<byte> lowNibbles = Vector128.UnpackLow(shiftedSrc, src);
            Vector128<byte> highNibbles = Vector128.UnpackHigh(shiftedSrc, src);

            return (Vector128.ShuffleUnsafe(hexMap, lowNibbles & Vector128.Create((byte)0xF)),
                Vector128.ShuffleUnsafe(hexMap, highNibbles & Vector128.Create((byte)0xF)));
        }

        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static void EncodeToUtf16_Vector128(ReadOnlySpan<byte> bytes, Span<char> chars, Casing casing)
        {
            Debug.Assert(bytes.Length >= Vector128<int>.Count);

            ref byte srcRef = ref MemoryMarshal.GetReference(bytes);
            ref ushort destRef = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(chars));

            Vector128<byte> hexMap = casing == Casing.Upper ?
                Vector128.Create((byte)'0', (byte)'1', (byte)'2', (byte)'3',
                                 (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                                 (byte)'8', (byte)'9', (byte)'A', (byte)'B',
                                 (byte)'C', (byte)'D', (byte)'E', (byte)'F') :
                Vector128.Create((byte)'0', (byte)'1', (byte)'2', (byte)'3',
                                 (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                                 (byte)'8', (byte)'9', (byte)'a', (byte)'b',
                                 (byte)'c', (byte)'d', (byte)'e', (byte)'f');

            nuint pos = 0;
            nuint lengthSubVector128 = (nuint)bytes.Length - (nuint)Vector128<int>.Count;
            do
            {
                // This implementation processes 4 bytes of input at once, it can be easily modified
                // to support 16 bytes at once, but that didn't demonstrate noticeable wins
                // for Converter.ToHexString (around 8% faster for large inputs) so
                // it focuses on small inputs instead.

                uint i32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref srcRef, pos));
                Vector128<byte> vec = Vector128.CreateScalar(i32).AsByte();

                // JIT is expected to eliminate all unused calculations
                (Vector128<byte> hexLow, _) = AsciiToHexVector128(vec, hexMap);
                (Vector128<ushort> v0, _) = Vector128.Widen(hexLow);

                v0.StoreUnsafe(ref destRef, pos * 2);

                pos += (nuint)Vector128<int>.Count;
                if (pos == (nuint)bytes.Length)
                {
                    return;
                }

                // Overlap with the current chunk for trailing elements
                if (pos > lengthSubVector128)
                {
                    pos = lengthSubVector128;
                }

            } while (true);
        }
#endif

        public static void EncodeToUtf16(ReadOnlySpan<byte> bytes, Span<char> chars, Casing casing = Casing.Upper)
        {
            Debug.Assert(chars.Length >= bytes.Length * 2);

#if SYSTEM_PRIVATE_CORELIB
            if ((AdvSimd.Arm64.IsSupported || Ssse3.IsSupported) && bytes.Length >= 4)
            {
                EncodeToUtf16_Vector128(bytes, chars, casing);
                return;
            }
#endif
            for (int pos = 0; pos < bytes.Length; pos++)
            {
                ToCharsBuffer(bytes[pos], chars, pos * 2, casing);
            }
        }

        public static unsafe string ToString(ReadOnlySpan<byte> bytes, Casing casing = Casing.Upper)
        {
#if NETFRAMEWORK || NETSTANDARD2_0
            Span<char> result = bytes.Length > 16 ?
                new char[bytes.Length * 2].AsSpan() :
                stackalloc char[bytes.Length * 2];

            int pos = 0;
            foreach (byte b in bytes)
            {
                ToCharsBuffer(b, result, pos, casing);
                pos += 2;
            }
            return result.ToString();
#else
#pragma warning disable CS8500 // takes address of managed type
            return string.Create(bytes.Length * 2, (RosPtr: (IntPtr)(&bytes), casing), static (chars, args) =>
                EncodeToUtf16(*(ReadOnlySpan<byte>*)args.RosPtr, chars, args.casing));
#pragma warning restore CS8500
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

        public static bool TryDecodeFromUtf16(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsProcessed)
        {
#if SYSTEM_PRIVATE_CORELIB
            if (BitConverter.IsLittleEndian && (Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) &&
                chars.Length >= Vector128<ushort>.Count * 2)
            {
                return TryDecodeFromUtf16_Vector128(chars, bytes, out charsProcessed);
            }
#endif
            return TryDecodeFromUtf16_Scalar(chars, bytes, out charsProcessed);
        }

#if SYSTEM_PRIVATE_CORELIB
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(Ssse3))]
        public static bool TryDecodeFromUtf16_Vector128(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsProcessed)
        {
            Debug.Assert(Ssse3.IsSupported || AdvSimd.Arm64.IsSupported);
            Debug.Assert(chars.Length <= bytes.Length * 2);
            Debug.Assert(chars.Length % 2 == 0);
            Debug.Assert(chars.Length >= Vector128<ushort>.Count * 2);

            nuint offset = 0;
            nuint lengthSubTwoVector128 = (nuint)chars.Length - ((nuint)Vector128<ushort>.Count * 2);

            ref ushort srcRef = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(chars));
            ref byte destRef = ref MemoryMarshal.GetReference(bytes);

            do
            {
                // The algorithm is UTF8 so we'll be loading two UTF-16 vectors to narrow them into a
                // single UTF8 ASCII vector - the implementation can be shared with UTF8 paths.
                Vector128<ushort> vec1 = Vector128.LoadUnsafe(ref srcRef, offset);
                Vector128<ushort> vec2 = Vector128.LoadUnsafe(ref srcRef, offset + (nuint)Vector128<ushort>.Count);
                Vector128<byte> vec = Vector128.Narrow(vec1, vec2);

                // Based on "Algorithm #3" https://github.com/WojciechMula/toys/blob/master/simd-parse-hex/geoff_algorithm.cpp
                // by Geoff Langdale and Wojciech Mula
                // Move digits '0'..'9' into range 0xf6..0xff.
                Vector128<byte> t1 = vec + Vector128.Create((byte)(0xFF - '9'));
                // And then correct the range to 0xf0..0xf9.
                // All other bytes become less than 0xf0.
                Vector128<byte> t2 = Vector128.SubtractSaturate(t1, Vector128.Create((byte)6));
                // Convert into uppercase 'a'..'f' => 'A'..'F' and
                // move hex letter 'A'..'F' into range 0..5.
                Vector128<byte> t3 = (vec & Vector128.Create((byte)0xDF)) - Vector128.Create((byte)'A');
                // And correct the range into 10..15.
                // The non-hex letters bytes become greater than 0x0f.
                Vector128<byte> t4 = Vector128.AddSaturate(t3, Vector128.Create((byte)10));
                // Convert '0'..'9' into nibbles 0..9. Non-digit bytes become
                // greater than 0x0f. Finally choose the result: either valid nibble (0..9/10..15)
                // or some byte greater than 0x0f.
                Vector128<byte> nibbles = Vector128.Min(t2 - Vector128.Create((byte)0xF0), t4);
                // Any high bit is a sign that input is not a valid hex data
                if (!Utf16Utility.AllCharsInVectorAreAscii(vec1 | vec2) ||
                    Vector128.AddSaturate(nibbles, Vector128.Create((byte)(127 - 15))).ExtractMostSignificantBits() != 0)
                {
                    // Input is either non-ASCII or invalid hex data
                    break;
                }
                Vector128<byte> output;
                if (Ssse3.IsSupported)
                {
                    output = Ssse3.MultiplyAddAdjacent(nibbles,
                        Vector128.Create((short)0x0110).AsSByte()).AsByte();
                }
                else
                {
                    // Workaround for missing MultiplyAddAdjacent on ARM
                    Vector128<short> even = AdvSimd.Arm64.TransposeEven(nibbles, Vector128<byte>.Zero).AsInt16();
                    Vector128<short> odd = AdvSimd.Arm64.TransposeOdd(nibbles, Vector128<byte>.Zero).AsInt16();
                    even = AdvSimd.ShiftLeftLogical(even, 4).AsInt16();
                    output = AdvSimd.AddSaturate(even, odd).AsByte();
                }
                // Accumulate output in lower INT64 half and take care about endianness
                output = Vector128.Shuffle(output, Vector128.Create((byte)0, 2, 4, 6, 8, 10, 12, 14, 0, 0, 0, 0, 0, 0, 0, 0));
                // Store 8 bytes in dest by given offset
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destRef, offset / 2), output.AsUInt64().ToScalar());

                offset += (nuint)Vector128<ushort>.Count * 2;
                if (offset == (nuint)chars.Length)
                {
                    charsProcessed = chars.Length;
                    return true;
                }
                // Overlap with the current chunk for trailing elements
                if (offset > lengthSubTwoVector128)
                {
                    offset = lengthSubTwoVector128;
                }
            }
            while (true);

            // Fall back to the scalar routine in case of invalid input.
            bool fallbackResult = TryDecodeFromUtf16_Scalar(chars.Slice((int)offset), bytes.Slice((int)(offset / 2)), out int fallbackProcessed);
            charsProcessed = (int)offset + fallbackProcessed;
            return fallbackResult;
        }
#endif

        private static bool TryDecodeFromUtf16_Scalar(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsProcessed)
        {
            Debug.Assert(chars.Length % 2 == 0, "Un-even number of characters provided");
            Debug.Assert(chars.Length / 2 == bytes.Length, "Target buffer not right-sized for provided characters");

            int i = 0;
            int j = 0;
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
            if (IntPtr.Size == 8)
            {
                // This code path, when used, has no branches and doesn't depend on cache hits,
                // so it's faster and does not vary in speed depending on input data distribution.
                // We only use this logic on 64-bit systems, as using 64 bit values would otherwise
                // be much slower than just using the lookup table anyway (no hardware support).
                // The magic constant 18428868213665201664 is a 64 bit value containing 1s at the
                // indices corresponding to all the valid hex characters (ie. "0123456789ABCDEFabcdef")
                // minus 48 (ie. '0'), and backwards (so from the most significant bit and downwards).
                // The offset of 48 for each bit is necessary so that the entire range fits in 64 bits.
                // First, we subtract '0' to the input digit (after casting to uint to account for any
                // negative inputs). Note that even if this subtraction underflows, this happens before
                // the result is zero-extended to ulong, meaning that `i` will always have upper 32 bits
                // equal to 0. We then left shift the constant with this offset, and apply a bitmask that
                // has the highest bit set (the sign bit) if and only if `c` is in the ['0', '0' + 64) range.
                // Then we only need to check whether this final result is less than 0: this will only be
                // the case if both `i` was in fact the index of a set bit in the magic constant, and also
                // `c` was in the allowed range (this ensures that false positive bit shifts are ignored).
                ulong i = (uint)c - '0';
                ulong shift = 18428868213665201664UL << (int)i;
                ulong mask = i - 64;

                return (long)(shift & mask) < 0 ? true : false;
            }

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
        public static ReadOnlySpan<byte> CharToHexLookup =>
        [
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
        ];
    }
}
