// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Text.Encodings.Web
{
    internal static class AdvSimdHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(Vector128<short> sourceValue)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            // Anything in the control characters range, and anything above short.MaxValue but less than or equal char.MaxValue
            // That's because anything between 32768 and 65535 (inclusive) will overflow and become negative.
            Vector128<short> mask = AdvSimd.CompareLessThan(sourceValue, s_spaceMaskInt16);

            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_quotationMarkMaskInt16));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_reverseSolidusMaskInt16));

            // Anything above the ASCII range, and also including the leftover control character in the ASCII range - 0x7F
            // When this method is called with only ASCII data, 0x7F is the only value that would meet this comparison.
            // However, when called from "Default", the source could contain characters outside the ASCII range.
            mask = AdvSimd.Or(mask, AdvSimd.CompareGreaterThan(sourceValue, s_tildeMaskInt16));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(AdvSimd.IsSupported);

            // Anything in the control characters range (except 0x7F), and anything above sbyte.MaxValue but less than or equal byte.MaxValue
            // That's because anything between 128 and 255 (inclusive) will overflow and become negative.
            Vector128<sbyte> mask = AdvSimd.CompareLessThan(sourceValue, s_spaceMaskSByte);

            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_quotationMarkMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_reverseSolidusMaskSByte));

            // Leftover control character in the ASCII range - 0x7F
            // Since we are dealing with sbytes, 0x7F is the only value that would meet this comparison.
            mask = AdvSimd.Or(mask, AdvSimd.CompareGreaterThan(sourceValue, s_tildeMaskSByte));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(AdvSimd.IsSupported);

            Vector128<sbyte> mask = CreateEscapingMask_UnsafeRelaxedJavaScriptEncoder(sourceValue);

            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_ampersandMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_apostropheMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_plusSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_lessThanSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_greaterThanSignMaskSByte));
            mask = AdvSimd.Or(mask, AdvSimd.CompareEqual(sourceValue, s_graveAccentMaskSByte));

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> CreateAsciiMask(Vector128<short> sourceValue)
        {
            Debug.Assert(AdvSimd.IsSupported);

            // Anything above short.MaxValue but less than or equal char.MaxValue
            // That's because anything between 32768 and 65535 (inclusive) will overflow and become negative.
            Vector128<short> mask = AdvSimd.CompareLessThan(sourceValue, s_nullMaskInt16);

            // Anything above the ASCII range
            mask = AdvSimd.Or(mask, AdvSimd.CompareGreaterThan(sourceValue, s_maxAsciiCharacterMaskInt16));

            return mask;
        }

        // Encodes an operation equivalent to Sse2.MoveMask
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MoveMask(Vector128<byte> value)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            // extractedBits[i] = (value[i] & 0x80) == 0x80 & (1 << i);
            Vector128<byte> mostSignficantBitMask = s_mostSignficantBitMask;
            Vector128<byte> mostSignificantBitIsSet = AdvSimd.CompareEqual(AdvSimd.And(value, mostSignficantBitMask), mostSignficantBitMask);
            Vector128<byte> extractedBits = AdvSimd.And(mostSignificantBitIsSet, s_bitMask128);

            // self-pairwise add until all flags have moved to the first two bytes of the vector
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
            return extractedBits.AsInt32().ToScalar();
        }

        private static readonly Vector128<short> s_nullMaskInt16 = Vector128<short>.Zero;
        private static readonly Vector128<short> s_spaceMaskInt16 = Vector128.Create((short)' ');
        private static readonly Vector128<short> s_quotationMarkMaskInt16 = Vector128.Create((short)'"');
        private static readonly Vector128<short> s_reverseSolidusMaskInt16 = Vector128.Create((short)'\\');
        private static readonly Vector128<short> s_tildeMaskInt16 = Vector128.Create((short)'~');
        private static readonly Vector128<short> s_maxAsciiCharacterMaskInt16 = Vector128.Create((short)0x7F); // Delete control character

        private static readonly Vector128<sbyte> s_spaceMaskSByte = Vector128.Create((sbyte)' ');
        private static readonly Vector128<sbyte> s_quotationMarkMaskSByte = Vector128.Create((sbyte)'"');
        private static readonly Vector128<sbyte> s_ampersandMaskSByte = Vector128.Create((sbyte)'&');
        private static readonly Vector128<sbyte> s_apostropheMaskSByte = Vector128.Create((sbyte)'\'');
        private static readonly Vector128<sbyte> s_plusSignMaskSByte = Vector128.Create((sbyte)'+');
        private static readonly Vector128<sbyte> s_lessThanSignMaskSByte = Vector128.Create((sbyte)'<');
        private static readonly Vector128<sbyte> s_greaterThanSignMaskSByte = Vector128.Create((sbyte)'>');
        private static readonly Vector128<sbyte> s_reverseSolidusMaskSByte = Vector128.Create((sbyte)'\\');
        private static readonly Vector128<sbyte> s_graveAccentMaskSByte = Vector128.Create((sbyte)'`');
        private static readonly Vector128<sbyte> s_tildeMaskSByte = Vector128.Create((sbyte)'~');

        private static readonly Vector128<byte> s_mostSignficantBitMask = Vector128.Create((byte)0x80);
        private static readonly Vector128<byte> s_bitMask128 = BitConverter.IsLittleEndian ?
                                        Vector128.Create(0x80402010_08040201).AsByte() :
                                        Vector128.Create(0x01020408_10204080).AsByte();
    }
}
#endif
