// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text.Encodings.Web
{
    internal static class Ssse3Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(Vector128<sbyte> sourceValue)
            => CreateEscapingMask(sourceValue, s_bitMaskLookupBasicLatin, s_bitPosLookup, s_nibbleMaskSByte, s_nullMaskSByte);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask(
            Vector128<sbyte> sourceValue,
            Vector128<sbyte> bitMaskLookup,
            Vector128<sbyte> bitPosLookup,
            Vector128<sbyte> nibbleMaskSByte,
            Vector128<sbyte> nullMaskSByte)
        {
            // To check if an input byte needs to be escaped or not, we use a bitmask-lookup.
            // Therefore we split the input byte into the low- and high-nibble, which will get
            // the row-/column-index in the bit-mask.
            // The bitmask-lookup looks like (here for example s_bitMaskLookupBasicLatin):
            //                                     high-nibble
            // low-nibble  0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
            //         0   1   1   0   0   0   0   1   0   1   1   1   1   1   1   1   1
            //         1   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         2   1   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         3   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         4   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         5   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         6   1   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         7   1   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         8   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         9   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         A   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         B   1   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         C   1   1   0   1   0   1   0   0   1   1   1   1   1   1   1   1
            //         D   1   1   0   0   0   0   0   0   1   1   1   1   1   1   1   1
            //         E   1   1   0   1   0   0   0   0   1   1   1   1   1   1   1   1
            //         F   1   1   0   0   0   0   0   1   1   1   1   1   1   1   1   1
            //
            // where 1 denotes the need for escaping, while 0 means no escaping needed.
            // For high-nibbles in the range 8..F every input needs to be escaped, so we
            // can omit them in the bit-mask, thus only high-nibbles in the range 0..7 need
            // to be considered, hence the entries in the bit-mask can be of type byte.
            //
            // In the bitmask-lookup for each row (= low-nibble) a bit-mask for the
            // high-nibbles (= columns) is created.

            Debug.Assert(Ssse3.IsSupported);

            Vector128<sbyte> highNibbles = Sse2.And(Sse2.ShiftRightLogical(sourceValue.AsInt32(), 4).AsSByte(), nibbleMaskSByte);
            Vector128<sbyte> lowNibbles = Sse2.And(sourceValue, nibbleMaskSByte);

            Vector128<sbyte> bitMask = Ssse3.Shuffle(bitMaskLookup, lowNibbles);
            Vector128<sbyte> bitPositions = Ssse3.Shuffle(bitPosLookup, highNibbles);

            Vector128<sbyte> mask = Sse2.And(bitPositions, bitMask);

            mask = Sse2.CompareEqual(nullMaskSByte, Sse2.CompareEqual(nullMaskSByte, mask));
            return mask;
        }

        internal static readonly Vector128<sbyte> s_nibbleMaskSByte = Vector128.Create((sbyte)0xF);
        internal static readonly Vector128<sbyte> s_nullMaskSByte = Vector128<sbyte>.Zero;

        // See comment above in method CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin
        // for description of the bit-mask.
        private static readonly Vector128<sbyte> s_bitMaskLookupBasicLatin = Vector128.Create(
            0b_01000011,        // low-nibble 0
            0b_00000011,        // low-nibble 1
            0b_00000111,        // low-nibble 2
            0b_00000011,        // low-nibble 3
            0b_00000011,        // low-nibble 4
            0b_00000011,        // low-nibble 5
            0b_00000111,        // low-nibble 6
            0b_00000111,        // low-nibble 7
            0b_00000011,        // low-nibble 8
            0b_00000011,        // low-nibble 9
            0b_00000011,        // low-nibble A
            0b_00000111,        // low-nibble B
            0b_00101011,        // low-nibble C
            0b_00000011,        // low-nibble D
            0b_00001011,        // low-nibble E
            0b_10000011         // low-nibble F
        ).AsSByte();

        // To check if a bit in a bitmask from the Bitmask is set, in a sequential code
        // we would do ((1 << bitIndex) & bitmask) != 0
        // As there is no hardware instrinic for such a shift, we use a lookup that
        // stores the shifted bitpositions.
        // So (1 << bitIndex) becomes BitPosLook[bitIndex], which is simd-friendly.
        //
        // A bitmask from the Bitmask (above) is created only for values 0..7 (one byte),
        // so to avoid a explicit check for values outside 0..7, i.e.
        // high nibbles 8..F, we use a bitpos that always results in escaping.
        internal static readonly Vector128<sbyte> s_bitPosLookup = Vector128.Create(
            0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,     // high-nibble 0..7
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF      // high-nibble 8..F
        ).AsSByte();
    }
}
